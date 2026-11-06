using System;
using System.Collections.Generic;
using System.Linq;
using Bancada.Dominio;

namespace Bancada.Aplicacao;

/// <summary>
/// Relógio do laboratório. A data é fictícia e fixa (quarta, 04/11/2026, 10:15), então a demonstração funciona igual
/// em qualquer dia e hora. O tempo só anda enquanto há exame processando, acelerado pelo fator escolhido.
/// </summary>
public sealed class RelogioDeSimulacao
{
    public static readonly DateTime Inicio = new(2026, 11, 4, 10, 15, 0);

    private static readonly int[] Fatores = [1, 60, 600];

    private DateTimeOffset? _ultimaLeitura;

    public DateTime Agora { get; private set; } = Inicio;

    public int Fator { get; private set; } = 60;

    public bool Pausado { get; private set; }

    public DateOnly Hoje => DateOnly.FromDateTime(Agora);

    public void Definir(int fator)
    {
        Fator = Array.IndexOf(Fatores, fator) >= 0 ? fator : throw new ViolacaoDeRegra("equipamento.erro.fator");
        Pausado = false;
    }

    public void Pausar() => Pausado = true;

    /// <summary>Só marca o instante real, sem andar o relógio (nada processando).</summary>
    public void Marcar(DateTimeOffset real) => _ultimaLeitura = real;

    /// <summary>Anda o relógio pelo tempo real decorrido e devolve quantos minutos simulados passaram.</summary>
    public double Avancar(DateTimeOffset real)
    {
        var anterior = _ultimaLeitura ?? real;
        _ultimaLeitura = real;
        if (Pausado)
        {
            return 0;
        }

        var minutos = (real - anterior).TotalSeconds * Fator / 60;
        Agora = Agora.AddMinutes(minutos);
        return minutos;
    }
}

/// <summary>Cenários prontos (vault: Dados-de-Semente). Mudam valores, falhas e o controle de qualidade da sessão.</summary>
public static class Cenarios
{
    public const string Padrao = "critico";
    public const string PedidoDoCenario = "PED-000045";

    public static readonly string[] Todos = ["feliz", "critico", "hemolisada", "erro", "cq"];

    private static readonly Dictionary<string, double> Criticos = new() { ["POT"] = 7.1, ["SOD"] = 138, ["GLI"] = 108, ["CRE"] = 1.4 };
    private static readonly Dictionary<string, double> Felizes = new() { ["POT"] = 4.5, ["SOD"] = 139, ["GLI"] = 92, ["CRE"] = 1.1 };

    public static double? ValorForcado(string cenario, string pedido, string analito)
    {
        if (pedido != PedidoDoCenario)
        {
            return null;
        }

        var tabela = cenario == "feliz" ? Felizes : Criticos;
        return tabela.TryGetValue(analito, out var valor) ? valor : null;
    }

    public static bool ExameFalha(string cenario, string exame) => cenario == "erro" && exame == "GLI";

    public static bool SugereHemolise(string cenario, string pedido) => cenario == "hemolisada" && pedido == PedidoDoCenario;
}

/// <summary>Valor determinístico dentro da faixa normal: mesma entrada, mesmo resultado.</summary>
public static class GeradorDeValores
{
    public static double Normal(Analito analito, string sexo, string semente)
    {
        var f = analito.FaixaPara(sexo);
        var baseDoValor = (f.NormalMin, f.NormalMax) switch
        {
            (double min, double max) => (min + max) / 2,
            (null, double max) => max * 0.7,
            _ => f.NormalMin!.Value * 1.4,
        };
        var variacao = (Fnv(semente + analito.Codigo) - 0.5) * 0.06;
        return Math.Round(baseDoValor * (1 + variacao), analito.Casas);
    }

    private static double Fnv(string texto)
    {
        var h = 2166136261u;
        foreach (var c in texto)
        {
            h = (h ^ c) * 16777619u;
        }

        return h / (double)uint.MaxValue;
    }
}
