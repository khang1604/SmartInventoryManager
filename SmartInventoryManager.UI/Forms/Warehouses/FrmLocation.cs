using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Warehouses
{
    /// <summary>Skeleton form for the FrmLocation screen.</summary>
    public partial class FrmLocation : Form
    {
        public FrmLocation(UiServices services)
        {
            InitializeComponent();
            WarehouseUi.BuildList(this, true, services.Authorization.CanManageWarehouse(services.Session), services);
        }

        public FrmLocation()
        {
            InitializeComponent();
            WarehouseUi.BuildList(this, true, false);
        }
    }
}

