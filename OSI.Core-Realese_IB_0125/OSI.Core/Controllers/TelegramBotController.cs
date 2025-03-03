using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests.Telegram;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Телеграм мессенджер
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles.Support)]
    public class TelegramBotController : ControllerBase
    {
        /// <summary>
        /// Перезапустить бота
        /// </summary>
        /// <returns></returns>
        [HttpGet("restart")]
        public IActionResult RestartBot([FromServices] ITelegramBotSvc bot)
        {
            bot.RestartBot();
            return Ok();
        }

        /// <summary>
        /// Перезапустить бота подписок
        /// </summary>
        /// <returns></returns>
        [HttpGet("restart-subscription")]
        public IActionResult RestartSubscriptionBot([FromServices] ITelegramSubscriptionBotSvc bot)
        {
            bot.RestartBot();
            return Ok();
        }

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="modelService"></param>
        /// <param name="phone">Телефон</param>
        /// <param name="message">Сообщение</param>
        /// <returns></returns>
        [HttpGet("send/{phone}/{message}")]
        public async Task<IActionResult> SendMessageGet(
            [FromServices] ITelegramBotSvc bot,
            [FromServices] IModelService<OSIBillingDbContext, TelegramChat> modelService,
            string phone,
            string message) =>
            await SendMessage(bot, modelService, phone, message);

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="modelService"></param>
        /// <param name="phone">Телефон</param>
        /// <param name="message">Сообщение</param>
        /// <returns></returns>
        [HttpPost("send/{phone}")]
        [Consumes("text/plain")]
        public async Task<IActionResult> SendMessagePost(
            [FromServices] ITelegramBotSvc bot,
            [FromServices] IModelService<OSIBillingDbContext, TelegramChat> modelService,
            string phone,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Disallow)] string message) =>
            await SendMessage(bot, modelService, phone, message);

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        /// <returns></returns>
        [HttpPost("send")]
        public async Task<IActionResult> SendMessagePostModel(
            [FromServices] ITelegramBotSvc bot,
            [FromServices] IModelService<OSIBillingDbContext, TelegramChat> modelService,
            [FromBody] SendMessage request) =>
            await SendMessage(bot, modelService, request.Phone, request.Message);

        private async Task<IActionResult> SendMessage(
            ITelegramBotSvc bot,
            IModelService<OSIBillingDbContext, TelegramChat> modelService,
            string phone,
            string message)
        {
            // убираем все нечисловые символы и берем последние 10 цифр
            phone = new string(Regex.Replace(phone, "[^0-9]", string.Empty).TakeLast(10).ToArray());
            var chat = await modelService.GetModelByFunc(tc => tc.Phone == phone);
            if (chat == null)
                return NotFound();
            await bot.SendMessage(chat.ChatId, message);
            return Ok();
        }

        /// <summary>
        /// Отправить сообщение председателям
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="message">Сообщение</param>
        /// <returns></returns>
        [HttpPost("send/chairmans")]
        [Consumes("text/plain")]
        public async Task<IActionResult> SendMessageToChairmansPost(
            [FromServices] ITelegramBotSvc bot,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Disallow)] string message)
        {
            await bot.SendMessageToChairmans(message);
            return Ok();
        }

        /// <summary>
        /// Отправить уведомление о заявке на консультацию
        /// </summary>
        /// <returns></returns>
        [HttpPost("send/call-me-back")]
        [AllowAnonymous]
        public async Task<IActionResult> SendCallMeBackNotification(
            [FromServices] ITelegramBotSvc bot,
            [FromBody] CallMeBackNotificationRequest request)
        {
            await bot.SendCallMeBackNotification(request);
            return Ok();
        }
    }
}
