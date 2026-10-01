#nullable enable

namespace SmartInventoryManager.Controls
{
    partial class ChartControl
    {
        private System.ComponentModel.IContainer? components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            AutoScaleMode = AutoScaleMode.Font;
            Name = nameof(ChartControl);
            Size = new System.Drawing.Size(200, 100);
        }
    }
}

