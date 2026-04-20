using Supabase;
using TaskFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskFlow.Core.Services
{
    public class SupabaseService
    {
        private const string StatusNew = "Новая";
        private const string StatusCompleted = "Выполнено";
        private const string StatusOverdue = "Просрочено";

        private readonly Client _client;
        private bool _isInitialized;

        private const string SupabaseUrl = "https://bycusssvkhqtndugmrlv.supabase.co";
        private const string SupabaseKey = "sb_publishable_3rWpTadtQciSBsLGMR9aGQ_ABEQNOjr";

        public SupabaseService()
        {
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };

            _client = new Client(SupabaseUrl, SupabaseKey, options);
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _client.InitializeAsync();
            _isInitialized = true;
        }

        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            await InitializeAsync();
            await CheckAndUpdateOverdueTasksAsync();

            var tasks = await _client
                .From<TaskItem>()
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var taskList = tasks.Models ?? new List<TaskItem>();
            await EnrichTasksWithUserNamesAsync(taskList);

            return taskList;
        }

        public async Task<List<TaskItem>> GetTasksByResponsibleAsync(Guid responsibleId)
        {
            await InitializeAsync();
            await CheckAndUpdateOverdueTasksAsync();

            var tasks = await _client
                .From<TaskItem>()
                .Filter("responsible_id", Supabase.Postgrest.Constants.Operator.Equals, responsibleId.ToString())
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var taskList = tasks.Models ?? new List<TaskItem>();
            await EnrichTasksWithUserNamesAsync(taskList);

            return taskList;
        }

        public async Task<bool> CreateTaskAsync(TaskDraft draft)
        {
            await InitializeAsync();

            try
            {
                var insertModel = new TaskInsertModel
                {
                    Title = draft.Title,
                    Description = draft.Description,
                    ResponsibleId = draft.ResponsibleId,
                    AssignedById = CurrentUser.User?.Id ?? Guid.Empty,
                    DueDate = draft.DueDate,
                    Status = ResolveActualStatus(string.IsNullOrWhiteSpace(draft.Status) ? StatusNew : draft.Status, draft.DueDate),
                    Priority = string.IsNullOrWhiteSpace(draft.Priority) ? "Средний" : draft.Priority,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                var response = await _client
                    .From<TaskInsertModel>()
                    .Insert(insertModel);

                return response.ResponseMessage?.IsSuccessStatusCode == true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Не удалось создать задачу в Supabase.", ex);
            }
        }

        /// <summary>
        /// Обновление статуса задачи (самый безопасный способ для твоей версии Supabase)
        /// </summary>
        public async Task<bool> UpdateTaskStatusAsync(TaskItem task, string newStatus)
        {
            await InitializeAsync();

            try
            {
                var updateObject = new TaskStatusUpdate
                {
                    Status = ResolveActualStatus(newStatus, task.DueDate),
                    UpdatedAt = DateTime.Now
                };

                var response = await _client
                    .From<TaskStatusUpdate>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, task.Id.ToString())
                    .Update(updateObject);

                return response.ResponseMessage?.IsSuccessStatusCode == true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Не удалось обновить статус задачи.", ex);
            }
        }

        /// <summary>
        /// Проверка и обновление просроченных задач
        /// </summary>
        public async Task<int> CheckAndUpdateOverdueTasksAsync()
        {
            await InitializeAsync();

            var tasksResponse = await _client
                .From<TaskItem>()
                .Get();

            var tasks = tasksResponse.Models ?? new List<TaskItem>();
            var updatedCount = 0;

            foreach (var task in tasks.Where(ShouldMarkAsOverdue))
            {
                var updateObject = new TaskStatusUpdate
                {
                    Status = StatusOverdue,
                    UpdatedAt = DateTime.Now
                };

                var response = await _client
                    .From<TaskStatusUpdate>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, task.Id.ToString())
                    .Update(updateObject);

                if (response.ResponseMessage?.IsSuccessStatusCode == true)
                {
                    task.Status = StatusOverdue;
                    updatedCount++;
                }
            }

            Console.WriteLine($"✅ Проверка просроченных задач завершена. Обновлено: {updatedCount}");
            return updatedCount;
        }

        // ====================== Остальные методы без изменений ======================

        public async Task<List<User>> GetAllUsersAsync()
        {
            await InitializeAsync();

            var response = await _client
                .From<User>()
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            return response.Models ?? new List<User>();
        }

        public async Task<bool> CreateUserAsync(UserUpsertModel user)
        {
            await InitializeAsync();

            try
            {
                user.CreatedAt = DateTime.Now;
                var response = await _client
                    .From<UserUpsertModel>()
                    .Insert(user);

                return response.ResponseMessage?.IsSuccessStatusCode == true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Не удалось добавить пользователя.", ex);
            }
        }

        public async Task<bool> UpdateUserAsync(UserUpsertModel user)
        {
            await InitializeAsync();

            try
            {
                var response = await _client
                    .From<UserUpsertModel>()
                    .Update(user);

                return response.ResponseMessage?.IsSuccessStatusCode == true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Не удалось обновить пользователя.", ex);
            }
        }

        public async Task<bool> SetUserBlockedAsync(User user, bool isBlocked)
        {
            await InitializeAsync();

            try
            {
                if (isBlocked && user.IsProtectedAdministrator)
                {
                    throw new InvalidOperationException("Администратора (Председателя) нельзя заблокировать.");
                }

                var model = ToUpsertModel(user);
                model.IsBlocked = isBlocked;

                var response = await _client
                    .From<UserUpsertModel>()
                    .Update(model);

                return response.ResponseMessage?.IsSuccessStatusCode == true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Не удалось изменить статус блокировки. Для этой функции нужна колонка 'is_blocked' в таблице users.",
                    ex);
            }
        }

        public UserUpsertModel ToUpsertModel(User user)
        {
            return new UserUpsertModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Role = user.Role,
                Email = user.Email,
                Phone = user.Phone,
                Password = user.Password,
                Login = user.Login,
                IsBlocked = user.IsBlocked,
                CreatedAt = user.CreatedAt == default ? DateTime.Now : user.CreatedAt
            };
        }

        private async Task EnrichTasksWithUserNamesAsync(List<TaskItem> tasks)
        {
            if (tasks.Count == 0) return;

            var userIds = tasks
                .Where(t => t.ResponsibleId.HasValue || t.AssignedById.HasValue)
                .SelectMany(t => new[] { t.ResponsibleId, t.AssignedById })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (userIds.Count == 0) return;

            var usersResponse = await _client
                .From<User>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.In, userIds.Select(id => id.ToString()).ToArray())
                .Get();

            var users = usersResponse.Models ?? new List<User>();
            var usersDict = users.ToDictionary(u => u.Id, u => u.FullName);

            foreach (var task in tasks)
            {
                task.ResponsibleName = task.ResponsibleId.HasValue && usersDict.TryGetValue(task.ResponsibleId.Value, out var responsibleName)
                    ? responsibleName
                    : "Не назначен";

                task.AssignedByName = task.AssignedById.HasValue && usersDict.TryGetValue(task.AssignedById.Value, out var assignedByName)
                    ? assignedByName
                    : "Система";
            }
        }

        private static bool ShouldMarkAsOverdue(TaskItem task)
        {
            return task.DueDate.HasValue &&
                   task.DueDate.Value.Date < DateTime.Today &&
                   !string.Equals(task.Status, StatusCompleted, StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(task.Status, StatusOverdue, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveActualStatus(string requestedStatus, DateTime? dueDate)
        {
            if (string.Equals(requestedStatus, StatusCompleted, StringComparison.OrdinalIgnoreCase))
            {
                return StatusCompleted;
            }

            if (dueDate.HasValue && dueDate.Value.Date < DateTime.Today)
            {
                return StatusOverdue;
            }

            return requestedStatus;
        }
    }

    // ====================== Вспомогательная модель для обновления статуса ======================
    [Supabase.Postgrest.Attributes.Table("tasks")]
    public class TaskStatusUpdate : Supabase.Postgrest.Models.BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Supabase.Postgrest.Attributes.Column("status")]
        public string Status { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
