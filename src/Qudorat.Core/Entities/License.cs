using Qudorat.Core.Common;
using Qudorat.Core.Enums;

namespace Qudorat.Core.Entities;

public class License : AuditableEntity
{
    public string LicenseNumber { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime IssuedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public LicenseStatus Status { get; set; } = LicenseStatus.Active;
    public string? CertificatePath { get; set; }
    public string? CardPath { get; set; }
    public bool RenewalNotificationSent { get; set; } = false;
    public DateTime? RenewalNotificationSentAt { get; set; }
    
    public bool IsExpired => DateTime.UtcNow > ExpiryDate;
    public bool IsWithinRenewalPeriod => !IsExpired && ExpiryDate <= DateTime.UtcNow.AddDays(30);
    public bool IsWithinGracePeriod => IsExpired && ExpiryDate.AddDays(30) >= DateTime.UtcNow;
    
    // Navigation properties
    public virtual Application Application { get; set; } = null!;
    public virtual Applicant Applicant { get; set; } = null!;
    public virtual Service Service { get; set; } = null!;
}
