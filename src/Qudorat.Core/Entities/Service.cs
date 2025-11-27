using Qudorat.Core.Common;
using Qudorat.Core.Enums;

namespace Qudorat.Core.Entities;

public class Service : BaseEntity
{
    public string ServiceCode { get; set; } = string.Empty;
    public string NameEnglish { get; set; } = string.Empty;
    public string NameArabic { get; set; } = string.Empty;
    public string DescriptionEnglish { get; set; } = string.Empty;
    public string DescriptionArabic { get; set; } = string.Empty;
    public ServiceType ServiceType { get; set; }
    public ServiceCategory ServiceCategory { get; set; }
    public decimal? ServiceFee { get; set; }
    public int ProcessingDays { get; set; } = 15;
    public int SLADays { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public string? TermsEnglish { get; set; }
    public string? TermsArabic { get; set; }
    
    // Navigation properties
    public virtual ICollection<ServiceDocument> RequiredDocuments { get; set; } = new List<ServiceDocument>();
    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
}

public class ServiceDocument : BaseEntity
{
    public Guid ServiceId { get; set; }
    public string DocumentNameEnglish { get; set; } = string.Empty;
    public string DocumentNameArabic { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public int DisplayOrder { get; set; }
    
    // Navigation properties
    public virtual Service Service { get; set; } = null!;
}
