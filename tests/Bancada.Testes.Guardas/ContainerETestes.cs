using System;
using System.IO;

namespace Bancada.Testes.Guardas;

/// <summary>Requisitos objetivos do Dockerfile e do CI (vault: Pipeline-CI-e-Container).</summary>
public class ContainerETestes
{
    private static string Ler(params string[] caminho) => File.ReadAllText(Path.Combine(Repositorio.Raiz, Path.Combine(caminho)));

    [Test]
    public void Dockerfile_RodaSemPrivilegioSemAddDeUrlESemTagLatest()
    {
        var docker = Ler("Dockerfile");

        Assert.That(docker, Does.Contain("USER $APP_UID"));
        Assert.That(docker, Does.Not.Contain("ADD http"));
        Assert.That(docker, Does.Not.Contain(":latest"));
        Assert.That(docker, Does.Contain("--locked-mode"));
        Assert.That(docker, Does.Contain("EXPOSE 8080"));
    }

    [Test]
    public void SEC10_Ci_TokenSoDeLeituraEActionsFixadasPorCommit()
    {
        var ci = Ler(".github", "workflows", "ci.yml");
        var usos = System.Text.RegularExpressions.Regex.Matches(ci, @"uses:\s*(\S+)");

        Assert.That(ci, Does.Contain("permissions:\n  contents: read").Or.Contain("permissions:\r\n  contents: read"));
        Assert.That(ci, Does.Contain("persist-credentials: false"));
        Assert.That(usos, Is.Not.Empty);
        foreach (System.Text.RegularExpressions.Match uso in usos)
        {
            Assert.That(uso.Groups[1].Value, Does.Match("@[0-9a-f]{40}$"), $"action sem SHA: {uso.Groups[1].Value}");
        }
    }

    [Test]
    public void SEC11_ContextoDoDocker_NaoLevaGitNemArtefatosLocais()
    {
        var ignorados = Ler(".dockerignore");

        foreach (var item in new[] { ".git/", "**/bin/", "**/obj/", "vault/", "**/painel-instantaneo.json" })
        {
            Assert.That(ignorados, Does.Contain(item));
        }
    }

    [Test]
    public void Ci_TemOsMesmosPassosDoScriptLocal()
    {
        var ci = Ler(".github", "workflows", "ci.yml");
        var local = Ler("tools", "verificar.ps1");

        foreach (var passo in new[] { "--locked-mode", "--verify-no-changes", "dotnet build", "dotnet test", "test:js", "--vulnerable" })
        {
            Assert.That(ci, Does.Contain(passo), $"ci.yml sem: {passo}");
            Assert.That(local, Does.Contain(passo), $"verificar.ps1 sem: {passo}");
        }
    }
}
