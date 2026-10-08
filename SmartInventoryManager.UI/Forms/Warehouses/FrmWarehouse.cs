using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Warehouses
{
    /// <summary>Skeleton form for the FrmWarehouse screen.</summary>
    public partial class FrmWarehouse : Form
    {
        public FrmWarehouse(UiServices services)
        {
            InitializeComponent();
            WorkflowUi.StyleForm(this);
            WarehouseUi.BuildList(this, false, services.Authorization.CanManageWarehouse(services.Session), services);
        }
        public FrmWarehouse() : this(false) { }

        public FrmWarehouse(bool canManageWarehouses)
        {
            InitializeComponent();
            WarehouseUi.BuildList(this, false, canManageWarehouses);
        }
    }
}
