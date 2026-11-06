using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Bancada.Web;
using Bancada.Web.Controllers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Bancada.Testes.Web;

internal sealed class ProcessoDeTeste : IProcessos
{
    public int Chamadas { get; private set; }

    public Task<int?> Rodar(ComandoDeTeste comando, Action<string> aoLer, CancellationToken cancelar)
    {
        Chamadas++;
        aoLer("<script>saida</script>");
        return Task.FromResult<int?>(0);
    }
}

public class TelasTestes
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

    [Test]
    public async Task Jornada_DoAgendamentoAoLaudo()
    {
        var (_, agenda) = await _n.Ver("/agenda?semana=1");
        var horario = Navegador.Numeros(agenda, @"class=""livre clicavel""[^>]*><a href=""/agenda\?[^""]*horario=(\d+)""")[0];
        await _n.Ver($"/agenda?profissional=MED-01&semana=1&horario={horario}");
        Assert.That(_n.UltimaPagina, Does.Contain("/agenda/agendar"));
        Assert.That(await _n.PostarESeguir("/agenda/agendar", ("horario", horario.Txt()), ("paciente", "PAC-0002"), ("profissional", "MED-01"), ("semana", "1")), Does.Contain("Agendamento criado"));
        var (_, depois) = await _n.Ver($"/agenda?profissional=MED-01&semana=1&horario={horario}");
        var id = Navegador.Numeros(depois, @"/agenda/(\d+)/confirmar")[0];
        Assert.That(await _n.PostarESeguir($"/agenda/{id}/confirmar", ("profissional", "MED-01"), ("semana", "1")), Does.Contain("Agendamento confirmado"));
        var (_, outro) = await _n.Ver($"/agenda?profissional=MED-01&semana=1");
        var novo = Navegador.Numeros(outro, @"class=""livre clicavel""[^>]*><a href=""/agenda\?[^""]*horario=(\d+)""")[1];
        Assert.That(await _n.PostarESeguir($"/agenda/{id}/reagendar", ("horario", novo.Txt())), Does.Contain("novo horário"));
        Assert.That(await _n.PostarESeguir($"/agenda/{id}/cancelar"), Does.Contain("Agendamento cancelado"));
        Assert.That(await _n.PostarESeguir("/agenda/agendar", ("horario", "999999"), ("paciente", "PAC-0002")), Does.Contain("não existe na grade"));

        var (_, fila) = await _n.Ver("/atendimentos");
        var checkin = Navegador.Numeros(fila, @"/atendimentos/(\d+)/checkin")[0];
        Assert.That(await _n.PostarESeguir($"/atendimentos/{checkin}/checkin"), Does.Contain("Check-in feito"));
        await _n.Ver("/atendimentos?semana=true");
        Assert.That(_n.UltimaPagina, Does.Contain("Só hoje"));

        await _n.Assumir("Medico", "MED-01");
        var (_, filaMedico) = await _n.Ver("/atendimentos");
        var atender = Navegador.Numeros(filaMedico, @"/atendimentos/(\d+)/iniciar")[0];
        Assert.That(await _n.PostarESeguir($"/atendimentos/{atender}/iniciar"), Does.Contain("Atendimento iniciado"));
        Assert.That(await _n.PostarESeguir($"/atendimentos/{atender}/concluir"), Does.Contain("pelo menos uma evolução"));
        Assert.That(await _n.PostarESeguir($"/atendimentos/{atender}/evolucao", ("texto", "<script>alert(1)</script>")), Does.Contain("&lt;script&gt;alert(1)&lt;/script&gt;"));
        Assert.That(await _n.PostarESeguir($"/atendimentos/{atender}/concluir"), Does.Contain("Atendimento concluído"));

        var (_, novoPedido) = await _n.Ver("/pedidos/novo?paciente=PAC-0047");
        Assert.That(novoPedido, Does.Contain("Látex"));
        await _n.Ver("/pedidos/novo?paciente=inventado");
        await _n.Ver("/pedidos/novo");
        var pedido = await _n.PostarESeguir("/pedidos/novo", ("paciente", "PAC-0002"), ("exames", "GLI"), ("exames", "HEM"), ("urgente", "true"));
        Assert.That(pedido, Does.Contain("Pedido criado"));
        var criado = Navegador.Numeros(pedido, @"/pedidos/(\d+)/cancelar")[0];
        Assert.That(await _n.PostarESeguir($"/pedidos/{criado}/cancelar"), Does.Contain("Pedido cancelado"));
        Assert.That(await _n.PostarESeguir("/pedidos/novo", ("paciente", "PAC-0002")), Does.Contain("de 1 a 20 exames"));

        await _n.Ver("/prontuario");
        var (_, prontuario) = await _n.Ver("/prontuario/8");
        Assert.That(await _n.PostarESeguir("/prontuario/2/alergia", ("substancia", "Mofo"), ("reacao", "Rinite")), Does.Contain("Mofo"));
        var alergia = Navegador.Numeros(_n.UltimaPagina, @"/prontuario/alergia/(\d+)/alternar")[0];
        Assert.That(await _n.PostarESeguir($"/prontuario/alergia/{alergia}/alternar", ("paciente", "2")), Does.Contain("Alergias atualizadas"));
        var evolucao = Navegador.Numeros(prontuario, @"corrigir=(\d+)")[0];
        await _n.Ver($"/prontuario/8?corrigir={evolucao}");
        Assert.That(_n.UltimaPagina, Does.Contain($"/prontuario/evolucao/{evolucao}/corrigir"));
        Assert.That(await _n.PostarESeguir($"/prontuario/evolucao/{evolucao}/corrigir", ("texto", "Corrigido"), ("paciente", "8")), Does.Contain("Correção registrada"));

        await _n.Assumir("Coleta");
        var (_, coletas) = await _n.Ver("/coletas");
        var pedido45 = Navegador.Numeros(coletas, @"<td class=""mono"">PED-000045</td>[\s\S]*?/coletas/(\d+)/coletar")[0];
        Assert.That(await _n.PostarESeguir($"/coletas/{pedido45}/coletar", ("material", "Soro"), ("volume", "0.1"), ("jejum", "12")), Does.Contain("Volume menor"));
        var etiqueta = await _n.PostarESeguir($"/coletas/{pedido45}/coletar", ("material", "Soro"), ("volume", "5"), ("jejum", "12"));
        Assert.That(etiqueta, Does.Contain("code128"));
        var amostra = Navegador.Numeros((await _n.Ver("/coletas")).Html, @"/amostras/(\d+)/etiqueta")[0];

        var (_, soroteca) = await _n.Ver("/soroteca?caixa=R2-C4");
        var posicao = Navegador.Numeros(soroteca, @"posicao=(\d+)")[0];
        var (_, painelPos) = await _n.Ver($"/soroteca?caixa=R2-C4&posicao={posicao}");
        Assert.That(painelPos, Does.Contain("/soroteca/armazenar"));
        Assert.That(await _n.PostarESeguir("/soroteca/armazenar", ("amostra", amostra.Txt()), ("posicao", posicao.Txt()), ("caixa", "R2-C4")), Does.Contain("Amostra armazenada"));
        await _n.Ver($"/soroteca?caixa=R2-C4&posicao={posicao}");
        var destino = Navegador.Numeros(_n.UltimaPagina, @"<option value=""(\d+)"">R2-C4")[0];
        Assert.That(await _n.PostarESeguir("/soroteca/mover", ("origem", posicao.Txt()), ("destino", destino.Txt()), ("caixa", "R2-C4")), Does.Contain("Amostra movida"));

        await _n.Assumir("Analista", "ANA-01");
        await _n.Ver("/equipamentos");
        Assert.That(_n.UltimaPagina, Does.Contain($"/equipamentos/{amostra}/enviar"));
        Assert.That(await _n.PostarESeguir($"/equipamentos/{amostra}/enviar"), Does.Contain("enviada ao equipamento"));
        Assert.That(await _n.PostarESeguir("/equipamentos/relogio", ("comando", "600")), Does.Contain("Relógio ajustado"));
        await Task.Delay(1200);
        var (_, equipamentos) = await _n.Ver("/equipamentos");
        Assert.That(equipamentos, Does.Contain("ENQ"));
        await Task.Delay(200);
        await _n.Ver("/equipamentos");

        var (_, pendentes) = await _n.Ver("/resultados");
        Assert.That(pendentes, Does.Contain("delta suspeito"));
        Assert.That(pendentes, Does.Contain("disabled"));
        await _n.Assumir("Analista", "ANA-02");
        var (_, comoAna2) = await _n.Ver("/resultados");
        var critico = Navegador.Numeros(comoAna2, @"/resultados/(\d+)/confirmar-critico")[0];
        Assert.That(await _n.PostarESeguir($"/resultados/{critico}/validar"), Does.Contain("Confirme o valor crítico"));
        Assert.That(await _n.PostarESeguir($"/resultados/{critico}/confirmar-critico"), Does.Contain("Valor crítico confirmado"));
        foreach (var r in Navegador.Numeros(_n.UltimaPagina, @"/resultados/(\d+)/validar"))
        {
            await _n.PostarESeguir($"/resultados/{r}/validar", ("justificativa", "Conferido"));
        }

        var liberar = Navegador.Numeros(_n.UltimaPagina, @"/laudos/(\d+)/liberar")[0];
        var laudo = await _n.PostarESeguir($"/laudos/{liberar}/liberar");
        Assert.That(laudo, Does.Contain("Laudo liberado"));
        Assert.That(laudo, Does.Contain("CH ▲▲"));
        Assert.That(laudo, Does.Contain("PED-000045 · v1"));
        var retificado = await _n.PostarESeguir($"/laudos/{liberar}/retificar", ("motivo", "Erro de digitação"));
        Assert.That(retificado, Does.Contain("Erro de digitação"));
        Assert.That(await _n.PostarESeguir("/laudos/999999/liberar"), Does.Contain(string.Empty));
    }

    [Test]
    public async Task Soroteca_DescartaERejeita()
    {
        await _n.Assumir("Analista", "ANA-01");
        var (_, mapa) = await _n.Ver("/soroteca?caixa=R1-C1");
        var ocupada = Navegador.Numeros(mapa, @"class=""pos oc"" href=""/soroteca\?caixa=R1-C1&(?:amp;)?posicao=(\d+)""")[0];
        var (_, detalhe) = await _n.Ver($"/soroteca?caixa=R1-C1&posicao={ocupada}");
        var amostra = Navegador.Numeros(detalhe, @"name=""amostra"" value=""(\d+)""")[0];

        Assert.That(await _n.PostarESeguir("/soroteca/descartar", ("amostra", amostra.Txt()), ("motivo", " ")), Does.Contain("motivo do descarte"));
        Assert.That(await _n.PostarESeguir("/soroteca/descartar", ("amostra", amostra.Txt()), ("motivo", "Prazo")), Does.Contain("Amostra descartada"));
    }

    [Test]
    public async Task Cenario_HemolisadaVemMarcadaEARejeicaoVoltaParaColeta()
    {
        await _n.Postar("/sessao/cenario", ("cenario", "hemolisada"));
        await _n.Assumir("Coleta");
        var (_, coletas) = await _n.Ver("/coletas");
        Assert.That(coletas, Does.Contain("checked"));
        var pedido = Navegador.Numeros(coletas, @"<td class=""mono"">PED-000045</td>[\s\S]*?/coletas/(\d+)/coletar")[0];
        await _n.PostarESeguir($"/coletas/{pedido}/coletar", ("material", "Soro"), ("volume", "5"), ("jejum", "12"), ("hemolisada", "true"));
        var (_, soroteca) = await _n.Ver("/soroteca");
        Assert.That(soroteca, Does.Contain("Rejeitar por hemólise"));
        var amostra = Navegador.Numeros(soroteca, @"/amostras/(\d+)/rejeitar")[0];

        Assert.That(await _n.PostarESeguir($"/amostras/{amostra}/rejeitar", ("motivo", "Hemolise")), Does.Contain("recoleta"));
        Assert.That((await _n.Ver("/coletas")).Html, Does.Contain("PED-000045"));
    }

    [Test]
    public async Task Cenario_ErroDoEquipamentoEReprocesso()
    {
        await _n.Postar("/sessao/cenario", ("cenario", "erro"));
        await _n.Assumir("Coleta");
        var pedido = Navegador.Numeros((await _n.Ver("/coletas")).Html, @"<td class=""mono"">PED-000045</td>[\s\S]*?/coletas/(\d+)/coletar")[0];
        await _n.Postar($"/coletas/{pedido}/coletar", ("material", "Soro"), ("volume", "5"), ("jejum", "12"));
        var amostra = Navegador.Numeros((await _n.Ver("/coletas")).Html, @"/amostras/(\d+)/etiqueta")[0];
        var posicao = Navegador.Numeros((await _n.Ver("/soroteca?caixa=R2-C4")).Html, @"posicao=(\d+)")[0];
        await _n.Postar("/soroteca/armazenar", ("amostra", amostra.Txt()), ("posicao", posicao.Txt()), ("caixa", "R2-C4"));
        await _n.Assumir("Analista", "ANA-01");
        await _n.Postar($"/equipamentos/{amostra}/enviar");
        await _n.Postar("/equipamentos/relogio", ("comando", "600"));
        await _n.Ver("/equipamentos");
        await Task.Delay(1100);
        var (_, tela) = await _n.Ver("/equipamentos");
        var execucao = Navegador.Numeros(tela, @"/execucoes/(\d+)/reprocessar")[0];

        Assert.That(await _n.PostarESeguir($"/execucoes/{execucao}/reprocessar"), Does.Contain("voltou para a fila"));
    }

    [TestCase("cq", "Controle de qualidade")]
    public async Task Cenario_ControleReprovadoApareceEMedidaNovaLibera(string cenario, string titulo)
    {
        await _n.Postar("/sessao/cenario", ("cenario", cenario));
        await _n.Assumir("Analista");
        var (_, q) = await _n.Ver("/qualidade");

        Assert.That(q, Does.Contain(titulo));
        Assert.That(q, Does.Contain("2-2s"));
        Assert.That(await _n.PostarESeguir("/qualidade/medicao"), Does.Contain("Controle registrado"));
    }

    [Test]
    public async Task Cenario_InvalidoEIdiomaInvalidoSao400()
    {
        Assert.That((await _n.Postar("/sessao/cenario", ("cenario", "inventado"))).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await _n.Postar("/sessao/cenario")).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await _n.Postar("/sessao/idioma", ("idioma", "fr"))).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Idioma_TrocaATelaParaIngles()
    {
        await _n.Postar("/sessao/idioma", ("idioma", "en"));

        Assert.That((await _n.Ver("/pacientes")).Html, Does.Contain("Fictional patients"));
    }

    [Test]
    public async Task Pacientes_BuscaPaginaEFicha()
    {
        Assert.That((await _n.Ver("/pacientes?busca=PAC-0014")).Html, Does.Contain("PAC-0014"));
        Assert.That((await _n.Ver("/pacientes?busca=zzzz")).Html, Does.Contain("Nenhum paciente"));
        Assert.That((await _n.Ver("/pacientes?pagina=2")).Html, Does.Contain("Anterior"));
        Assert.That((await _n.Ver("/pacientes/14")).Status, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await _n.Ver("/pacientes/9999")).Status, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await _n.Ver("/pedidos")).Html, Does.Contain("PED-000045"));
        Assert.That((await _n.Ver("/pedidos/45")).Html, Does.Contain("PED-000045"));
    }

    [Test]
    public async Task Auditoria_VerificaSimulaEFiltra()
    {
        await _n.Assumir("Analista");
        Assert.That((await _n.Ver("/auditoria?grupo=seguranca")).Status, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await _n.Ver("/auditoria?pagina=0")).Status, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await _n.PostarESeguir("/auditoria/verificar"), Does.Contain("Cadeia íntegra"));
        Assert.That(await _n.PostarESeguir("/auditoria/simular"), Does.Contain("quebrou no evento"));
    }

    [Test]
    public async Task Operacao_MostraGraficos()
    {
        var html = (await _n.Ver("/operacao")).Html;

        Assert.That(html, Does.Contain("<svg"));
        Assert.That(html, Does.Not.Contain("style="));
    }

    [TestCase("geral")]
    [TestCase("testes")]
    [TestCase("arquitetura")]
    [TestCase("banco")]
    [TestCase("seguranca")]
    [TestCase("licencas")]
    [TestCase("catalogo")]
    [TestCase("codigo")]
    [TestCase("inventada")]
    public async Task Painel_TodaAbaAbre(string aba) =>
        Assert.That((await _n.Ver("/painel?aba=" + aba)).Status, Is.EqualTo(HttpStatusCode.OK));

    [TestCase("../../appsettings.json")]
    [TestCase("..\\..\\etc\\passwd")]
    [TestCase("C:\\Windows\\win.ini")]
    public async Task Codigo_ForaDaListaE404(string arquivo) =>
        Assert.That((await _n.Ver("/painel?aba=codigo&arquivo=" + Uri.EscapeDataString(arquivo))).Status, Is.EqualTo(HttpStatusCode.NotFound));

    [Test]
    public async Task Codigo_DaListaAbreComTextoEscapado()
    {
        var html = (await _n.Ver("/painel?aba=codigo&arquivo=Regras.cs")).Html;

        Assert.That(html, Does.Contain("MaquinaDeEstados"));
        Assert.That(CodigoPermitido.Arquivos, Has.Count.GreaterThan(5));
        Assert.That(CodigoPermitido.Ler(null), Is.Null);
    }

    [TestCase("/prontuario/1/alergia", "substancia")]
    [TestCase("/prontuario/1/alergia", "reacao")]
    [TestCase("/agenda/agendar", "motivo")]
    [TestCase("/atendimentos/1/evolucao", "texto")]
    [TestCase("/prontuario/evolucao/1/corrigir", "texto")]
    [TestCase("/soroteca/descartar", "motivo")]
    [TestCase("/resultados/1/validar", "justificativa")]
    [TestCase("/laudos/1/retificar", "motivo")]
    [TestCase("/amostras/1/rejeitar", "motivo")]
    public async Task Corpus_EntradaHostilNuncaDa500(string url, string campo)
    {
        await _n.Assumir("Medico");
        await _n.Assumir("Admin");
        foreach (var lista in Corpus.Values)
        {
            foreach (var payload in lista.Append("\r\nSet-Cookie: a=b").Append("\0"))
            {
                var r = await _n.Postar(url, (campo, payload));
                Assert.That((int)r.StatusCode, Is.LessThan(500), payload);
                Assert.That(r.Headers.Contains("a"), Is.False);
            }
        }
    }

    [Test]
    public async Task Corpus_NaBuscaApareceComoTextoEEsquemaFicaIntacto()
    {
        foreach (var payload in Corpus.Values.SelectMany(l => l))
        {
            var (status, html) = await _n.Ver("/pacientes?busca=" + Uri.EscapeDataString(payload));
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), payload);
            Assert.That(html, Does.Not.Contain("<script>alert"));
        }
    }

    /// <summary>Corpus público de entradas hostis (vault: Corpus-de-Ataque): nenhum formulário pode responder 500 nem vazar cabeçalho.</summary>
    private static readonly IReadOnlyDictionary<string, string[]> Corpus = new Dictionary<string, string[]>
    {
        ["sqli"] = ["' OR '1'='1", "'; DROP TABLE Paciente;--", "1 UNION SELECT NULL--", "\" OR \"\"=\""],
        ["xss"] = ["<script>alert(1)</script>", "\"><img src=x onerror=alert(1)>", "<svg/onload=alert(1)>", "javascript:alert(1)"],
        ["template"] = ["{{7*7}}", "${7*7}", "@(7*7)"],
        ["caminho"] = ["../../etc/passwd", "..\\..\\appsettings.json", "%2e%2e%2f", "....//"],
    };

    [Test]
    public async Task Papel_ErradoRecebe403EEventoNaAuditoria()
    {
        foreach (var url in new[] { "/auditoria", "/equipamentos", "/qualidade", "/coletas", "/soroteca", "/resultados" })
        {
            Assert.That((await _n.Ver(url)).Status, Is.EqualTo(HttpStatusCode.Forbidden), url);
        }

        await _n.Assumir("Analista");
        Assert.That((await _n.Ver("/auditoria?grupo=seguranca")).Html, Does.Contain("AutorizacaoRecusada"));
    }

    [Test]
    public async Task Medico_VeLaudosSemFilaDeValidacao()
    {
        await _n.Assumir("Medico");
        var (_, html) = await _n.Ver("/resultados");

        Assert.That(html, Does.Contain("Laudos liberados"));
        Assert.That(html, Does.Not.Contain("aguardando validação"));
    }

    [Test]
    public async Task Antifalsificacao_PostSemTokenVira400EEventoDeSeguranca()
    {
        await _n.Ver("/bancada");
        var r = await _n.Cliente.PostAsync("/agenda/agendar", null);

        Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await _n.Assumir("Analista");
        Assert.That((await _n.Ver("/auditoria?grupo=seguranca")).Html, Does.Contain("AntifalsificacaoInvalido"));
    }

    [Test]
    public async Task Corpo_GrandeDemaisRecebe413()
    {
        await _n.Ver("/bancada");
        using var corpo = new System.Net.Http.StringContent(new string('a', (int)ProtecaoDeEntrada.TamanhoMaximoDoCorpo + 1));
        var r = await _n.Cliente.PostAsync("/agenda/agendar", corpo);

        Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.RequestEntityTooLarge));
        await _n.Assumir("Analista");
        Assert.That((await _n.Ver("/auditoria?grupo=seguranca")).Html, Does.Contain("CorpoGrandeDemais"));
    }

    [Test]
    public async Task Corpo_GrandeSemSessaoNaoCriaEstado()
    {
        using var c = _app.NovoCliente();
        using var corpo = new System.Net.Http.StringContent(new string('a', 20000));

        Assert.That((await c.PostAsync("/agenda/agendar", corpo)).StatusCode, Is.EqualTo(HttpStatusCode.RequestEntityTooLarge));
    }

    [Test]
    public async Task Cota_EstouradaViraAvisoEEvento()
    {
        await _n.Assumir("Admin");
        var sessao = _app.UltimaSessao();
        for (var i = 0; i < Sessao.CotaDeRegistros; i++)
        {
            sessao.ContarRegistro();
        }

        Assert.That(await _n.PostarESeguir("/prontuario/1/alergia", ("substancia", "Mofo")), Does.Contain("Limite de 500"));
        Assert.That((await _n.Ver("/auditoria?grupo=seguranca")).Html, Does.Contain("CotaExcedida"));
    }
}

