#nullable enable

namespace SmartInventoryManager.Forms.Reports
{
    partial class FrmInventoryReport
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
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 450);
            Name = nameof(FrmInventoryReport);
            Text = nameof(FrmInventoryReport);
            ResumeLayout(false);
        }
    }
}

