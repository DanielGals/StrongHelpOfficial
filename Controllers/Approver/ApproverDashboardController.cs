using Microsoft.AspNetCore.Mvc;

namespace StrongHelpOfficial.Controllers.Approver
{
    public class ApproverDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Approvers/ApproversDashboard.cshtml");
        }
    }
}
