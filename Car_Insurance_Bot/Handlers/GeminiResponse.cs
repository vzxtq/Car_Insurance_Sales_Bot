using System.Text;
using System.Text.Json;
using Car_Insurance_Bot.Models;
using Microsoft.Extensions.Configuration;

namespace Car_Insurance_Bot.Handlers
{  
    public class GeminiHandler
    {
        private readonly string _geminiApiKey;
        private readonly HttpClient _httpClient;

        public GeminiHandler(IConfiguration configuration)
        {
            _geminiApiKey = configuration["GeminiAi:ApiKey"] ?? throw new ArgumentNullException("GeminiAi:ApiKey", "API key cannot be null.");
            _httpClient = new HttpClient();
        }

        public async Task<string> SendToGeminiAsync(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_geminiApiKey}";

            var payload = new
            {
                contents = new[]
                {
                    new {
                        parts = new[]
                        {
                            new { text = "You are a Telegram bot whose job is to sell car insurance. Always respond briefly and in a helpful but persuasive tone. If the user is unsure, explain why car insurance is important. Be confident and proactive - your goal is to convince the user to proceed with the insurance."},
                            new { text = prompt }
                        }
                    }
                }
            };

            using var client = new HttpClient();
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
                return $"AI request failed. Status: {response.StatusCode}";

            var responseJson = await response.Content.ReadAsStringAsync();

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson, options);

                var text = geminiResponse?.Candidates?
                    .FirstOrDefault()?.Content?
                    .Parts?.FirstOrDefault()?.Text;

                return string.IsNullOrWhiteSpace(text) ? "AI returned empty response." : text;
            }
            catch (Exception ex)
            {
                Console.WriteLine("JSON Parse Error: " + ex.Message);
                Console.WriteLine("Full Response: " + responseJson);
                return "AI response parse error.";
            }
        }
    }
}