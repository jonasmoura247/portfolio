using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Bancada.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Bancada.Testes.Web;

/// <summary>Controller que só existe nos testes: uma ação protegida, uma que explode e uma lenta (para provar a serialização por sessão).</summary>
[Route("/teste")]
public sealed class ControladorDeTeste : Controller
{
    private static int _emExecucao;

    public static int MaximoSimultaneo { get; private set; }

    public static void Zerar()
    {
        _emExecucao = 0;
        MaximoSimultaneo = 0;
    }

    [HttpGet("protegido"), Autorizar(Acao.DescartarAmostra)]
    public IActionResult Protegido() => Content("liberado");

    [HttpGet("explode")]
    public IActionResult Explode() => throw new InvalidOperationException("segredo interno do servidor em " + Request.Path);

    [HttpGet("lento")]
    public async Task<IActionResult> Lento()
    {
        var agora = Interlocked.Increment(ref _emExecucao);
        MaximoSimultaneo = Math.Max(MaximoSimultaneo, agora);
        await Task.Delay(40);
        Interlocked.Decrement(ref _emExecucao);
        return Content("ok");
    }
}

internal sealed partial class AppDeTeste : IDisposable
{
    private readonly WebApplicationFactory<Program> _fabrica;

    public AppDeTeste(LimitesDeTaxa? limites = null, bool cookiesSeguros = true, Action<IServiceCollection>? extra = null, (string Chave, string Valor)? configuracao = null, string? papelInicial = "Recepcao")
    {
        ControladorDeTeste.Zerar();
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(construtor => PapelInicial(construtor.UseSetting("Bancada:CookiesSeguros", cookiesSeguros ? "true" : "false"), papelInicial).UseSetting(configuracao?.Chave ?? "Bancada:Instantaneo", configuracao?.Valor ?? "inexistente.json").ConfigureServices(servicos =>
        {
            extra?.Invoke(servicos);
            servicos.AddControllersWithViews().AddApplicationPart(typeof(ControladorDeTeste).Assembly);
            if (limites is not null)
            {
                servicos.AddSingleton(limites);
            }
        }));
    }

    /// <summary>Sem valor, não define nada: vale o padrão de produção (Admin).</summary>
    private static Microsoft.AspNetCore.Hosting.IWebHostBuilder PapelInicial(Microsoft.AspNetCore.Hosting.IWebHostBuilder construtor, string? papel) =>
        papel is null ? construtor : construtor.UseSetting("Bancada:PapelInicial", papel);

    public Sessao UltimaSessao() => Sessoes.UltimaCriada!;

    public RepositorioDeSessoes Sessoes => _fabrica.Services.GetRequiredService<RepositorioDeSessoes>();

    public HttpClient NovoCliente() => _fabrica.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    public HttpClient NovoClienteHttp() => _fabrica.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("http://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    public static string? IdDaSessao(HttpResponseMessage resposta)
    {
        if (!resposta.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        foreach (var c in cookies)
        {
            var m = CookieDaSessao().Match(c);
            if (m.Success)
            {
                return m.Groups[1].Value;
            }
        }

        return null;
    }

    public static string TokenAntifalsificacao(string html) => TokenCsrf().Match(html).Groups[1].Value;

    public static async Task<HttpResponseMessage> Postar(HttpClient cliente, string url, string token, params (string Chave, string Valor)[] campos)
    {
        var dados = new List<KeyValuePair<string, string>> { new("__RequestVerificationToken", token) };
        foreach (var (chave, valor) in campos)
        {
            dados.Add(new(chave, valor));
        }

        return await cliente.PostAsync(url, new FormUrlEncodedContent(dados));
    }

    public void Dispose() => _fabrica.Dispose();

    [GeneratedRegex(@"__Host-bancada\.sid=([^;]+)")]
    private static partial Regex CookieDaSessao();

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenCsrf();
}
