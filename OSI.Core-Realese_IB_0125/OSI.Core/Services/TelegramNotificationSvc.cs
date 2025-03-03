using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface ITelegramNotificationSvc
    {
        Task SendNotificationAsync(string system, string header, string notification, bool silent);
    }

    public class TelegramNotificationSvc : ITelegramNotificationSvc
    {
        private readonly ILogger _logger;
        private TelegramServiceReference.NotificationServiceClient _Telegram;

        //private TelegramServiceReference.NotificationServiceClient Telegram
        //{
        //    get
        //    {
        //        if (_Telegram == null)
        //        {
        //            _Telegram = new TelegramServiceReference.NotificationServiceClient();
        //            _Telegram.InnerChannel.Closed += TelegramInnerChannel_ClosedOrFaulted;
        //            _Telegram.InnerChannel.Faulted += TelegramInnerChannel_ClosedOrFaulted;
        //        }
        //        return _Telegram;
        //    }
        //}

        public TelegramNotificationSvc()
        {
            _Telegram = new TelegramServiceReference.NotificationServiceClient();
            _Telegram.InnerChannel.Closed += TelegramInnerChannel_ClosedOrFaulted;
            _Telegram.InnerChannel.Faulted += TelegramInnerChannel_ClosedOrFaulted;
        }

        public TelegramNotificationSvc(ILogger<TelegramNotificationSvc> logger) : this()
        {
            _logger = logger;
        }

        private void TelegramInnerChannel_ClosedOrFaulted(object sender, EventArgs e)
        {
            _logger?.LogError("Telegram fault");
            _Telegram = null;
        }

        public async Task SendNotificationAsync(string system, string header, string notification, bool silent)
        {
            _logger?.LogInformation($"Отправка уведомления: Header='{header}' Notification='{notification}' Silent={silent}");
            await _Telegram.SendNotificationAsync(system, header, notification, silent);
        }
    }
}
