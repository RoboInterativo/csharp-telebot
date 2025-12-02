using csharp_bot.Configuration;
using csharp_bot.Services;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);

// Настройка Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/bot-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Конфигурация бота
builder.Services.Configure<BotConfiguration>(
    builder.Configuration.GetSection("BotConfiguration"));

// Регистрация Telegram Bot Client
builder.Services.AddHttpClient("telegram_bot_client")
    .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
    {
        var botConfig = sp.GetService<IOptions<BotConfiguration>>();
        var botToken = botConfig?.Value.BotToken ??
                      builder.Configuration["BotConfiguration:BotToken"] ??
                      Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") ??
                      throw new InvalidOperationException("Bot token is not configured");

        return new TelegramBotClient(botToken, httpClient);
    });

// Регистрация сервисов
builder.Services.AddScoped<UpdateHandler>();

// Регистрация контроллеров
builder.Services.AddControllers();

// Добавляем OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Настройка конвейера HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();

// Современный способ маршрутизации в .NET 8+
app.MapControllers();
app.MapGet("/", () => "🤖 Telegram Bot Webhook is running! 🚀");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Устанавливаем webhook автоматически при запуске
try
{
    using var scope = app.Services.CreateScope();
    var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    var webhookUrl = configuration["BotConfiguration:WebhookUrl"] ??
                     Environment.GetEnvironmentVariable("WEBHOOK_URL");

    if (!string.IsNullOrEmpty(webhookUrl))
    {
        // Удаляем старый webhook
        await botClient.DeleteWebhookAsync();
        Log.Information("Старый webhook удален");

        // Устанавливаем новый (в версии 22.x используется WebhookInfo)
        var fullWebhookUrl = $"{webhookUrl}/api/bot/webhook";
        await botClient.SetWebhookAsync(
            url: fullWebhookUrl,
            cancellationToken: CancellationToken.None);

        Log.Information($"✅ Webhook установлен: {fullWebhookUrl}");

        // Показываем информацию о боте
        var me = await botClient.GetMeAsync(CancellationToken.None);
        Log.Information($"🤖 Бот @{me.Username} готов к работе!");
    }
    else
    {
        Log.Warning("⚠️ WEBHOOK_URL не настроен. Используйте /api/bot/setup для ручной установки");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "❌ Ошибка при настройке webhook");
}

app.Run();