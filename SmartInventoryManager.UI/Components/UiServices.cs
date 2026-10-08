using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.Interfaces;
using SmartInventoryManager.Business.Services;

namespace SmartInventoryManager.UI.Components;

// Các màn hình dùng chung context do Program quản lý: mỗi lần chỉ chạy một thao tác.
public sealed class UiServices : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Action _resetTracking;
    private int _pending;
    public bool HasPendingOperations => Volatile.Read(ref _pending) > 0;
    public UserSession Session { get; }
    public AuthorizationService Authorization { get; }
    public ImportService Imports { get; }
    public ExportService Exports { get; }
    public InventoryService Inventory { get; }
    public StockAlertService Alerts { get; }
    public ProductService Products { get; }
    public IProductService ProductManagement { get; }
    public WarehouseService Warehouses { get; }
    public LocationService Locations { get; }
    public SupplierService Suppliers { get; }
    public CategoryService Categories { get; }
    public DashboardService Dashboard { get; }
    public ReportService Reports { get; }
    public UserService Users { get; }
    public NotificationService Notifications { get; }

    public UiServices(UserSession session, AuthorizationService authorization,
        ImportService imports, ExportService exports, InventoryService inventory,
        StockAlertService alerts, ProductService products, WarehouseService warehouses,
        SupplierService suppliers, CategoryService categories, IProductService productManagement, LocationService locations,
        DashboardService dashboard, ReportService reports, UserService users, NotificationService notifications, Action resetTracking)
    {
        Session = session;
        Authorization = authorization;
        Imports = imports;
        Exports = exports;
        Inventory = inventory;
        Alerts = alerts;
        Products = products;
        ProductManagement = productManagement;
        ProductManagement = productManagement;
        Warehouses = warehouses;
        Suppliers = suppliers;
        Locations = locations;
        Categories = categories;
        Dashboard = dashboard;
        Reports = reports;
        Users = users;
        Notifications = notifications;
        _resetTracking = resetTracking;
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> operation)
    {
        Interlocked.Increment(ref _pending);
        try
        {
            await _gate.WaitAsync();
            try
            {
                if (!Session.IsAuthenticated)
                    throw new UnauthorizedAccessException("Vui lòng đăng nhập lại.");
                return await operation();
            }
            finally
            {
                // Không giữ entity đã thay đổi sau lỗi để tránh lưu nhầm ở thao tác kế tiếp.
                try { _resetTracking(); }
                finally { _gate.Release(); }
            }
        }
        finally
        {
            Interlocked.Decrement(ref _pending);
        }
    }

    public Task RunAsync(Func<Task> operation) => RunAsync(async () =>
    {
        await operation();
        return true;
    });

    public void Dispose() => _gate.Dispose();
}
