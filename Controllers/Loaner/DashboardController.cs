using Microsoft.AspNetCore.Mvc;

namespace StrongHelpOfficial.Controllers.Loaner
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
