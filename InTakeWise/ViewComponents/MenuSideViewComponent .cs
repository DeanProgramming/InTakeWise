using InTakeWise.Data;
using InTakeWise.Models;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InTakeWise.ViewComponents
{
    public class MenuSideViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public MenuSideViewComponent(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(string variant = "large")
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Content("");

            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            if (user == null) return Content("");

            var profile = await _db.UsersInformation
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            

            if (profile == null)
            {
                return View(new MenuSideViewModel
                {
                    UserName = profile.ProfileUserName,
                    CaloriesTarget = 0,
                    ProteinTarget = 0,
                    CarbsTarget = 0,
                    FatTarget = 0,
                    FiberTarget = 0,
                    IsGymDayToday = false
                });
            }

            var todayFlag = DayOfWeekToGymDay(DateTime.Today.DayOfWeek);
            var isGymDayToday = todayFlag != GymDays.None && (profile.ChosenGymDays & todayFlag) != 0;

            var vm = new MenuSideViewModel
            {
                UserName = profile.ProfileUserName,
                IsGymDayToday = isGymDayToday,

                CaloriesTarget = isGymDayToday ? profile.CaloriesTargetGymDay : profile.CaloriesTargetNonGymDay,
                ProteinTarget = isGymDayToday ? profile.ProteinTargetGymDay : profile.ProteinTargetNonGymDay,
                CarbsTarget = isGymDayToday ? profile.CarbsTargetGymDay : profile.CarbsTargetNonGymDay,
                FatTarget = isGymDayToday ? profile.FatTargetGymDay : profile.FatTargetNonGymDay,
                FiberTarget = isGymDayToday ? profile.FiberTargetGymDay : profile.FiberTargetNonGymDay,
                 
            };

            var viewName = variant?.ToLowerInvariant() switch
            {
                "compact" => "ProfileInfoShorten",
                _ => "ProfileInfo"
            };

            return View(viewName, vm); 
        }

        private static GymDays DayOfWeekToGymDay(DayOfWeek d) => d switch
        {
            DayOfWeek.Monday => GymDays.Monday,
            DayOfWeek.Tuesday => GymDays.Tuesday,
            DayOfWeek.Wednesday => GymDays.Wednesday,
            DayOfWeek.Thursday => GymDays.Thursday,
            DayOfWeek.Friday => GymDays.Friday,
            DayOfWeek.Saturday => GymDays.Saturday,
            DayOfWeek.Sunday => GymDays.Sunday,
            _ => GymDays.None
        };
    }
}
