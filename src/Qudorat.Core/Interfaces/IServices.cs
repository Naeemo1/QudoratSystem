using Qudorat.Core.Entities;
using Qudorat.Core.Enums;

namespace Qudorat.Core.Interfaces;

public interface IApplicationService
{
    Task<Application> CreateApplicationAsync(Application application, CancellationToken cancellationToken = default);
    Task<Application?> GetApplicationByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Application?> GetApplicationByRequestNumberAsync(string requestNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Application>> GetApplicationsByApplicantAsync(Guid applicantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Application>> GetAssignedApplicationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Application>> GetUnassignedApplicationsAsync(CancellationToken cancellationToken = default);
    Task<Application> ApproveApplicationAsync(Guid applicationId, Guid userId, string? comment = null, CancellationToken cancellationToken = default);
    Task<Application> RejectApplicationAsync(Guid applicationId, Guid userId, Guid reasonId, string? comment = null, CancellationToken cancellationToken = default);
    Task<Application> ReturnApplicationAsync(Guid applicationId, Guid userId, Guid reasonId, string? comment = null, CancellationToken cancellationToken = default);
    Task<Application> ReassignApplicationAsync(Guid applicationId, Guid fromUserId, Guid toUserId, Guid reasonId, string? comment = null, CancellationToken cancellationToken = default);
    Task<Application> LockApplicationAsync(Guid applicationId, Guid userId, CancellationToken cancellationToken = default);
    Task<Application> ReleaseApplicationAsync(Guid applicationId, Guid userId, CancellationToken cancellationToken = default);
    Task<Application> ReopenApplicationAsync(Guid applicationId, Guid userId, ApplicationStatus newStatus, CancellationToken cancellationToken = default);
    Task ArchiveApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<bool> ValidateDuplicateApplicationAsync(Guid applicantId, Guid serviceId, CancellationToken cancellationToken = default);
}

public interface ITaskAssignmentService
{
    Task AssignPendingTasksAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAvailableOfficersAsync(CancellationToken cancellationToken = default);
    Task<int> GetAssignedTaskCountAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface ISLAService
{
    Task MonitorSLAAsync(CancellationToken cancellationToken = default);
    Task<DateTime> CalculateSLADeadlineAsync(DateTime startDate, int slaDays, CancellationToken cancellationToken = default);
    Task SendEscalationAlertsAsync(CancellationToken cancellationToken = default);
}

public interface ILicenseService
{
    Task<License> IssueLicenseAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<License?> GetLicenseByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<License?> GetLicenseByNumberAsync(string licenseNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<License>> GetLicensesByApplicantAsync(Guid applicantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<License>> SearchLicensesAsync(string? name = null, string? licenseNumber = null, bool? isEntity = null, CancellationToken cancellationToken = default);
    Task CheckExpiringLicensesAsync(CancellationToken cancellationToken = default);
    Task<string> GenerateCertificateAsync(Guid licenseId, CancellationToken cancellationToken = default);
    Task<string> GenerateCardAsync(Guid licenseId, CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
    Task SendEmailNotificationAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IUserService
{
    Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    Task UpdateUserStatusAsync(Guid userId, UserStatus status, CancellationToken cancellationToken = default);
    Task<User> UpdateUserAsync(User user, CancellationToken cancellationToken = default);
    Task DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IApplicantService
{
    Task<Applicant> CreateOrUpdateApplicantAsync(Applicant applicant, CancellationToken cancellationToken = default);
    Task<Applicant?> GetApplicantByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Applicant?> GetApplicantByEmiratesIdAsync(string emiratesId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Applicant>> SearchApplicantsAsync(string? name = null, string? emiratesId = null, string? email = null, CancellationToken cancellationToken = default);
    Task<ApplicantSuspension> SuspendApplicantAsync(Guid applicantId, List<Guid> serviceIds, Guid reasonId, string? notes = null, CancellationToken cancellationToken = default);
    Task DeactivateSuspensionAsync(Guid suspensionId, CancellationToken cancellationToken = default);
    Task<bool> IsApplicantSuspendedForServiceAsync(Guid applicantId, Guid serviceId, CancellationToken cancellationToken = default);
}

public interface IReportService
{
    Task<ApplicationStatusReport> GetApplicationStatusReportAsync(DateTime? startDate = null, DateTime? endDate = null, Guid? serviceId = null, ApplicationStatus? status = null, CancellationToken cancellationToken = default);
    Task<UserPerformanceReport> GetUserPerformanceReportAsync(DateTime? startDate = null, DateTime? endDate = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<SLAComplianceReport> GetSLAComplianceReportAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<byte[]> ExportReportToExcelAsync<T>(T report, string reportName, CancellationToken cancellationToken = default) where T : class;
}

public interface ITammIntegrationService
{
    Task<Application> ReceiveApplicationFromTammAsync(TammApplicationRequest request, CancellationToken cancellationToken = default);
    Task SendStatusUpdateToTammAsync(Guid applicationId, TammStatus status, string? message = null, CancellationToken cancellationToken = default);
    Task SendLicenseToTammAsync(Guid licenseId, CancellationToken cancellationToken = default);
}

// Report DTOs
public class ApplicationStatusReport
{
    public int TotalApplications { get; set; }
    public Dictionary<ApplicationStatus, int> StatusCounts { get; set; } = new();
    public Dictionary<string, int> ServiceCounts { get; set; } = new();
    public List<ApplicationReportItem> Applications { get; set; } = new();
}

public class ApplicationReportItem
{
    public string RequestNumber { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? AssignedTo { get; set; }
}

public class UserPerformanceReport
{
    public List<UserPerformanceItem> Users { get; set; } = new();
}

public class UserPerformanceItem
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int TasksCompleted { get; set; }
    public int TasksApproved { get; set; }
    public int TasksRejected { get; set; }
    public int TasksReturned { get; set; }
    public double AverageProcessingTimeHours { get; set; }
}

public class SLAComplianceReport
{
    public int TotalApplications { get; set; }
    public int CompletedWithinSLA { get; set; }
    public int CompletedAfterSLA { get; set; }
    public int PendingWithinSLA { get; set; }
    public int PendingNearSLA { get; set; }
    public int PendingBreachedSLA { get; set; }
    public double SLACompliancePercentage { get; set; }
}

public class TammApplicationRequest
{
    public string TammRequestId { get; set; } = string.Empty;
    public string ApplicantEmiratesId { get; set; } = string.Empty;
    public string ApplicantFirstName { get; set; } = string.Empty;
    public string ApplicantLastName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string ApplicantPhone { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public string FormData { get; set; } = string.Empty;
    public List<TammDocument> Documents { get; set; } = new();
    public List<TammStaffMember>? StaffMembers { get; set; }
    public CommunicationPreference PreferredCommunication { get; set; }
    public CommunicationLanguage CommunicationLanguage { get; set; }
}

public class TammDocument
{
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string FileContent { get; set; } = string.Empty; // Base64
}

public class TammStaffMember
{
    public string EmiratesId { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
}
