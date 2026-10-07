using SmartInventoryManager.Business.Inventory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SmartInventoryManager.Tests;

[TestClass]
public class ProductValidatorTests
{
    [DataTestMethod]
    [DataRow(0, 10, 100, StockStatus.OutOfStock)]
    [DataRow(1, 1, 100, StockStatus.LowStock)]
    [DataRow(10, 10, 100, StockStatus.LowStock)]
    [DataRow(11, 10, 100, StockStatus.Normal)]
    [DataRow(100, 10, 100, StockStatus.Normal)]
    [DataRow(101, 10, 100, StockStatus.Overstock)]
    public void Calculate_ReturnsExpectedBoundaryStatus(
        int quantity,
        int reorderPoint,
        int maximumStock,
        StockStatus expected)
    {
        var result = StockStatusCalculator.Calculate(quantity, reorderPoint, maximumStock);

        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Calculate_RejectsNegativeQuantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StockStatusCalculator.Calculate(-1, 10, 100));
    }

    [TestMethod]
    public void ValidateThresholds_RejectsReorderPointNotBelowMaximumStock()
    {
        var errors = StockStatusCalculator.ValidateThresholds(100, 100);

        Assert.IsTrue(errors.Count > 0);
    }
}
