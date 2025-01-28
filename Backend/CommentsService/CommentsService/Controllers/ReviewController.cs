using CommentsService.Models;
using CommentsService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CommentsService.Controllers
{
    [Route("api/reviews")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly ReviewService _reviewService;

        public ReviewController(ReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        
        [HttpGet("{doctorId}")]
        public async Task<IActionResult> GetReviews(string doctorId)
        {
            var reviews = await _reviewService.GetReviewsByDoctorAsync(doctorId);

            if (reviews == null || reviews.Count == 0)
            {
                return Ok(new { DoctorId = doctorId, AverageRating = 0, Reviews = new List<Review>() });
            }

            
            double averageRating = reviews.Average(r => r.Rating);

            return Ok(new
            {
                DoctorId = doctorId,
                AverageRating = Math.Round(averageRating, 2),
                Reviews = reviews
            });
        }

        
        [HttpPost("add-review")]
        public async Task<IActionResult> AddReview([FromBody] Review review)
        {
            if (await _reviewService.ContainsInappropriateLanguage(review.Comment))
            {
                return BadRequest("Your comment contains inappropriate language.");
            }

            var newReview = await _reviewService.CreateReviewAsync(review);
            return CreatedAtAction(nameof(GetReviews), new { doctorId = newReview.DoctorId }, newReview);
        }

        
        [HttpPost("check-inappropriate")]
        public IActionResult CheckInappropriateComment([FromBody] string comment)
        {
            bool isInappropriate = _reviewService.ContainsInappropriateLanguage(comment).Result;
            return Ok(new { isInappropriate });
        }
    }
}