public class PainelExecutorTestes
{
    [Test]
    public async Task Executor_RodaPeloBotaoESaidaVoltaEscapadaNoJson()
    {
        var processo = new ProcessoDeTeste();
        using var app = new AppDeTeste(Navegador.SemLimite, extra: s => s.AddSingleton<IProcessos>(processo));
        using var n = new Navegador(app.NovoCliente());

        Assert.That(await n.PostarESeguir("/painel/testes/executar", ("suite", "js"), ("comando", "rm -rf /")), Does.Contain("Execução em andamento"));
        Assert.That(await n.PostarESeguir("/painel/testes/executar", ("suite", "csharp")), Does.Contain("Limite de execuções"));
        var (_, estado) = await n.Ver("/painel/testes/estado");

        Assert.That(processo.Chamadas, Is.EqualTo(1));
        Assert.That(estado, Does.Contain("\"suite\":\"js\""));
    }

    [Test]
    public async Task Executor_Desligado_RecusaEAvisa()
    {
        using var app = new AppDeTeste(Navegador.SemLimite, configuracao: ("Bancada:Executor:Habilitado", "false"));
        using var n = new Navegador(app.NovoCliente());

        Assert.That(await n.PostarESeguir("/painel/testes/executar", ("suite", "js")), Does.Contain("desligado"));
    }

