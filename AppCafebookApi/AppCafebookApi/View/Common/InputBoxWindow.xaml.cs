// Tập tin: AppCafebookApi/View/Common/InputBoxWindow.xaml.cs (ĐÃ SỬA)
using System.Windows;

namespace AppCafebookApi.View.Common // (Hoặc .common tùy theo thư mục của bạn)
{
    public partial class InputBoxWindow : Window
    {
        public string InputText { get; private set; }

        public InputBoxWindow(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();
            this.Title = title;
            lblPrompt.Text = prompt;
            txtInput.Text = defaultValue;
            InputText = defaultValue;
        }

        // SỬA: Đã xóa chữ 'D' bị thừa
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtInput.Focus();
            txtInput.SelectAll();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            InputText = txtInput.Text;
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}