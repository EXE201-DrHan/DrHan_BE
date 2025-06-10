using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Users;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.UserAllergyServices.Commands.AddUserAllergy;

public class AddUserAllergyCommand : IRequest<AppResponse<UserAllergyDto>>
{
    public int UserId { get; set; }
    public int AllergenId { get; set; }
    public string? Severity { get; set; }
    public DateOnly? DiagnosisDate { get; set; }
    public string? DiagnosedBy { get; set; }
    public DateOnly? LastReactionDate { get; set; }
    public string? AvoidanceNotes { get; set; }
    public bool? Outgrown { get; set; }
    public DateOnly? OutgrownDate { get; set; }
    public bool? NeedsVerification { get; set; }
}

public class AddUserAllergyCommandValidator : AbstractValidator<AddUserAllergyCommand>
{
    public AddUserAllergyCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID must be greater than 0");

        RuleFor(x => x.AllergenId)
            .GreaterThan(0).WithMessage("Allergen ID must be greater than 0");

        RuleFor(x => x.Severity)
            .MaximumLength(50).WithMessage("Severity cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.Severity));

        RuleFor(x => x.DiagnosedBy)
            .MaximumLength(100).WithMessage("Diagnosed by cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.DiagnosedBy));

        RuleFor(x => x.AvoidanceNotes)
            .MaximumLength(1000).WithMessage("Avoidance notes cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.AvoidanceNotes));

        RuleFor(x => x.OutgrownDate)
            .GreaterThan(x => x.DiagnosisDate)
            .WithMessage("Outgrown date must be after diagnosis date")
            .When(x => x.OutgrownDate.HasValue && x.DiagnosisDate.HasValue);

        RuleFor(x => x.LastReactionDate)
            .GreaterThanOrEqualTo(x => x.DiagnosisDate)
            .WithMessage("Last reaction date cannot be before diagnosis date")
            .When(x => x.LastReactionDate.HasValue && x.DiagnosisDate.HasValue);
    }
} 