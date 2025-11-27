using AutoMapper;
using Qudorat.Application.DTOs;
using Qudorat.Core.Entities;

namespace Qudorat.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>();
        CreateMap<CreateUserDto, User>();
        CreateMap<UpdateUserDto, User>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Applicant mappings
        CreateMap<Applicant, ApplicantDto>();
        CreateMap<Applicant, ApplicantDetailDto>()
            .ForMember(dest => dest.Applications, opt => opt.MapFrom(src => src.Applications))
            .ForMember(dest => dest.Licenses, opt => opt.MapFrom(src => src.Licenses))
            .ForMember(dest => dest.ActiveSuspensions, opt => opt.MapFrom(src => 
                src.Suspensions.Where(s => s.Status == Core.Enums.SuspensionStatus.Active)));

        // Application mappings
        CreateMap<Core.Entities.Application, ApplicationDto>()
            .ForMember(dest => dest.ApplicantName, opt => opt.MapFrom(src => src.Applicant.FullName))
            .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service.NameEnglish))
            .ForMember(dest => dest.ServiceType, opt => opt.MapFrom(src => src.Service.ServiceType))
            .ForMember(dest => dest.AssignedUserName, opt => opt.MapFrom(src => src.AssignedUser != null ? src.AssignedUser.FullName : null));

        CreateMap<Core.Entities.Application, ApplicationDetailDto>()
            .ForMember(dest => dest.ApplicantDocuments, opt => opt.MapFrom(src => 
                src.Documents.Where(d => d.IsApplicantDocument)))
            .ForMember(dest => dest.InternalDocuments, opt => opt.MapFrom(src => 
                src.Documents.Where(d => !d.IsApplicantDocument)))
            .ForMember(dest => dest.History, opt => opt.MapFrom(src => src.Histories.OrderByDescending(h => h.CreatedAt)))
            .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments.OrderByDescending(c => c.CreatedAt)));

        CreateMap<Core.Entities.Application, ApplicationSummaryDto>()
            .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service.NameEnglish));

        // Document mappings
        CreateMap<ApplicationDocument, ApplicationDocumentDto>();

        // History mappings
        CreateMap<ApplicationHistory, ApplicationHistoryDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : "System"));

        // Comment mappings
        CreateMap<ApplicationComment, ApplicationCommentDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName))
            .ForMember(dest => dest.ReasonDescription, opt => opt.MapFrom(src => src.Reason != null ? src.Reason.DescriptionEnglish : null));

        // Service mappings
        CreateMap<Service, ServiceDto>();
        CreateMap<Service, ServiceDetailDto>()
            .ForMember(dest => dest.RequiredDocuments, opt => opt.MapFrom(src => 
                src.RequiredDocuments.OrderBy(d => d.DisplayOrder)));
        CreateMap<ServiceDocument, ServiceDocumentDto>();

        // License mappings
        CreateMap<License, LicenseDto>()
            .ForMember(dest => dest.ApplicantName, opt => opt.MapFrom(src => src.Applicant.FullName))
            .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service.NameEnglish));
        CreateMap<License, LicenseSummaryDto>()
            .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service.NameEnglish));

        // Suspension mappings
        CreateMap<ApplicantSuspension, SuspensionDto>()
            .ForMember(dest => dest.ApplicantName, opt => opt.MapFrom(src => src.Applicant.FullName))
            .ForMember(dest => dest.ApplicantEmail, opt => opt.MapFrom(src => src.Applicant.Email))
            .ForMember(dest => dest.SuspendedServices, opt => opt.MapFrom(src => 
                src.SuspendedServices.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()))
            .ForMember(dest => dest.ReasonDescription, opt => opt.MapFrom(src => src.Reason.DescriptionEnglish));

        // Notification mappings
        CreateMap<Notification, NotificationDto>();

        // Reason Code mappings
        CreateMap<ReasonCode, ReasonCodeDto>();
    }
}
