using System;
using System.Collections.Generic;
using System.Linq;
using Bancada.Protocolos;

namespace Bancada.Testes.Protocolos;

public class AnaliseTestes
{
    [TestCase(3.5, "N")]
    [TestCase(5.1, "N")]
    [TestCase(5.2, "H")]
    [TestCase(3.4, "L")]
    [TestCase(6.5, "H")]
    [TestCase(6.6, "CH")]
    [TestCase(2.5, "L")]
    [TestCase(2.4, "CL")]
    public void RN_051_Flags_FaixaFechada_RespeitaOsLimites(double valor, string flag)
    {
        Assert.That(Analise.Classificar(valor, 3.5, 5.1, 2.5, 6.5), Is.EqualTo(flag));
    }

    [Test]
    public void RN_051_Flags_FaixaAberta_SoOLadoDefinidoConta()
    {
        Assert.That(Analise.Classificar(10, null, 190, null, null), Is.EqualTo("N"));
        Assert.That(Analise.Classificar(200, null, 190, null, null), Is.EqualTo("H"));
        Assert.That(Analise.Classificar(0.1, 0.4, null, null, null), Is.EqualTo("L"));
        Assert.That(Analise.Classificar(99, 0.4, null, null, null), Is.EqualTo("N"));
    }

    [TestCase(7.1, 4.4, 25, true)]
    [TestCase(1.4, 1.2, 40, false)]
    [TestCase(5.0, null, 25, false)]
    [TestCase(5.0, 0.0, 25, false)]
    public void DeltaSuspeito_ComparaComOAnterior(double valor, double? anterior, double pct, bool esperado)
    {
        Assert.That(Analise.DeltaSuspeito(valor, anterior, pct), Is.EqualTo(esperado));
    }

    private static (IReadOnlyList<string> Regras, bool SoAviso) W(params double[] z)
    {
        var r = Analise.Westgard(z, out var aviso);
        return (r, aviso);
    }

    [Test]
    public void RN_044_Westgard_SerieVazia_SemViolacao()
    {
        Assert.That(W().Regras, Is.Empty);
    }

    [Test]
    public void RN_044_Westgard_MedidasNormais_SemViolacao()
    {
        Assert.That(W(0.1, -0.5, 0.3).Regras, Is.Empty);
    }

    [Test]
    public void RN_044_Westgard_UmaAlemDeDois_SoAviso()
    {
        var (regras, aviso) = W(0.1, 2.3);

        Assert.That(regras, Is.EqualTo(new[] { "1-2s" }));
        Assert.That(aviso, Is.True);
    }

    [TestCase("1-3s", 0.0, 3.2)]
    [TestCase("2-2s", 0.0, 2.4, 2.2)]
    [TestCase("2-2s", 0.0, -2.4, -2.2)]
    [TestCase("R-4s", 0.0, 2.4, -2.2)]
    [TestCase("4-1s", 0.0, 1.2, 1.5, 1.1, 1.3)]
    [TestCase("4-1s", 0.0, -1.2, -1.5, -1.1, -1.3)]
    public void RN_044_Westgard_CadaRegra_Reprova(string regra, params double[] z)
    {
        var (regras, aviso) = W(z);

        Assert.That(regras, Does.Contain(regra));
        Assert.That(aviso, Is.False);
    }

    [Test]
    public void RN_044_Westgard_DezDoMesmoLado_Reprova()
    {
        Assert.That(W(Enumerable.Repeat(0.3, 10).ToArray()).Regras, Does.Contain("10x"));
        Assert.That(W(Enumerable.Repeat(-0.3, 10).ToArray()).Regras, Does.Contain("10x"));
        Assert.That(W(Enumerable.Repeat(0.3, 9).ToArray()).Regras, Is.Empty);
    }

    [Test]
    public void RN_044_Westgard_MedidaUnicaAlta_SerieCurtaNaoQuebra()
    {
        Assert.That(W(2.5).Regras, Is.EqualTo(new[] { "1-2s" }));
        Assert.That(W(0.2).Regras, Is.Empty);
    }

    [Test]
    public void RN_044_Westgard_PenultimaAltaMasUltimaNormal_NaoReprova()
    {
        Assert.That(W(2.5, 0.1).Regras, Is.Empty);
        Assert.That(W(0.1, 2.5, 0.1).Regras, Is.Empty);
    }
}

public class AstmTestes
{
    [Test]
    public void Quadro_MontaEValida()
    {
        var q = Astm.Quadro(1, "L|1|N");

        Assert.That(q[0], Is.EqualTo(Astm.Stx));
        Assert.That(Astm.Ler(q), Is.EqualTo((1, "L|1|N", true)));
    }

