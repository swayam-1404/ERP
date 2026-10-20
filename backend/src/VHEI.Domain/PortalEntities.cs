namespace VHEI.Domain;
public class PortalAccess : ScopedEntity
{
    public Guid UserId { get; set; }
    public Guid StudentId { get; set; }
    public string Kind { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public string EvidenceReference { get; set; } = "";
    public Guid GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public string RevocationReason { get; set; } = "";
}
