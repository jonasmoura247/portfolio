using System;
using System.Collections.Generic;
using System.Linq;
using Bancada.Dominio;
using Bancada.Web;
using Microsoft.AspNetCore.Http;

namespace Bancada.Testes.Web;

public class UnidadesWebTestes
{
    [Test]
    public void PrecisaDeSessao_SemEndpoint_Nao()
    {
        Assert.That(PipelineDaSessao.PrecisaDeSessao(new DefaultHttpContext()), Is.False);
    }

    [TestCase("/", true)]
    [TestCase("/pacientes", true)]
    [TestCase("/saude", false)]
    [TestCase("/erro/404", false)]
    public void PrecisaDeSessao_ComEndpoint_SoParaRotasDoSistema(string caminho, bool esperado)
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = caminho;
        contexto.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(), "teste"));

        Assert.That(PipelineDaSessao.PrecisaDeSessao(contexto), Is.EqualTo(esperado));
    }

    [Test]
    public void Sessao_SemMiddleware_LancaErroDeConfiguracao()
    {
        Assert.That(() => new DefaultHttpContext().Sessao(), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Sessao_ItemDeOutroTipo_LancaErroDeConfiguracao()
    {
        var contexto = new DefaultHttpContext();
        contexto.Items[SessaoMiddleware.ChaveNoContexto] = "nao sou uma sessao";

        Assert.That(() => contexto.Sessao(), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Textos_ChaveOuIdiomaInexistente_LancaErro()
    {
        Assert.That(() => CatalogoDeTextos.Obter("nao.existe", "pt"), Throws.TypeOf<KeyNotFoundException>());
        Assert.That(() => CatalogoDeTextos.Obter("app.nome", "es"), Throws.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public void Textos_ChaveExistente_DevolveOTextoDoIdioma()
    {
        Assert.That(CatalogoDeTextos.Obter("menu.inicio", "pt"), Is.EqualTo("Início"));
        Assert.That(CatalogoDeTextos.Obter("menu.inicio", "en"), Is.EqualTo("Home"));
        Assert.That(CatalogoDeTextos.Chaves, Does.Contain("app.nome"));
    }

    [Test]
    public void Modulos_IdsUnicosESempreOInicioParaTodos()
    {
        Assert.That(Modulos.Todos.Select(m => m.Id).Distinct().Count(), Is.EqualTo(Modulos.Todos.Count));
        foreach (var papel in Enum.GetValues<Papel>())
        {
            Assert.That(Modulos.Todos.First(m => m.Id == "inicio").VisivelPara(papel), Is.True);
        }
    }

    [TestCase(Papel.Recepcao, "agenda", true)]
    [TestCase(Papel.Recepcao, "soroteca", false)]
    [TestCase(Papel.Medico, "prontuario", true)]
    [TestCase(Papel.Coleta, "prontuario", true)]
    [TestCase(Papel.Coleta, "agenda", false)]
    [TestCase(Papel.Analista, "equipamento", true)]
    [TestCase(Papel.Analista, "agenda", false)]
    [TestCase(Papel.Medico, "resultados", true)]
    [TestCase(Papel.Recepcao, "resultados", false)]
    [TestCase(Papel.Admin, "auditoria", true)]
    public void Modulo_VisivelPara_SegueAMatrizDeAutorizacao(Papel papel, string id, bool esperado)
    {
        Assert.That(Modulos.Todos.First(m => m.Id == id).VisivelPara(papel), Is.EqualTo(esperado));
    }

    [Test]
    public void CabecalhosPadrao_NaoTemNomeRepetido()
    {
        Assert.That(CabecalhosDeSeguranca.Padrao.Select(c => c.Nome).Distinct().Count(), Is.EqualTo(CabecalhosDeSeguranca.Padrao.Length));
    }
}
