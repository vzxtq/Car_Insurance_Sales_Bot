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
                        await _botClient.SendTextMessageAsync(chatId, "ℹ️ If you need help at any point, type /help");
                    }
                    else if (userInput == "/insurance")
                    {
                        _userState[chatId] = "awaiting_passport";
                        await _botClient.SendTextMessageAsync(chatId, "📄 Please upload a clear image of your passport (as a file).\nThis is required to extract your personal information for the insurance contract");
                    }
                return;
                
                case "/cancel":
                    _userState.TryRemove(chatId, out _);
                    _userState[chatId] = "idle";
                    await _botClient.SendTextMessageAsync(chatId, "❌ The insurance process has been cancelled.\nTo restart, please type /start at any time");
                    return;

                case "/help":
                    _userState[chatId] = "awaiting_passport";
                    await _botClient.SendTextMessageAsync(chatId, "Help Menu\n\n/cancel — Cancel the current process \n\n🤖 You may also ask questions at any time. Our AI assistant is here to support you throughout the application");
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
                    prompt = $"The user is in the insurance flow and has not uploaded a passport yet. They asked: '{message.Text}'. Explain why the passport photo is needed for insurance in a friendly and clear way.";
                    break;
                
                case "awaiting_vin":
                    prompt = $"The user is in the VIN stage and asked: '{message.Text}'. Explain why the VIN is required and how to properly photograph the VIN document.";
                    break;
                
                case "completed":
                    prompt = $"The insurance process is complete and the policy has been generated. The user asked: '{message.Text}'. Respond helpfully and briefly, assuming they may have questions about their policy, coverage, changes, or next steps.";
                    break;
                
                default:
                    prompt = $"The user is in state '{state}' and asked: '{message.Text}'. Reply briefly, helpfully, and in context.";
                    break;
            }

            var reply = await _geminiHandler.SendToGeminiAsync(prompt);
            await _botClient.SendTextMessageAsync(chatId, reply);
        }
    }
}
