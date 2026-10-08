using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Tests;

[TestClass]
public class ProductManagementServiceTests
{
    [TestMethod]
    public async Task ProductLifecycle_LoadsCategoryAfterCreateUpdateAndDeactivation()
    {
        using var db = CreateDbContext();
        db.Categories.AddRange(
            new Category { CategoryId = 1, CategoryName = "Điện tử", IsActive = true },
            new Category { CategoryId = 2, CategoryName = "Gia dụng", IsActive = true });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = CreateService(db);
        var session = CreateSession();

        var id = await service.CreateAsync(session, Request(1));
        Assert.IsNull(db.Products.Local.Single().Category);
        db.ChangeTracker.Clear();

        var detail = await service.GetByIdAsync(session, id);
        Assert.IsNotNull(detail);
        Assert.AreEqual("Điện tử", detail.CategoryName);
        var list = await service.SearchAsync(session, new ProductSearchDto());
        Assert.AreEqual(1, list.TotalCount);
        Assert.AreEqual("Điện tử", list.Items.Single().CategoryName);
        Assert.AreEqual(0, db.ChangeTracker.Entries().Count());

        await service.UpdateAsync(session, id, Request(2));
        db.ChangeTracker.Clear();
        detail = await service.GetByIdAsync(session, id);
        Assert.IsNotNull(detail);
        Assert.AreEqual(2, detail.CategoryId);
        Assert.AreEqual("Gia dụng", detail.CategoryName);
        list = await service.SearchAsync(session, new ProductSearchDto { CategoryId = 2, Search = "P001" });
        Assert.AreEqual("Gia dụng", list.Items.Single().CategoryName);

        await service.SetActiveAsync(session, id, false);
        db.ChangeTracker.Clear();
        list = await service.SearchAsync(session, new ProductSearchDto { IsActive = true });
        Assert.AreEqual(0, list.TotalCount);
        detail = await service.GetByIdAsync(session, id);
        Assert.IsNotNull(detail);
        Assert.IsFalse(detail.IsActive);
        Assert.AreEqual("Gia dụng", detail.CategoryName);
        Assert.IsNull(await service.GetByIdAsync(session, int.MaxValue));
    }

    [TestMethod]
    public async Task CreateAndUpdate_RejectMissingOrInactiveCategoryWithoutSavingChanges()
    {
        using var db = CreateDbContext();
        db.Categories.AddRange(
            new Category { CategoryId = 1, CategoryName = "Active", IsActive = true },
            new Category { CategoryId = 2, CategoryName = "Inactive", IsActive = false });
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var session = CreateSession();
        foreach (var categoryId in new[] { 0, -1, 999, 2 })
        {
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.CreateAsync(session, Request(categoryId)));
        }
        Assert.AreEqual(0, await db.Products.CountAsync());
        var id = await service.CreateAsync(session, Request(1));
        foreach (var categoryId in new[] { 0, -1, 999, 2 })
        {
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.UpdateAsync(session, id, Request(categoryId)));
        }
        db.ChangeTracker.Clear();
        Assert.AreEqual(1, (await service.GetByIdAsync(session, id))!.CategoryId);
    }

    [TestMethod]
    public async Task Reads_LoadInactiveCategoryForExistingProduct()
    {
        using var db = CreateDbContext();
        db.Categories.Add(new Category { CategoryId = 1, CategoryName = "Inactive", IsActive = false });
        db.Products.Add(new Product { ProductCode = "P001", ProductName = "Product", Unit = "Unit", CategoryId = 1 });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = CreateService(db);
        Assert.AreEqual("Inactive", (await service.SearchAsync(CreateSession(), new ProductSearchDto())).Items.Single().CategoryName);
        Assert.AreEqual("Inactive", (await service.GetByIdAsync(CreateSession(), 1))!.CategoryName);
    }

    [TestMethod]
    public void ToDto_RejectsUnloadedCategoryWithDiagnosticError()
    {
        var mapper = typeof(ProductManagementService).GetMethod("ToDto", BindingFlags.NonPublic | BindingFlags.Static)!;
        var exception = Assert.ThrowsException<TargetInvocationException>(() =>
            mapper.Invoke(null, new object[] { new Product { ProductId = 7, CategoryId = 99 } }));
        Assert.IsInstanceOfType<InvalidOperationException>(exception.InnerException);
        StringAssert.Contains(exception.InnerException!.Message, "7");
        StringAssert.Contains(exception.InnerException.Message, "99");
    }

    [TestMethod]
    public void ProductCategoryRelationship_IsRequiredAndRestrictsDeletion()
    {
        using var db = CreateDbContext();
        var relationship = db.Model.FindEntityType(typeof(Product))!.GetForeignKeys()
            .Single(key => key.PrincipalEntityType.ClrType == typeof(Category));
        Assert.IsTrue(relationship.IsRequired);
        Assert.AreEqual(nameof(Product.CategoryId), relationship.Properties.Single().Name);
        Assert.AreEqual(DeleteBehavior.Restrict, relationship.DeleteBehavior);
    }

    private static InventoryDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ProductManagementService CreateService(InventoryDbContext db) => new(db, new AuthorizationService(db));

    private static UserSession CreateSession()
    {
        var session = new UserSession();
        session.SignIn(new AuthenticatedUserDto(1, "admin", "Admin", null, RoleNames.Admin));
        return session;
    }

    private static ProductRequestDto Request(int categoryId) => new()
    {
        ProductCode = "P001", ProductName = "Product", CategoryId = categoryId,
        Unit = "Unit", PurchasePrice = 10m, SellingPrice = 20m, ReorderPoint = 1, MaximumStock = 10
    };
}
