//using Azure.Messaging.ServiceBus;
//using DoctorAppointmentSystemAPI.Services;
//using Microsoft.Extensions.Configuration;

//public class QueueProcessingService : BackgroundService
//{
//    private readonly ServiceBusClient _serviceBusClient;
//    private readonly string _queueName = "incompleteAppointmentsQueue";
//    private readonly IConfiguration _configuration; // Add this field

//    public QueueProcessingService(IConfiguration configuration)
//    {
//        _configuration = configuration; // Initialize the configuration field
//        _serviceBusClient = new ServiceBusClient(configuration["ServiceBusConnectionString"]);
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        var processor = _serviceBusClient.CreateProcessor(_queueName, new ServiceBusProcessorOptions());

//        processor.ProcessMessageAsync += MessageHandler;
//        processor.ProcessErrorAsync += ErrorHandler;

//        await processor.StartProcessingAsync(stoppingToken);
//    }

//    private Task ErrorHandler(ProcessErrorEventArgs arg)
//    {
//        Console.Error.WriteLine(arg.Exception.ToString());
//        return Task.CompletedTask;
//    }

//    private async Task MessageHandler(ProcessMessageEventArgs args)
//    {
//        string userId = args.Message.Body.ToString();
//        // Send email to user
//        await SendEmailToUser(userId);
//        await args.CompleteMessageAsync(args.Message);
//    }

//    private async Task SendEmailToUser(string userId)
//    {
//        var emailService = new EmailService(_configuration); // Use the _configuration field
//        await emailService.SendEmailAsync(userId, "Complete Your Appointment", "Please complete your appointment.");
//    }
//}