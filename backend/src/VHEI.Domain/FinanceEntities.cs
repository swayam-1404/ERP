namespace VHEI.Domain;

public class AdmissionFeePlan : ScopedEntity
{
    public string Name { get; set; } = "";
    public Guid ProgrammeId { get; set; }
    public Guid AcademicSessionId { get; set; }
    public string SourceAcademicYear { get; set; } = "";
    public string SourceReference { get; set; } = "";
    public string AuthorityReference { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string CollectionMode { get; set; } = "Offline";
    public string Status { get; set; } = "Draft";
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string ApprovalNote { get; set; } = "";
    public int Version { get; set; } = 1;
    public bool IsDemo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class AdmissionCharge : ScopedEntity
{
    public Guid ApplicationId { get; set; }
    public Guid FeePlanId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public bool IsVoid { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class AdmissionPayment : ScopedEntity
{
    public Guid ChargeId { get; set; }
    public Guid IdempotencyKey { get; set; }
    public string ExternalReference { get; set; } = "";
    public string EvidenceReference { get; set; } = "";
    public string Method { get; set; } = "";
    public DateOnly PaidOn { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid RecordedBy { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string ReviewNote { get; set; } = "";
    public string? AcknowledgementNumber { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
