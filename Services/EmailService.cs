using System.Net;
using System.Net.Mail;

namespace DFindApi.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            var smtpSection = _config.GetSection("Smtp");
            var host = smtpSection["Host"];
            var port = int.Parse(smtpSection["Port"] ?? "587");
            var enableSsl = bool.Parse(smtpSection["EnableSsl"] ?? "true");
            var user = smtpSection["User"];
            var password = smtpSection["Password"];
            var fromName = smtpSection["FromName"] ?? "DFind App";

            using var client = new SmtpClient(host, port)
            {
                Host = host,
                Port = port,
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,  
                Credentials = new NetworkCredential(user, password)
            };

            using var msg = new MailMessage
            {
                From = new MailAddress(user!, fromName),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            };

            msg.To.Add(destinatario);

            await client.SendMailAsync(msg);
        }
    }
}
