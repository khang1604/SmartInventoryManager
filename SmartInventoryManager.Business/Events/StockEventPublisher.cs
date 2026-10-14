using System.Diagnostics;

namespace SmartInventoryManager.Business.Events;

public sealed class StockEventPublisher
{
    public event EventHandler<InventoryChangedEvent>? InventoryChanged;
    public event EventHandler<StockAlertEvent>? AlertChanged;
    public void Publish(InventoryChangedEvent value) => Dispatch(InventoryChanged, value);
    public void Publish(StockAlertEvent value) => Dispatch(AlertChanged, value);
    private void Dispatch<T>(EventHandler<T>? handlers, T value) where T : EventArgs
    {
        if (handlers is null) return;
        foreach (EventHandler<T> handler in handlers.GetInvocationList())
            try { handler(this, value); }
            catch (Exception ex) { Debug.WriteLine($"Post-commit subscriber failed: {ex.GetType().Name}"); }
    }
}
