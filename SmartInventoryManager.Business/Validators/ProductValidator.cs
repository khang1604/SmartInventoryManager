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

            if (string.IsNullOrWhiteSpace(productCode))
            {
                errors.Add("Mã sản phẩm không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(productName))
            {
                errors.Add("Tên sản phẩm không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(unit))
            {
                errors.Add("Đơn vị tính không được để trống.");
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
