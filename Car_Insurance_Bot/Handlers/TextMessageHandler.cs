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
                    if (_userState.TryGetValue(chatId, out var stated) && stated != "idle" && stated != "canceled")
                    {
                        await _botClient.SendTextMessageAsync(chatId, "⚠️ An insurance process is already in progress.\nIf you wish to restart, please use /cancel first");
                        return;
                    }

                    if (userInput == "/start")
                    {
                        _userState[chatId] = "awaiting_passport";
                          await _botClient.SendTextMessageAsync(chatId,
                                "👋 Welcome to the Car Insurance Assistant.\n\nTo begin, please prepare the following documents:\n" +
                                "📄 A photo of your passport\n🚗 A photo of your vehicle title showing the VIN number");

                        await Task.Delay(1000);

                         _userState[chatId] = "awaiting_passport";
                        await _botClient.SendTextMessageAsync(chatId, "Please send a clear photo of the main page of your passport");
                      
                    }
                return;
                
                case "/cancel":
                    _userState.TryRemove(chatId, out _);
                    _userState[chatId] = "idle";
                    await _botClient.SendTextMessageAsync(chatId, "❌ The insurance process has been cancelled.\nTo restart, please type /start at any time");
                    return;
            }

            if (!_userState.TryGetValue(chatId, out var state))
            {
                await _botClient.SendTextMessageAsync(chatId, "❗ It looks like we haven't started yet. Please type /start to begin the insurance process");
                return;
            }

            string prompt;
            switch(state)
            {
                case "awaiting_passport":
                    prompt = $"The user is in the 'passport upload' stage and asked: '{message.Text}'. Respond briefly and clearly. Then remind them to upload a clear photo of their passport to continue.";
                    break;

                case "awaiting_vin":
                    prompt = $"The user is in the VIN stage and asked: '{message.Text}'. Respond briefly, explain why the VIN is required and how to take a proper photo. Then remind them to upload the VIN document.";
                    break;

                case "completed":
                    prompt = $"The user's insurance process is complete. They asked: '{message.Text}'. Respond briefly and helpfully. Offer support for next steps, policy questions, or changes.";
                    break;

                default:
                    prompt = $"The user is in state '{state}' and asked: '{message.Text}'. Respond briefly and helpfully in a professional tone.";
                    break;
            }

            var reply = await _geminiHandler.SendToGeminiAsync(prompt);
            await _botClient.SendTextMessageAsync(chatId, reply);
        }
    }
}
