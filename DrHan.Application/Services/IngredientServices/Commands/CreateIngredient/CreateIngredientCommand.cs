using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Ingredients;
using DrHan.Application.Commons;
using System.ComponentModel.DataAnnotations;

namespace DrHan.Application.Services.IngredientServices.Commands.CreateIngredient;

public class CreateIngredientCommand : IRequest<AppResponse<IngredientDto>>
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<CreateIngredientNutritionDto> Nutritions { get; set; } = new();
    public List<CreateIngredientNameDto> AlternativeNames { get; set; } = new();
    public List<int> AllergenIds { get; set; } = new();
}

public class CreateIngredientCommandValidator : AbstractValidator<CreateIngredientCommand>
{
    public CreateIngredientCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");
        
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters");
        
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        RuleForEach(x => x.Nutritions).SetValidator(new CreateIngredientNutritionValidator());
        RuleForEach(x => x.AlternativeNames).SetValidator(new CreateIngredientNameValidator());
    }
}

public class CreateIngredientNutritionValidator : AbstractValidator<CreateIngredientNutritionDto>
{
    public CreateIngredientNutritionValidator()
    {
        RuleFor(x => x.NutrientName)
            .NotEmpty().WithMessage("Nutrient name is required")
            .MaximumLength(100).WithMessage("Nutrient name cannot exceed 100 characters");
        
        RuleFor(x => x.AmountPer100g)
            .GreaterThanOrEqualTo(0).WithMessage("Amount must be greater than or equal to 0")
            .When(x => x.AmountPer100g.HasValue);
        
        RuleFor(x => x.Unit)
            .MaximumLength(20).WithMessage("Unit cannot exceed 20 characters");
    }
}

public class CreateIngredientNameValidator : AbstractValidator<CreateIngredientNameDto>
{
    public CreateIngredientNameValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Alternative name is required")
            .MaximumLength(255).WithMessage("Alternative name cannot exceed 255 characters");
    }
} 