using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Tests;

[TestClass]
public sealed class MasterDataDtoTests
{
    // Kiểm tra yêu cầu tạo danh mục mặc định ở trạng thái hoạt động.
    [TestMethod]
    public void CategoryRequest_DefaultsToActive()
    {
        var request = new CategoryRequestDto { CategoryName = "Đồ uống" };

        Assert.IsTrue(request.IsActive);
        Assert.AreEqual("Đồ uống", request.CategoryName);
    }

    // Kiểm tra DTO nhà cung cấp giữ nguyên các trường liên hệ không bắt buộc.
    [TestMethod]
    public void SupplierRequest_PreservesOptionalContactFields()
    {
        var request = new SupplierRequestDto
        {
            SupplierCode = "NCC-01",
            SupplierName = "Nhà cung cấp A",
            Phone = "0900000000",
            Email = "supplier@example.com"
        };

        Assert.AreEqual("0900000000", request.Phone);
        Assert.AreEqual("supplier@example.com", request.Email);
    }

    // Kiểm tra DTO vị trí có dữ liệu xác định kho chứa vị trí.
    [TestMethod]
    public void LocationRequest_RequiresWarehouseShape()
    {
        var request = new LocationRequestDto { WarehouseId = 7, LocationCode = "A-01" };

        Assert.AreEqual(7, request.WarehouseId);
        Assert.AreEqual("A-01", request.LocationCode);
        Assert.IsTrue(request.IsActive);
    }
}
