using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AppCafebookApi.Converters
{
    /// <summary>
    /// Converter này chuyển giá trị null hoặc string rỗng thành Collapsed (ẩn)
    /// và giá trị không-null thành Visible (hiện).
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Nếu value là null, hoặc là string rỗng/khoảng trắng
            if (value == null || (value is string s && string.IsNullOrWhiteSpace(s)))
            {
                return Visibility.Collapsed; // Ẩn đi
            }

            return Visibility.Visible; // Hiện
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}