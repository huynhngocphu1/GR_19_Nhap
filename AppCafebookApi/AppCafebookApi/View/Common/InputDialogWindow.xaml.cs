using System.Windows;

namespace AppCafebookApi.View.common
{
    public partial class InputDialogWindow : Window
    {
        public string InputText { get; private set; } = string.Empty;

        public InputDialogWindow(string title, string prompt)
        {
            InitializeComponent();
            this.Title = title;
            lblPrompt.Text = prompt;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.InputText = txtInput.Text;
            this.DialogResult = true;
            this.Close();
        }
    }
}