using System;
using System.Collections.Generic;
using System.Linq;

namespace Bancada.Dominio;

/// <summary>Faixa de um analito. Limite nulo = faixa aberta daquele lado. Valores fictícios e simplificados: não usar clinicamente.</summary>
public sealed class Faixa
{
    public required string Sexo { get; init; }

    public double? NormalMin { get; init; }

    public double? NormalMax { get; init; }

    public double? CriticoMin { get; init; }

    public double? CriticoMax { get; init; }
}

public sealed class Analito
{
    public required string Codigo { get; init; }

    public required string Unidade { get; init; }

    public required int Casas { get; init; }

    public required double DeltaPct { get; init; }

    public required Faixa[] Faixas { get; init; }

    public Faixa FaixaPara(string sexo) => Faixas.FirstOrDefault(f => f.Sexo == sexo) ?? Faixas.First(f => f.Sexo == "A");
}

public sealed class Exame
{
    public required string Codigo { get; init; }

    public required string Material { get; init; }

    public required double VolumeMl { get; init; }

    public required int JejumHoras { get; init; }

    public required double TempoMin { get; init; }

    public required string Equipamento { get; init; }

    public required string[] Analitos { get; init; }
}

public sealed class Equipamento
{
    public required string Codigo { get; init; }

    public required string Protocolo { get; init; }

    public required int Capacidade { get; init; }
}

public sealed class Tubo
{
    public required string Material { get; init; }

    public required IReadOnlyList<string> Exames { get; init; }

    public required double VolumeMl { get; init; }

    public required int JejumHoras { get; init; }
}

public static class Catalogo
{
    public const string Soro = "Soro";
    public const string SangueTotal = "SangueTotalEDTA";
    public const string Bioquimica = "EQ-BIO-01";
    public const string Hematologia = "EQ-HEM-01";
    public const string Imunoensaio = "EQ-IMU-01";

    private static readonly Dictionary<string, Analito> PorAnalito = new[]
    {
        A("GLI", "mg/dL", 0, 50, F("A", 70, 99, 40, 500)),
        A("URE", "mg/dL", 0, 50, F("A", 15, 45, null, 180)),
        A("CRE", "mg/dL", 1, 40, F("M", 0.7, 1.3, null, 7), F("F", 0.6, 1.1, null, 7)),
        A("COL", "mg/dL", 0, 50, F("A", null, 190, null, null)),
        A("TRI", "mg/dL", 0, 50, F("A", null, 150, null, 1000)),
        A("SOD", "mmol/L", 0, 50, F("A", 135, 145, 120, 160)),
        A("POT", "mmol/L", 1, 25, F("A", 3.5, 5.1, 2.5, 6.5)),
        A("HGB", "g/dL", 1, 20, F("M", 13.5, 17.5, 7, 20), F("F", 12, 15.5, 7, 20)),
        A("LEU", "/µL", 0, 50, F("A", 4000, 11000, 1000, 50000)),
        A("PLQ", "/µL", 0, 50, F("A", 150000, 450000, 20000, 1000000)),
        A("TSH", "mUI/L", 2, 50, F("A", 0.4, 4, null, null)),
        A("T4L", "ng/dL", 1, 50, F("A", 0.8, 1.8, null, null)),
        A("FER", "ng/mL", 0, 50, F("M", 30, 400, null, null), F("F", 15, 150, null, null)),
    }.ToDictionary(a => a.Codigo);

    public static IReadOnlyList<Equipamento> Equipamentos { get; } =
    [
        new() { Codigo = Bioquimica, Protocolo = "ASTM", Capacidade = 4 },
        new() { Codigo = Hematologia, Protocolo = "HL7", Capacidade = 2 },
        new() { Codigo = Imunoensaio, Protocolo = "HL7", Capacidade = 6 },
    ];

    public static IReadOnlyList<Exame> Exames { get; } =
    [
        E("GLI", Soro, 0.5, 8, 8, Bioquimica, "GLI"),
        E("URE", Soro, 0.5, 0, 8, Bioquimica, "URE"),
        E("CRE", Soro, 0.5, 0, 8, Bioquimica, "CRE"),
        E("COL", Soro, 0.5, 12, 10, Bioquimica, "COL"),
        E("TRI", Soro, 0.5, 12, 10, Bioquimica, "TRI"),
        E("ELE", Soro, 0.5, 0, 6, Bioquimica, "SOD", "POT"),
        E("HEM", SangueTotal, 2, 0, 12, Hematologia, "HGB", "LEU", "PLQ"),
        E("TSH", Soro, 0.5, 0, 20, Imunoensaio, "TSH"),
        E("T4L", Soro, 0.5, 0, 20, Imunoensaio, "T4L"),
        E("FER", Soro, 0.5, 0, 20, Imunoensaio, "FER"),
    ];

    public static Analito AnalitoDe(string codigo) => PorAnalito[codigo];

    public static Exame ExameDe(string codigo) =>
        Exames.FirstOrDefault(e => e.Codigo == codigo) ?? throw new ViolacaoDeRegra("pedido.erro.exame");

    public static Equipamento EquipamentoDe(string codigo) => Equipamentos.First(e => e.Codigo == codigo);

    /// <summary>Exames do mesmo material vão no mesmo tubo (RN-021); o tubo herda o maior jejum e o maior volume.</summary>
    public static IReadOnlyList<Tubo> Tubos(IEnumerable<string> exames) =>
        exames.Select(ExameDe).GroupBy(e => e.Material).Select(g => new Tubo
        {
            Material = g.Key,
            Exames = g.Select(e => e.Codigo).ToList(),
            VolumeMl = g.Max(e => e.VolumeMl),
            JejumHoras = g.Max(e => e.JejumHoras),
        }).ToList();

    private static Faixa F(string sexo, double? nmin, double? nmax, double? cmin, double? cmax) =>
        new() { Sexo = sexo, NormalMin = nmin, NormalMax = nmax, CriticoMin = cmin, CriticoMax = cmax };

    private static Analito A(string codigo, string unidade, int casas, double delta, params Faixa[] faixas) =>
        new() { Codigo = codigo, Unidade = unidade, Casas = casas, DeltaPct = delta, Faixas = faixas };

    private static Exame E(string codigo, string material, double volume, int jejum, double tempo, string equipamento, params string[] analitos) =>
        new() { Codigo = codigo, Material = material, VolumeMl = volume, JejumHoras = jejum, TempoMin = tempo, Equipamento = equipamento, Analitos = analitos };
}
