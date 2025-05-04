using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Car_Insurance_Bot.Services;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Car_Insurance_Bot.Utils;

namespace Car_Insurance_Bot.Handlers
{
    public class UpdateHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly InsuranceService _insuranceService;
        private readonly string? _botToken;
        private readonly MindeePassportService _mindeePassportService;
        private readonly MindeeService _mindeeService;
        private static readonly ConcurrentDictionary<long, (string Name, string Passport)> _userData = new();
        private static readonly ConcurrentDictionary<long, string> _userState = new();
        private readonly GeminiHandler _geminiHandler;
        private readonly TextMessageHandler _textMessageHandler;
        private readonly FileMessageHandler _fileMessageHandler;
        private readonly CallbackHandler _callbackHandler;

        public UpdateHandler(IConfiguration configuration, ITelegramBotClient botClient, 
                            InsuranceService insuranceService, MindeePassportService mindeePassportService, MindeeService mindeeService)
        {
            _botClient = botClient;
            _geminiHandler = new GeminiHandler(configuration);
            _insuranceService = insuranceService;
            _mindeePassportService = mindeePassportService;
            _mindeeService = mindeeService;
            _botToken = configuration["Telegram:BotToken"];

            _textMessageHandler = new TextMessageHandler(_botClient, _geminiHandler, _userState);
            _fileMessageHandler = new FileMessageHandler(_botClient, _mindeePassportService, _mindeeService, _botToken!, _userData, _userState);
            _callbackHandler = new CallbackHandler(_botClient, _insuranceService, _mindeePassportService, _mindeeService, _userData, _userState);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            if (update.Type == UpdateType.Message && update.Message is { } message)
            {
                var chatId = message.Chat.Id;

                if (message.Type == MessageType.Text)
                {
                    await _textMessageHandler.HandleAsync(message, chatId);
                }
                else if (message.Type == MessageType.Document)
                {
                    await _fileMessageHandler.HandleAsync(message, chatId);
                }
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery is { } callbackQuery)
            {
                await _callbackHandler.HandleAsync(update.CallbackQuery);           
            }
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"[ERROR] {exception.Message}");
            return Task.CompletedTask;
        }
    }
}