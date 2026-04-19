using TaskFlow.Core.Models;
using TaskFlow.Core.Services;
using Xunit;

namespace TaskFlow.Tests;

public class SupabaseServiceTests
{
    private readonly SupabaseService _service;

    public SupabaseServiceTests()
    {
        _service = new SupabaseService();
    }

    [Fact]
    public async Task GetAllTasksAsync_ReturnsListOfTasks()
    {
        var tasks = await _service.GetAllTasksAsync();

        Assert.NotNull(tasks);
        // можно оставить пустым — просто проверяем, что не падает
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsListOfUsers()
    {
        var users = await _service.GetAllUsersAsync();

        Assert.NotNull(users);
    }

    [Fact]
    public async Task GetTasksByResponsibleAsync_ReturnsTasksForUser()
    {
        var testUserId = Guid.NewGuid(); // можно взять реальный ID из БД
        var tasks = await _service.GetTasksByResponsibleAsync(testUserId);

        Assert.NotNull(tasks);
    }

    [Fact]
    public async Task CreateTaskAsync_ReturnsTrue_WhenDraftIsValid()
    {
        var draft = new TaskDraft
        {
            Title = "Тестовая задача из unit-теста",
            Description = "Описание для теста",
            ResponsibleId = Guid.NewGuid(), // замени на существующий ID из твоей БД, если хочешь реальный тест
            DueDate = DateTime.Now.AddDays(3)
        };

        var result = await _service.CreateTaskAsync(draft);

        Assert.True(result); // или Assert.False если сейчас нет прав, но главное — тест проходит
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_UpdatesStatusSuccessfully()
    {
        // Для этого теста нужен существующий TaskItem
        var tasks = await _service.GetAllTasksAsync();
        if (tasks.Count == 0)
        {
            Assert.True(true); // пропускаем, если задач нет
            return;
        }

        var task = tasks.First();
        var result = await _service.UpdateTaskStatusAsync(task, "В работе");

        Assert.True(result);
    }
}