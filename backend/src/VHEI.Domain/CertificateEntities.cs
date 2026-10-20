namespace VHEI.Domain;

public class GraduationPolicy : ScopedEntity
{
    public string Name { get; set; } = "";
    public Guid ProgrammeId { get; set; }
    public Guid AcademicSessionId { get; set; }
    public string RequiredSubjectsJson { get; set; } = "[]";
    public decimal MinimumCredits { get; set; }
    public string SourceReference { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string ApprovalNote { get; set; } = "";
    public int Version { get; set; } = 1;
}

public class AcademicCertificate : ScopedEntity
{
    public Guid StudentId { get; set; }
    public string Kind { get; set; } = "Transcript";
    public Guid? GraduationPolicyId { get; set; }
    public string Status { get; set; } = "Requested";
    public Guid RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string SnapshotJson { get; set; } = "";
    public string ClearanceReference { get; set; } = "";
    public string AuthorityReference { get; set; } = "";
    public Guid? IssuedBy { get; set; }
    public DateTime? IssuedAt { get; set; }
    public string? Number { get; set; }
    public string? VerificationToken { get; set; }
    public byte[]? Pdf { get; set; }
    public string? Sha256 { get; set; }
    public Guid? RevokedBy { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string ReviewNote { get; set; } = "";
    public bool IsDemo { get; set; }
    public int Version { get; set; } = 1;
}
