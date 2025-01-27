using DoctorAppointmentSystemAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IMongoDatabase _database;

    public ReviewsController(IMongoDatabase database)
    {
        _database = database;
    }

    [HttpGet]
    public IActionResult GetReviews()
    {
        var collection = _database.GetCollection<Review>("Reviews");
        return Ok(collection.Find(_ => true).ToList());
    }
}