    [Test]
    public async Task Instantaneo_AparecePrincipalmenteNasAbasDeNumeros()
    {
        var arquivo = Path.GetTempFileName();
        await File.WriteAllTextAsync(arquivo, """
            {"gerado_em":"2026-11-01T12:00:00Z","cobertura_minima":100,
             "csharp":{"testes":3,"falhas":0,"projetos":[{"nome":"Dominio","testes":3}],"cobertura":[{"nome":"Bancada.Dominio","linhas":100}]},
             "javascript":{"testes":2,"falhas":0},
             "regras":[{"rn":"RN-001","teste":"RN_001_X"}],
             "licencas":[{"pacote":"NUnit","versao":"5.0.0","licenca":"MIT"}]}
            """);
        using var app = new AppDeTeste(Navegador.SemLimite, configuracao: ("Bancada:Instantaneo", arquivo));
        using var n = new Navegador(app.NovoCliente());

        Assert.That((await n.Ver("/painel")).Html, Does.Contain("Bancada.Dominio"));
        Assert.That((await n.Ver("/painel?aba=licencas")).Html, Does.Contain("NUnit"));
        Assert.That((await n.Ver("/painel?aba=catalogo")).Html, Does.Contain("RN_001_X"));
        File.Delete(arquivo);
    }

