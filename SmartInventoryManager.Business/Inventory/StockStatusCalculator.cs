namespace SmartInventoryManager.Business.Inventory;

public static class StockStatusCalculator
{
    // Kiểm tra ngưỡng đặt hàng dương và nhỏ hơn mức tồn tối đa.
    public static IReadOnlyList<string> ValidateThresholds(int reorderPoint, int maximumStock)
    {
        var errors = new List<string>();

        if (reorderPoint <= 0)
        {
            errors.Add("Điểm đặt hàng lại phải lớn hơn 0.");
        }

        if (maximumStock <= 0)
        {
            errors.Add("Mức tồn tối đa phải lớn hơn 0.");
        }

        if (reorderPoint > 0 && maximumStock > 0 && reorderPoint >= maximumStock)
        {
            errors.Add("Mức tồn tối đa phải lớn hơn điểm đặt hàng lại.");
        }

        return errors.AsReadOnly();
    }

    // Cho biết cặp ngưỡng đặt hàng và tồn tối đa có hợp lệ hay không.
    public static bool HasValidThresholds(int reorderPoint, int maximumStock)
    {
        return ValidateThresholds(reorderPoint, maximumStock).Count == 0;
    }

    // Phân loại trạng thái tồn dựa trên số lượng hiện tại và các ngưỡng.
    public static StockStatus Calculate(int quantity, int reorderPoint, int maximumStock)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Số lượng tồn không được âm.");
        }

        var thresholdErrors = ValidateThresholds(reorderPoint, maximumStock);
        if (thresholdErrors.Count > 0)
        {
            throw new ArgumentException(
                string.Join(" ", thresholdErrors),
                nameof(reorderPoint));
        }

        if (quantity == 0)
        {
            return StockStatus.OutOfStock;
        }

        if (quantity <= reorderPoint)
        {
            return StockStatus.LowStock;
        }

        return quantity <= maximumStock
            ? StockStatus.Normal
            : StockStatus.Overstock;
    }
}
