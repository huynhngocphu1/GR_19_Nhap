using AppCafebookApi.Services;
using AppCafebookApi.View;
using AppCafebookApi.View.common;
using AppCafebookApi.View.quanly.pages;
using CafebookModel.Model.Data;
using CafebookModel.Model.ModelApp;
using CafebookModel.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace AppCafebookApi.View.quanly
{
    public partial class ManHinhQuanly : Window
    {
        private ToggleButton? currentNavButton;
        private NhanVienDto? currentUser;
        private DispatcherTimer _notificationTimer;
        private static readonly HttpClient httpClient;

        static ManHinhQuanly()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public ManHinhQuanly()
        {
            InitializeComponent();
            _notificationTimer = new DispatcherTimer();
            _notificationTimer.Interval = TimeSpan.FromSeconds(30);
            _notificationTimer.Tick += _notificationTimer_Tick;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            currentUser = AuthService.CurrentUser;
            if (currentUser != null)
            {
                // ... (Code cập nhật Avatar, Tên, Phân quyền giữ nguyên) ...
                txtAdminName.Text = currentUser.HoTen;
                txtUserRole.Text = currentUser.TenVaiTro;
                AvatarBorder.Child = null;
                BitmapImage avatarImage = HinhAnhHelper.LoadImage(
                    currentUser.AnhDaiDien,
                    HinhAnhPaths.DefaultAvatar
                );
                AvatarBorder.Background = new ImageBrush(avatarImage)
                {
                    Stretch = Stretch.UniformToFill
                };

                // === SỬA LỖI 2: THÊM LOGIC PHÂN QUYỀN ===

                // 1. Ẩn tất cả các nút
                btnTongQuan.Visibility = Visibility.Collapsed;
                btnSanPham.Visibility = Visibility.Collapsed;
                btnBan.Visibility = Visibility.Collapsed;
                btnKho.Visibility = Visibility.Collapsed;
                btnDonHang.Visibility = Visibility.Collapsed;
                btnSach.Visibility = Visibility.Collapsed;
                btnNhanSu.Visibility = Visibility.Collapsed;
                btnKhachHang.Visibility = Visibility.Collapsed;
                btnLuong.Visibility = Visibility.Collapsed; // <-- Nút này bị thiếu trong code của bạn
                btnDanhGia.Visibility = Visibility.Collapsed; // <-- Thêm nút Đánh Giá

                // 2. Hiện lại theo quyền
                if (AuthService.CoQuyen("Dashboard.Xem"))
                    btnTongQuan.Visibility = Visibility.Visible;

                if (AuthService.CoQuyen("SanPham.QuanLy"))
                {
                    btnSanPham.Visibility = Visibility.Visible;
                    btnBan.Visibility = Visibility.Visible;
                    btnSach.Visibility = Visibility.Visible;
                }
                if (AuthService.CoQuyen("Kho.Nhap", "Kho.KiemKe"))
                    btnKho.Visibility = Visibility.Visible;

                if (AuthService.CoQuyen("HoaDon.Tao", "HoaDon.ThanhToan", "HoaDon.Huy"))
                    btnDonHang.Visibility = Visibility.Visible;

                if (AuthService.CoQuyen("NhanSu.QuanLy", "NhanSu.TinhLuong"))
                    btnNhanSu.Visibility = Visibility.Visible;

                // Gộp chung Lương vào Quyền Nhân sự
                if (AuthService.CoQuyen("NhanSu.TinhLuong"))
                    btnLuong.Visibility = Visibility.Visible;

                if (AuthService.CoQuyen("NhanSu.QuanLy"))
                {
                    btnKhachHang.Visibility = Visibility.Visible;
                    btnDanhGia.Visibility = Visibility.Visible; // <-- Hiện nút Đánh Giá
                }
            }

            // (Phần điều hướng mặc định giữ nguyên)
            if (btnTongQuan.Visibility == Visibility.Visible)
            {
                UpdateSelectedButton(btnTongQuan);
                MainFrame.Navigate(new TongQuanView());
            }

            await CheckNotificationsAsync();
            _notificationTimer.Start();
        }

        private async void _notificationTimer_Tick(object? sender, EventArgs e)
        {
            await CheckNotificationsAsync();
        }

        private async Task CheckNotificationsAsync()
        {
            // (Hàm này giữ nguyên)
            try
            {
                var result = await httpClient.GetFromJsonAsync<ThongBaoCountDto>("api/app/thongbao/unread-count");
                if (result != null && result.UnreadCount > 0)
                {
                    lblSoThongBao.Text = result.UnreadCount.ToString();
                    BadgeThongBao.Visibility = Visibility.Visible;
                }
                else
                {
                    BadgeThongBao.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi kiểm tra thông báo: {ex.Message}");
            }
        }

        /// <summary>
        /// Hàm Click chính, đã được sửa lại
        /// </summary>
        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as ToggleButton;
            if (clickedButton == null || clickedButton == currentNavButton)
            {
                if (clickedButton != null) clickedButton.IsChecked = true;
                return;
            }
            Page? pageToNavigate = null;

            if (clickedButton == btnTongQuan)
            {
                pageToNavigate = new TongQuanView();
            }
            else if (clickedButton == btnBan)
            {
                pageToNavigate = new QuanLyBanView();
            }
            else if (clickedButton == btnSanPham)
            {
                pageToNavigate = new QuanLySanPhamView();
            }
            else if (clickedButton == btnDonHang)
            {
                pageToNavigate = new QuanLyDonHangView();
            }
            else if (clickedButton == btnKhachHang)
            {
                pageToNavigate = new QuanLyKhachHangView();
            }
            else if (clickedButton == btnSach)
            {
                pageToNavigate = new QuanLySachView();
            }
            else if (clickedButton == btnNhanSu)
            {
                pageToNavigate = new QuanLyNhanVienView();
            }
            else if (clickedButton == btnLuong)
            {
                pageToNavigate = new QuanLyLuongView();
            }
            else if (clickedButton == btnKho)
            {
                pageToNavigate = new QuanLyTonKhoView();
            }
            // === SỬA LỖI 3: THÊM CASE CHO NÚT ĐÁNH GIÁ ===
            else if (clickedButton == btnDanhGia)
            {
                pageToNavigate = new QuanLyDanhGiaView();
            }
            // === KẾT THÚC SỬA LỖI 3 ===
            else if (clickedButton.Name == "btnLichLamViec")
            {
                pageToNavigate = new QuanLyLichLamViecView();
            }
            else if (clickedButton.Name == "btnDonXinNghi")
            {
                pageToNavigate = new QuanLyDonXinNghiView();
            }
            else if (clickedButton.Name == "btnCaiDatNhanSu")
            {
                pageToNavigate = new CaiDatNhanSuView();
            }
            else if (clickedButton.Name == "btnBaoCaoNhanSu")
            {
                pageToNavigate = new BaoCaoNhanSuView();
            }
            // (Đã xóa dòng 'else if (clickedButton.Name == "")' bị lỗi)

            if (pageToNavigate != null)
            {
                UpdateSelectedButton(clickedButton);
                MainFrame.Navigate(pageToNavigate);
            }
            else
            {
                // (Phần thông báo lỗi giữ nguyên)
                MessageBox.Show($"Chức năng '{clickedButton.Content}' đang được phát triển!", "Thông báo");
                clickedButton.IsChecked = false;
                if (currentNavButton != null)
                {
                    currentNavButton.IsChecked = true;
                }
            }
        }

        private void UpdateSelectedButton(ToggleButton newButton)
        {
            // (Hàm này giữ nguyên)
            if (currentNavButton != null && currentNavButton != newButton)
            {
                currentNavButton.IsChecked = false;
            }
            currentNavButton = newButton;
            currentNavButton.IsChecked = true;
        }

        private async void BtnThongBao_Click(object sender, RoutedEventArgs e)
        {
            // (Hàm này giữ nguyên)
            try
            {
                var allNotifications = await httpClient.GetFromJsonAsync<List<ThongBaoDto>>("api/app/thongbao/all");

                if (allNotifications != null)
                {
                    var unreadNotifications = allNotifications
                        .Where(t => t.DaXem == false)
                        .ToList();
                    icThongBaoPopup.ItemsSource = unreadNotifications;
                }
                else
                {
                    icThongBaoPopup.ItemsSource = null;
                }

                PopupThongBao.IsOpen = true;
                await CheckNotificationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải thông báo: {ex.Message}", "Lỗi API");
            }
        }

        private async void ThongBaoItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ThongBaoDto thongBao)
            {
                // 1. Đánh dấu đã đọc (gọi API)
                if (!thongBao.DaXem)
                {
                    try
                    {
                        var response = await httpClient.PostAsync($"/api/app/thongbao/mark-as-read/{thongBao.IdThongBao}", null);
                        if (response.IsSuccessStatusCode)
                        {
                            thongBao.DaXem = true;
                            // Cập nhật lại UI nếu cần (ví dụ đổi màu background)
                            border.Background = Brushes.Transparent;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Lỗi đánh dấu đã đọc: {ex.Message}");
                    }
                }

                // 2. Đóng Popup thông báo
                // 3. ĐIỀU HƯỚNG MÀN HÌNH TƯƠNG ỨNG
                // Xử lý Sự cố bàn -> Màn hình Quản lý Bàn
                if (thongBao.LoaiThongBao == "SuCoBan")
                {
                    if (thongBao.IdLienQuan.HasValue)
                    {
                        int idBanCanDen = thongBao.IdLienQuan.Value;
                        UpdateSelectedButton(btnBan); // Giả sử btnBan là nút điều hướng trang Bàn
                        MainFrame.Navigate(new QuanLyBanView(idBanCanDen));
                    }
                }
                // Xử lý Kho và Cảnh báo kho (và HetHang cũ nếu còn dùng) -> Màn hình Tồn Kho
                else if (thongBao.LoaiThongBao == "Kho" ||
                         thongBao.LoaiThongBao == "CanhBaoKho" ||
                         thongBao.LoaiThongBao == "HetHang")
                {
                    UpdateSelectedButton(btnKho); // Giả sử btnKho là nút điều hướng trang Kho
                    MainFrame.Navigate(new QuanLyTonKhoView());
                }
                // Xử lý Đơn xin nghỉ -> Màn hình Đơn xin nghỉ
                else if (thongBao.LoaiThongBao == "DonXinNghi")
                {
                    // Tìm nút btnDonXinNghi hoặc dùng nút Nhân sự
                    ToggleButton? targetButton = FindName("btnDonXinNghi") as ToggleButton ?? btnNhanSu;
                    UpdateSelectedButton(targetButton);
                    MainFrame.Navigate(new QuanLyDonXinNghiView());
                }
                // Xử lý Hệ thống -> Chỉ hiện thông báo (vì thường không có màn hình sửa lỗi hệ thống cho User)
                else if (thongBao.LoaiThongBao == "HeThong")
                {
                    MessageBox.Show(thongBao.NoiDung, "Thông báo hệ thống", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // 4. Cập nhật lại số lượng thông báo chưa đọc (badge)
                await CheckNotificationsAsync();
            }
        }

        private void BtnDangXuat_Click(object sender, RoutedEventArgs e)
        {
            // (Hàm này giữ nguyên)
            var result = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?",
                                        "Xác nhận đăng xuất",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                AuthService.Logout();
                ManHinhDangNhap loginWindow = new ManHinhDangNhap();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}