using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bancada.Testes.Guardas;

/// <summary>Localiza a raiz do repositório e lista arquivos de código, sem depender do diretório de execução.</summary>
internal static class Repositorio
{
    public static string Raiz { get; } = Localizar();

    public static IEnumerable<string> Arquivos(string pasta, string padrao, bool incluirObj = false) =>
        Directory.EnumerateFiles(Path.Combine(Raiz, pasta), padrao, SearchOption.AllDirectories)
            .Where(f => (incluirObj || !Contem(f, "obj"))
                     && !Contem(f, "bin")
                     && !Contem(f, "node_modules"));

    private static bool Contem(string caminho, string pasta) =>
        caminho.Contains($"{Path.DirectorySeparatorChar}{pasta}{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string Localizar()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Bancada.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Raiz do repositório (Bancada.sln) não encontrada.");
    }
}
