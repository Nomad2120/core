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
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Linq.Expressions;

namespace OSI.Core.Services
{
    public interface ITelegramSubscriptionBotSvc
    {
        //Task SendInvoices();
        void RestartBot();

        Task SendPdfForAllSubscriptions();
        Task SendPdfForOsiSubscriptions(int osiId);
    }

    public class TelegramSubscriptionBotSvc : ITelegramSubscriptionBotSvc
    {
        private readonly IConfiguration configuration;
        private readonly IModelService<OSIBillingDbContext, TelegramSubscription> telegramSubscriptionSvc;
        private readonly IAbonentSvc abonentSvc;
        private readonly IOsiSvc osiSvc;
        private readonly IPrintInvoiceSvc printInvoiceSvc;
        private readonly IWebHostEnvironment env;
        private readonly ILogger<TelegramSubscriptionBotSvc> logger;
        private ITelegramBotClient BotClient;
        private string _BotServiceResponseHeader = "<b>OSI.Subscription.Bot</b>";
        private Task receivingTask = null;
        private CancellationTokenSource cancellationTokenSource = null;
        private bool throwOutPendingUpdates = false;
        private bool ThrowOutPendingUpdates => throwOutPendingUpdates ? !(throwOutPendingUpdates = false) : throwOutPendingUpdates;
        private readonly ConcurrentDictionary<long, (int MessageId, int OsiId)> WaitingFlat = new();
        private readonly ConcurrentDictionary<long, int> WaitingNum = new();

        internal static bool DoNotStart = false;

