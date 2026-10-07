namespace SmartInventoryManager.Business.Validators
{
    public class ImportValidator
    {
        public IReadOnlyList<string> ValidateDetail(int productId, int quantity, decimal unitPrice)
        {
            var errors = new List<string>();

            if (productId <= 0)
            {
                errors.Add("Sản phẩm không hợp lệ.");
            }

            if (quantity <= 0)
            {
                errors.Add("Số lượng nhập phải lớn hơn 0.");
            }

            if (unitPrice < 0)
            {
                errors.Add("Đơn giá nhập không được âm.");
            }

            return errors.AsReadOnly();
        }

        public bool IsValidDetail(int productId, int quantity, decimal unitPrice)
        {
            return ValidateDetail(productId, quantity, unitPrice).Count == 0;
        }
    }
}
