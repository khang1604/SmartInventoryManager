using SmartInventoryManager.Business.Inventory;
using SmartInventoryManager.Business.Validators;
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
    public void ValidateThresholds_RejectsEqualReorderPointAndMaximumStock()
    {
        var errors = StockStatusCalculator.ValidateThresholds(100, 100);

        Assert.AreEqual(1, errors.Count);
    }

    [TestMethod]
    public void ValidateThresholds_RejectsNegativeThresholds()
    {
        var errors = StockStatusCalculator.ValidateThresholds(-1, 0);

        Assert.AreEqual(1, errors.Count);
    }

    [TestMethod]
    public void Calculate_RejectsZeroMaximumWhenReorderPointIsZero()
    {
        Assert.Throws<ArgumentException>(() => StockStatusCalculator.Calculate(5, 0, 0));
    }

    [TestMethod]
    public void Calculate_ReturnsLowStockImmediatelyAboveZeroWhenReorderPointIsPositive()
    {
        var result = StockStatusCalculator.Calculate(1, 10, 100);

        Assert.AreEqual(StockStatus.LowStock, result);
    }

    [TestMethod]
    public void ProductValidator_AcceptsValidProduct()
    {
        var isValid = new ProductValidator().IsValid(
            "P001", "Sản phẩm", "cái", 10m, 15m, 10, 100);

        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void ProductValidator_ReportsRequiredFieldsAndInvalidValues()
    {
        var errors = new ProductValidator().Validate(
            " ", null, "", -1m, -2m, 100, 100);

        Assert.AreEqual(6, errors.Count);
        CollectionAssert.Contains(errors.ToList(), "Mã sản phẩm là bắt buộc và không quá 50 ký tự.");
        CollectionAssert.Contains(errors.ToList(), "Giá nhập không được âm.");
    }

    [TestMethod]
    public void ProductValidator_RejectsZeroMaximumStockWhenReorderPointIsZero()
    {
        var errors = new ProductValidator().Validate(
            "P001", "Sản phẩm", "cái", 1m, 2m, 0, 0);

        Assert.AreEqual(1, errors.Count);
    }

    [TestMethod]
    public void ProductValidator_RejectsFieldsLongerThanDatabaseLimits()
    {
        var errors = new ProductValidator().Validate(
            new string('P', 51), new string('N', 201), new string('U', 51), 1m, 2m, 1, 10);

        Assert.AreEqual(3, errors.Count);
    }
}
