using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailService(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var smtpClient = new SmtpClient(_smtpSettings.IsDevelopment ? _smtpSettings.DevSettings.Host : _smtpSettings.ProdSettings.Host)
            {
                Port = _smtpSettings.IsDevelopment ? _smtpSettings.DevSettings.Port : _smtpSettings.ProdSettings.Port,
                Credentials = new NetworkCredential(
                    _smtpSettings.IsDevelopment ? _smtpSettings.DevSettings.UserName : _smtpSettings.ProdSettings.UserName,
                    _smtpSettings.IsDevelopment ? _smtpSettings.DevSettings.Password : _smtpSettings.ProdSettings.Password),
                EnableSsl = _smtpSettings.IsDevelopment ? _smtpSettings.DevSettings.EnableSSL : _smtpSettings.ProdSettings.EnableSSL,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.IsDevelopment ? _smtpSettings.DevSettings.UserName : _smtpSettings.ProdSettings.UserName),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
