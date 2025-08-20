using Microsoft.AspNetCore.Mvc;
using StrongHelpOfficial.Models;

namespace StrongHelpOfficial.Controllers.Admin
{
    public class AdminLogsController : Controller
    {
        public IActionResult Index()
        {
            var model = new AdminLogsViewModel();
            return View("~/Views/Admin/AdminLogs.cshtml", model);
        }
    }
}
