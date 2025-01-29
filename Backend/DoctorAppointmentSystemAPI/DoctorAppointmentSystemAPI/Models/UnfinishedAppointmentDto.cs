public class UnfinishedAppointmentDto
{
    public int UserId { get; set; }
    public int DoctorId { get; set; }
    public DateTime SelectedDateTime { get; set; }
    public string Email { get; set; } // For sending notifications
}
