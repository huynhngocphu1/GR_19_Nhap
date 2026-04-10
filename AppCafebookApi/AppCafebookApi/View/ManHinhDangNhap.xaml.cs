using System.Windows;
using System.Windows.Input;
using AppCafebookApi.Services;
using CafebookModel.Model.ModelApi;
using CafebookModel.Model.Data;
using AppCafebookApi.View.Common;
using AppCafebookApi.View.quanly;
using AppCafebookApi.View.nhanvien;
using System.Windows.Controls;
using System.Threading.Tasks; // <-- Thêm using này

namespace AppCafebookApi.View
{
    public partial class ManHinhDangNhap : Window
    {
        public ManHinhDangNhap()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtUsername.Focus();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var loginRequest = new LoginRequestModel
            {
                TenDangNhap = txtUsername.Text,
                MatKhau = chkShowPassword.IsChecked == true ? txtVisiblePassword.Text : txtPassword.Password
            };

            btnLogin.IsEnabled = false;
            btnLogin.Content = "ĐANG KIỂM TRA...";

            LoginResponseModel response = AuthService.LoginBackdoor(loginRequest);

            if (!response.Success)
            {
                response = await AuthService.LoginAsync(loginRequest);
            }

            if (response.Success && response.UserData != null)
            {
                // === BẮT ĐẦU SỬA LUỒNG (FLOW) ===

                // 1. Ẩn màn hình đăng nhập ngay lập tức
                this.Hide();

                // 2. Hiển thị màn hình chào mừng (ShowDialog để chặn tương tác)
                HienThiManHinhChao(response.UserData);

                // 3. (Sau khi màn hình chào mừng đóng) Phân quyền và mở màn hình chính
                PhanQuyenVaChuyenHuong(response.UserData.TenVaiTro ?? string.Empty);

                // 4. Đóng hẳn màn hình đăng nhập
                this.Close();

                // === KẾT THÚC SỬA LUỒNG ===
            }
            else
            {
                // Đăng nhập thất bại, hiện lại nút
                btnLogin.IsEnabled = true;
                btnLogin.Content = "ĐĂNG NHẬP";
                MessageBox.Show(response.Message, "Đăng nhập thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HienThiManHinhChao(NhanVienDto user)
        {
            var welcomeWindow = new WelcomeWindow(user);
            // Không cần gán Owner nữa vì màn hình đăng nhập đã Hide()
            welcomeWindow.ShowDialog();
        }

        private void PhanQuyenVaChuyenHuong(string tenVaiTro)
        {
            if (tenVaiTro == "Quản trị viên" || tenVaiTro == "Quản lý")
            {
                ManHinhQuanly adminWindow = new ManHinhQuanly();
                adminWindow.Show();
            }
            else
            {
                ManHinhNhanVien employeeWindow = new ManHinhNhanVien();
                employeeWindow.Show();
            }

            // this.Close(); // Đã chuyển lên hàm BtnLogin_Click
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && btnLogin.IsEnabled) // Chỉ chạy nếu nút đang bật
            {
                BtnLogin_Click(sender, e);
                e.Handled = true;
            }
        }

        // --- Logic Hiển thị Mật khẩu ---

        private void ChkShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            txtVisiblePassword.Text = txtPassword.Password;
            txtVisiblePassword.Visibility = Visibility.Visible;
            txtPassword.Visibility = Visibility.Collapsed;
        }

        private void ChkShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            txtPassword.Password = txtVisiblePassword.Text;
            txtVisiblePassword.Visibility = Visibility.Collapsed;
            txtPassword.Visibility = Visibility.Visible;
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (chkShowPassword.IsChecked == true)
            {
                txtVisiblePassword.Text = txtPassword.Password;
            }
        }

        private void TxtVisiblePassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (chkShowPassword.IsChecked == false)
            {
                txtPassword.Password = txtVisiblePassword.Text;
            }
        }
    }
}