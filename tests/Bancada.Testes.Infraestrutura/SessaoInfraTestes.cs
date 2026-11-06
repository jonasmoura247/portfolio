using System;
using System.Linq;
using System.Security.Cryptography;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Bancada.Infraestrutura;
using Bancada.Infraestrutura.Dados;

namespace Bancada.Testes.Infraestrutura;

public class SessaoInfraTestes
{
    private static readonly ModeloDoBanco Modelo = new();

    [Test]
    public void GeradorDeId_TemQuarentaETresCaracteresUrlSafe()
    {
        var id = new GeradorDeId().Novo();

        Assert.That(id, Has.Length.EqualTo(43));
        Assert.That(id, Does.Match("^[A-Za-z0-9_-]+$"));
    }

    [Test]
    public void GeradorDeId_MilIds_NaoRepetem()
    {
        var gerador = new GeradorDeId();

        Assert.That(Enumerable.Range(0, 1000).Select(_ => gerador.Novo()).Distinct().Count(), Is.EqualTo(1000));
    }

    [Test]
    public void Base64Url_TroqueiaMaisBarraETiraPreenchimento()
    {
        Assert.That(GeradorDeId.Base64Url([0xfb, 0xff, 0xfe]), Is.EqualTo("-__-"));
        Assert.That(GeradorDeId.Base64Url([1]), Is.EqualTo("AQ"));
    }

    [Test]
    public void FabricaDeSessao_CriaComOPapelPedidoOperadorValidoECifraPropria()
    {
        var agora = new DateTimeOffset(2026, 11, 4, 13, 0, 0, TimeSpan.Zero);
        using var a = new FabricaDeSessao(Modelo, Papel.Recepcao).Criar("id-a", agora, Cenarios.Padrao);
        using var b = new FabricaDeSessao(Modelo, Papel.Recepcao).Criar("id-b", agora, Cenarios.Padrao);

        Assert.That(a.Papel, Is.EqualTo(Papel.Recepcao));
        Assert.That(Operadores.Pertence(a.Operador, a.Papel), Is.True);
        Assert.That(a.CriadaEm, Is.EqualTo(agora));
        var valor = a.Cifra.Cifrar("segredo", "x");
        Assert.That(a.Cifra.Decifrar(valor, "x"), Is.EqualTo("segredo"));
        Assert.That(() => b.Cifra.Decifrar(valor, "x"), Throws.InstanceOf<CryptographicException>(), "chave de outra sessão não decifra");
    }

    [Test]
    public void FabricaDeSessao_SemPapelPedido_ComecaComoAdmin()
    {
        using var s = new FabricaDeSessao(Modelo).Criar("id-admin", DateTimeOffset.UnixEpoch, Cenarios.Padrao);

        Assert.That(s.Papel, Is.EqualTo(Papel.Admin));
        Assert.That(Operadores.Pertence(s.Operador, Papel.Admin), Is.True);
    }

    [Test]
    public void FabricaDeSessao_AoDescartarASessao_AChaveEZerada()
    {
        var sessao = new FabricaDeSessao(Modelo).Criar("id", DateTimeOffset.UnixEpoch, "cq");
        var valor = sessao.Cifra.Cifrar("x", "x");

        sessao.Dispose();

        Assert.That(() => sessao.Cifra.Decifrar(valor, "x"), Throws.TypeOf<ObjectDisposedException>());
    }
}
