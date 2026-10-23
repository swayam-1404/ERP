using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VHEI.Domain;
namespace VHEI.Infrastructure;

public class ErpUser : IdentityUser<Guid>
{
    public bool MustChangePassword { get; set; }
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
    public DbSet<AdmissionApplication> AdmissionApplications => Set<AdmissionApplication>();
    public DbSet<AdmissionEvent> AdmissionEvents => Set<AdmissionEvent>();
    public DbSet<PortalAccess> PortalAccesses => Set<PortalAccess>();
    public DbSet<GradingPolicy> GradingPolicies => Set<GradingPolicy>();
    public DbSet<ExamAssessment> ExamAssessments => Set<ExamAssessment>();
    public DbSet<ExamMark> ExamMarks => Set<ExamMark>();
    public DbSet<GraduationPolicy> GraduationPolicies => Set<GraduationPolicy>();
    public DbSet<AcademicCertificate> AcademicCertificates => Set<AcademicCertificate>();
    public DbSet<AdmissionFeePlan> AdmissionFeePlans => Set<AdmissionFeePlan>();
    public DbSet<AdmissionCharge> AdmissionCharges => Set<AdmissionCharge>();
    public DbSet<AdmissionPayment> AdmissionPayments => Set<AdmissionPayment>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
    public DbSet<TimetableSeries> TimetableSeries => Set<TimetableSeries>();
    public DbSet<ClassRoster> ClassRosters => Set<ClassRoster>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AttendanceCorrection> AttendanceCorrections => Set<AttendanceCorrection>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        ConfigureExaminations(model);
        ConfigureCertificates(model);
        ConfigureScoped<PortalAccess>(model);
        model.Entity<PortalAccess>().HasIndex(x=>new{x.InstitutionId,x.UserId,x.StudentId,x.Kind}).IsUnique();
        model.Entity<PortalAccess>().Property(x=>x.Version).IsConcurrencyToken();
        model.Entity<PortalAccess>().HasOne<ErpUser>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<PortalAccess>().HasOne<Student>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.StudentId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<PortalAccess>().ToTable(t=>t.HasCheckConstraint("CK_Portal_Kind","\"Kind\" IN ('Student','Guardian')"));
        ConfigureFinance(model);
        ConfigureScoped<TimetableSeries>(model);
        model.Entity<TimetableSeries>().Property(x=>x.Version).IsConcurrencyToken();
        model.Entity<TimetableSeries>().Property(x=>x.Name).HasMaxLength(120);
        model.Entity<TimetableSeries>().Property(x=>x.Section).HasMaxLength(12);
        model.Entity<TimetableSeries>().HasIndex(x=>new{x.InstitutionId,x.CreatedAt});
        model.Entity<TimetableSeries>().HasOne<Subject>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.SubjectId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<TimetableSeries>().HasOne<Faculty>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.FacultyId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<TimetableSeries>().HasOne<Classroom>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.ClassroomId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ClassSession>().HasOne<TimetableSeries>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.TimetableSeriesId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        ConfigureScoped<AdmissionApplication>(model);ConfigureScoped<AdmissionEvent>(model);
        model.Entity<AdmissionApplication>().HasIndex(x=>new{x.InstitutionId,x.ApplicationNumber}).IsUnique();
        model.Entity<AdmissionApplication>().HasIndex(x=>new{x.InstitutionId,x.Status,x.CreatedAt});
        model.Entity<AdmissionApplication>().Property(x=>x.Version).IsConcurrencyToken();
        model.Entity<AdmissionApplication>().Property(x=>x.ApplicationNumber).HasMaxLength(40);
        model.Entity<AdmissionApplication>().Property(x=>x.FullName).HasMaxLength(120);
        model.Entity<AdmissionApplication>().Property(x=>x.Email).HasMaxLength(254);
        model.Entity<AdmissionApplication>().Property(x=>x.ExternalReference).HasMaxLength(120);
        model.Entity<AdmissionApplication>().HasOne<Programme>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.ProgrammeId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AdmissionApplication>().HasOne<AcademicSession>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.AcademicSessionId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AdmissionApplication>().ToTable(t=>{
            t.HasCheckConstraint("CK_Admission_Status","\"Status\" IN ('Draft','Submitted','Reviewed','Approved','Enrolled','Rejected','Cancelled')");
            t.HasCheckConstraint("CK_Admission_Review","\"Status\" NOT IN ('Reviewed','Approved','Enrolled','Rejected') OR (\"ReviewedBy\" IS NOT NULL AND \"SubmittedBy\" IS NOT NULL AND \"ReviewedBy\" <> \"SubmittedBy\")");
            t.HasCheckConstraint("CK_Admission_Approval","\"Status\" NOT IN ('Approved','Enrolled') OR (\"ApprovedBy\" IS NOT NULL AND \"ApprovedBy\" <> \"SubmittedBy\" AND length(trim(\"ApprovalReference\")) >= 3)");
            t.HasCheckConstraint("CK_Admission_Enrolled","(\"Status\" = 'Enrolled') = (\"StudentId\" IS NOT NULL)");
        });
        model.Entity<AdmissionApplication>().HasOne<Student>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.StudentId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AdmissionApplication>().HasIndex(x=>new{x.InstitutionId,x.StudentId}).IsUnique().HasFilter("\"StudentId\" IS NOT NULL");
        model.Entity<AdmissionEvent>().HasOne<AdmissionApplication>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.ApplicationId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
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
        model.Entity<Faculty>().HasOne<ErpUser>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Faculty>().HasIndex(x=>new{x.InstitutionId,x.UserId}).IsUnique().HasFilter("\"UserId\" IS NOT NULL");
        ConfigureScoped<Classroom>(model);ConfigureScoped<Subject>(model);ConfigureScoped<ClassSession>(model);ConfigureScoped<ClassRoster>(model);ConfigureScoped<AttendanceRecord>(model);ConfigureScoped<AttendanceCorrection>(model);
        model.Entity<Classroom>().HasIndex(x=>new{x.InstitutionId,x.Name}).IsUnique();
        model.Entity<Classroom>().Property(x=>x.Name).HasMaxLength(120);
        model.Entity<Subject>().HasIndex(x=>new{x.InstitutionId,x.ProgrammeId,x.AcademicSessionId,x.Code}).IsUnique();
        model.Entity<Subject>().HasOne<Programme>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.ProgrammeId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Subject>().HasOne<AcademicSession>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.AcademicSessionId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Subject>().HasAlternateKey(x=>new{x.InstitutionId,x.Id,x.ProgrammeId,x.AcademicSessionId,x.Semester});
        model.Entity<ClassSession>().HasOne<Subject>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.SubjectId,x.ProgrammeId,x.AcademicSessionId,x.Semester}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id,x.ProgrammeId,x.AcademicSessionId,x.Semester}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ClassSession>().HasOne<Faculty>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.FacultyId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ClassSession>().HasOne<Classroom>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.ClassroomId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ClassSession>().Property(x=>x.Version).IsConcurrencyToken();
        model.Entity<ClassSession>().Property(x=>x.Section).HasMaxLength(12);
        model.Entity<ClassSession>().ToTable(t=>t.HasCheckConstraint("CK_ClassSession_Time","\"EndsAt\" > \"StartsAt\""));
        model.Entity<ClassRoster>().HasAlternateKey(x=>new{x.InstitutionId,x.ClassSessionId,x.StudentId});
        model.Entity<ClassRoster>().HasOne<ClassSession>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.ClassSessionId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Enrollment>().HasAlternateKey(x=>new{x.InstitutionId,x.Id,x.StudentId});
        model.Entity<ClassRoster>().HasOne<Enrollment>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.EnrollmentId,x.StudentId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id,x.StudentId}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AttendanceRecord>().HasIndex(x=>new{x.InstitutionId,x.ClassSessionId,x.StudentId}).IsUnique();
        model.Entity<AttendanceRecord>().HasOne<ClassRoster>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.ClassSessionId,x.StudentId}).HasPrincipalKey(x=>new{x.InstitutionId,x.ClassSessionId,x.StudentId}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AttendanceRecord>().ToTable(t=>t.HasCheckConstraint("CK_Attendance_Status","\"Status\" IN ('Present','Absent','Excused')"));
        model.Entity<AttendanceCorrection>().HasOne<AttendanceRecord>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.AttendanceRecordId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AttendanceCorrection>().ToTable(t=>{t.HasCheckConstraint("CK_Correction_Status","\"Status\" IN ('Pending','Approved','Rejected')");t.HasCheckConstraint("CK_Correction_Separation","\"ReviewedBy\" IS NULL OR \"ReviewedBy\" <> \"RequestedBy\"");});
    }
    private static void ConfigureExaminations(ModelBuilder model)
    {
        ConfigureScoped<GradingPolicy>(model);ConfigureScoped<ExamAssessment>(model);ConfigureScoped<ExamMark>(model);
        model.Entity<GradingPolicy>().Property(x=>x.Version).IsConcurrencyToken();
        model.Entity<GradingPolicy>().HasOne<Programme>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.ProgrammeId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<GradingPolicy>().HasOne<AcademicSession>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.AcademicSessionId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<GradingPolicy>().ToTable(t=>t.HasCheckConstraint("CK_GradingPolicy_Approval","\"Status\" = 'Draft' OR (\"Status\" = 'Approved' AND \"ApprovedBy\" IS NOT NULL AND \"ApprovedBy\" <> \"CreatedBy\")"));
        model.Entity<ExamAssessment>().Property(x=>x.Version).IsConcurrencyToken();
        model.Entity<ExamAssessment>().HasOne<Subject>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.SubjectId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ExamAssessment>().HasOne<GradingPolicy>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.GradingPolicyId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ExamAssessment>().HasOne<Faculty>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.FacultyId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ExamAssessment>().HasAlternateKey(x=>new{x.InstitutionId,x.Id,x.SubjectId});
        model.Entity<ExamAssessment>().HasIndex(x=>new{x.InstitutionId,x.SubjectId,x.Section}).IsUnique();
        model.Entity<ExamAssessment>().ToTable(t=>{t.HasCheckConstraint("CK_Assessment_Status","\"Status\" IN ('Draft','Submitted','Approved','Published')");t.HasCheckConstraint("CK_Assessment_Approval","\"Status\" NOT IN ('Approved','Published') OR (\"ApprovedBy\" IS NOT NULL AND \"SubmittedBy\" IS NOT NULL AND \"ApprovedBy\" <> \"SubmittedBy\")");t.HasCheckConstraint("CK_Assessment_Limits","\"Credits\" > 0 AND \"MaximumInternal\" >= 0 AND \"MaximumExternal\" > 0 AND \"PassInternal\" BETWEEN 0 AND \"MaximumInternal\" AND \"PassExternal\" BETWEEN 0 AND \"MaximumExternal\" AND \"PassTotal\" BETWEEN 0 AND (\"MaximumInternal\" + \"MaximumExternal\")");});
        foreach(var p in new[]{"Credits","MaximumInternal","MaximumExternal","PassInternal","PassExternal","PassTotal"})model.Entity<ExamAssessment>().Property<decimal>(p).HasPrecision(12,2);
        foreach(var p in new[]{"InternalMark","ExternalMark","Total","Percentage","GradePoints"})model.Entity<ExamMark>().Property<decimal?>(p).HasPrecision(12,2);
        model.Entity<ExamMark>().HasOne<ExamAssessment>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.AssessmentId,x.SubjectId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id,x.SubjectId}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ExamMark>().HasOne<Enrollment>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.EnrollmentId,x.StudentId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id,x.StudentId}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ExamMark>().HasIndex(x=>new{x.InstitutionId,x.AssessmentId,x.StudentId}).IsUnique();
        model.Entity<ExamMark>().HasIndex(x=>new{x.InstitutionId,x.StudentId,x.SubjectId}).IsUnique().HasFilter("\"IsPublished\"");
        model.Entity<ExamMark>().ToTable(t=>t.HasCheckConstraint("CK_ExamMark_Attendance","\"Attendance\" IN ('Unentered','Present','Absent')"));
    }
    private static void ConfigureCertificates(ModelBuilder model)
    {
        ConfigureScoped<GraduationPolicy>(model); ConfigureScoped<AcademicCertificate>(model);
        model.Entity<GraduationPolicy>().Property(x=>x.Version).IsConcurrencyToken();
        model.Entity<GraduationPolicy>().Property(x=>x.MinimumCredits).HasPrecision(12,2);
        model.Entity<GraduationPolicy>().HasOne<Programme>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.ProgrammeId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<GraduationPolicy>().HasOne<AcademicSession>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.AcademicSessionId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<GraduationPolicy>().ToTable(t=>{t.HasCheckConstraint("CK_GraduationPolicy_Credits","\"MinimumCredits\" > 0");t.HasCheckConstraint("CK_GraduationPolicy_Approval","\"Status\" = 'Draft' OR (\"Status\" = 'Approved' AND \"ApprovedBy\" IS NOT NULL AND \"ApprovedBy\" <> \"CreatedBy\")");});
        model.Entity<AcademicCertificate>().Property(x=>x.Version).IsConcurrencyToken();
        model.Entity<AcademicCertificate>().HasOne<Student>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.StudentId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AcademicCertificate>().HasOne<GraduationPolicy>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.GraduationPolicyId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AcademicCertificate>().HasIndex(x=>x.VerificationToken).IsUnique().HasFilter("\"VerificationToken\" IS NOT NULL");
        model.Entity<AcademicCertificate>().HasIndex(x=>x.Number).IsUnique().HasFilter("\"Number\" IS NOT NULL");
        model.Entity<AcademicCertificate>().ToTable(t=>{
            t.HasCheckConstraint("CK_Certificate_Kind","\"Kind\" IN ('Transcript','CourseCompletion') AND (\"Kind\" <> 'CourseCompletion' OR \"GraduationPolicyId\" IS NOT NULL)");
            t.HasCheckConstraint("CK_Certificate_Issue","\"Status\" IN ('Requested','Cancelled') OR (\"Status\" IN ('Issued','Revoked') AND \"IssuedBy\" IS NOT NULL AND \"IssuedBy\" <> \"RequestedBy\" AND \"Pdf\" IS NOT NULL AND \"Number\" IS NOT NULL AND \"Sha256\" IS NOT NULL AND \"VerificationToken\" IS NOT NULL AND \"IssuedAt\" IS NOT NULL)");
            t.HasCheckConstraint("CK_Certificate_Revoke","\"Status\" <> 'Revoked' OR (\"RevokedBy\" IS NOT NULL AND \"RevokedAt\" IS NOT NULL)");
        });
    }
    private static void ConfigureFinance(ModelBuilder model)
    {
        ConfigureScoped<AdmissionFeePlan>(model);ConfigureScoped<AdmissionCharge>(model);ConfigureScoped<AdmissionPayment>(model);
        model.Entity<AdmissionFeePlan>().Property(x=>x.Amount).HasPrecision(18,2);
        model.Entity<AdmissionFeePlan>().Property(x=>x.Version).IsConcurrencyToken();
        model.Entity<AdmissionFeePlan>().Property(x=>x.CollectionMode).HasDefaultValue("Offline");
        model.Entity<AdmissionFeePlan>().HasOne<Programme>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.ProgrammeId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AdmissionFeePlan>().HasOne<AcademicSession>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.AcademicSessionId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AdmissionFeePlan>().ToTable(t=>{t.HasCheckConstraint("CK_FeePlan_Amount","\"Amount\" > 0 AND \"Currency\" = 'INR'");t.HasCheckConstraint("CK_FeePlan_Approval","\"Status\" = 'Draft' OR (\"Status\" = 'Approved' AND \"ApprovedBy\" IS NOT NULL AND \"ApprovedBy\" <> \"CreatedBy\")");});
        model.Entity<AdmissionCharge>().Property(x=>x.Amount).HasPrecision(18,2);
        model.Entity<AdmissionCharge>().HasIndex(x=>new{x.InstitutionId,x.ApplicationId}).IsUnique();
        model.Entity<AdmissionCharge>().HasOne<AdmissionApplication>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.ApplicationId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AdmissionCharge>().HasOne<AdmissionFeePlan>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.FeePlanId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AdmissionCharge>().ToTable(t=>t.HasCheckConstraint("CK_Charge_Amount","\"Amount\" > 0 AND \"Currency\" = 'INR'"));
        model.Entity<AdmissionPayment>().Property(x=>x.Amount).HasPrecision(18,2);
        model.Entity<AdmissionPayment>().Property(x=>x.ExternalReference).HasMaxLength(120);
        model.Entity<AdmissionPayment>().HasOne<AdmissionCharge>().WithMany().HasForeignKey(x=>new{x.InstitutionId,x.ChargeId}).HasPrincipalKey(x=>new{x.InstitutionId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AdmissionPayment>().HasIndex(x=>new{x.InstitutionId,x.IdempotencyKey}).IsUnique();
        model.Entity<AdmissionPayment>().HasIndex(x=>new{x.InstitutionId,x.ExternalReference}).IsUnique();
        model.Entity<AdmissionPayment>().HasIndex(x=>new{x.InstitutionId,x.AcknowledgementNumber}).IsUnique().HasFilter("\"AcknowledgementNumber\" IS NOT NULL");
        model.Entity<AdmissionPayment>().ToTable(t=>{t.HasCheckConstraint("CK_Payment_Amount","\"Amount\" > 0");t.HasCheckConstraint("CK_Payment_Status","\"Status\" IN ('Pending','Confirmed','Rejected')");t.HasCheckConstraint("CK_Payment_Review","\"Status\" = 'Pending' OR (\"ReviewedBy\" IS NOT NULL AND \"ReviewedBy\" <> \"RecordedBy\")");t.HasCheckConstraint("CK_Payment_Acknowledgement","(\"Status\" = 'Confirmed') = (\"AcknowledgementNumber\" IS NOT NULL)");});
    }
    private static void ConfigureScoped<T>(ModelBuilder model) where T : ScopedEntity
    {
        model.Entity<T>().HasKey(x => x.Id);
        model.Entity<T>().HasAlternateKey(x => new { x.InstitutionId, x.Id });
        model.Entity<T>().HasOne<Institution>().WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
    }
}
