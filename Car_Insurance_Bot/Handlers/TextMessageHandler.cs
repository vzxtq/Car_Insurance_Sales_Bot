using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Types;
using Car_Insurance_Bot.Handlers;
using Car_Insurance_Bot.Services;

namespace Car_Insurance_Bot.Handlers
{
    public class TextMessageHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly GeminiHandler _geminiHandler;
        private readonly ConcurrentDictionary<long, string> _userState;

        public TextMessageHandler(
            ITelegramBotClient botClient,
            GeminiHandler geminiHandler,
            ConcurrentDictionary<long, string> userState)
        {
            _botClient = botClient;
            _geminiHandler = geminiHandler;
            _userState = userState;
        }

        public async Task HandleAsync(Message message, long chatId)
        {
            var userInput = message.Text?.Trim();

            switch (userInput?.ToLower())
            {
                case "/start":
                case "/insurance":
                    if (_userState.TryGetValue(chatId, out var stated) && stated != "idle" && stated != "canceled")
                    {
                        await _botClient.SendTextMessageAsync(chatId, "⚠️ An insurance process is already in progress.\nIf you wish to restart, please use /cancel first");
                        return;
                    }

                    if (userInput == "/start")
                    {
                        _userState[chatId] = "idle";
                        await _botClient.SendTextMessageAsync(chatId, "👋 Welcome to the Car Insurance Assistant.\n\nTo begin the vehicle insurance application process, please type /insurance");
                    }
                    else if (userInput == "/insurance")
                    {
                        _userState[chatId] = "awaiting_passport";
                        await _botClient.SendTextMessageAsync(chatId, "📄 Please upload a clear image of your passport (as a file).\nThis is required to extract your personal information for the insurance contract");
                    }
                return;
                
                case "/cancel":
                    _userState[chatId] = "cancelled";
                    _userState.TryRemove(chatId, out _);
                    await _botClient.SendTextMessageAsync(chatId, "❌ The insurance process has been cancelled.\nTo restart, please type /start at any time");
                    return;

                case "/help":
                    _userState[chatId] = "help";
                    await _botClient.SendTextMessageAsync(chatId, "🆘 Help Menu\n\n• /start — Begin the car insurance process\n• /cancel — Cancel the current process\n\nIf you have any questions, feel free to type them directly here");
                    return;
            }

            if (!_userState.TryGetValue(chatId, out var state))
            {
                await _botClient.SendTextMessageAsync(chatId, "❗ It looks like we haven't started yet. Please type /start to begin the insurance process");
                return;
            }

            switch (state)
            {
                case "awaiting_passport":
                    var prompt = $"User is in insurance flow, passport not yet provided. They ask: '{message.Text}'. Explain why passport photo is needed and guide them.";
                    var explanation = await _geminiHandler.SendToGeminiAsync(prompt);
                    await _botClient.SendTextMessageAsync(chatId, explanation);
                    break;
                
                case "awaiting_confirm":
                    var confPrompt = $"User is reviewing the extracted personal data from their document. They asked: '{message.Text}'. Provide a brief and clear response encouraging them to confirm or correct the data.";
                    var confReply = await _geminiHandler.SendToGeminiAsync(confPrompt);
                    await _botClient.SendTextMessageAsync(chatId, confReply);
                    break;

                case "confirmed":
                    var pricePrompt = $"User is reviewing price agreement. They ask: '{message.Text}'. Persuade them on the benefits of insurance and price fairness.";
                    var priceReply = await _geminiHandler.SendToGeminiAsync(pricePrompt);
                    await _botClient.SendTextMessageAsync(chatId, priceReply);
                    break;

                default:
                    var defaultPrompt = $"User in state '{state}' asks: '{message.Text}'. Respond briefly, stay on topic, and be helpful.";
                    var reply = await _geminiHandler.SendToGeminiAsync(defaultPrompt);
                    await _botClient.SendTextMessageAsync(chatId, reply);
                    break;
            }
        }
    }
}
