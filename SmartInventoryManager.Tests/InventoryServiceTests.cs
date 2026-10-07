using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Business.Validators;

namespace SmartInventoryManager.Tests;

[TestClass]
public class InventoryServiceTests
{
    [TestMethod]
    public void ImportValidator_RejectsInvalidDetail()
    {
        var errors = new ImportValidator().ValidateDetail(0, 0, -1m);

        Assert.AreEqual(3, errors.Count);
    }

    [TestMethod]
    public void ExportValidator_AcceptsValidDetail()
    {
        var isValid = new ExportValidator().IsValidDetail(1, 5, 10m);

        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void ExportValidator_RejectsZeroQuantity()
    {
        var errors = new ExportValidator().ValidateDetail(1, 0, 10m);

        CollectionAssert.Contains(errors.ToList(), "Số lượng xuất phải lớn hơn 0.");
    }

    [TestMethod]
    public void LoginValidation_AcceptsUsername()
    {
        var errors = new AuthService(CreateDbContext()).ValidateLoginCredentials("admin", "SmartInventoryManager");

        Assert.AreEqual(0, errors.Count);
    }

    private static InventoryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>().Options;
        return new InventoryDbContext(options);
    }
}
