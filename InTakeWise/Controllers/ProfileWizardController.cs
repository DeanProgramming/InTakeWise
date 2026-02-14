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

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var existing = await _db.UsersInformation.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (existing == null) return RedirectToAction(nameof(Step1));

            if (step1 == null || step2 == null)
            {  
                step1 = MapToStep1(existing);
                step2 = MapToStep2(existing);

                HttpContext.Session.SetObject(Step1Key, step1);
                HttpContext.Session.SetObject(Step2Key, step2);
            }

            var predictedByGoal = Enum.GetValues<FitnessGoal>()
                .ToDictionary(
                    goal => goal,
                    goal => PredictWeight(step1, step2, months, goal)
                );

            var startBf = EstimateBodyFatPercentFromBmi(step1);

            var fatByGoal = Enum.GetValues<FitnessGoal>()
                .ToDictionary(g => g,
                        g => PredictBodyFatPercent(step1, step2, months, step1.WeightInKg, g, startBf));

            return View(new ProfileStep3ViewModel
            {
                CurrentWeightKg = step1.WeightInKg,
                CurrentFatPercentage = startBf,
                PredictedWeightKgByGoal = predictedByGoal,
                FatPercentageByGoal = fatByGoal,
                currentFitnessGoal = existing.ChosenFitnessGoal,
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

        private static double PredictWeight(ProfileStep1ViewModel p, ProfileStep2ViewModel g, int months, FitnessGoal fitnessGoal)
        { 
            var goalMonthlyDelta = fitnessGoal switch
            {
                FitnessGoal.HeavyCut => -1.5,
                FitnessGoal.LightCut => -0.75,
                FitnessGoal.Maintain => 0.0,
                FitnessGoal.LightBulk => 0.5,
                FitnessGoal.HeavyBulk => 1.0,
                _ => 0.0
            };
             
            var fitnessMultiplier = g.EveryDayFitnessLevel switch
            {
                FitnessLevel.Low => 0.9,
                FitnessLevel.Medium => 1.0,
                FitnessLevel.High => 1.1,
                _ => 1.0
            };
             
            var gymDays = CountBits((int)g.ChosenGymDays);
            var gymBonus = Math.Clamp(gymDays, 0, 7) * 0.1;

            var direction = Math.Sign(goalMonthlyDelta); // -1 cut, +1 bulk, 0 maintain
            var monthlyDelta = (goalMonthlyDelta * fitnessMultiplier) + (gymBonus * direction);

            var predicted = p.WeightInKg + (monthlyDelta * months);
            return Math.Max(predicted, 0);
        }

        private static double PredictBodyFatPercent(
            ProfileStep1ViewModel p,
            ProfileStep2ViewModel g,
            int months,
            double currentWeight,
            FitnessGoal goal,
            double startBfPercent)
        {
            months = Math.Max(months, 0);

            var startWeight = (double)currentWeight;
            var startFatMass = startWeight * startBfPercent / 100.0;

            var predictedWeight = PredictWeight(p, g, months, goal); 
            if (predictedWeight <= 0.0) return 0.0;

            // Maintain can "recomp" if training (fat down, lean up)
            if (goal == FitnessGoal.Maintain)
            {
                var gymDays = CountBits((int)g.ChosenGymDays);
                var recomp = gymDays >= 3 ? 0.20 : 0.0; // kg/month (dummy)

                var fatMass = Math.Max(startFatMass - (recomp * months), 0);
                var leanMass = (startWeight - startFatMass) + (recomp * months);
                var w = fatMass + leanMass;

                return Math.Clamp(fatMass / w * 100.0, 2.0, 60.0);
            }

            var (fatFrac, leanFrac) = GetFatLeanSplit(p, g, goal);

            var weightChange = predictedWeight - startWeight;
            var fatMassNew = startFatMass + (weightChange * fatFrac);

            // Essential fat floor (dummy-safe clamp)
            var minBf = p.Gender == Genders.Male ? 4.0
                      : p.Gender == Genders.Female ? 12.0
                      : 8.0;

            var minFatMass = predictedWeight * minBf / 100.0;
            fatMassNew = Math.Max(fatMassNew, minFatMass);
            fatMassNew = Math.Min(fatMassNew, predictedWeight);

            var bf = fatMassNew / predictedWeight * 100.0;
            return Math.Clamp(bf, minBf, 60.0);
        }

        private static (double fatFrac, double leanFrac) GetFatLeanSplit(
            ProfileStep1ViewModel p,
            ProfileStep2ViewModel g,
            FitnessGoal goal)
        {
            // fraction of the weight change that is FAT vs LEAN (dummy assumptions)
            var (fat, lean) = goal switch
            {
                FitnessGoal.HeavyCut => (0.85, 0.15),
                FitnessGoal.LightCut => (0.90, 0.10),
                FitnessGoal.LightBulk => (0.35, 0.65),
                FitnessGoal.HeavyBulk => (0.45, 0.55),
                _ => (0.50, 0.50)
            };

            var gymDays = CountBits((int)g.ChosenGymDays);
            var adj = Math.Clamp(gymDays * 0.01, 0.0, 0.05); // up to 5%

            if (goal is FitnessGoal.HeavyCut or FitnessGoal.LightCut)
            {
                fat = Math.Clamp(fat + adj, 0.75, 0.97); // more gym = better lean retention
                lean = 1.0 - fat;
            }
            else if (goal is FitnessGoal.LightBulk or FitnessGoal.HeavyBulk)
            {
                lean = Math.Clamp(lean + adj, 0.40, 0.85); // more gym = more lean gain
                fat = 1.0 - lean;
            }

            return (fat, lean);
        }


        private static double EstimateBodyFatPercentFromBmi(ProfileStep1ViewModel p)
        {
            var h = p.HeightInCM / 100.0;
            var bmi = p.WeightInKg / (h * h);

            var sex = p.Gender switch
            {
                Genders.Male => 1.0,
                Genders.Female => 0.0,
                _ => 0.5
            };

            var bf = (1.20 * bmi) + (0.23 * p.Age) - (10.8 * sex) - 5.4;
            return Math.Clamp(bf, 2.0, 60.0);
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
