using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using CafebookModel.Model.ModelApp;
using System.Windows.Threading;
using System.Windows.Media;

namespace AppCafebookApi.View.common
{
    // Lớp nội bộ để hỗ trợ grouping
    public class CaiDatViewItem : CaiDatDto
    {
        public string Nhom { get; set; } = "Cài Đặt Khác";
    }

    public partial class CaiDatWindow : Page
    {
        private static readonly HttpClient httpClient;
        private ObservableCollection<CaiDatViewItem> settingsList = new ObservableCollection<CaiDatViewItem>();
        private DispatcherTimer notificationTimer;

        static CaiDatWindow()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public CaiDatWindow()
        {
            InitializeComponent();
            notificationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            notificationTimer.Tick += NotificationTimer_Tick;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.GetFromJsonAsync<List<CaiDatDto>>("api/app/caidat/all");

                if (response != null)
                {
                    // 1. CHUYỂN DTO THÀNH VIEWITEM VÀ PHÂN NHÓM
                    var viewItems = response
                        .Where(s => !s.TenCaiDat.StartsWith("HR_")) // Lọc bỏ các cài đặt HR
                        .OrderBy(s => GetNhomOrder(s.TenCaiDat)) // Sắp xếp theo thứ tự logic
                        .ThenBy(s => s.TenCaiDat) // Sắp xếp theo tên
                        .Select(dto => new CaiDatViewItem
                        {
                            TenCaiDat = dto.TenCaiDat,
                            GiaTri = dto.GiaTri,
                            MoTa = dto.MoTa,
                            Nhom = GetNhom(dto.TenCaiDat) // Hàm phân nhóm
                        });

                    settingsList = new ObservableCollection<CaiDatViewItem>(viewItems);

                    // 2. TẠO COLLECTIONVIEW ĐỂ GROUPING
                    var cvs = new CollectionViewSource();
                    cvs.Source = settingsList;
                    cvs.GroupDescriptions.Add(new PropertyGroupDescription("Nhom"));

                    lvSettings.ItemsSource = cvs.View;
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"Không thể tải cài đặt: {ex.Message}", isError: true);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// CẬP NHẬT: Hàm helper để phân loại cài đặt theo nhóm
        /// </summary>
        private string GetNhom(string tenCaiDat)
        {
            if (tenCaiDat == "TenQuan" || tenCaiDat == "DiaChi" || tenCaiDat == "SoDienThoai" || tenCaiDat == "Wifi_MatKhau" || tenCaiDat == "Wifi_Ten")
            {
                return "1. Thông tin Chung (In Hóa đơn)";
            }
            if (tenCaiDat == "GioiThieu" || tenCaiDat == "LienHe_GioMoCua")
            {
                return "2. Cài đặt Thông Tin Quán (Web)";
            }
            if (tenCaiDat.StartsWith("LienHe_") && tenCaiDat != "LienHe_GioMoCua")
            {
                return "3. Cài Đặt Mạng Xã Hội (Web)";
            }
            if (tenCaiDat.StartsWith("Sach_"))
            {
                return "4. Cài đặt Thuê Sách";
            }
            if (tenCaiDat.StartsWith("DiemTichLuy_"))
            {
                return "5. Cài đặt Điểm Tích Lũy";
            }
            if (tenCaiDat.StartsWith("AI_Chat_"))
            {
                return "6. Cài đặt AI (Nâng cao)";
            }
            return "7. Cài Đặt Khác";
        }

        // CẬP NHẬT: Hàm helper để sắp xếp nhóm theo thứ tự
        private int GetNhomOrder(string tenCaiDat)
        {
            if (tenCaiDat == "TenQuan" || tenCaiDat == "DiaChi" || tenCaiDat == "SoDienThoai" || tenCaiDat == "Wifi_MatKhau" || tenCaiDat == "Wifi_Ten") return 1;
            if (tenCaiDat == "GioiThieu" || tenCaiDat == "LienHe_GioMoCua") return 2;
            if (tenCaiDat.StartsWith("LienHe_") && tenCaiDat != "LienHe_GioMoCua") return 3;
            if (tenCaiDat.StartsWith("Sach_")) return 4;
            if (tenCaiDat.StartsWith("DiemTichLuy_")) return 5;
            if (tenCaiDat.StartsWith("AI_Chat_")) return 6;
            return 7;
        }

        private async void BtnSaveRow_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var itemToSave = button.Tag as CaiDatViewItem;
            if (itemToSave == null) return;

            button.IsEnabled = false;
            button.Content = "Đang lưu...";

            try
            {
                var dto = new CaiDatDto
                {
                    TenCaiDat = itemToSave.TenCaiDat,
                    GiaTri = itemToSave.GiaTri,
                    MoTa = itemToSave.MoTa
                };

                var response = await httpClient.PutAsJsonAsync("api/app/caidat/update-single", dto);

                if (response.IsSuccessStatusCode)
                {
                    ShowNotification($"Đã lưu '{itemToSave.MoTa}' thành công!");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ShowNotification($"Lỗi khi lưu: {error}", isError: true);
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"Không thể lưu: {ex.Message}", isError: true);
            }
            finally
            {
                button.IsEnabled = true;
                button.Content = "Lưu";
            }
        }

        private void ShowNotification(string message, bool isError = false)
        {
            notificationTimer.Stop();
            NotificationText.Text = message;
            if (isError)
            {
                NotificationBorder.Background = (SolidColorBrush)FindResource("ErrorBrush");
            }
            else
            {
                NotificationBorder.Background = (SolidColorBrush)FindResource("SuccessBrush");
            }
            NotificationBorder.Visibility = Visibility.Visible;
            notificationTimer.Start();
        }

        private void NotificationTimer_Tick(object? sender, EventArgs e)
        {
            notificationTimer.Stop();
            NotificationBorder.Visibility = Visibility.Collapsed;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}