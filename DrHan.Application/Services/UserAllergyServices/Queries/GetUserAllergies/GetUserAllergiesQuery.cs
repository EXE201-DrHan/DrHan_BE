using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Users;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.UserAllergyServices.Queries.GetUserAllergies;

public class GetUserAllergiesQuery : IRequest<AppResponse<IEnumerable<UserAllergyDto>>>
{
    public int UserId { get; set; }
}

public class GetUserAllergiesQueryValidator : AbstractValidator<GetUserAllergiesQuery>
{
    public GetUserAllergiesQueryValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID must be greater than 0");
    }
} 