using System;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace Bancada.Web.Controllers;

/// <summary>Troca de papel, de operador e reinício. Sem senha: qualquer visitante pode assumir qualquer papel (é demonstração).</summary>
public sealed class SessaoController(RepositorioDeSessoes repositorio, ServicoDeAuditoria auditoria) : Controller
{
    [HttpPost("/sessao/papel"), Autorizar(Acao.TrocarPapel)]
    public IActionResult Papel([FromForm] string? papel)
    {
        if (!Enum.TryParse<Papel>(papel, out var novo) || !Enum.IsDefined(novo))
        {
            return BadRequest();
        }

        var sessao = HttpContext.Sessao();
        repositorio.Renovar(sessao, novo, Operadores.DoPapel(novo)[0].Id);
        auditoria.Registrar(sessao, "PapelTrocado", "Sessao", novo.ToString());
        return RedirectToAction("Index", "Inicio");
    }

    [HttpPost("/sessao/operador"), Autorizar(Acao.TrocarOperador)]
    public IActionResult Operador([FromForm] string? operador)
    {
        var sessao = HttpContext.Sessao();
        if (operador is null || !Operadores.Pertence(operador, sessao.Papel))
        {
            return BadRequest();
        }

        repositorio.Renovar(sessao, sessao.Papel, operador);
        auditoria.Registrar(sessao, "OperadorTrocado", "Sessao", operador);
        return RedirectToAction("Index", "Inicio");
    }

    [HttpPost("/sessao/idioma"), Autorizar(Acao.TrocarIdioma)]
    public IActionResult Idioma([FromForm] string? idioma)
    {
        if (Array.IndexOf(CatalogoDeTextos.Idiomas, idioma) < 0)
        {
            return BadRequest();
        }

        var sessao = HttpContext.Sessao();
        sessao.Idioma = idioma!;
        auditoria.Registrar(sessao, "IdiomaTrocado", "Sessao", idioma!);
        return RedirectToAction("Index", "Inicio");
    }

    [HttpPost("/sessao/cenario"), Autorizar(Acao.CarregarCenario)]
    public IActionResult Cenario([FromForm] string? cenario)
    {
        if (Array.IndexOf(Cenarios.Todos, cenario) < 0)
        {
            return BadRequest();
        }

        var nova = repositorio.Recriar(HttpContext.Sessao(), cenario!);
        nova.Idioma = HttpContext.Sessao().Idioma;
        HttpContext.Items[SessaoMiddleware.ChaveNoContexto] = nova;
        auditoria.Registrar(nova, "SessaoCriada");
        auditoria.Registrar(nova, "CenarioCarregado", "Sessao", cenario!);
        nova.Aviso = ("inicio.ok.cenario", true);
        return RedirectToAction("Index", "Inicio");
    }

    [HttpPost("/sessao/reiniciar"), Autorizar(Acao.ReiniciarSessao)]
    public IActionResult Reiniciar()
    {
        repositorio.Remover(HttpContext.Sessao().Id);
        return RedirectToAction("Index", "Inicio");
    }
}
