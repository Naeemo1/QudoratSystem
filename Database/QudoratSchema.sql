-- Qudorat System Database Schema
-- SQL Server

-- Create Database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'QudoratDb')
BEGIN
    CREATE DATABASE QudoratDb;
END
GO

USE QudoratDb;
GO

-- Users Table
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    PhoneNumber NVARCHAR(20) NULL,
    Role INT NOT NULL, -- 1: Officer, 2: Specialist, 3: SeniorSpecialist, 4: SectionHead, 5: Director, 6: SystemAdmin
    Status INT NOT NULL DEFAULT 2, -- 1: Online, 2: Offline
    LastLoginAt DATETIME2 NULL,
    StatusChangedAt DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedBy NVARCHAR(100) NULL,
    DeletedAt DATETIME2 NULL,
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);

CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_Role ON Users(Role);
CREATE INDEX IX_Users_Status ON Users(Status);

-- Applicants Table
CREATE TABLE Applicants (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EmiratesId NVARCHAR(15) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    PreferredCommunication INT NOT NULL, -- 1: Phone, 2: Email
    CommunicationLanguage INT NOT NULL, -- 1: Arabic, 2: English
    IsSuspended BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedBy NVARCHAR(100) NULL,
    DeletedAt DATETIME2 NULL,
    CONSTRAINT UQ_Applicants_EmiratesId UNIQUE (EmiratesId)
);

CREATE INDEX IX_Applicants_EmiratesId ON Applicants(EmiratesId);
CREATE INDEX IX_Applicants_Email ON Applicants(Email);

-- Services Table
CREATE TABLE Services (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ServiceCode NVARCHAR(50) NOT NULL,
    NameEnglish NVARCHAR(200) NOT NULL,
    NameArabic NVARCHAR(200) NOT NULL,
    DescriptionEnglish NVARCHAR(2000) NULL,
    DescriptionArabic NVARCHAR(2000) NULL,
    ServiceType INT NOT NULL, -- 1: Individual, 2: Provider
    ServiceCategory INT NOT NULL,
    ServiceFee DECIMAL(18,2) NULL,
    ProcessingDays INT NOT NULL DEFAULT 15,
    SLADays INT NOT NULL DEFAULT 5,
    IsActive BIT NOT NULL DEFAULT 1,
    TermsEnglish NVARCHAR(4000) NULL,
    TermsArabic NVARCHAR(4000) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CONSTRAINT UQ_Services_ServiceCode UNIQUE (ServiceCode)
);

CREATE INDEX IX_Services_ServiceCode ON Services(ServiceCode);
CREATE INDEX IX_Services_ServiceType ON Services(ServiceType);

-- ServiceDocuments Table
CREATE TABLE ServiceDocuments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ServiceId UNIQUEIDENTIFIER NOT NULL,
    DocumentNameEnglish NVARCHAR(200) NOT NULL,
    DocumentNameArabic NVARCHAR(200) NOT NULL,
    IsRequired BIT NOT NULL DEFAULT 1,
    DisplayOrder INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CONSTRAINT FK_ServiceDocuments_Services FOREIGN KEY (ServiceId) REFERENCES Services(Id) ON DELETE CASCADE
);

-- ReasonCodes Table
CREATE TABLE ReasonCodes (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code NVARCHAR(50) NOT NULL,
    DescriptionEnglish NVARCHAR(500) NOT NULL,
    DescriptionArabic NVARCHAR(500) NOT NULL,
    ReasonType INT NOT NULL, -- 1: Rejection, 2: Return, 3: Suspension, 4: Reassignment
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CONSTRAINT UQ_ReasonCodes_Code_Type UNIQUE (Code, ReasonType)
);

