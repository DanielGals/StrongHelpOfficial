using Microsoft.AspNetCore.Mvc;

namespace StrongHelpOfficial.Controllers.Approver
{
    public class ApproverApplicationsController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Approvers/ApproversApplications.cshtml");
        }
    }
}
