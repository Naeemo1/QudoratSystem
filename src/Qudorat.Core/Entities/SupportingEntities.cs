using Qudorat.Core.Common;
using Qudorat.Core.Enums;

namespace Qudorat.Core.Entities;

public class ApplicantSuspension : AuditableEntity
{
    public Guid ApplicantId { get; set; }
    public string SuspendedServices { get; set; } = string.Empty; // Comma-separated service IDs
    public Guid ReasonId { get; set; }
    public DateTime EnabledDate { get; set; }
    public DateTime? DisabledDate { get; set; }
    public SuspensionStatus Status { get; set; } = SuspensionStatus.Active;
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual Applicant Applicant { get; set; } = null!;
    public virtual ReasonCode Reason { get; set; } = null!;
}

public class Notification : BaseEntity
{
    public Guid? UserId { get; set; }
    public Guid? ApplicantId { get; set; }
    public Guid? ApplicationId { get; set; }
    public NotificationType Type { get; set; }
    public string TitleEnglish { get; set; } = string.Empty;
    public string TitleArabic { get; set; } = string.Empty;
    public string MessageEnglish { get; set; } = string.Empty;
    public string MessageArabic { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public bool IsEmailSent { get; set; } = false;
    public DateTime? EmailSentAt { get; set; }
    
    // Navigation properties
    public virtual User? User { get; set; }
    public virtual Application? Application { get; set; }
}

public class ReasonCode : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string DescriptionEnglish { get; set; } = string.Empty;
    public string DescriptionArabic { get; set; } = string.Empty;
    public ReasonType ReasonType { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<ApplicantSuspension> Suspensions { get; set; } = new List<ApplicantSuspension>();
    public virtual ICollection<ApplicationComment> Comments { get; set; } = new List<ApplicationComment>();
}

public enum ReasonType
{
    Rejection = 1,
    Return = 2,
    Suspension = 3,
    Reassignment = 4
}
