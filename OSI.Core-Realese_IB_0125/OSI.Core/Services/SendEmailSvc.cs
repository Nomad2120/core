using ESoft.CommonLibrary;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface ISendEmailSvc
    {
        Task<State> SendEmail(string emails, string subject, string message, string attachmentName = null, Stream stream = null);
    }

    public class SendEmailSvc : ISendEmailSvc
    {
        private readonly ILogger _logger;

        public SendEmailSvc(ILogger<SendEmailSvc> logger)
        {
            _logger = logger;
        }

        public async Task<State> SendEmail(string emails, string subject, string message, string attachmentName = null, Stream stream = null)
        {
            void Send()
            {
                _logger.LogInformation($"subject: '{subject}', emails: '{emails}', message: '{message}'");
                using (MailMessage mail = new MailMessage("reestr@posterc.kz", emails, subject, message))
                {
                    using (SmtpClient client = new SmtpClient("10.1.1.8", 25))
                    {
                        client.EnableSsl = false;
                        client.Credentials = new NetworkCredential("reestr@posterc.kz", "tUO56E2220@~j@E");
                        //client.DeliveryMethod = SmtpDeliveryMethod.Network;
                        //client.UseDefaultCredentials = true;
                        if (!string.IsNullOrEmpty(attachmentName))
                        { 
                            if (stream != null)
                            {
                                mail.Attachments.Add(new Attachment(stream, attachmentName));
                            }
                            else
                            {
                                mail.Attachments.Add(new Attachment(attachmentName));
                            }
                        }
                        client.Send(mail);
                    }
                }
            }

            State state = new State();
            try
            {
                await Task.Run(() => Send());
                state.Message = "Выполнено";
            }
            catch
            {
                _logger.LogInformation($"Вторая попытка отправки");
                try
                {
                    await Task.Run(() => Send());
                    state.Message = "Выполнено";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"subject: '{subject}', emails: '{emails}', message: '{message}'");
                    state.Code = -1;
                    state.Message = ex.Message;
                }
            }

            return state;
        }
    }
}

