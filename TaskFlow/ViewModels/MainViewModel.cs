using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly SupabaseService _supabaseService;
        private readonly OllamaService _ollamaService;

        public ObservableCollection<TaskItem> Tasks { get; } = new();
        public ObservableCollection<TaskTemplate> TaskTemplates { get; } = new();
        public ObservableCollection<ChatMessage> ChatMessages { get; } = new();
        public ObservableCollection<User> Users { get; } = new();

        public ICollectionView FilteredTasks { get; }

        public IReadOnlyList<string> StatusFilters { get; } = new[] { "Все", "Новая", "В работе", "На проверке", "Выполнено" };

        public event Func<TaskDraft?, Task<bool>>? TaskEditorRequested;
        public event Func<User?, Task<bool>>? UserEditorRequested;
        public event Action<string, string>? NotificationRequested;

        [ObservableProperty]
        private string currentUserFullName = "Гость";

        [ObservableProperty]
        private string currentUserRole = "Не авторизован";

        [ObservableProperty]
        private bool isAdmin;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string busyMessage = "Загрузка данных...";

        [ObservableProperty]
        private string selectedStatusFilter = "Все";

        [ObservableProperty]
        private string messageText = string.Empty;

        public MainViewModel()
            : this(new SupabaseService(), new OllamaService())
        {
        }

        public MainViewModel(SupabaseService supabaseService, OllamaService ollamaService)
        {
            _supabaseService = supabaseService;
            _ollamaService = ollamaService;

            FilteredTasks = CollectionViewSource.GetDefaultView(Tasks);
            FilteredTasks.Filter = FilterTask;

            LoadTemplates();
            AddWelcomeMessage();
        }

        public void ApplyCurrentUser()
        {
            CurrentUserFullName = CurrentUser.User?.FullName ?? "Гость";
            CurrentUserRole = CurrentUser.User?.Role ?? "Не авторизован";
            IsAdmin = CurrentUser.IsAdmin;
        }

        public async Task InitializeForCurrentUserAsync()
        {
            ApplyCurrentUser();
            await RefreshAllAsync();
        }

        public async Task RefreshAllAsync()
        {
            await TryRunBusyActionAsync("Обновление данных...", async () =>
            {
                await RefreshTasksAsync();

                if (IsAdmin)
                {
                    await RefreshUsersAsync();
                }
                else
                {
                    Users.Clear();
                }
            });
        }

        public async Task RefreshTasksAsync()
        {
            List<TaskItem> tasks;

            if (CurrentUser.IsAdmin)
            {
                tasks = await _supabaseService.GetAllTasksAsync();
            }
            else if (CurrentUser.User is not null)
            {
                tasks = await _supabaseService.GetTasksByResponsibleAsync(CurrentUser.User.Id);
            }
            else
            {
                tasks = new List<TaskItem>();
            }

            ReplaceCollection(Tasks, tasks);
            FilteredTasks.Refresh();
        }

        public async Task RefreshUsersAsync()
        {
            var users = await _supabaseService.GetAllUsersAsync();
            ReplaceCollection(Users, users.OrderBy(u => u.IsBlocked).ThenBy(u => u.FullName));
        }

        [RelayCommand]
        private async Task CreateTaskAsync()
        {
            if (TaskEditorRequested is null)
            {
                return;
            }

            var created = await TaskEditorRequested.Invoke(new TaskDraft
            {
                DueDate = DateTime.Today.AddDays(3)
            });

            if (created)
            {
                await RefreshTasksAsync();
                Notify("Задача создана", "Новая задача успешно сохранена.");
            }
        }

        [RelayCommand]
        private async Task ApplyTemplateAsync(TaskTemplate? template)
        {
            if (template is null || TaskEditorRequested is null)
            {
                return;
            }

            var created = await TaskEditorRequested.Invoke(new TaskDraft
            {
                Title = template.Title,
                Description = template.Description,
                Priority = template.Priority,
                DueDate = DateTime.Today.AddDays(template.DefaultDueInDays)
            });

            if (created)
            {
                await RefreshTasksAsync();
                Notify("Шаблон применён", "Задача создана на основе шаблона.");
            }
        }

        [RelayCommand]
        private async Task CycleTaskStatusAsync(TaskItem? task)
        {
            if (task is null)
            {
                return;
            }

            var nextStatus = task.Status switch
            {
                "Новая" => "В работе",
                "В работе" => "На проверке",
                "На проверке" => "Выполнено",
                _ => "Новая"
            };

            var updated = await TryRunBusyActionAsync("Обновление статуса...", async () =>
            {
                var success = await _supabaseService.UpdateTaskStatusAsync(task, nextStatus);
                if (!success)
                {
                    throw new InvalidOperationException("Supabase не подтвердил обновление статуса.");
                }
            });

            if (!updated)
            {
                return;
            }

            task.Status = nextStatus;
            FilteredTasks.Refresh();
        }

        [RelayCommand]
        private async Task SendMessageAsync()
        {
            var userMessage = MessageText.Trim();
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return;
            }

            AddChatMessage(userMessage, true);
            MessageText = string.Empty;

            var typingMessage = new ChatMessage
            {
                Text = "Думаю над ответом...",
                IsUser = false,
                IsTransient = true
            };

            ChatMessages.Add(typingMessage);

            try
            {
                var aiResponse = await _ollamaService.GetResponseAsync(userMessage, CurrentUser.User);
                ChatMessages.Remove(typingMessage);
                AddChatMessage(aiResponse, false);
            }
            catch (Exception ex)
            {
                ChatMessages.Remove(typingMessage);
                AddChatMessage($"Ошибка: {ex.Message}", false);
            }
        }

        [RelayCommand]
        private async Task CreateTaskFromChatMessageAsync(ChatMessage? message)
        {
            if (message is null || !message.CanCreateTask || TaskEditorRequested is null)
            {
                return;
            }

            var draft = BuildDraftFromAiMessage(message.Text);
            var created = await TaskEditorRequested.Invoke(draft);

            if (created)
            {
                await RefreshTasksAsync();
                Notify("Задача создана", "Ответ AI перенесён в форму создания задачи.");
            }
        }

        [RelayCommand]
        private async Task AddUserAsync()
        {
            if (UserEditorRequested is null)
            {
                return;
            }

            var saved = await UserEditorRequested.Invoke(null);
            if (saved)
            {
                await RefreshUsersAsync();
                Notify("Пользователь добавлен", "Новый пользователь сохранён в базе данных.");
            }
        }

        [RelayCommand]
        private async Task EditUserAsync(User? user)
        {
            if (user is null || UserEditorRequested is null)
            {
                return;
            }

            var saved = await UserEditorRequested.Invoke(user);
            if (saved)
            {
                await RefreshUsersAsync();
                Notify("Пользователь обновлён", "Изменения пользователя сохранены.");
            }
        }

        [RelayCommand]
        private async Task ToggleUserBlockedAsync(User? user)
        {
            if (user is null)
            {
                return;
            }

            var updated = await TryRunBusyActionAsync(user.IsBlocked ? "Разблокировка пользователя..." : "Блокировка пользователя...", async () =>
            {
                var success = await _supabaseService.SetUserBlockedAsync(user, !user.IsBlocked);
                if (!success)
                {
                    throw new InvalidOperationException("Supabase не подтвердил обновление пользователя.");
                }
            });

            if (!updated)
            {
                return;
            }

            user.IsBlocked = !user.IsBlocked;
            await RefreshUsersAsync();
            Notify("Статус обновлён", user.IsBlocked ? "Пользователь заблокирован." : "Пользователь разблокирован.");
        }

        partial void OnSelectedStatusFilterChanged(string value)
        {
            FilteredTasks.Refresh();
        }

        private bool FilterTask(object item)
        {
            if (item is not TaskItem task)
            {
                return false;
            }

            return SelectedStatusFilter == "Все" || string.Equals(task.Status, SelectedStatusFilter, StringComparison.OrdinalIgnoreCase);
        }

        private void AddWelcomeMessage()
        {
            if (ChatMessages.Count > 0)
            {
                return;
            }

            AddChatMessage("Здравствуйте! Я AI-помощник TaskFlow. Помогу сформулировать задачу, улучшить поручение и при необходимости перенести ответ прямо в форму создания задачи.", false);
        }

        private void AddChatMessage(string text, bool isUser)
        {
            ChatMessages.Add(new ChatMessage
            {
                Text = text,
                IsUser = isUser
            });
        }

        private TaskDraft BuildDraftFromAiMessage(string message)
        {
            var titleSource = message
                .Split(new[] { '.', '!', '?', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .FirstOrDefault(part => part.Length > 5)
                ?? "Новая задача по рекомендации AI";

            var normalizedTitle = titleSource.Length > 80
                ? titleSource[..80].TrimEnd() + "..."
                : titleSource;

            var priority = message.Contains("сроч", StringComparison.OrdinalIgnoreCase) || message.Contains("важн", StringComparison.OrdinalIgnoreCase)
                ? "Высокий"
                : "Средний";

            return new TaskDraft
            {
                Title = normalizedTitle,
                Description = message,
                DueDate = DateTime.Today.AddDays(3),
                Priority = priority
            };
        }

        private void LoadTemplates()
        {
            TaskTemplates.Clear();

            TaskTemplates.Add(new TaskTemplate
            {
                Title = "Еженедельная проверка остатков",
                Description = "Проверить фактические остатки по складу и магазинам, сверить с системой, зафиксировать расхождения и передать итоговый отчёт руководителю.",
                Category = "Склад",
                Priority = "Средний",
                DefaultDueInDays = 2,
                AccentColor = "#1565C0"
            });

            TaskTemplates.Add(new TaskTemplate
            {
                Title = "Подготовка отчёта для председателя",
                Description = "Собрать данные по подразделению, оформить краткий управленческий отчёт, выделить проблемы, сроки и предложения по действиям.",
                Category = "Отчётность",
                Priority = "Высокий",
                DefaultDueInDays = 1,
                AccentColor = "#6A1B9A"
            });

            TaskTemplates.Add(new TaskTemplate
            {
                Title = "Инвентаризация производственного участка",
                Description = "Провести инвентаризацию на участке, проверить остатки, оборудование и материалы, затем подготовить ведомость и список отклонений.",
                Category = "Производство",
                Priority = "Высокий",
                DefaultDueInDays = 4,
                AccentColor = "#EF6C00"
            });

            TaskTemplates.Add(new TaskTemplate
            {
                Title = "Контроль исполнения поручений",
                Description = "Проверить статус ранее выданных поручений, отметить завершённые задачи, запросить уточнения по просроченным и подготовить краткую сводку.",
                Category = "Контроль",
                Priority = "Средний",
                DefaultDueInDays = 3,
                AccentColor = "#2E7D32"
            });
        }

        private async Task<bool> TryRunBusyActionAsync(string message, Func<Task> action)
        {
            try
            {
                BusyMessage = message;
                IsBusy = true;
                await action();
                return true;
            }
            catch (Exception ex)
            {
                Notify("Ошибка", ex.Message);
                return false;
            }
            finally
            {
                IsBusy = false;
                BusyMessage = "Загрузка данных...";
            }
        }

        private void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
        {
            target.Clear();
            foreach (var item in items)
            {
                target.Add(item);
            }
        }

        private void Notify(string title, string message)
        {
            NotificationRequested?.Invoke(title, message);
        }
    }
}
