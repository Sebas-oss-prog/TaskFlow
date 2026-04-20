using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TaskFlow.Core.Models;
using TaskFlow.Core.Services;
using TaskFlow.ViewModels;
using TaskFlow.Views;

namespace TaskFlow
{
    public partial class MainWindow : Window
    {
        private readonly SupabaseService _supabaseService;
        private readonly MainViewModel _viewModel;

        private readonly MyTasksView _myTasksView;
        private readonly CalendarView _calendarView;
        private readonly AIAssistantView _aiAssistantView;
        private readonly TemplatesView _templatesView;

        private LoginOverlay? _loginOverlay;

        public MainWindow()
        {
            InitializeComponent();

            _supabaseService = new SupabaseService();
            _viewModel = new MainViewModel(_supabaseService, new OllamaService());
            _myTasksView = new MyTasksView();
            _calendarView = new CalendarView();
            _aiAssistantView = new AIAssistantView();
            _templatesView = new TemplatesView();

            DataContext = _viewModel;
            _myTasksView.DataContext = _viewModel;
            _aiAssistantView.DataContext = _viewModel;
            _templatesView.DataContext = _viewModel;

            _viewModel.TaskEditorRequested += OpenTaskEditorAsync;
            _viewModel.UserEditorRequested += OpenUserEditorAsync;
            _viewModel.NotificationRequested += (title, message) =>
                MessageBox.Show(this, message, title, MessageBoxButton.OK, title == "Ошибка" ? MessageBoxImage.Error : MessageBoxImage.Information);

            _viewModel.Tasks.CollectionChanged += Tasks_CollectionChanged;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _supabaseService.InitializeAsync();
                await _supabaseService.CheckAndUpdateOverdueTasksAsync();
                _viewModel.ApplyCurrentUser();
                ShowLoginOverlay();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Ошибка подключения к Supabase:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Tasks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _calendarView.SetTasks(_viewModel.Tasks.ToList());
        }

        private void ShowLoginOverlay()
        {
            MainContentArea.Content = null;
            ShellGrid.IsHitTestVisible = false;

            if (_loginOverlay is null)
            {
                _loginOverlay = new LoginOverlay();
                _loginOverlay.LoginSuccessful += LoginOverlay_LoginSuccessful;
                Grid.SetRowSpan(_loginOverlay, MainGrid.RowDefinitions.Count);
                Panel.SetZIndex(_loginOverlay, 1000);
                OverlayHost.Children.Add(_loginOverlay);
            }

            OverlayHost.Visibility = Visibility.Visible;
        }

        private async void LoginOverlay_LoginSuccessful(object? sender, User loggedInUser)
        {
            if (_loginOverlay is not null)
            {
                OverlayHost.Children.Remove(_loginOverlay);
                _loginOverlay.LoginSuccessful -= LoginOverlay_LoginSuccessful;
                _loginOverlay = null;
            }

            OverlayHost.Visibility = Visibility.Collapsed;
            ShellGrid.IsHitTestVisible = true;

            await _viewModel.InitializeForCurrentUserAsync();
            _calendarView.SetTasks(_viewModel.Tasks.ToList());
            LoadPage(_myTasksView);
        }

        private void LoadPage(object page)
        {
            MainContentArea.Content = page;
        }

        private async Task<bool> OpenTaskEditorAsync(TaskDraft? draft)
        {
            var editorViewModel = new TaskEditorViewModel(_supabaseService, draft);
            var window = new NewTaskWindow(editorViewModel)
            {
                Owner = this
            };

            await editorViewModel.InitializeAsync(draft?.ResponsibleId);
            var result = window.ShowDialog();

            if (result == true)
            {
                await _viewModel.RefreshTasksAsync();
                _calendarView.SetTasks(_viewModel.Tasks.ToList());
            }

            return result == true;
        }

        private async Task<bool> OpenUserEditorAsync(User? user)
        {
            var editorViewModel = new UserEditorViewModel(_supabaseService, user);
            var window = new UserEditorWindow(editorViewModel)
            {
                Owner = this
            };

            var result = window.ShowDialog();
            if (result == true)
            {
                await _viewModel.RefreshUsersAsync();
            }

            return result == true;
        }

        private void BtnMyTasks_Click(object sender, RoutedEventArgs e)
        {
            LoadPage(_myTasksView);
        }

        private void BtnCalendar_Click(object sender, RoutedEventArgs e)
        {
            _calendarView.SetTasks(_viewModel.Tasks.ToList());
            LoadPage(_calendarView);
        }

        private void BtnAIAssistant_Click(object sender, RoutedEventArgs e)
        {
            LoadPage(_aiAssistantView);
        }

        private void BtnTemplates_Click(object sender, RoutedEventArgs e)
        {
            LoadPage(_templatesView);
        }

        private void BtnUsers_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.IsAdmin)
            {
                MessageBox.Show(this, "Окно управления пользователями доступно только администратору.", "Доступ ограничен", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new UserManagementWindow(_viewModel)
            {
                Owner = this
            };

            window.ShowDialog();
        }

        private void BtnSwitchUser_Click(object sender, RoutedEventArgs e)
        {
            CurrentUser.Logout();
            _viewModel.ApplyCurrentUser();
            ShowLoginOverlay();
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this,
                "«РайПО Задачи» — приложение для Слободского РайПО на WPF .NET 9 с Supabase, Material Design 3 и ИИ-помощником.\n\nДля блокировки пользователей в Supabase должна существовать колонка users.is_blocked с типом boolean и значением по умолчанию false.",
                "О программе",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public async Task RefreshTasksAsync()
        {
            await _supabaseService.CheckAndUpdateOverdueTasksAsync();
            await _viewModel.RefreshTasksAsync();
            _calendarView.SetTasks(_viewModel.Tasks.ToList());
        }
    }
}
