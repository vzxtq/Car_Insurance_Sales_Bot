    using Telegram.Bot;
    using Telegram.Bot.Types;
    using Telegram.Bot.Types.Enums;
    using Telegram.Bot.Types.ReplyMarkups;
    using Car_Insurance_Bot.Services;
using System.Diagnostics.CodeAnalysis;

public class UpdateHandler
    {
        private readonly ITelegramBotClient _botClient;
        public UpdateHandler(ITelegramBotClient botClient)
        {
            _botClient = botClient;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            if (update.Type == UpdateType.Message && update.Message != null)
            {
                var message = update.Message;
                var chatId = message.Chat.Id;

                if (message.Type == MessageType.Text)
                {
                    switch (message.Text?.ToLower())
                    {
                        case "/start":
                            await botClient.SendTextMessageAsync(chatId, "Hello! I can help you get car insurance. Please send a photo of your passport.");
                            break;

                        default:
                            await botClient.SendTextMessageAsync(chatId, "Please send a photo of your passport or type /start to begin.");
                            break;
                    }
                }
                else if (message.Type == MessageType.Photo)
                {
                    await botClient.SendTextMessageAsync(chatId, "Photo received. Processing...");
                    await Task.Delay(1000);

                    var (name, passport) = MindeeMock.ExtractData();
                    string extractedData = $"Name: {name}\nPassport:{passport}";
                    var dataKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] {
                            InlineKeyboardButton.WithCallbackData("Yes", "confirm_yes"),
                            InlineKeyboardButton.WithCallbackData("No", "confirm_no")
                        }
                    });
                    await botClient.SendTextMessageAsync(chatId,  $"Extracted data:\n{extractedData}\n\nIs this correct?", replyMarkup: dataKeyboard);
                }
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                await HandleCallbackQueryAsync(botClient, update.CallbackQuery, ct);
            }
        }

        public async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken ct)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var data = callbackQuery.Data;

            switch (data)
            {
                case "confirm_yes":
                    await botClient.SendTextMessageAsync(chatId, "Data confirmed.");
                    await AskForPriceAgreement(botClient, chatId);
                    break;

                case "confirm_no":
                    await botClient.SendTextMessageAsync(chatId, "Please send another photo of your passport.");
                    break;

                case "agree_price":
                    await botClient.SendTextMessageAsync(chatId, "Thank you! Generating your insurance policy...");
                    await Task.Delay(1000);
                    await SendInsurancePolicy(botClient, chatId);
                    break;

                case "disagree_price":
  
                    await ShowFinalChanceButtons(botClient, chatId);
                    break;

                case "final_agree":
                    await botClient.SendTextMessageAsync(chatId, "Glad you reconsidered! Generating your insurance policy...");
                    await Task.Delay(1000);
                    await SendInsurancePolicy(botClient, chatId);
                    break;
                
                case "final_disagree":
                    await botClient.SendTextMessageAsync(chatId, "Thank you for your time. If you change your mind, just type /start again. Goodbye.");
                    break;

                default:
                    await botClient.SendTextMessageAsync(chatId, "Unknown action.");
                    break;
            }

            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        private async Task ShowFinalChanceButtons(ITelegramBotClient botClient, long chatId)
        {
            var finalKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] {
                    InlineKeyboardButton.WithCallbackData("Yes", "final_agree"),
                    InlineKeyboardButton.WithCallbackData("No", "final_disagree")
                }
            });

            await botClient.SendTextMessageAsync(chatId, "The price is fixed at 100 USD. Do you want to continue?", replyMarkup: finalKeyboard);
        }

        private async Task AskForPriceAgreement(ITelegramBotClient botClient, long chatId)
        {
            var priceKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] {
                    InlineKeyboardButton.WithCallbackData("Yes", "agree_price"),
                    InlineKeyboardButton.WithCallbackData("No", "disagree_price")
                }
            });

            await botClient.SendTextMessageAsync(chatId, "The insurance price is 100 USD. Do you agree?", replyMarkup: priceKeyboard);
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

        private async Task SendInsurancePolicy(ITelegramBotClient botClient, long chatId)
        {
            var (name, passport) = MindeeMock.ExtractData(); 

            string template = await System.IO.File.ReadAllTextAsync("template.txt"); 

            string policy = template 
                .Replace("{POLICY_NUMBER}", Guid.NewGuid().ToString().Substring(0, 8).ToUpper())
                .Replace("{NAME}", name)
                .Replace("{PASSPORT}", passport)
                .Replace("{PRICE}", "100 USD");

            policy = EscapeMarkdown(policy);

            await botClient.SendTextMessageAsync(chatId, policy, parseMode: ParseMode.MarkdownV2);
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"Error: {exception.Message}");
            return Task.CompletedTask;
        }
    }