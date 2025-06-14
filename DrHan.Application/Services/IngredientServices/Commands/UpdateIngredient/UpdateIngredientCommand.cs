using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Ingredients;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.IngredientServices.Commands.UpdateIngredient;

public class UpdateIngredientCommand : IRequest<AppResponse<IngredientDto>>
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public List<UpdateIngredientNutritionDto>? Nutritions { get; set; }
    public List<UpdateIngredientNameDto>? AlternativeNames { get; set; }
    public List<int>? AllergenIds { get; set; }
}

public class UpdateIngredientCommandValidator : AbstractValidator<UpdateIngredientCommand>
{
    public UpdateIngredientCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than 0");
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(255).WithMessage("Name cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));
        
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Category));
        
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleForEach(x => x.Nutritions).SetValidator(new UpdateIngredientNutritionValidator())
            .When(x => x.Nutritions != null);
        
        RuleForEach(x => x.AlternativeNames).SetValidator(new UpdateIngredientNameValidator())
            .When(x => x.AlternativeNames != null);
    }
}

public class UpdateIngredientNutritionValidator : AbstractValidator<UpdateIngredientNutritionDto>
{
    public UpdateIngredientNutritionValidator()
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

public class UpdateIngredientNameValidator : AbstractValidator<UpdateIngredientNameDto>
{
    public UpdateIngredientNameValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Alternative name is required")
            .MaximumLength(255).WithMessage("Alternative name cannot exceed 255 characters");
    }
} 