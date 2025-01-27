using DoctorAppointmentSystemAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MongoDB.Driver;
using System.Text.Json;

namespace DoctorAppointmentSystemAPI.Services
{
    public interface IPatientService
    {
        Task<IEnumerable<Doctor>> SearchDoctorsAsync(string specialty, string location);
        Task<Appointment> MakeAppointmentAsync(Appointment appointment);
        Task<Review> AddReviewAsync(string doctorId, Review review);
        Task<List<string>> GetAllSpecialtiesAsync();
        Task<List<Appointment>> GetUpcomingAppointments(int doctorId);
        Task RegisterPatient(string firebaseUserId, string email, RegisterPatientDTO patientDto);
    }

    public class PatientService : IPatientService
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IDoctorService _doctorService; 
        public PatientService(AppDbContext context, IDistributedCache cache, IMongoDatabase mongoDatabase)
        {
            _context = context;
            _cache = cache;
            _mongoDatabase = mongoDatabase;
        }

        
        public async Task RegisterPatient(string firebaseUserId, string email, RegisterPatientDTO patientDto)
        {
            // Check if the patient already exists
            var existingPatient = await _context.Patients
                .AsNoTracking() // Optimization for read-only query
                .FirstOrDefaultAsync(p => p.FirebaseUserId == firebaseUserId);

            if (existingPatient != null)
            {
                throw new InvalidOperationException("Patient is already registered.");
            }

            // Create and save the new patient
            var patient = new Patient
            {
                FirebaseUserId = firebaseUserId,
                FullName = patientDto.FullName,
                Email = email
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
        }
        


        public async Task<List<string>> GetAllSpecialtiesAsync()
        {
            // Retrieve unique specialties from the database
            return await _context.Doctors
                .Select(d => d.Specialty)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<Appointment>> GetUpcomingAppointments(int doctorId)
        {
            var today = DateTime.UtcNow.Date;

            return await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate >= today)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();
        }
        public async Task<IEnumerable<Doctor>> SearchDoctorsAsync(string specialty, string location)
        {
            // Start query with approved doctors
            var query = _context.Doctors
                .Where(d => d.IsApproved) // Only approved doctors
                .AsQueryable();

            // Apply specialty filter if provided
            if (!string.IsNullOrEmpty(specialty))
            {
                query = query.Where(d => d.Specialty.ToLower().Contains(specialty.ToLower()));
            }

            // Apply location filter if provided
            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(d => d.Address.ToLower().Contains(location.ToLower()));
            }

            // Execute query and return the results
            return await query.ToListAsync();
        }


        public async Task<Appointment> MakeAppointmentAsync(Appointment appointment)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Verify doctor availability
                var doctor = await _context.Doctors
                    .Include(d => d.Availability)
                    .Include(d => d.Appointments)
                    .FirstOrDefaultAsync(d => d.Id == appointment.DoctorId);

                if (doctor == null)
                    throw new ArgumentException("Doctor not found");

                // Check availability slot
                var isAvailable = doctor.Availability.Any(a =>
                    a.Day == appointment.AppointmentDate.DayOfWeek &&
                    appointment.AppointmentDate.TimeOfDay >= a.StartTime &&
                    appointment.AppointmentDate.TimeOfDay <= a.EndTime);

                if (!isAvailable)
                    throw new ArgumentException("Doctor not available at this time");

                // Check existing appointments
                var hasConflict = doctor.Appointments.Any(a =>
                    a.AppointmentDate == appointment.AppointmentDate);

                if (hasConflict)
                    throw new ArgumentException("Time slot already booked");

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return appointment;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task<Review> AddReviewAsync(string doctorId, Review review)
        {
            if (review == null || doctorId != review.DoctorId)
            {
                throw new ArgumentException("Invalid review data: Doctor ID mismatch.");
            }

            var collection = _mongoDatabase.GetCollection<Review>("Reviews");
            await collection.InsertOneAsync(review);
            return review;
        }
    }

}
