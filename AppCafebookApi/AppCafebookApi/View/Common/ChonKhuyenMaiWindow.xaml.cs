using CafebookModel.Model.ModelApp.NhanVien;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AppCafebookApi.View.common
{
    public partial class ChonKhuyenMaiWindow : Window
    {
        private readonly int _idHoaDon;
        private readonly int? _currentSelectedId;
        private static readonly HttpClient _httpClient;
        private List<KhuyenMaiHienThiDto> _allKms = new List<KhuyenMaiHienThiDto>();

        public int? SelectedId { get; private set; }

        static ChonKhuyenMaiWindow()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5166") };
        }

        public ChonKhuyenMaiWindow(int idHoaDon, int? currentSelectedId)
        {
            InitializeComponent();
            _idHoaDon = idHoaDon;
            _currentSelectedId = currentSelectedId;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            try
            {
                var response = await _httpClient.GetAsync($"api/app/nhanvien/khuyenmai/available/{_idHoaDon}");

                if (response.IsSuccessStatusCode)
                {
                    _allKms = await response.Content.ReadFromJsonAsync<List<KhuyenMaiHienThiDto>>() ?? new List<KhuyenMaiHienThiDto>();

                    // SỬA LỖI: Gán giá trị MaKhuyenMai để tránh NullReference
                    _allKms.Insert(0, new KhuyenMaiHienThiDto
                    {
                        IdKhuyenMai = 0,
                        TenChuongTrinh = "-- Không áp dụng --",
                        MaKhuyenMai = "", // <-- THÊM DÒNG NÀY
                        DieuKienApDung = "Chọn mục này để gỡ bỏ khuyến mãi hiện tại.",
                        IsEligible = true
                    });

                    lvKhuyenMai.ItemsSource = _allKms;

                    if (_currentSelectedId.HasValue)
                    {
                        lvKhuyenMai.SelectedValue = _currentSelectedId.Value;
                    }
                    else
                    {
                        lvKhuyenMai.SelectedValue = 0;
                    }
                }
                else
                {
                    MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi tải Khuyến mãi");
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi API");
                this.Close();
            }
            this.IsEnabled = true;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lvKhuyenMai == null || _allKms == null)
            {
                return;
            }

            string filter = txtSearch.Text.ToLower().Trim();
            if (string.IsNullOrEmpty(filter) || filter == "nhập mã hoặc tên km...")
            {
                lvKhuyenMai.ItemsSource = _allKms;
                return;
            }

            // SỬA LỖI: Thêm kiểm tra null cho MaKhuyenMai (mặc dù đã fix ở trên)
            var filteredList = _allKms.Where(k =>
                k.TenChuongTrinh.ToLower().Contains(filter) ||
                (k.MaKhuyenMai ?? "").ToLower().Contains(filter) || // <-- SỬA DÒNG NÀY
                k.IdKhuyenMai == 0
            ).ToList();

            lvKhuyenMai.ItemsSource = filteredList;
        }

        private void BtnApDung_Click(object sender, RoutedEventArgs e)
        {
            if (lvKhuyenMai.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn một khuyến mãi.", "Chưa chọn");
                return;
            }

            var selectedKm = (KhuyenMaiHienThiDto)lvKhuyenMai.SelectedItem;

            // Kiểm tra IsEligible đã được style loạibỏ, nhưng đây là check logic cuối
            if (!selectedKm.IsEligible)
            {
                MessageBox.Show($"Không thể áp dụng khuyến mãi này.\nLý do: {selectedKm.IneligibilityReason}", "Không đủ điều kiện", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.SelectedId = selectedKm.IdKhuyenMai;
            this.DialogResult = true;
            this.Close();
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}