    [Test]
    public async Task Bloqueio_RepetirOEstouroBloqueiaOIpEAnotaEvento()
    {
        using var app = new AppDeTeste(new LimitesDeTaxa { Leitura = 3, Escrita = 3, NovaSessao = 100 });
        using var n = new Navegador(app.NovoCliente());
        await n.Ver("/bancada");
        var ultimo = HttpStatusCode.OK;
        for (var i = 0; i < 12; i++)
        {
            ultimo = (await n.Ver("/bancada")).Status;
        }

        var sessao = app.UltimaSessao();
        Assert.That(ultimo, Is.EqualTo(HttpStatusCode.TooManyRequests));
        Assert.That(sessao.Eventos.Select(e => e.Acao), Does.Contain("IpBloqueado"));
        Assert.That(sessao.Eventos.Select(e => e.Acao), Does.Contain("LimiteDeTaxaExcedido"));
        Assert.That((await n.Ver("/saude")).Status, Is.EqualTo(HttpStatusCode.OK));
    }
}

public class ApresentacaoTestes
{
    private sealed class RelogioFixo : IRelogio
    {
        public DateTimeOffset Agora { get; set; } = DateTimeOffset.UnixEpoch;
    }

    private static string Texto(IHtmlContent c)
    {
        using var w = new StringWriter();
        c.WriteTo(w, System.Text.Encodings.Web.HtmlEncoder.Default);
        return w.ToString();
    }

