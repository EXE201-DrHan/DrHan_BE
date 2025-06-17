using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using MediatR;

namespace DrHan.Application.Services.MealPlanServices.Commands.GenerateSmartMealPlan;

public record GenerateSmartMealPlanCommand(GenerateMealPlanDto Request) : IRequest<AppResponse<MealPlanDto>>; 