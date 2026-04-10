using AppCafebookApi.Services;
using CafebookModel.Model.Data;
using CafebookModel.Utils;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Linq;

namespace AppCafebookApi.View.Common
{
    public partial class WelcomeWindow : Window
    {
        private readonly NhanVienDto? _user;
        private DispatcherTimer? _timer;

        public WelcomeWindow()
        {
            InitializeComponent();
        }

        public WelcomeWindow(NhanVienDto user)
        {
            InitializeComponent();
            _user = user;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_user != null)
            {
                txtUserGreeting.Text = $"Xin chào, {_user.HoTen}";

                // 1. Tải ảnh bằng HinhAnhHelper
                BitmapImage avatar = HinhAnhHelper.LoadImage(
                    _user.AnhDaiDien ?? string.Empty,
                    HinhAnhPaths.DefaultAvatar
                );

                // 2. GÁN ẢNH VÀO UI (ĐÃ SỬA)
                // Gán trực tiếp vào Ellipse 'imgAvatar' đã thêm trong XAML
                if (avatar != null)
                {
                    imgAvatar.Fill = new ImageBrush(avatar)
                    {
                        Stretch = Stretch.UniformToFill
                    };
                }
            }

            // Cấu hình timer
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2500)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _timer?.Stop();
            this.Close();
        }
    }
}