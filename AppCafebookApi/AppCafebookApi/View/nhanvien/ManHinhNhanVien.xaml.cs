using AppCafebookApi.Services;
using AppCafebookApi.View.nhanvien.pages;
using CafebookModel.Utils;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CafebookModel.Model.ModelApp; // Chứa ThongBaoDto
using CafebookModel.Model.ModelApp.NhanVien; // Chứa ChamCongDashboardDto
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace AppCafebookApi.View.nhanvien
{
    public partial class ManHinhNhanVien : Window
    {
        // --- 1. KHAI BÁO BIẾN ---
        private DispatcherTimer _sidebarTimer;      // Timer cập nhật trạng thái chấm công
        private DispatcherTimer _notificationTimer; // Timer cập nhật thông báo
        private static readonly HttpClient httpClient; // Dùng chung cho toàn app để tránh socket exhaustion

        public static string CurrentTrangThai { get; set; } = "KhongCoCa";

        // Khởi tạo HttpClient một lần duy nhất (Static Constructor)
        static ManHinhNhanVien()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166") // Đổi port nếu cần thiết
            };
        }

        public ManHinhNhanVien()
        {
            InitializeComponent();
            this.Loaded += ManHinhNhanVien_Loaded;

            // Timer chấm công (30s/lần)
            _sidebarTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _sidebarTimer.Tick += SidebarTimer_Tick;

            // Timer thông báo (30s/lần)
            _notificationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _notificationTimer.Tick += _notificationTimer_Tick;
        }

        // --- 2. HÀM LOAD MÀN HÌNH ---
        private async void ManHinhNhanVien_Loaded(object sender, RoutedEventArgs e)
        {
            var currentUser = AuthService.CurrentUser;
            if (currentUser == null) return;

            // Cập nhật giao diện User
            txtUserName.Text = currentUser.HoTen ?? string.Empty;
            txtUserRole.Text = currentUser.TenVaiTro ?? string.Empty;

            try
            {
                AvatarBorder.Child = null;
                BitmapImage avatarImage = HinhAnhHelper.LoadImage(
                    currentUser.AnhDaiDien,
                    HinhAnhPaths.DefaultAvatar
                );
                AvatarBorder.Background = new ImageBrush(avatarImage) { Stretch = Stretch.UniformToFill };
            }
            catch { }

            // Phân quyền hiển thị nút
            SetupPermissions(currentUser);

            // Điều hướng trang mặc định
            if (btnSoDoBan != null && btnSoDoBan.Visibility == Visibility.Visible)
            {
                btnSoDoBan.IsChecked = true;
                NavigateToPage(btnSoDoBan, new SoDoBanView());
            }
            else if (btnThongTinCaNhan != null)
            {
                btnThongTinCaNhan.IsChecked = true;
                NavigateToPage(btnThongTinCaNhan, new ThongTinCaNhanView());
            }

            // Khởi động các Timer
            await UpdateSidebarStatusSafeAsync(); // Check chấm công ngay lập tức
            _sidebarTimer.Start();

            await CheckNotificationsAsync();      // Check thông báo ngay lập tức
            _notificationTimer.Start();
        }

        // --- 3. LOGIC THÔNG BÁO (NOTIFICATION) ---

        // Timer Tick
        private async void _notificationTimer_Tick(object? sender, EventArgs e)
        {
            await CheckNotificationsAsync();
        }

        // Hàm kiểm tra số lượng tin chưa đọc
        private async Task CheckNotificationsAsync()
        {
            try
            {
                // Gọi API dành riêng cho Staff (Lọc: PhieuGoiMon, DatBan, DonHangMoi, HeThong)
                var result = await httpClient.GetFromJsonAsync<ThongBaoCountDto>("api/app/thongbao/staff/unread-count");

                if (result != null && result.UnreadCount > 0)
                {
                    lblSoThongBao.Text = result.UnreadCount > 99 ? "99+" : result.UnreadCount.ToString();
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

        // Sự kiện Click nút Chuông -> Mở Popup
        private async void BtnThongBao_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Gọi API lấy danh sách tin nhắn cho Staff
                var allNotifications = await httpClient.GetFromJsonAsync<List<ThongBaoDto>>("api/app/thongbao/staff/all");

                if (allNotifications != null)
                {
                    // Hiển thị lên Popup
                    icThongBaoPopup.ItemsSource = allNotifications;
                }
                else
                {
                    icThongBaoPopup.ItemsSource = null;
                }

                PopupThongBao.IsOpen = true; // Mở Popup

                // Cập nhật lại số lượng (để đảm bảo đồng bộ)
                await CheckNotificationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải thông báo: {ex.Message}", "Lỗi kết nối");
            }
        }

        // Sự kiện Click vào 1 dòng thông báo
        private async void ThongBaoItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ThongBaoDto thongBao)
            {
                // A. Đánh dấu đã đọc (Gọi API)
                if (!thongBao.DaXem)
                {
                    try
                    {
                        var response = await httpClient.PostAsync($"/api/app/thongbao/mark-as-read/{thongBao.IdThongBao}", null);
                        if (response.IsSuccessStatusCode)
                        {
                            thongBao.DaXem = true;
                            // Đổi màu nền ngay lập tức trên UI để phản hồi người dùng
                            border.Background = Brushes.Transparent;
                        }
                    }
                    catch (Exception ex) { Debug.WriteLine($"Lỗi mark read: {ex.Message}"); }
                }

                // B. Đóng Popup
                PopupThongBao.IsOpen = false;

                // C. ĐIỀU HƯỚNG THÔNG MINH (Dựa trên Loại Thông Báo)
                switch (thongBao.LoaiThongBao)
                {
                    case "PhieuGoiMon":
                        // Có món mới/trả món -> Về Sơ đồ bàn
                        NavigateToPage(btnSoDoBan, new SoDoBanView());
                        break;

                    case "DatBan":
                        // Có khách đặt bàn -> Về trang Đặt bàn
                        NavigateToPage(btnDatBan, new DatBanView());
                        break;

                    case "DonHangMoi":
                        // Có đơn ship -> Về trang Giao hàng
                        NavigateToPage(btnGiaoHang, new GiaoHangView());
                        break;

                    case "HeThong":
                        // Thông báo hệ thống -> Chỉ hiện Dialog
                        MessageBox.Show(thongBao.NoiDung, "Thông báo hệ thống", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;

                    default:
                        // Các loại khác -> Mặc định về trang chủ (Sơ đồ bàn)
                        NavigateToPage(btnSoDoBan, new SoDoBanView());
                        break;
                }

                // D. Cập nhật lại Badge
                await CheckNotificationsAsync();
            }
        }

        // --- 4. NAVIGATION & PERMISSIONS ---

        private void SetupPermissions(dynamic currentUser)
        {
            bool isFullRole = string.Equals(currentUser.TenVaiTro?.Trim(), "Cửa Hàng Trưởng", StringComparison.OrdinalIgnoreCase);

            var navButtons = new List<ToggleButton> {
                btnSoDoBan, btnCheBien, btnDatBan, btnGiaoHang, btnThueSach,
                btnThongTinCaNhan, btnChamCong, btnLichLamViecCuaToi, btnPhieuLuongCuaToi, 
            };

            // Mặc định hiện các trang cá nhân
            btnThongTinCaNhan.Visibility = Visibility.Visible;
            btnChamCong.Visibility = Visibility.Visible;
            btnLichLamViecCuaToi.Visibility = Visibility.Visible;
            btnPhieuLuongCuaToi.Visibility = Visibility.Visible;
            btnThongBao.Visibility = Visibility.Visible; // Luôn hiện thông báo

            if (isFullRole)
            {
                foreach (var btn in navButtons) if (btn != null) btn.Visibility = Visibility.Visible;
            }
            else
            {
                // Logic phân quyền cũ
                bool coQuyenOrder = AuthService.CoQuyen("BanHang.XemSoDo", "BanHang.ThanhToan");
                if (btnSoDoBan != null) btnSoDoBan.Visibility = coQuyenOrder ? Visibility.Visible : Visibility.Collapsed;
                if (btnDatBan != null) btnDatBan.Visibility = coQuyenOrder ? Visibility.Visible : Visibility.Collapsed;

                bool coQuyenGiaoHang = AuthService.CoQuyen("BanHang.ThanhToan", "GiaoHang.Xem");
                if (btnGiaoHang != null) btnGiaoHang.Visibility = coQuyenGiaoHang ? Visibility.Visible : Visibility.Collapsed;

                bool coQuyenSach = AuthService.CoQuyen("Sach.QuanLy");
                if (btnThueSach != null) btnThueSach.Visibility = coQuyenSach ? Visibility.Visible : Visibility.Collapsed;

                bool coQuyenCheBien = AuthService.CoQuyen("CheBien.Xem");
                if (btnCheBien != null) btnCheBien.Visibility = coQuyenCheBien ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void NavigateToPage(ToggleButton? clickedButton, Page pageInstance)
        {
            if (clickedButton == null) return;
            UncheckOtherButtons(clickedButton);
            clickedButton.IsChecked = true;
            MainFrame.Navigate(pageInstance);
        }

        private void UncheckOtherButtons(ToggleButton? exception)
        {
            var navButtons = new List<ToggleButton>
            {
                btnSoDoBan, btnCheBien, btnDatBan, btnGiaoHang, btnThueSach,
                btnThongTinCaNhan, btnChamCong, btnLichLamViecCuaToi, btnPhieuLuongCuaToi, 
            };
            foreach (var button in navButtons)
            {
                if (button != null && button != exception) button.IsChecked = false;
            }
        }

        // Sự kiện Click Menu
        private void BtnSoDoBan_Click(object sender, RoutedEventArgs e) => NavigateToPage(sender as ToggleButton, new SoDoBanView());
        private void BtnDatBan_Click(object sender, RoutedEventArgs e) => NavigateToPage(sender as ToggleButton, new DatBanView());
        private void BtnGiaoHang_Click(object sender, RoutedEventArgs e) => NavigateToPage(sender as ToggleButton, new GiaoHangView());
        private void BtnCheBien_Click(object sender, RoutedEventArgs e) => NavigateToPage(sender as ToggleButton, new CheBienView());
        private void BtnThueSach_Click(object sender, RoutedEventArgs e) => NavigateToPage(sender as ToggleButton, new ThueSachView());
        private void BtnThongTinCaNhan_Click(object sender, RoutedEventArgs e) => NavigateToPage(sender as ToggleButton, new ThongTinCaNhanView());
        private void BtnChamCong_Click(object sender, RoutedEventArgs e) => NavigateToPage(sender as ToggleButton, new ChamCongView());
        private void BtnLichLamViecCuaToi_Click(object sender, RoutedEventArgs e) => NavigateToPage(sender as ToggleButton, new LichLamViecView());
        private void BtnPhieuLuongCuaToi_Click(object sender, RoutedEventArgs e) => NavigateToPage(sender as ToggleButton, new PhieuLuongView());

        // --- 5. SIDEBAR STATUS & LOGOUT ---

        private async void SidebarTimer_Tick(object? sender, EventArgs e) => await UpdateSidebarStatusSafeAsync();

        private async Task UpdateSidebarStatusSafeAsync()
        {
            try { await UpdateSidebarStatusAsync(); }
            catch
            {
                if (lblSidebarStatus != null)
                {
                    lblSidebarStatus.Text = "Lỗi đồng bộ";
                    lblSidebarStatus.Foreground = Brushes.OrangeRed;
                }
            }
        }

        private async Task UpdateSidebarStatusAsync()
        {
            if (AuthService.CurrentUser == null || lblSidebarStatus == null) return;
            try
            {
                // Dùng ApiClient.Instance (đã có auth header) cho các request nghiệp vụ cần bảo mật
                var status = await ApiClient.Instance.GetFromJsonAsync<ChamCongDashboardDto>("api/app/chamcong/status");
                if (status != null)
                {
                    CurrentTrangThai = status.TrangThai;
                    switch (status.TrangThai)
                    {
                        case "DaChamCong":
                            // FIX LỖI NULL: Kiểm tra HasValue trước khi dùng
                            if (status.GioVao.HasValue)
                            {
                                var duration = DateTime.Now - status.GioVao.Value;
                                lblSidebarStatus.Text = $"Đang làm ({duration:hh\\:mm})";
                            }
                            else
                            {
                                lblSidebarStatus.Text = "Đang làm";
                            }
                            lblSidebarStatus.Foreground = Brushes.LightGreen;
                            break;

                        case "ChuaChamCong":
                            lblSidebarStatus.Text = "Chưa chấm công";
                            lblSidebarStatus.Foreground = Brushes.LightGray;
                            break;

                        case "NghiPhep":
                            lblSidebarStatus.Text = "Đang nghỉ phép";
                            lblSidebarStatus.Foreground = Brushes.LightBlue;
                            break;

                        case "KhongCoCa":
                            lblSidebarStatus.Text = "Không có ca";
                            lblSidebarStatus.Foreground = Brushes.Gray;
                            break;

                        default:
                            lblSidebarStatus.Text = "Đã trả ca";
                            lblSidebarStatus.Foreground = Brushes.Gray;
                            break;
                    }
                }
            }
            catch
            {
                lblSidebarStatus.Text = "Lỗi đồng bộ";
                lblSidebarStatus.Foreground = Brushes.OrangeRed;
            }
        }

        public static string GetCurrentCheckInStatus() => CurrentTrangThai;

        private void BtnDangXuat_Click(object sender, RoutedEventArgs e)
        {
            if (GetCurrentCheckInStatus() == "DaChamCong")
            {
                MessageBox.Show("Bạn chưa trả ca. Vui lòng nhấn \"TRẢ CA\" trước khi đăng xuất.", "Cảnh báo chưa trả ca", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác nhận đăng xuất", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                AuthService.Logout();
                new ManHinhDangNhap().Show();
                this.Close();
            }
        }
    }
}