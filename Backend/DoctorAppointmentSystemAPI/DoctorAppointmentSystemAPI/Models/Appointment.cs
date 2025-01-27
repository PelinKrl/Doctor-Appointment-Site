
namespace DoctorAppointmentSystemAPI.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public string PatientId { get; set; }
        public DateTime AppointmentDate { get; set; }

       
    }
}