    [Test]
    public void Checksum_ValorCalculadoAMao()
    {
        // '1'=49, 'A'=65, ETX=3 -> 117 = 0x75
        Assert.That(Astm.Checksum("1A\x03"), Is.EqualTo("75"));
    }

    [TestCase("")]
    [TestCase("lixo")]
    [TestCase("\x02" + "1A\x03" + "75")]
    public void Ler_Malformado_LancaErro(string quadro)
    {
        Assert.That(() => Astm.Ler(quadro), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void Ler_SemStx_LancaErro()
    {
        Assert.That(() => Astm.Ler("X1A\x03" + "75\r\n"), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void Ler_ChecksumErrado_LancaErro()
    {
        Assert.That(() => Astm.Ler("\x02" + "1A\x03" + "00\r\n"), Throws.TypeOf<FormatException>().With.Message.Contain("Checksum"));
    }

    [Test]
    public void Ler_SemEtx_LancaErro()
    {
        Assert.That(() => Astm.Ler("\x02" + "1AB75\r\n"), Throws.TypeOf<FormatException>().With.Message.Contain("ETX"));
    }

    [TestCase('8')]
    [TestCase('X')]
    public void Ler_NumeroInvalido_LancaErro(char numero)
    {
        Assert.That(() => Astm.Ler("\x02" + numero + "A\x03" + "00\r\n"), Throws.TypeOf<FormatException>().With.Message.Contain("Número"));
    }

    [Test]
    public void Quadros_RegistroLongo_DivideEmEtbERemonta()
    {
        var longo = new string('x', 500);

        var quadros = Astm.Quadros([longo, "L|1|N"]);

        Assert.That(quadros, Has.Count.EqualTo(4));
        Assert.That(Astm.Ler(quadros[0]).Ultimo, Is.False);
        Assert.That(Astm.Registros(quadros), Is.EqualTo(new[] { longo, "L|1|N" }));
    }

    [Test]
    public void Quadros_NumeracaoRodaDeSeteParaZero()
    {
        var registros = Enumerable.Range(0, 9).Select(i => "R" + i).ToList();

        var quadros = Astm.Quadros(registros);

        Assert.That(quadros.Select(q => Astm.Ler(q).Numero), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 0, 1 }));
        Assert.That(Astm.Registros(quadros), Is.EqualTo(registros));
    }

    [Test]
    public void Registros_QuadroRepetido_EIgnorado()
    {
        var q = Astm.Quadros(["A", "B"]);

        Assert.That(Astm.Registros([q[0], q[0], q[1]]), Is.EqualTo(new[] { "A", "B" }));
    }

    [Test]
    public void Registros_ForaDeOrdem_LancaErro()
    {
        var q = Astm.Quadros(["A", "B", "C"]);

        Assert.That(() => Astm.Registros([q[0], q[2]]), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void Registros_CortadoNoMeioDoRegistro_LancaErro()
    {
        var q = Astm.Quadros([new string('x', 300)]);

        Assert.That(() => Astm.Registros([q[0]]), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void Resultado_IdaEVolta()
    {
        var registros = new[]
        {
            Astm.Cabecalho("Bancada", "20261104"), Astm.Paciente("PAC-0014", "19740312", "M"), Astm.Pedido("AMO-000123", "ELE"),
            Astm.Resultado(1, "POT", 7.1, "mmol/L", "H"), Astm.Resultado(2, "SOD", 138, "mmol/L", "N"), Astm.Fim(),
        };

        var lidos = Astm.LerResultados(Astm.Registros(Astm.Quadros(registros)));

        Assert.That(lidos, Is.EqualTo(new[] { ("POT", 7.1), ("SOD", 138.0) }));
    }

    [TestCase("R|1")]
    [TestCase("R|1|^^^POT|abc|mmol/L")]
    [TestCase("R|1|^^^POT|NaN|mmol/L")]
    [TestCase("R|1|^^^POT|Infinity|mmol/L")]
    [TestCase("R|1|^^^POT|-Infinity|mmol/L")]
    [TestCase("R|1|^^^POT|1e999|mmol/L")]
    public void LerResultados_RegistroInvalido_LancaErro(string registro)
    {
        Assert.That(() => Astm.LerResultados([registro]), Throws.TypeOf<FormatException>());
    }
}

public class Hl7Testes
{
    [Test]
    public void Pedido_TemOsSegmentosDoOrm()
    {
        var m = Hl7.Pedido("EQHEM01", "PED-1", "PAC-1", "19800101", "F", "HEM", "20261104", 7);

        Assert.That(m.Select(s => s[..3]), Is.EqualTo(new[] { "MSH", "PID", "ORC", "OBR" }));
        Assert.That(m[0], Does.Contain("ORM^O01").And.Contain("MSG7"));
    }

    [Test]
    public void Resultado_IdaEVolta()
    {
        var m = Hl7.Resultado("EQHEM01", "PED-1", "HEM", "20261104", 8, [Hl7.Observacao(1, "HGB", 13.2, "g/dL", "N"), Hl7.Observacao(2, "LEU", 5400, "/uL", "N")]);

        Assert.That(Hl7.LerResultados(m), Is.EqualTo(new[] { ("HGB", 13.2), ("LEU", 5400.0) }));
    }

    [Test]
    public void LerResultados_NaoEOru_LancaErro()
    {
        Assert.That(() => Hl7.LerResultados([]), Throws.TypeOf<FormatException>());
        Assert.That(() => Hl7.LerResultados(["PID|1"]), Throws.TypeOf<FormatException>());
        Assert.That(() => Hl7.LerResultados(["MSH|x|ORM^O01"]), Throws.TypeOf<FormatException>());
    }

    [TestCase("OBX|1")]
    [TestCase("OBX|1|NM|POT||abc")]
    [TestCase("OBX|1|NM|POT||NaN")]
    [TestCase("OBX|1|NM|POT||Infinity")]
    [TestCase("OBX|1|NM|POT||1e999")]
    public void LerResultados_ObxInvalido_LancaErro(string obx)
    {
        Assert.That(() => Hl7.LerResultados(["MSH|x|ORU^R01", obx]), Throws.TypeOf<FormatException>());
    }
}

/// <summary>
/// SEC-08: entrada hostil nos leitores de protocolo. Milhares de quadros e mensagens aleatórios ou mutados (semente fixa,
/// repetível): o único erro aceito é FormatException, nada de IndexOutOfRange, laço sem fim ou valor não finito aceito.
/// </summary>
public class ProtocolosHostisTestes
{
    private const string Alfabeto = "|^\\&~ RHOPL0123456789.-+eE\x02\x03\x17\r\nNaInfityé\u0000￿";

    private static string Aleatorio(Random r, int maximo) => new([.. Enumerable.Range(0, r.Next(maximo)).Select(_ => Alfabeto[r.Next(Alfabeto.Length)])]);

    private static void SoFormatException(Action ler)
    {
        try
        {
            ler();
        }
        catch (FormatException)
        {
        }
    }

    [Test]
    public void Astm_QuadrosAleatoriosEMutados_SoRecusaComFormatException()
    {
        var r = new Random(20261107);
        var validos = Astm.Quadros([Astm.Cabecalho("EQ", "20261107"), Astm.Resultado(1, "POT", 4.2, "mmol/L", "N"), Astm.Fim()]);
        for (var i = 0; i < 5000; i++)
        {
            var mutado = validos.Select(q => r.Next(4) == 0 ? q.Insert(r.Next(q.Length), Aleatorio(r, 6)) : q).ToList();
            var lixo = Enumerable.Range(0, r.Next(1, 4)).Select(_ => Aleatorio(r, 300)).ToList();

            Assert.DoesNotThrow(() => SoFormatException(() => Astm.Registros(mutado)), $"mutado {i}");
            Assert.DoesNotThrow(() => SoFormatException(() => Astm.Registros(lixo)), $"lixo {i}");
            Assert.DoesNotThrow(() => SoFormatException(() => Astm.LerResultados(lixo)), $"registros {i}");
        }
    }

    [Test]
    public void Hl7_SegmentosAleatorios_SoRecusaComFormatExceptionENuncaValorNaoFinito()
    {
        var r = new Random(20261107);
        for (var i = 0; i < 5000; i++)
        {
            var segmentos = new List<string> { "MSH|^~\\&|EQ|LAB|BANCADA|LAB|1||ORU^R01|MSG1|P|2.5.1" };
            segmentos.AddRange(Enumerable.Range(0, r.Next(1, 5)).Select(_ => "OBX|" + Aleatorio(r, 80)));
            IReadOnlyList<(string, double Valor)> lidos = [];

            Assert.DoesNotThrow(() => SoFormatException(() => lidos = Hl7.LerResultados(segmentos)), $"mensagem {i}");
            Assert.That(lidos.All(x => double.IsFinite(x.Valor)), $"mensagem {i}");
        }
    }

    [Test]
    public void Astm_TransmissaoGrande_RemontaSemCrescerSemLimite()
    {
        var registros = Enumerable.Range(1, 2000).Select(i => Astm.Resultado(i, "POT", 4.2, "mmol/L", "N")).ToList();

        var lidos = Astm.LerResultados(Astm.Registros(Astm.Quadros(registros)));

        Assert.That(lidos, Has.Count.EqualTo(2000));
    }
}
