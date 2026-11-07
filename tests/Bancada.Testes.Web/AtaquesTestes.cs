using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Bancada.Infraestrutura.Dados;
using Bancada.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Bancada.Testes.Web;

/// <summary>
/// Regressão dos ataques da auditoria de segurança (RELATORIO-AUDITORIA-CYBERSECURITY.md). Cada teste reproduz um ataque
/// que funcionou antes da correção; o identificador SEC-xx liga o teste ao achado.
/// </summary>
public class AtaquesTestes
{
    private static readonly LimitesDeTaxa DuasNovasSessoes = new() { Leitura = 1000, Escrita = 1000, NovaSessao = 2 };

    /// <summary>Faz o servidor de teste enxergar a conexão vindo deste IP (como um proxy reverso na frente da aplicação).</summary>
    private sealed class IpRemoto(string ip) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> proximo) => app =>
        {
            app.Use((contexto, seguir) =>
            {
                contexto.Connection.RemoteIpAddress = IPAddress.Parse(ip);
                return seguir(contexto);
            });
            proximo(app);
        };
    }

    private static async Task<HttpStatusCode[]> NovosVisitantes(AppDeTeste app, params string?[] encaminhadoPara)
    {
        var status = new HttpStatusCode[encaminhadoPara.Length];
        for (var i = 0; i < encaminhadoPara.Length; i++)
        {
            using var cliente = app.NovoCliente();
            using var pedido = new HttpRequestMessage(HttpMethod.Get, "/bancada");
            if (encaminhadoPara[i] is { } ip)
            {
                pedido.Headers.Add("X-Forwarded-For", ip);
            }

            status[i] = (await cliente.SendAsync(pedido)).StatusCode;
        }

        return status;
    }

    [Test]
    public async Task SEC01_Executor_EmProducaoSemConfiguracao_NaoRodaNada()
    {
        var processo = new ProcessoDeTeste();
        using var app = new AppDeTeste(Navegador.SemLimite, extra: s => s.AddSingleton<IProcessos>(processo), ambiente: "Production");
        using var n = new Navegador(app.NovoCliente());

        Assert.That(await n.PostarESeguir("/painel/testes/executar", ("suite", "js")), Does.Contain("desligado"));
        Assert.That(processo.Chamadas, Is.Zero);
    }

    [Test]
    public async Task SEC01_Executor_EmProducao_SoRodaSeLigadoDePropositoNaConfiguracao()
    {
        var processo = new ProcessoDeTeste();
        using var app = new AppDeTeste(Navegador.SemLimite, extra: s => s.AddSingleton<IProcessos>(processo), configuracao: ("Bancada:Executor:Habilitado", "true"), ambiente: "Production");
        using var n = new Navegador(app.NovoCliente());

        await n.PostarESeguir("/painel/testes/executar", ("suite", "js"));

        Assert.That(processo.Chamadas, Is.EqualTo(1));
    }

    [Test]
    public async Task SEC02_CookieDeSessaoInventado_ContaNoLimiteDeNovasSessoes()
    {
        using var app = new AppDeTeste(DuasNovasSessoes);
        var status = new HttpStatusCode[3];
        for (var i = 0; i < 3; i++)
        {
            using var cliente = app.NovoCliente();
            cliente.DefaultRequestHeaders.Add("Cookie", $"{SessaoMiddleware.NomeDoCookie}=inventado{i.Txt()}");
            status[i] = (await cliente.GetAsync("/bancada")).StatusCode;
        }

        Assert.That(status, Is.EqualTo(new[] { HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests }));
        Assert.That(app.Sessoes.Quantidade, Is.EqualTo(2));
    }

    [Test]
    public async Task SEC03_XForwardedFor_SemProxyConfigurado_EIgnorado()
    {
        using var app = new AppDeTeste(DuasNovasSessoes);

        var status = await NovosVisitantes(app, "198.51.100.1", "198.51.100.2", "198.51.100.3");

        Assert.That(status[2], Is.EqualTo(HttpStatusCode.TooManyRequests), "o cabeçalho forjado não pode criar um balde novo por requisição");
    }

    [Test]
    public async Task SEC03_XForwardedFor_DoProxyConfigurado_SeparaOsVisitantes()
    {
        using var app = new AppDeTeste(DuasNovasSessoes, extra: s => s.AddSingleton<IStartupFilter>(new IpRemoto("10.0.0.1")), configuracao: ("Bancada:ProxiesConfiaveis", "10.0.0.1"));

        var distintos = await NovosVisitantes(app, "198.51.100.1", "198.51.100.2", "198.51.100.3");
        var mesmo = await NovosVisitantes(app, "198.51.100.9", "198.51.100.9", "198.51.100.9");

        Assert.That(distintos, Is.All.EqualTo(HttpStatusCode.OK), "atrás do proxy, cada visitante tem o seu balde");
        Assert.That(mesmo[2], Is.EqualTo(HttpStatusCode.TooManyRequests), "o mesmo visitante continua limitado");
    }

    [Test]
    public async Task SEC03_XForwardedFor_DeQuemNaoEOProxy_EIgnorado()
    {
        using var app = new AppDeTeste(DuasNovasSessoes, extra: s => s.AddSingleton<IStartupFilter>(new IpRemoto("10.9.9.9")), configuracao: ("Bancada:ProxiesConfiaveis", "10.0.0.0/24"));

        var status = await NovosVisitantes(app, "198.51.100.1", "198.51.100.2", "198.51.100.3");

        Assert.That(status[2], Is.EqualTo(HttpStatusCode.TooManyRequests));
    }

    [Test]
    public async Task SEC04_Auditoria_RecusasSimultaneasComRequisicoesEmCurso_CadeiaContinuaIntegra()
    {
        using var app = new AppDeTeste(new LimitesDeTaxa { Leitura = 4, Escrita = 4, NovaSessao = 100 });
        using var n = new Navegador(app.NovoCliente());
        await n.Ver("/bancada");
        var sessao = app.UltimaSessao();

        // Requisições lentas seguram a sessão enquanto as recusas do limite de taxa chegam ao mesmo tempo.
        var respostas = await Task.WhenAll(Enumerable.Range(0, 40).Select(_ => n.Cliente.GetAsync("/teste/lento")));
        await n.Ver("/saude");

        var noBanco = ((BancoDaSessao)sessao.Banco!).NovoContexto().Set<EventoDeAuditoria>().OrderBy(e => e.Seq).ToList();
        Assert.That(respostas.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests), Is.GreaterThan(0));
        Assert.That(ServicoDeAuditoria.Verificar(sessao), Is.Null, "cadeia em memória íntegra");
        Assert.That(CadeiaDeAuditoria.PrimeiroEloQuebrado(noBanco), Is.Null, "cadeia gravada íntegra");
        Assert.That(sessao.Eventos.Select(e => e.Seq), Is.EqualTo(Enumerable.Range(1, sessao.Eventos.Count)), "sequência sem buraco nem repetição");
        Assert.That(noBanco, Has.Count.EqualTo(sessao.Eventos.Count));
        Assert.That(sessao.Pendentes, Is.Zero, "nenhum evento esquecido na fila");
        Assert.That(sessao.Eventos.Count(e => e.Acao == "LimiteDeTaxaExcedido"), Is.GreaterThan(0));
    }

    [Test]
    public async Task SEC07_SessaoDescartadaEnquantoAEsperava_SegueNumaSessaoNova()
    {
        using var app = new AppDeTeste(Navegador.SemLimite);
        using var n = new Navegador(app.NovoCliente());
        await n.Ver("/bancada");
        var sessao = app.UltimaSessao();

        var segurando = n.Cliente.GetAsync("/teste/portao");
        await ControladorDeTeste.Chegou.Task;
        var antes = sessao.UltimoAcesso;
        var esperando = n.Cliente.GetAsync("/bancada");
        while (sessao.UltimoAcesso == antes)
        {
            await Task.Delay(5); // a segunda requisição já pegou a sessão e espera a vez
        }

        app.Sessoes.Remover(sessao.Id);
        Assert.That(sessao.Descartada, Is.True);
        ControladorDeTeste.Abrir.SetResult();
        var primeira = await segurando;
        var segunda = await esperando;

        Assert.That(primeira.StatusCode, Is.EqualTo(HttpStatusCode.OK), "quem estava usando terminou sem erro: o banco não fechou no meio");
        Assert.That(segunda.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(AppDeTeste.IdDaSessao(segunda), Is.Not.Null.And.Not.EqualTo(sessao.Id));
    }

    [TestCase("203.0.113.7", "203.0.113.7")]
    [TestCase("::ffff:203.0.113.7", "203.0.113.7")]
    [TestCase("2001:db8:1:2:3:4:5:6", "2001:db8:1:2::/64")]
    [TestCase("::1", "::/64")]
    public void SEC03_ChaveDoCliente_IPv4InteiroEIPv6PorPrefixo(string ip, string esperado)
    {
        Assert.That(ProtecaoDeEntrada.ChaveDoCliente(IPAddress.Parse(ip)), Is.EqualTo(esperado));
    }

    [Test]
    public void SEC03_ChaveDoCliente_SemIp_Desconhecido()
    {
        Assert.That(ProtecaoDeEntrada.ChaveDoCliente(null), Is.EqualTo("desconhecido"));
    }

    [Test]
    public void SEC03_ProxiesConfiaveis_SoOsListados()
    {
        Assert.That(Registro.CabecalhosEncaminhados(null), Is.Null);
        Assert.That(Registro.CabecalhosEncaminhados(" , "), Is.Null);

        var opcoes = Registro.CabecalhosEncaminhados("10.0.0.1, 192.168.0.0/16; 2001:db8::/32")!;

        Assert.That(opcoes.KnownProxies, Is.EqualTo(new[] { IPAddress.Parse("10.0.0.1") }));
        Assert.That(opcoes.KnownNetworks.Select(r => $"{r.Prefix}/{r.PrefixLength.Txt()}"), Is.EqualTo(new[] { "192.168.0.0/16", "2001:db8::/32" }));
        Assert.That(opcoes.ForwardLimit, Is.EqualTo(1));
        Assert.That(() => Registro.CabecalhosEncaminhados("nao-e-ip"), Throws.InstanceOf<FormatException>());
    }

    [Test]
    public void SEC09_BloqueioDeIp_MuitosIpsDiferentes_PodaOsVencidos()
    {
        var relogio = new RelogioDeTeste();
        var bloqueio = new BloqueioDeIp(relogio);
        for (var i = 0; i < BloqueioDeIp.PodarAcimaDe; i++)
        {
            bloqueio.Contar("ip" + i.Txt());
        }

        for (var i = 0; i < BloqueioDeIp.Tolerancia; i++)
        {
            bloqueio.Contar("insistente");
        }

        relogio.Agora += TimeSpan.FromMinutes(2);
        bloqueio.Contar("novo");

        Assert.That(bloqueio.Quantidade, Is.EqualTo(2), "sobram o bloqueado (ainda vale) e o novo");
        Assert.That(bloqueio.Bloqueado("insistente"), Is.True);
    }

    private sealed class RelogioDeTeste : IRelogio
    {
        public DateTimeOffset Agora { get; set; } = DateTimeOffset.UnixEpoch;
    }

    [Test]
    public async Task SEC12_DuasSessoesEmParalelo_NenhumaVeOuMexeNoDadoDaOutra()
    {
        using var app = new AppDeTeste(Navegador.SemLimite, papelInicial: null);
        using var a = new Navegador(app.NovoCliente());
        using var b = new Navegador(app.NovoCliente());
        await Task.WhenAll(a.Ver("/bancada"), b.Ver("/bancada"));

        await Task.WhenAll(Enumerable.Range(0, 10).SelectMany(i => new[]
        {
            a.Postar("/prontuario/14/alergia", ("substancia", "MarcaA" + i.Txt())),
            b.Postar("/prontuario/14/alergia", ("substancia", "MarcaB" + i.Txt())),
        }));
        var (_, deA) = await a.Ver("/prontuario/14");
        var (_, deB) = await b.Ver("/prontuario/14");

        Assert.That(Enumerable.Range(0, 10).All(i => deA.Contains("MarcaA" + i.Txt(), StringComparison.Ordinal)), Is.True);
        Assert.That(Enumerable.Range(0, 10).All(i => deB.Contains("MarcaB" + i.Txt(), StringComparison.Ordinal)), Is.True);
        Assert.That(deA, Does.Not.Contain("MarcaB"));
        Assert.That(deB, Does.Not.Contain("MarcaA"));
    }

    [TestCase("PUT")]
    [TestCase("DELETE")]
    [TestCase("PATCH")]
    [TestCase("OPTIONS")]
    [TestCase("TRACE")]
    public async Task SEC05_VerboNaoSuportado_Devolve405ENao500(string verbo)
    {
        using var app = new AppDeTeste(Navegador.SemLimite);
        using var cliente = app.NovoCliente();

        var r = await cliente.SendAsync(new HttpRequestMessage(new HttpMethod(verbo), "/pacientes"));

        Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.MethodNotAllowed));
        Assert.That(await r.Content.ReadAsStringAsync(), Does.Not.Contain("Exception"));
    }
}

