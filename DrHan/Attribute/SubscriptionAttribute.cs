using DrHan.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace DrHan.API.Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class TrackUsageAttribute : System.Attribute, IAsyncActionFilter
{
    private readonly string _featureName;
    private readonly string _limitType;

    public TrackUsageAttribute(string featureName, string limitType = "daily")
    {
        _featureName = featureName;
        _limitType = limitType;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var subscriptionService = context.HttpContext.RequestServices
            .GetRequiredService<ISubscriptionService>();

        var userIdClaim = context.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (int.TryParse(userIdClaim, out int userId))
        {
            var canUse = await subscriptionService.CanUseFeature(userId, _featureName, _limitType);

            if (!canUse)
            {
                context.Result = new ObjectResult(new
                {
                    message = "Usage limit exceeded",
                    feature = _featureName,
                    limitType = _limitType
                })
                {
                    StatusCode = 429
                };
                return;
            }
        }

        var executedContext = await next();

        if (executedContext.Result is OkObjectResult || executedContext.Result is OkResult)
        {
            if (int.TryParse(userIdClaim, out userId))
            {
                await subscriptionService.TrackUsage(userId, _featureName);
            }
        }
    }
}

