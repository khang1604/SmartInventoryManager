using SmartInventoryManager.UI.Components;
namespace SmartInventoryManager.Forms.Exports;
public partial class FrmExportDetail : Form
{
    public FrmExportDetail()
    {
        InitializeComponent();
        ReceiptUi.Build(this, false);
    }
}
