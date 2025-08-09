using Microsoft.AspNetCore.Mvc;

namespace StrongHelpOfficial.Controllers.Approver
{
    public class ApproverLogsController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Approvers/ApproversLogs.cshtml");
        }
    }
}
