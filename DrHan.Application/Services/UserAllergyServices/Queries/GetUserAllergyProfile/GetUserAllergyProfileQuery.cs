using MediatR;
using FluentValidation;
using DrHan.Application.DTOs.Users;
using DrHan.Application.Commons;

namespace DrHan.Application.Services.UserAllergyServices.Queries.GetUserAllergyProfile;

public class GetUserAllergyProfileQuery : IRequest<AppResponse<UserAllergyProfileDto>>
{
    public int UserId { get; set; }
}

public class GetUserAllergyProfileQueryValidator : AbstractValidator<GetUserAllergyProfileQuery>
{
    public GetUserAllergyProfileQueryValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID must be greater than 0");
    }
} 