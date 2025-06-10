using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.AllergenServices.Commands.UpdateAllergen;

public class UpdateAllergenCommand : IRequest<AppResponse<AllergenDto>>
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? ScientificName { get; set; }
    public string? Description { get; set; }
    public bool? IsFdaMajor { get; set; }
    public bool? IsEuMajor { get; set; }
}

public class UpdateAllergenCommandValidator : AbstractValidator<UpdateAllergenCommand>
{
    public UpdateAllergenCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than 0");

        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Category)
            .MaximumLength(50).WithMessage("Category cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.Category));

        RuleFor(x => x.ScientificName)
            .MaximumLength(100).WithMessage("Scientific name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.ScientificName));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
} 