    [Test]
    public void Barras_EscapamRotuloEAguentamListaVazia()
    {
        var html = Texto(Graficos.Barras([new Dado("<b>", 5, "a"), new Dado("zero", 0, "t")], "min", "r\"ótulo"));

        Assert.That(html, Does.Contain("&lt;b&gt;"));
        Assert.That(html, Does.Not.Contain("<b>"));
        Assert.That(Texto(Graficos.Barras([], "x", "r")), Does.StartWith("<svg"));
    }

    [Test]
    public void Rosca_SomaAsFatiasEAguentaZero()
    {
        Assert.That(Texto(Graficos.Rosca([new Dado("a", 1, "a"), new Dado("b", 3, "t")], "75%", "x", "r")), Does.Contain("25%"));
        Assert.That(Texto(Graficos.Rosca([new Dado("a", 0, "a")], "0%", "x", "r")), Does.Contain("<circle"));
    }

    [Test]
    public void LeveyJennings_PintaPelaDistanciaDaMedia()
    {
        var html = Texto(Graficos.LeveyJennings([0, 2.5, -3.5, 9], 4.2, 0.12, "r"));

        Assert.That(html, Does.Contain("b-at"));
        Assert.That(html, Does.Contain("b-pe"));
        Assert.That(html, Does.Contain("b-a"));
    }

