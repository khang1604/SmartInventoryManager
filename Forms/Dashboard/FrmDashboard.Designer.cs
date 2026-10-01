#nullable enable

namespace SmartInventoryManager.Forms.Dashboard
{
    partial class FrmDashboard
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
            Name = nameof(FrmDashboard);
            Text = nameof(FrmDashboard);
            ResumeLayout(false);
        }
    }
}

