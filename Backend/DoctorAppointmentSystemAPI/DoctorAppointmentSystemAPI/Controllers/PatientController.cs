using DoctorAppointmentSystemAPI.Models;
using DoctorAppointmentSystemAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Google.Apis.Auth;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/patients")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;


    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;

    }

    
    [HttpPost("register")]
    public async Task<IActionResult> RegisterPatient([FromBody] RegisterPatientDTO patientDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Extract FirebaseUserId and Email from authenticated user's claims
            var firebaseUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? patientDto.Email; // Use provided email if not found in claims

            if (string.IsNullOrEmpty(firebaseUserId))
            {
                return BadRequest("FirebaseUserId is missing from the request.");
            }

            // Call the service to register the patient
            await _patientService.RegisterPatient(firebaseUserId, email, patientDto);

            return Ok(new { Message = "Patient registered successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = $"An error occurred while registering the patient: {ex.Message}" });
        }
    }



    [Authorize]
    [HttpGet("search-doctors")]
    public async Task<ActionResult<IEnumerable<Doctor>>> SearchDoctors([FromQuery] string specialty = null, [FromQuery] string location = null)
    {
        // Fetch doctors using the service
        var doctors = await _patientService.SearchDoctorsAsync(specialty, location);

        // Return a 404 if no doctors match
        if (!doctors.Any())
        {
            return NotFound("No doctors found matching the criteria.");
        }

        // Return the matching doctors
        return Ok(doctors);
    }

    [HttpPost("make-appointment")]
    public async Task<ActionResult<Appointment>> MakeAppointment([FromBody] Appointment appointment)
    {
        // Get patient ID from the authenticated user's session
        var patientId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (patientId == null)
        {
            return Unauthorized("User not logged in.");
        }

        // Ensure the DoctorId is provided
        if (appointment.DoctorId == 0)
        {
            return BadRequest("Doctor ID is required.");
        }

        // Assign PatientId to the appointment
        appointment.PatientId = patientId;

        try
        {
            // Call the service to book the appointment
            var result = await _patientService.MakeAppointmentAsync(appointment);

            // Return the newly created appointment with a 201 status code
            return CreatedAtAction(nameof(MakeAppointment), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            // Handle any service-level errors
            return StatusCode(500, new { Error = $"An error occurred: {ex.Message}" });
        }
    }


    [HttpGet("get-appointment/{doctorId}")]
    public async Task<IActionResult> GetUpcomingAppointments(int doctorId)
    {
        try
        {
            var appointments = await _patientService.GetUpcomingAppointments(doctorId);

            // Always return a 200 response, even if no appointments are found
            return Ok(appointments ?? new List<Appointment>());
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }


    [HttpGet("specialties")]
    public async Task<ActionResult<IEnumerable<string>>> GetSpecialties()
    {
        var specialties = await _patientService.GetAllSpecialtiesAsync();
        return Ok(specialties);
    }
}
