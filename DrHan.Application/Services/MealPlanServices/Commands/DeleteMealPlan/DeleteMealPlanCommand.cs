using DrHan.Application.Commons;
using MediatR;

namespace DrHan.Application.Services.MealPlanServices.Commands.DeleteMealPlan;

public record DeleteMealPlanCommand(int Id) : IRequest<AppResponse<bool>>; 