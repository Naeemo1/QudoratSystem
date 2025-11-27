using Qudorat.Core.Enums;

namespace Qudorat.Application.DTOs;

// User DTOs
public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    UserRole Role,
    UserStatus Status,
    bool IsActive,
    DateTime? LastLoginAt
);

public record CreateUserDto(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    UserRole Role
);

public record UpdateUserDto(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    UserRole Role,
    bool IsActive
);

public record UpdateUserStatusDto(UserStatus Status);

// Applicant DTOs
public record ApplicantDto(
    Guid Id,
    string EmiratesId,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    CommunicationPreference PreferredCommunication,
    CommunicationLanguage CommunicationLanguage,
    bool IsSuspended
);

public record ApplicantDetailDto(
    Guid Id,
    string EmiratesId,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    CommunicationPreference PreferredCommunication,
    CommunicationLanguage CommunicationLanguage,
    bool IsSuspended,
    List<ApplicationSummaryDto> Applications,
    List<LicenseSummaryDto> Licenses,
    List<SuspensionDto> ActiveSuspensions
);

public record ApplicantSearchDto(
    string? Name,
    string? EmiratesId,
    string? Email
);

// Application DTOs
public record ApplicationDto(
    Guid Id,
    string RequestNumber,
    string? TammRequestId,
    Guid ApplicantId,
    string ApplicantName,
    Guid ServiceId,
    string ServiceName,
    ServiceType ServiceType,
    Guid? AssignedUserId,
    string? AssignedUserName,
    ApplicationStatus QudoratStatus,
    TammStatus TammStatus,
    PaymentStatus PaymentStatus,
    decimal? ServiceCharges,
    DateTime SubmittedAt,
    DateTime? ResponseAt,
    DateTime? SLADeadline,
    int ReturnCount,
    bool IsArchived
);

public record ApplicationDetailDto(
    Guid Id,
    string RequestNumber,
    string? TammRequestId,
    ApplicantDto Applicant,
    ServiceDto Service,
    UserDto? AssignedUser,
    ApplicationStatus QudoratStatus,
    TammStatus TammStatus,
    PaymentStatus PaymentStatus,
    decimal? ServiceCharges,
    DateTime SubmittedAt,
    DateTime? ResponseAt,
    DateTime? SLADeadline,
    int ReturnCount,
    bool IsArchived,
    string? FormData,
    List<ApplicationDocumentDto> ApplicantDocuments,
    List<ApplicationDocumentDto> InternalDocuments,
    List<ApplicationHistoryDto> History,
    List<ApplicationCommentDto> Comments,
    List<ApplicationSummaryDto> OtherActiveApplications
);

public record ApplicationSummaryDto(
    Guid Id,
    string RequestNumber,
    string ServiceName,
    ApplicationStatus QudoratStatus,
    TammStatus TammStatus,
    DateTime SubmittedAt
);

public record CreateApplicationDto(
    string TammRequestId,
    string ApplicantEmiratesId,
    string ApplicantFirstName,
    string ApplicantLastName,
    string ApplicantEmail,
    string ApplicantPhone,
    string ServiceCode,
    string FormData,
    List<CreateDocumentDto> Documents,
    List<CreateStaffMemberDto>? StaffMembers,
    CommunicationPreference PreferredCommunication,
    CommunicationLanguage CommunicationLanguage
);

public record CreateDocumentDto(
    string FileName,
    string FileType,
    string FileContent // Base64
);

public record CreateStaffMemberDto(
    string EmiratesId,
    string LicenseNumber
);

public record ApplicationActionDto(
    Guid? ReasonId,
    string? Comment
);

public record ReassignApplicationDto(
    Guid ToUserId,
    Guid ReasonId,
    string? Comment
);

public record ReopenApplicationDto(
    ApplicationStatus NewStatus
);

// Document DTOs
public record ApplicationDocumentDto(
    Guid Id,
    string FileName,
    string FilePath,
    string FileType,
    long FileSize,
    bool IsApplicantDocument,
    string? Description,
    DateTime CreatedAt
);

public record AddInternalDocumentDto(
    string FileName,
    string FileType,
    string FileContent, // Base64
    string? Description
);

// History DTOs
public record ApplicationHistoryDto(
    Guid Id,
    ActionType ActionType,
    string ActionDescription,
    ApplicationStatus? PreviousStatus,
    ApplicationStatus? NewStatus,
    string? FieldName,
    string? OldValue,
    string? NewValue,
    string? UserName,
    UserRole? UserRole,
    DateTime CreatedAt
);

