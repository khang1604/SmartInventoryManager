using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Warehouses
{
    /// <summary>Skeleton form for the FrmWarehouse screen.</summary>
    public partial class FrmWarehouse : Form
    {
        // Khởi tạo màn hình danh sách kho.
        public FrmWarehouse(UiServices services)
        {
            InitializeComponent();
            WorkflowUi.StyleForm(this);
            WarehouseUi.BuildList(this, false, services.Authorization.CanManageWarehouse(services.Session), services);
        }
        // Khởi tạo màn hình danh sách kho.
        public FrmWarehouse() : this(false) { }

        // Khởi tạo màn hình danh sách kho.
        public FrmWarehouse(bool canManageWarehouses)
        {
            InitializeComponent();
            WarehouseUi.BuildList(this, false, canManageWarehouses);
        }
    }
}
