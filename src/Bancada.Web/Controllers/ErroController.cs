using Microsoft.AspNetCore.Mvc;

namespace Bancada.Web.Controllers;

/// <summary>Página de erro genérica: nunca mostra stack trace, caminho nem nome de classe. Não depende de sessão.</summary>
[IgnoreAntiforgeryToken]
public sealed class ErroController : Controller
{
    private static readonly int[] Conhecidos = [400, 403, 404, 405, 413, 429, 500];

    [Route("/erro/{codigo:int}")]
    public IActionResult Index()
    {
        // Sem parâmetro de action: na reexecução de um POST, o binder leria de novo o formulário que já foi recusado.
        var codigo = int.Parse((string)RouteData.Values["codigo"]!, System.Globalization.CultureInfo.InvariantCulture);

        // Código que a página não conhece mantém a classe: erro do cliente (4xx) nunca vira erro do servidor (500).
        var status = System.Array.IndexOf(Conhecidos, codigo) >= 0 ? codigo : codigo is >= 400 and < 500 ? 400 : 500;
        Response.StatusCode = status;
        return View(status);
    }
}