    [Test]
    public void Fluxo_MarcaOQueFoiFeito()
    {
        var html = Texto(Graficos.Fluxo([("Agenda", true), ("Laudo", false)], "r"));

        Assert.That(html, Does.Contain("b-t"));
        Assert.That(html, Does.Contain("caixa"));
    }

    [Test]
    public void Dependencias_LeAsReferenciasReais()
    {
        var camadas = PainelController.Camadas();

        Assert.That(camadas.Single(c => c.Nome == "Bancada.Dominio").Usa, Is.Empty);
        Assert.That(camadas.Single(c => c.Nome == "Bancada.Web").Usa, Does.Contain("Bancada.Aplicacao"));
        Assert.That(Texto(Graficos.Dependencias(camadas, "r")), Does.Contain("marker-end"));
    }

    [Test]
    public void CodigoDeBarras_DesenhaSoAsBarras()
    {
        var html = Texto(Graficos.CodigoDeBarras("2112", "AMO"));

        Assert.That(html.Split("<rect").Length - 1, Is.EqualTo(2));
        Assert.That(html, Does.Contain("viewBox=\"0 0 6 40\""));
    }

    [TestCase("Validado", "s-ok")]
    [TestCase("Inventado", "s-neutro")]
    public void Selo_TemCorPorEstado(string estado, string classe) => Assert.That(Apresentacao.Selo(estado), Is.EqualTo(classe));

