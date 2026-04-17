using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.ViewModels
{
    public partial class TaskEditorViewModel : ObservableObject
    {
        private readonly SupabaseService _supabaseService;

        public ObservableCollection<User> ResponsibleUsers { get; } = new();
        public ObservableCollection<string> Priorities { get; } = new(new[] { "Низкий", "Средний", "Высокий" });

        public event Action<bool?>? CloseRequested;
        public event Action<string, string>? NotificationRequested;

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string description = string.Empty;

        [ObservableProperty]
        private User? selectedResponsible;

        [ObservableProperty]
        private DateTime? dueDate = DateTime.Today.AddDays(3);

        [ObservableProperty]
        private string selectedPriority = "Средний";

        [ObservableProperty]
        private bool isBusy;

        public TaskEditorViewModel(SupabaseService supabaseService, TaskDraft? draft = null)
        {
            _supabaseService = supabaseService;

            if (draft is not null)
            {
                Title = draft.Title;
                Description = draft.Description;
                DueDate = draft.DueDate;
                SelectedPriority = string.IsNullOrWhiteSpace(draft.Priority) ? "Средний" : draft.Priority;
            }
        }

        public async Task InitializeAsync(Guid? responsibleId = null)
        {
            try
            {
                IsBusy = true;
                var users = await _supabaseService.GetAllUsersAsync();
                ResponsibleUsers.Clear();

                foreach (var user in users.Where(u => !u.IsBlocked))
                {
                    ResponsibleUsers.Add(user);
                }

                SelectedResponsible = ResponsibleUsers.FirstOrDefault(u => u.Id == responsibleId)
                    ?? ResponsibleUsers.FirstOrDefault();
            }
            catch (Exception ex)
            {
                NotificationRequested?.Invoke("Ошибка", $"Не удалось загрузить сотрудников: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                NotificationRequested?.Invoke("Проверка", "Введите название задачи.");
                return;
            }

            if (SelectedResponsible is null)
            {
                NotificationRequested?.Invoke("Проверка", "Выберите ответственного сотрудника.");
                return;
            }

            try
            {
                IsBusy = true;

                var success = await _supabaseService.CreateTaskAsync(new TaskDraft
                {
                    Title = Title.Trim(),
                    Description = Description.Trim(),
                    ResponsibleId = SelectedResponsible.Id,
                    DueDate = DueDate,
                    Priority = SelectedPriority,
                    Status = "Новая"
                });

                if (!success)
                {
                    NotificationRequested?.Invoke("Ошибка", "Supabase не подтвердил создание задачи.");
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
