using Qudorat.Core.Common;
using Qudorat.Core.Enums;

namespace Qudorat.Core.Entities;

public class Applicant : AuditableEntity
{
    public string EmiratesId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public CommunicationPreference PreferredCommunication { get; set; }
    public CommunicationLanguage CommunicationLanguage { get; set; }
    public bool IsSuspended { get; set; } = false;
    
    public string FullName => $"{FirstName} {LastName}";
    
    // Navigation properties
    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
    public virtual ICollection<License> Licenses { get; set; } = new List<License>();
    public virtual ICollection<ApplicantSuspension> Suspensions { get; set; } = new List<ApplicantSuspension>();
    public virtual ICollection<EntityStaffMember> EntityMemberships { get; set; } = new List<EntityStaffMember>();
}
