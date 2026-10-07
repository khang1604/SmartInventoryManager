using AntdUI;
namespace SmartInventoryManager.UI.Forms.Login
{
    partial class FrmLogin
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmLogin));
            label5 = new System.Windows.Forms.Label();
            label2 = new AntdUI.Label();
            btnShowPassword = new System.Windows.Forms.Button();
            btnLogin = new AntdUI.Button();
            label4 = new AntdUI.Label();
            label3 = new AntdUI.Label();
            lnkForgotPassword = new AntdUI.Label();
            chkRemember = new Checkbox();
            txtPassword = new Input();
            txtUsername = new Input();
            lblTitle = new AntdUI.Label();
            label7 = new AntdUI.Label();
            pnlLeft = new System.Windows.Forms.Panel();
            label1 = new AntdUI.Label();
            pnlLeft.SuspendLayout();
            SuspendLayout();
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.BackColor = Color.Transparent;
            label5.Font = new Font("Segoe UI", 42F, FontStyle.Bold);
            label5.ForeColor = Color.FromArgb(70, 70, 150);
            label5.Location = new Point(311, 34);
            label5.Name = "label5";
            label5.Size = new Size(255, 74);
            label5.TabIndex = 1;
            label5.Text = "VioWare";
            // 
            // label2
            // 
            label2.BackColor = Color.Transparent;
            label2.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            label2.ForeColor = Color.FromArgb(100, 100, 255);
            label2.Location = new Point(370, 483);
            label2.Name = "label2";
            label2.Size = new Size(411, 32);
            label2.TabIndex = 0;
            label2.Text = "Quan sát rõ - Phân tích thông minh - Tối ưu tồn kho";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnShowPassword
            // 
            btnShowPassword.BackColor = Color.White;
            btnShowPassword.Cursor = Cursors.Hand;
            btnShowPassword.FlatAppearance.BorderSize = 0;
            btnShowPassword.FlatStyle = FlatStyle.Flat;
            btnShowPassword.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnShowPassword.ForeColor = SystemColors.ActiveCaptionText;
            btnShowPassword.Location = new Point(755, 351);
            btnShowPassword.Name = "btnShowPassword";
            btnShowPassword.Size = new Size(26, 22);
            btnShowPassword.TabIndex = 7;
            btnShowPassword.TabStop = false;
            btnShowPassword.Text = "👁";
            btnShowPassword.UseVisualStyleBackColor = false;
            btnShowPassword.Click += btnShowPassword_Click;
            // 
            // btnLogin
            // 
            btnLogin.BackColor = Color.FromArgb(181, 186, 255);
            btnLogin.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnLogin.ForeColor = Color.White;
            btnLogin.Location = new Point(416, 422);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(327, 46);
            btnLogin.TabIndex = 6;
            btnLogin.Text = "→ Đăng nhập";
            btnLogin.Type = TTypeMini.Primary;
            btnLogin.Click += btnLogin_Click;
            // 
            // label4
            // 
            label4.BackColor = Color.Transparent;
            label4.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label4.ForeColor = Color.FromArgb(93, 91, 190);
            label4.Location = new Point(355, 320);
            label4.Name = "label4";
            label4.Size = new Size(68, 20);
            label4.TabIndex = 5;
            label4.Text = "Mật khẩu:";
            label4.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            label3.BackColor = Color.Transparent;
            label3.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label3.ForeColor = Color.FromArgb(93, 91, 190);
            label3.Location = new Point(355, 244);
            label3.Name = "label3";
            label3.Size = new Size(69, 20);
            label3.TabIndex = 5;
            label3.Text = "Tài khoản:";
            label3.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lnkForgotPassword
            // 
            lnkForgotPassword.BackColor = Color.Transparent;
            lnkForgotPassword.Cursor = Cursors.Hand;
            lnkForgotPassword.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lnkForgotPassword.ForeColor = Color.FromArgb(93, 91, 190);
            lnkForgotPassword.Location = new Point(678, 396);
            lnkForgotPassword.Name = "lnkForgotPassword";
            lnkForgotPassword.Size = new Size(123, 20);
            lnkForgotPassword.TabIndex = 5;
            lnkForgotPassword.Text = "Quên mật khẩu?";
            lnkForgotPassword.TextAlign = ContentAlignment.MiddleRight;
            // 
            // chkRemember
            // 
            chkRemember.BackColor = Color.Transparent;
            chkRemember.Checked = true;
            chkRemember.CheckState = CheckState.Checked;
            chkRemember.Fill = Color.FromArgb(122, 113, 232);
            chkRemember.Font = new Font("Segoe UI", 10F);
            chkRemember.ForeColor = Color.FromArgb(45, 49, 66);
            chkRemember.Location = new Point(355, 396);
            chkRemember.Name = "chkRemember";
            chkRemember.Size = new Size(129, 20);
            chkRemember.TabIndex = 4;
            chkRemember.Text = "Nhớ tài khoản";
            // 
            // txtPassword
            // 
            txtPassword.BorderActive = Color.FromArgb(122, 113, 232);
            txtPassword.BorderColor = Color.FromArgb(220, 220, 235);
            txtPassword.BorderHover = Color.FromArgb(174, 178, 245);
            txtPassword.Font = new Font("Segoe UI", 11F);
            txtPassword.Location = new Point(343, 342);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '●';
            txtPassword.PrefixFore = Color.FromArgb(93, 91, 190);
            txtPassword.PrefixSvg = resources.GetString("txtPassword.PrefixSvg");
            txtPassword.PrefixWidth = 28;
            txtPassword.Radius = 10;
            txtPassword.Size = new Size(458, 40);
            txtPassword.TabIndex = 3;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // txtUsername
            // 
            txtUsername.BorderActive = Color.FromArgb(122, 113, 232);
            txtUsername.BorderColor = Color.FromArgb(220, 220, 235);
            txtUsername.BorderHover = Color.FromArgb(174, 178, 245);
            txtUsername.Font = new Font("Segoe UI", 11F);
            txtUsername.Location = new Point(343, 266);
            txtUsername.Name = "txtUsername";
            txtUsername.PrefixFore = Color.FromArgb(93, 91, 190);
            txtUsername.PrefixSvg = resources.GetString("txtUsername.PrefixSvg");
            txtUsername.PrefixWidth = 28;
            txtUsername.Radius = 10;
            txtUsername.Size = new Size(458, 40);
            txtUsername.TabIndex = 2;
            // 
            // lblTitle
            // 
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(120, 90, 255);
            lblTitle.Location = new Point(507, 190);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(130, 48);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Đăng nhập";
            // 
            // label7
            // 
            label7.BackColor = Color.Transparent;
            label7.Font = new Font("Segoe UI", 8F);
            label7.ForeColor = Color.FromArgb(139, 143, 163);
            label7.Location = new Point(1016, 618);
            label7.Name = "label7";
            label7.Size = new Size(102, 19);
            label7.TabIndex = 0;
            label7.Text = "Develop by VioWare";
            label7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pnlLeft
            // 
            pnlLeft.BackColor = Color.FromArgb(235, 237, 255);
            pnlLeft.Controls.Add(btnShowPassword);
            pnlLeft.Controls.Add(label5);
            pnlLeft.Controls.Add(btnLogin);
            pnlLeft.Controls.Add(txtUsername);
            pnlLeft.Controls.Add(label7);
            pnlLeft.Controls.Add(label1);
            pnlLeft.Controls.Add(lblTitle);
            pnlLeft.Controls.Add(label2);
            pnlLeft.Controls.Add(txtPassword);
            pnlLeft.Controls.Add(label4);
            pnlLeft.Controls.Add(chkRemember);
            pnlLeft.Controls.Add(label3);
            pnlLeft.Controls.Add(lnkForgotPassword);
            pnlLeft.Location = new Point(0, 0);
            pnlLeft.Name = "pnlLeft";
            pnlLeft.Size = new Size(1120, 640);
            pnlLeft.TabIndex = 0;
            // 
            // label1
            // 
            label1.BackColor = Color.Transparent;
            label1.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            label1.ForeColor = Color.FromArgb(70, 70, 255);
            label1.Location = new Point(328, 101);
            label1.Name = "label1";
            label1.Size = new Size(312, 34);
            label1.TabIndex = 0;
            label1.Text = "Smart Inventory Manager";
            // 
            // FrmLogin
            // 
            AcceptButton = btnLogin;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1120, 640);
            Controls.Add(pnlLeft);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmLogin";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "VioWare - Đăng nhập";
            pnlLeft.ResumeLayout(false);
            pnlLeft.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private AntdUI.Button btnLogin;
        private AntdUI.Label lnkForgotPassword;
        private AntdUI.Checkbox chkRemember;
        private AntdUI.Input txtPassword;
        private AntdUI.Input txtUsername;
        private AntdUI.Label lblTitle;
        private AntdUI.Label label2;
        private AntdUI.Label label4;
        private AntdUI.Label label3;
        private System.Windows.Forms.Button btnShowPassword;
        private System.Windows.Forms.Label label5;
        private AntdUI.Label label7;
        private System.Windows.Forms.Panel pnlLeft;
        private AntdUI.Label label1;
    }
}