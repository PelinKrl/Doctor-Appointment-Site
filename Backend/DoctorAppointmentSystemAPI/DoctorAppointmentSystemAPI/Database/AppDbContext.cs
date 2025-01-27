using DoctorAppointmentSystemAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Emit;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Admin> Admins { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Doctor>().HasIndex(d => d.Specialty); 

        // Configure Doctor -> Availability relationship
        modelBuilder.Entity<Doctor>(doctor =>
        {
            doctor.OwnsMany(d => d.Availability, availability =>
            {
                availability.WithOwner().HasForeignKey("DoctorId");
                availability.Property(a => a.Id).ValueGeneratedOnAdd();
                availability.HasKey(a => a.Id); // Define primary key
            });

            // New Appointments relationship (1-to-many)
            doctor.HasMany(d => d.Appointments)
                 .WithOne()
                 .HasForeignKey(a => a.DoctorId)
                 .OnDelete(DeleteBehavior.Cascade); // Optional: Delete appointments when doctor is deleted
        });

        // Configure Patient -> Appointments relationship
        modelBuilder.Entity<Patient>(patient =>
        {
            patient.HasKey(p => p.FirebaseUserId); // Set FirebaseUserId as primary key
            patient.HasMany(p => p.Appointments)
                   .WithOne()
                   .HasForeignKey(a => a.PatientId) // Foreign key in Appointment
                   .IsRequired();
        });

        modelBuilder.Entity<Admin>().HasData(
            new Admin
            {
                     Id = 1,
                     Username = "admin",
                     Password = "password" // In production, hash this password
            }
        );


    }
}
