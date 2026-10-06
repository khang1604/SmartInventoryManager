using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AntdUI;

namespace SmartInventoryManager.UI.Forms.Login
{
    public partial class FrmLogin : Form
    {
        // [COLOR CONFIG]: Màu hover button - accent xanh pastel, #AEE2FF
        private readonly Color _btnLoginHoverColor = Color.FromArgb(174, 226, 255);
        // [COLOR CONFIG]: Màu gốc button - secondary, #B5BAFF
        private readonly Color _btnLoginOriginalColor = Color.FromArgb(181, 186, 255);

        private const string PlaceholderUsername = "Tên đăng nhập";
        private const string PlaceholderPassword = "Mật khẩu";
        private readonly PictureBox _logoPictureBox = new();

        public FrmLogin()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            string backgroundPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Images", "background.png");
            if (File.Exists(backgroundPath))
            {
                pnlLeft.BackgroundImage = Image.FromFile(backgroundPath);
                pnlLeft.BackgroundImageLayout = ImageLayout.Stretch;
            }

            // Hiệu ứng hover cho button
            btnLogin.MouseEnter += BtnLogin_MouseEnter;
            btnLogin.MouseLeave += BtnLogin_MouseLeave;

            // Xử lý phím Enter
            txtUsername.KeyDown += TxtUsername_KeyDown;
            txtPassword.KeyDown += TxtPassword_KeyDown;

            // Xử lý link quên mật khẩu
            lnkForgotPassword.Click += LnkForgotPassword_Click;
            // Nút hiển thị mật khẩu
            btnShowPassword.FlatStyle = FlatStyle.Flat;
            btnShowPassword.FlatAppearance.BorderSize = 0;
            btnLogin.Cursor = Cursors.Hand;
            btnShowPassword.AccessibleName = "Hiện hoặc ẩn mật khẩu";
            txtUsername.AccessibleName = "Tên đăng nhập";
            txtPassword.AccessibleName = "Mật khẩu";
            // NOTE: Title bar tùy chỉnh cần tự implement nếu muốn giống hình mẫu 100%
            // FormBorderStyle = None + tự vẽ title bar với icon cube + text
        }

        private void BtnLogin_MouseEnter(object? sender, EventArgs e)
        {
            btnLogin.BackColor = _btnLoginHoverColor;
            btnLogin.Cursor = Cursors.Hand;
        }

        private void BtnLogin_MouseLeave(object? sender, EventArgs e)
        {
            btnLogin.BackColor = _btnLoginOriginalColor;
            btnLogin.Cursor = Cursors.Default;
        }

        private void TxtUsername_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                txtPassword.Focus();
            }
        }

        private void TxtPassword_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                PerformLogin();
            }
        }

        private void LnkForgotPassword_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "Vui lòng liên hệ quản trị viên để được hỗ trợ đặt lại mật khẩu.",
                "Quên mật khẩu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            PerformLogin();
        }

        private void PerformLogin()
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            // Validation UI cơ bản
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show(
                    "Vui lòng nhập tên đăng nhập.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show(
                    "Vui lòng nhập mật khẩu.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                txtPassword.Focus();
                return;
            }

            // TODO: Gọi AuthService.Login(username, password) khi Business Service sẵn sàng (phần việc của Khang)

            MessageBox.Show(
                $"Đang xử lý đăng nhập cho tài khoản: {username}\n\n" +
                "(Chức năng xác thực sẽ được tích hợp khi AuthService sẵn sàng)",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            // Sau khi đăng nhập thành công:
            // this.DialogResult = DialogResult.OK;
            // this.Close();
        }

        private void btnShowPassword_Click(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar =
                !txtPassword.UseSystemPasswordChar;
            btnShowPassword.Text = txtPassword.UseSystemPasswordChar ? "👁" : "🙈";
            btnShowPassword.AccessibleName = txtPassword.UseSystemPasswordChar
                ? "Hiện mật khẩu"
                : "Ẩn mật khẩu";
        }

    }
}