using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InTakeWise.ViewComponents
{
    public class FloatingBackButtonViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string text = "BACK", string fallbackUrl = null)
        {
            var vm = new FloatingBackButtonViewModel
            {
                Text = text,
                FallbackUrl = fallbackUrl ?? Url.Action("Index", "Home") ?? "/"
            };

            return View(vm);
        }
    }
}