-- Applications Table
CREATE TABLE Applications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    RequestNumber NVARCHAR(50) NOT NULL,
    TammRequestId NVARCHAR(100) NULL,
    ApplicantId UNIQUEIDENTIFIER NOT NULL,
    ServiceId UNIQUEIDENTIFIER NOT NULL,
    AssignedUserId UNIQUEIDENTIFIER NULL,
    QudoratStatus INT NOT NULL DEFAULT 1, -- 1: PendingAssignment, 2: InProgress, 3: PendingStaffAction, 4: Approved, 5: Rejected, 6: ReturnedForInfo, 7: Archived, 8: AutoRejected
    TammStatus INT NOT NULL DEFAULT 1, -- 1: Pending, 2: InProgress, 3: Approved, 4: Rejected, 5: RequiresMoreInformation
    PaymentStatus INT NOT NULL DEFAULT 0, -- 0: NotRequired, 1: Pending, 2: Paid, 3: Failed
    ServiceCharges DECIMAL(18,2) NULL,
    SubmittedAt DATETIME2 NOT NULL,
    ResponseAt DATETIME2 NULL,
    SLADeadline DATETIME2 NULL,
    ReturnCount INT NOT NULL DEFAULT 0,
    ApprovalCount INT NOT NULL DEFAULT 0,
    RejectionCount INT NOT NULL DEFAULT 0,
    LastActionByRole INT NULL,
    IsArchived BIT NOT NULL DEFAULT 0,
    ArchivedAt DATETIME2 NULL,
    FormData NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedBy NVARCHAR(100) NULL,
    DeletedAt DATETIME2 NULL,
    CONSTRAINT UQ_Applications_RequestNumber UNIQUE (RequestNumber),
    CONSTRAINT FK_Applications_Applicants FOREIGN KEY (ApplicantId) REFERENCES Applicants(Id),
    CONSTRAINT FK_Applications_Services FOREIGN KEY (ServiceId) REFERENCES Services(Id),
    CONSTRAINT FK_Applications_Users FOREIGN KEY (AssignedUserId) REFERENCES Users(Id) ON DELETE SET NULL
);

CREATE INDEX IX_Applications_RequestNumber ON Applications(RequestNumber);
CREATE INDEX IX_Applications_TammRequestId ON Applications(TammRequestId);
CREATE INDEX IX_Applications_ApplicantId ON Applications(ApplicantId);
CREATE INDEX IX_Applications_ServiceId ON Applications(ServiceId);
CREATE INDEX IX_Applications_AssignedUserId ON Applications(AssignedUserId);
CREATE INDEX IX_Applications_QudoratStatus ON Applications(QudoratStatus);
CREATE INDEX IX_Applications_SubmittedAt ON Applications(SubmittedAt);

-- ApplicationDocuments Table
CREATE TABLE ApplicationDocuments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ApplicationId UNIQUEIDENTIFIER NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    FileType NVARCHAR(50) NOT NULL,
    FileSize BIGINT NOT NULL,
    IsApplicantDocument BIT NOT NULL DEFAULT 1,
    Description NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CONSTRAINT FK_ApplicationDocuments_Applications FOREIGN KEY (ApplicationId) REFERENCES Applications(Id) ON DELETE CASCADE
);

CREATE INDEX IX_ApplicationDocuments_ApplicationId ON ApplicationDocuments(ApplicationId);

-- ApplicationComments Table
CREATE TABLE ApplicationComments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ApplicationId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Comment NVARCHAR(2000) NOT NULL,
    IsInternal BIT NOT NULL DEFAULT 1,
    ReasonId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CONSTRAINT FK_ApplicationComments_Applications FOREIGN KEY (ApplicationId) REFERENCES Applications(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ApplicationComments_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_ApplicationComments_ReasonCodes FOREIGN KEY (ReasonId) REFERENCES ReasonCodes(Id) ON DELETE SET NULL
);

CREATE INDEX IX_ApplicationComments_ApplicationId ON ApplicationComments(ApplicationId);

