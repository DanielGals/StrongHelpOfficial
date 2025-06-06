using Microsoft.AspNetCore.Mvc;

namespace StrongHelpOfficial.Controllers;

public class AuthController : Controller
{
    public IActionResult Login()
    {
        return View();
    }
}
