using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AppCafebookApi.Services;
using AppCafebookApi.View.common;
using System.Linq;
using System.Windows.Controls.Primitives;
using CafebookModel.Model.ModelApp.NhanVien;
using CafebookModel.Model.ModelApp;
using System.Text.Json;

namespace AppCafebookApi.View.nhanvien.pages
{
    public partial class SoDoBanView : Page
    {
        private int? _idBanToHighlight = null; // <-- THÊM DÒNG NÀY
        // THÊM LỚP HELPER NÀY VÀO BÊN TRONG CLASS SoDoBanView
        private class CreateOrderResponseDto
        {
            [System.Text.Json.Serialization.JsonPropertyName("idHoaDon")]
            public int idHoaDon { get; set; }
        }
        private enum SelectionMode { None, ChuyenBan, GopBan }

        private static readonly HttpClient httpClient;
        private BanSoDoDto? _selectedBan = null;
        private List<BanSoDoDto> _allTablesCache = new List<BanSoDoDto>();
        private List<KhuVucDto> _khuVucCache = new List<KhuVucDto>();
        private SelectionMode _currentMode = SelectionMode.None;

        static SoDoBanView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public SoDoBanView()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        // THÊM HÀM KHỞI TẠO MỚI NÀY:
        public SoDoBanView(int idBan)
        {
            InitializeComponent();
            _idBanToHighlight = idBan; // Lưu ID bàn lại
        }

        #region Tải Dữ Liệu và Lọc Khu Vực

        // [SoDoBanView.xaml.cs]
        // THAY THẾ TOÀN BỘ HÀM PAGE_LOADED

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            MainPanel.Opacity = 0.5;
            // Tải lại toàn bộ dữ liệu khi trang được tải
            await ReloadDataAsync(); // [cite: 2, 66]
            MainPanel.Opacity = 1.0;

            // === SỬA LỖI LOGIC HIGHLIGHT ===
             if (_idBanToHighlight.HasValue) // 
            {
                var banToSelect = _allTablesCache.FirstOrDefault(b => b.IdBan == _idBanToHighlight.Value); // 
                if (banToSelect != null)
                {
                    // Kiểm tra xem bàn có trong filter hiện tại không
                    var currentItemsSource = icBan.ItemsSource as IEnumerable<BanSoDoDto>;
                    if (currentItemsSource == null || !currentItemsSource.Any(b => b.IdBan == banToSelect.IdBan))
                    {
                        // Nếu KHÔNG, BỎ QUA BỘ LỌC:
                        // 1. Reset về "Tất cả"
                        btnKhuVucAll.IsChecked = true;
                        UncheckOtherKhuVucButtons(btnKhuVucAll); // 
                                                                 // 2. Áp dụng filter "Tất cả" (null)
                        ApplyTableFilter(null); // 

                        // 3. Cập nhật UI ngay lập tức để tạo container
                        icBan.UpdateLayout();
                    }

                    // Tìm container (Button) của bàn
                    var banContainer = icBan.ItemContainerGenerator.ContainerFromItem(banToSelect) as FrameworkElement;
                    if (banContainer != null)
                    {
                        // YÊU CẦU MỚI: "Trỏ vào" (Cuộn đến)
                        banContainer.BringIntoView();
                    }

                    // Mở Bảng điều khiển cho bàn đó
                    ShowPanelForBan(banToSelect); // 
                }
                _idBanToHighlight = null; // [cite: 2, 80]
            }
            // === KẾT THÚC ===
        }

        // Đổi tên hàm Page_Loaded thành ReloadDataAsync
        private async Task ReloadDataAsync()
        {
            // Lưu lại khu vực đang chọn
            var selectedKhuVucBtn = FindCheckedKhuVucButton();
            int? selectedKhuVucId = null;
            if (selectedKhuVucBtn != null && selectedKhuVucBtn != btnKhuVucAll && selectedKhuVucBtn.DataContext is KhuVucDto dto)
            {
                selectedKhuVucId = dto.IdKhuVuc;
            }

            panelChuaChon.Visibility = Visibility.Visible;
            panelDaChon.Visibility = Visibility.Collapsed;

            // === XÓA DÒNG NÀY ===
            // _selectedBan = null; // <-- DÒNG NÀY GÂY LỖI
            // === KẾT THÚC ===

            // Tải lại cả hai danh sách
            await Task.WhenAll(LoadKhuVucSidebarAsync(), LoadTablesAsync());

            // Áp dụng lại bộ lọc
            ApplyTableFilter(selectedKhuVucId);

            // Chọn lại nút khu vực
            if (selectedKhuVucBtn != null)
                selectedKhuVucBtn.IsChecked = true;
            else if (btnKhuVucAll != null) // Thêm kiểm tra null
                btnKhuVucAll.IsChecked = true;
        }


