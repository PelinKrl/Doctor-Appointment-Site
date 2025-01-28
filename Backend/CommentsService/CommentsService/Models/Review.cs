using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CommentsService.Models
{
    public class Review
    {
        [BsonId] 
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } 

        [BsonElement("doctorId")]
        public string DoctorId { get; set; }

        [BsonElement("patientId")]
        public string PatientId { get; set; }

        [BsonElement("rating")]
        public int Rating { get; set; }

        [BsonElement("comment")]
        public string Comment { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; 
    }
}