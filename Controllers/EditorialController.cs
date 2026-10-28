using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyberSecurityApp.Controllers;

[Authorize(Roles = "Administrator,Redaktor")]
public class EditorialController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
