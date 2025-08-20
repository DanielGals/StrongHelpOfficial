using Microsoft.AspNetCore.Mvc;
using StrongHelpOfficial.Models;

namespace StrongHelpOfficial.Controllers.Admin
{
    public class AdminRADController : Controller
    {
        public IActionResult Index()
        {
            var model = new AdminRADViewModel();
            return View("~/Views/Admin/AdminRAD.cshtml", model);
        }
    }
}
