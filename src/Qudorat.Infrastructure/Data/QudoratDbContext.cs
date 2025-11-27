using Microsoft.EntityFrameworkCore;
using Qudorat.Core.Entities;

namespace Qudorat.Infrastructure.Data;

public class QudoratDbContext : DbContext
{
    public QudoratDbContext(DbContextOptions<QudoratDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Applicant> Applicants => Set<Applicant>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceDocument> ServiceDocuments => Set<ServiceDocument>();
    public DbSet<Core.Entities.Application> Applications => Set<Core.Entities.Application>();
    public DbSet<ApplicationDocument> ApplicationDocuments => Set<ApplicationDocument>();
    public DbSet<ApplicationComment> ApplicationComments => Set<ApplicationComment>();
    public DbSet<ApplicationHistory> ApplicationHistories => Set<ApplicationHistory>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<EntityStaffMember> EntityStaffMembers => Set<EntityStaffMember>();
    public DbSet<ApplicantSuspension> ApplicantSuspensions => Set<ApplicantSuspension>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ReasonCode> ReasonCodes => Set<ReasonCode>();
    public DbSet<SLAConfiguration> SLAConfigurations => Set<SLAConfiguration>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Applicant configuration
        modelBuilder.Entity<Applicant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmiratesId).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.Property(e => e.EmiratesId).HasMaxLength(15).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Service configuration
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ServiceCode).IsUnique();
            entity.Property(e => e.ServiceCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.NameEnglish).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NameArabic).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DescriptionEnglish).HasMaxLength(2000);
            entity.Property(e => e.DescriptionArabic).HasMaxLength(2000);
            entity.Property(e => e.TermsEnglish).HasMaxLength(4000);
            entity.Property(e => e.TermsArabic).HasMaxLength(4000);
            entity.Property(e => e.ServiceFee).HasPrecision(18, 2);
            entity.HasMany(e => e.RequiredDocuments)
                  .WithOne(e => e.Service)
                  .HasForeignKey(e => e.ServiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ServiceDocument configuration
        modelBuilder.Entity<ServiceDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocumentNameEnglish).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DocumentNameArabic).HasMaxLength(200).IsRequired();
        });

        // Application configuration
        modelBuilder.Entity<Core.Entities.Application>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RequestNumber).IsUnique();
            entity.HasIndex(e => e.TammRequestId);
            entity.HasIndex(e => new { e.ApplicantId, e.ServiceId, e.QudoratStatus });
            entity.Property(e => e.RequestNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.TammRequestId).HasMaxLength(100);
            entity.Property(e => e.ServiceCharges).HasPrecision(18, 2);
            entity.Property(e => e.FormData).HasColumnType("nvarchar(max)");
            
            entity.HasOne(e => e.Applicant)
                  .WithMany(e => e.Applications)
                  .HasForeignKey(e => e.ApplicantId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Service)
                  .WithMany(e => e.Applications)
                  .HasForeignKey(e => e.ServiceId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.AssignedUser)
                  .WithMany(e => e.AssignedApplications)
                  .HasForeignKey(e => e.AssignedUserId)
                  .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.License)
                  .WithOne(e => e.Application)
                  .HasForeignKey<License>(e => e.ApplicationId);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ApplicationDocument configuration
        modelBuilder.Entity<ApplicationDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FileType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // ApplicationComment configuration
        modelBuilder.Entity<ApplicationComment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Comment).HasMaxLength(2000).IsRequired();
            
            entity.HasOne(e => e.Reason)
                  .WithMany(e => e.Comments)
                  .HasForeignKey(e => e.ReasonId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ApplicationHistory configuration
        modelBuilder.Entity<ApplicationHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ApplicationId, e.CreatedAt });
            entity.Property(e => e.ActionDescription).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FieldName).HasMaxLength(100);
            entity.Property(e => e.OldValue).HasMaxLength(2000);
            entity.Property(e => e.NewValue).HasMaxLength(2000);
            entity.Property(e => e.IPAddress).HasMaxLength(50);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // License configuration
        modelBuilder.Entity<License>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LicenseNumber).IsUnique();
            entity.HasIndex(e => new { e.ApplicantId, e.Status });
            entity.Property(e => e.LicenseNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CertificatePath).HasMaxLength(500);
            entity.Property(e => e.CardPath).HasMaxLength(500);
            
            entity.HasOne(e => e.Applicant)
                  .WithMany(e => e.Licenses)
                  .HasForeignKey(e => e.ApplicantId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Service)
                  .WithMany()
                  .HasForeignKey(e => e.ServiceId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // EntityStaffMember configuration
        modelBuilder.Entity<EntityStaffMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ApplicationId, e.ApplicantId }).IsUnique();
            entity.Property(e => e.PractitionerLicenseNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ResponseComment).HasMaxLength(500);
        });

        // ApplicantSuspension configuration
        modelBuilder.Entity<ApplicantSuspension>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ApplicantId, e.Status });
            entity.Property(e => e.SuspendedServices).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.Reason)
                  .WithMany(e => e.Suspensions)
                  .HasForeignKey(e => e.ReasonId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.Property(e => e.TitleEnglish).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TitleArabic).HasMaxLength(200).IsRequired();
            entity.Property(e => e.MessageEnglish).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.MessageArabic).HasMaxLength(1000).IsRequired();
        });

        // ReasonCode configuration
        modelBuilder.Entity<ReasonCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Code, e.ReasonType }).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DescriptionEnglish).HasMaxLength(500).IsRequired();
            entity.Property(e => e.DescriptionArabic).HasMaxLength(500).IsRequired();
        });

        // SLAConfiguration
        modelBuilder.Entity<SLAConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConfigKey).IsUnique();
            entity.Property(e => e.ConfigKey).HasMaxLength(100).IsRequired();
        });

        // SystemConfiguration
        modelBuilder.Entity<SystemConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Value).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DataType).HasMaxLength(50).IsRequired();
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityName, e.EntityId });
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.EntityName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
            entity.Property(e => e.OldValues).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NewValues).HasColumnType("nvarchar(max)");
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.UserName).HasMaxLength(200);
            entity.Property(e => e.IPAddress).HasMaxLength(50);
        });

        // Seed initial data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed SLA Configuration
        modelBuilder.Entity<SLAConfiguration>().HasData(new SLAConfiguration
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            ConfigKey = "Default",
            SLATotalDays = 5,
            EscalationToSpecialistDays = 2,
            EscalationToSectionHeadDays = 3,
            MaxReturnCount = 3,
            MaxTasksPerOfficer = 10,
            TaskDistributionIntervalMinutes = 3,
            OnlineGracePeriodMinutes = 2,
            LicenseValidityDays = 365,
            RenewalNotificationDays = 30,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        // Seed Services
        var services = new[]
        {
            new Service { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), ServiceCode = "DOH/0208", NameEnglish = "Register as an OSH General Practitioner", NameArabic = "التسجيل كممارس سلامة وصحة مهنية", DescriptionEnglish = "Through this service, you will be able to obtain an Occupational Safety and Health General Practitioners registration to work in the field of Occupational Safety and Health.", DescriptionArabic = "من خلال هذه الخدمة ستتمكن من الحصول على تسجيل ممارس عام للسلامة والصحة المهنية للعمل في مجال السلامة والصحة المهنية", ServiceType = Core.Enums.ServiceType.Individual, ServiceCategory = Core.Enums.ServiceCategory.NewGeneralPractitioner, ProcessingDays = 15, SLADays = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Service { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), ServiceCode = "DOH/0209", NameEnglish = "Register as an OSH Senior Practitioner", NameArabic = "التسجيل كممارس أول للسلامة والصحة المهنية", DescriptionEnglish = "Through this service, you will be able to obtain an Occupational Safety and Health Senior Practitioners registration to work in the field of Occupational Safety and Health.", DescriptionArabic = "من خلال هذه الخدمة ستتمكن من الحصول على تسجيل ممارس أول للسلامة والصحة المهنية للعمل في مجال السلامة والصحة المهنية", ServiceType = Core.Enums.ServiceType.Individual, ServiceCategory = Core.Enums.ServiceCategory.NewSeniorPractitioner, ProcessingDays = 15, SLADays = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Service { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), ServiceCode = "DOH/0214", NameEnglish = "Register as an OSH Health Auditor", NameArabic = "التسجيل كمدقق سلامة وصحة مهنية", DescriptionEnglish = "Through this service, you will be able to obtain an Occupational Safety and Health Auditor registration to deliver Auditing Services in the field of Occupational Safety and Health.", DescriptionArabic = "من خلال هذه الخدمة ستتمكن من الحصول على تسجيل مدقق للسلامة والصحة المهنية للعمل في مجال السلامة والصحة المهنية", ServiceType = Core.Enums.ServiceType.Individual, ServiceCategory = Core.Enums.ServiceCategory.NewOSHAuditor, ProcessingDays = 15, SLADays = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Service { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), ServiceCode = "DOH/0211", NameEnglish = "Register as an Asbestos Supervising Consultant", NameArabic = "التسجيل كاستشاري مشرف على الأسبستوس", DescriptionEnglish = "Through this service, you will be able to obtain an Asbestos Supervising Consultants registration to deliver Asbestos Supervising Consultancy Services.", DescriptionArabic = "من خلال هذه الخدمة ستتمكن من الحصول على تسجيل استشاري مشرف على الأسبستوس لتقديم خدمات استشارات اشرافية على الأسبستوس", ServiceType = Core.Enums.ServiceType.Individual, ServiceCategory = Core.Enums.ServiceCategory.NewAsbestosSupervisingConsultant, ProcessingDays = 15, SLADays = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Service { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), ServiceCode = "DOH/0212", NameEnglish = "Register as a Workplace First Aider", NameArabic = "التسجيل كمسعف أولي في مكان العمل", DescriptionEnglish = "Through this service, you will be able to obtain a Workplace First Aider registration that allows you to deliver First Aid services in the workplace.", DescriptionArabic = "من خلال هذه الخدمة ستتمكن من الحصول على تسجيل مسعف أولي في مكان العمل التي تسمح لك بتقديم خدمات الإسعافات الأولية في مكان العمل", ServiceType = Core.Enums.ServiceType.Individual, ServiceCategory = Core.Enums.ServiceCategory.NewWorkplaceFirstAider, ProcessingDays = 15, SLADays = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Service { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), ServiceCode = "DOH/0213", NameEnglish = "Registration as an OSH Consultancy Office", NameArabic = "التسجيل كمكتب استشارات السلامة والصحة المهنية", DescriptionEnglish = "Through this service, the Service Provider will be able to register as an Occupational Safety and Health Consultancy office to deliver Consultancy Services in the field of Occupational Safety and Health.", DescriptionArabic = "من خلال هذه الخدمة سيتمكن مكتب الاستشارات من الحصول على تسجيل مكتب استشارات السلامة والصحة المهنية لتقديم خدمات استشارات في السلامة والصحة المهنية", ServiceType = Core.Enums.ServiceType.Provider, ServiceCategory = Core.Enums.ServiceCategory.NewOSHConsultancyOffice, ProcessingDays = 15, SLADays = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Service { Id = Guid.Parse("10000000-0000-0000-0000-000000000007"), ServiceCode = "DOH/0222", NameEnglish = "Registration as an OSH Auditing Office", NameArabic = "التسجيل كمكتب تدقيق للسلامة و الصحة المهنية", DescriptionEnglish = "Through this service, the Service Provider will be able to register as an Occupational Safety and Health Auditing Office to deliver Auditing Services in the field of Occupational Safety and Health.", DescriptionArabic = "من خلال هذه الخدمة سيتمكن مكتب التدقيق من الحصول على تسجيل مكتب تدقيق للسلامة والصحة المهنية لتقديم خدمات التدقيق في السلامة والصحة المهنية", ServiceType = Core.Enums.ServiceType.Provider, ServiceCategory = Core.Enums.ServiceCategory.NewOSHAuditingOffice, ProcessingDays = 15, SLADays = 5, IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        modelBuilder.Entity<Service>().HasData(services);

        // Seed Reason Codes
        var reasonCodes = new[]
        {
            new ReasonCode { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), Code = "REJ001", DescriptionEnglish = "Incomplete documentation", DescriptionArabic = "الوثائق غير مكتملة", ReasonType = ReasonType.Rejection, IsActive = true, CreatedAt = DateTime.UtcNow },
            new ReasonCode { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), Code = "REJ002", DescriptionEnglish = "Invalid credentials", DescriptionArabic = "بيانات اعتماد غير صالحة", ReasonType = ReasonType.Rejection, IsActive = true, CreatedAt = DateTime.UtcNow },
            new ReasonCode { Id = Guid.Parse("20000000-0000-0000-0000-000000000003"), Code = "REJ003", DescriptionEnglish = "Does not meet requirements", DescriptionArabic = "لا يستوفي المتطلبات", ReasonType = ReasonType.Rejection, IsActive = true, CreatedAt = DateTime.UtcNow },
            new ReasonCode { Id = Guid.Parse("20000000-0000-0000-0000-000000000004"), Code = "RET001", DescriptionEnglish = "Additional documents required", DescriptionArabic = "مطلوب مستندات إضافية", ReasonType = ReasonType.Return, IsActive = true, CreatedAt = DateTime.UtcNow },
            new ReasonCode { Id = Guid.Parse("20000000-0000-0000-0000-000000000005"), Code = "RET002", DescriptionEnglish = "Clarification needed", DescriptionArabic = "يحتاج إلى توضيح", ReasonType = ReasonType.Return, IsActive = true, CreatedAt = DateTime.UtcNow },
            new ReasonCode { Id = Guid.Parse("20000000-0000-0000-0000-000000000006"), Code = "SUS001", DescriptionEnglish = "Non-compliance", DescriptionArabic = "عدم الامتثال", ReasonType = ReasonType.Suspension, IsActive = true, CreatedAt = DateTime.UtcNow },
            new ReasonCode { Id = Guid.Parse("20000000-0000-0000-0000-000000000007"), Code = "SUS002", DescriptionEnglish = "Fraudulent activity", DescriptionArabic = "نشاط احتيالي", ReasonType = ReasonType.Suspension, IsActive = true, CreatedAt = DateTime.UtcNow },
            new ReasonCode { Id = Guid.Parse("20000000-0000-0000-0000-000000000008"), Code = "REA001", DescriptionEnglish = "Workload balancing", DescriptionArabic = "موازنة عبء العمل", ReasonType = ReasonType.Reassignment, IsActive = true, CreatedAt = DateTime.UtcNow },
            new ReasonCode { Id = Guid.Parse("20000000-0000-0000-0000-000000000009"), Code = "REA002", DescriptionEnglish = "User unavailable", DescriptionArabic = "المستخدم غير متاح", ReasonType = ReasonType.Reassignment, IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        modelBuilder.Entity<ReasonCode>().HasData(reasonCodes);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Core.Common.BaseEntity baseEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        baseEntity.CreatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        baseEntity.UpdatedAt = DateTime.UtcNow;
                        break;
                }
            }

            if (entry.Entity is Core.Common.AuditableEntity auditableEntity && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                auditableEntity.IsDeleted = true;
                auditableEntity.DeletedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
