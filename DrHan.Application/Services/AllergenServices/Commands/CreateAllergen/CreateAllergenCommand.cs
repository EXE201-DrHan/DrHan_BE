using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.AllergenServices.Commands.CreateAllergen;

public class CreateAllergenCommand : IRequest<AppResponse<AllergenDto>>
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ScientificName { get; set; }
    public string? Description { get; set; }
    public bool? IsFdaMajor { get; set; }
    public bool? IsEuMajor { get; set; }
}

public class CreateAllergenCommandValidator : AbstractValidator<CreateAllergenCommand>
{
    public CreateAllergenCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(50).WithMessage("Category cannot exceed 50 characters");

        RuleFor(x => x.ScientificName)
            .MaximumLength(100).WithMessage("Scientific name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.ScientificName));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
} 