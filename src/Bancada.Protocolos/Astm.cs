using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Bancada.Protocolos;

/// <summary>
/// Quadros no estilo ASTM E1381 (STX, número, texto, ETX/ETB, checksum, CR LF) e registros E1394 (H, P, O, R, L).
/// O checksum é a soma dos bytes do número do quadro até o ETX/ETB, módulo 256, em dois dígitos hexadecimais.
/// </summary>
public static class Astm
{
    public const char Stx = '\x02';
    public const char Etx = '\x03';
    public const char Etb = '\x17';
    public const int TextoMaximo = 240;

    public static string Checksum(string corpo) =>
        (corpo.Sum(c => (int)c) % 256).ToString("X2", CultureInfo.InvariantCulture);

    public static string Quadro(int numero, string texto, bool ultimo = true)
    {
        var corpo = (numero % 8).ToString(CultureInfo.InvariantCulture) + texto + (ultimo ? Etx : Etb);
        return Stx + corpo + Checksum(corpo) + "\r\n";
    }

    /// <summary>Quebra os registros em quadros numerados de 1 a 7 e depois 0; registro maior que 240 caracteres continua em ETB.</summary>
    public static IReadOnlyList<string> Quadros(IEnumerable<string> registros)
    {
        var quadros = new List<string>();
        foreach (var registro in registros)
        {
            for (var i = 0; i < registro.Length; i += TextoMaximo)
            {
                var ultimo = i + TextoMaximo >= registro.Length;
                quadros.Add(Quadro(quadros.Count + 1, registro.Substring(i, Math.Min(TextoMaximo, registro.Length - i)), ultimo));
            }
        }

        return quadros;
    }

    /// <summary>Valida um quadro e devolve número, texto e se fecha o registro. Quadro inválido lança FormatException.</summary>
    public static (int Numero, string Texto, bool Ultimo) Ler(string quadro)
    {
        if (quadro.Length < 7 || quadro[0] != Stx || !quadro.EndsWith("\r\n", StringComparison.Ordinal))
        {
            throw new FormatException("Quadro ASTM malformado.");
        }

        var fim = quadro[^5];
        if (fim != Etx && fim != Etb)
        {
            throw new FormatException("Quadro ASTM sem ETX ou ETB.");
        }

        if (!char.IsAsciiDigit(quadro[1]) || quadro[1] > '7')
        {
            throw new FormatException("Número de quadro inválido.");
        }

        var corpo = quadro[1..^4];
        if (Checksum(corpo) != quadro[^4..^2])
        {
            throw new FormatException("Checksum ASTM não confere.");
        }

        return (quadro[1] - '0', quadro[2..^5], fim == Etx);
    }

    /// <summary>Remonta os registros. Quadro repetido (reenvio) é ignorado; salto na sequência é erro.</summary>
    public static IReadOnlyList<string> Registros(IEnumerable<string> quadros)
    {
        var registros = new List<string>();
        var parcial = new StringBuilder();
        var esperado = 1;
        foreach (var quadro in quadros)
        {
            var (numero, texto, ultimo) = Ler(quadro);
            if (numero == (esperado + 7) % 8)
            {
                continue;
            }

            if (numero != esperado % 8)
            {
                throw new FormatException("Quadro fora de ordem.");
            }

            esperado++;
            parcial.Append(texto);
            if (ultimo)
            {
                registros.Add(parcial.ToString());
                parcial.Clear();
            }
        }

        if (parcial.Length > 0)
        {
            throw new FormatException("Transmissão terminou no meio de um registro.");
        }

        return registros;
    }

    public static string Cabecalho(string remetente, string quando) => $@"H|\^&|||{remetente}|||||||P|1|{quando}";

    public static string Paciente(string codigo, string nascimento, string sexo) => $"P|1||{codigo}||||{nascimento}|{sexo}";

    public static string Pedido(string etiqueta, string exame) => $"O|1|{etiqueta}||^^^{exame}|R";

    public static string Resultado(int sequencia, string analito, double valor, string unidade, string flag) =>
        string.Create(CultureInfo.InvariantCulture, $"R|{sequencia}|^^^{analito}|{valor}|{unidade}||{flag}||F");

    public static string Fim() => "L|1|N";

    public static IReadOnlyList<(string Analito, double Valor)> LerResultados(IEnumerable<string> registros)
    {
        var lidos = new List<(string, double)>();
        foreach (var r in registros.Where(x => x.StartsWith("R|", StringComparison.Ordinal)))
        {
            var campos = r.Split('|');
            if (campos.Length < 4 || !double.TryParse(campos[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var valor) || !double.IsFinite(valor))
            {
                throw new FormatException("Registro R inválido.");
            }

            lidos.Add((campos[2].Replace("^", string.Empty, StringComparison.Ordinal), valor));
        }

        return lidos;
    }
}
