using DoctorAppointmentSystemAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;

public interface IDoctorService
{
    Task RegisterDoctor(string firebaseUserId, string email, RegisterDoctorDTO doctorDto);
    Task<Doctor> GetDoctorById(int id);
    Task<bool> ApproveDoctor(int doctorId);
    Task<List<Availability>> GetAvailabilityForCurrentWeek(int doctorId);
    Task<List<Doctor>> GetApprovedDoctors();
}

public class DoctorService : IDoctorService
{

    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDistributedCache _cache;

    public DoctorService(AppDbContext context, IHttpContextAccessor httpContextAccessor, IDistributedCache cache)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
    }


    public async Task RegisterDoctor(string firebaseUserId, string email, RegisterDoctorDTO doctorDto)
    {
        // Check if the doctor is already registered
        var existingDoctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.FirebaseUserId == firebaseUserId);

        if (existingDoctor != null)
        {
            throw new InvalidOperationException("Doctor already registered.");
        }

        // Create and populate the doctor object
        var doctor = new Doctor
        {
            FirebaseUserId = firebaseUserId,
            FullName = doctorDto.FullName,
            Specialty = doctorDto.Specialty,
            Address = doctorDto.Address,
            IsApproved = false, // Default to unapproved
            Email = email,
            Availability = doctorDto.Availability.Select(a => new Availability
            {
                Day = a.Day,
                StartTime = a.StartTime,
                EndTime = a.EndTime
            }).ToList()
        };

        // Save the doctor to the database
        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();

        // Invalidate cache for related data
        var cacheKey = $"doctors_search_{doctorDto.Specialty}_{doctorDto.Address}";
        await _cache.RemoveAsync(cacheKey);
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
