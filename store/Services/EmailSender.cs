using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;

namespace store.Services
{
    public class EmailSender : IEmailSender
    {

        private SendGridClient sendGridClient;

        public EmailSender(SendGridClient sendGridClient)
        {
            this.sendGridClient = sendGridClient;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            SendGrid.Helpers.Mail.SendGridMessage message = new SendGrid.Helpers.Mail.SendGridMessage
            {
                From = new SendGrid.Helpers.Mail.EmailAddress("admin@sodastore.codingtemple.com", "Store Admin"),
                Subject = subject,
                HtmlContent = htmlMessage,
                PlainTextContent = htmlMessage
            };

            //If using templates:
            message.TemplateId = "d-3e431049bdd54e0faf4bac6e174d5509";
            message.SetTemplateData(new
            {
                subject = subject,
                body = htmlMessage
            });

            message.SetClickTracking(false, false);

            message.AddTo(email);
            return this.sendGridClient.SendEmailAsync(message);
        }
    }
}
