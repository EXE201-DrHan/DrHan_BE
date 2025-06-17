using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using MediatR;

namespace DrHan.Application.Services.MealPlanServices.Queries.GetMealPlanById;

public record GetMealPlanByIdQuery(int Id) : IRequest<AppResponse<MealPlanDto>>; 