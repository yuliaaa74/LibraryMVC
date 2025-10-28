using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace LibraryMVC.Services
{
    public class TelegramNotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string? _botToken;
        private readonly string? _chatId;

        public TelegramNotificationService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            
            _botToken = configuration["Telegram:BotToken"];
            _chatId = configuration["Telegram:ChatId"];

            Console.WriteLine("--- Telegram Service Initialized ---");
            Console.WriteLine($"Bot Token Loaded: {!string.IsNullOrEmpty(_botToken)}");
            Console.WriteLine($"Chat ID Loaded: {!string.IsNullOrEmpty(_chatId)}");
            Console.WriteLine("------------------------------------");
        }

        public async Task SendMessageAsync(string message)
        {
            Console.WriteLine("\n--- Attempting to send Telegram message ---");
            if (string.IsNullOrEmpty(_botToken) || string.IsNullOrEmpty(_chatId))
            {
                Console.WriteLine("!!! ERROR: Bot Token or Chat ID is not configured. Message not sent.");
                Console.WriteLine("--- Telegram message attempt finished ---\n");
                return;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

                Console.WriteLine($"Sending to URL: {url}");
                Console.WriteLine($"Chat ID: {_chatId}");

                var payload = new
                {
                    chat_id = _chatId,
                    text = message,
                    parse_mode = "HTML"
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("SUCCESS: Telegram API returned a success status.");
                }
                else
                {
                    Console.WriteLine($"!!! ERROR: Telegram API returned an error. Status: {response.StatusCode}");
                    Console.WriteLine($"Response Body: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!! CRITICAL ERROR: An exception occurred while sending message: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("--- Telegram message attempt finished ---\n");
            }
        }
    }
}
