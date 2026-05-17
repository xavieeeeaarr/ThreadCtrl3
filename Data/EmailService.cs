using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace ThrdCtrl2.Data
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailSettings = _config.GetSection("EmailSettings");
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailSettings["FromName"], emailSettings["FromEmail"]));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            message.Body = new TextPart("html")
            {
                Text = body
            };

            using var client = new SmtpClient();
            try
            {
                var server = emailSettings["SmtpServer"] ?? "smtp.gmail.com";
                var port = int.Parse(emailSettings["SmtpPort"] ?? "587");
                var user = emailSettings["SmtpUser"] ?? "";
                var pass = emailSettings["SmtpPass"] ?? "";

                await client.ConnectAsync(server, port, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(user, pass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (System.Exception ex)
            {
                // In a real app, log this error. 
                // For now, we'll just throw it or handle it in the controller.
                System.Diagnostics.Debug.WriteLine($"Email sending failed: {ex.Message}");
                throw;
            }
        }
    }
}
