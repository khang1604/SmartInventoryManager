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
        private readonly PictureBox _logoPictureBox = new();


        // Khởi tạo màn hình đăng nhập và các sự kiện xác thực.
        public FrmLogin(AuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            InitializeComponent();
            SetupUI();
            var remembered = RememberedUsername.Load();
            chkRemember.Text = "Nhớ tên đăng nhập";
            chkRemember.Width = 200;
            chkRemember.AccessibleDescription = "Chỉ lưu tên đăng nhập trên máy, không lưu mật khẩu hoặc tự đăng nhập.";
            chkRemember.Checked = remembered is not null;
            if (remembered is not null) txtUsername.Text = remembered;
            chkRemember.CheckedChanged += (_, _) =>
            {
                if (!chkRemember.Checked && !RememberedUsername.TrySave(null))
                    label2.Text = "Không cập nhật được tên đăng nhập đã nhớ trên máy.";
            };
        }

        public AuthenticatedUserDto? AuthenticatedUser { get; private set; }

        // Sắp xếp các thành phần đăng nhập theo kích thước cửa sổ.
        private void ArrangeLoginLayout(PictureBox logo)
        {
            // Quy đổi kích thước bố cục theo tỷ lệ hiển thị của màn hình.
            int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96F);
            int center = pnlLeft.ClientSize.Width / 2;
            int textWidth = Math.Max(label5.PreferredSize.Width,
                TextRenderer.MeasureText(label1.Text, label1.Font).Width + 8);
            int brandLeft = center - (logo.Width + Scale(6) + textWidth) / 2;
            logo.Location = new Point(brandLeft, Scale(38));
            int textLeft = brandLeft + logo.Width + Scale(6);
            label5.Location = new Point(textLeft, Scale(34));
            label1.SetBounds(textLeft, Scale(108), textWidth, Scale(34));
            int fieldWidth = Math.Min(Scale(458), Math.Max(1, pnlLeft.ClientSize.Width - Scale(48)));
            int fieldLeft = center - fieldWidth / 2;
            lblTitle.SetBounds(fieldLeft, Scale(190), fieldWidth, Scale(48));
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            label3.SetBounds(fieldLeft + Scale(12), Scale(244), Scale(110), Scale(20));
            label4.SetBounds(fieldLeft + Scale(12), Scale(320), Scale(110), Scale(20));
            label3.TextAlign = label4.TextAlign = ContentAlignment.MiddleLeft;
            txtUsername.SetBounds(fieldLeft, Scale(268), fieldWidth, Scale(44));
            txtPassword.SetBounds(fieldLeft, Scale(344), fieldWidth, Scale(44));
            btnShowPassword.Location = new Point(fieldLeft + fieldWidth - Scale(44), Scale(355));
            chkRemember.Location = new Point(fieldLeft + Scale(12), Scale(402));
            lnkForgotPassword.SetBounds(fieldLeft + fieldWidth - Scale(135), Scale(402), Scale(123), Scale(20));
            btnLogin.SetBounds(fieldLeft, Scale(438), fieldWidth, Scale(46));
            label2.SetBounds(fieldLeft, Scale(494), fieldWidth, Scale(64));
            label7.Location = new Point(pnlLeft.ClientSize.Width - label7.Width - Scale(12),
                Math.Max(pnlLeft.ClientSize.Height, Scale(600)) - label7.Height - Scale(8));
        }

        // Thiết lập bố cục, màu sắc và hành vi của giao diện đăng nhập.
        private void SetupUI()
        {
            UiBrand.ApplyIcon(this);
            ScrollbarAppearance.Apply(this);
            pnlLeft.Dock = DockStyle.Fill;
            pnlLeft.AutoScroll = true;
            Shown += (_, _) =>
            {
                pnlLeft.AutoScrollMinSize = new Size(0, 600 * DeviceDpi / 96);
                WorkflowUi.FitToWorkingArea(this);
            };
            var projectLogo = UiBrand.CreateLogo();
            // Logo đặt cạnh tên app, cùng chiều cao vùng chữ VioWare.
            projectLogo.Location = new Point(244, 34);
            projectLogo.Size = new Size(74, 74);
            label5.Text = UiBrand.ApplicationName;
            label1.Text = UiBrand.ProjectName;
            Text = $"{UiBrand.ApplicationName} - Đăng nhập";
            pnlLeft.Controls.Add(projectLogo);
            projectLogo.BringToFront();
            ArrangeLoginLayout(projectLogo);
            pnlLeft.SizeChanged += (_, _) => ArrangeLoginLayout(projectLogo);
            using (var backgroundStream = typeof(FrmLogin).Assembly.GetManifestResourceStream("SmartInventoryManager.UI.LoginBackground")
                ?? throw new InvalidOperationException("Missing login background resource"))
            using (var originalBackground = Image.FromStream(backgroundStream))
            {
                pnlLeft.BackgroundImage = new Bitmap(originalBackground);
                pnlLeft.BackgroundImageLayout = ImageLayout.Stretch;
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
            txtUsername.AccessibleName = "Tên đăng nhập";
            txtPassword.AccessibleName = "Mật khẩu";
            label2.AccessibleName = "Thông báo đăng nhập";
            txtUsername.TextChanged += (_, _) => label2.Text = string.Empty;
            txtPassword.TextChanged += (_, _) => label2.Text = string.Empty;
            // NOTE: Title bar tùy chỉnh cần tự implement nếu muốn giống hình mẫu 100%
            // FormBorderStyle = None + tự vẽ title bar với icon cube + text
        }

        // Đổi kiểu hiển thị nút đăng nhập khi con trỏ đi vào.
        private void BtnLogin_MouseEnter(object? sender, EventArgs e)
        {
            btnLogin.BackColor = _btnLoginHoverColor;
            btnLogin.Cursor = Cursors.Hand;
        }

        // Khôi phục kiểu hiển thị nút đăng nhập khi con trỏ rời đi.
        private void BtnLogin_MouseLeave(object? sender, EventArgs e)
        {
            btnLogin.BackColor = _btnLoginOriginalColor;
            btnLogin.Cursor = Cursors.Default;
        }

        // Chuyển bước nhập hoặc đăng nhập khi người dùng nhấn phím ở ô tài khoản.
        private void TxtUsername_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                txtPassword.Focus();
            }
        }

        // Gửi yêu cầu đăng nhập khi nhấn Enter tại ô mật khẩu.
        private async void TxtPassword_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await PerformLoginAsync();
            }
        }

        // Hiển thị hướng dẫn liên hệ để được đặt lại mật khẩu.
        private void LnkForgotPassword_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "Vui lòng liên hệ quản trị viên để được hỗ trợ đặt lại mật khẩu.",
                "Quên mật khẩu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        // Tiếp nhận thao tác bấm nút và bắt đầu xác thực đăng nhập.
        private async void btnLogin_Click(object sender, EventArgs e)
        {
            await PerformLoginAsync();
        }

        // Kiểm tra thông tin nhập, xác thực tài khoản và trả kết quả đăng nhập.
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

                if (!RememberedUsername.TrySave(chkRemember.Checked ? AuthenticatedUser.Username : null))
                    MessageBox.Show(this, "Đăng nhập thành công nhưng không lưu được tùy chọn nhớ tên đăng nhập trên máy.",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (InvalidOperationException exception) when (exception.Message == "Thông tin đăng nhập bị trùng giữa các tài khoản. Vui lòng liên hệ ADMIN để kiểm tra.")
            {
                label2.Text = "Thông tin đăng nhập bị trùng. Vui lòng liên hệ quản trị viên để kiểm tra.";
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

        // Đổi trạng thái hiện hoặc che ký tự trong ô mật khẩu.
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
