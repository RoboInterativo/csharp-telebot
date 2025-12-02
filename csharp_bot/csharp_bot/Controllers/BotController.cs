using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using csharp_bot.Configuration;
using csharp_bot.Services;

namespace csharp_bot.Controllers
{
    [ApiController]
    [Route("api/bot")]
    public class BotController : ControllerBase
    {
        private readonly ITelegramBotClient _botClient;
        private readonly UpdateHandler _updateHandler;
        private readonly BotConfiguration _botConfig;
        private readonly ILogger<BotController> _logger;

        public BotController(
            ITelegramBotClient botClient,
            UpdateHandler updateHandler,
            IOptions<BotConfiguration> botOptions,
            ILogger<BotController> logger)
        {
            _botClient = botClient;
            _updateHandler = updateHandler;
            _botConfig = botOptions.Value;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            _logger.LogInformation("Received update {UpdateId}", update.Id);

            // Обрабатываем асинхронно
            _ = Task.Run(async () =>
            {
                try
                {
                    await _updateHandler.HandleUpdateAsync(update, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling update");
                }
            });

            return Ok();
        }

        [HttpGet("setup")]
        public async Task<IActionResult> SetupWebhook()
        {
            try
            {
                var webhookUrl = $"{_botConfig.WebhookUrl}/api/bot/webhook";
                await _botClient.SetWebhookAsync(
                    url: webhookUrl,
                    cancellationToken: HttpContext.RequestAborted);

                _logger.LogInformation("Webhook set to: {WebhookUrl}", webhookUrl);
                return Ok($"Webhook установлен: {webhookUrl}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set webhook");
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }

        [HttpGet("remove")]
        public async Task<IActionResult> RemoveWebhook()
        {
            try
            {
                await _botClient.DeleteWebhookAsync(cancellationToken: HttpContext.RequestAborted);
                _logger.LogInformation("Webhook removed");
                return Ok("Webhook удален");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove webhook");
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetBotInfo()
        {
            try
            {
                var me = await _botClient.GetMeAsync(HttpContext.RequestAborted);
                var webhookInfo = await _botClient.GetWebhookInfoAsync(HttpContext.RequestAborted);

                return Ok(new
                {
                    BotName = me.Username,
                    FirstName = me.FirstName,
                    IsBot = me.IsBot,
                    WebhookUrl = webhookInfo.Url,
                    HasCustomCertificate = webhookInfo.HasCustomCertificate,
                    PendingUpdates = webhookInfo.PendingUpdateCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }
    }
}