using InTakeWise.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace InTakeWise.Controllers
{
    [Authorize]
    public abstract class BaseController : Controller
    {
        protected readonly ApplicationDbContext _db;
        protected readonly UserManager<IdentityUser> _userManager;

        protected BaseController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        protected Task<IdentityUser?> GetCurrentUserAsync()
            => _userManager.GetUserAsync(User);

        protected async Task<bool> HasCompletedProfileAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;

            return await _db.UsersInformation
                .AsNoTracking()
                .AnyAsync(x => x.UserId == user.Id);
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // If not logged in, let normal pipeline handle it (Authorize will redirect)
            if (User?.Identity?.IsAuthenticated != true)
            {
                await next();
                return;
            }

            // Avoid redirect loops: allow ProfileWizard freely
            var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
            if (controller.Equals("ProfileWizard", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            // Also allow Identity endpoints if you ever hit them while signed in (logout, etc.)
            var area = context.RouteData.Values["area"]?.ToString() ?? "";
            if (area.Equals("Identity", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            // Gate: if profile not complete -> force wizard
            if (!await HasCompletedProfileAsync())
            {
                context.Result = RedirectToAction("Step1", "ProfileWizard");
                return;
            }

            await next();
        }
    }
}
