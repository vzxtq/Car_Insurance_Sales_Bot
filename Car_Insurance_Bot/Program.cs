using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
new TelegramBotClient(builder.Configuration["Telegram:BotToken"]));

builder.Services.AddSingleton<UpdateHandler>();
builder.Services.AddHostedService<BotBackgroundService>();

var app = builder.Build();
app.Run();
