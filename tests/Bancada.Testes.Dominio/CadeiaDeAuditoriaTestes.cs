using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bancada.Dominio;

namespace Bancada.Testes.Dominio;

public class CadeiaDeAuditoriaTestes
{
    private static EventoDeAuditoria Bruto(int seq, string acao = "PedidoCriado", string alvoId = "PED-000045") => new()
    {
        Seq = seq,
        Quando = new DateTimeOffset(2026, 11, 4, 13, 15, 0, TimeSpan.Zero).AddSeconds(seq),
        Papel = "Medico",
        Operador = "MED-01",
        Acao = acao,
        AlvoTipo = "Pedido",
        AlvoId = alvoId,
        DetalheJson = "{\"exames\":3}",
        Resultado = "Ok",
    };

    private static List<EventoDeAuditoria> Cadeia(int n)
    {
        var lista = new List<EventoDeAuditoria>();
        var anterior = CadeiaDeAuditoria.HashGenesis;
        for (var i = 1; i <= n; i++)
        {
            var e = CadeiaDeAuditoria.Encadear(anterior, Bruto(i));
            lista.Add(e);
            anterior = e.Hash;
        }

        return lista;
    }

    private static EventoDeAuditoria Copia(EventoDeAuditoria e, string? papel = null, string? operador = null, string? acao = null, string? alvoTipo = null,
        string? alvoId = null, string? detalhe = null, string? resultado = null, DateTimeOffset? quando = null, int? seq = null) => new()
        {
            Seq = seq ?? e.Seq,
            Quando = quando ?? e.Quando,
            Papel = papel ?? e.Papel,
            Operador = operador ?? e.Operador,
            Acao = acao ?? e.Acao,
            AlvoTipo = alvoTipo ?? e.AlvoTipo,
            AlvoId = alvoId ?? e.AlvoId,
            DetalheJson = detalhe ?? e.DetalheJson,
            Resultado = resultado ?? e.Resultado,
            HashAnterior = e.HashAnterior,
            Hash = e.Hash,
        };

    [Test]
    public void HashGenesis_ESha256DoTextoFixo()
    {
        var esperado = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("BANCADA-GENESIS"))).ToLowerInvariant();

        Assert.That(CadeiaDeAuditoria.HashGenesis, Is.EqualTo(esperado));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(1000)]
    public void PrimeiroEloQuebrado_CadeiaIntegra_DevolveNulo(int tamanho)
    {
        Assert.That(CadeiaDeAuditoria.PrimeiroEloQuebrado(Cadeia(tamanho)), Is.Null);
    }

    [Test]
    public void Encadear_LigaAoHashAnteriorEPreencheOHash()
    {
        var e = CadeiaDeAuditoria.Encadear(CadeiaDeAuditoria.HashGenesis, Bruto(1));

        Assert.That(e.HashAnterior, Is.EqualTo(CadeiaDeAuditoria.HashGenesis));
        Assert.That(e.Hash, Has.Length.EqualTo(64));
        Assert.That(e.Hash, Is.Not.EqualTo(e.HashAnterior));
    }

    [Test]
    public void PrimeiroEloQuebrado_AlterarQualquerCampoDeUmEventoDoMeio_AcusaOElo()
    {
        var alteracoes = new Func<EventoDeAuditoria, EventoDeAuditoria>[]
        {
            e => Copia(e, papel: "Admin"), e => Copia(e, operador: "MED-02"), e => Copia(e, acao: "PedidoCancelado"),
            e => Copia(e, alvoTipo: "Amostra"), e => Copia(e, alvoId: "PED-000046"), e => Copia(e, detalhe: "{}"),
            e => Copia(e, resultado: "Recusado"), e => Copia(e, quando: e.Quando.AddSeconds(1)), e => Copia(e, seq: 99),
        };

        foreach (var altera in alteracoes)
        {
            var cadeia = Cadeia(6);
            var original = cadeia[2];
            cadeia[2] = altera(original);

            var quebrado = CadeiaDeAuditoria.PrimeiroEloQuebrado(cadeia);

            Assert.That(quebrado, Is.EqualTo(cadeia[2].Seq), "elo adulterado deve ser o primeiro a quebrar");
        }
    }

    [Test]
    public void PrimeiroEloQuebrado_RemoverEvento_Acusa()
    {
        var cadeia = Cadeia(6);
        cadeia.RemoveAt(3);

        Assert.That(CadeiaDeAuditoria.PrimeiroEloQuebrado(cadeia), Is.EqualTo(5));
    }

    [Test]
    public void PrimeiroEloQuebrado_TrocarOrdem_Acusa()
    {
        var cadeia = Cadeia(6);
        (cadeia[1], cadeia[2]) = (cadeia[2], cadeia[1]);

        Assert.That(CadeiaDeAuditoria.PrimeiroEloQuebrado(cadeia), Is.EqualTo(3));
    }

    [Test]
    public void PrimeiroEloQuebrado_HashTrocadoSemRecalcular_Acusa()
    {
        var cadeia = Cadeia(3);
        var e = cadeia[1];
        cadeia[1] = new EventoDeAuditoria
        {
            Seq = e.Seq,
            Quando = e.Quando,
            Papel = e.Papel,
            Operador = e.Operador,
            Acao = e.Acao,
            AlvoTipo = e.AlvoTipo,
            AlvoId = e.AlvoId,
            DetalheJson = e.DetalheJson,
            Resultado = e.Resultado,
            HashAnterior = e.HashAnterior,
            Hash = new string('0', 64),
        };

        Assert.That(CadeiaDeAuditoria.PrimeiroEloQuebrado(cadeia), Is.EqualTo(2));
    }

    [Test]
    public void Canonico_MesmoEventoEmQualquerCultura_ProduzMesmoTexto()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            var ptBr = CadeiaDeAuditoria.Canonico(Bruto(7));
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            var enUs = CadeiaDeAuditoria.Canonico(Bruto(7));

            Assert.That(ptBr, Is.EqualTo(enUs));
            Assert.That(ptBr, Does.StartWith("7|2026-11-04T13:15:07.000Z|Medico|MED-01|"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Test]
    public void Canonico_BarraEBarraInvertidaNoCampo_NaoColidemComOSeparador()
    {
        var a = CadeiaDeAuditoria.Canonico(Copia(Bruto(1), acao: "a|b", alvoTipo: "c"));
        var b = CadeiaDeAuditoria.Canonico(Copia(Bruto(1), acao: "a", alvoTipo: "b|c"));
        var c = CadeiaDeAuditoria.Canonico(Copia(Bruto(1), acao: "a\\", alvoTipo: "|b"));
        var d = CadeiaDeAuditoria.Canonico(Copia(Bruto(1), acao: "a", alvoTipo: "\\|b"));

        Assert.That(a, Is.Not.EqualTo(b));
        Assert.That(c, Is.Not.EqualTo(d));
        Assert.That(a, Does.Contain("a\\|b"));
    }
}
