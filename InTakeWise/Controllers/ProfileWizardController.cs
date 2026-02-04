using InTakeWise.Models;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace InTakeWise.Controllers
{
    [Authorize]
    public class ProfileWizardController : Controller
    {
        private const string Step1Key = "ProfileStep1";
        private const string Step2Key = "ProfileStep2"; 

        [HttpGet]
        public IActionResult Step1()
        {
            var vm = HttpContext.Session.GetObject<ProfileStep1ViewModel>(Step1Key)
                     ?? new ProfileStep1ViewModel();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step1(ProfileStep1ViewModel vm)
        {
            HttpContext.Session.SetObject(Step1Key, vm);
            return RedirectToAction(nameof(Step2));
        } 

        [HttpGet]
        public IActionResult Step2()
        {
            var vm = HttpContext.Session.GetObject<ProfileStep2ViewModel>(Step2Key)
                     ?? new ProfileStep2ViewModel();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step2(ProfileStep2ViewModel vm)
        { 
            var days = (vm.WorkoutDaysCsv ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
             
            HttpContext.Session.SetObject("ProfileStep2", vm);

            return RedirectToAction(nameof(Step3));
        }


        [HttpGet]
        public IActionResult Step3(int months = 3)
        {
            var step1 = HttpContext.Session.GetObject<ProfileStep1ViewModel>(Step1Key);
            var step2 = HttpContext.Session.GetObject<ProfileStep2ViewModel>(Step2Key);

            if (step1 == null || step2 == null)
                return RedirectToAction(nameof(Step1));

            var predicted = PredictWeight(step1, step2, months);

            var vm = new ProfileStep3ViewModel
            {
                CurrentWeightKg = step1.WeightKg,
                PredictedWeightKg = predicted,
                Months = months
            };

            return View(vm);
        }
         
        private double PredictWeight(
            ProfileStep1ViewModel p,
            ProfileStep2ViewModel g,
            int months)
        {
            var baseLoss = g.ActivityLevel switch
            {
                "Low" => 0.5,
                "Moderate" => 1.0,
                "High" => 1.5,
                _ => 1.0
            };

            var gymBonus = Math.Clamp(g.GymDaysPerWeek, 0, 7) * 0.1;
            var monthlyLoss = baseLoss + gymBonus;

            return Math.Max(p.WeightKg - (monthlyLoss * months), 0);
        }
    }

}
