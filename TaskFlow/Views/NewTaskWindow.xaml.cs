using System.Windows;
using TaskFlow.ViewModels;

namespace TaskFlow.Views
{
    public partial class NewTaskWindow : Window
    {
        public NewTaskWindow(TaskEditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

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
