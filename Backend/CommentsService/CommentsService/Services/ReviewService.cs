using MongoDB.Driver;
using CommentsService.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace CommentsService.Services
{
    public class ReviewService
    {
        private readonly IMongoCollection<Review> _reviews;

        public ReviewService(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _reviews = database.GetCollection<Review>(settings.Value.CollectionName);
        }

  
        public async Task<List<Review>> GetReviewsByDoctorAsync(string doctorId)
        {
            return await _reviews.Find(r => r.DoctorId == doctorId).ToListAsync();
        }

        public async Task<bool> TestMongoConnection()
        {
            try
            {
                var databases = await _reviews.Database.Client.ListDatabaseNamesAsync();
                return databases.Any();
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ MongoDB bağlantı hatası: " + ex.Message);
                return false;
            }
        }


        public async Task<double> GetAverageRatingAsync(string doctorId)
        {
            var reviews = await GetReviewsByDoctorAsync(doctorId);

            if (reviews == null || reviews.Count == 0)
                return 0; // Eğer hiç yorum yoksa 0 döndür

            return Math.Round(reviews.Average(r => r.Rating), 2); // 2 basamaklı yuvarlama
        }


        public async Task<Review> CreateReviewAsync(Review review)
        {
            review.Id = ObjectId.GenerateNewId().ToString();
            review.Timestamp = DateTime.UtcNow;

            await _reviews.InsertOneAsync(review);
            return review;
        }

    
        public async Task<bool> ContainsInappropriateLanguage(string comment)
        {
            string[] bannedWords = { "badword1", "badword2", "offensive" };
            return bannedWords.Any(word => comment.Contains(word, StringComparison.OrdinalIgnoreCase));
        }
    }
}