using System;
using System.Collections.Generic;
using System.Linq;

namespace Bancada.Protocolos;

/// <summary>Cálculos de laboratório: flags por faixa, delta check e regras de Westgard. Funções puras.</summary>
public static class Analise
{
    public static string Classificar(double valor, double? normalMin, double? normalMax, double? criticoMin, double? criticoMax)
    {
        if (valor < criticoMin)
        {
            return "CL";
        }

        if (valor > criticoMax)
        {
            return "CH";
        }

        if (valor < normalMin)
        {
            return "L";
        }

        return valor > normalMax ? "H" : "N";
    }

    public static bool DeltaSuspeito(double valor, double? anterior, double percentualAceito) =>
        anterior is > 0 && Math.Abs(valor - anterior.Value) / anterior.Value * 100 > percentualAceito;

    /// <summary>Regras de Westgard avaliadas na última medida da série (em desvios da média). 1-2s é só aviso.</summary>
    public static IReadOnlyList<string> Westgard(IReadOnlyList<double> z, out bool soAviso)
    {
        var violadas = new List<string>();
        if (z.Count == 0)
        {
            soAviso = false;
            return violadas;
        }

        var ultima = z[^1];
        if (Math.Abs(ultima) > 3)
        {
            violadas.Add("1-3s");
        }

        if (z.Count >= 2)
        {
            var penultima = z[^2];
            if (Math.Abs(ultima) > 2 && Math.Abs(penultima) > 2)
            {
                violadas.Add(Math.Sign(ultima) == Math.Sign(penultima) ? "2-2s" : "R-4s");
            }
        }

        if (MesmoLado(z, 4, 1))
        {
            violadas.Add("4-1s");
        }

        if (MesmoLado(z, 10, 0))
        {
            violadas.Add("10x");
        }

        soAviso = violadas.Count == 0 && Math.Abs(ultima) > 2;
        if (soAviso)
        {
            violadas.Add("1-2s");
        }

        return violadas;
    }

    private static bool MesmoLado(IReadOnlyList<double> z, int quantidade, double limite)
    {
        if (z.Count < quantidade)
        {
            return false;
        }

        var fim = z.Skip(z.Count - quantidade).ToList();
        return fim.All(v => v > limite) || fim.All(v => v < -limite);
    }
}
