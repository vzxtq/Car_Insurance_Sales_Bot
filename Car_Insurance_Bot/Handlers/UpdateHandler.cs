using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Car_Insurance_Bot.Services;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;

namespace Car_Insurance_Bot.Handlers
{
    public class UpdateHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly InsuranceService _insuranceService;
        private readonly string? _botToken;
        private readonly TesseractPassportService _tesseractService;
        private static readonly ConcurrentDictionary<long, (string Name, string Passport)> _userData = new();
        private static readonly ConcurrentDictionary<long, string> _userState = new();

        public UpdateHandler(IConfiguration configuration, ITelegramBotClient botClient, InsuranceService insuranceService, TesseractPassportService tesseractService)
        {
            _botClient = botClient;
            _insuranceService = insuranceService;
            _tesseractService = tesseractService;
            _botToken = configuration["Telegram:BotToken"];
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            if (update.Type == UpdateType.Message && update.Message is { } message)
            {
                var chatId = message.Chat.Id;

                if (message.Type == MessageType.Text)
                {
                    await HandleTextMessageAsync(botClient, message, chatId);
                }
                else if (message.Type == MessageType.Document)
                {
                    await HandleFileMessageAsync(botClient, message, chatId);
                }
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery is { } callbackQuery)
            {
                await HandleCallbackQueryAsync(botClient, callbackQuery, ct);
            }
        }

        private async Task HandleTextMessageAsync(ITelegramBotClient botClient, Message message, long chatId)
        {
            switch (message.Text?.ToLower())
            {
                case "/start":
                    _userState[chatId] = "started";
                    await botClient.SendTextMessageAsync(chatId,"Hello! I can help you get car insurance. Please send a photo (file) of your passport.");
                    break;

                default:
                    await botClient.SendTextMessageAsync(chatId, "I'm here to help you with insurance. Type /start to begin.");
                    break;
            }
        }

        private async Task HandleFileMessageAsync(ITelegramBotClient botClient, Message message, long chatId)
        {
            if (!_userState.ContainsKey(chatId))
            {
                await botClient.SendTextMessageAsync(chatId, "Please type /start to begin the process.");
                return;
            }

            if (message.Document == null)
            {
                await botClient.SendTextMessageAsync(chatId, "No document found. Please send a valid file.");
                return;
            }

            var mimeType = message.Document.MimeType?.ToLower();
            var fileName = message.Document.FileName?.ToLower();

            var isImage = mimeType is not null && mimeType.StartsWith("image/") || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg") || fileName.EndsWith(".png");

            if (!isImage)
            {
                await botClient.SendTextMessageAsync(chatId, "Unsupported file type. Please send a photo (JPG, JPEG, PNG) of your passport.");
                return;
            }

            if (_userState.TryGetValue(chatId, out var state) && state == "awaiting_confirm")
            {
                await botClient.SendTextMessageAsync(chatId, "Please confirm the previous document before sending a new one.");
                return;
            }

            var fileId = message.Document.FileId;
            var file = await botClient.GetFileAsync(fileId);
            var filePath = file.FilePath;
            var fileUrl = $"https://api.telegram.org/file/bot{_botToken}/{filePath}";
            var downloadedFilePath = await DownloadFileAsync(fileUrl);

            await botClient.SendTextMessageAsync(chatId, "Document received. Processing...");

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

            await botClient.SendTextMessageAsync(chatId,
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
                var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".jpg");
                await System.IO.File.WriteAllBytesAsync(filePath, fileCont);
                return filePath;
            }
            else
            {
                throw new Exception("Failed to download file.");
            }
        }

        public async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken ct)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var data = callbackQuery.Data;

            switch (data)
            {
                case "confirm_yes":
                    _userState[chatId] = "confirmed";
                    await botClient.SendTextMessageAsync(chatId, "Data confirmed.");
                    await PromptPriceConfirmationAsync(botClient, chatId);
                    break;

                case "confirm_no":
                    _userState[chatId] = "confirmed";
                    _userData.TryRemove(chatId, out _);
                    await botClient.SendTextMessageAsync(chatId, "Please send another photo (file) of your passport.");
                    break;

                case "agree_price":
                    await botClient.SendTextMessageAsync(chatId, "Thank you! Generating your insurance policy...");
                    await Task.Delay(1000);
                    await SendGeneratedPolicyAsync(botClient, chatId);
                    break;

                case "disagree_price":
                    await ShowFinalChanceButtonsAsync(botClient, chatId);
                    break;

                case "final_agree":
                    await botClient.SendTextMessageAsync(chatId, "Glad you reconsidered! Generating your policy...");
                    await Task.Delay(1000);
                    await SendGeneratedPolicyAsync(botClient, chatId);
                    break;

                case "final_disagree":
                    await botClient.SendTextMessageAsync(chatId, "Thank you for your time. If you change your mind, type /start again. Goodbye.");
                    break;

                default:
                    await botClient.SendTextMessageAsync(chatId, "Unknown action.");
                    break;
            }

            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        private async Task PromptPriceConfirmationAsync(ITelegramBotClient botClient, long chatId)
        {
            var priceKeyboard = new InlineKeyboardMarkup(
            [
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Yes", "agree_price"),
                    InlineKeyboardButton.WithCallbackData("No", "disagree_price")
                }
            ]);

            await botClient.SendTextMessageAsync(chatId, "The insurance price is 100 USD. Do you agree?", replyMarkup: priceKeyboard);
        }

        private async Task ShowFinalChanceButtonsAsync(ITelegramBotClient botClient, long chatId)
        {
            var finalKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Yes", "final_agree"),
                    InlineKeyboardButton.WithCallbackData("No", "final_disagree")
                }
            });

            await botClient.SendTextMessageAsync(chatId, "The price is fixed at 100 USD. Would you like to proceed?", replyMarkup: finalKeyboard);
        }

        private async Task SendGeneratedPolicyAsync(ITelegramBotClient botClient, long chatId)
        {
            if (!_userData.TryGetValue(chatId, out var userInfo))
            {
                await botClient.SendTextMessageAsync(chatId, "User data not found. Please start again.");
                return;
            }

            var policyText = await _insuranceService.GeneratePolicyAsync(userInfo.Name, userInfo.Passport, "100 USD");
            await botClient.SendTextMessageAsync(chatId, EscapeMarkdown(policyText), parseMode: ParseMode.MarkdownV2);
        }

        private string EscapeMarkdown(string text)
        {
            var specialChars = new[] { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };
            foreach (var c in specialChars)
            {
                text = text.Replace(c, "\\" + c);
            }
            return text;
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"[ERROR] {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
