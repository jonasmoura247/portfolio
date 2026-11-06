using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Bancada.Testes.Web;

/// <summary>Agenda e fila abrem modal com os dados; linhas das listas levam ao detalhe em qualquer ponto do clique.</summary>
public class ModaisTestes
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
    public async Task Agenda_CelulaOcupadaAbreModalComDadosDoPaciente()
    {
        var (_, agenda) = await _n.Ver("/agenda?profissional=MED-01&semana=0");
        var horario = Navegador.Numeros(agenda, @"class=""clicavel"" data-href=""/agenda\?[^""]*horario=(\d+)""")[0];

        var (status, html) = await _n.Ver($"/agenda?profissional=MED-01&semana=0&horario={horario}");

        Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(html, Does.Contain("<dialog class=\"modal\" data-modal data-volta=\"/agenda?profissional=MED-01&amp;semana=0\""));
        Assert.That(html, Does.Contain("Ver ficha do paciente"));
        Assert.That(html, Does.Contain("Motivo"));
    }

    [Test]
    public async Task Agenda_MedicoVeOModalSemAcoesDeRecepcao()
    {
        await _n.Assumir("Medico", "MED-01");
        var (_, agenda) = await _n.Ver("/agenda?profissional=MED-01&semana=0");
        var horario = Navegador.Numeros(agenda, @"class=""clicavel"" data-href=""/agenda\?[^""]*horario=(\d+)""")[0];

        var (_, html) = await _n.Ver($"/agenda?profissional=MED-01&semana=0&horario={horario}");

        Assert.That(html, Does.Contain("data-modal"));
        Assert.That(html, Does.Not.Contain("/confirmar"));
        Assert.That(html, Does.Contain("Nenhuma ação disponível"));
    }

    [Test]
    public async Task Agenda_HorarioInexistenteNaoAbreModal()
    {
        var (_, html) = await _n.Ver("/agenda?horario=999999");

        Assert.That(html, Does.Not.Contain("data-modal"));
    }

    [Test]
    public async Task Fila_LinhaInteiraAbreOResumo_ClinicoSoParaMedicoEAdmin()
    {
        var (_, fila) = await _n.Ver("/atendimentos?semana=true");
        var ids = Navegador.Numeros(fila, @"<tr class=""clicavel"" data-href=""/atendimentos\?[^""]*abrir=(\d+)""");
        Assert.That(ids, Is.Not.Empty);

        var (_, recepcao) = await _n.Ver($"/atendimentos?semana=true&abrir={ids[0]}");
        Assert.That(recepcao, Does.Contain("data-modal"));
        Assert.That(recepcao, Does.Contain("O conteúdo clínico só aparece para Médico e Admin."));
        Assert.That(recepcao, Does.Not.Contain("class=\"evolucao\""));

        await _n.Assumir("Admin");
        var paginas = new System.Collections.Generic.List<string>();
        foreach (var id in ids)
        {
            var (s, h) = await _n.Ver($"/atendimentos?semana=true&abrir={id}");
            Assert.That(s, Is.EqualTo(HttpStatusCode.OK), id.Txt());
            paginas.Add(h);
        }

        Assert.That(paginas.Any(p => p.Contains("class=\"evolucao\"")), Is.True, "algum atendimento concluído do seed tem evolução");
        Assert.That(paginas.Any(p => p.Contains("Abrir atendimento completo")), Is.True);
        Assert.That(paginas.Any(p => p.Contains("Chegada")), Is.True);
        Assert.That(paginas.Any(p => p.Contains("Duração do atendimento")), Is.True);
        Assert.That(paginas.Any(p => p.Contains("Sem registros.")), Is.True);
        Assert.That(paginas.Any(p => p.Contains("Nenhum exame pedido neste atendimento.")), Is.True);
    }

    [Test]
    public async Task Fila_ResumoDeAtendimentoInexistenteE404()
    {
        var (status, _) = await _n.Ver("/atendimentos?abrir=999999");

        Assert.That(status, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Pedidos_LinhaInteiraLevaAoDetalhe()
    {
        var (_, lista) = await _n.Ver("/pedidos");

        Assert.That(Navegador.Numeros(lista, @"<tr class=""clicavel"" data-href=""/pedidos/(\d+)"""), Is.Not.Empty);
    }

    [Test]
    public async Task Coletas_AmostraTemLinhaClicavelEBotaoDeEtiqueta()
    {
        await _n.Assumir("Coleta");
        var (_, coletas) = await _n.Ver("/coletas");
        var pedido = Navegador.Numeros(coletas, @"<td class=""mono"">PED-000045</td>[\s\S]*?/coletas/(\d+)/coletar")[0];
        await _n.PostarESeguir($"/coletas/{pedido}/coletar", ("material", "Soro"), ("volume", "5"), ("jejum", "12"));

        var (_, depois) = await _n.Ver("/coletas");

        Assert.That(Navegador.Numeros(depois, @"<tr class=""clicavel"" data-href=""/amostras/(\d+)/etiqueta"""), Is.Not.Empty);
        Assert.That(depois, Does.Contain("Ver etiqueta"));
    }

    [Test]
    public async Task Etiquetas_ConsultaPorCodigoEMenuLateralPorPapel()
    {
        Assert.That((await _n.Ver("/etiquetas")).Status, Is.EqualTo(HttpStatusCode.Forbidden));
        var (_, menuRecepcao) = await _n.Ver("/bancada");
        Assert.That(menuRecepcao, Does.Not.Contain("Consultar etiqueta"));

        await _n.Assumir("Coleta");
        var (_, coletas) = await _n.Ver("/coletas");
        var pedido = Navegador.Numeros(coletas, @"<td class=""mono"">PED-000045</td>[\s\S]*?/coletas/(\d+)/coletar")[0];
        await _n.PostarESeguir($"/coletas/{pedido}/coletar", ("material", "Soro"), ("volume", "5"), ("jejum", "12"));

        var (status, todas) = await _n.Ver("/etiquetas");
        Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(todas, Does.Contain("Consultar etiqueta"));
        Assert.That(todas, Does.Contain("Consultar soroteca"));
        var id = Navegador.Numeros(todas, @"<tr class=""clicavel"" data-href=""/amostras/(\d+)/etiqueta""")[0];
        Assert.That((await _n.Ver("/etiquetas?busca=ZZZ-NADA")).Html, Does.Contain("Nenhuma etiqueta encontrada."));

        await _n.Assumir("Analista");
        Assert.That((await _n.Ver($"/amostras/{id}/etiqueta")).Html, Does.Contain("code128"));
        Assert.That(_n.UltimaPagina, Does.Contain("href=\"/etiquetas\""));
    }

    [Test]
    public async Task Operacao_MostraOsNumerosEOsGraficos()
    {
        var (status, html) = await _n.Ver("/operacao");

        Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(html, Does.Contain("class=\"numeros\""));
        Assert.That(html, Does.Contain("class=\"operacao\""));
    }

    [Test]
    public async Task Bancada_SemConfiguracao_EntraComoAdmin()
    {
        using var app = new AppDeTeste(Navegador.SemLimite, papelInicial: null);
        using var n = new Navegador(app.NovoCliente());

        var (_, html) = await n.Ver("/bancada");

        Assert.That(html, Does.Contain("ADM-01"));
        Assert.That(html, Does.Contain("href=\"/etiquetas\""));
        Assert.That(html, Does.Contain("href=\"/auditoria\""));
        Assert.That(app.UltimaSessao().Papel, Is.EqualTo(Bancada.Dominio.Papel.Admin));
    }

    [Test]
    public async Task LabAtaque_FoiRemovido()
    {
        Assert.That((await _n.Ver("/lab-ataque")).Status, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
