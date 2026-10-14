using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Tests;

internal static class SqlDeadlockFixture
{
    internal static async Task RunAsync(DbContextOptions<InventoryDbContext> options)
    {
        await using var db = new InventoryDbContext(options);
        var token = "DLTEST_" + Guid.NewGuid().ToString("N")[..12];
        var category = new Category { CategoryName = token, IsActive = true };
        var warehouse = new Warehouse { WarehouseCode = token, WarehouseName = token, IsActive = true };
        db.AddRange(category, warehouse); await db.SaveChangesAsync();
        var products = Enumerable.Range(1, 2).Select(i => new Product { ProductCode = token + i, ProductName = token + i,
            CategoryId = category.CategoryId, Unit = "piece", ReorderPoint = 1, MaximumStock = 10, IsActive = true }).ToArray();
        try
        {
            db.Products.AddRange(products); await db.SaveChangesAsync();
            var stocks = products.Select(product => new Inventory { ProductId = product.ProductId, WarehouseId = warehouse.WarehouseId, Quantity = 0 }).ToArray();
            db.Inventories.AddRange(stocks); await db.SaveChangesAsync();
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var locked = 0;
            var counts = new int[2];
            var method = typeof(ImportService).Assembly.GetType("SmartInventoryManager.Business.Services.DeadlockRetry")!
                .GetMethod("RunAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
            async Task Run(int operation)
            {
                var committed = false;
                async Task Attempt()
                {
                    var attempt = Interlocked.Increment(ref counts[operation]);
                    await using var context = new InventoryDbContext(options);
                    await using var tx = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                    var first = stocks[operation].InventoryId;
                    var second = stocks[1 - operation].InventoryId;
                    await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Inventory WITH (ROWLOCK) SET Quantity=Quantity+1 WHERE InventoryId={first}");
                    if (attempt == 1)
                    {
                        if (Interlocked.Increment(ref locked) == 2) ready.TrySetResult();
                        await ready.Task;
                    }
                    await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Inventory WITH (ROWLOCK) SET Quantity=Quantity+1 WHERE InventoryId={second}");
                    await tx.CommitAsync(); committed = true;
                }
                var task = (Task)method.Invoke(null, [new Func<Task>(Attempt), new Action(() => { }), new Func<bool>(() => committed), CancellationToken.None])!;
                await task;
            }
            await Task.WhenAll(Run(0), Run(1));
            db.ChangeTracker.Clear();
            var quantities = await db.Inventories.Where(item => item.WarehouseId == warehouse.WarehouseId).Select(item => item.Quantity).ToArrayAsync();
            if (quantities.Length != 2 || quantities.Any(q => q != 2) || counts.Sum() <= 2 || counts.Any(c => c > 3))
                throw new Exception("Deadlock retry did not preserve exactly-once committed effects");
            Console.WriteLine($"PASS: controlled real SQL1205 retry; attempts {counts[0]}/{counts[1]}, both quantities2 (rollback victim changes not replayed)");
        }
        finally
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync();
            await db.Inventories.Where(item => item.WarehouseId == warehouse.WarehouseId).ExecuteDeleteAsync();
            await db.Products.Where(item => item.CategoryId == category.CategoryId && item.ProductCode.StartsWith(token)).ExecuteDeleteAsync();
            await db.Warehouses.Where(item => item.WarehouseId == warehouse.WarehouseId && item.WarehouseCode == token).ExecuteDeleteAsync();
            await db.Categories.Where(item => item.CategoryId == category.CategoryId && item.CategoryName == token).ExecuteDeleteAsync();
            await tx.CommitAsync();
            Console.WriteLine("PASS: controlled deadlock fixture removed");
        }
    }
}
