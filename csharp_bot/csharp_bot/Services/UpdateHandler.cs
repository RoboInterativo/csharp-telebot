using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace csharp_bot.Services
{
    public class UpdateHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<UpdateHandler> _logger;

        public UpdateHandler(
            ITelegramBotClient botClient,
            ILogger<UpdateHandler> logger)
        {
            _botClient = botClient;
            _logger = logger;
        }

        public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => OnMessageReceived(update.Message!, cancellationToken),
                UpdateType.CallbackQuery => OnCallbackQueryReceived(update.CallbackQuery!, cancellationToken),
                _ => UnknownUpdateHandlerAsync(update, cancellationToken)
            };

            try
            {
                await handler;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling update");
            }
        }

        private async Task OnMessageReceived(Message message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received message from {UserId}: {Text}",
                message.From?.Id, message.Text);

            if (message.Text is not { } messageText)
                return;

            var action = messageText.Split(' ')[0] switch
            {
                "/start" => SendStartMessage(_botClient, message, cancellationToken),
                "/help" => SendHelpMessage(_botClient, message, cancellationToken),
                "/echo" => SendEchoMessage(_botClient, message, cancellationToken),
                _ => SendDefaultMessage(_botClient, message, cancellationToken)
            };

            await action;
        }

        private static async Task<Message> SendStartMessage(
            ITelegramBotClient botClient,
            Message message,
            CancellationToken cancellationToken)
        {
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "🎉 Добро пожаловать!\n\n" +
                      "Я бот, созданный в Visual Studio 2022!\n" +
                      "Используйте /help для списка команд.",
                cancellationToken: cancellationToken);
        }

        private static async Task<Message> SendHelpMessage(
            ITelegramBotClient botClient,
            Message message,
            CancellationToken cancellationToken)
        {
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "📋 Доступные команды:\n" +
                      "/start - Приветствие\n" +
                      "/help - Эта справка\n" +
                      "/echo [текст] - Эхо-ответ\n\n" +
                      "Просто напишите сообщение, и я отвечу!",
                cancellationToken: cancellationToken);
        }

        private static async Task<Message> SendEchoMessage(
            ITelegramBotClient botClient,
            Message message,
            CancellationToken cancellationToken)
        {
            var echoText = message.Text!.Length > 6 ? message.Text[6..] : "Я не услышал что эхо...";

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"📢 Эхо: {echoText}",
                cancellationToken: cancellationToken);
        }

        private static async Task<Message> SendDefaultMessage(
            ITelegramBotClient botClient,
            Message message,
            CancellationToken cancellationToken)
        {
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Вы написали: {message.Text}",
                cancellationToken: cancellationToken);
        }

        private Task OnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received callback query from {UserId}", callbackQuery.From.Id);
            return Task.CompletedTask;
        }

        private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
            return Task.CompletedTask;
        }
    }
}