using Microsoft.AspNetCore.Mvc;

namespace StrongHelpOfficial.Controllers.Loaner
{
    public class ApplyForLoanController : Controller
    {
        public IActionResult Index()
        {
            // Get user data from session
            ViewData["UserID"] = HttpContext.Session.GetInt32("UserID");
            ViewData["RoleName"] = HttpContext.Session.GetString("RoleName");
            ViewData["Email"] = HttpContext.Session.GetString("Email");

            return View("~/Views/Loaner/ApplyForLoan.cshtml");
        }
    }
}
