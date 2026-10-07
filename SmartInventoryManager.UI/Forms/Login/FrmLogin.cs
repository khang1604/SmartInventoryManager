using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AntdUI;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Services;

namespace SmartInventoryManager.UI.Forms.Login
{
    public partial class FrmLogin : Form
    {
        private const char PasswordMaskChar = '●';
        private readonly AuthService _authService;
        private bool _isSigningIn;

        // [COLOR CONFIG]: Màu hover button - accent xanh pastel, #AEE2FF
        private readonly Color _btnLoginHoverColor = Color.FromArgb(174, 226, 255);
        // [COLOR CONFIG]: Màu gốc button - secondary, #B5BAFF
        private readonly Color _btnLoginOriginalColor = Color.FromArgb(181, 186, 255);

        private const string PlaceholderUsername = "Tên đăng nhập";
        private const string PlaceholderPassword = "Mật khẩu";
        private readonly PictureBox _logoPictureBox = new();

        public FrmLogin(AuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            InitializeComponent();
            SetupUI();
        }

        public AuthenticatedUserDto? AuthenticatedUser { get; private set; }

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

        private async void TxtPassword_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await PerformLoginAsync();
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

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            await PerformLoginAsync();
        }

        private async Task PerformLoginAsync()
        {
            if (_isSigningIn)
            {
                return;
            }

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            var validationErrors = _authService.ValidateLoginCredentials(username, password);
            if (validationErrors.Count > 0)
            {
                MessageBox.Show(
                    validationErrors[0],
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                (string.IsNullOrWhiteSpace(username) ? txtUsername : txtPassword).Focus();
                return;
            }

            _isSigningIn = true;
            btnLogin.Enabled = false;

            try
            {
                AuthenticatedUser = await _authService.AuthenticateAsync(username, password);
                if (AuthenticatedUser is null)
                {
                    MessageBox.Show(
                        "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.",
                        "Đăng nhập thất bại",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    txtPassword.SelectAll();
                    txtPassword.Focus();
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
                MessageBox.Show(
                    "Không thể kết nối hoặc xác thực với cơ sở dữ liệu. Vui lòng thử lại sau.",
                    "Lỗi đăng nhập",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isSigningIn = false;
                btnLogin.Enabled = true;
            }
        }

        private void btnShowPassword_Click(object sender, EventArgs e)
        {
            // Explicit toggle mapping:
            // If currently masked (PasswordChar != '\0') -> clear PasswordChar to show plaintext.
            // If currently visible (PasswordChar == '\0') -> restore PasswordMaskChar to hide.
            var isCurrentlyMasked = txtPassword.PasswordChar != '\0';

            if (isCurrentlyMasked)
            {
                // Show password (clear mask)
                txtPassword.UseSystemPasswordChar = false;
                txtPassword.PasswordChar = '\0';
                btnShowPassword.Text = "👁";
                btnShowPassword.AccessibleName = "Ẩn mật khẩu";
            }
            else
            {
                // Hide password (restore mask)
                txtPassword.UseSystemPasswordChar = false;
                txtPassword.PasswordChar = PasswordMaskChar;
                btnShowPassword.Text = "🙈";
                btnShowPassword.AccessibleName = "Hiện mật khẩu";
            }
        }

    }
}