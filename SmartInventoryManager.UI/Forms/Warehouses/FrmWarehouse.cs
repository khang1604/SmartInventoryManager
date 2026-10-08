namespace SmartInventoryManager.Forms.Warehouses
{
    /// <summary>Skeleton form for the FrmWarehouse screen.</summary>
    public partial class FrmWarehouse : Form
    {
        public FrmWarehouse() : this(false) { }

        public FrmWarehouse(bool canManageWarehouses)
        {
            InitializeComponent();
            WarehouseUi.BuildList(this, false, canManageWarehouses);
        }
    }
}

