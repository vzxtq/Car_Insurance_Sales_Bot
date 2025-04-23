using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Car_Insurance_Bot.Services;
using System.Collections.Concurrent;
using Car_Insurance_Bot.Utils;

namespace Car_Insurance_Bot.Handlers
{
    public class FileMessageHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly TesseractService _tesseractService;
        private readonly string _botToken;
        private readonly ConcurrentDictionary<long, (string Name, string Passport)> _userData;
        private readonly ConcurrentDictionary<long, string> _userState;

        public FileMessageHandler(
            ITelegramBotClient botClient,
            TesseractService tesseractService,
            string botToken,
            ConcurrentDictionary<long, (string Name, string Passport)> userData,
            ConcurrentDictionary<long, string> userState)
        {
            _botClient = botClient;
            _tesseractService = tesseractService;
            _botToken = botToken;
            _userData = userData;
            _userState = userState;
        }

        public async Task HandleAsync(Message message, long chatId)
        {
            if (!_userState.ContainsKey(chatId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Please type /start to begin the process.");
                return;
            }

            if (message.Document == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "No document found. Please send a valid file.");
                return;
            }

            var mimeType = message.Document.MimeType?.ToLower();
            var fileName = message.Document.FileName?.ToLower();
            var isImage = mimeType?.StartsWith("image/") == true || fileName?.EndsWith(".jpg") == true || fileName.EndsWith(".jpeg") || fileName.EndsWith(".png");

            if (!isImage)
            {
                await _botClient.SendTextMessageAsync(chatId, "Unsupported file type. Please send a photo (JPG, JPEG, PNG) of your passport.");
                return;
            }

            if (_userState.TryGetValue(chatId, out var state) && state == "awaiting_confirm")
            {
                await _botClient.SendTextMessageAsync(chatId, "Please confirm the previous document before sending a new one.");
                return;
            }

            var fileId = message.Document.FileId;
            var file = await _botClient.GetFileAsync(fileId);
            var filePath = file.FilePath;
            var fileUrl = $"https://api.telegram.org/file/bot{_botToken}/{filePath}";
            var downloadedFilePath = await DownloadFileAsync(fileUrl);

            await _botClient.SendTextMessageAsync(chatId, "Document received. Processing...");

            var (name, passport) = await _tesseractService.ParsePassport(downloadedFilePath);

            _userData[chatId] = (name, passport);
            _userState[chatId] = "awaiting_confirm";

            string extractedInfo = $"Name: {name}\nPassport: {passport}";

            var confirmKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Yes", "confirm_yes"),
                    InlineKeyboardButton.WithCallbackData("No", "confirm_no")
                }
            });

            await _botClient.SendTextMessageAsync(chatId,
                $"Extracted data:\n{extractedInfo}\n\nIs this correct?",
                replyMarkup: confirmKeyboard);
        }

        private async Task<string> DownloadFileAsync(string fileUrl)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(fileUrl);
            if (response.IsSuccessStatusCode)
            {
                var fileCont = await response.Content.ReadAsByteArrayAsync();
                var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");
                await System.IO.File.WriteAllBytesAsync(filePath, fileCont);
                return filePath;
            }

            throw new Exception("Failed to download file.");
        }
    }
}
