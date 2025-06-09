using Microsoft.AspNetCore.Mvc;

namespace StrongHelpOfficial.Controllers.BenefitsAssistant
{
    public class BenefitsAssistantDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/BenefitsAssistant/BenefitsAssistantDashboard.cshtml");
        }
    }
}
