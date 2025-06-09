using Microsoft.AspNetCore.Mvc;

namespace StrongHelpOfficial.Controllers.Approver
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
