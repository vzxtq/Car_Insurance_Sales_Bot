using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Car_Insurance_Bot.Services;
using Car_Insurance_Bot.Utils;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using System.Reflection;

namespace Car_Insurance_Bot.Handlers
{
    public class CallbackHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly InsuranceService _insuranceService;
        private readonly MindeeService _mindeeService;
        private readonly ConcurrentDictionary<long, (string Name, string Passport)> _userPassportData;
        private readonly ConcurrentDictionary<long, string> _userVinData;
        private readonly ConcurrentDictionary<long, string> _userState;
        private readonly MarkDownEscaper _escaper;

        public CallbackHandler(
            ITelegramBotClient botClient,
            InsuranceService insuranceService,
            MindeePassportService mindeePassportService,
            MindeeService mindeeService,
            ConcurrentDictionary<long, (string Name, string Passport)> userPassportData,
            ConcurrentDictionary<long, string> userVinData,

            ConcurrentDictionary<long, string> userState)
        {
            _botClient = botClient;
            _insuranceService = insuranceService;
            _mindeeService = mindeeService;
            _userVinData = userVinData;
            _userPassportData = userPassportData;
            _userState = userState;
            _escaper = new MarkDownEscaper();
        }

        public async Task HandleAsync(CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var data = callbackQuery.Data;

            switch (data)
            {
                case "confirm_yes_passport":
                    _userState[chatId] = "awaiting_vin";
                    await _botClient.SendTextMessageAsync(chatId, "🆗 Passport confirmed");
                    await Task.Delay(1000);
                    await _botClient.SendTextMessageAsync(chatId, " Please upload a photo of your vehicle title (with VIN clearly visible)");
                    
                    await _botClient.EditMessageReplyMarkupAsync(chatId: chatId, messageId: callbackQuery.Message.MessageId, replyMarkup: null);
                    break;

                case "confirm_no_passport":
                    _userState[chatId] = "awaiting_passport";
                    _userPassportData.TryRemove(chatId, out _);

                    await _botClient.SendTextMessageAsync(chatId, "❌ Passport not confirmed.\n\nPlease upload a new photo of your passport.");
                    
                    await _botClient.EditMessageReplyMarkupAsync(chatId: chatId, messageId: callbackQuery.Message.MessageId, replyMarkup: null);
                    break;

                case "confirm_yes_vin":
                    _userState[chatId] = "confirmed_vin";
                    await _botClient.SendTextMessageAsync(chatId, "✅ Vehicle title confirmed");

                    await _botClient.EditMessageReplyMarkupAsync(chatId: chatId, messageId: callbackQuery.Message.MessageId, replyMarkup: null);

                    await PromptPriceConfirmationAsync(chatId);
                    break;

                case "confirm_no_vin":
                    _userState[chatId] = "awaiting_vin";
                    _userVinData.TryRemove(chatId, out _);

                    await _botClient.SendTextMessageAsync(chatId, "❌ VIN not confirmed.\n\nPlease upload a new image of the vehicle title with a clearly visible VIN number.");
                    
                    await _botClient.EditMessageReplyMarkupAsync(chatId: chatId, messageId: callbackQuery.Message.MessageId, replyMarkup: null);
                    break;

                case "agree_price":
                    await _botClient.SendTextMessageAsync(chatId, "🎉 Thank you! Generating your insurance policy...");
                    
                    await _botClient.EditMessageReplyMarkupAsync(chatId: chatId, messageId: callbackQuery.Message.MessageId, replyMarkup: null);

                    await Task.Delay(1000);
                    await GenerateAndSendPolicyAsync(chatId);
                    break;

                case "disagree_price":
                    await _botClient.EditMessageReplyMarkupAsync(chatId: chatId, messageId: callbackQuery.Message.MessageId, replyMarkup: null);

                    await ShowFinalChanceButtonsAsync(chatId);
                    break;

                case "final_agree":
                    await _botClient.SendTextMessageAsync(chatId, "🚀 Glad you reconsidered! Generating your policy...");
                    
                    await _botClient.EditMessageReplyMarkupAsync(chatId: chatId, messageId: callbackQuery.Message.MessageId, replyMarkup: null);

                    await Task.Delay(1000);
                    await GenerateAndSendPolicyAsync(chatId);
                    break;

                case "final_disagree":
                    await _botClient.SendTextMessageAsync(chatId, "Thank you for your time. If you change your mind, simply type /start to begin again.");
                    break;

                default:
                    await _botClient.SendTextMessageAsync(chatId, "⚠️ Unknown action. Please try again.");
                    break;
            }

            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        private async Task PromptPriceConfirmationAsync(long chatId)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Yes", "agree_price"),
                    InlineKeyboardButton.WithCallbackData("No", "disagree_price")
                }
            });
            await _botClient.SendTextMessageAsync(chatId, "The insurance cost is 100 USD.\nWould you like to proceed?", replyMarkup: keyboard);
        }

        private async Task ShowFinalChanceButtonsAsync(long chatId)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Yes", "final_agree"),
                    InlineKeyboardButton.WithCallbackData("No", "final_disagree")
                }
            });
            await _botClient.SendTextMessageAsync(chatId, "The price is fixed at 100 USD.\nWould you like to continue with the policy?", replyMarkup: keyboard);
        }

        private async Task GenerateAndSendPolicyAsync(long chatId)
        {
            if (!_userPassportData.TryGetValue(chatId, out var userInfo))
            {
                await _botClient.SendTextMessageAsync(chatId, "❗ Passport data is missing or incomplete.\nPlease type /cancel and restart the process.");
                return;
            }

            if (!_userVinData.TryGetValue(chatId, out var vinInfo))
            {
                await _botClient.SendTextMessageAsync(chatId, "❗ Vehicle data is missing or incomplete.\nPlease type /cancel and restart the process..");
                return;
            }

            var vin = vinInfo; 
            var policyText = await _insuranceService.GeneratePolicyAsync(userInfo.Name, userInfo.Passport, vin);
            var escaped = _escaper.EscapeMarkdown(policyText);
            await _botClient.SendTextMessageAsync(chatId, escaped, parseMode: ParseMode.MarkdownV2);
        }
    }
}
