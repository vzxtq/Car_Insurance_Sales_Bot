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

            // load JSON (optional) + ENV
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            // HttpClient for Gemini, etc.
            builder.Services.AddHttpClient();

            // Telegram client
            builder.Services.AddSingleton<ITelegramBotClient>(sp =>
            {
                var botToken = builder.Configuration["Telegram:BotToken"];
                if (string.IsNullOrEmpty(botToken))
                    throw new ArgumentNullException("Telegram:BotToken", "Telegram bot token is missing.");
                return new TelegramBotClient(botToken);
            });

            builder.Services.AddSingleton<InsuranceService>();
            builder.Services.AddSingleton<UpdateHandler>();
            builder.Services.AddHostedService<BotBackgroundService>();

            // Tesseract
            builder.Services.AddSingleton<TesseractService>(sp =>
            {
                var tessDataPath = builder.Configuration["TesseractDataPath"];
                if (string.IsNullOrEmpty(tessDataPath))
                    throw new ArgumentNullException("TesseractDataPath", "Tesseract data path is missing.");
                return new TesseractService(tessDataPath);
            });

            var app = builder.Build();
            app.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Host terminated unexpectedly.");
            Console.WriteLine(ex.ToString());    // this prints InnerException too
            Environment.Exit(1);
        }
    }
}
