using DoctorAppointmentSystemAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Security.Claims;

public interface IDoctorService
{
    Task<Doctor> RegisterDoctor(RegisterDoctorDTO doctorDto);
    Task<Doctor> GetDoctorById(int id);
    Task<bool> ApproveDoctor(int doctorId);
    Task<List<Availability>> GetAvailabilityForCurrentWeek(int doctorId);
    Task<List<Doctor>> GetApprovedDoctors();
}

public class DoctorService : IDoctorService
{

    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DoctorService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }


    public async Task<Doctor> RegisterDoctor(RegisterDoctorDTO doctorDto)
    {
        var firebaseUserId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var doctor = new Doctor
        {
            FirebaseUserId = firebaseUserId,
            FullName = doctorDto.FullName,
            Specialty = doctorDto.Specialty,
            Address = doctorDto.Address,
            IsApproved = false, // Default to unapproved
            Availability = doctorDto.Availability.Select(a => new Availability
            {
                Day = a.Day,
                StartTime = a.StartTime,
                EndTime = a.EndTime
            }).ToList()
        };

        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();
        return doctor;
    }

    public async Task<Doctor> GetDoctorById(int id)
    {
        return await _context.Doctors
            .Include(d => d.Availability)
            .FirstOrDefaultAsync(d => d.Id == id);
    }


    public async Task<bool> ApproveDoctor(int doctorId)
    {
        var doctor = await _context.Doctors.FindAsync(doctorId);
        if (doctor == null)
        {
            return false;
        }

        if (!doctor.IsApproved)
        {
            doctor.IsApproved = true;
            _context.Update(doctor);
            await _context.SaveChangesAsync();
        }
        return true;
    }
    public async Task<List<Doctor>> GetApprovedDoctors()
    {
        return await _context.Doctors
            .Where(d => d.IsApproved)
            .ToListAsync();
    }

    public async Task<List<Availability>> GetAvailabilityForCurrentWeek(int doctorId)
    {
        var doctor = await _context.Doctors
            .Include(d => d.Availability)
            .FirstOrDefaultAsync(d => d.Id == doctorId);

        return doctor?.Availability; // Null-safe return
    }
}
