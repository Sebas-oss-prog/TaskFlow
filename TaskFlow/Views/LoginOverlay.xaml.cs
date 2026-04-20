using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TaskFlow.Core.Models;
using TaskFlow.Core.Services;

namespace TaskFlow.Views
{
    public partial class LoginOverlay : UserControl
    {
        public event EventHandler<User>? LoginSuccessful;

        private readonly SupabaseService _supabaseService = new SupabaseService();

        public LoginOverlay()
        {
            InitializeComponent();
            Loaded += (_, _) => txtLogin.Focus();
        }

        public void LoadUsers(System.Collections.Generic.List<User> users)
        {
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var loginButton = sender as Button;
            var login = txtLogin.Text.Trim();
            var password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (loginButton is not null)
                {
                    loginButton.IsEnabled = false;
                }

                var users = await _supabaseService.GetAllUsersAsync();

                var user = users.FirstOrDefault(u =>
                    u.Login.Equals(login, StringComparison.OrdinalIgnoreCase) &&
                    u.Password == password);

                if (user is null)
                {
                    MessageBox.Show("Неверный логин или пароль.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (user.IsBlocked)
                {
                    if (user.IsProtectedAdministrator)
                    {
                        await _supabaseService.SetUserBlockedAsync(user, false);
                        user.IsBlocked = false;
                        MessageBox.Show("Администратора (Председателя) нельзя заблокировать.", "Доступ восстановлен", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Этот пользователь заблокирован. Обратитесь к администратору.", "Вход запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                CurrentUser.Login(user);
                LoginSuccessful?.Invoke(this, user);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (loginButton is not null)
                {
                    loginButton.IsEnabled = true;
                }
            }
        }
    }
}

