using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/appointments")]
public class AppointmentController : ControllerBase
{
    private readonly RabbitMQService _rabbitMQService;

    public AppointmentController(RabbitMQService rabbitMQService)
    {
        _rabbitMQService = rabbitMQService;
    }

    [HttpPost("queue-unfinished")]
    public IActionResult QueueUnfinishedAppointment([FromBody] UnfinishedAppointmentDto appointment)
    {
        if (appointment == null || string.IsNullOrEmpty(appointment.Email))
        {
            return BadRequest("Invalid appointment details.");
        }

        _rabbitMQService.PublishMessage(appointment);
        return Ok("Appointment queued for follow-up.");
    }
}