-- ApplicationHistories Table
CREATE TABLE ApplicationHistories (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ApplicationId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NULL,
    ActionType INT NOT NULL, -- 1: Submitted, 2: Assigned, 3: Approved, 4: Rejected, 5: Returned, etc.
    ActionDescription NVARCHAR(500) NOT NULL,
    PreviousStatus INT NULL,
    NewStatus INT NULL,
    FieldName NVARCHAR(100) NULL,
    OldValue NVARCHAR(2000) NULL,
    NewValue NVARCHAR(2000) NULL,
    UserRole INT NULL,
    IPAddress NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedBy NVARCHAR(100) NULL,
    DeletedAt DATETIME2 NULL,
    CONSTRAINT FK_ApplicationHistories_Applications FOREIGN KEY (ApplicationId) REFERENCES Applications(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ApplicationHistories_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
);

CREATE INDEX IX_ApplicationHistories_ApplicationId ON ApplicationHistories(ApplicationId);
CREATE INDEX IX_ApplicationHistories_CreatedAt ON ApplicationHistories(CreatedAt);

-- Licenses Table
CREATE TABLE Licenses (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    LicenseNumber NVARCHAR(50) NOT NULL,
    ApplicationId UNIQUEIDENTIFIER NOT NULL,
    ApplicantId UNIQUEIDENTIFIER NOT NULL,
    ServiceId UNIQUEIDENTIFIER NOT NULL,
    IssuedDate DATETIME2 NOT NULL,
    ExpiryDate DATETIME2 NOT NULL,
    Status INT NOT NULL DEFAULT 1, -- 1: Active, 2: Expired, 3: Revoked, 4: PendingRenewal
    CertificatePath NVARCHAR(500) NULL,
    CardPath NVARCHAR(500) NULL,
    RenewalNotificationSent BIT NOT NULL DEFAULT 0,
    RenewalNotificationSentAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedBy NVARCHAR(100) NULL,
    DeletedAt DATETIME2 NULL,
    CONSTRAINT UQ_Licenses_LicenseNumber UNIQUE (LicenseNumber),
    CONSTRAINT FK_Licenses_Applications FOREIGN KEY (ApplicationId) REFERENCES Applications(Id),
    CONSTRAINT FK_Licenses_Applicants FOREIGN KEY (ApplicantId) REFERENCES Applicants(Id),
    CONSTRAINT FK_Licenses_Services FOREIGN KEY (ServiceId) REFERENCES Services(Id)
);

CREATE INDEX IX_Licenses_LicenseNumber ON Licenses(LicenseNumber);
CREATE INDEX IX_Licenses_ApplicantId ON Licenses(ApplicantId);
CREATE INDEX IX_Licenses_Status ON Licenses(Status);
CREATE INDEX IX_Licenses_ExpiryDate ON Licenses(ExpiryDate);

-- EntityStaffMembers Table
CREATE TABLE EntityStaffMembers (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ApplicationId UNIQUEIDENTIFIER NOT NULL,
    ApplicantId UNIQUEIDENTIFIER NOT NULL,
    PractitionerLicenseNumber NVARCHAR(50) NOT NULL,
    IsAccepted BIT NULL,
    RespondedAt DATETIME2 NULL,
    ResponseComment NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CONSTRAINT FK_EntityStaffMembers_Applications FOREIGN KEY (ApplicationId) REFERENCES Applications(Id) ON DELETE CASCADE,
    CONSTRAINT FK_EntityStaffMembers_Applicants FOREIGN KEY (ApplicantId) REFERENCES Applicants(Id),
    CONSTRAINT UQ_EntityStaffMembers UNIQUE (ApplicationId, ApplicantId)
);

-- ApplicantSuspensions Table
CREATE TABLE ApplicantSuspensions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ApplicantId UNIQUEIDENTIFIER NOT NULL,
    SuspendedServices NVARCHAR(1000) NOT NULL,
    ReasonId UNIQUEIDENTIFIER NOT NULL,
    EnabledDate DATETIME2 NOT NULL,
    DisabledDate DATETIME2 NULL,
    Status INT NOT NULL DEFAULT 1, -- 1: Active, 2: Inactive
    Notes NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedBy NVARCHAR(100) NULL,
    DeletedAt DATETIME2 NULL,
    CONSTRAINT FK_ApplicantSuspensions_Applicants FOREIGN KEY (ApplicantId) REFERENCES Applicants(Id),
    CONSTRAINT FK_ApplicantSuspensions_ReasonCodes FOREIGN KEY (ReasonId) REFERENCES ReasonCodes(Id)
);

CREATE INDEX IX_ApplicantSuspensions_ApplicantId ON ApplicantSuspensions(ApplicantId);
CREATE INDEX IX_ApplicantSuspensions_Status ON ApplicantSuspensions(Status);

