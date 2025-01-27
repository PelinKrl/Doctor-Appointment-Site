namespace DoctorAppointmentSystemAPI.Models
{

    public class Availability
    {
        public int Id { get; set; } // Primary key
        public DayOfWeek Day { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
    public class Doctor
    {
        public int Id { get; set; }
        public string FirebaseUserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Specialty { get; set; }
        public List<Availability> Availability { get; set; }  // This could be further broken down into more specific properties
        public string Address { get; set; }
        public bool IsApproved { get; set; }
        // public string GoogleToken { get; set; }

        public List<Availability> GetAvailabilityForCurrentWeek()
        {
            // Logic to fetch or calculate availability
            return new List<Availability>();
        }

        public List<Appointment> Appointments { get; set; }

    }
}