        public TelegramSubscriptionBotSvc(IConfiguration configuration,
            IModelService<OSIBillingDbContext, TelegramSubscription> telegramSubscriptionSvc,
            IAbonentSvc abonentSvc, IOsiSvc osiSvc, IPrintInvoiceSvc printInvoiceSvc,
            IWebHostEnvironment env,
            ILogger<TelegramSubscriptionBotSvc> logger)
        {
            this.configuration = configuration;
            this.telegramSubscriptionSvc = telegramSubscriptionSvc;
            this.abonentSvc = abonentSvc;
            this.osiSvc = osiSvc;
            this.printInvoiceSvc = printInvoiceSvc;
            this.env = env;
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
                BotClient = new TelegramBotClient(configuration["SubscriptionBot:Token"]);
                _BotServiceResponseHeader = $"<b>{(await BotClient.GetMeAsync()).FirstName}</b>";
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

        private async Task SendError(ITelegramBotClient bot, long chatId, Exception ex, CancellationToken cancellationToken)
        {
            await bot.SendTextMessageToChatAsync(chatId,
                _BotServiceResponseHeader + Environment.NewLine + "Произошла ошибка. Попробуйте еще раз." +
                (env.IsDevelopment() && ex != null ? Environment.NewLine + ex.ToString() : null),
                logger: logger, cancellationToken: cancellationToken);
        }

        private async Task SendFatalError(ITelegramBotClient bot, long chatId, Exception ex, CancellationToken cancellationToken)
        {
            logger.LogError(ex, "SendFatalError {chatId}", chatId);
            await bot.SendTextMessageToChatAsync(chatId,
                _BotServiceResponseHeader + Environment.NewLine + "Произошла ошибка. Обратитесь в своё ОСИ." +
                (env.IsDevelopment() && ex != null ? Environment.NewLine + ex.ToString() : null),
                logger: logger, cancellationToken: cancellationToken);
        }

        private async Task ShowError(ITelegramBotClient bot, long chatId, int messageId, Exception ex, CancellationToken cancellationToken)
        {
            await bot.EditMessageTextAsync(chatId, messageId,
                _BotServiceResponseHeader + Environment.NewLine + "Произошла ошибка. Попробуйте еще раз." +
                (env.IsDevelopment() && ex != null ? Environment.NewLine + ex.ToString() : null),
                logger: logger, cancellationToken: cancellationToken);
        }

        private async Task ShowFatalError(ITelegramBotClient bot, long chatId, int messageId, Exception ex, CancellationToken cancellationToken)
        {
            logger.LogError(ex, "ShowFatalError {chatId}", chatId);
            await bot.EditMessageTextAsync(chatId, messageId,
                _BotServiceResponseHeader + Environment.NewLine + "Произошла ошибка. Обратитесь в своё ОСИ." +
                (env.IsDevelopment() && ex != null ? Environment.NewLine + ex.ToString() : null),
                logger: logger, cancellationToken: cancellationToken);
        }

        private async Task ShowCallbackError(ITelegramBotClient bot, long chatId, Update update, Exception ex,
            CancellationToken cancellationToken)
        {
            await bot.EditMessageTextAsync(update.CallbackQuery.Message,
                _BotServiceResponseHeader + Environment.NewLine + "Произошла ошибка. Попробуйте еще раз." +
                (env.IsDevelopment() && ex != null ? Environment.NewLine + ex.ToString() : null),
                replyMarkup: await CreateMenu(chatId), logger: logger, cancellationToken: cancellationToken);
        }

        private async Task ShowCallbackFatalError(ITelegramBotClient bot, long chatId, Update update, Exception ex,
            CancellationToken cancellationToken)
        {
            await bot.EditMessageTextAsync(update.CallbackQuery.Message,
                _BotServiceResponseHeader + Environment.NewLine + "Произошла ошибка. Обратитесь в своё ОСИ." +
                (env.IsDevelopment() && ex != null ? Environment.NewLine + ex.ToString() : null),
                replyMarkup: await CreateMenu(chatId), logger: logger, cancellationToken: cancellationToken);
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
                        bool removeWaitingFlat = true;
                        bool removeWaitingNum = true;
                        if (update.Message.Text.StartsWith("/start"))
                        {
                            if (update.Message.Text.StartsWith("/start "))
                            {
                                if (int.TryParse(update.Message.Text["/start ".Length..], out int osiId))
                                {
                                    try
                                    {
                                        if (!await CheckSubscriptionsByOsi(chatId, osiId))
                                        {
                                            var osi = await osiSvc.GetOsiById(osiId);
                                            await bot.SendTextMessageToChatAsync(chatId,
                                                _BotServiceResponseHeader + Environment.NewLine +
                                                $"Ваше ОСИ: <b>{osi.Name}</b>" + Environment.NewLine +
                                                $"Адрес: <b>{osi.Address}</b>",
                                                logger: logger, cancellationToken: cancellationToken);
                                            var message = await bot.SendTextMessageToChatAsync(chatId,
                                                _BotServiceResponseHeader + Environment.NewLine +
                                                $"Введите номер Вашей квартиры",
                                                logger: logger, cancellationToken: cancellationToken);
                                            WaitingFlat.AddOrUpdate(chatId,
                                                (_) => (message.MessageId, osiId),
                                                (_, _) => (message.MessageId, osiId));
                                            removeWaitingFlat = false;
                                        }
                                        else
                                        {
                                            await SendMenuToChatAsync(chatId, bot, logger, cancellationToken);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        await SendFatalError(bot, chatId, ex, cancellationToken);
                                        throw;
                                    }
                                }
                            }
                            else
                            {
                                await bot.SendTextMessageToChatAsync(chatId,
                                    _BotServiceResponseHeader + Environment.NewLine +
                                    $"Чтобы начать работу отсканируйте QR код вашего ОСИ",
                                    logger: logger, cancellationToken: cancellationToken);
                            }
                        }
                        else if (update.Message.Text.StartsWith("/menu"))
                        {
                            await SendMenuToChatAsync(chatId, bot, logger, cancellationToken);
                        }
                        else
                        {
                            if (!update.Message.From.IsBot)
                            {
                                if (WaitingFlat.TryGetValue(chatId, out var waitingFlat))
                                {
                                    try
                                    {
                                        var flat = update.Message.Text.Trim();
                                        await bot.SafeDeleteMessageAsync(chatId, update.Message.MessageId,
                                            logger: logger, cancellationToken: cancellationToken);
                                        await bot.EditMessageTextAsync(chatId, waitingFlat.MessageId,
                                            _BotServiceResponseHeader + Environment.NewLine +
                                            $"Пожалуйста, подождите...",
                                            logger: logger, cancellationToken: cancellationToken);
                                        var abonent = await osiSvc.GetAbonentByOsiIdAndFlat(waitingFlat.OsiId, flat);
                                        if (abonent != null)
                                        {
                                            var abonentNum = abonent.ErcAccount ?? abonent.Id.ToString();
                                            await AddSubscription(chatId, abonent.Id);
                                            await bot.EditMessageTextAsync(chatId, waitingFlat.MessageId,
                                                _BotServiceResponseHeader + Environment.NewLine +
                                                $"Лицевой счет {abonentNum} зарегистрирован на Ваш Telegram" + Environment.NewLine +
                                                $"Теперь вы будете ежемесячно получать электронную квитанцию по данному лицевому счету",
                                                logger: logger, cancellationToken: cancellationToken);
                                            WaitingFlat.TryRemove(chatId, out _);
                                            var response = await printInvoiceSvc.GetInvoiceByAbonentIdOnCurrentDate(abonent.Id);
                                            if (response.Code == 0)
                                            {
                                                await bot.SendDocumentToChatAsync(chatId,
                                                    new MemoryStream(response.Result), $"{DateTime.Today:yyyy-MM-dd}_{abonentNum}.pdf",
                                                    replyMarkup: CreatePayButton(abonentNum),
                                                    logger: logger, cancellationToken: cancellationToken);
                                            }
                                            await SendMenuToChatAsync(chatId, bot, logger: logger, cancellationToken: cancellationToken);
                                        }
                                        else
                                        {
                                            await bot.EditMessageTextAsync(chatId, waitingFlat.MessageId,
                                                _BotServiceResponseHeader + Environment.NewLine +
                                                $"Квартира с номером <b>{flat}</b> не найдена" + Environment.NewLine +
                                                $"Введите номер Вашей квартиры",
                                                logger: logger, cancellationToken: cancellationToken);
                                            removeWaitingFlat = false;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        await ShowFatalError(bot, chatId, waitingFlat.MessageId, ex, cancellationToken);
                                        throw;
                                    }
                                }
                                else if (WaitingNum.TryGetValue(chatId, out var messageId))
                                {
                                    try
                                    {
                                        var abonentNum = update.Message.Text.Trim();
                                        await bot.SafeDeleteMessageAsync(chatId, update.Message.MessageId,
                                            logger: logger, cancellationToken: cancellationToken);
                                        await bot.EditMessageTextAsync(chatId, messageId,
                                            _BotServiceResponseHeader + Environment.NewLine +
                                            $"Пожалуйста, подождите...",
                                            logger: logger, cancellationToken: cancellationToken);
                                        var abonent = await abonentSvc.GetAbonentForPaymentService(abonentNum);
                                        if (abonent != null)
                                        {
                                            abonentNum = abonent.ErcAccount ?? abonent.Id.ToString();
                                            await AddSubscription(chatId, abonent.Id);
                                            await bot.EditMessageTextAsync(chatId, messageId,
                                                _BotServiceResponseHeader + Environment.NewLine +
                                                $"Лицевой счет {abonentNum} зарегистрирован на Ваш Telegram" + Environment.NewLine +
                                                $"Теперь вы будете ежемесячно получать электронную квитанцию по данному лицевому счету",
                                                logger: logger, cancellationToken: cancellationToken);
                                            WaitingNum.TryRemove(chatId, out _);
                                            var response = await printInvoiceSvc.GetInvoiceByAbonentIdOnCurrentDate(abonent.Id);
                                            if (response.Code == 0)
                                            {
                                                await bot.SendDocumentToChatAsync(chatId,
                                                    new MemoryStream(response.Result), $"{DateTime.Today:yyyy-MM-dd}_{abonentNum}.pdf",
                                                    replyMarkup: CreatePayButton(abonentNum),
                                                    logger: logger, cancellationToken: cancellationToken);
                                            }
                                            await SendMenuToChatAsync(chatId, bot, logger: logger, cancellationToken: cancellationToken);
                                        }
                                        else
                                        {
                                            await bot.EditMessageTextAsync(chatId, messageId,
                                                _BotServiceResponseHeader + Environment.NewLine +
                                                $"Лицевой счет <b>{abonentNum}</b> не найден" + Environment.NewLine +
                                                $"Введите номер лицевого счета",
                                                logger: logger, cancellationToken: cancellationToken);
                                            removeWaitingNum = false;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        await ShowFatalError(bot, chatId, messageId, ex, cancellationToken);
                                        throw;
                                    }
                                }
                                else
                                {
                                    await bot.SafeDeleteMessageAsync(chatId, update.Message.MessageId,
                                        logger: logger, cancellationToken: cancellationToken);
                                }
                            }
                        }
                        if (removeWaitingFlat)
                        {
                            WaitingFlat.TryRemove(chatId, out _);
                        }
                        if (removeWaitingNum)
                        {
                            WaitingNum.TryRemove(chatId, out _);
                        }
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
            else if (update.Type == UpdateType.CallbackQuery)
            {
                try
                {
                    var chatId = update.CallbackQuery.Message.Chat.Id;
                    string[] callbackData = update.CallbackQuery.Data.Split('-');
                    switch (callbackData[0])
                    {
                        case "menu":
                            await bot.EditMessageTextAsync(update.CallbackQuery.Message,
                                _BotServiceResponseHeader + Environment.NewLine + "Список зарегистрированных лицевых счетов",
                                replyMarkup: await CreateMenu(chatId), logger: logger, cancellationToken: cancellationToken);
                            break;
                        case "abonent":
                            try
                            {
                                int abonentId = int.Parse(callbackData[1]);
                                string abonentNum = callbackData[2];
                                await bot.EditMessageTextAsync(update.CallbackQuery.Message,
                                    _BotServiceResponseHeader + Environment.NewLine + "Лицевой счет " + abonentNum,
                                    replyMarkup: CreateMenuForAbonent(abonentId, abonentNum), logger: logger, cancellationToken: cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, $"{update.CallbackQuery.Data} CallbackError");
                                await ShowCallbackError(bot, chatId, update, ex, cancellationToken);
                            }
                            break;
                        case "getpdf":
                            await bot.EditMessageTextAsync(update.CallbackQuery.Message,
                                _BotServiceResponseHeader + Environment.NewLine + "Пожалуйста подождите...",
                                logger: logger, cancellationToken: cancellationToken);
                            try
                            {
                                int abonentId = int.Parse(callbackData[1]);
                                string abonentNum = callbackData[2];
                                var response = await printInvoiceSvc.GetInvoiceByAbonentIdOnCurrentDate(abonentId);
                                if (response.Code == 0)
                                {
                                    await bot.SendDocumentToChatAsync(chatId,
                                        new MemoryStream(response.Result), $"{DateTime.Today:yyyy-MM-dd}_{abonentNum}.pdf",
                                        replyMarkup: CreatePayButton(abonentNum),
                                        logger: logger, cancellationToken: cancellationToken);
                                    await bot.SafeDeleteMessageAsync(chatId, update.CallbackQuery.Message.MessageId,
                                        logger: logger, cancellationToken: cancellationToken);
                                    await SendMenuToChatAsync(chatId, bot, logger: logger, cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    string errorMessage = $"{response.Code} {response.Message}";
                                    logger.LogError($"{update.CallbackQuery.Data} CallbackError: " + errorMessage);
                                    if (response.Code == -1)
                                    {
                                        await ShowCallbackFatalError(bot, chatId, update, new Exception(errorMessage),
                                            cancellationToken);
                                    }
                                    else
                                    {
                                        await ShowCallbackError(bot, chatId, update, new Exception(errorMessage),
                                            cancellationToken);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, $"{update.CallbackQuery.Data} CallbackError");
                                await ShowCallbackError(bot, chatId, update, ex, cancellationToken);
                            }
                            break;
                        case "sub":
                            var message = await bot.SendTextMessageToChatAsync(chatId,
                                _BotServiceResponseHeader + Environment.NewLine +
                                $"Введите номер лицевого счета",
                                logger: logger, cancellationToken: cancellationToken);
                            WaitingNum.AddOrUpdate(chatId, (_) => message.MessageId, (_, _) => message.MessageId);
                            break;
                        case "unsub":
                            try
                            {
                                int abonentId = int.Parse(callbackData[1]);
                                string abonentNum = callbackData[2];
                                await RemoveSubscription(chatId, abonentId);
                                await bot.SendTextMessageToChatAsync(chatId,
                                    _BotServiceResponseHeader + Environment.NewLine + $"Лицевой счет {abonentNum} больше не зарегистрирован на Ваш Telegram",
                                    logger: logger, cancellationToken: cancellationToken);
                                await bot.SafeDeleteMessageAsync(chatId, update.CallbackQuery.Message.MessageId,
                                    logger: logger, cancellationToken: cancellationToken);
                                await SendMenuToChatAsync(chatId, bot, logger: logger, cancellationToken: cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, $"{update.CallbackQuery.Data} CallbackError");
                                await ShowCallbackError(bot, chatId, update, ex, cancellationToken);
                            }
                            break;
                        default:
                            break;
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
                ApiRequestException apiRequestException => $"Telegram API Error [{apiRequestException.ErrorCode}]",
                _ => "Telegram Polling Error"
            };
            logger.LogError(ex, ErrorMessage);
            return Task.CompletedTask;
        }

        private async Task<bool> CheckSubscriptionsByOsi(long chatId, int osiId)
        {
            return await telegramSubscriptionSvc.HasModels(ts => ts.ChatId == chatId && ts.Abonent.OsiId == osiId);
        }

        private async Task AddSubscription(long chatId, int abonentId)
        {
            if (await telegramSubscriptionSvc.GetModelByFunc(ts => ts.ChatId == chatId && ts.AbonentId == abonentId) == null)
            {
                var subscription = new TelegramSubscription { ChatId = chatId, AbonentId = abonentId, Dt = DateTime.Now };
                await telegramSubscriptionSvc.AddOrUpdateModel(subscription);
            }
        }

        private async Task RemoveSubscription(long chatId, int abonentId)
        {
            TelegramSubscription subscription = await telegramSubscriptionSvc.GetModelByFunc(ts => ts.ChatId == chatId && ts.AbonentId == abonentId);
            if (subscription != null)
            {
                await telegramSubscriptionSvc.RemoveModel(subscription);
            }
        }

        #region Menu
        private async Task SendMenuToChatAsync(long chatId, ITelegramBotClient bot, ILogger logger, CancellationToken cancellationToken)
        {
            var menu = await CreateMenu(chatId);
            if (menu.InlineKeyboard.Any())
            {
                await bot.SendTextMessageToChatAsync(chatId, _BotServiceResponseHeader + Environment.NewLine + "Список зарегистрированных лицевых счетов", replyMarkup: menu, logger: logger, cancellationToken: cancellationToken);
            }
            else
            {
                await bot.SendTextMessageToChatAsync(chatId, _BotServiceResponseHeader + Environment.NewLine + "У вас нет зарегистрированных лицевых счетов", logger: logger, cancellationToken: cancellationToken);
            }
        }

        private async Task<InlineKeyboardMarkup> CreateMenu(long chatId)
        {
            var abonents = await telegramSubscriptionSvc.GetModelsByQuery(ts => ts.Where(x => x.ChatId == chatId).Select(x => new { Id = x.AbonentId, Num = x.Abonent.ErcAccount ?? x.Abonent.Id.ToString() }));
            var menu = abonents.Select(abonent => new[] {
                InlineKeyboardButton.WithCallbackData(
                    abonent.Num ?? abonent.Id.ToString(),
                    $"abonent-{abonent.Id}-{abonent.Num ?? abonent.Id.ToString()}")
            }).ToList();
            menu.Add(new[] {
                InlineKeyboardButton.WithCallbackData(
                    "Зарегистрировать лицевой счет",
                    $"sub")
            });
            return new(menu);
        }

        private static InlineKeyboardMarkup CreateMenuForAbonent(int abonentId, string abonentNum) =>
            new(new InlineKeyboardButton[][]
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Получить электронную квитацию", $"getpdf-{abonentId}-{abonentNum}"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithUrl("Оплатить в Kaspi.kz", $"https://kaspi.kz/pay/NurTau?5742={abonentNum}"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Отменить регистрацию", $"unsub-{abonentId}-{abonentNum}"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Назад", "menu")
                },
            });

        private static InlineKeyboardMarkup CreatePayButton(string abonentNum) =>
            new(new InlineKeyboardButton[][]
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithUrl("Оплатить в Kaspi.kz", $"https://kaspi.kz/pay/NurTau?5742={abonentNum}"),
                },
            });
        #endregion

        public Task SendPdfForAllSubscriptions() => SendPdfForSubscriptions(x => x.Abonent.Osi.IsLaunched);

        public Task SendPdfForOsiSubscriptions(int osiId) => SendPdfForSubscriptions(x => x.Abonent.OsiId == osiId);

        private async Task SendPdfForSubscriptions(Expression<Func<TelegramSubscription, bool>> predicate)
        {
            var subscriptions = await telegramSubscriptionSvc
                .GetModelsByQuery(ts => ts
                .Where(predicate)
                .Select(x => new
                {
                    x.ChatId,
                    x.AbonentId,
                    AbonentNum = x.Abonent.ErcAccount ?? x.Abonent.Id.ToString(),
                    OsiName = x.Abonent.Osi.Name,
                    x.Abonent.Flat
                }));
            foreach (var subscription in subscriptions)
            {
                var response = await printInvoiceSvc.GetInvoiceByAbonentIdOnCurrentDate(subscription.AbonentId);
                if (response.Code == 0)
                {
                    await BotClient.SendDocumentToChatAsync(subscription.ChatId,
                        new MemoryStream(response.Result), $"{DateTime.Today:yyyy-MM-dd}_{subscription.AbonentNum}.pdf",
                        $"Выставлена новая квитанция {subscription.OsiName}, квартира {subscription.Flat}",
                        replyMarkup: CreatePayButton(subscription.AbonentNum),
                        logger: logger);
                }
            }
        }
    }
}
