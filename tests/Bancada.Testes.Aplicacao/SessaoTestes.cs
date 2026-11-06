using System;
using Bancada.Aplicacao;
using Bancada.Dominio;

namespace Bancada.Testes.Aplicacao;

public class SessaoTestes
{
    [Test]
    public void Dispose_DescartaCadaRecursoUmaSoVezMesmoSeChamadoDuasVezes()
    {
        var a = new RecursoRastreado();
        var b = new RecursoRastreado();
        var sessao = new Sessao("x", Papel.Medico, "MED-01", DateTimeOffset.UnixEpoch, new CifraFalsa(), [a, b]);

        sessao.Dispose();
        sessao.Dispose();

        Assert.That(sessao.Descartada, Is.True);
        Assert.That(a.Descartes, Is.EqualTo(1));
        Assert.That(b.Descartes, Is.EqualTo(1));
    }

    [Test]
    public void Tocar_AtualizaOUltimoAcessoSemMexerNaCriacao()
    {
        var inicio = new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.Zero);
        var sessao = new Sessao("x", Papel.Medico, "MED-01", inicio, new CifraFalsa(), []);

        sessao.Tocar(inicio.AddMinutes(5));

        Assert.That(sessao.UltimoAcesso, Is.EqualTo(inicio.AddMinutes(5)));
        Assert.That(sessao.CriadaEm, Is.EqualTo(inicio));
    }

    [Test]
    public void Cifra_EAMesmaInjetadaNaCriacao()
    {
        var cifra = new CifraFalsa();
        var sessao = new Sessao("x", Papel.Coleta, "COL-01", DateTimeOffset.UnixEpoch, cifra, []);

        Assert.That(sessao.Cifra, Is.SameAs(cifra));
    }

    [Test]
    public void CifraFalsa_IdaEVolta()
    {
        var cifra = new CifraFalsa();

        Assert.That(cifra.Decifrar(cifra.Cifrar("oi", "aad"), "aad"), Is.EqualTo("oi"));
    }

    [Test]
    public void Aviso_GuardaAChaveAteAProximaPagina()
    {
        using var s = new FabricaDeTeste().Criar("s", DateTimeOffset.UnixEpoch, Cenarios.Padrao);
        s.Aviso = ("a.b", true);

        Assert.That(s.Aviso, Is.EqualTo(("a.b", true)));
    }

    [Test]
    public void ModeloDoBanco_Descarta()
    {
        new Bancada.Infraestrutura.Dados.ModeloDoBanco().Dispose();
        Assert.Pass();
    }
}
