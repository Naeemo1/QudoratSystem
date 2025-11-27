namespace Qudorat.Core.Enums;

public enum ServiceType
{
    Individual = 1,
    Provider = 2
}

public enum ServiceCategory
{
    NewGeneralPractitioner = 1,
    NewSeniorPractitioner = 2,
    NewOSHAuditor = 3,
    NewAsbestosSupervisingConsultant = 4,
    NewWorkplaceFirstAider = 5,
    NewOSHConsultancyOffice = 6,
    NewOSHAuditingOffice = 7,
    RenewPractitioner = 8,
    RenewSeniorPractitioner = 9,
    RenewOSHAuditor = 10,
    RenewAsbestosSupervisingConsultant = 11,
    RenewWorkplaceFirstAider = 12,
    RenewOSHConsultancyOffice = 13,
    RenewOSHAuditingOffice = 14,
    ExitFromEntityEnrollment = 15
}

public enum ApplicationStatus
{
    PendingAssignment = 1,
    InProgress = 2,
    PendingStaffAction = 3,
    Approved = 4,
    Rejected = 5,
    ReturnedForInfo = 6,
    Archived = 7,
    AutoRejected = 8
}

public enum TammStatus
{
    Pending = 1,
    InProgress = 2,
    Approved = 3,
    Rejected = 4,
    RequiresMoreInformation = 5
}

public enum UserRole
{
    Officer = 1,
    Specialist = 2,
    SeniorSpecialist = 3,
    SectionHead = 4,
    Director = 5,
    SystemAdmin = 6
}

public enum UserStatus
{
    Online = 1,
    Offline = 2
}

public enum ActionType
{
    Submitted = 1,
    Assigned = 2,
    Approved = 3,
    Rejected = 4,
    Returned = 5,
    Reassigned = 6,
    Locked = 7,
    Released = 8,
    CommentAdded = 9,
    AttachmentAdded = 10,
    StatusChanged = 11,
    Suspended = 12,
    Reactivated = 13,
    Reopened = 14,
    FieldUpdated = 15,
    AutoRejected = 16
}

public enum CommunicationPreference
{
    Phone = 1,
    Email = 2
}

public enum CommunicationLanguage
{
    Arabic = 1,
    English = 2
}

public enum LicenseStatus
{
    Active = 1,
    Expired = 2,
    Revoked = 3,
    PendingRenewal = 4
}

public enum SuspensionStatus
{
    Active = 1,
    Inactive = 2
}

public enum NotificationType
{
    ApplicationSubmitted = 1,
    ApplicationApproved = 2,
    ApplicationRejected = 3,
    ApplicationReturned = 4,
    TaskAssigned = 5,
    SLAWarning = 6,
    SLAEscalation = 7,
    LicenseExpiring = 8,
    StatusUpdated = 9,
    StaffActionRequired = 10,
    UserWentOffline = 11
}

public enum PaymentStatus
{
    NotRequired = 0,
    Pending = 1,
    Paid = 2,
    Failed = 3
}
