using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Bancada.Aplicacao;

namespace Bancada.Infraestrutura;

/// <summary>
/// Cria o processo de verdade: diretório fixo, sem shell, comando e argumentos só de constantes. O ambiente é limpo de
/// verdade: só passam as variáveis desta lista (caminhos do sistema e do .NET). Segredo injetado pela hospedagem não chega
/// ao processo dos testes, nem à saída que o visitante lê.
/// </summary>
public sealed class ProcessosDoSistema(string raiz) : IProcessos
{
    public static readonly string[] VariaveisPermitidas =
    [
        "PATH", "HOME", "USERPROFILE", "TEMP", "TMP", "TMPDIR", "LANG", "SystemRoot", "SystemDrive", "windir", "ProgramFiles", "ProgramFiles(x86)",
        "ProgramData", "APPDATA", "LOCALAPPDATA", "DOTNET_ROOT", "DOTNET_CLI_HOME", "DOTNET_NOLOGO", "DOTNET_CLI_TELEMETRY_OPTOUT", "NUGET_PACKAGES",
    ];

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

        inicio.Environment.Clear();
        foreach (var nome in VariaveisPermitidas)
        {
            if (Environment.GetEnvironmentVariable(nome) is { } valor)
            {
                inicio.Environment[nome] = valor;
            }
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
