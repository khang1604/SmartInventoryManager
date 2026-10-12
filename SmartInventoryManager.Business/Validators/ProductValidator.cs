namespace SmartInventoryManager.Business.Validators
{
    public class ProductValidator
    {
        public IReadOnlyList<string> Validate(
            string? productCode,
            string? productName,
            string? unit,
            decimal purchasePrice,
            decimal sellingPrice,
            int reorderPoint,
            int maximumStock)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(productCode) || productCode.Trim().Length > 50)
            {
                errors.Add("Mã sản phẩm là bắt buộc và không quá 50 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(productName) || productName.Trim().Length > 200)
            {
                errors.Add("Tên sản phẩm là bắt buộc và không quá 200 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(unit) || unit.Trim().Length > 50)
            {
                errors.Add("Đơn vị tính là bắt buộc và không quá 50 ký tự.");
            }

            if (purchasePrice < 0)
            {
                errors.Add("Giá nhập không được âm.");
            }

            if (sellingPrice < 0)
            {
                errors.Add("Giá bán không được âm.");
            }

            errors.AddRange(SmartInventoryManager.Business.Inventory.StockStatusCalculator.ValidateThresholds(
                reorderPoint,
                maximumStock));

            return errors.AsReadOnly();
        }

        public bool IsValid(
            string? productCode,
            string? productName,
            string? unit,
            decimal purchasePrice,
            decimal sellingPrice,
            int reorderPoint,
            int maximumStock)
        {
            return Validate(
                productCode,
                productName,
                unit,
                purchasePrice,
                sellingPrice,
                reorderPoint,
                maximumStock).Count == 0;
        }
    }
}
