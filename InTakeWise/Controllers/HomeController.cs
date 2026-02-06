using InTakeWise.Data;
using InTakeWise.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace InTakeWise.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager)
            : base(db, userManager)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();

            return View(new HomeViewModel
            {
                UserName = user?.UserName
            });
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
