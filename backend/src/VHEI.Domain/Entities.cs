namespace VHEI.Domain;

public class Institution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string SourceUrl { get; set; } = "";
    public DateOnly ExtractedOn { get; set; }
    public string VerificationStatus { get; set; } = "VERIFIED_OFFICIAL";
}
public abstract class ScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstitutionId { get; set; }
}
public class Department : ScopedEntity
{
    public string Name { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string VerificationStatus { get; set; } = "VERIFIED_OFFICIAL";
}
public class Programme : ScopedEntity
{
    public string Name { get; set; } = "";
    public Guid? DepartmentId { get; set; }
    public int? DurationYears { get; set; }
    public string SourceUrl { get; set; } = "";
    public string VerificationStatus { get; set; } = "VERIFIED_OFFICIAL";
}
public class AcademicSession : ScopedEntity
{
    public string Name { get; set; } = "";
    public bool IsDemo { get; set; }
}
public class InstitutionAssignment
{
    public Guid UserId { get; set; }
    public Guid InstitutionId { get; set; }
}
public class AuditLog : ScopedEntity
{
    public Guid ActorId { get; set; }
    public string Action { get; set; } = "";
    public string Resource { get; set; } = "";
    public string Details { get; set; } = "";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
public class Guardian : ScopedEntity
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Relationship { get; set; } = "Guardian";
    public bool IsDemo { get; set; }
}
public class Student : ScopedEntity
{
    public string StudentNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Status { get; set; } = "Active";
    public Guid GuardianId { get; set; }
    public bool IsDemo { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Enrollment : ScopedEntity
{
    public Guid StudentId { get; set; }
    public Guid ProgrammeId { get; set; }
    public Guid AcademicSessionId { get; set; }
    public int BatchYear { get; set; }
    public int Semester { get; set; }
    public string Section { get; set; } = "A";
    public bool IsDemo { get; set; }
}
public class Faculty : ScopedEntity
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Designation { get; set; } = "";
    public Guid? DepartmentId { get; set; }
    public bool IsDemo { get; set; }
    public bool IsArchived { get; set; }
    public int Version { get; set; } = 1;
}
