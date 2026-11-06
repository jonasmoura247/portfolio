using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Bancada.Web.Controllers;

public sealed record AuditoriaVm(IReadOnlyList<EventoDeAuditoria> Eventos, string? Grupo, int Pagina, int Paginas, VerificacaoDaCadeia? Verificacao);

public sealed class AuditoriaController(ConsultaDeAuditoria auditoria) : ControladorDaBancada
{
    private const int Tamanho = 20;

    [HttpGet("/auditoria"), Autorizar(Acao.VerAuditoria)]
    public IActionResult Index(string? grupo, int pagina = 1) => Tela(grupo, pagina, null);

    [HttpPost("/auditoria/verificar"), Autorizar(Acao.VerificarCadeia)]
    public IActionResult Verificar() => Tela(null, 1, auditoria.Verificar());

    [HttpPost("/auditoria/simular"), Autorizar(Acao.SimularAdulteracao)]
    public IActionResult Simular() => Tela(null, 1, auditoria.SimularAdulteracao());

    private ViewResult Tela(string? grupo, int pagina, VerificacaoDaCadeia? verificacao)
    {
        var todos = auditoria.Listar(grupo);
        var atual = Math.Max(pagina, 1);
        var itens = todos.Skip((atual - 1) * Tamanho).Take(Tamanho).ToList();
        return View("Index", new AuditoriaVm(itens, grupo, atual, Math.Max(1, (todos.Count + Tamanho - 1) / Tamanho), verificacao));
    }
}

public sealed class OperacaoController(ServicoDeOperacao operacao) : ControladorDaBancada
{
    [HttpGet("/operacao"), Autorizar(Acao.VerPainel)]
    public IActionResult Index() => View(operacao.Ver());
}

/// <summary>Arquivos que o visualizador mostra: vão embutidos no assembly, então não há caminho de disco para atravessar.</summary>
public static class CodigoPermitido
{
    private const string Prefixo = "codigo/";

    public static IReadOnlyList<string> Arquivos { get; } =
        Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Where(n => n.StartsWith(Prefixo, StringComparison.Ordinal)).Select(n => n[Prefixo.Length..]).Order().ToList();

    public static string? Ler(string? arquivo)
    {
        if (arquivo is null || !Arquivos.Contains(arquivo))
        {
            return null;
        }

        using var fluxo = Assembly.GetExecutingAssembly().GetManifestResourceStream(Prefixo + arquivo)!;
        using var leitor = new StreamReader(fluxo);
        return leitor.ReadToEnd();
    }
}

public sealed record PainelVm(string Aba, JsonElement? Instantaneo, IReadOnlyList<(string Tipo, string Nome)> Banco, IReadOnlyList<(string Nome, IReadOnlyList<string> Usa)> Camadas, RetratoDoExecutor Executor, bool ExecutorLigado, string? Arquivo, string? Codigo);

public sealed class PainelController(IEsquemaDoBanco esquema, ExecutorDeTestes executor, IWebHostEnvironment ambiente, IConfiguration configuracao, ServicoDeAuditoria auditoria) : ControladorDaBancada
{
    public static readonly string[] Abas = ["geral", "testes", "arquitetura", "banco", "seguranca", "licencas", "catalogo", "codigo"];

    private static readonly string[] Projetos = ["Bancada.Web", "Bancada.Aplicacao", "Bancada.Infraestrutura", "Bancada.Protocolos", "Bancada.Dominio"];

    public static IReadOnlyList<(string Nome, IReadOnlyList<string> Usa)> Camadas() =>
        Projetos.Select(p => (p, (IReadOnlyList<string>)Assembly.Load(p).GetReferencedAssemblies().Select(r => r.Name!).Where(Projetos.Contains).ToList())).ToList();

    [HttpGet("/painel"), Autorizar(Acao.VerPainel)]
    public IActionResult Index(string? aba, string? arquivo)
    {
        var atual = Abas.Contains(aba) ? aba! : Abas[0];
        var escolhido = atual == "codigo" ? arquivo ?? CodigoPermitido.Arquivos[0] : null;
        var codigo = CodigoPermitido.Ler(escolhido);
        if (atual == "codigo" && codigo is null)
        {
            return NotFound();
        }

        return View(new PainelVm(atual, LerInstantaneo(), esquema.Objetos(), Camadas(), executor.Retrato, executor.Habilitado, escolhido, codigo));
    }

    [HttpPost("/painel/testes/executar"), Autorizar(Acao.ExecutarTestes)]
    public IActionResult Executar([FromForm] string? suite)
    {
        var recusa = executor.Iniciar(suite, ProtecaoDeEntrada.Ip(HttpContext));
        auditoria.Registrar(Sessao, recusa is null ? "TestesIniciados" : "TestesRecusados", "Painel", suite == "js" ? "js" : "csharp", "{}", recusa is null ? "Ok" : "Recusado");
        Sessao.Aviso = (recusa ?? "painel.ok.iniciado", recusa is null);
        return LocalRedirect("/painel?aba=testes");
    }

    [HttpGet("/painel/testes/estado"), Autorizar(Acao.VerPainel)]
    public IActionResult Estado()
    {
        var r = executor.Retrato;
        return Json(new { estado = r.Estado.ToString(), suite = r.Suite, saida = r.Saida, cortada = r.Cortada });
    }

    private JsonElement? LerInstantaneo()
    {
        var arquivo = Path.Combine(ambiente.ContentRootPath, configuracao["Bancada:Instantaneo"] ?? "painel-instantaneo.json");
        if (!System.IO.File.Exists(arquivo))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(arquivo));
        return doc.RootElement.Clone();
    }
}
