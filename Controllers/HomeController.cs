using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using Microsoft.AspNetCore.Mvc;
using StrongHelpOfficial.Models;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



namespace StrongHelpOfficial.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // Restrict access to users in "StrongHelp\\Admin" group
    //[Authorize(Roles = "StrongHelp\\Admin")]
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
