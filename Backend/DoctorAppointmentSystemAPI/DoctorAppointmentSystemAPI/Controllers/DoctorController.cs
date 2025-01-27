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

    
    [HttpPost("register")]
    public async Task<IActionResult> RegisterDoctor([FromBody] RegisterDoctorDTO doctorDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var firebaseUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "No email";

            // Call the service to register the doctor
            await _doctorService.RegisterDoctor(firebaseUserId, email, doctorDto);

            return Ok(new { Message = "Doctor registration pending approval" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while registering the doctor.");
        }
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
    [HttpGet("unapproved")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUnapprovedDoctors()
    {
        // Log user info
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        Console.WriteLine($"UserId: {userId}, Role: {role}");

        if (role != "Admin")
        {
            return Forbid("Access Denied: You are not an Admin.");
        }

        var unapprovedDoctors = await _context.Doctors.Where(d => !d.IsApproved).ToListAsync();
        return Ok(unapprovedDoctors);
    }


}
