using System.Windows;
using TaskFlow.ViewModels;

namespace TaskFlow.Views
{
    public partial class UserEditorWindow : Window
    {
        public UserEditorWindow(UserEditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Title = viewModel.WindowTitle;

            viewModel.CloseRequested += result =>
            {
                DialogResult = result;
                Close();
            };

            viewModel.NotificationRequested += (title, message) =>
                MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
