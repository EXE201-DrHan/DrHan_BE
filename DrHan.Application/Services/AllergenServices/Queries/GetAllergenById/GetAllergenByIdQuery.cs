using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Allergens;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.AllergenServices.Queries.GetAllergenById;

public class GetAllergenByIdQuery : IRequest<AppResponse<AllergenDto>>
{
    public int Id { get; set; }
}

public class GetAllergenByIdQueryValidator : AbstractValidator<GetAllergenByIdQuery>
{
    public GetAllergenByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than 0");
    }
} 