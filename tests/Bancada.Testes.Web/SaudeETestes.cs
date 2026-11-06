using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bancada.Web;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bancada.Testes.Web;

public class SaudeETestes
{
    private WebApplicationFactory<Program> _fabrica = null!;

    [SetUp]
    public void Preparar() => _fabrica = new WebApplicationFactory<Program>();

    [TearDown]
    public void Limpar() => _fabrica.Dispose();

    [Test]
    public async Task Saude_Get_Devolve200ComOk()
    {
        using var cliente = _fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/saude");

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await resposta.Content.ReadAsStringAsync(), Is.EqualTo("ok"));
    }

    [Test]
    public async Task Resposta_QualquerRota_TrazTodosOsCabecalhosDeSeguranca()
    {
        using var cliente = _fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/saude");

        foreach (var (nome, valor) in CabecalhosDeSeguranca.Padrao)
        {
            Assert.That(resposta.Headers.Contains(nome) || resposta.Content.Headers.Contains(nome), Is.True, nome);
            Assert.That(ValorDe(resposta, nome), Is.EqualTo(valor), nome);
        }
    }

    [Test]
    public async Task Csp_NaoPermiteScriptNemEstiloInline()
    {
        using var cliente = _fabrica.CreateClient();

        var csp = ValorDe(await cliente.GetAsync("/saude"), "Content-Security-Policy");

        Assert.That(csp, Does.Not.Contain("unsafe-inline"));
        Assert.That(csp, Does.Not.Contain("unsafe-eval"));
        Assert.That(csp, Does.Contain("default-src 'none'"));
        Assert.That(csp, Does.Contain("frame-ancestors 'none'"));
    }

    [Test]
    public async Task Resposta_NaoRevelaServidorNemTecnologia()
    {
        using var cliente = _fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/saude");

        Assert.That(resposta.Headers.Contains("Server"), Is.False);
        Assert.That(resposta.Headers.Contains("X-Powered-By"), Is.False);
    }

    private static string ValorDe(HttpResponseMessage resposta, string nome) =>
        string.Join(",", resposta.Headers.TryGetValues(nome, out var v) ? v : resposta.Content.Headers.GetValues(nome));
}
