using System.Diagnostics;

namespace SmartInventoryManager.Business.Events;

public sealed class StockEventPublisher
{
    public event EventHandler<InventoryChangedEvent>? InventoryChanged;
    public event EventHandler<StockAlertEvent>? AlertChanged;
    // Phát sự kiện thay đổi tồn kho cho các bên đã đăng ký.
    public void Publish(InventoryChangedEvent value) => Dispatch(InventoryChanged, value);
    // Phát sự kiện cảnh báo tồn kho cho các bên đã đăng ký.
    public void Publish(StockAlertEvent value) => Dispatch(AlertChanged, value);
    // Gọi từng bên nhận sự kiện và cô lập lỗi của từng bên nhận.
    private void Dispatch<T>(EventHandler<T>? handlers, T value) where T : EventArgs
    {
        if (handlers is null) return;
        foreach (EventHandler<T> handler in handlers.GetInvocationList())
            try { handler(this, value); }
            catch (Exception ex) { Debug.WriteLine($"Post-commit subscriber failed: {ex.GetType().Name}"); }
    }
}
