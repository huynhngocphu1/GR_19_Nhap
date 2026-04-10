// Tệp: AppCafebookApi/View/nhanvien/pages/ChamCongView.xaml.cs
using AppCafebookApi.Services;
using CafebookModel.Model.ModelApp.NhanVien;
using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AppCafebookApi.View.common;
using System.Net.Http;

namespace AppCafebookApi.View.nhanvien.pages
{
    public partial class ChamCongView : Page
    {
        private DispatcherTimer _timerClock;
        private DispatcherTimer _timerWork;
        private DateTime? _gioVaoCache;

        private readonly Brush _mauXanh = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28a745"));
        private readonly Brush _mauDo = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc3545"));
        private readonly Brush _mauXam = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6c757d"));
        private readonly Brush _mauCam = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fd7e14"));
        private readonly Brush _mauLam = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007bff"));

        public ChamCongView()
        {
            InitializeComponent();

            _timerClock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timerClock.Tick += TimerClock_Tick;
            _timerClock.Start();

            _timerWork = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timerWork.Tick += TimerWork_Tick;
        }

        private void TimerClock_Tick(object? sender, EventArgs e)
        {
            lblThoiGianThucTe.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void TimerWork_Tick(object? sender, EventArgs e)
        {
            if (_gioVaoCache.HasValue)
            {
                var duration = DateTime.Now - _gioVaoCache.Value;
                lblTrangThaiChinh.Text = $"Đang làm: {duration:hh\\:mm\\:ss}";
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                return;

            if (AuthService.CurrentUser == null)
            {
                lblTrangThaiChinh.Text = "Vui lòng đăng nhập";
                btnChamCong.Visibility = Visibility.Collapsed;
                return;
            }

            await LoadStatusAsync();

            dpChonThang.SelectedDate = DateTime.Now;
            await LoadLichSuAsync(DateTime.Now.Month, DateTime.Now.Year);

            dpNgayBatDau.SelectedDate = DateTime.Today;
            dpNgayKetThuc.SelectedDate = DateTime.Today;
            cmbLoaiDon.SelectedIndex = 0;
        }

        private async Task LoadStatusAsync()
        {
            try
            {
                btnChamCong.IsEnabled = false;

                var response = await ApiClient.Instance.GetAsync("api/app/chamcong/status");

                if (response.IsSuccessStatusCode)
                {
                    var status = await response.Content.ReadFromJsonAsync<ChamCongDashboardDto>();
                    if (status != null)
                    {
                        UpdateUI(status);
                    }
                }
                else
                {
                    lblTrangThaiChinh.Text = "Lỗi tải trạng thái.";
                }
            }
            catch (Exception ex)
            {
                // Hiển thị lỗi chi tiết hơn (nếu có)
                lblTrangThaiChinh.Text = $"Lỗi kết nối API: {ex.Message}";
            }
        }

        private void UpdateUI(ChamCongDashboardDto status)
        {
            ManHinhNhanVien.CurrentTrangThai = status.TrangThai;

            btnChamCong.Visibility = Visibility.Visible;
            btnChamCong.IsEnabled = true;
            _timerWork.Stop();
            _gioVaoCache = null;

            UpdateUI_Phu(status); // Gọi hàm đã sửa lỗi

            switch (status.TrangThai)
            {
                case "NghiPhep":
                    lblTrangThaiChinh.Text = status.TenCa; // "Nghỉ phép"
                    btnChamCong.Content = "ĐANG NGHỈ PHÉP";
                    btnChamCong.Background = _mauLam;
                    btnChamCong.IsEnabled = false;
                    btnChamCong.Tag = null;
                    break;
                case "KhongCoCa":
                    lblTrangThaiChinh.Text = status.TenCa; // "Không có lịch làm"
                    btnChamCong.Visibility = Visibility.Collapsed;
                    break;
                case "ChuaChamCong":
                    lblTrangThaiChinh.Text = $"{status.TenCa} ({status.GioBatDauCa:hh\\:mm} - {status.GioKetThucCa:hh\\:mm})";
                    btnChamCong.Content = "CHẤM CÔNG";
                    btnChamCong.Background = _mauXanh;
                    btnChamCong.Tag = "clock-in";
                    break;
                case "DaChamCong":
                    _gioVaoCache = status.GioVao;
                    _timerWork.Start();
                    TimerWork_Tick(null, EventArgs.Empty);
                    btnChamCong.Content = "TRẢ CA";
                    btnChamCong.Background = _mauDo;
                    btnChamCong.Tag = "clock-out";
                    break;
                case "DaTraCa":
                    lblTrangThaiChinh.Text = $"Đã trả ca lúc {status.GioRa:HH:mm} (Làm: {status.SoGioLam:N2} giờ)";
                    btnChamCong.Content = "ĐÃ TRẢ CA";
                    btnChamCong.Background = _mauXam;
                    btnChamCong.IsEnabled = false;
                    btnChamCong.Tag = null;
                    break;
            }
        }

        // *** HÀM ĐÃ SỬA LỖI ***
        private void UpdateUI_Phu(ChamCongDashboardDto status)
        {
            if (!string.IsNullOrEmpty(status.TrangThaiDonNghi))
            {
                lblTrangThaiDonNghi.Text = status.TrangThaiDonNghi;
                if (status.TrangThaiDonNghi == "Đã duyệt")
                {
                    // SỬA: Tham chiếu trực tiếp đến borderDonNghi
                    borderDonNghi.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                    lblTrangThaiDonNghi.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
                }
                else
                {
                    // SỬA: Tham chiếu trực tiếp đến borderDonNghi
                    borderDonNghi.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1"));
                    lblTrangThaiDonNghi.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA000"));
                }
                borderDonNghi.Visibility = Visibility.Visible;
            }
            else
            {
                borderDonNghi.Visibility = Visibility.Collapsed;
            }

            if (status.SoLanDiTreThangNay > 0)
            {
                lblDiTre.Text = $"Trễ: {status.SoLanDiTreThangNay} lần";
                borderDiTre.Visibility = Visibility.Visible;
            }
            else
            {
                borderDiTre.Visibility = Visibility.Collapsed;
            }
        }
        // *** KẾT THÚC SỬA LỖI ***

        private async void BtnChamCong_Click(object sender, RoutedEventArgs e)
        {
            string? action = btnChamCong.Tag as string;
            if (string.IsNullOrEmpty(action)) return;

            btnChamCong.IsEnabled = false;
            string apiUrl = (action == "clock-in") ? "api/app/chamcong/clock-in" : "api/app/chamcong/clock-out";

            try
            {
                var response = await ApiClient.Instance.PostAsync(apiUrl, null);
                if (response.IsSuccessStatusCode)
                {
                    var newStatus = await response.Content.ReadFromJsonAsync<ChamCongDashboardDto>();
                    if (newStatus != null)
                    {
                        UpdateUI(newStatus);
                    }
                }
                else
                {
                    string errorMessage = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(errorMessage, "Lỗi Chấm Công", MessageBoxButton.OK, MessageBoxImage.Warning);
                    btnChamCong.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối API: {ex.Message}", "Lỗi nghiêm trọng", MessageBoxButton.OK, MessageBoxImage.Error);
                btnChamCong.IsEnabled = true;
            }
        }

        private async Task LoadLichSuAsync(int thang, int nam)
        {
            try
            {
                var response = await ApiClient.Instance.GetFromJsonAsync<LichSuChamCongPageDto>($"api/app/chamcong/lich-su?thang={thang}&nam={nam}");
                if (response != null)
                {
                    dgLichSuChamCong.ItemsSource = response.LichSuChamCong;
                    dgDonXinNghi.ItemsSource = response.DanhSachDonNghi;

                    lblTongGioLam.Text = response.ThongKe.TongGioLam.ToString("N2");
                    lblSoLanDiTre.Text = response.ThongKe.SoLanDiTre.ToString();
                    lblSoNgayNghiPhep.Text = response.ThongKe.SoNgayNghiPhep.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch sử chấm công: {ex.Message}", "Lỗi API", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DpChonThang_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpChonThang.SelectedDate.HasValue && !IsLoaded)
                return;

            var date = dpChonThang.SelectedDate ?? DateTime.Now;
            await LoadLichSuAsync(date.Month, date.Year);
        }

        private async void BtnGuiDon_Click(object sender, RoutedEventArgs e)
        {
            if (dpNgayBatDau.SelectedDate == null || dpNgayKetThuc.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn ngày bắt đầu và ngày kết thúc.", "Thiếu thông tin");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtLyDo.Text))
            {
                MessageBox.Show("Vui lòng nhập lý do xin nghỉ.", "Thiếu thông tin");
                return;
            }
            if (dpNgayKetThuc.SelectedDate < dpNgayBatDau.SelectedDate)
            {
                MessageBox.Show("Ngày kết thúc không thể trước ngày bắt đầu.", "Lỗi logic");
                return;
            }

            var request = new DonXinNghiRequestDto
            {
                LoaiDon = (cmbLoaiDon.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Đơn xin nghỉ",
                LyDo = txtLyDo.Text,
                NgayBatDau = dpNgayBatDau.SelectedDate.Value,
                NgayKetThuc = dpNgayKetThuc.SelectedDate.Value
            };

            btnGuiDon.IsEnabled = false;
            try
            {
                var response = await ApiClient.Instance.PostAsJsonAsync("api/app/chamcong/submit-leave", request);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Gửi đơn xin nghỉ thành công!", "Thành công");
                    BtnHuyGuiDon_Click(null, null); // Xóa form

                    var date = dpChonThang.SelectedDate ?? DateTime.Now;
                    await LoadLichSuAsync(date.Month, date.Year);
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi gửi đơn: {error}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi nghiêm trọng");
            }
            btnGuiDon.IsEnabled = true;
        }

        private void BtnHuyGuiDon_Click(object? sender, RoutedEventArgs? e)
        {
            txtLyDo.Text = "";
            dpNgayBatDau.SelectedDate = DateTime.Today;
            dpNgayKetThuc.SelectedDate = DateTime.Today;
            cmbLoaiDon.SelectedIndex = 0;
        }
    }
}