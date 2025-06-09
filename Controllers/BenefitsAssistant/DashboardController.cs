using Microsoft.AspNetCore.Mvc;

namespace StrongHelpOfficial.Controllers.BenefitsAssistant
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
