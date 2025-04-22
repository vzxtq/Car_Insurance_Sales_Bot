using Car_Insurance_Bot.Handlers;
using Car_Insurance_Bot.Infrastructure;
using Car_Insurance_Bot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddHttpClient();

builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var botToken = builder.Configuration["Telegram:BotToken"];
    if (string.IsNullOrEmpty(botToken))
    {
        throw new ArgumentNullException("Telegram:BotToken", "Token for Telegram Bot is missing.");
    }

    return new TelegramBotClient(botToken);
});

builder.Services.AddSingleton<InsuranceService>();

builder.Services.AddSingleton<UpdateHandler>();

builder.Services.AddHostedService<BotBackgroundService>();

builder.Services.AddSingleton<TesseractPassportService>(sp=>
{
    var tessDataPath = builder.Configuration["TesseractDataPath"];
    if (string.IsNullOrEmpty(tessDataPath))
    {
        throw new ArgumentNullException("TesseractDataPath", "Tesseract data path is missing.");
    }

    return new TesseractPassportService(tessDataPath);
});

var app = builder.Build();

app.Run();
