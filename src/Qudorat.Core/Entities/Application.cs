using Qudorat.Core.Common;
using Qudorat.Core.Enums;

namespace Qudorat.Core.Entities;

public class Application : AuditableEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public string? TammRequestId { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public ApplicationStatus QudoratStatus { get; set; } = ApplicationStatus.PendingAssignment;
    public TammStatus TammStatus { get; set; } = TammStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.NotRequired;
    public decimal? ServiceCharges { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ResponseAt { get; set; }
    public DateTime? SLADeadline { get; set; }
    public int ReturnCount { get; set; } = 0;
    public int ApprovalCount { get; set; } = 0;
    public int RejectionCount { get; set; } = 0;
    public UserRole? LastActionByRole { get; set; }
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }
    
    // Application form data stored as JSON
    public string? FormData { get; set; }
    
    // Navigation properties
    public virtual Applicant Applicant { get; set; } = null!;
    public virtual Service Service { get; set; } = null!;
    public virtual User? AssignedUser { get; set; }
    public virtual ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();
    public virtual ICollection<ApplicationHistory> Histories { get; set; } = new List<ApplicationHistory>();
    public virtual ICollection<ApplicationComment> Comments { get; set; } = new List<ApplicationComment>();
    public virtual ICollection<EntityStaffMember> StaffMembers { get; set; } = new List<EntityStaffMember>();
    public virtual License? License { get; set; }
}

public class ApplicationDocument : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public bool IsApplicantDocument { get; set; } = true; // false for internal documents
    public string? Description { get; set; }
    
    // Navigation properties
    public virtual Application Application { get; set; } = null!;
}

public class ApplicationComment : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public Guid UserId { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = true;
    public Guid? ReasonId { get; set; }
    
    // Navigation properties
    public virtual Application Application { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual ReasonCode? Reason { get; set; }
}

public class ApplicationHistory : AuditableEntity
{
    public Guid ApplicationId { get; set; }
    public Guid? UserId { get; set; }
    public ActionType ActionType { get; set; }
    public string ActionDescription { get; set; } = string.Empty;
    public ApplicationStatus? PreviousStatus { get; set; }
    public ApplicationStatus? NewStatus { get; set; }
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public UserRole? UserRole { get; set; }
    public string? IPAddress { get; set; }
    
    // Navigation properties
    public virtual Application Application { get; set; } = null!;
    public virtual User? User { get; set; }
}
