using InTakeWise.Data;  
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class RequireProfileCompletedAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        var routeValues = context.RouteData.Values;
        var controller = (routeValues["controller"]?.ToString() ?? "");
        var action = (routeValues["action"]?.ToString() ?? "");

        if (controller.Equals("ProfileWizard", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();

        var user = await userManager.GetUserAsync(context.HttpContext.User);
        if (user == null)
        {
            await next();
            return;
        }

        var hasProfile = await db.UsersInformation.AnyAsync(x => x.UserId == user.Id);

        if (!hasProfile)
        {
            context.Result = new RedirectToActionResult("Step1", "ProfileWizard", null);
            return;
        }

        await next();
    }
}