/// <summary>
/// SEC-12: as regras de negócio do laboratório atacadas por requisição direta (sem a tela), fora de ordem e em paralelo.
/// O botão desabilitado na tela não é controle: quem decide é o servidor.
/// </summary>
public class RegrasSobAtaqueTestes
{
    private AppDeTeste _app = null!;
    private Navegador _n = null!;

    [SetUp]
    public void Preparar()
    {
        _app = new AppDeTeste(Navegador.SemLimite);
        _n = new Navegador(_app.NovoCliente());
    }

    [TearDown]
    public void Limpar()
    {
        _n.Dispose();
        _app.Dispose();
    }

    /// <summary>Coleta, guarda e processa o PED-000045 até ter resultados preliminares (enviados por ANA-01). Devolve o id do pedido.</summary>
    private async Task<int> AteResultadosPendentes(string? cenario = null)
    {
        if (cenario is not null)
        {
            await _n.PostarESeguir("/sessao/cenario", ("cenario", cenario));
        }

        await _n.Assumir("Coleta");
        var pedido = Navegador.Numeros((await _n.Ver("/coletas")).Html, @"<td class=""mono"">PED-000045</td>[\s\S]*?/coletas/(\d+)/coletar")[0];
        await _n.PostarESeguir($"/coletas/{pedido}/coletar", ("material", "Soro"), ("volume", "5"), ("jejum", "12"));
        var amostra = Navegador.Numeros((await _n.Ver("/coletas")).Html, @"/amostras/(\d+)/etiqueta")[0];
        var posicao = Navegador.Numeros((await _n.Ver("/soroteca?caixa=R2-C4")).Html, @"posicao=(\d+)")[0];
        await _n.PostarESeguir("/soroteca/armazenar", ("amostra", amostra.Txt()), ("posicao", posicao.Txt()), ("caixa", "R2-C4"));
        await _n.Assumir("Analista", "ANA-01");
        await _n.PostarESeguir($"/equipamentos/{amostra}/enviar");
        await _n.PostarESeguir("/equipamentos/relogio", ("comando", "600"));
        await Task.Delay(1200);
        await _n.Ver("/equipamentos");
        await Task.Delay(200);
        await _n.Ver("/equipamentos");
        return pedido;
    }