        private async Task LoadKhuVucSidebarAsync()
        {
            try
            {
                _khuVucCache = (await httpClient.GetFromJsonAsync<List<KhuVucDto>>("api/app/banquanly/tree"))
                                 ?? new List<KhuVucDto>();
                icKhuVuc.ItemsSource = _khuVucCache;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải danh sách Khu Vực: {ex.Message}", "Lỗi API");
            }
        }

        private async Task LoadTablesAsync()
        {
            try
            {
                _allTablesCache = (await httpClient.GetFromJsonAsync<List<BanSoDoDto>>("api/app/sodoban/tables"))
                                      ?? new List<BanSoDoDto>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải sơ đồ bàn: {ex.Message}", "Lỗi API");
            }
        }

        private void BtnKhuVuc_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as ToggleButton;
            if (clickedButton == null) return;
            UncheckOtherKhuVucButtons(clickedButton);
            int? khuVucId = null;
            if (clickedButton != btnKhuVucAll && clickedButton.DataContext is KhuVucDto selectedKhuVuc)
            {
                khuVucId = selectedKhuVuc.IdKhuVuc;
            }
            ApplyTableFilter(khuVucId);
        }

        private void ApplyTableFilter(int? khuVucId)
        {
            if (khuVucId == null)
            {
                icBan.ItemsSource = _allTablesCache;
            }
            else
            {
                icBan.ItemsSource = _allTablesCache.Where(ban => ban.IdKhuVuc == khuVucId).ToList();
            }
        }

        private void UncheckOtherKhuVucButtons(ToggleButton? exception)
        {
            if (btnKhuVucAll != null && btnKhuVucAll != exception)
            {
                btnKhuVucAll.IsChecked = false;
            }
            foreach (var item in icKhuVuc.Items)
            {
                var container = icKhuVuc.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                if (container == null) continue;
                var toggleButton = FindVisualChild<ToggleButton>(container);
                if (toggleButton != null && toggleButton != exception)
                {
                    toggleButton.IsChecked = false;
                }
            }
        }

        private T? FindVisualChild<T>(DependencyObject? obj) where T : DependencyObject
        {
            if (obj == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null)
                {
                    if (child is T)
                        return (T)child;
                    else
                    {
                        T? childOfChild = FindVisualChild<T>(child);
                        if (childOfChild != null)
                            return childOfChild;
                    }
                }
            }
            return null;
        }

        private ToggleButton? FindCheckedKhuVucButton()
        {
            if (btnKhuVucAll != null && btnKhuVucAll.IsChecked == true) return btnKhuVucAll;
            foreach (var item in icKhuVuc.Items)
            {
                var container = icKhuVuc.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                if (container == null) continue;
                var toggleButton = FindVisualChild<ToggleButton>(container);
                if (toggleButton != null && toggleButton.IsChecked == true)
                {
                    return toggleButton;
                }
            }
            return btnKhuVucAll; // Mặc định
        }

        #endregion

        // === LOGIC XỬ LÝ CHÍNH ===

        private void BtnDonMoi_Click(object sender, RoutedEventArgs e)
        {
            var virtualBan = new BanSoDoDto
            {
                IdBan = -1, // Dấu hiệu "Tại Quầy"
                SoBan = "Tại Quầy",
                TrangThai = "Trống",
                IdKhuVuc = null,
                IdHoaDonHienTai = null,
                TongTienHienTai = 0
            };
            UncheckOtherKhuVucButtons(null);
            _selectedBan = null;
            ShowPanelForBan(virtualBan);
        }

        private async void BtnBan_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var clickedBan = button?.DataContext as BanSoDoDto;
            if (clickedBan == null) return;

            if (_currentMode != SelectionMode.None)
            {
                await HandleTableSelectionAsync(clickedBan);
            }
            else
            {
                UncheckOtherKhuVucButtons(null);
                ShowPanelForBan(clickedBan);
            }
        }

        private void ShowPanelForBan(BanSoDoDto ban)
        {
            _selectedBan = ban;

            panelChuaChon.Visibility = Visibility.Collapsed;
            panelChonBan.Visibility = Visibility.Collapsed;
            panelDaChon.Visibility = Visibility.Visible;

            runSoBan.Text = _selectedBan.SoBan;
            runTrangThai.Text = _selectedBan.TrangThai;

            // === LOGIC MỚI (YÊU CẦU 1) ===
            // Hiển thị thông tin đặt bàn trên panel nếu bàn "Trống"
            if (!string.IsNullOrEmpty(_selectedBan.ThongTinDatBan) && _selectedBan.TrangThai == "Trống")
            {
                tbThongTinDatBan.Text = _selectedBan.ThongTinDatBan;
                tbThongTinDatBan.Visibility = Visibility.Visible;
            }
            else
            {
                tbThongTinDatBan.Visibility = Visibility.Collapsed;
            }
            // === KẾT THÚC ===

            if (!string.IsNullOrEmpty(_selectedBan.GhiChu))
            {
                tbGhiChu.Text = $"Ghi chú: {_selectedBan.GhiChu}";
                tbGhiChu.Visibility = Visibility.Visible;
            }
            else
            {
                tbGhiChu.Visibility = Visibility.Collapsed;
            }

            switch (_selectedBan.TrangThai)
            {
                case "Trống":
                    btnGoiMon.Content = "Tạo Hóa Đơn Mới";
                    btnGoiMon.IsEnabled = true;
                    btnChuyenBan.IsEnabled = false;
                    btnGopBan.IsEnabled = false;
                    btnBaoCaoSuCo.IsEnabled = (_selectedBan.IdBan > 0);
                    tbTongTienWrapper.Visibility = Visibility.Collapsed;
                    break;
                case "Có khách":
                    btnGoiMon.Content = "Gọi Món / Thanh Toán";
                    btnGoiMon.IsEnabled = true;
                    btnChuyenBan.IsEnabled = true;
                    btnGopBan.IsEnabled = true;
                    btnBaoCaoSuCo.IsEnabled = false;
                    tbTongTienWrapper.Visibility = Visibility.Visible;
                    runTongTien.Text = _selectedBan.TongTienHienTai.ToString("N0") + " đ";
                    break;
                case "Đã đặt":
                    btnGoiMon.Content = "Khách đặt (Mở Hóa Đơn)";
                    btnGoiMon.IsEnabled = true;
                    btnChuyenBan.IsEnabled = false;
                    btnGopBan.IsEnabled = false;
                    btnBaoCaoSuCo.IsEnabled = true;
                    tbTongTienWrapper.Visibility = Visibility.Collapsed;
                    break;
                case "Bảo trì":
                case "Tạm ngưng":
                    btnGoiMon.Content = "BÀN ĐANG BẢO TRÌ";
                    btnGoiMon.IsEnabled = false;
                    btnChuyenBan.IsEnabled = false;
                    btnGopBan.IsEnabled = false;
                    btnBaoCaoSuCo.IsEnabled = false;
                    tbTongTienWrapper.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        // === HÀM BỊ LỖI ===
        private async void BtnGoiMon_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBan == null) return;

            if (_selectedBan.TrangThai == "Có khách")
            {
                int? idHoaDon = _selectedBan.IdHoaDonHienTai;
                if (idHoaDon.HasValue)
                {
                    this.NavigationService?.Navigate(new GoiMonView(idHoaDon.Value));
                }
                return;
            }

            // === SỬA LỖI NULL REFERENCE ===
            // Di chuyển kiểm tra Auth lên đầu
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("Lỗi: Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", "Lỗi Phiên");
                return;
            }

            // Gán idNhanVien SAU KHI đã kiểm tra
            int idNhanVien = AuthService.CurrentUser.IdNhanVien;
            // === KẾT THÚC SỬA LỖI ===

            int? idHoaDonMoi = null;

            try
            {
                HttpResponseMessage response;
                if (_selectedBan.IdBan > 0)
                {
                    response = await httpClient.PostAsJsonAsync($"api/app/sodoban/createorder/{_selectedBan.IdBan}/{idNhanVien}", new { });
                }
                else
                {
                    // Sửa "Tại quầy" thành "Tại quán" (Sửa lỗi CHECK constraint)
                    string loaiHoaDon = (_selectedBan.IdBan == -1) ? "Tại quán" : "Mang về";
                    response = await httpClient.PostAsJsonAsync($"api/app/sodoban/createorder-no-table/{idNhanVien}", loaiHoaDon);
                }

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreateOrderResponseDto>();
                    if (result != null && result.idHoaDon > 0)
                    {
                        idHoaDonMoi = result.idHoaDon;
                    }
                    if (_selectedBan.IdBan > 0)
                    {
                        // Sửa: Gọi hàm ReloadDataAsync() mới
                        await ReloadDataAsync();
                    }
                }
                else
                {
                    MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi tạo hóa đơn");
                    return;
                }
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi API: {ex.Message}", "Lỗi"); return; }

            if (idHoaDonMoi.HasValue)
            {
                this.NavigationService?.Navigate(new GoiMonView(idHoaDonMoi.Value));

                if (_selectedBan.IdBan <= 0)
                {
                    ResetForm();
                }
            }
        }

        private async void BtnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            MainPanel.Opacity = 0.5;
            await ReloadDataAsync();
            MainPanel.Opacity = 1.0;
        }


        private async void BtnBaoCaoSuCo_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBan == null) return;
            // Thêm kiểm tra Auth
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("Lỗi: Phiên đăng nhập đã hết hạn.", "Lỗi Phiên");
                return;
            }
            int idNhanVien = AuthService.CurrentUser.IdNhanVien;
            var inputDialog = new InputDialogWindow("Báo cáo sự cố", $"Vui lòng mô tả sự cố cho bàn {_selectedBan.SoBan}:");
            if (inputDialog.ShowDialog() == true)
            {
                string ghiChu = inputDialog.InputText;
                if (string.IsNullOrWhiteSpace(ghiChu)) return;
                try
                {
                    var request = new BaoCaoSuCoRequestDto { GhiChuSuCo = ghiChu };
                    var response = await httpClient.PostAsJsonAsync($"api/app/sodoban/reportproblem/{_selectedBan.IdBan}/{idNhanVien}", request);
                    if (response.IsSuccessStatusCode)
                    {
                        await ReloadDataAsync();
                    }
                    else
                    {
                        MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi báo cáo");
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Lỗi API: {ex.Message}", "Lỗi"); }
            }
        }

        private void StartSelectionMode(SelectionMode mode, string instructionText)
        {
            _currentMode = mode;
            selectionText.Text = instructionText;

            panelChuaChon.Visibility = Visibility.Collapsed;
            panelDaChon.Visibility = Visibility.Collapsed;
            panelChonBan.Visibility = Visibility.Visible;
        }

        private void BtnCancelSelect_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private void BtnChuyenBan_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBan == null) return;
            StartSelectionMode(SelectionMode.ChuyenBan,
                $"Đang Chuyển [Bàn {_selectedBan.SoBan}]\n" +
                $"Vui lòng chọn một [Bàn Trống] làm Bàn Đích.");
        }

        private void BtnGopBan_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBan == null) return;
            StartSelectionMode(SelectionMode.GopBan,
                $"Đang Gộp [Bàn {_selectedBan.SoBan}]\n" +
                $"Vui lòng chọn một [Bàn Có Khách] khác làm Bàn Đích.");
        }

        private async Task HandleTableSelectionAsync(BanSoDoDto targetBan)
        {
            if (_selectedBan == null)
            {
                ResetForm();
                return;
            }
            int? idHoaDonNguon = _selectedBan.IdHoaDonHienTai;
            if (!idHoaDonNguon.HasValue)
            {
                MessageBox.Show("Bàn nguồn không có hóa đơn để thao tác.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                ResetForm();
                return;
            }

            try
            {
                HttpResponseMessage response;
                BanActionRequestDto request = new BanActionRequestDto
                {
                    IdHoaDonNguon = idHoaDonNguon.Value
                };

                if (_currentMode == SelectionMode.ChuyenBan)
                {
                    if (targetBan.TrangThai != "Trống")
                    {
                        MessageBox.Show($"Bàn đích [{targetBan.SoBan}] phải là [Bàn Trống].", "Chọn sai bàn", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    request.IdBanDich = targetBan.IdBan;
                    response = await httpClient.PostAsJsonAsync("api/app/sodoban/move-table", request);
                }
                else // (mode == SelectionMode.GopBan)
                {
                    if (targetBan.TrangThai != "Có khách")
                    {
                        MessageBox.Show($"Bàn đích [{targetBan.SoBan}] phải là [Bàn Có Khách].", "Chọn sai bàn", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (targetBan.IdBan == _selectedBan.IdBan)
                    {
                        MessageBox.Show("Không thể gộp bàn vào chính nó.", "Chọn sai bàn", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    request.IdHoaDonDich = targetBan.IdHoaDonHienTai;
                    response = await httpClient.PostAsJsonAsync("api/app/sodoban/merge-table", request);
                }

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show(
                        _currentMode == SelectionMode.ChuyenBan ? "Chuyển bàn thành công!" : "Gộp bàn thành công!",
                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    await ReloadDataAsync();
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Thao tác thất bại: {error}", "Lỗi API", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ResetForm();
            }
        }

        private void ResetForm()
        {
            _selectedBan = null;

            panelDaChon.Visibility = Visibility.Collapsed;
            panelChonBan.Visibility = Visibility.Collapsed;
            panelChuaChon.Visibility = Visibility.Visible;

            _currentMode = SelectionMode.None;

            if (btnKhuVucAll != null) // Thêm kiểm tra null an toàn
            {
                btnKhuVucAll.IsChecked = true;
                UncheckOtherKhuVucButtons(btnKhuVucAll);
            }
            ApplyTableFilter(null);
        }
    }
}