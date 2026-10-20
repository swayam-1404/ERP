using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VHEI.Infrastructure;
namespace VHEI.Api;

public class InstitutionAccessFilter(ErpDbContext db) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var route = context.HttpContext.Request.RouteValues["institutionId"]?.ToString();
        if (!Guid.TryParse(route, out var institutionId) || !Guid.TryParse(context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),out var userId)) return Results.Unauthorized();
        if(await db.Users.AnyAsync(x=>x.Id==userId&&x.MustChangePassword))return Results.Problem("Change your temporary password before opening the workspace.",statusCode:403);
        if (!await db.InstitutionAssignments.AnyAsync(x => x.InstitutionId == institutionId && x.UserId == userId, context.HttpContext.RequestAborted)) return Results.Problem("Access to this institution is not assigned.", statusCode:403);
        return await next(context);
    }
}
public static class Security
{
    public static Guid Actor(this HttpContext context) => Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
