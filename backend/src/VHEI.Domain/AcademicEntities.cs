namespace VHEI.Domain;
public class Classroom : ScopedEntity
{
    public string Name { get; set; } = "";
    public int Capacity { get; set; }
    public bool IsDemo { get; set; }
}
public class Subject : ScopedEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public Guid ProgrammeId { get; set; }
    public Guid AcademicSessionId { get; set; }
    public int Semester { get; set; }
    public bool IsDemo { get; set; }
}
public class ClassSession : ScopedEntity
{
    public Guid SubjectId { get; set; }
    public Guid ProgrammeId { get; set; }
    public Guid AcademicSessionId { get; set; }
    public Guid FacultyId { get; set; }
    public Guid ClassroomId { get; set; }
    public int Semester { get; set; }
    public string Section { get; set; } = "";
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsCancelled { get; set; }
    public bool AttendanceSubmitted { get; set; }
    public int Version { get; set; } = 1;
    public bool IsDemo { get; set; }
}
public class ClassRoster : ScopedEntity
{
    public Guid ClassSessionId { get; set; }
    public Guid StudentId { get; set; }
    public Guid EnrollmentId { get; set; }
}
public class AttendanceRecord : ScopedEntity
{
    public Guid ClassSessionId { get; set; }
    public Guid StudentId { get; set; }
    public string Status { get; set; } = "";
    public Guid RecordedBy { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
public class AttendanceCorrection : ScopedEntity
{
    public Guid AttendanceRecordId { get; set; }
    public Guid RequestedBy { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string OriginalStatus { get; set; } = "";
    public string NewStatus { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string ReviewNote { get; set; } = "";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}
