using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
        _botClient.StartReceiving(
            _handler.HandleUpdateAsync,
            _handler.HandleErrorAsync,
            new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
            cancellationToken: stoppingToken
        );

        var me = await _botClient.GetMeAsync();
        Console.WriteLine($"Bot {me.Username} is up and running");
    }
}