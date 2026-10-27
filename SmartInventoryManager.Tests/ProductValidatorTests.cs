using SmartInventoryManager.Business.Inventory;
using SmartInventoryManager.Business.Validators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SmartInventoryManager.Tests;

[TestClass]
public class ProductValidatorTests
{
    // Kiểm tra trạng thái tồn được tính đúng tại các giá trị biên.
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

    // Kiểm tra tính trạng thái tồn từ chối số lượng âm.
    [TestMethod]
    public void Calculate_RejectsNegativeQuantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StockStatusCalculator.Calculate(-1, 10, 100));
    }

    // Kiểm tra ngưỡng đặt hàng không được bằng mức tồn tối đa.
    [TestMethod]
    public void ValidateThresholds_RejectsEqualReorderPointAndMaximumStock()
    {
        var errors = StockStatusCalculator.ValidateThresholds(100, 100);

        Assert.AreEqual(1, errors.Count);
    }

    // Kiểm tra cấu hình ngưỡng tồn từ chối các giá trị âm.
    [TestMethod]
    public void ValidateThresholds_RejectsNegativeThresholds()
    {
        var errors = StockStatusCalculator.ValidateThresholds(-1, 0);

        Assert.AreEqual(2, errors.Count);
    }

    // Kiểm tra không chấp nhận cấu hình cả ngưỡng đặt và tồn tối đa bằng 0.
    [TestMethod]
    public void Calculate_RejectsZeroMaximumWhenReorderPointIsZero()
    {
        Assert.ThrowsException<ArgumentException>(() => StockStatusCalculator.Calculate(5, 0, 0));
    }

    // Kiểm tra lượng tồn dương nhỏ thuộc trạng thái sắp hết khi ngưỡng đặt hàng dương.
    [TestMethod]
    public void Calculate_ReturnsLowStockImmediatelyAboveZeroWhenReorderPointIsPositive()
    {
        var result = StockStatusCalculator.Calculate(1, 10, 100);

        Assert.AreEqual(StockStatus.LowStock, result);
    }

    // Kiểm tra bộ kiểm tra sản phẩm chấp nhận dữ liệu hợp lệ.
    [TestMethod]
    public void ProductValidator_AcceptsValidProduct()
    {
        var isValid = new ProductValidator().IsValid(
            "P001", "Sản phẩm", "cái", 10m, 15m, 10, 100);

        Assert.IsTrue(isValid);
    }

    // Kiểm tra các trường bắt buộc và giá trị sản phẩm sai được báo lỗi.
    [TestMethod]
    public void ProductValidator_ReportsRequiredFieldsAndInvalidValues()
    {
        var errors = new ProductValidator().Validate(
            " ", null, "", -1m, -2m, 100, 100);

        Assert.AreEqual(6, errors.Count);
        CollectionAssert.Contains(errors.ToList(), "Mã sản phẩm là bắt buộc và không quá 50 ký tự.");
        CollectionAssert.Contains(errors.ToList(), "Giá nhập không được âm.");
    }

    // Kiểm tra sản phẩm từ chối hai ngưỡng tồn cùng bằng 0.
    [TestMethod]
    public void ProductValidator_RejectsEqualZeroThresholds()
    {
        var errors = new ProductValidator().Validate(
            "P001", "Sản phẩm", "cái", 1m, 2m, 0, 0);

        Assert.AreEqual(2, errors.Count);
    }

    // Kiểm tra ngưỡng đặt hàng bằng 0 bị từ chối dù mức tồn tối đa dương.
    [TestMethod]
    public void ProductValidator_RejectsZeroReorderPointWithPositiveMaximumStock()
    {
        var errors = new ProductValidator().Validate(
            "P001", "Sản phẩm", "cái", 1m, 2m, 0, 10);

        CollectionAssert.Contains(errors.ToList(), "Điểm đặt hàng lại phải lớn hơn 0.");
        Assert.IsFalse(new ProductValidator().IsValid(
            "P001", "Sản phẩm", "cái", 1m, 2m, 0, 10));
    }

    // Kiểm tra dữ liệu sản phẩm vượt giới hạn độ dài cơ sở dữ liệu bị từ chối.
    [TestMethod]
    public void ProductValidator_RejectsFieldsLongerThanDatabaseLimits()
    {
        var errors = new ProductValidator().Validate(
            new string('P', 51), new string('N', 201), new string('U', 51), 1m, 2m, 1, 10);

        Assert.AreEqual(3, errors.Count);
    }
}
