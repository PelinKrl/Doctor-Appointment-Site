//using Azure.Messaging.ServiceBus;
//using Microsoft.AspNetCore.Mvc;
//using System.Text.Json;

//[ApiController]
//[Route("api/notifications")]
//public class NotificationsController : ControllerBase
//{
//    private readonly ServiceBusClient _serviceBusClient;
//    private readonly string _queueName = "incompleteAppointmentsQueue";

//    public NotificationsController(IConfiguration configuration)
//    {
//        _serviceBusClient = new ServiceBusClient(configuration["ServiceBusConnectionString"]);
//    }

//    [HttpPost("incomplete")]
//    public async Task<IActionResult> NotifyIncompleteAppointment([FromBody] NotificationMessage message)
//    {
//        if (string.IsNullOrEmpty(message.PatientId) || string.IsNullOrEmpty(message.DoctorId))
//        {
//            return BadRequest("Patient ID and Doctor ID are required.");
//        }

//        var sender = _serviceBusClient.CreateSender(_queueName);
//        var serviceBusMessage = new ServiceBusMessage(JsonSerializer.Serialize(message));
//        await sender.SendMessageAsync(serviceBusMessage);
//        await sender.DisposeAsync();

//        return Ok("Notification enqueued successfully.");
//    }

//    public class NotificationMessage
//    {
//        public string PatientId { get; set; }
//        public string DoctorId { get; set; } // Add this property
//        public string Message { get; set; }
//    }
//}