using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace Bancada.Testes.Guardas;

/// <summary>
/// Auditoria de licenças (vault: Licencas-MIT). Política estrita: só MIT.
/// Única exceção aceita pelo Jonas em 01/11/2026: SQLitePCLRaw (Apache-2.0), puxado pelo Microsoft.Data.Sqlite.
/// </summary>
public class LicencasTestes
{
    private static readonly HashSet<string> Aceitas = new(StringComparer.OrdinalIgnoreCase) { "MIT" };

    private static readonly Dictionary<string, string> Excecoes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SQLitePCLRaw.bundle_e_sqlite3"] = "Apache-2.0",
        ["SQLitePCLRaw.core"] = "Apache-2.0",
        ["SQLitePCLRaw.lib.e_sqlite3"] = "Apache-2.0",
        ["SQLitePCLRaw.provider.e_sqlite3"] = "Apache-2.0",
    };

    public static IEnumerable<(string Id, string Versao, string Licenca)> Pacotes()
    {
        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assets in Repositorio.Arquivos(".", "project.assets.json", incluirObj: true))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(assets));
            var pastas = doc.RootElement.GetProperty("packageFolders").EnumerateObject().Select(p => p.Name).ToList();
            foreach (var lib in doc.RootElement.GetProperty("libraries").EnumerateObject())
            {
                if (lib.Value.GetProperty("type").GetString() != "package")
                {
                    continue;
                }

                var partes = lib.Name.Split('/');
                if (!vistos.Add(lib.Name))
                {
                    continue;
                }

                var caminho = lib.Value.GetProperty("path").GetString()!;
                var dir = pastas.Select(p => Path.Combine(p, caminho)).First(Directory.Exists);
                yield return (partes[0], partes[1], LerLicenca(dir));
            }
        }
    }

    [Test]
    public void Dependencias_TodasMitComAExcecaoDocumentada()
    {
        var fora = Pacotes()
            .Where(p => !Aceitas.Contains(p.Licenca) && !(Excecoes.TryGetValue(p.Id, out var e) && e == p.Licenca))
            .Select(p => $"{p.Id} {p.Versao}: {p.Licenca}")
            .ToList();

        Assert.That(fora, Is.Empty, "Dependência com licença fora da política (só MIT). Veja vault/05-Qualidade/Licencas-MIT.md");
    }

    [Test]
    public void Dependencias_AuditoriaEncontrouPacotes()
    {
        Assert.That(Pacotes().Count(), Is.GreaterThan(10), "A auditoria não leu os project.assets.json: rode dotnet restore.");
    }

    [Test]
    public void Excecoes_SoExistemSeAindaForemUsadas()
    {
        var ids = Pacotes().Select(p => p.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sobra = Excecoes.Keys.Where(k => ids.Count > 0 && !ids.Contains(k) && ids.Any(i => i.StartsWith("SQLitePCLRaw", StringComparison.OrdinalIgnoreCase))).ToList();

        Assert.That(sobra, Is.Empty);
    }

    private static string LerLicenca(string dir)
    {
        var nuspec = Directory.GetFiles(dir, "*.nuspec").Single();
        var xml = XDocument.Load(nuspec);
        var lic = xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "license");
        if (lic is null)
        {
            // Pacotes antigos da plataforma citam só a URL; o repositório dotnet/corefx é MIT.
            if (xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "licenseUrl")?.Value.Contains("dotnet/corefx", StringComparison.Ordinal) == true)
            {
                return "MIT";
            }

            return "SEM LICENCA: " + (xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "licenseUrl")?.Value ?? "-");
        }

        if (lic.Attribute("type")?.Value == "file")
        {
            var texto = File.ReadAllText(Path.Combine(dir, lic.Value));
            return texto.Contains("Permission is hereby granted, free of charge", StringComparison.Ordinal) ? "MIT" : "ARQUIVO DE LICENCA NAO RECONHECIDO";
        }

        return lic.Value.Trim();
    }
}
