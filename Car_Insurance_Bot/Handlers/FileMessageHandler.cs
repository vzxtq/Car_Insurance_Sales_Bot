using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Car_Insurance_Bot.Services;
using System.Collections.Concurrent;
using Car_Insurance_Bot.Utils;
using System.Diagnostics.CodeAnalysis;
using OpenAI.Chat;
using System.Diagnostics;

namespace Car_Insurance_Bot.Handlers
{
    public class FileMessageHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly MindeeService _mindeeService;
        private readonly MindeePassportService _mindeePassportService;
        private readonly string _botToken;
        private readonly ConcurrentDictionary<long, (string Name, string Passport)> _userPassportData;
        private readonly ConcurrentDictionary<long, string> _userVinData;
        private readonly ConcurrentDictionary<long, string> _userState;

        public FileMessageHandler(
            ITelegramBotClient botClient,
            MindeePassportService mindeePassportSerivice,
            MindeeService mindeeService,
            string botToken,
            ConcurrentDictionary<long, (string Name, string Passport)> userPassportData,
            ConcurrentDictionary<long, string> userVinData,
            ConcurrentDictionary<long, string> userState)
        {
            _botClient = botClient;
            _mindeePassportService = mindeePassportSerivice;
            _mindeeService = mindeeService;
            _botToken = botToken;
            _userPassportData = userPassportData;
            _userVinData = userVinData;
            _userState = userState;
        }

        public async Task HandleAsync(Message message, long chatId)
        {
            if (!_userState.TryGetValue(chatId, out var currentState))
            {
                await _botClient.SendTextMessageAsync(chatId, "Please type /start to begin the process");
                return;
            }

            string downloadedPath = null;

            try 
            {
                if (message.Document != null && IsImageFile(message.Document))
                {
                    var fileUrl = await _botClient.GetFileAsync(message.Document.FileId);
                    if (fileUrl.FilePath != null)
                    {
                        downloadedPath = await DownloadFileAsync(fileUrl.FilePath);
                    }
                    else
                    {
                        throw new Exception("File path is null.");
                    }
                }
                else if (message.Photo != null && message.Photo.Any())
                {
                    var photo = message.Photo.OrderByDescending(p => p.FileSize).First();
                    var fileUrl = await _botClient.GetFileAsync(photo.FileId);
                    if (fileUrl.FilePath != null)
                    {
                        downloadedPath = await DownloadFileAsync(fileUrl.FilePath);
                    }
                    else
                    {
                        throw new Exception("Photo file path is null.");
                    }
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "⚠️ Please send a valid image (JPG, PNG) or document file.");
                    return;
                }

                switch (currentState)
                {
                    case "awaiting_passport":
                        await ProcessPassportAsync(chatId, downloadedPath);
                        break;
                    
                    case "awaiting_vin":
                        await ProcessVinAsync(chatId, downloadedPath);
                        break;

                    default:
                        await _botClient.SendTextMessageAsync(chatId, "⚠️ Unexpected document. Please follow the instructions.");
                        break;
                }
            }

            finally
            {
                try {System.IO.File.Delete(downloadedPath); } 
                catch (Exception ex) {Console.WriteLine($"[ERROR] Failed to delete file: {ex.Message}");}
            }
        }

        private bool IsImageFile(Document document)
        {
            var mime = document.MimeType?.ToLower();
            var name = document.FileName?.ToLower();
            return mime?.StartsWith("image/") == true ||
                   name?.EndsWith(".jpg") == true || name.EndsWith(".jpeg") || name.EndsWith(".png");
        }

        private async Task ProcessPassportAsync (long chatId, string path)
        {
            await _botClient.SendTextMessageAsync(chatId, "🔍 Processing your Passport image... Please wait.");

            var (fullname, idNumber) = await _mindeePassportService.ProcessPassportAsync(path);
            _userPassportData[chatId] = (fullname, idNumber);

            _userState[chatId] = "awaiting_passport";

            var confirmButtons = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("Confirm", "confirm_yes_passport"),
                InlineKeyboardButton.WithCallbackData("Incorrect", "confirm_no_passport")
            });

            await _botClient.SendTextMessageAsync(chatId, $"Name: {fullname}\nId Number: {idNumber}\n\nCorrect?", replyMarkup: confirmButtons);
        }

        private async Task ProcessVinAsync(long chatId, string path)
        {
            await _botClient.SendTextMessageAsync(chatId, "🔍 Processing your Car Title image... Please wait.");

            var vin = await _mindeeService.ProcessDocumentAsync(path);
            _userVinData[chatId] = vin;

            _userState[chatId] = "awaiting_vin";

            var confirmButtons = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("Confirm", "confirm_yes_vin"),
                InlineKeyboardButton.WithCallbackData("Incorrect", "confirm_no_vin")
            });
            if (string.IsNullOrEmpty(vin))
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ VIN not found. Please send a clear image of your Car Title.");
                return;
            }

            await _botClient.SendTextMessageAsync(chatId, $"VIN: {vin}\n\nCorrect?", replyMarkup: confirmButtons);
        }

        private async Task<string> DownloadFileAsync(string filePath)
        {
            var fullUrl = $"https://api.telegram.org/file/bot{_botToken}/{filePath}";

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(fullUrl);
            if (response.IsSuccessStatusCode)
            {
                var fileCont = await response.Content.ReadAsByteArrayAsync();
                var localPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");
                await System.IO.File.WriteAllBytesAsync(localPath, fileCont);
                return localPath;
            }

            throw new Exception("Failed to download file.");
        }
    }
}
