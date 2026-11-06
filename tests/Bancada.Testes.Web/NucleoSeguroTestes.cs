using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Bancada.Web;

namespace Bancada.Testes.Web;

public class NucleoSeguroTestes
{
    private static readonly string[] EventosEsperados = ["SessaoCriada", "PapelTrocado", "OperadorTrocado"];

    private AppDeTeste _app = null!;

    [SetUp]
    public void Preparar() => _app = new AppDeTeste();

    [TearDown]
    public void Limpar() => _app.Dispose();

    private static async Task<(HttpResponseMessage Resposta, string Html)> Pegar(HttpClient c, string url)
    {
        var r = await c.GetAsync(url);
        return (r, await r.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task Home_PrimeiraVisita_CriaSessaoComCookieSeguro()
    {
        using var cliente = _app.NovoCliente();

        var (resposta, html) = await Pegar(cliente, "/bancada");

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var cookie = resposta.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("__Host-bancada.sid=", StringComparison.Ordinal)).ToLowerInvariant();
        Assert.That(cookie, Does.Contain("httponly"));
        Assert.That(cookie, Does.Contain("secure"));
        Assert.That(cookie, Does.Contain("samesite=lax"));
        Assert.That(cookie, Does.Contain("path=/"));
        Assert.That(cookie, Does.Not.Contain("domain="), "prefixo __Host- proíbe Domain");
        Assert.That(cookie, Does.Not.Contain("expires="), "cookie de sessão");
        Assert.That(AppDeTeste.IdDaSessao(resposta), Has.Length.EqualTo(43));
        Assert.That(html, Does.Contain("Um laboratório de mentira"));
    }

    [Test]
    public async Task Home_Recepcao_MostraSoOsModulosDoPapel()
    {
        using var cliente = _app.NovoCliente();

        var (_, html) = await Pegar(cliente, "/bancada");

        Assert.That(html, Does.Contain("Agenda"));
        Assert.That(html, Does.Contain("Pedido de exame"));
        Assert.That(html, Does.Not.Contain("<a href=\"/soroteca\""));
        Assert.That(html, Does.Not.Contain("<a href=\"/equipamentos\""));
    }

    [Test]
    public async Task Pagina_NaoTemScriptNemEstiloInlineNemManipuladorDeEvento()
    {
        using var cliente = _app.NovoCliente();

        var (_, html) = await Pegar(cliente, "/bancada");

        Assert.That(html, Does.Not.Match(@"<script(?![^>]*\bsrc=)"), "script inline");
        Assert.That(html, Does.Not.Match(@"\sstyle="), "estilo inline");
        Assert.That(html, Does.Not.Match(@"\son\w+="), "manipulador de evento inline");
    }

    [Test]
    public async Task TrocarPapel_RenovaOIdEMudaOMenu()
    {
        using var cliente = _app.NovoCliente();
        var (primeira, html) = await Pegar(cliente, "/bancada");
        var idAntigo = AppDeTeste.IdDaSessao(primeira)!;

        var resposta = await AppDeTeste.Postar(cliente, "/sessao/papel", AppDeTeste.TokenAntifalsificacao(html), ("papel", "Analista"));

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        var idNovo = AppDeTeste.IdDaSessao(resposta)!;
        Assert.That(idNovo, Is.Not.EqualTo(idAntigo), "id renovado: defesa contra fixação de sessão");
        var (_, depois) = await Pegar(cliente, "/bancada");
        Assert.That(depois, Does.Contain("Soroteca"));
        Assert.That(depois, Does.Contain("Equipamento"));
        Assert.That(depois, Does.Not.Contain("<a href=\"/agenda\""));
        Assert.That(depois, Does.Contain("ANA-01"));
    }

    [Test]
    public async Task IdAntigoDepoisDaTroca_NaoValeMais()
    {
        using var cliente = _app.NovoCliente();
        var (primeira, html) = await Pegar(cliente, "/bancada");
        var antigo = AppDeTeste.IdDaSessao(primeira)!;
        await AppDeTeste.Postar(cliente, "/sessao/papel", AppDeTeste.TokenAntifalsificacao(html), ("papel", "Admin"));

        using var outro = _app.NovoCliente();
        outro.DefaultRequestHeaders.Add("Cookie", $"__Host-bancada.sid={antigo}");
        var (resposta, _) = await Pegar(outro, "/bancada");

        Assert.That(AppDeTeste.IdDaSessao(resposta), Is.Not.Null.And.Not.EqualTo(antigo), "id desconhecido nunca é adotado");
    }

    [Test]
    public async Task TrocarOperador_Valido_RenovaEGravaAuditoria()
    {
        using var cliente = _app.NovoCliente();
        var (_, html) = await Pegar(cliente, "/bancada");
        await AppDeTeste.Postar(cliente, "/sessao/papel", AppDeTeste.TokenAntifalsificacao(html), ("papel", "Analista"));
        var (_, html2) = await Pegar(cliente, "/bancada");

        var resposta = await AppDeTeste.Postar(cliente, "/sessao/operador", AppDeTeste.TokenAntifalsificacao(html2), ("operador", "ANA-02"));

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        var sessao = _app.Sessoes.ObterOuCriar(AppDeTeste.IdDaSessao(resposta));
        Assert.That(sessao.Operador, Is.EqualTo("ANA-02"));
        Assert.That(sessao.Eventos.Select(e => e.Acao), Is.EqualTo(EventosEsperados));
        Assert.That(ServicoDeAuditoria.Verificar(sessao), Is.Null);
    }

    [Test]
    public async Task TrocarOperador_DeOutroPapelOuAusente_Devolve400()
    {
        using var cliente = _app.NovoCliente();
        var (_, html) = await Pegar(cliente, "/bancada");
        var token = AppDeTeste.TokenAntifalsificacao(html);

        var outroPapel = await AppDeTeste.Postar(cliente, "/sessao/operador", token, ("operador", "MED-01"));
        var ausente = await AppDeTeste.Postar(cliente, "/sessao/operador", token);

        Assert.That(outroPapel.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(ausente.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase("")]
    [TestCase("Inexistente")]
    [TestCase("99")]
    public async Task TrocarPapel_Invalido_Devolve400(string valor)
    {
        using var cliente = _app.NovoCliente();
        var (_, html) = await Pegar(cliente, "/bancada");

        var resposta = await AppDeTeste.Postar(cliente, "/sessao/papel", AppDeTeste.TokenAntifalsificacao(html), ("papel", valor));

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task TrocarPapel_SemCampo_Devolve400()
    {
        using var cliente = _app.NovoCliente();
        var (_, html) = await Pegar(cliente, "/bancada");

        var resposta = await AppDeTeste.Postar(cliente, "/sessao/papel", AppDeTeste.TokenAntifalsificacao(html));

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Post_SemTokenAntifalsificacao_Devolve400ComPaginaGenerica()
    {
        using var cliente = _app.NovoCliente();
        await Pegar(cliente, "/bancada");

        var resposta = await cliente.PostAsync("/sessao/papel", new FormUrlEncodedContent([new("papel", "Admin")]));

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await resposta.Content.ReadAsStringAsync(), Does.Contain("Requisição inválida"));
    }

    [Test]
    public async Task TokenDeOutraSessao_NaoServe()
    {
        using var vitima = _app.NovoCliente();
        using var atacante = _app.NovoCliente();
        var (_, htmlAtacante) = await Pegar(atacante, "/bancada");
        await Pegar(vitima, "/bancada");

        var resposta = await AppDeTeste.Postar(vitima, "/sessao/papel", AppDeTeste.TokenAntifalsificacao(htmlAtacante), ("papel", "Admin"));

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task TokenAntigo_DepoisDeTrocarPapel_NaoServeMais()
    {
        using var cliente = _app.NovoCliente();
        var (_, html) = await Pegar(cliente, "/bancada");
        var tokenAntigo = AppDeTeste.TokenAntifalsificacao(html);
        await AppDeTeste.Postar(cliente, "/sessao/papel", tokenAntigo, ("papel", "Medico"));

        var reuso = await AppDeTeste.Postar(cliente, "/sessao/papel", tokenAntigo, ("papel", "Admin"));

        Assert.That(reuso.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Reiniciar_DescartaASessao_EOProximoAcessoCriaOutra()
    {
        using var cliente = _app.NovoCliente();
        var (primeira, html) = await Pegar(cliente, "/bancada");
        var antigo = AppDeTeste.IdDaSessao(primeira)!;

        var resposta = await AppDeTeste.Postar(cliente, "/sessao/reiniciar", AppDeTeste.TokenAntifalsificacao(html));
        var (proxima, _) = await Pegar(cliente, "/bancada");

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(AppDeTeste.IdDaSessao(proxima), Is.Not.Null.And.Not.EqualTo(antigo));
    }

    [Test]
    public async Task AcaoProtegida_PapelSemPermissao_Devolve403EAuditaARecusa()
    {
        using var cliente = _app.NovoCliente();
        var (primeira, _) = await Pegar(cliente, "/bancada");
        var id = AppDeTeste.IdDaSessao(primeira)!;

        var resposta = await cliente.GetAsync("/teste/protegido");

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(await resposta.Content.ReadAsStringAsync(), Does.Contain("Acesso negado"));
        var sessao = _app.Sessoes.ObterOuCriar(id);
        var recusa = sessao.Eventos.Single(e => e.Acao == "AutorizacaoRecusada");
        Assert.That(recusa.Resultado, Is.EqualTo("Recusado"));
        Assert.That(recusa.AlvoId, Is.EqualTo("DescartarAmostra"));
    }

    [Test]
    public async Task AcaoProtegida_PapelComPermissao_Libera()
    {
        using var cliente = _app.NovoCliente();
        var (_, html) = await Pegar(cliente, "/bancada");
        await AppDeTeste.Postar(cliente, "/sessao/papel", AppDeTeste.TokenAntifalsificacao(html), ("papel", "Analista"));

        var resposta = await cliente.GetAsync("/teste/protegido");

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await resposta.Content.ReadAsStringAsync(), Is.EqualTo("liberado"));
    }

    [Test]
    public async Task RotaInexistente_Devolve404GenericoESemCriarSessao()
    {
        using var cliente = _app.NovoCliente();

        var resposta = await cliente.GetAsync("/wp-login.php");

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await resposta.Content.ReadAsStringAsync(), Does.Contain("Página não encontrada"));
        Assert.That(resposta.Headers.Contains("Set-Cookie"), Is.False);
        Assert.That(_app.Sessoes.Quantidade, Is.Zero, "varredura de caminhos não enche a memória");
    }

    [Test]
    public async Task ExcecaoNaoTratada_Devolve500SemVazarDetalhe()
    {
        using var cliente = _app.NovoCliente();
        await Pegar(cliente, "/bancada");

        var resposta = await cliente.GetAsync("/teste/explode");
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(corpo, Does.Contain("Algo deu errado"));
        Assert.That(corpo, Does.Not.Contain("segredo interno"));
        Assert.That(corpo, Does.Not.Contain("InvalidOperationException"));
        Assert.That(corpo, Does.Not.Contain("Bancada.Testes"));
    }

    [TestCase(400, HttpStatusCode.BadRequest)]
    [TestCase(403, HttpStatusCode.Forbidden)]
    [TestCase(404, HttpStatusCode.NotFound)]
    [TestCase(429, HttpStatusCode.TooManyRequests)]
    [TestCase(500, HttpStatusCode.InternalServerError)]
    [TestCase(418, HttpStatusCode.InternalServerError)]
    public async Task PaginaDeErro_CodigoConhecidoOuNao_UsaOStatusCerto(int codigo, HttpStatusCode esperado)
    {
        using var cliente = _app.NovoCliente();

        var resposta = await cliente.GetAsync($"/erro/{codigo}");

        Assert.That(resposta.StatusCode, Is.EqualTo(esperado));
        Assert.That(resposta.Headers.Contains("Set-Cookie"), Is.False, "página de erro não cria sessão");
    }

    [Test]
    public async Task MesmaSessao_RequisicoesParalelas_ExecutamUmaDeCadaVez()
    {
        using var cliente = _app.NovoCliente();
        await Pegar(cliente, "/bancada");

        var respostas = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => cliente.GetAsync("/teste/lento")));

        Assert.That(respostas.All(r => r.StatusCode == HttpStatusCode.OK), Is.True);
        Assert.That(ControladorDeTeste.MaximoSimultaneo, Is.EqualTo(1));
    }

    [Test]
    public async Task SessoesDiferentes_NaoSeEnxergam()
    {
        using var a = _app.NovoCliente();
        using var b = _app.NovoCliente();
        var (_, htmlA) = await Pegar(a, "/bancada");
        await AppDeTeste.Postar(a, "/sessao/papel", AppDeTeste.TokenAntifalsificacao(htmlA), ("papel", "Admin"));

        var (_, htmlB) = await Pegar(b, "/bancada");

        Assert.That(htmlB, Does.Contain("REC-01"), "B continua Recepção");
        Assert.That(htmlB, Does.Not.Contain("ADM-01"));
        Assert.That(_app.Sessoes.Quantidade, Is.EqualTo(2));
    }

    [Test]
    public async Task Idioma_EmIngles_TrocaOsTextosDaInterface()
    {
        using var cliente = _app.NovoCliente();
        var (primeira, _) = await Pegar(cliente, "/bancada");
        _app.Sessoes.ObterOuCriar(AppDeTeste.IdDaSessao(primeira)).Idioma = "en";

        var (_, html) = await Pegar(cliente, "/bancada");

        Assert.That(html, Does.Contain("A fake laboratory, built for real"));
        Assert.That(html, Does.Contain("lang=\"en\""));
    }

    [Test]
    public async Task Saude_NaoCriaSessaoNemCookie()
    {
        using var cliente = _app.NovoCliente();

        var resposta = await cliente.GetAsync("/saude");

        Assert.That(resposta.Headers.Contains("Set-Cookie"), Is.False);
        Assert.That(_app.Sessoes.Quantidade, Is.Zero);
    }

    [Test]
    public async Task ArquivosEstaticos_SaoServidosComOsCabecalhosDeSeguranca()
    {
        using var cliente = _app.NovoCliente();

        var resposta = await cliente.GetAsync("/css/estilo.css");

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(resposta.Headers.Contains("Content-Security-Policy"), Is.True);
        Assert.That(resposta.Headers.Contains("Set-Cookie"), Is.False);
    }

    [Test]
    public async Task Raiz_ServeOPortfolio_SemSessao_ELevaAoSistema()
    {
        using var cliente = _app.NovoCliente();

        var (resposta, html) = await Pegar(cliente, "/");

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(html, Does.Contain("Jonas Emanuel"));
        Assert.That(html, Does.Contain("href=\"/bancada\""));
        Assert.That(html, Does.Not.Contain("<style").And.Not.Contain("style=\"").And.Not.Contain("onclick"), "a CSP não aceita estilo nem evento inline");
        Assert.That(System.Text.RegularExpressions.Regex.IsMatch(html, @"<script(?![^>]*\ssrc=)"), Is.False, "a CSP não aceita script inline");
        Assert.That(resposta.Headers.Contains("Set-Cookie"), Is.False);
        Assert.That(_app.Sessoes.Quantidade, Is.Zero);
        foreach (var ativo in new[] { "/portfolio/portfolio.css", "/portfolio/portfolio.js" })
        {
            Assert.That((await cliente.GetAsync(ativo)).StatusCode, Is.EqualTo(HttpStatusCode.OK), ativo);
        }
    }

    [Test]
    public async Task Https_Ausente_PorPadrao_FalhaFechado()
    {
        using var http = _app.NovoClienteHttp();

        var resposta = await http.GetAsync("/bancada");

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(await resposta.Content.ReadAsStringAsync(), Does.Not.Contain("antiforgery"));
    }

    [Test]
    public async Task CookiesSeguros_DesligadoSoParaRodarLocal_FuncionaEmHttp()
    {
        using var local = new AppDeTeste(cookiesSeguros: false);
        using var http = local.NovoClienteHttp();

        var resposta = await http.GetAsync("/bancada");

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(AppDeTeste.IdDaSessao(resposta), Is.Not.Null);
        var cookies = string.Join(";", resposta.Headers.GetValues("Set-Cookie"));
        Assert.That(cookies, Does.Contain("bancada.af="));
        Assert.That(cookies, Does.Not.Contain("__Host-bancada.af"), "sem Secure o prefixo __Host- é descartado pelo navegador");
    }

    [Test]
    public async Task CookiesSeguros_PorPadrao_AntifalsificacaoUsaPrefixoHostESecure()
    {
        using var cliente = _app.NovoCliente();

        var resposta = await cliente.GetAsync("/bancada");

        var af = resposta.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("__Host-bancada.af=", StringComparison.Ordinal)).ToLowerInvariant();
        Assert.That(af, Does.Contain("secure"));
        Assert.That(af, Does.Contain("samesite=strict"));
        Assert.That(af, Does.Contain("httponly"));
    }
}
