using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;
public class UnfinishedAppointmentProcessor : BackgroundService
{
    private readonly RabbitMQService _rabbitMQService;
    private RabbitMQ.Client.IModel _channel;
    private EventingBasicConsumer _consumer;

    public UnfinishedAppointmentProcessor(RabbitMQService rabbitMQService)
    {
        _rabbitMQService = rabbitMQService;
        _channel = _rabbitMQService.CreateChannel();
        _consumer = _rabbitMQService.CreateConsumer(_channel, HandleMessage);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.BasicConsume(queue: "unfinished_appointments", autoAck: true, consumer: _consumer);
        return Task.CompletedTask;
    }

    private void HandleMessage(object sender, BasicDeliverEventArgs args)
    {
        var body = args.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var appointment = JsonSerializer.Deserialize<UnfinishedAppointmentDto>(message);

        if (appointment != null)
        {
            SendNotification(appointment);
        }
    }

    private async Task SendNotification(UnfinishedAppointmentDto appointment)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Doctor Appointment System", "doctorappointmentsystem1@gmail.com"));  // Sender email
            message.To.Add(new MailboxAddress("", appointment.Email));  // Patient email
            message.Subject = "Reminder: Finish Your Appointment Booking";

            var bodyBuilder = new BodyBuilder
            {
                TextBody = $"Dear User,\n\nYou started booking an appointment for {appointment.SelectedDateTime}, but you didn’t complete it.\n\nClick here to complete: [LINK]\n\nBest Regards,\nDoctor Appointment System"
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var smtpClient = new SmtpClient();
            smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await smtpClient.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.Auto);
            await smtpClient.AuthenticateAsync("doctorappointmentsystem1@gmail.com", "gbes mpcb ngbw dmpf"); // Use valid credentials
            await smtpClient.SendAsync(message);
            await smtpClient.DisconnectAsync(true);

            Console.WriteLine($"Reminder email successfully sent to {appointment.Email}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
        }
    }

    public override void Dispose()
    {
        _channel.Close();
        base.Dispose();
    }
}
