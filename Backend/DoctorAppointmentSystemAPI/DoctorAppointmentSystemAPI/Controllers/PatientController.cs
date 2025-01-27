using DoctorAppointmentSystemAPI.Models;
using DoctorAppointmentSystemAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Google.Apis.Auth;
using System.Security.Claims;

[ApiController]
[Route("api/patients")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;
    

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
        
    }

    [HttpGet("search-doctors")]
    public async Task<ActionResult<IEnumerable<Doctor>>> SearchDoctors([FromQuery] string specialty, [FromQuery] string location)
    {
        if (string.IsNullOrEmpty(specialty) && string.IsNullOrEmpty(location))
        {
            return BadRequest("Specialty and location parameters are required.");
        }

        var doctors = await _patientService.SearchDoctorsAsync(specialty, location);

        if (!doctors.Any())
        {
            return NotFound("No doctors found matching the criteria.");
        }

        return Ok(doctors);
    }

    [HttpPost("make-appointment")]
    public async Task<ActionResult<Appointment>> MakeAppointment([FromBody] Appointment appointment)
    {
        // Get patient ID from the authenticated user's session
        var patientId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Requires authentication middleware
        if (patientId == null)
        {
            return Unauthorized("User not logged in.");
        }

        // Validate appointment details
        if (appointment.DoctorId == 0)
        {
            return BadRequest("Doctor ID is required.");
        }

        // Assign patient ID from session
        appointment.PatientId = patientId;

        var result = await _patientService.MakeAppointmentAsync(appointment);
        return CreatedAtAction(nameof(MakeAppointment), new { id = result.Id }, result);
    }

    [HttpPost("reviews/{doctorId}")]
    public async Task<ActionResult<Review>> AddReview(string doctorId, [FromBody] Review review)
    {
        try
        {
            var result = await _patientService.AddReviewAsync(doctorId, review);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("specialties")]
    public async Task<ActionResult<IEnumerable<string>>> GetSpecialties()
    {
        var specialties = await _patientService.GetAllSpecialtiesAsync();
        return Ok(specialties);
    }

}
