using System;
using System.Linq;
using System.Security.Cryptography;
using Bancada.Infraestrutura;

namespace Bancada.Testes.Infraestrutura;

public class CifraAesGcmTestes
{
    private const string Aad = "evolucao:texto_cifrado:42";

    [TestCase("")]
    [TestCase("a")]
    [TestCase("Paciente com cansaço há duas semanas. <script>alert(1)</script> 🙂")]
    public void Decifrar_DepoisDeCifrar_VoltaAoOriginal(string texto)
    {
        using var cifra = new CifraAesGcm(CifraAesGcm.NovaChave());

        Assert.That(cifra.Decifrar(cifra.Cifrar(texto, Aad), Aad), Is.EqualTo(texto));
    }

    [Test]
    public void Decifrar_TextoDeDoisMilCaracteres_Funciona()
    {
        using var cifra = new CifraAesGcm(CifraAesGcm.NovaChave());
        var texto = new string('x', 2000);

        Assert.That(cifra.Decifrar(cifra.Cifrar(texto, Aad), Aad), Is.EqualTo(texto));
    }

    [Test]
    public void Cifrar_MesmoTextoDuasVezes_ProduzValoresDiferentes()
    {
        using var cifra = new CifraAesGcm(CifraAesGcm.NovaChave());

        var primeiro = cifra.Cifrar("igual", Aad);
        var segundo = cifra.Cifrar("igual", Aad);

        Assert.That(primeiro, Is.Not.EqualTo(segundo));
    }

    [Test]
    public void Cifrar_ValorGuardado_NaoContemOTextoOriginal()
    {
        using var cifra = new CifraAesGcm(CifraAesGcm.NovaChave());

        var valor = cifra.Cifrar("texto legivel secreto", Aad);

        Assert.That(valor, Does.Not.Contain("legivel"));
        Assert.That(Convert.FromBase64String(valor)[0], Is.EqualTo(1), "byte de versão");
    }

    [Test]
    public void Decifrar_QualquerByteAlterado_Falha()
    {
        using var cifra = new CifraAesGcm(CifraAesGcm.NovaChave());
        var original = Convert.FromBase64String(cifra.Cifrar("conteudo", Aad));

        for (var i = 1; i < original.Length; i++) // o byte 0 é a versão, tratado em outro teste
        {
            var adulterado = (byte[])original.Clone();
            adulterado[i] ^= 0x01;

            Assert.That(() => cifra.Decifrar(Convert.ToBase64String(adulterado), Aad), Throws.InstanceOf<CryptographicException>(), $"byte {i}");
        }
    }

    [Test]
    public void Decifrar_DadosAssociadosDeOutroRegistro_Falha()
    {
        using var cifra = new CifraAesGcm(CifraAesGcm.NovaChave());
        var valor = cifra.Cifrar("conteudo", Aad);

        Assert.That(() => cifra.Decifrar(valor, "evolucao:texto_cifrado:43"), Throws.InstanceOf<CryptographicException>());
    }

    [Test]
    public void Decifrar_ChaveDeOutraSessao_Falha()
    {
        using var minha = new CifraAesGcm(CifraAesGcm.NovaChave());
        using var outra = new CifraAesGcm(CifraAesGcm.NovaChave());

        Assert.That(() => outra.Decifrar(minha.Cifrar("conteudo", Aad), Aad), Throws.InstanceOf<CryptographicException>());
    }

    [Test]
    public void Decifrar_VersaoDesconhecidaOuCurtoDemais_LancaFormatException()
    {
        using var cifra = new CifraAesGcm(CifraAesGcm.NovaChave());
        var bytes = Convert.FromBase64String(cifra.Cifrar("x", Aad));
        bytes[0] = 2;

        Assert.That(() => cifra.Decifrar(Convert.ToBase64String(bytes), Aad), Throws.TypeOf<FormatException>());
        Assert.That(() => cifra.Decifrar(Convert.ToBase64String(new byte[10]), Aad), Throws.TypeOf<FormatException>());
        Assert.That(() => cifra.Decifrar("isto não é base64", Aad), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void Construtor_ChaveInvalida_LancaErro()
    {
        Assert.That(() => new CifraAesGcm(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new CifraAesGcm(new byte[16]), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Argumentos_Nulos_LancamErro()
    {
        using var cifra = new CifraAesGcm(CifraAesGcm.NovaChave());

        Assert.That(() => cifra.Cifrar(null!, Aad), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => cifra.Cifrar("x", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => cifra.Decifrar(null!, Aad), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => cifra.Decifrar("x", null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Dispose_ZeraAChaveEImpedeUsoDepois()
    {
        var chave = CifraAesGcm.NovaChave();
        Assert.That(chave.Any(b => b != 0), Is.True);
        var cifra = new CifraAesGcm(chave);
        var valor = cifra.Cifrar("x", Aad);

        cifra.Dispose();
        cifra.Dispose(); // idempotente

        Assert.That(chave.All(b => b == 0), Is.True, "chave zerada");
        Assert.That(() => cifra.Cifrar("x", Aad), Throws.TypeOf<ObjectDisposedException>());
        Assert.That(() => cifra.Decifrar(valor, Aad), Throws.TypeOf<ObjectDisposedException>());
    }

    [Test]
    public void NovaChave_TemTrintaEDoisBytesAleatorios()
    {
        var a = CifraAesGcm.NovaChave();
        var b = CifraAesGcm.NovaChave();

        Assert.That(a, Has.Length.EqualTo(CifraAesGcm.TamanhoDaChave));
        Assert.That(a, Is.Not.EqualTo(b));
    }
}
