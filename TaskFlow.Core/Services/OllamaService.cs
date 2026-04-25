using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TaskFlow.Core.Models;

namespace TaskFlow.Core.Services
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private const string OLLAMA_URL = "http://localhost:11434/api/chat";

        public OllamaService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        }

        public async Task<string> GetResponseAsync(string userMessage, User? currentUser)
        {
            try
            {
                var systemPrompt = $@"
Ты — полезный AI-помощник по управлению задачами в Слободском РайПО.

Твоя роль:
- Помогать сотрудникам и руководству формулировать чёткие, понятные задачи.
- Делать предложения по улучшению формулировок.
- Давать советы по планированию и приоритизации.
- Отвечать вежливо, кратко и по делу.

Не пытайся сам создавать задачи в системе. 
Только помогай пользователю формулировать их.

Текущий пользователь: {currentUser?.FullName} ({currentUser?.Role})
";

                var requestBody = new
                {
                    model = "phi4-mini",        // или llama3.2:3b, если лучше работает
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userMessage }
                    },
                    stream = false,
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(OLLAMA_URL, content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseString);
                var aiText = doc.RootElement
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return aiText ?? "Извините, я не смог обработать запрос.";
            }
            catch (Exception ex)
            {
                return $"Ошибка подключения к Ollama:\n{ex.Message}\n\nУбедитесь, что Ollama запущен (ollama serve).";
            }
        }
    }
}
