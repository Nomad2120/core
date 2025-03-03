using ESoft.CommonLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OSI.Core.Extensions;
using OSI.Core.Models.Db;
using OSI.Core.Pages;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using OSI.Core.Models.Requests.Telegram;

namespace OSI.Core.Services
{
    public interface ITelegramBotSvc
    {
        Task GenerateAndSendOtp(long chatId, string phone);
        Task SendMessage(long chatId, string message);
        Task SendMessageToChairmans(string message);
        Task SendRegistrationSignedNotification(Registration registration);
        Task SendRegistrationRejectedNotification(Registration registration);
        Task SendOsiCreatedNotification(Registration registration);
        Task SendActCreatedNotification(int actId);
        Task SendCallMeBackNotification(CallMeBackNotificationRequest request);
        Task SendOsiAccountApplicationNotification(OsiAccountApplication application, bool repeat = false);
        void RestartBot();
    }

    public class TelegramBotSvc : ITelegramBotSvc
    {
        private readonly IConfiguration configuration;
        private readonly IModelService<OSIBillingDbContext, TelegramChat> telegramChatSvc;
        private readonly IOsiSvc osiSvc;
        private readonly IOTPSvc otpSvc;
        private readonly IUserSvc userSvc;
        private readonly ILogger<TelegramBotSvc> logger;
        private ITelegramBotClient BotClient;
        private string _BotServiceResponseHeader = "<b>OSI.Bot</b>";
        private Task receivingTask = null;
        private CancellationTokenSource cancellationTokenSource = null;
        private bool throwOutPendingUpdates = false;
        private bool ThrowOutPendingUpdates => throwOutPendingUpdates ? !(throwOutPendingUpdates = false) : throwOutPendingUpdates;

        internal static bool DoNotStart = false;

        public TelegramBotSvc(IConfiguration configuration,
            IModelService<OSIBillingDbContext, TelegramChat> telegramChatSvc,
            IOsiSvc osiSvc, IOTPSvc otpSvc, IUserSvc userSvc,
            ILogger<TelegramBotSvc> logger)
        {
            this.configuration = configuration;
            this.telegramChatSvc = telegramChatSvc;
            this.osiSvc = osiSvc;
            this.otpSvc = otpSvc;
            this.userSvc = userSvc;
            this.logger = logger;
            StartBot();
        }

