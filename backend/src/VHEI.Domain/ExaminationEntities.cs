namespace VHEI.Domain;
public record GradeBand(decimal MinimumPercent,string Grade,decimal Points);
public class GradingPolicy : ScopedEntity
{
    public string Name { get; set; } = "";
    public Guid ProgrammeId { get; set; }
    public Guid AcademicSessionId { get; set; }
    public string SourceAcademicYear { get; set; } = "";
    public string SourceReference { get; set; } = "";
    public string BandsJson { get; set; } = "[]";
    public string FailGrade { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string ApprovalNote { get; set; } = "";
    public int Version { get; set; } = 1;
    public bool IsDemo { get; set; }
}
public class ExamAssessment : ScopedEntity
{
    public string Name { get; set; } = "";
    public Guid SubjectId { get; set; }
    public Guid GradingPolicyId { get; set; }
    public Guid FacultyId { get; set; }
    public string Section { get; set; } = "";
    public DateOnly ScheduledOn { get; set; }
    public string ConfigurationReference { get; set; } = "";
    public decimal Credits { get; set; }
    public decimal MaximumInternal { get; set; }
    public decimal MaximumExternal { get; set; }
    public decimal PassInternal { get; set; }
    public decimal PassExternal { get; set; }
    public decimal PassTotal { get; set; }
    public string Status { get; set; } = "Draft";
    public int Version { get; set; } = 1;
    public Guid CreatedBy { get; set; }
    public Guid? SubmittedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string ReviewNote { get; set; } = "";
    public Guid? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsDemo { get; set; }
}
public class ExamMark : ScopedEntity
{
    public Guid AssessmentId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid StudentId { get; set; }
    public Guid EnrollmentId { get; set; }
    public string Attendance { get; set; } = "Unentered";
    public decimal? InternalMark { get; set; }
    public decimal? ExternalMark { get; set; }
    public decimal? Total { get; set; }
    public decimal? Percentage { get; set; }
    public string? Grade { get; set; }
    public decimal? GradePoints { get; set; }
    public bool IsPass { get; set; }
    public bool IsPublished { get; set; }
}
