using Microsoft.AspNetCore.Mvc;
namespace CyberSecurityApp.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }

    [HttpGet]
    public IActionResult Error()
    {
        return View();
    }
}