        public async void RestartBot()
        {
            logger.LogInformation("Restarting bot");
            try
            {
                if (BotClient != null && receivingTask?.IsCompleted == false && cancellationTokenSource != null)
                {
                    cancellationTokenSource.Cancel();
                    try
                    {
                        await receivingTask;
                    }
                    catch { }
                    finally
                    {
                        cancellationTokenSource.Dispose();
                        receivingTask.Dispose();
                        cancellationTokenSource = null;
                        receivingTask = null;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bot stop error");
            }
            StartBot();
        }

        private async void StartBot()
        {
            if (DoNotStart)
                return;
            try
            {
                BotClient = new TelegramBotClient(configuration["Bot:Token"]);
                _BotServiceResponseHeader = $"<b>{(await BotClient.GetMeAsync()).Username}</b>";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Telegram connect error");
                Task.Delay(TimeSpan.FromMinutes(5)).Wait();
                RestartBot();
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            receivingTask = BotClient.ReceiveAsync(
                HandleUpdate,
                HandlePollingError,
                new ReceiverOptions
                {
                    AllowedUpdates = Array.Empty<UpdateType>(),
                    ThrowPendingUpdates = ThrowOutPendingUpdates,
                },
                cancellationTokenSource.Token)
                .ContinueWith(t =>
                {
                    logger.LogError(t.Exception, "ReceiveAsync exception");
                    // Убираем все скопившиеся обновления, так как из-за одного из них падает обработка обновлений,
                    // и мы будем бесконечно пытаться обработать одно и то же сообщение 
                    logger.LogInformation("ReceiveAsync exception, throwing out pending updates");
                    throwOutPendingUpdates = true;
                    RestartBot();
                }, TaskContinuationOptions.OnlyOnFaulted);

            logger.LogInformation("Bot started");
        }

        private async Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message)
            {
                try
                {
                    var chatId = update.Message.Chat.Id;
                    if (update.Message.Type == MessageType.Text)
                    {
                        if (update.Message.Text.StartsWith("/start"))
                        {
                            await bot.SendTextMessageToChatAsync(chatId,
                                _BotServiceResponseHeader + Environment.NewLine + "Чтобы начать работу необходимо поделиться своим контактом. Нажмите кнопку \"Отправить контакт\"",
                                replyMarkup:
                                new ReplyKeyboardMarkup(
                                    new KeyboardButton[][]
                                    {
                                        new KeyboardButton[]
                                        {
                                            KeyboardButton.WithRequestContact("Отправить контакт"),
                                        }
                                    })
                                {
                                    OneTimeKeyboard = true,
                                    ResizeKeyboard = true,
                                },
                                logger: logger,
                                cancellationToken: cancellationToken);
                        }
                        else
                        {
                            if (!update.Message.From.IsBot)
                            {
                                await bot.SafeDeleteMessageAsync(chatId, update.Message.MessageId, logger: logger, cancellationToken: cancellationToken);
                            }
                        }
                    }
                    else if (update.Message.Type == MessageType.Contact)
                    {
                        string phone = new(Regex.Replace(update.Message.Contact.PhoneNumber, "[^0-9]", string.Empty).TakeLast(10).ToArray());
                        var telegramChat = await telegramChatSvc.GetModelByFunc(tc => tc.ChatId == chatId) ?? new TelegramChat
                        {
                            ChatId = chatId,
                        };
                        telegramChat.Phone = phone;
                        telegramChat.FIO = $"{update.Message.Contact.FirstName} {update.Message.Contact.LastName}".Trim();
                        await telegramChatSvc.AddOrUpdateModel(telegramChat);
                        await bot.SendTextMessageToChatAsync(chatId,
                            _BotServiceResponseHeader + Environment.NewLine + $"Ваш номер принят",
                            replyMarkup: new ReplyKeyboardRemove(),
                            logger: logger, cancellationToken: cancellationToken);
                        await bot.SafeDeleteMessageAsync(chatId, update.Message.MessageId, logger: logger, cancellationToken: cancellationToken);
                        // обход для iPhone, там контакт делится не как ответ на сообщение
                        if (update.Message.ReplyToMessage != null)
                        {
                            await bot.SafeDeleteMessageAsync(chatId, update.Message.ReplyToMessage.MessageId, logger: logger, cancellationToken: cancellationToken);
                        }
                        await GenerateAndSendOtp(chatId, phone);
                    }
                    else
                    {
                        if (!update.Message.From.IsBot)
                        {
                            await bot.SafeDeleteMessageAsync(chatId, update.Message.MessageId, logger: logger, cancellationToken: cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error");
                }
            }
        }

        private Task HandlePollingError(ITelegramBotClient bot, Exception ex, CancellationToken cancellationToken)
        {
            var ErrorMessage = ex switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error [{apiRequestException.ErrorCode}]",
                _ => "Telegram Polling Error"
            };
            logger.LogError(ex, ErrorMessage);
            return Task.CompletedTask;
        }

        public async Task GenerateAndSendOtp(long chatId, string phone)
        {
            string otp = otpSvc.GenerateOTP(phone);
            await SendMessage(chatId, "Данный код сгенерирован системой eOsi.kz. Никому не говорите его! Код: " + otp + Environment.NewLine +
                $"<a href=\"{configuration["Bot:LoginUrl"]}\">Вход</a>");
        }

        public async Task SendMessage(long chatId, string message)
        {
            await BotClient.SendTextMessageToChatAsync(chatId, message, logger: logger);
        }

        public async Task SendMessageToChairmans(string message)
        {
            var chairmans = await userSvc.GetActiveChairmans();
            foreach (var user in chairmans)
            {
                var chatId = (await telegramChatSvc.GetModelByFunc(tc => tc.Phone == user.Phone))?.ChatId;
                if (chatId.HasValue)
                    await SendMessage(chatId.Value, message);
            }
        }

        public async Task SendRegistrationSignedNotification(Registration registration)
        {
            var operators = await userSvc.GetUsersByRole("OPERATOR");
            foreach (var user in operators)
            {
                var chatId = (await telegramChatSvc.GetModelByFunc(tc => tc.Phone == user.Phone))?.ChatId;
                var registrationKindText = registration.RegistrationKind switch
                {
                    "INITIAL" => " на регистрацию",
                    "CHANGE_CHAIRMAN" => " на смену председателя",
                    "CHANGE_UNION_TYPE" => " на смену формы управления",
                    _ => ""
                };
                if (chatId.HasValue)
                    await SendMessage(chatId.Value,
                        $"<b>{registration.Name}</b>" + Environment.NewLine +
                        $"Адрес: <b>{registration.Address}</b>" + Environment.NewLine +
                        $"Поступила заявка{registrationKindText}. Необходимо проверить документы и подтвердить совпадают ли все данные в справке и заявке.");
            }
        }

        public async Task SendRegistrationRejectedNotification(Registration registration)
        {
            var user = await userSvc.GetUserById(registration.UserId);
            var chatId = (await telegramChatSvc.GetModelByFunc(tc => tc.Phone == user.Phone))?.ChatId;
            if (chatId.HasValue)
            {
                var registrationKindText = registration.RegistrationKind switch
                {
                    "INITIAL" => " на регистрацию",
                    "CHANGE_CHAIRMAN" => " на смену председателя",
                    "CHANGE_UNION_TYPE" => " на смену формы управления",
                    _ => ""
                };
                await SendMessage(chatId.Value,
                    $"Ваша заявка{registrationKindText} <b>{registration.Name}</b> в системе eOsi.kz отклонена. Причина: {registration.RejectReason}");
            }
        }

        public async Task SendOsiCreatedNotification(Registration registration)
        {
            var user = await userSvc.GetUserById(registration.UserId);
            var chatId = (await telegramChatSvc.GetModelByFunc(tc => tc.Phone == user.Phone))?.ChatId;
            if (chatId.HasValue)
                await SendMessage(chatId.Value,
                    $"Ваша заявка на регистрацию <b>{registration.Name}</b> в системе eOsi.kz одобрена и подтверждена. Войдите в личный кабинет в системе и введите полные данные о Вашем ОСИ.");
        }

        public async Task SendActCreatedNotification(int actId)
        {
            using var db = OSIBillingDbContext.DbContext;
            var actInfo = await db.Acts
                .Include(a => a.Osi)
                .ThenInclude(o => o.OsiUsers)
                .Where(a => a.Id == actId)
                .Select(a => new { OsiName = a.Osi.Name, a.Osi.OsiUsers.First().UserId })
                .FirstAsync();
            var user = await userSvc.GetUserById(actInfo.UserId);
            var chatId = (await telegramChatSvc.GetModelByFunc(tc => tc.Phone == user.Phone))?.ChatId;
            if (chatId.HasValue)
                await SendMessage(chatId.Value,
                    $"В вашем личном кабинете eOsi.kz выставлен акт выполненных работ за прошлый месяц по <b>{actInfo.OsiName}</b>. Необходимо зайти в личный кабинет, выбрать раздел \"Бухгалтерские документы\" и подписать акт при помощи ЭЦП. При неподписании акта доступ к полному функционалу будет ограничен, но система eOsi.kz будет продолжать обслуживание в полном обьеме.");
        }

        public async Task SendCallMeBackNotification(CallMeBackNotificationRequest request)
        {
            var operators = await userSvc.GetUsersByRole("OPERATOR");
            foreach (var user in operators)
            {
                var chatId = (await telegramChatSvc.GetModelByFunc(tc => tc.Phone == user.Phone))?.ChatId;
                if (chatId.HasValue)
                    await SendMessage(chatId.Value,
                        "Поступила заявка на консультацию " +
                        (!request.AfterInactivity ? "с основной формы" : "после 20-ти секунд бездействия") +
                        Environment.NewLine + $"Телефон: <b>{request.Phone}</b>" +
                        Environment.NewLine + $"Имя: <b>{request.Name}</b>");
            }
        }

        public async Task SendOsiAccountApplicationNotification(OsiAccountApplication application, bool repeat = false)
        {
            var osi = await osiSvc.GetOsiById(application.OsiId);
            var operators = await userSvc.GetUsersByRole("OPERATOR");
            foreach (var user in operators)
            {
                var chatId = (await telegramChatSvc.GetModelByFunc(tc => tc.Phone == user.Phone))?.ChatId;
                if (chatId.HasValue)
                    await SendMessage(chatId.Value,
                        (!repeat ? "Поступила" : "Ожидает рассмотрения") +
                        " заявка " +
                        application.ApplicationType switch
                        {
                            "ADD" => "на добавление счета",
                            "UPDATE" => "на изменение счета",
                            //"REMOVE" => "на удаление счета",
                            _ => "на изменение счета"
                        } +
                        Environment.NewLine + $"ОСИ: <b>{osi.Name}</b>");
            }
        }
    }
}
