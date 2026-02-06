using InTakeWise.Data;
using InTakeWise.Models;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InTakeWise.Controllers
{
    [Authorize]
    public class ProfileWizardController : Controller
    {
        private const string Step1Key = "ProfileStep1";
        private const string Step2Key = "ProfileStep2";

        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileWizardController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Step1()
        { 
            var fromSession = HttpContext.Session.GetObject<ProfileStep1ViewModel>(Step1Key);
            if (fromSession != null)
                return View(fromSession);
             
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var existing = await _db.UsersInformation.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            var vm = existing != null ? MapToStep1(existing) : new ProfileStep1ViewModel();
            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step1(ProfileStep1ViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            HttpContext.Session.SetObject(Step1Key, vm);
            return RedirectToAction(nameof(Step2));
        }

        [HttpGet]
        public async Task<IActionResult> Step2()
        {
            var fromSession = HttpContext.Session.GetObject<ProfileStep2ViewModel>(Step2Key);
            if (fromSession != null)
                return View(fromSession);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var existing = await _db.UsersInformation.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            var vm = existing != null ? MapToStep2(existing) : new ProfileStep2ViewModel();
            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step2(ProfileStep2ViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            if (vm.ChosenGymDays == GymDays.None)
            {
                ModelState.AddModelError(nameof(vm.ChosenGymDays), "Please pick at least one gym day.");
                return View(vm);
            }

            HttpContext.Session.SetObject(Step2Key, vm);
            return RedirectToAction(nameof(Step3));
        }

        [HttpGet]
        public async Task<IActionResult> Step3(int months = 3)
        {
            var step1 = HttpContext.Session.GetObject<ProfileStep1ViewModel>(Step1Key);
            var step2 = HttpContext.Session.GetObject<ProfileStep2ViewModel>(Step2Key);

            if (step1 == null || step2 == null)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Challenge();

                var existing = await _db.UsersInformation.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UserId == user.Id);

                if (existing == null) return RedirectToAction(nameof(Step1));

                step1 = MapToStep1(existing);
                step2 = MapToStep2(existing);

                HttpContext.Session.SetObject(Step1Key, step1);
                HttpContext.Session.SetObject(Step2Key, step2);
            }

            var predicted = PredictWeight(step1, step2, months);

            return View(new ProfileStep3ViewModel
            {
                CurrentWeightKg = step1.WeightInKg,
                PredictedWeightKg = predicted,
                Months = months
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete()
        {
            var step1 = HttpContext.Session.GetObject<ProfileStep1ViewModel>(Step1Key);
            var step2 = HttpContext.Session.GetObject<ProfileStep2ViewModel>(Step2Key);

            if (step1 == null || step2 == null)
                return RedirectToAction(nameof(Step1));

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var existing = await _db.UsersInformation
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            const int gymCalories = 2200, nonGymCalories = 2000;

            if (existing == null)
            {
                existing = new UsersInformation { UserId = user.Id };
                _db.UsersInformation.Add(existing);
            }

            existing.ProfileUserName = step1.ProfileUserName;
            existing.Age = step1.Age;
            existing.Gender = step1.Gender;
            existing.HeightInCM = step1.HeightInCM;
            existing.WeightInKg = step1.WeightInKg;
            existing.EveryDayFitnessLevel = step2.EveryDayFitnessLevel;
            existing.ChosenGymDays = step2.ChosenGymDays;

            // Dummy macros
            existing.CaloriesTargetGymDay = gymCalories;
            existing.ProteinTargetGymDay = 160;
            existing.CarbsTargetGymDay = 220;
            existing.FatTargetGymDay = 70;
            existing.FiberTargetGymDay = 30;

            existing.CaloriesTargetNonGymDay = nonGymCalories;
            existing.ProteinTargetNonGymDay = 150;
            existing.CarbsTargetNonGymDay = 200;
            existing.FatTargetNonGymDay = 65;
            existing.FiberTargetNonGymDay = 30;

            await _db.SaveChangesAsync();

            HttpContext.Session.Remove(Step1Key);
            HttpContext.Session.Remove(Step2Key);

            return RedirectToAction("Index", "Home");
        }

        private static double PredictWeight(ProfileStep1ViewModel p, ProfileStep2ViewModel g, int months)
        {
            var baseLoss = g.EveryDayFitnessLevel switch
            {
                FitnessLevel.Low => 0.5,
                FitnessLevel.Medium => 1.0,
                FitnessLevel.High => 1.5,
                _ => 1.0
            };

            var gymDays = CountBits((int)g.ChosenGymDays);
            var gymBonus = Math.Clamp(gymDays, 0, 7) * 0.1;

            var monthlyLoss = baseLoss + gymBonus;
            return Math.Max(p.WeightInKg - (monthlyLoss * months), 0);
        }

        private static int CountBits(int n)
        {
            int c = 0;
            while (n != 0) { n &= (n - 1); c++; }
            return c;
        }

        private static ProfileStep1ViewModel MapToStep1(UsersInformation u) => new()
        {
            ProfileUserName = u.ProfileUserName,
            Age = u.Age,
            Gender = u.Gender,
            HeightInCM = u.HeightInCM,
            WeightInKg = u.WeightInKg
        };

        private static ProfileStep2ViewModel MapToStep2(UsersInformation u) => new()
        {
            EveryDayFitnessLevel = u.EveryDayFitnessLevel,
            ChosenGymDays = u.ChosenGymDays
        };

    }
}
