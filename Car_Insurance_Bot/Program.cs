using System;
using Car_Insurance_Bot.Handlers;
using Car_Insurance_Bot.Infrastructure;
using Car_Insurance_Bot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

class Program
{
    public static void Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<ITelegramBotClient>(sp =>
            {
                var botToken = builder.Configuration["Telegram:BotToken"];
                if (string.IsNullOrEmpty(botToken))
                    throw new ArgumentNullException("Telegram:BotToken", "Telegram bot token is missing.");
                return new TelegramBotClient(botToken);
            });

            builder.Services.AddSingleton<InsuranceService>();
            builder.Services.AddHostedService<BotBackgroundService>();

            builder.Services.AddSingleton<TesseractService>(sp =>
            {
                var tessDataPath = builder.Configuration["TesseractDataPath"];
                if (string.IsNullOrEmpty(tessDataPath))
                    throw new ArgumentNullException("TesseractDataPath", "Tesseract data path is missing.");
                return new TesseractService(tessDataPath);
            });

            builder.Services.AddSingleton<MindeeService>(sp =>
            {
                var apiKey = builder.Configuration["Mindee:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                    throw new ArgumentNullException("Mindee:ApiKey", "Mindee API key is missing.");
                return new MindeeService(apiKey);
            });

            builder.Services.AddSingleton<UpdateHandler>();

            var app = builder.Build();
            app.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Host terminated unexpectedly.");
            Console.WriteLine(ex.ToString());
            Environment.Exit(1);
        }
    }
}
