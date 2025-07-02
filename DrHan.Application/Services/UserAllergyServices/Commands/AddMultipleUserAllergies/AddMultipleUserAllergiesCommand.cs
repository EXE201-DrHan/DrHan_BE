using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Users;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.UserAllergyServices.Commands.AddMultipleUserAllergies;

public class AddMultipleUserAllergiesCommand : IRequest<AppResponse<BulkUserAllergyResponseDto>>
{
    public int UserId { get; set; }
    public List<int> AllergenIds { get; set; } = new();
    public string? Severity { get; set; }
    public DateOnly? DiagnosisDate { get; set; }
    public string? DiagnosedBy { get; set; }
    public DateOnly? LastReactionDate { get; set; }
    public string? AvoidanceNotes { get; set; }
    public bool? Outgrown { get; set; }
    public DateOnly? OutgrownDate { get; set; }
    public bool? NeedsVerification { get; set; }
}

public class AddMultipleUserAllergiesCommandValidator : AbstractValidator<AddMultipleUserAllergiesCommand>
{
    public AddMultipleUserAllergiesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID must be greater than 0");

        RuleFor(x => x.AllergenIds)
            .NotEmpty().WithMessage("At least one allergen ID is required")
            .Must(ids => ids.All(id => id > 0)).WithMessage("All allergen IDs must be greater than 0")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Duplicate allergen IDs are not allowed");

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