// Comment DTOs
public record ApplicationCommentDto(
    Guid Id,
    string Comment,
    string UserName,
    bool IsInternal,
    string? ReasonDescription,
    DateTime CreatedAt
);

public record AddCommentDto(
    string Comment,
    bool IsInternal,
    Guid? ReasonId
);

// Service DTOs
public record ServiceDto(
    Guid Id,
    string ServiceCode,
    string NameEnglish,
    string NameArabic,
    string DescriptionEnglish,
    string DescriptionArabic,
    ServiceType ServiceType,
    ServiceCategory ServiceCategory,
    decimal? ServiceFee,
    int ProcessingDays,
    int SLADays,
    bool IsActive
);

public record ServiceDetailDto(
    Guid Id,
    string ServiceCode,
    string NameEnglish,
    string NameArabic,
    string DescriptionEnglish,
    string DescriptionArabic,
    ServiceType ServiceType,
    ServiceCategory ServiceCategory,
    decimal? ServiceFee,
    int ProcessingDays,
    int SLADays,
    bool IsActive,
    string? TermsEnglish,
    string? TermsArabic,
    List<ServiceDocumentDto> RequiredDocuments
);

public record ServiceDocumentDto(
    Guid Id,
    string DocumentNameEnglish,
    string DocumentNameArabic,
    bool IsRequired,
    int DisplayOrder
);

// License DTOs
public record LicenseDto(
    Guid Id,
    string LicenseNumber,
    Guid ApplicationId,
    Guid ApplicantId,
    string ApplicantName,
    Guid ServiceId,
    string ServiceName,
    DateTime IssuedDate,
    DateTime ExpiryDate,
    LicenseStatus Status,
    bool IsExpired,
    bool IsWithinRenewalPeriod
);

public record LicenseSummaryDto(
    Guid Id,
    string LicenseNumber,
    string ServiceName,
    LicenseStatus Status,
    DateTime ExpiryDate
);

public record LicenseSearchDto(
    string? Name,
    string? LicenseNumber,
    bool? IsEntity
);

// Suspension DTOs
public record SuspensionDto(
    Guid Id,
    Guid ApplicantId,
    string ApplicantName,
    string ApplicantEmail,
    List<string> SuspendedServices,
    string ReasonDescription,
    DateTime EnabledDate,
    DateTime? DisabledDate,
    SuspensionStatus Status
);

public record CreateSuspensionDto(
    string ApplicantEmail,
    List<Guid> ServiceIds,
    Guid ReasonId,
    string? Notes
);

// Notification DTOs
public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string TitleEnglish,
    string TitleArabic,
    string MessageEnglish,
    string MessageArabic,
    bool IsRead,
    DateTime CreatedAt,
    Guid? ApplicationId
);

// Reason Code DTOs
public record ReasonCodeDto(
    Guid Id,
    string Code,
    string DescriptionEnglish,
    string DescriptionArabic,
    ReasonType ReasonType
);

// Dashboard DTOs
public record DashboardDto(
    int PendingAssignment,
    int InProgress,
    int CompletedToday,
    int NearingSLA,
    int BreachedSLA,
    List<TaskSummaryDto> RecentTasks
);

public record TaskSummaryDto(
    Guid ApplicationId,
    string RequestNumber,
    string ApplicantName,
    string ServiceName,
    ApplicationStatus Status,
    DateTime SubmittedAt,
    DateTime? SLADeadline,
    string? AssignedTo
);

// KPI DTOs
public record KPIDto(
    int TotalApplicationsToday,
    int TotalApplicationsThisWeek,
    int TotalApplicationsThisMonth,
    double AverageProcessingTimeHours,
    double SLACompliancePercentage,
    int ApprovedCount,
    int RejectedCount,
    int ReturnedCount,
    Dictionary<string, int> ApplicationsByService,
    Dictionary<string, int> ApplicationsByStatus
);

// Pagination
public record PaginatedResult<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public record PaginationParams(
    int PageNumber = 1,
    int PageSize = 10,
    string? SortBy = null,
    bool SortDescending = false
);

// Filter DTOs
public record ApplicationFilterDto(
    string? RequestNumber,
    string? ApplicantName,
    Guid? ServiceId,
    ServiceType? ServiceType,
    ApplicationStatus? QudoratStatus,
    TammStatus? TammStatus,
    Guid? AssignedUserId,
    DateTime? FromDate,
    DateTime? ToDate,
    bool? IsArchived
);
