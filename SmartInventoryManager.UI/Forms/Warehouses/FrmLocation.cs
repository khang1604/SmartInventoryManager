using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Warehouses
{
    /// <summary>Skeleton form for the FrmLocation screen.</summary>
    public partial class FrmLocation : Form
    {
        // Khởi tạo màn hình danh sách vị trí trong kho.
        public FrmLocation(UiServices services)
        {
            InitializeComponent();
            WarehouseUi.BuildList(this, true, services.Authorization.CanManageWarehouse(services.Session), services);
        }

        // Khởi tạo màn hình danh sách vị trí trong kho.
        public FrmLocation()
        {
            InitializeComponent();
            WarehouseUi.BuildList(this, true, false);
        }
    }
}

