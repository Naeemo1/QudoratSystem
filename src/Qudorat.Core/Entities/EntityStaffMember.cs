using Qudorat.Core.Common;
using Qudorat.Core.Enums;

namespace Qudorat.Core.Entities;

public class EntityStaffMember : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public Guid ApplicantId { get; set; }
    public string PractitionerLicenseNumber { get; set; } = string.Empty;
    public bool? IsAccepted { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? ResponseComment { get; set; }
    
    // Navigation properties
    public virtual Application Application { get; set; } = null!;
    public virtual Applicant Applicant { get; set; } = null!;
}
