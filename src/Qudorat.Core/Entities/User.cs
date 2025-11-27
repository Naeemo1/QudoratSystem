using Qudorat.Core.Common;
using Qudorat.Core.Enums;

namespace Qudorat.Core.Entities;

public class User : AuditableEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Offline;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? StatusChangedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    public string FullName => $"{FirstName} {LastName}";
    
    // Navigation properties
    public virtual ICollection<Application> AssignedApplications { get; set; } = new List<Application>();
    public virtual ICollection<ApplicationHistory> ActionHistories { get; set; } = new List<ApplicationHistory>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