-- Notifications Table
CREATE TABLE Notifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NULL,
    ApplicantId UNIQUEIDENTIFIER NULL,
    ApplicationId UNIQUEIDENTIFIER NULL,
    Type INT NOT NULL,
    TitleEnglish NVARCHAR(200) NOT NULL,
    TitleArabic NVARCHAR(200) NOT NULL,
    MessageEnglish NVARCHAR(1000) NOT NULL,
    MessageArabic NVARCHAR(1000) NOT NULL,
    IsRead BIT NOT NULL DEFAULT 0,
    ReadAt DATETIME2 NULL,
    IsEmailSent BIT NOT NULL DEFAULT 0,
    EmailSentAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Notifications_Applications FOREIGN KEY (ApplicationId) REFERENCES Applications(Id) ON DELETE SET NULL
);

CREATE INDEX IX_Notifications_UserId ON Notifications(UserId);
CREATE INDEX IX_Notifications_IsRead ON Notifications(IsRead);

-- SLAConfigurations Table
CREATE TABLE SLAConfigurations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ConfigKey NVARCHAR(100) NOT NULL,
    SLATotalDays INT NOT NULL DEFAULT 5,
    EscalationToSpecialistDays INT NOT NULL DEFAULT 2,
    EscalationToSectionHeadDays INT NOT NULL DEFAULT 3,
    MaxReturnCount INT NOT NULL DEFAULT 3,
    MaxTasksPerOfficer INT NOT NULL DEFAULT 10,
    TaskDistributionIntervalMinutes INT NOT NULL DEFAULT 3,
    OnlineGracePeriodMinutes INT NOT NULL DEFAULT 2,
    LicenseValidityDays INT NOT NULL DEFAULT 365,
    RenewalNotificationDays INT NOT NULL DEFAULT 30,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CONSTRAINT UQ_SLAConfigurations_ConfigKey UNIQUE (ConfigKey)
);

-- SystemConfigurations Table
CREATE TABLE SystemConfigurations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Key] NVARCHAR(100) NOT NULL,
    Value NVARCHAR(2000) NOT NULL,
    Description NVARCHAR(500) NULL,
    DataType NVARCHAR(50) NOT NULL DEFAULT 'string',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CONSTRAINT UQ_SystemConfigurations_Key UNIQUE ([Key])
);

-- AuditLogs Table
CREATE TABLE AuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EntityName NVARCHAR(100) NOT NULL,
    EntityId UNIQUEIDENTIFIER NOT NULL,
    Action NVARCHAR(50) NOT NULL,
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    UserId NVARCHAR(100) NULL,
    UserName NVARCHAR(200) NULL,
    IPAddress NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL
);

CREATE INDEX IX_AuditLogs_EntityName_EntityId ON AuditLogs(EntityName, EntityId);
CREATE INDEX IX_AuditLogs_CreatedAt ON AuditLogs(CreatedAt);

-- Insert Default Data

-- Insert SLA Configuration
INSERT INTO SLAConfigurations (Id, ConfigKey, SLATotalDays, EscalationToSpecialistDays, EscalationToSectionHeadDays, MaxReturnCount, MaxTasksPerOfficer, TaskDistributionIntervalMinutes, OnlineGracePeriodMinutes, LicenseValidityDays, RenewalNotificationDays, IsActive)
VALUES ('00000000-0000-0000-0000-000000000001', 'Default', 5, 2, 3, 3, 10, 3, 2, 365, 30, 1);

