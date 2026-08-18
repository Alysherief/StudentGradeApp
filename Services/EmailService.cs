using SendGrid;
using SendGrid.Helpers.Mail;
using StudentGradeApp.Interfaces;
using System.Net.Mail;

namespace StudentGradeApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(
            string email,
            string subject,
            string htmlMessage)
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            var fromEmail = _configuration["SendGrid:FromEmail"];
            var fromName = _configuration["SendGrid:FromName"];

            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(
                fromEmail,
                fromName);

            var to = new EmailAddress(email);

            var message = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                string.Empty,
                htmlMessage);

            var response = await client.SendEmailAsync(message);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"SendGrid failed to send the email. Status code: {response.StatusCode}");
            }
        }
    }
}