    [TestCase("N", "f-n", "N")]
    [TestCase("H", "f-a", "H ▲")]
    [TestCase("L", "f-a", "L ▼")]
    [TestCase("CH", "f-c", "CH ▲▲")]
    [TestCase("CL", "f-c", "CL ▼▼")]
    public void Flag_NuncaESoCor(string flag, string classe, string rotulo)
    {
        Assert.That(Apresentacao.Flag(flag), Is.EqualTo(classe));
        Assert.That(Apresentacao.Rotulo(flag), Is.EqualTo(rotulo));
    }

    [Test]
    public void Protocolo_MostraOsControlesComoTexto()
    {
        Assert.That(Apresentacao.Protocolo("\x02" + "1H|\r\x03\x1F"), Is.EqualTo("⟨STX⟩1H|⟨CR⟩⟨ETX⟩⟨?⟩"));
    }

    [Test]
    public void Faixa_CobreLimitesAbertos()
    {
        Assert.That(Apresentacao.Faixa(1, 2, 1), Is.EqualTo("1,0 a 2,0"));
        Assert.That(Apresentacao.Faixa(null, 2, 0), Is.EqualTo("< 2"));
        Assert.That(Apresentacao.Faixa(1, null, 0), Is.EqualTo("> 1"));
        Assert.That(Apresentacao.Faixa(null, null, 0), Is.EqualTo("—"));
    }

