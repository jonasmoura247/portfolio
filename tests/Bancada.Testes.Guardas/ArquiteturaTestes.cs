using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Bancada.Testes.Guardas;

/// <summary>Guardas de arquitetura (vault: Arquitetura-MVC). Falham o build se alguém quebrar uma seta.</summary>
public class ArquiteturaTestes
{
    private static readonly Dictionary<string, string[]> Permitidas = new()
    {
        ["Bancada.Dominio"] = [],
        ["Bancada.Protocolos"] = [],
        ["Bancada.Aplicacao"] = ["Bancada.Dominio", "Bancada.Protocolos"],
        ["Bancada.Infraestrutura"] = ["Bancada.Dominio", "Bancada.Aplicacao"],
        ["Bancada.Web"] = ["Bancada.Dominio", "Bancada.Aplicacao", "Bancada.Infraestrutura"],
    };

    private static readonly string[] Proibidos =
    [
        "DateTime.Now", "DateTime.UtcNow", "DateTimeOffset.Now", "DateTimeOffset.UtcNow",
        "new Random(", "Random.Shared", "Guid.NewGuid(",
    ];

    [TestCaseSource(nameof(Camadas))]
    public void Camada_SoReferenciaQuemPodeReferenciar(string camada)
    {
        var assembly = Assembly.Load(camada);

        var referenciadas = assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith("Bancada.", StringComparison.Ordinal))
            .ToList();

        Assert.That(referenciadas.Except(Permitidas[camada]), Is.Empty, $"{camada} referencia camada proibida");
    }

    [TestCaseSource(nameof(Camadas))]
    public void Camada_PossuiProjetoDeTesteCorrespondente(string camada)
    {
        var nome = camada.Replace("Bancada.", "Bancada.Testes.", StringComparison.Ordinal);

        Assert.That(Directory.Exists(Path.Combine(Repositorio.Raiz, "tests", nome)), Is.True, nome);
    }

    [Test]
    public void Fonte_TempoEAleatorioSoNaInfraestrutura()
    {
        var violacoes = new List<string>();
        foreach (var arquivo in Repositorio.Arquivos("src", "*.cs"))
        {
            if (arquivo.Contains("Bancada.Infraestrutura", StringComparison.Ordinal))
            {
                continue;
            }

            var texto = File.ReadAllText(arquivo);
            violacoes.AddRange(Proibidos.Where(texto.Contains).Select(p => $"{Path.GetFileName(arquivo)}: {p}"));
        }

        Assert.That(violacoes, Is.Empty, "Use IRelogio e ISorteador; relógio e aleatório reais só na Infraestrutura.");
    }

    [Test]
    public void Fonte_NaoUsaSqlCruMontadoNemExclusaoDeCobertura()
    {
        var violacoes = new List<string>();
        foreach (var arquivo in Repositorio.Arquivos("src", "*.cs"))
        {
            var texto = File.ReadAllText(arquivo);
            foreach (var p in new[] { "FromSqlRaw", "ExecuteSqlRaw", "ExcludeFromCodeCoverage" })
            {
                if (texto.Contains(p, StringComparison.Ordinal))
                {
                    violacoes.Add($"{Path.GetFileName(arquivo)}: {p}");
                }
            }
        }

        Assert.That(violacoes, Is.Empty);
    }

    [Test]
    public void Projetos_NaoDesligamAvisosComoErroNemCobertura()
    {
        foreach (var csproj in Repositorio.Arquivos(".", "*.csproj"))
        {
            var texto = File.ReadAllText(csproj);

            Assert.That(texto, Does.Not.Contain("<TreatWarningsAsErrors>false"), csproj);
            Assert.That(texto, Does.Not.Contain("<Threshold>9"), csproj);
            Assert.That(texto, Does.Not.Contain("NoWarn"), csproj);
        }
    }

    [Test]
    public void Testes_ExigemCemPorCentoDeLinhasERamos()
    {
        foreach (var csproj in Repositorio.Arquivos("tests", "*.csproj").Where(p => !p.Contains("Guardas", StringComparison.Ordinal)))
        {
            var texto = File.ReadAllText(csproj);

            Assert.That(texto, Does.Contain("<Threshold>100</Threshold>"), csproj);
            Assert.That(texto, Does.Contain("<ThresholdType>line,branch</ThresholdType>"), csproj);
        }
    }

    public static IEnumerable<string> Camadas => Permitidas.Keys;
}
