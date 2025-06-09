using Microsoft.AspNetCore.Mvc;

namespace StrongHelpOfficial.Controllers.Loaner
{
    public class LoanerDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Loaner/LoanerDashboard.cshtml");
        }
    }
}
