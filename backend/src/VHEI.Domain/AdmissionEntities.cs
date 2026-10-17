namespace VHEI.Domain;

public class AdmissionApplication : ScopedEntity
{
    public string ApplicationNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public Guid ProgrammeId { get; set; }
    public Guid AcademicSessionId { get; set; }
    public string ExternalReference { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public int Version { get; set; } = 1;
    public Guid CreatedBy { get; set; }
    public Guid? SubmittedBy { get; set; }
    public Guid? ReviewedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string ApprovalReference { get; set; } = "";
    public Guid? StudentId { get; set; }
    public string PolicyReference { get; set; } = "";
    public string EligibilityNote { get; set; } = "";
    public string DocumentReference { get; set; } = "";
    public string ReviewNote { get; set; } = "";
    public bool IsDemo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AdmissionEvent : ScopedEntity
{
    public Guid ApplicationId { get; set; }
    public Guid ActorId { get; set; }
    public string Status { get; set; } = "";
    public string Note { get; set; } = "";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
