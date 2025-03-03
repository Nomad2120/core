using Microsoft.Extensions.Logging;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace OSI.Core.Extensions
{
    public static class BotExtensions
    {
        public async static Task<Message> SendTextMessageToChatAsync(this ITelegramBotClient bot,
            ChatId chatId, string message, ParseMode parseMode = ParseMode.Html,
            bool disableWebPagePreview = false, bool disableNotification = false,
            int replyToMessageId = 0, IReplyMarkup replyMarkup = null,
            ILogger logger = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await bot.SendChatActionAsync(chatId, ChatAction.Typing, cancellationToken: cancellationToken);
                return await bot.SendTextMessageAsync(chatId, message,
                    parseMode: parseMode,
                    disableWebPagePreview: disableWebPagePreview,
                    disableNotification: disableNotification,
                    replyToMessageId: replyToMessageId,
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Telegram error");
            }
            return null;
        }

        public async static Task<Message> SendDocumentToChatAsync(this ITelegramBotClient bot,
            ChatId chatId, InputOnlineFile inputFile,
            string caption = null, ParseMode parseMode = ParseMode.Html,
            bool disableNotification = false,
            int replyToMessageId = 0, IReplyMarkup replyMarkup = null,
            ILogger logger = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await bot.SendChatActionAsync(chatId, ChatAction.UploadDocument, cancellationToken: cancellationToken);
                return await bot.SendDocumentAsync(chatId, inputFile,
                    caption: caption,
                    parseMode: parseMode,
                    disableNotification: disableNotification,
                    replyToMessageId: replyToMessageId,
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Telegram error");
            }
            return null;
        }

        public static Task<Message> SendDocumentToChatAsync(this ITelegramBotClient bot,
            ChatId chatId, Stream inputFileStream, string inputFileName = null,
            string caption = null, ParseMode parseMode = ParseMode.Html,
            bool disableNotification = false,
            int replyToMessageId = 0, IReplyMarkup replyMarkup = null,
            ILogger logger = null,
            CancellationToken cancellationToken = default)
        {
            return bot.SendDocumentToChatAsync(chatId, string.IsNullOrEmpty(inputFileName) ? new InputOnlineFile(inputFileStream) : new InputOnlineFile(inputFileStream, inputFileName), caption, parseMode, disableNotification, replyToMessageId, replyMarkup, logger, cancellationToken: cancellationToken);
        }

        public async static Task EditMessageTextAsync(this ITelegramBotClient bot,
            ChatId chatId, int messageId, string text, ParseMode parseMode = ParseMode.Html,
            bool disableWebPagePreview = false,
            InlineKeyboardMarkup replyMarkup = null,
            ILogger logger = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await TelegramBotClientExtensions.EditMessageTextAsync(bot, chatId, messageId, text,
                    parseMode: parseMode,
                    disableWebPagePreview: disableWebPagePreview,
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Telegram error");
            }
        }

        public async static Task EditMessageTextAsync(this ITelegramBotClient bot,
            Message message, string text, ParseMode parseMode = ParseMode.Html,
            bool disableWebPagePreview = false,
            InlineKeyboardMarkup replyMarkup = null,
            ILogger logger = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, text,
                    parseMode: parseMode,
                    disableWebPagePreview: disableWebPagePreview,
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Telegram error");
            }
        }

        public async static Task SafeDeleteMessageAsync(this ITelegramBotClient bot,
            ChatId chatId, int messageId,
            ILogger logger = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await bot.DeleteMessageAsync(chatId, messageId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Telegram error");
            }
        }
    }
}
