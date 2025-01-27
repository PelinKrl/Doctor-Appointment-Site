namespace DoctorAppointmentSystemAPI.Models
{
    public class RegisterPatientDTO
    {
        
        public string FullName { get; set; }
        public string Email { get; set; } // Email is optional since it can come from Firebase claims  

    }
}
