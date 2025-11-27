using Qudorat.Core.Common;

namespace Qudorat.Core.Entities;

public class SLAConfiguration : BaseEntity
{
    public string ConfigKey { get; set; } = string.Empty;
    public int SLATotalDays { get; set; } = 5;
    public int EscalationToSpecialistDays { get; set; } = 2;
    public int EscalationToSectionHeadDays { get; set; } = 3;
    public int MaxReturnCount { get; set; } = 3;
    public int MaxTasksPerOfficer { get; set; } = 10;
    public int TaskDistributionIntervalMinutes { get; set; } = 3;
    public int OnlineGracePeriodMinutes { get; set; } = 2;
    public int LicenseValidityDays { get; set; } = 365;
    public int RenewalNotificationDays { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}

public class SystemConfiguration : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataType { get; set; } = "string";
}

public class AuditLog : BaseEntity
{
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IPAddress { get; set; }
}
