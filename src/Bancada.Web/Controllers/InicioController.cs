using Microsoft.AspNetCore.Mvc;

namespace Bancada.Web.Controllers;

public sealed class InicioController : Controller
{
    [HttpGet("/bancada")]
    public IActionResult Index() => View();
}
