using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using MediatR;

namespace DrHan.Application.Services.MealPlanServices.Commands.CreateMealPlan;

public record CreateMealPlanCommand(CreateMealPlanDto MealPlan) : IRequest<AppResponse<MealPlanDto>>; 