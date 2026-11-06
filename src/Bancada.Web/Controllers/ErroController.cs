using Microsoft.AspNetCore.Mvc;

namespace Bancada.Web.Controllers;

/// <summary>Página de erro genérica: nunca mostra stack trace, caminho nem nome de classe. Não depende de sessão.</summary>
[IgnoreAntiforgeryToken]
public sealed class ErroController : Controller
{
    private static readonly int[] Conhecidos = [400, 403, 404, 429, 500];

    [Route("/erro/{codigo:int}")]
    public IActionResult Index()
    {
        // Sem parâmetro de action: na reexecução de um POST, o binder leria de novo o formulário que já foi recusado.
        var codigo = int.Parse((string)RouteData.Values["codigo"]!, System.Globalization.CultureInfo.InvariantCulture);
        var status = System.Array.IndexOf(Conhecidos, codigo) >= 0 ? codigo : 500;
        Response.StatusCode = status;
        return View(status);
    }
}
