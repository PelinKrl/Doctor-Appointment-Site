using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace DoctorAppointmentSystemAPI.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }

   
public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(_configuration["Email:Username"], _configuration["Email:Password"]),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["Email:Username"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(to);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log or handle exceptions here
                throw;
            }
        }
    }
}

