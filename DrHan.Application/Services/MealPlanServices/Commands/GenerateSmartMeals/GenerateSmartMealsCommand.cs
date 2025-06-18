using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using MediatR;

namespace DrHan.Application.Services.MealPlanServices.Commands.GenerateSmartMeals;

public record GenerateSmartMealsCommand(int MealPlanId, GenerateSmartMealsDto Request) : IRequest<AppResponse<MealPlanDto>>; 