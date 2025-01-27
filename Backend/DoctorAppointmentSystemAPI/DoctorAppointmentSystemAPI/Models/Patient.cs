using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystemAPI.Models
{
    public class Patient
    {
        [Key] // Explicitly mark FirebaseUserId as the primary key
        public string FirebaseUserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public List<Appointment> Appointments { get; set; }
        public List<Review> Reviews { get; set; }
    }
}
