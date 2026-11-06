using System;
using System.Linq;
using System.Text;
using Bancada.Protocolos;

namespace Bancada.Testes.Protocolos;

public class Code128BTestes
{
    private const string InicioB = "211214";
    private const string Parada = "2331112";

    [Test]
    public void Codificar_Texto_ComecaNoInicioBETerminaNaParada()
    {
        var larguras = Code128B.Codificar("AMO-000123");

        Assert.That(larguras, Does.StartWith(InicioB));
        Assert.That(larguras, Does.EndWith(Parada));
    }

    [Test]
    public void Codificar_Texto_UsaOnzeModulosPorSimboloETrezeNaParada()
    {
        var texto = "PED-000045";

        var larguras = Code128B.Codificar(texto);

        var simbolos = texto.Length + 2; // início + dígito verificador; a parada é contada à parte
        Assert.That(larguras.Sum(d => d - '0'), Is.EqualTo((11 * simbolos) + 13));
        Assert.That(larguras, Has.Length.EqualTo((6 * simbolos) + 7));
    }

    [Test]
    public void Codificar_AMO_DigitoVerificadorBateComCalculoManual()
    {
        // (104 + 1*33 + 2*45 + 3*47) % 103 = 59, cujo padrão é 332111
        var larguras = Code128B.Codificar("AMO");

        Assert.That(larguras.Substring(6 + 18, 6), Is.EqualTo("332111"));
    }

    [Test]
    public void Decodificar_Codificado_VoltaAoOriginal()
    {
        foreach (var texto in new[] { "AMO-000123", "A", " ", "~", "PED-000045", "Bancada 128" })
        {
            Assert.That(Code128B.Decodificar(Code128B.Codificar(texto)), Is.EqualTo(texto));
        }
    }

    [Test]
    public void Decodificar_TodoAsciiImprimivel_IdaEVolta()
    {
        var todos = new StringBuilder();
        for (var c = ' '; c <= '~'; c++)
        {
            todos.Append(c);
        }

        Assert.That(Code128B.Decodificar(Code128B.Codificar(todos.ToString())), Is.EqualTo(todos.ToString()));
    }

    [Test]
    public void Codificar_NuloOuVazio_LancaErro()
    {
        Assert.That(() => Code128B.Codificar(null!), Throws.InstanceOf<ArgumentException>());
        Assert.That(() => Code128B.Codificar(string.Empty), Throws.InstanceOf<ArgumentException>());
    }

    [TestCase("ção")]
    [TestCase("a\tb")]
    public void Codificar_ForaDoAsciiImprimivel_LancaErro(string texto)
    {
        Assert.That(() => Code128B.Codificar(texto), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Decodificar_Vazio_LancaErro()
    {
        Assert.That(() => Code128B.Decodificar(string.Empty), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Decodificar_SimboloInexistente_LancaErro()
    {
        Assert.That(() => Code128B.Decodificar("999999"), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void Decodificar_CortadoNoMeio_LancaErro()
    {
        var cortado = Code128B.Codificar("AMO")[..^3];

        Assert.That(() => Code128B.Decodificar(cortado), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void Decodificar_SoInicioEParada_LancaErro()
    {
        Assert.That(() => Code128B.Decodificar(InicioB + Parada), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void Decodificar_SemInicioB_LancaErro()
    {
        Assert.That(() => Code128B.Decodificar("212222212222212222" + Parada), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void Decodificar_SemParada_LancaErro()
    {
        Assert.That(() => Code128B.Decodificar(InicioB + "212222212222212222"), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void Decodificar_DigitoVerificadorAdulterado_LancaErro()
    {
        var larguras = Code128B.Codificar("AMO");
        var antes = larguras[..(6 + 18)];
        var adulterado = antes + "212222" + Parada; // valor 0 no lugar do verificador 59

        Assert.That(() => Code128B.Decodificar(adulterado), Throws.TypeOf<FormatException>().With.Message.Contain("verificador"));
    }
}
