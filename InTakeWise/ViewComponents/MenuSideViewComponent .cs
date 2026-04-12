using InTakeWise.Data;
using InTakeWise.Models;
using InTakeWise.Services;
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
        private readonly IAppClock _clock;

        public MenuSideViewComponent(
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            IAppClock clock)
        {
            _db = db;
            _userManager = userManager;
            _clock = clock;
        }

        public async Task<IViewComponentResult> InvokeAsync(string variant = "large")
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Content("");

            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            if (user == null)
                return Content("");

            var profile = await _db.UsersInformation
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            var (startUtc, endUtc) = _clock.GetTodayLondonRangeUtc();

            var totals = await _db.MealLogs
                .AsNoTracking()
                .Where(x => x.UserId == user.Id && x.Timestamp >= startUtc && x.Timestamp < endUtc)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Calories = g.Sum(x => x.Calories ?? 0),
                    Protein = g.Sum(x => x.Protein ?? 0),
                    Carbs = g.Sum(x => x.Carbs ?? 0),
                    Fat = g.Sum(x => x.Fat ?? 0),
                    Fiber = g.Sum(x => x.Fiber ?? 0),
                })
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                var vmNoProfile = new MenuSideViewModel
                {
                    UserName = user.UserName,
                    CaloriesTarget = 0,
                    ProteinTarget = 0,
                    CarbsTarget = 0,
                    FatTarget = 0,
                    FiberTarget = 0,
                    IsGymDayToday = false,
                    CaloriesConsumed = totals?.Calories ?? 0,
                    ProteinConsumed = totals?.Protein ?? 0,
                    CarbsConsumed = totals?.Carbs ?? 0,
                    FatConsumed = totals?.Fat ?? 0,
                    FiberConsumed = totals?.Fiber ?? 0,
                };

                var viewNameNoProfile = variant?.ToLowerInvariant() switch
                {
                    "compact" => "ProfileInfoShorten",
                    _ => "ProfileInfo"
                };

                return View(viewNameNoProfile, vmNoProfile);
            }

            var todayFlag = DayOfWeekToGymDay(_clock.LondonDayOfWeek);
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

                CaloriesConsumed = totals?.Calories ?? 0,
                ProteinConsumed = totals?.Protein ?? 0,
                CarbsConsumed = totals?.Carbs ?? 0,
                FatConsumed = totals?.Fat ?? 0,
                FiberConsumed = totals?.Fiber ?? 0,
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