    [Test]
    public void Bloqueio_ExpiraENaoContaRecusaAntiga()
    {
        var relogio = new RelogioFixo();
        var b = new BloqueioDeIp(relogio);
        for (var i = 1; i < BloqueioDeIp.Tolerancia; i++)
        {
            Assert.That(b.Contar("ip"), Is.False);
        }

        relogio.Agora += TimeSpan.FromMinutes(2);
        Assert.That(b.Contar("ip"), Is.False, "a janela reiniciou");
        Assert.That(b.Bloqueado("ip"), Is.False);
        for (var i = 1; i < BloqueioDeIp.Tolerancia - 1; i++)
        {
            b.Contar("ip");
        }

        Assert.That(b.Contar("ip"), Is.True);
        Assert.That(b.Bloqueado("ip"), Is.True);
        Assert.That(b.Bloqueado("outro"), Is.False);
        relogio.Agora += TimeSpan.FromMinutes(11);
        Assert.That(b.Bloqueado("ip"), Is.False);
    }

    [Test]
    public void Ip_SemConexaoCaiNoDesconhecido()
    {
        var contexto = new DefaultHttpContext();
        Assert.That(ProtecaoDeEntrada.Ip(contexto), Is.EqualTo("desconhecido"));
        contexto.Connection.RemoteIpAddress = IPAddress.Loopback;
        Assert.That(ProtecaoDeEntrada.Ip(contexto), Is.EqualTo("127.0.0.1"));
    }

    [Test]
    public void Kestrel_SemCabecalhoServerEComCorpoLimitado()
    {
        var opcoes = new Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions();

        Registro.Kestrel(opcoes);

        Assert.That(opcoes.AddServerHeader, Is.False);
        Assert.That(opcoes.Limits.MaxRequestBodySize, Is.EqualTo(ProtecaoDeEntrada.TamanhoMaximoDoCorpo));
    }

    [Test]
    public void RaizDosTestes_ConfiguradaOuARaizDoRepositorio()
    {
        Assert.That(Registro.RaizDosTestes(null, Path.Combine(Path.GetTempPath(), "a", "b")), Is.EqualTo(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)));
        Assert.That(Registro.RaizDosTestes(Path.GetTempPath(), "x"), Is.EqualTo(Path.GetFullPath(Path.GetTempPath())));
    }

    [Test]
    public void Filtro_SoTrataNaoEncontrado()
    {
        var contexto = new Microsoft.AspNetCore.Mvc.Filters.ExceptionContext(
            new Microsoft.AspNetCore.Mvc.ActionContext(new DefaultHttpContext(), new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()),
            [])
        { Exception = new InvalidOperationException() };

        new FiltroDeNaoEncontrado().OnException(contexto);

        Assert.That(contexto.ExceptionHandled, Is.False);
    }
}
