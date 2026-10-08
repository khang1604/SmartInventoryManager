using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Tests;

[TestClass]
public sealed class MasterDataDtoTests
{
    [TestMethod]
    public void CategoryRequest_DefaultsToActive()
    {
        var request = new CategoryRequestDto { CategoryName = "Đồ uống" };

        Assert.IsTrue(request.IsActive);
        Assert.AreEqual("Đồ uống", request.CategoryName);
    }

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

    [TestMethod]
    public void LocationRequest_RequiresWarehouseShape()
    {
        var request = new LocationRequestDto { WarehouseId = 7, LocationCode = "A-01" };

        Assert.AreEqual(7, request.WarehouseId);
        Assert.AreEqual("A-01", request.LocationCode);
        Assert.IsTrue(request.IsActive);
    }
}
