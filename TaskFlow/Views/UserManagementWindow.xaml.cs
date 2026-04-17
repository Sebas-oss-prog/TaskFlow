using System.Windows;
using TaskFlow.ViewModels;

namespace TaskFlow.Views
{
    public partial class UserManagementWindow : Window
    {
        public UserManagementWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
