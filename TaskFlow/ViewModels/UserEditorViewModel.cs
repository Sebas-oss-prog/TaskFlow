using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TaskFlow.Core.Models;
using TaskFlow.Core.Services;

namespace TaskFlow.ViewModels
{
    public partial class UserEditorViewModel : ObservableObject
    {
        private readonly SupabaseService _supabaseService;
        private readonly User? _sourceUser;

        public ObservableCollection<string> RoleOptions { get; } = new(new[] { "Пользователь", "Администратор", "Председатель" });

        public event Action<bool?>? CloseRequested;
        public event Action<string, string>? NotificationRequested;

        public bool IsEditMode => _sourceUser is not null;
        public string WindowTitle => IsEditMode ? "Редактирование пользователя" : "Новый пользователь";

        [ObservableProperty]
        private string fullName = string.Empty;

        [ObservableProperty]
        private string role = "Пользователь";

        [ObservableProperty]
        private string login = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string? email;

        [ObservableProperty]
        private string? phone;

        [ObservableProperty]
        private bool isBlocked;

        [ObservableProperty]
        private bool isBusy;

        public UserEditorViewModel(SupabaseService supabaseService, User? user = null)
        {
            _supabaseService = supabaseService;
            _sourceUser = user;

            if (user is not null)
            {
                FullName = user.FullName;
                Role = string.IsNullOrWhiteSpace(user.Role) ? "Пользователь" : user.Role;
                Login = user.Login;
                Password = user.Password;
                Email = user.Email;
                Phone = user.Phone;
                IsBlocked = user.IsBlocked;
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Role) || string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
            {
                NotificationRequested?.Invoke("Проверка", "Заполните ФИО, роль, логин и пароль.");
                return;
            }

            try
            {
                IsBusy = true;

                var model = new UserUpsertModel
                {
                    Id = _sourceUser?.Id ?? Guid.NewGuid(),
                    FullName = FullName.Trim(),
                    Role = Role.Trim(),
                    Login = Login.Trim(),
                    Password = Password.Trim(),
                    Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                    Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                    IsBlocked = IsBlocked,
                    CreatedAt = _sourceUser?.CreatedAt ?? DateTime.Now
                };

                var success = IsEditMode
                    ? await _supabaseService.UpdateUserAsync(model)
                    : await _supabaseService.CreateUserAsync(model);

                if (!success)
                {
                    NotificationRequested?.Invoke("Ошибка", "Supabase не подтвердил сохранение пользователя.");
                    return;
                }

                CloseRequested?.Invoke(true);
            }
            catch (Exception ex)
            {
                NotificationRequested?.Invoke("Ошибка", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseRequested?.Invoke(false);
        }
    }
}
