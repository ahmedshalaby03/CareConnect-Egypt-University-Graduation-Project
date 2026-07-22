using System.Reflection;
using CareConnect.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
    public DbSet<DoctorProfile> DoctorProfiles => Set<DoctorProfile>();
    public DbSet<HospitalProfile> HospitalProfiles => Set<HospitalProfile>();
    public DbSet<MedicalServiceProviderProfile> MedicalServiceProviderProfiles =>
        Set<MedicalServiceProviderProfile>();

    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<HospitalSpecialty> HospitalSpecialties => Set<HospitalSpecialty>();
    public DbSet<DoctorHospitalAffiliation> DoctorHospitalAffiliations =>
        Set<DoctorHospitalAffiliation>();

    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();
    public DbSet<DoctorUnavailablePeriod> DoctorUnavailablePeriods => Set<DoctorUnavailablePeriod>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
