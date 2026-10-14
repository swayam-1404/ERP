using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VHEI.Domain;
namespace VHEI.Infrastructure;

public class ErpUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = "";
}
public class ErpDbContext(DbContextOptions<ErpDbContext> options) : IdentityDbContext<ErpUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Programme> Programmes => Set<Programme>();
    public DbSet<AcademicSession> AcademicSessions => Set<AcademicSession>();
    public DbSet<InstitutionAssignment> InstitutionAssignments => Set<InstitutionAssignment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Faculty> Faculty => Set<Faculty>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<Institution>().HasIndex(x => x.Code).IsUnique();
        model.Entity<Institution>().Property(x => x.Code).HasMaxLength(24);
        model.Entity<Institution>().Property(x => x.Name).HasMaxLength(250);
        ConfigureScoped<Department>(model);
        ConfigureScoped<Programme>(model);
        ConfigureScoped<AcademicSession>(model);
        ConfigureScoped<AuditLog>(model);
        model.Entity<Department>().HasIndex(x => new { x.InstitutionId, x.Name }).IsUnique();
        model.Entity<Programme>().HasIndex(x => new { x.InstitutionId, x.Name }).IsUnique();
        model.Entity<Programme>().HasOne<Department>().WithMany().HasForeignKey(x => new { x.InstitutionId, x.DepartmentId }).HasPrincipalKey(x => new { x.InstitutionId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AcademicSession>().HasIndex(x => new { x.InstitutionId, x.Name }).IsUnique();
        model.Entity<InstitutionAssignment>().HasKey(x => new { x.UserId, x.InstitutionId });
        model.Entity<InstitutionAssignment>().HasOne<ErpUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<InstitutionAssignment>().HasOne<Institution>().WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AuditLog>().HasIndex(x => new { x.InstitutionId, x.OccurredAt });
        ConfigureScoped<Guardian>(model);ConfigureScoped<Student>(model);ConfigureScoped<Enrollment>(model);ConfigureScoped<Faculty>(model);
        model.Entity<Student>().HasIndex(x=>new{x.InstitutionId,x.StudentNumber}).IsUnique();
        model.Entity<Student>().Property(x=>x.StudentNumber).HasMaxLength(40);
        model.Entity<Student>().Property(x=>x.FullName).HasMaxLength(120);
        model.Entity<Student>().Property(x=>x.Email).HasMaxLength(254);
        model.Entity<Student>().Property(x=>x.Version).IsConcurrencyToken();
        model.Entity<Student>().HasOne<Guardian>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.GuardianId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Enrollment>().HasIndex(x=>new{x.InstitutionId,x.StudentId,x.AcademicSessionId}).IsUnique();
        model.Entity<Enrollment>().HasOne<Student>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.StudentId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Enrollment>().HasOne<Programme>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.ProgrammeId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Enrollment>().HasOne<AcademicSession>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.AcademicSessionId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Faculty>().HasOne<Department>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.DepartmentId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Faculty>().Property(x=>x.Version).IsConcurrencyToken();
        model.Entity<Faculty>().HasIndex(x=>new{x.InstitutionId,x.Email}).IsUnique();
        model.Entity<Student>().HasIndex(x=>new{x.InstitutionId,x.Status,x.CreatedAt});
    }
    private static void ConfigureScoped<T>(ModelBuilder model) where T : ScopedEntity
    {
        model.Entity<T>().HasKey(x => x.Id);
        model.Entity<T>().HasAlternateKey(x => new { x.InstitutionId, x.Id });
        model.Entity<T>().HasOne<Institution>().WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
    }
}
