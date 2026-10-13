using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AntdUI;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.UI.Components;

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
        private TableLayoutPanel _loginCard = null!;
        private TableLayoutPanel _loginOptions = null!;
        private TableLayoutPanel _loginBrand = null!;
        private TableLayoutPanel _loginBrandContainer = null!;
        private TableLayoutPanel _loginPasswordRow = null!;
        private bool _arrangingLogin;
        private readonly int[] _loginRowHeights = [64, 28, 52, 24, 48, 24, 48, 40, 48, 72];

        public FrmLogin(AuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            InitializeComponent();
            SetupUI();
        }

        public AuthenticatedUserDto? AuthenticatedUser { get; private set; }

        private void BuildResponsiveLogin()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            pnlLeft.SuspendLayout();
            pnlLeft.Controls.Clear();
            pnlLeft.Dock = DockStyle.Fill;
            pnlLeft.AutoScroll = true;
            _loginCard = new TableLayoutPanel { Name = "loginCard", ColumnCount = 1, RowCount = 10,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize, Margin = Padding.Empty };
            // [COLOR CONFIG]: bố cục trong suốt giữ ảnh nền đăng nhập đã duyệt.
            _loginCard.BackColor = Color.Transparent;
            _loginCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (var height in _loginRowHeights) _loginCard.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            TableLayoutPanel Row(int columns)
            {
                var row = new TableLayoutPanel { ColumnCount = columns, RowCount = 1, Dock = DockStyle.Fill, Margin = Padding.Empty };
                // [COLOR CONFIG]: hàng trong suốt trên nền ảnh.
                row.BackColor = Color.Transparent;
                row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                return row;
            }
            void Place(Control control, int row)
            {
                control.Dock = DockStyle.Fill; control.Margin = Padding.Empty;
                _loginCard.Controls.Add(control, 0, row);
            }
            _loginBrand = Row(2);
            _loginBrand.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
            _loginBrand.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var logo = UiBrand.CreateLogo(); logo.Dock = DockStyle.Fill; logo.Margin = new Padding(4);
            label5.AutoSize = false; label5.Text = UiBrand.ApplicationName;
            label5.Font = new Font("Segoe UI", 28, FontStyle.Bold);
            label5.Dock = DockStyle.Fill; label5.Margin = Padding.Empty;
            label5.TextAlign = ContentAlignment.MiddleLeft;
            _loginBrand.Controls.Add(logo, 0, 0); _loginBrand.Controls.Add(label5, 1, 0);
            _loginBrandContainer = Row(3);
            _loginBrandContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _loginBrandContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
            _loginBrandContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _loginBrandContainer.Controls.Add(_loginBrand, 1, 0);
            Place(_loginBrandContainer, 0);
            label1.Text = UiBrand.ProjectName; label1.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            label1.TextAlign = ContentAlignment.MiddleCenter; Place(label1, 1);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter; Place(lblTitle, 2);
            label3.TextAlign = label4.TextAlign = ContentAlignment.MiddleLeft;
            Place(label3, 3); Place(txtUsername, 4); Place(label4, 5);
            txtUsername.PlaceholderText = PlaceholderUsername;
            txtPassword.PlaceholderText = PlaceholderPassword;
            _loginPasswordRow = Row(2);
            _loginPasswordRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _loginPasswordRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
            txtPassword.Dock = btnShowPassword.Dock = DockStyle.Fill;
            txtPassword.Margin = Padding.Empty; btnShowPassword.Margin = new Padding(4, 0, 0, 0);
            _loginPasswordRow.Controls.Add(txtPassword, 0, 0); _loginPasswordRow.Controls.Add(btnShowPassword, 1, 0);
            Place(_loginPasswordRow, 6);
            _loginOptions = Row(2); _loginOptions.RowCount = 2;
            _loginOptions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            _loginOptions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            _loginOptions.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
            chkRemember.Dock = lnkForgotPassword.Dock = DockStyle.Fill;
            chkRemember.Margin = lnkForgotPassword.Margin = Padding.Empty;
            _loginOptions.Controls.Add(chkRemember, 0, 0); _loginOptions.Controls.Add(lnkForgotPassword, 1, 0);
            Place(_loginOptions, 7); Place(btnLogin, 8); Place(label2, 9);
            label2.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            pnlLeft.Controls.Add(_loginCard); pnlLeft.Controls.Add(label7);
            Text = $"{UiBrand.ApplicationName} - Đăng nhập";
            pnlLeft.ResumeLayout();
            pnlLeft.SizeChanged += (_, _) => ArrangeResponsiveLogin();
            DpiChanged += (_, _) => { FitLoginWindow(); ArrangeResponsiveLogin(); };
            Shown += (_, _) => { FitLoginWindow(); ArrangeResponsiveLogin(); txtUsername.Focus(); };
            ArrangeResponsiveLogin();
        }

        private void FitLoginWindow()
        {
            if (WindowState != FormWindowState.Normal) return;
            var area = Screen.FromControl(this).WorkingArea;
            float scale = DeviceDpi / 96F;
            int margin = Math.Min(24, Math.Max(8, area.Width / 50));
            MinimumSize = new Size(Math.Min((int)(480 * scale), area.Width - margin * 2),
                Math.Min((int)(320 * scale), area.Height - margin * 2));
            var size = new Size(Math.Min(Width, area.Width - margin * 2), Math.Min(Height, area.Height - margin * 2));
            Size = size;
            Location = new Point(Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right - Width)),
                Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom - Height)));
        }

        private void ArrangeResponsiveLogin()
        {
            if (_arrangingLogin || _loginCard is null || pnlLeft.ClientSize.Width < 1) return;
            _arrangingLogin = true;
            try
            {
                float scale = DeviceDpi / 96F;
                int margin = (int)(16 * scale);
                int width = Math.Max(1, Math.Min((int)(520 * scale), pnlLeft.ClientSize.Width - margin * 2 - SystemInformation.VerticalScrollBarWidth));
                bool compact = width / scale < 400;
                _loginCard.SuspendLayout();
                _loginCard.Padding = new Padding((int)(20 * scale));
                int total = _loginCard.Padding.Vertical;
                for (int i = 0; i < _loginRowHeights.Length; i++)
                {
                    int height = (int)((i == 7 && compact ? 64 : _loginRowHeights[i]) * scale);
                    _loginCard.RowStyles[i].Height = height; total += height;
                }
                _loginBrand.ColumnStyles[0].Width = 72 * scale;
                _loginBrandContainer.ColumnStyles[1].Width = Math.Min(width - _loginCard.Padding.Horizontal,
                    TextRenderer.MeasureText(label5.Text, label5.Font).Width + 80 * scale);
                _loginPasswordRow.ColumnStyles[1].Width = 44 * scale;
                _loginOptions.ColumnStyles[0].Width = compact ? 100 : 55;
                _loginOptions.ColumnStyles[1].SizeType = compact ? SizeType.Absolute : SizeType.Percent;
                _loginOptions.ColumnStyles[1].Width = compact ? 0 : 45;
                _loginOptions.RowStyles[0].SizeType = SizeType.Percent;
                _loginOptions.RowStyles[0].Height = compact ? 50 : 100;
                _loginOptions.RowStyles[1].SizeType = compact ? SizeType.Percent : SizeType.Absolute;
                _loginOptions.RowStyles[1].Height = compact ? 50 : 0;
                _loginOptions.SetCellPosition(lnkForgotPassword, compact ? new TableLayoutPanelCellPosition(0, 1) : new TableLayoutPanelCellPosition(1, 0));
                lnkForgotPassword.TextAlign = compact ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleRight;
                _loginCard.Size = new Size(width, total);
                int footerHeight = (int)(24 * scale);
                pnlLeft.AutoScrollMinSize = new Size(0, total + margin * 2 + footerHeight);
                _loginCard.Location = new Point(Math.Max(margin, (pnlLeft.ClientSize.Width - width) / 2),
                    Math.Max(margin, (pnlLeft.ClientSize.Height - total - footerHeight) / 2));
                label7.Size = new Size((int)(180 * scale), footerHeight);
                label7.Location = new Point(Math.Max(margin, pnlLeft.ClientSize.Width - label7.Width - margin),
                    Math.Max(total + margin, pnlLeft.ClientSize.Height - footerHeight));
                _loginCard.ResumeLayout(true);
                int eyeSize = (int)(20 * scale);
                if (btnShowPassword.Image?.Width != eyeSize)
                {
                    var oldEye = btnShowPassword.Image;
                    // [COLOR CONFIG]: icon mắt dùng màu chữ hiện có để rõ trên nền nút.
                    btnShowPassword.Image = AntdUI.SvgExtend.SvgToBmp(MainIcons.Get("eye"), eyeSize, eyeSize, btnShowPassword.ForeColor);
                    if (!ReferenceEquals(oldEye, btnShowPassword.Image)) oldEye?.Dispose();
                }
                btnShowPassword.Text = string.Empty;
            }
            finally { _arrangingLogin = false; }
        }
        private void SetupUI()
        {
            UiBrand.ApplyIcon(this);
            BuildResponsiveLogin();
            using (var backgroundStream = typeof(FrmLogin).Assembly.GetManifestResourceStream("SmartInventoryManager.UI.LoginBackground")
                ?? throw new InvalidOperationException("Missing login background resource"))
            using (var originalBackground = Image.FromStream(backgroundStream))
            {
                pnlLeft.BackgroundImage = new Bitmap(originalBackground);
                pnlLeft.BackgroundImageLayout = ImageLayout.Zoom;
            }
            Disposed += (_, _) => pnlLeft.BackgroundImage?.Dispose();

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
            btnShowPassword.TabStop = true;
            txtUsername.TabIndex = 0; txtPassword.TabIndex = 1;
            btnShowPassword.TabIndex = 2; chkRemember.TabIndex = 3;
            lnkForgotPassword.TabIndex = 4; btnLogin.TabIndex = 5;
            Disposed += (_, _) => btnShowPassword.Image?.Dispose();
            txtUsername.AccessibleName = "Tên đăng nhập";
            txtPassword.AccessibleName = "Mật khẩu";
            label2.AccessibleName = "Thông báo đăng nhập";
            txtUsername.TextChanged += (_, _) => label2.Text = string.Empty;
            txtPassword.TextChanged += (_, _) => label2.Text = string.Empty;
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
            label2.Text = string.Empty;

            var validationErrors = _authService.ValidateLoginCredentials(username, password);
            if (validationErrors.Count > 0)
            {
                label2.Text = validationErrors[0];
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
                    label2.Text = "Tên đăng nhập hoặc mật khẩu không đúng,\nhoặc tài khoản đã bị khóa.";
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
                label2.Text = "Không thể kết nối hoặc xác thực.\nVui lòng thử lại sau.";
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
                btnShowPassword.AccessibleName = "Ẩn mật khẩu";
            }
            else
            {
                // Hide password (restore mask)
                txtPassword.UseSystemPasswordChar = false;
                txtPassword.PasswordChar = PasswordMaskChar;
                btnShowPassword.AccessibleName = "Hiện mật khẩu";
            }
        }

    }
}
