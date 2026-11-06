using System;
using System.Net;
using System.Threading.Tasks;
using Bancada.Web;
using Microsoft.AspNetCore.Http;

namespace Bancada.Testes.Web;

public class LimiteDeTaxaTestes
{
    private static readonly LimitesDeTaxa Pequenos = new() { Leitura = 3, Escrita = 2, NovaSessao = 2, Janela = TimeSpan.FromMinutes(5) };

    [Test]
    public async Task NovaSessao_AlemDoLimite_Devolve429ComRetryAfter()
    {
        using var app = new AppDeTeste(Pequenos);

        using var a = app.NovoCliente();
        using var b = app.NovoCliente();
        using var c = app.NovoCliente();
        var r1 = await a.GetAsync("/bancada");
        var r2 = await b.GetAsync("/bancada");
        var r3 = await c.GetAsync("/bancada");

        Assert.That(new[] { r1.StatusCode, r2.StatusCode }, Is.All.EqualTo(HttpStatusCode.OK));
        Assert.That(r3.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        Assert.That(r3.Headers.RetryAfter!.Delta, Is.EqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(await r3.Content.ReadAsStringAsync(), Does.Contain("Muitas requisições"));
    }

    [Test]
    public async Task Leitura_AlemDoLimiteComSessao_Devolve429()
    {
        using var app = new AppDeTeste(new LimitesDeTaxa { Leitura = 3, Escrita = 5, NovaSessao = 5 });
        using var cliente = app.NovoCliente();
        await cliente.GetAsync("/bancada"); // cria a sessão (balde "nova sessão")

        var statusEsperados = new[] { HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests };
        var obtidos = new HttpStatusCode[4];
        for (var i = 0; i < 4; i++)
        {
            obtidos[i] = (await cliente.GetAsync("/bancada")).StatusCode;
        }

        Assert.That(obtidos, Is.EqualTo(statusEsperados));
    }

    [Test]
    public async Task Escrita_AlemDoLimite_Devolve429()
    {
        using var app = new AppDeTeste(new LimitesDeTaxa { Leitura = 50, Escrita = 2, NovaSessao = 5 });
        using var cliente = app.NovoCliente();
        var html = await (await cliente.GetAsync("/bancada")).Content.ReadAsStringAsync();
        var token = AppDeTeste.TokenAntifalsificacao(html);

        var primeira = await AppDeTeste.Postar(cliente, "/sessao/papel", token, ("papel", "Invalido"));
        var segunda = await AppDeTeste.Postar(cliente, "/sessao/papel", token, ("papel", "Invalido"));
        var terceira = await AppDeTeste.Postar(cliente, "/sessao/papel", token, ("papel", "Invalido"));

        Assert.That(primeira.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(segunda.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(terceira.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
    }

    [Test]
    public async Task Saude_FicaForaDoLimite()
    {
        using var app = new AppDeTeste(Pequenos);
        using var cliente = app.NovoCliente();

        for (var i = 0; i < 20; i++)
        {
            Assert.That((await cliente.GetAsync("/saude")).StatusCode, Is.EqualTo(HttpStatusCode.OK), $"chamada {i}");
        }
    }

    [Test]
    public void Particao_SemIp_UsaChaveDesconhecido()
    {
        var contexto = new DefaultHttpContext();

        var particao = ConfiguracaoDeTaxa.Particao(contexto, new LimitesDeTaxa());

        Assert.That(particao.PartitionKey, Is.EqualTo("s:desconhecido"));
    }

    [Test]
    public void Particao_ComCookieEIp_SeparaLeituraDeEscritaPorIp()
    {
        var leitura = new DefaultHttpContext();
        leitura.Request.Method = "GET";
        leitura.Request.Headers.Cookie = "__Host-bancada.sid=abc";
        leitura.Connection.RemoteIpAddress = IPAddress.Parse("10.1.2.3");
        var head = new DefaultHttpContext();
        head.Request.Method = "HEAD";
        head.Request.Headers.Cookie = "__Host-bancada.sid=abc";
        head.Connection.RemoteIpAddress = IPAddress.Parse("10.1.2.3");
        var escrita = new DefaultHttpContext();
        escrita.Request.Method = "POST";
        escrita.Request.Headers.Cookie = "__Host-bancada.sid=abc";
        escrita.Connection.RemoteIpAddress = IPAddress.Parse("10.1.2.3");

        var limites = new LimitesDeTaxa();

        Assert.That(ConfiguracaoDeTaxa.Particao(leitura, limites).PartitionKey, Is.EqualTo("r:10.1.2.3"));
        Assert.That(ConfiguracaoDeTaxa.Particao(head, limites).PartitionKey, Is.EqualTo("r:10.1.2.3"));
        Assert.That(ConfiguracaoDeTaxa.Particao(escrita, limites).PartitionKey, Is.EqualTo("w:10.1.2.3"));
    }

    [Test]
    public void Particao_PaginaDeErroESaude_FicamForaDoLimite()
    {
        foreach (var caminho in new[] { "/erro/400", "/saude" })
        {
            var contexto = new DefaultHttpContext();
            contexto.Request.Path = caminho;

            Assert.That(ConfiguracaoDeTaxa.Particao(contexto, new LimitesDeTaxa()).PartitionKey, Is.EqualTo("sem-limite"));
        }
    }

    [Test]
    public void LimitesDeTaxa_PadroesSaoOsDoVault()
    {
        var l = new LimitesDeTaxa();

        Assert.That((l.Leitura, l.Escrita, l.NovaSessao, l.Janela), Is.EqualTo((120, 30, 10, TimeSpan.FromMinutes(1))));
    }
}
