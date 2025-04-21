using Car_Insurance_Bot.Handlers;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace Car_Insurance_Bot.Infrastructure
{
    public class BotBackgroundService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly UpdateHandler _handler;

        public BotBackgroundService(ITelegramBotClient botClient, UpdateHandler handler)
        {
            _botClient = botClient;
            _handler = handler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
            updateHandler: async (client, update, token) =>
            {
                await _handler.HandleUpdateAsync(client, update, token);
            },
            pollingErrorHandler: async (client, exception, token) =>
            {
                await _handler.HandleErrorAsync(client, exception, token);
            },
            receiverOptions: new ReceiverOptions(),
            cancellationToken: stoppingToken
            );

            var botInfo = await _botClient.GetMeAsync(stoppingToken);
            Console.WriteLine($"[INFO] Bot @{botInfo.Username} is up and running.");
        }
    }
}
