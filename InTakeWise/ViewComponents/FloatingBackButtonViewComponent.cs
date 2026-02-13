using Microsoft.AspNetCore.Mvc;

namespace InTakeWise.ViewComponents
{
    public class FloatingBackButtonViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(
            string controller = "Home",
            string action = "Index",
            string text = "Back")
        {
            var vm = new FloatingBackButtonVm
            {
                Controller = controller,
                Action = action,
                Text = text
            };

            return View(vm);
        }
    }

    public class FloatingBackButtonVm
    {
        public string Controller { get; set; } = "Home";
        public string Action { get; set; } = "Index";
        public string Text { get; set; } = "Back";
    }
}
