using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Inventory;

namespace SmartInventoryManager.Tests;

[TestClass]
public class StockStatusCalculatorTests
{
    [DataTestMethod]
    [DataRow(0, 10, 100, StockStatus.OutOfStock)]
    [DataRow(10, 10, 100, StockStatus.LowStock)]
    [DataRow(11, 10, 100, StockStatus.Normal)]
    [DataRow(100, 10, 100, StockStatus.Normal)]
    [DataRow(101, 10, 100, StockStatus.Overstock)]
    public void Calculate_UsesLockedBoundaries(int quantity, int reorderPoint, int maximumStock, StockStatus expected)
    {
        Assert.AreEqual(expected, StockStatusCalculator.Calculate(quantity, reorderPoint, maximumStock));
    }

    [DataTestMethod]
    [DataRow(-1, 10)]
    [DataRow(10, 10)]
    [DataRow(11, 10)]
    [DataRow(0, 0)]
    public void ValidateThresholds_RejectsInvalidConfiguration(int reorderPoint, int maximumStock)
    {
        Assert.IsFalse(StockStatusCalculator.HasValidThresholds(reorderPoint, maximumStock));
    }

    [TestMethod]
    public void Calculate_RejectsNegativeQuantity()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => StockStatusCalculator.Calculate(-1, 10, 100));
    }
}