    private async Task<IReadOnlyList<int>> Laudos() => Navegador.Numeros((await _n.Ver("/resultados")).Html, @"/laudos/(\d+)"">PED-000045 v");

    [Test]
    public async Task SEC12_QuemEnviou_ValidaPorRequisicaoDireta_Recusado()
    {
        await AteResultadosPendentes();
        var resultado = Navegador.Numeros((await _n.Ver("/resultados")).Html, @"/resultados/(\d+)/validar")[0];

        Assert.That(await _n.PostarESeguir($"/resultados/{resultado}/validar", ("justificativa", "forçando")), Does.Contain("Quem enviou ao equipamento não valida"));
    }

    [Test]
    public async Task SEC12_CriticoDeltaLiberacaoERetificacao_NaoSeContornam()
    {
        var pedido = await AteResultadosPendentes();
        await _n.Assumir("Analista", "ANA-02");
        var pagina = (await _n.Ver("/resultados")).Html;
        var critico = Navegador.Numeros(pagina, @"/resultados/(\d+)/confirmar-critico")[0];

        Assert.That(await _n.PostarESeguir($"/resultados/{critico}/validar", ("justificativa", "x")), Does.Contain("Confirme o valor crítico"));
        await _n.PostarESeguir($"/resultados/{critico}/confirmar-critico");
        var delta = Navegador.Numeros((await _n.Ver("/resultados")).Html, @"action=""/resultados/(\d+)/validar""[^>]*>\s*<input[^>]*name=""justificativa""")[0];
        Assert.That(await _n.PostarESeguir($"/resultados/{delta}/validar"), Does.Contain("Delta suspeito exige justificativa"));
        Assert.That(await _n.PostarESeguir($"/resultados/{delta}/validar", ("justificativa", "    ")), Does.Contain("Delta suspeito exige justificativa"));
        Assert.That(await _n.PostarESeguir($"/laudos/{pedido}/liberar"), Does.Contain("O laudo só sai com todos"));
        Assert.That(await Laudos(), Is.Empty);

        foreach (var r in Navegador.Numeros((await _n.Ver("/resultados")).Html, @"/resultados/(\d+)/validar"))
        {
            await _n.PostarESeguir($"/resultados/{r}/validar", ("justificativa", "Conferido"));
        }

        Assert.That(await _n.PostarESeguir($"/resultados/{delta}/validar", ("justificativa", "de novo")), Does.Contain("não vale para o estado atual"), "validar duas vezes não passa");
        var corrida = await Task.WhenAll(_n.Postar($"/laudos/{pedido}/liberar"), _n.Postar($"/laudos/{pedido}/liberar"));
        Assert.That(corrida.Select(r => r.StatusCode), Is.All.EqualTo(HttpStatusCode.Redirect));
        var laudos = await Laudos();
        Assert.That(laudos, Has.Count.EqualTo(1), "duas liberações simultâneas geram um laudo só");

        Assert.That(await _n.PostarESeguir($"/laudos/{pedido}/retificar", ("motivo", "   ")), Does.Contain("Informe o motivo"));
        Assert.That(await Laudos(), Has.Count.EqualTo(1));
        await _n.PostarESeguir($"/laudos/{pedido}/retificar", ("motivo", "Erro de digitação"));
        Assert.That(await Laudos(), Has.Count.EqualTo(2));
        Assert.That((await _n.Ver($"/laudos/{laudos[0]}")).Html, Does.Not.Contain("Erro de digitação"), "a versão liberada não muda");
    }

    [Test]
    public async Task SEC12_ControleReprovado_BloqueiaValidacaoMesmoPorRequisicaoDireta()
    {
        await AteResultadosPendentes("cq");
        await _n.Assumir("Analista", "ANA-02");
        var resultados = Navegador.Numeros((await _n.Ver("/resultados")).Html, @"/resultados/(\d+)/validar");

        Assert.That(resultados, Is.Not.Empty);
        foreach (var r in resultados)
        {
            Assert.That(await _n.PostarESeguir($"/resultados/{r}/validar", ("justificativa", "forçando")), Does.Contain("Controle de qualidade reprovado"));
        }
    }
}
