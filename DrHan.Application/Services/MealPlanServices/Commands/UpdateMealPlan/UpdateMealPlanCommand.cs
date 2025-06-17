using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using MediatR;

namespace DrHan.Application.Services.MealPlanServices.Commands.UpdateMealPlan;

public record UpdateMealPlanCommand(UpdateMealPlanDto MealPlan) : IRequest<AppResponse<MealPlanDto>>; 