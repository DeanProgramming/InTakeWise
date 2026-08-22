using InTakeWise.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace InTakeWise.Filters
{
    public class RequireProfileCompletedAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
            {
                await next();
                return;
            }

            if (context.HttpContext.User.HasClaim(
                    DbSeeder.DemoClaimType,
                    DbSeeder.DemoClaimValue))
            {
                await next();
                return;
            }

            // 2. Skip [AllowAnonymous] endpoints.
            var hasAllowAnonymous =
                context.Filters.Any(f => f is IAllowAnonymousFilter) ||
                context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();

            if (hasAllowAnonymous)
            {
                await next();
                return;
            }

            // 3. Skip ProfileWizard to avoid redirect loops.
            var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
            if (controller.Equals("ProfileWizard", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            // 4. Skip Identity area endpoints.
            var area = context.RouteData.Values["area"]?.ToString() ?? "";
            if (area.Equals("Identity", StringComparison.OrdinalIgnoreCase))
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

            var hasProfile = await db.UsersInformation
                .AsNoTracking()
                .AnyAsync(x => x.UserId == user.Id);

            if (!hasProfile)
            {
                context.Result = new RedirectToActionResult("Step1", "ProfileWizard", null);
                return;
            }

            await next();
        }
    }
}