-- Insert Services
INSERT INTO Services (Id, ServiceCode, NameEnglish, NameArabic, DescriptionEnglish, DescriptionArabic, ServiceType, ServiceCategory, ProcessingDays, SLADays, IsActive)
VALUES 
('10000000-0000-0000-0000-000000000001', 'DOH/0208', 'Register as an OSH General Practitioner', N'التسجيل كممارس سلامة وصحة مهنية', 'Through this service, you will be able to obtain an Occupational Safety and Health General Practitioners registration.', N'من خلال هذه الخدمة ستتمكن من الحصول على تسجيل ممارس عام للسلامة والصحة المهنية', 1, 1, 15, 5, 1),
('10000000-0000-0000-0000-000000000002', 'DOH/0209', 'Register as an OSH Senior Practitioner', N'التسجيل كممارس أول للسلامة والصحة المهنية', 'Through this service, you will be able to obtain an Occupational Safety and Health Senior Practitioners registration.', N'من خلال هذه الخدمة ستتمكن من الحصول على تسجيل ممارس أول للسلامة والصحة المهنية', 1, 2, 15, 5, 1),
('10000000-0000-0000-0000-000000000003', 'DOH/0214', 'Register as an OSH Health Auditor', N'التسجيل كمدقق سلامة وصحة مهنية', 'Through this service, you will be able to obtain an Occupational Safety and Health Auditor registration.', N'من خلال هذه الخدمة ستتمكن من الحصول على تسجيل مدقق للسلامة والصحة المهنية', 1, 3, 15, 5, 1),
('10000000-0000-0000-0000-000000000004', 'DOH/0211', 'Register as an Asbestos Supervising Consultant', N'التسجيل كاستشاري مشرف على الأسبستوس', 'Through this service, you will be able to obtain an Asbestos Supervising Consultants registration.', N'من خلال هذه الخدمة ستتمكن من الحصول على تسجيل استشاري مشرف على الأسبستوس', 1, 4, 15, 5, 1),
('10000000-0000-0000-0000-000000000005', 'DOH/0212', 'Register as a Workplace First Aider', N'التسجيل كمسعف أولي في مكان العمل', 'Through this service, you will be able to obtain a Workplace First Aider registration.', N'من خلال هذه الخدمة ستتمكن من الحصول على تسجيل مسعف أولي في مكان العمل', 1, 5, 15, 5, 1),
('10000000-0000-0000-0000-000000000006', 'DOH/0213', 'Registration as an OSH Consultancy Office', N'التسجيل كمكتب استشارات السلامة والصحة المهنية', 'Through this service, the Service Provider will be able to register as an Occupational Safety and Health Consultancy office.', N'من خلال هذه الخدمة سيتمكن مكتب الاستشارات من الحصول على تسجيل مكتب استشارات السلامة والصحة المهنية', 2, 6, 15, 5, 1),
('10000000-0000-0000-0000-000000000007', 'DOH/0222', 'Registration as an OSH Auditing Office', N'التسجيل كمكتب تدقيق للسلامة والصحة المهنية', 'Through this service, the Service Provider will be able to register as an Occupational Safety and Health Auditing Office.', N'من خلال هذه الخدمة سيتمكن مكتب التدقيق من الحصول على تسجيل مكتب تدقيق للسلامة والصحة المهنية', 2, 7, 15, 5, 1);

-- Insert Reason Codes
INSERT INTO ReasonCodes (Id, Code, DescriptionEnglish, DescriptionArabic, ReasonType, IsActive)
VALUES 
('20000000-0000-0000-0000-000000000001', 'REJ001', 'Incomplete documentation', N'الوثائق غير مكتملة', 1, 1),
('20000000-0000-0000-0000-000000000002', 'REJ002', 'Invalid credentials', N'بيانات اعتماد غير صالحة', 1, 1),
('20000000-0000-0000-0000-000000000003', 'REJ003', 'Does not meet requirements', N'لا يستوفي المتطلبات', 1, 1),
('20000000-0000-0000-0000-000000000004', 'RET001', 'Additional documents required', N'مطلوب مستندات إضافية', 2, 1),
('20000000-0000-0000-0000-000000000005', 'RET002', 'Clarification needed', N'يحتاج إلى توضيح', 2, 1),
('20000000-0000-0000-0000-000000000006', 'SUS001', 'Non-compliance', N'عدم الامتثال', 3, 1),
('20000000-0000-0000-0000-000000000007', 'SUS002', 'Fraudulent activity', N'نشاط احتيالي', 3, 1),
('20000000-0000-0000-0000-000000000008', 'REA001', 'Workload balancing', N'موازنة عبء العمل', 4, 1),
('20000000-0000-0000-0000-000000000009', 'REA002', 'User unavailable', N'المستخدم غير متاح', 4, 1);

-- Insert Default Admin User
INSERT INTO Users (Id, FirstName, LastName, Email, Role, Status, IsActive)
VALUES ('30000000-0000-0000-0000-000000000001', 'System', 'Admin', 'admin@adphc.ae', 6, 1, 1);

GO

PRINT 'Database schema created successfully!';
