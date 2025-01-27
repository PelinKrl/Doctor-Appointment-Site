namespace DoctorAppointmentSystemAPI.Models
{
    public class RegisterDoctorDTO
    {
       
        public string FullName { get; set; }
        public string Specialty { get; set; }  // Renamed from "AreaOfInterest" to match Doctor model
        public List<AvailabilityDTO> Availability { get; set; } // Structured slots
        public string Address { get; set; }
        public string GoogleToken { get; set; }
    }

    public class AvailabilityDTO
    {
        public DayOfWeek Day { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}