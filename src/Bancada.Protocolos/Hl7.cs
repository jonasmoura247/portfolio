using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Bancada.Protocolos;

/// <summary>Mensagens HL7 v2 simplificadas: ORM^O01 (pedido) e ORU^R01 (resultado). Segmentos separados por CR.</summary>
public static class Hl7
{
    public static IReadOnlyList<string> Pedido(string equipamento, string pedido, string paciente, string nascimento, string sexo, string exame, string quando, int mensagem) =>
    [
        $"MSH|^~\\&|BANCADA|LAB|{equipamento}|LAB|{quando}||ORM^O01|MSG{mensagem}|P|2.5.1",
        $"PID|1||{paciente}||||{nascimento}|{sexo}",
        $"ORC|NW|{pedido}",
        $"OBR|1|{pedido}||{exame}",
    ];

    public static string Observacao(int sequencia, string analito, double valor, string unidade, string flag) =>
        string.Create(CultureInfo.InvariantCulture, $"OBX|{sequencia}|NM|{analito}||{valor}|{unidade}|||{flag}|||F");

    public static IReadOnlyList<string> Resultado(string equipamento, string pedido, string exame, string quando, int mensagem, IEnumerable<string> observacoes) =>
    [
        $"MSH|^~\\&|{equipamento}|LAB|BANCADA|LAB|{quando}||ORU^R01|MSG{mensagem}|P|2.5.1",
        $"OBR|1|{pedido}||{exame}",
        .. observacoes,
    ];

    public static IReadOnlyList<(string Analito, double Valor)> LerResultados(IEnumerable<string> segmentos)
    {
        var lista = segmentos.ToList();
        if (lista.Count == 0 || !lista[0].StartsWith("MSH|", StringComparison.Ordinal) || !lista[0].Contains("ORU^R01", StringComparison.Ordinal))
        {
            throw new FormatException("Mensagem não é um ORU^R01.");
        }

        var lidos = new List<(string, double)>();
        foreach (var obx in lista.Where(s => s.StartsWith("OBX|", StringComparison.Ordinal)))
        {
            var campos = obx.Split('|');
            if (campos.Length < 6 || !double.TryParse(campos[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var valor))
            {
                throw new FormatException("Segmento OBX inválido.");
            }

            lidos.Add((campos[3], valor));
        }

        return lidos;
    }
}
