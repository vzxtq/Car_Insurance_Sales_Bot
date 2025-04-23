using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Types;

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
            var userInput = message.Text?.ToLower();

            switch (userInput)
            {
                case "/start":
                    _userState[chatId] = "idle";
                    await _botClient.SendTextMessageAsync(chatId, "👋 Welcome! Use /insurance to start or /chat to talk to AI.");
                    return;

                case "/insurance":
                    _userState[chatId] = "started";
                    await _botClient.SendTextMessageAsync(chatId, "🚗 Let's get your car insured. Please send a photo of your passport.");
                    return;

                case "/chat":
                    _userState[chatId] = "chat";
                    await _botClient.SendTextMessageAsync(chatId, "🧠 Chat mode activated! Ask me anything.");
                    return;
            }

            if (!_userState.TryGetValue(chatId, out var state))
            {
                await _botClient.SendTextMessageAsync(chatId, "Please use /start to begin.");
                return;
            }

            switch (state)
            {
                case "chat":
                    var reply = await _geminiHandler.SendToGeminiAsync(message.Text);
                    await _botClient.SendTextMessageAsync(chatId, reply);
                    break;

                case "started":
                    await _botClient.SendTextMessageAsync(chatId, "📸 Please send your passport photo (file).");
                    break;

                case "awaiting_confirm":
                    await _botClient.SendTextMessageAsync(chatId, "✅ Please confirm the data using the buttons.");
                    break;

                case "confirmed":
                    await _botClient.SendTextMessageAsync(chatId, "💰 Please respond to the price offer below.");
                    break;

                default:
                    await _botClient.SendTextMessageAsync(chatId, "🤖 I'm not sure what to do. Try /chat or /insurance.");
                    break;
            }
        }
    }
}
