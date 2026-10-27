namespace SmartInventoryManager.Business.Validators
{
    public class ExportValidator
    {
        // Kiểm tra sản phẩm, số lượng và đơn giá của một dòng phiếu.
        public IReadOnlyList<string> ValidateDetail(int productId, int quantity, decimal unitPrice)
        {
            var errors = new List<string>();

            if (productId <= 0)
            {
                errors.Add("Sản phẩm không hợp lệ.");
            }

            if (quantity <= 0)
            {
                errors.Add("Số lượng xuất phải lớn hơn 0.");
            }

            if (unitPrice < 0)
            {
                errors.Add("Đơn giá xuất không được âm.");
            }

            return errors.AsReadOnly();
        }

        // Cho biết dữ liệu của một dòng phiếu có hợp lệ hay không.
        public bool IsValidDetail(int productId, int quantity, decimal unitPrice)
        {
            return ValidateDetail(productId, quantity, unitPrice).Count == 0;
        }
    }
}
