using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bancada.Protocolos;

/// <summary>
/// Code 128, conjunto B (ASCII 32 a 126), usado na etiqueta da amostra.
/// Cada símbolo tem 11 módulos (barra, espaço, barra...) e a parada tem 13.
/// A saída é a sequência de larguras de módulo, ex.: "211214" para o início B.
/// </summary>
public static class Code128B
{
    private const int InicioB = 104;
    private const int Parada = 106;
    private const int TamanhoSimbolo = 6;
    private const int TamanhoParada = 7;

    private static readonly string[] Padroes =
    [
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
        "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
        "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
        "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
        "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
        "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
        "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
        "114131", "311141", "411131", "211412", "211214", "211232", "2331112",
    ];

    public static string Codificar(string texto)
    {
        ArgumentException.ThrowIfNullOrEmpty(texto);

        var valores = new List<int>(texto.Length + 3) { InicioB };
        foreach (var c in texto)
        {
            if (c is < ' ' or > '~')
            {
                throw new ArgumentOutOfRangeException(nameof(texto), "Code 128 B aceita apenas ASCII imprimível.");
            }

            valores.Add(c - ' ');
        }

        valores.Add(DigitoVerificador(valores));
        valores.Add(Parada);

        var larguras = new StringBuilder();
        foreach (var v in valores)
        {
            larguras.Append(Padroes[v]);
        }

        return larguras.ToString();
    }

    public static string Decodificar(string larguras)
    {
        ArgumentException.ThrowIfNullOrEmpty(larguras);

        var valores = new List<int>();
        var posicao = 0;
        while (posicao < larguras.Length)
        {
            var tamanho = larguras.AsSpan(posicao).StartsWith(Padroes[Parada], StringComparison.Ordinal)
                ? TamanhoParada
                : TamanhoSimbolo;
            var valor = posicao + tamanho <= larguras.Length
                ? Array.IndexOf(Padroes, larguras.Substring(posicao, tamanho))
                : -1;
            if (valor < 0)
            {
                throw new FormatException("Símbolo de Code 128 inválido ou cortado.");
            }

            valores.Add(valor);
            posicao += tamanho;
        }

        if (valores.Count < 4 || valores[0] != InicioB || valores[^1] != Parada)
        {
            throw new FormatException("Faltam o início B ou a parada.");
        }

        var dados = valores.GetRange(0, valores.Count - 2);
        if (DigitoVerificador(dados) != valores[^2])
        {
            throw new FormatException("O dígito verificador não confere.");
        }

        var texto = new StringBuilder();
        foreach (var v in dados.GetRange(1, dados.Count - 1))
        {
            texto.Append(Convert.ToChar(v + ' ', CultureInfo.InvariantCulture));
        }

        return texto.ToString();
    }

    private static int DigitoVerificador(List<int> valoresSemVerificador)
    {
        var soma = valoresSemVerificador[0];
        for (var i = 1; i < valoresSemVerificador.Count; i++)
        {
            soma += valoresSemVerificador[i] * i;
        }

        return soma % 103;
    }
}
