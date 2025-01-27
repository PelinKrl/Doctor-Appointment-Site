using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DoctorAppointmentSystemAPI.Models
{
    public class Review
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } // MongoDB ObjectId as string

        public string DoctorId { get; set; } // Relational ID from SQL
        public string PatientId { get; set; } // Relational ID from SQL
        public string Comment { get; set; }
        public int Rating { get; set; }
    }
}