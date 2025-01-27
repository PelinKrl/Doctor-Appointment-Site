using DoctorAppointmentSystemAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;


[ApiController]
[Route("api/doctors")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;
    private readonly AppDbContext _context;  // Assuming you have a service layer for business logic
    private readonly IDistributedCache _cache;

    public DoctorsController(
        IDoctorService doctorService,
        AppDbContext context,
        IDistributedCache cache) // Add this parameter
    {
        _doctorService = doctorService;
        _context = context;
        _cache = cache; // Initialize
    }


    [Authorize]
    [HttpPost("register")]
    public async Task<IActionResult> RegisterDoctor([FromBody] RegisterDoctorDTO doctorDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var firebaseUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Check if already registered
        var existingDoctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.FirebaseUserId == firebaseUserId);

        if (existingDoctor != null)
            return BadRequest("Doctor already registered.");

        var doctor = new Doctor
        {
            FirebaseUserId = firebaseUserId,
            FullName = doctorDto.FullName,
            Specialty = doctorDto.Specialty,
            Address = doctorDto.Address,
            IsApproved = false,
            Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "No email",
            Availability = doctorDto.Availability.Select(a => new Availability
            {
                Day = a.Day,
                StartTime = a.StartTime,
                EndTime = a.EndTime
            }).ToList()
        };

        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();

        // Invalidate cache
        var cacheKey = $"doctors_search_{doctorDto.Specialty}_{doctorDto.Address}";
        await _cache.RemoveAsync(cacheKey);

        return Ok(new { Message = "Doctor registration pending approval" });
    }

    [HttpGet("{doctorId}/availability")]
    public async Task<IActionResult> GetDoctorAvailability(int doctorId)
    {
        var availability = await _doctorService.GetAvailabilityForCurrentWeek(doctorId);
        if (availability == null)
        {
            return NotFound("Doctor not found.");
        }
        return Ok(availability);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDoctor(int id)
    {
        var doctor = await _doctorService.GetDoctorById(id);
        if (doctor == null)
        {
            return NotFound();
        }
        
        return Ok(doctor);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("approve/{doctorId}")]
    public async Task<IActionResult> ApproveDoctor(int doctorId)
    {
        var doctor = await _context.Doctors.FindAsync(doctorId);
        if (doctor == null)
            return NotFound("Doctor not found");

        doctor.IsApproved = true;
        await _context.SaveChangesAsync();
        return Ok("Doctor approved successfully");
    }

    [HttpGet("approved")]
    public async Task<IActionResult> GetApprovedDoctors()
    {
        var approvedDoctors = await _doctorService.GetApprovedDoctors();
        return Ok(approvedDoctors);
    }


    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUnapprovedDoctors()
    {
        var doctors = await _context.Doctors
            .Where(d => !d.IsApproved)
            .ToListAsync();
        return Ok(doctors);
    }

}
