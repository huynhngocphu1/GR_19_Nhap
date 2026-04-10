// Tập tin: AppCafebookApi/Services/HinhAnhHelper.cs
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
// KHÔNG CẦN using System.Windows; nữa

namespace AppCafebookApi.Services
{
    public static class HinhAnhHelper
    {
        /// <summary>
        /// SỬA ĐỔI: Tải ảnh từ URL Web hoặc Đường dẫn File cục bộ.
        /// </summary>
        public static BitmapImage LoadImage(string? imageSource, string defaultImagePath)
        {
            if (string.IsNullOrEmpty(imageSource))
            {
                // 1. Không có nguồn -> Tải ảnh mặc định (pack://)
                return LoadImageFromPackUri(defaultImagePath);
            }

            // 2. Nguồn là URL (http://...)
            if (imageSource.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(imageSource, UriKind.Absolute);
                    image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();

                    // SỬA: ĐÃ XÓA DÒNG image.Freeze();

                    return image;
                }
                catch (Exception) // SỬA: Xóa MessageBox, chỉ trả về ảnh mặc định
                {
                    return LoadImageFromPackUri(defaultImagePath); // Lỗi URL -> Tải mặc định
                }
            }

            // 3. (SỬA) Nguồn là một đường dẫn file cục bộ (Dùng cho Preview)
            if (File.Exists(imageSource))
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(imageSource, UriKind.Absolute);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    // SỬA: ĐÃ XÓA DÒNG image.Freeze();
                    return image;
                }
                catch
                {
                    return LoadImageFromPackUri(defaultImagePath);
                }
            }

            return LoadImageFromPackUri(defaultImagePath);
        }

        /// <summary>
        /// (Giữ nguyên) Hàm tải ảnh từ Resource (pack://)
        /// </summary>
        private static BitmapImage LoadImageFromPackUri(string uriPath)
        {
            try
            {
                var image = new BitmapImage();
                var uri = new Uri($"pack://application:,,,/AppCafebookApi;component{uriPath}", UriKind.Absolute);

                image.BeginInit();
                image.UriSource = uri;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze(); // Freeze ở đây thì an toàn, vì file nằm sẵn trong app
                return image;
            }
            catch (Exception)
            {
                // Tạo ảnh 1x1 trong suốt nếu resource bị lỗi
                var image = new BitmapImage();
                var writeableBitmap = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Pbgra32, null);
                using (var stream = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                    encoder.Save(stream);
                    stream.Position = 0;
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                }
                image.Freeze();
                return image;
            }
        }
    }
}