using FluentValidation;
using Qudorat.Application.DTOs;

namespace Qudorat.Application.Validators;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role");
    }
}

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role");
    }
}

public class CreateApplicationDtoValidator : AbstractValidator<CreateApplicationDto>
{
    public CreateApplicationDtoValidator()
    {
        RuleFor(x => x.TammRequestId)
            .NotEmpty().WithMessage("TAMM Request ID is required");

        RuleFor(x => x.ApplicantEmiratesId)
            .NotEmpty().WithMessage("Emirates ID is required")
            .Length(15, 15).WithMessage("Emirates ID must be 15 characters");

        RuleFor(x => x.ApplicantFirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100);

        RuleFor(x => x.ApplicantLastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100);

        RuleFor(x => x.ApplicantEmail)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.ApplicantPhone)
            .NotEmpty().WithMessage("Phone number is required");

        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithMessage("Service code is required");

        RuleFor(x => x.PreferredCommunication)
            .IsInEnum().WithMessage("Invalid communication preference");

        RuleFor(x => x.CommunicationLanguage)
            .IsInEnum().WithMessage("Invalid communication language");
    }
}

public class ApplicationActionDtoValidator : AbstractValidator<ApplicationActionDto>
{
    public ApplicationActionDtoValidator()
    {
        RuleFor(x => x.Comment)
            .MaximumLength(2000).WithMessage("Comment cannot exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Comment));
    }
}

public class ReassignApplicationDtoValidator : AbstractValidator<ReassignApplicationDto>
{
    public ReassignApplicationDtoValidator()
    {
        RuleFor(x => x.ToUserId)
            .NotEmpty().WithMessage("Target user is required");

        RuleFor(x => x.ReasonId)
            .NotEmpty().WithMessage("Reason is required");
    }
}

public class CreateSuspensionDtoValidator : AbstractValidator<CreateSuspensionDto>
{
    public CreateSuspensionDtoValidator()
    {
        RuleFor(x => x.ApplicantEmail)
            .NotEmpty().WithMessage("Applicant email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.ServiceIds)
            .NotEmpty().WithMessage("At least one service must be selected");

        RuleFor(x => x.ReasonId)
            .NotEmpty().WithMessage("Reason is required");
    }
}

public class AddCommentDtoValidator : AbstractValidator<AddCommentDto>
{
    public AddCommentDtoValidator()
    {
        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Comment is required")
            .MaximumLength(2000).WithMessage("Comment cannot exceed 2000 characters");
    }
}

public class PaginationParamsValidator : AbstractValidator<PaginationParams>
{
    public PaginationParamsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");
    }
}
