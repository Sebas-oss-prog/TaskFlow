using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using TaskFlow.Core.Models;
using TaskFlow.Core.Services;
using Xunit;

namespace TaskFlow.Tests;

public class OllamaServiceTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly OllamaService _service;

    public OllamaServiceTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object) { Timeout = TimeSpan.FromSeconds(60) };
        _service = new OllamaService(); // твой реальный конструктор
    }

    [Fact]
    public async Task GetResponseAsync_ReturnsAiResponse_WhenOllamaWorks()
    {
        // Arrange
        var user = new User { FullName = "Иван Иванов", Role = "Сотрудник" };
        const string expectedText = "Хорошо, сделаем задачу с приоритетом Высокий.";

        var fakeResponse = new
        {
            message = new { content = expectedText }
        };

        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(fakeResponse), Encoding.UTF8, "application/json")
        };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _service.GetResponseAsync("Сделай задачу по отчёту", user);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("задачу", result);
    }

    [Fact]
    public async Task GetResponseAsync_ReturnsErrorMessage_WhenOllamaNotRunning()
    {
        // Act
        var result = await _service.GetResponseAsync("Тест", null);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Ошибка подключения к Ollama", result);
    }
}