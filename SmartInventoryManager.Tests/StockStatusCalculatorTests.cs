using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Inventory;

namespace SmartInventoryManager.Tests;

[TestClass]
public class StockStatusCalculatorTests
{
    // Kiểm tra phân loại tồn tuân theo các điều kiện biên đã chốt.
    [DataTestMethod]
    [DataRow(0, 10, 100, StockStatus.OutOfStock)]
    [DataRow(10, 10, 100, StockStatus.LowStock)]
    [DataRow(11, 10, 100, StockStatus.Normal)]
    [DataRow(100, 10, 100, StockStatus.Normal)]
    [DataRow(101, 10, 100, StockStatus.Overstock)]
    [DataRow(0, 1, 2, StockStatus.OutOfStock)]
    [DataRow(1, 1, 2, StockStatus.LowStock)]
    [DataRow(2, 1, 2, StockStatus.Normal)]
    [DataRow(3, 1, 2, StockStatus.Overstock)]
    public void Calculate_UsesLockedBoundaries(int quantity, int reorderPoint, int maximumStock, StockStatus expected)
    {
        Assert.AreEqual(expected, StockStatusCalculator.Calculate(quantity, reorderPoint, maximumStock));
    }

    // Kiểm tra cấu hình ngưỡng tồn không hợp lệ bị từ chối.
    [DataTestMethod]
    [DataRow(-1, 10)]
    [DataRow(10, 9)]
    [DataRow(11, 10)]
    [DataRow(1, 0)]
    [DataRow(0, 0)]
    [DataRow(10, 10)]
    [DataRow(0, 10)]
    [DataRow(1, -1)]
    public void ValidateThresholds_RejectsInvalidConfiguration(int reorderPoint, int maximumStock)
    {
        Assert.IsFalse(StockStatusCalculator.HasValidThresholds(reorderPoint, maximumStock));
    }

    // Kiểm tra tính trạng thái tồn từ chối số lượng âm.
    [TestMethod]
    public void Calculate_RejectsNegativeQuantity()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => StockStatusCalculator.Calculate(-1, 10, 100));
    }

    // Kiểm tra tính trạng thái tồn từ chối cặp ngưỡng bằng nhau.
    [DataTestMethod]
    [DataRow(0, 0)]
    [DataRow(10, 10)]
    public void Calculate_RejectsEqualConfigurationThresholds(int reorderPoint, int maximumStock)
    {
        Assert.ThrowsException<ArgumentException>(() => StockStatusCalculator.Calculate(0, reorderPoint, maximumStock));
    }

    // Kiểm tra ngưỡng đặt hàng bằng 0 bị từ chối với mọi số lượng tồn.
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(10)]
    public void Calculate_RejectsZeroReorderPointRegardlessOfQuantity(int quantity)
    {
        Assert.ThrowsException<ArgumentException>(() => StockStatusCalculator.Calculate(quantity, 0, 10));
    }
}
