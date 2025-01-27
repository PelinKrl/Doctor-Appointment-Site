using DoctorAppointmentSystemAPI.Models;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("patient-login")]
    public async Task<IActionResult> PatientLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {
            FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.Token);
            string uid = decodedToken.Uid;

            // First check if user is a doctor
            var existingDoctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.FirebaseUserId == uid);

            if (existingDoctor != null)
            {
                return BadRequest("User is registered as a doctor. Please use doctor login.");
            }

            // Handle patient registration/login
            var existingPatient = await _context.Patients
                .FirstOrDefaultAsync(p => p.FirebaseUserId == uid);

            if (existingPatient == null)
            {
                var newPatient = new Patient
                {
                    FirebaseUserId = uid,
                    FullName = decodedToken.Claims["name"]?.ToString() ?? "Unknown",
                    Email = decodedToken.Claims["email"]?.ToString() ?? "No email"
                };
                _context.Patients.Add(newPatient);
                await _context.SaveChangesAsync();
            }

            return Ok(new { Role = "Patient" });
        }
        catch (FirebaseAuthException ex)
        {
            return Unauthorized($"Invalid token: {ex.Message}");
        }
    }

    [HttpPost("doctor-login")]
    public async Task<IActionResult> DoctorLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {
            FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.Token);
            string uid = decodedToken.Uid;

            var existingDoctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.FirebaseUserId == uid);

            if (existingDoctor != null)
            {
                return Ok(new
                {
                    Role = existingDoctor.IsApproved ? "Doctor" : "Pending",
                    IsApproved = existingDoctor.IsApproved,
                    Name = existingDoctor.FullName
                });
            }

            return NotFound("Doctor registration not found. Please complete doctor registration.");
        }
        catch (FirebaseAuthException ex)
        {
            return Unauthorized($"Invalid token: {ex.Message}");
        }
    }
    [HttpPost("admin-login")]
    public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequest request)
    {
        if (request.Username == "admin" && request.Password == "password")
        {
            // Generate a JWT token for admin
            var token = GenerateAdminToken();
            return Ok(new { Token = token, Role = "Admin" });
        }
        return Unauthorized("Invalid credentials");
    }

    private string GenerateAdminToken()
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("this-is-a-256-bit-secret-key-1234567890"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var token = new JwtSecurityToken(
            issuer: "your-issuer",
            audience: "your-audience",
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Keep existing AdminLogin method
}