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
            var step1 = HttpContext.Session.GetObject<ProfileStep1ViewModel>(Step1Key);
            var fromSession = HttpContext.Session.GetObject<ProfileStep2ViewModel>(Step2Key);

            if (fromSession != null)
                return View(fromSession);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var existing = await _db.UsersInformation.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            // New user cannot skip Step1
            if (step1 == null && existing == null)
                return RedirectToAction(nameof(Step1));

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

            // Existing user editing profile:
            // if session is missing, seed it from DB
            if (existing != null)
            {
                if (step1 == null)
                {
                    step1 = MapToStep1(existing);
                    HttpContext.Session.SetObject(Step1Key, step1);
                }

                if (step2 == null)
                {
                    step2 = MapToStep2(existing);
                    HttpContext.Session.SetObject(Step2Key, step2);
                }
            }

            // New user cannot skip Step1
            if (step1 == null)
                return RedirectToAction(nameof(Step1));

            // New user cannot skip Step2
            if (step2 == null)
                return RedirectToAction(nameof(Step2));

            months = Math.Max(months, 1);

            var predictedByGoal = Enum.GetValues<FitnessGoal>()
                .ToDictionary(
                    goal => goal,
                    goal => PredictWeight(step1, step2, months, goal)
                );

            var startBf = EstimateBodyFatPercentFromBmi(step1);

            var fatByGoal = Enum.GetValues<FitnessGoal>()
                .ToDictionary(
                    goal => goal,
                    goal => PredictBodyFatPercent(step1, step2, months, step1.WeightInKg, goal, startBf)
                );

            return View(new ProfileStep3ViewModel
            {
                CurrentWeightKg = step1.WeightInKg,
                CurrentFatPercentage = startBf,
                PredictedWeightKgByGoal = predictedByGoal,
                FatPercentageByGoal = fatByGoal,
                CurrentFitnessGoal = existing?.ChosenFitnessGoal ?? FitnessGoal.Maintain,
                Months = months
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(ProfileStep3ViewModel step3)
        {
            var step1 = HttpContext.Session.GetObject<ProfileStep1ViewModel>(Step1Key);
            var step2 = HttpContext.Session.GetObject<ProfileStep2ViewModel>(Step2Key);

            if (step1 == null)
                return RedirectToAction(nameof(Step1));

            if (step2 == null)
                return RedirectToAction(nameof(Step2));

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var existing = await _db.UsersInformation
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

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
            existing.ChosenFitnessGoal = step3.CurrentFitnessGoal;

            var (gymCalories, nonGymCalories) = ComputeCaloriesFromRoughEstimate(step1, step2, step3.CurrentFitnessGoal);

            SetMacroTargets(ref existing, step1.WeightInKg, step3.CurrentFitnessGoal, gymCalories, nonGymCalories);

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

        static double ProteinPerKg(FitnessGoal g) => g switch
        {
            FitnessGoal.HeavyCut => 2.2,
            FitnessGoal.LightCut => 2.0,
            FitnessGoal.Maintain => 1.8,
            FitnessGoal.LightBulk => 1.7,
            FitnessGoal.HeavyBulk => 1.6,
            _ => 1.8
        };

        static double FatPerKg(FitnessGoal g) => g switch
        {
            FitnessGoal.HeavyCut or FitnessGoal.LightCut => 0.7,
            FitnessGoal.Maintain => 0.8,
            FitnessGoal.LightBulk or FitnessGoal.HeavyBulk => 0.9,
            _ => 0.8
        };

        static int ClampInt(int v, int lo, int hi) => Math.Max(lo, Math.Min(hi, v));

        static int FiberFromCalories(int cals)
            => ClampInt((int)Math.Round(14.0 * (cals / 1000.0)), 25, 45);

        static int CarbsFrom(int calories, int p, int f)
        {
            var remaining = calories - (p * 4) - (f * 9);
            return Math.Max(0, (int)Math.Round(remaining / 4.0));
        }

        private static void SetMacroTargets(
            ref UsersInformation existing,
            double weightKg,
            FitnessGoal goal,
            int gymCalories,
            int nonGymCalories,
            bool carbBumpOnGymDays = true)
        {


            var protein = (int)Math.Round(weightKg * ProteinPerKg(goal));
            protein = ClampInt(protein, 90, 220);

            // Fat scaled by bodyweight with a minimum floor
            var fatBase = (int)Math.Round(weightKg * FatPerKg(goal));
            var fatMin = Math.Max((int)Math.Round(weightKg * 0.6), 40);
            var fatGym = Math.Max(fatBase, fatMin);
            var fatNonGym = Math.Max(fatBase, fatMin);

            var fiberGym = FiberFromCalories(gymCalories);
            var fiberNonGym = FiberFromCalories(nonGymCalories);



            var carbsGym = CarbsFrom(gymCalories, protein, fatGym);
            var carbsNonGym = CarbsFrom(nonGymCalories, protein, fatNonGym);

            if (carbBumpOnGymDays && carbsGym > 0)
            {
                var bump = (int)Math.Round(carbsGym * 0.15);
                var bumpCals = bump * 4;

                var fatReduction = (int)Math.Round(bumpCals / 9.0);
                var newFatGym = Math.Max(fatGym - fatReduction, fatMin);

                fatGym = newFatGym;
                carbsGym = CarbsFrom(gymCalories, protein, fatGym);
            }

            existing.CaloriesTargetGymDay = gymCalories;
            existing.ProteinTargetGymDay = protein;
            existing.FatTargetGymDay = fatGym;
            existing.CarbsTargetGymDay = carbsGym;
            existing.FiberTargetGymDay = fiberGym;

            existing.CaloriesTargetNonGymDay = nonGymCalories;
            existing.ProteinTargetNonGymDay = protein;
            existing.FatTargetNonGymDay = fatNonGym;
            existing.CarbsTargetNonGymDay = carbsNonGym;
            existing.FiberTargetNonGymDay = fiberNonGym;
        }

        private static (int gymCalories, int nonGymCalories) ComputeCaloriesFromRoughEstimate(
            ProfileStep1ViewModel p,
            ProfileStep2ViewModel g,
            FitnessGoal goal)
        { 
            var goalMonthlyDelta = goal switch
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

            var gymDays = Math.Clamp(CountBits((int)g.ChosenGymDays), 0, 7);
            var gymBonus = gymDays * 0.1;

            var direction = Math.Sign(goalMonthlyDelta); 
            var monthlyDeltaKg = (goalMonthlyDelta * fitnessMultiplier) + (gymBonus * direction);
             
            var kcalPerDayFromGoal = (monthlyDeltaKg * 7700.0) / 30.0;  
             
            var tdee = EstimateTdee(p, g);
             
            var avgTarget = tdee + kcalPerDayFromGoal;
             
            var minCalories = p.Gender == Genders.Female ? 1200 : 1500;
            avgTarget = Math.Max(avgTarget, minCalories);
             
            var nonGymDays = 7 - gymDays;

            if (gymDays == 0 || nonGymDays == 0)
            {
                var c = (int)Math.Round(avgTarget);
                c = Math.Max(c, minCalories);
                return (c, c);
            }
             
            var shift = (int)Math.Round(Math.Clamp(tdee * 0.06, 120, 300));  

            var weeklyTarget = avgTarget * 7.0;

            var gymCalories = (int)Math.Round(avgTarget + shift);
            var nonGymCalories = (int)Math.Round((weeklyTarget - (gymCalories * gymDays)) / nonGymDays);
             
            if (nonGymCalories < minCalories)
            {
                nonGymCalories = minCalories;
                gymCalories = (int)Math.Round((weeklyTarget - (nonGymCalories * nonGymDays)) / gymDays);
            }

            gymCalories = Math.Max(gymCalories, minCalories);

            return (gymCalories, nonGymCalories);
        }

        private static double EstimateTdee(ProfileStep1ViewModel p, ProfileStep2ViewModel g)
        {
            // Mifflin–St Jeor BMR
            var w = (double)p.WeightInKg;
            var h = (double)p.HeightInCM;
            var a = (double)p.Age;

            var bmr = p.Gender switch
            {
                Genders.Male => 10 * w + 6.25 * h - 5 * a + 5,
                Genders.Female => 10 * w + 6.25 * h - 5 * a - 161,
                _ => 10 * w + 6.25 * h - 5 * a - 78
            };
             
            var af = g.EveryDayFitnessLevel switch
            {
                FitnessLevel.Low => 1.35,
                FitnessLevel.Medium => 1.50,
                FitnessLevel.High => 1.65,
                _ => 1.50
            };
             
            var gymDays = Math.Clamp(CountBits((int)g.ChosenGymDays), 0, 7);
            af = Math.Clamp(af + gymDays * 0.02, 1.25, 1.90);

            return bmr * af;
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
