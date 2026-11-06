using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Bancada.Aplicacao;

namespace Bancada.Infraestrutura;

/// <summary>Cria o processo de verdade: ambiente limpo, diretório fixo, sem shell. Comando e argumentos vêm só de constantes.</summary>
public sealed class ProcessosDoSistema(string raiz) : IProcessos
{
    public async Task<int?> Rodar(ComandoDeTeste comando, Action<string> aoLer, CancellationToken cancelar)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var inicio = new ProcessStartInfo(comando.Programa)
        {
            WorkingDirectory = raiz,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argumento in comando.Argumentos)
        {
            inicio.ArgumentList.Add(argumento);
        }

        foreach (var nome in new[] { "GITHUB_TOKEN", "HTTP_PROXY", "HTTPS_PROXY" })
        {
            inicio.Environment.Remove(nome);
        }

        using var processo = new Process { StartInfo = inicio };
        processo.OutputDataReceived += (_, e) => Ler(e.Data, aoLer);
        processo.ErrorDataReceived += (_, e) => Ler(e.Data, aoLer);
        processo.Start();
        processo.BeginOutputReadLine();
        processo.BeginErrorReadLine();
        try
        {
            await processo.WaitForExitAsync(cancelar).ConfigureAwait(false);
            return processo.ExitCode;
        }
        catch (OperationCanceledException)
        {
            processo.Kill(entireProcessTree: true);
            return null;
        }
    }

    private static void Ler(string? linha, Action<string> aoLer)
    {
        if (linha is not null)
        {
            aoLer(linha);
        }
    }
}
