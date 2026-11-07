using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bancada.Dominio;

namespace Bancada.Aplicacao;

public sealed record VerificacaoDaCadeia(int? QuebraEm, bool Simulada, int Total);

public sealed class ConsultaDeAuditoria(IRegistroDeAuditoria registro, ContextoDaSessao contexto, ServicoDeAuditoria auditoria)
{
    public static readonly Dictionary<string, string[]> Grupos = new()
    {
        ["sessao"] = ["SessaoCriada", "PapelTrocado", "OperadorTrocado", "CenarioCarregado", "IdiomaTrocado"],
        ["clinica"] = ["AgendamentoCriado", "AgendamentoReagendado", "AgendamentoCancelado", "AgendamentoConfirmado", "FaltaRegistrada", "CheckInFeito", "AtendimentoIniciado", "AtendimentoConcluido", "EvolucaoRegistrada", "EvolucaoCorrigida", "AlergiaAlterada", "PedidoCriado", "PedidoCancelado"],
        ["laboratorio"] = ["AmostraColetada", "AmostraRejeitada", "AmostraArmazenada", "AmostraMovida", "AmostraDescartada", "ExecucaoEnviada", "ExecucaoConcluida", "ExecucaoComErro", "ExecucaoReprocessada", "ControleRegistrado", "CriticoConfirmado", "ResultadoValidado", "LaudoLiberado", "LaudoRetificado"],
        ["seguranca"] = ["AutorizacaoRecusada", "AntifalsificacaoInvalido", "LimiteDeTaxaExcedido", "CotaExcedida", "IpBloqueado", "CorpoGrandeDemais", "PayloadBloqueado"],
    };

    /// <summary>Lê a tabela (que os gatilhos protegem), do mais novo para o mais antigo, filtrando por grupo.</summary>
    public IReadOnlyList<EventoDeAuditoria> Listar(string? grupo) =>
        registro.Eventos().Where(e => grupo is null || !Grupos.TryGetValue(grupo, out var acoes) || acoes.Contains(e.Acao)).Reverse().ToList();

    public VerificacaoDaCadeia Verificar()
    {
        auditoria.Registrar(contexto.Sessao, "CadeiaVerificada", "Auditoria");
        var eventos = registro.Eventos();
        return new VerificacaoDaCadeia(CadeiaDeAuditoria.PrimeiroEloQuebrado(eventos), false, eventos.Count);
    }

    /// <summary>Adultera uma cópia em memória: o banco recusaria (gatilhos), e a tela avisa que é só uma simulação.</summary>
    public VerificacaoDaCadeia SimularAdulteracao()
    {
        auditoria.Registrar(contexto.Sessao, "AdulteracaoSimulada", "Auditoria");
        var copia = registro.Eventos().ToList();
        var alvo = copia[copia.Count / 2];
        copia[copia.Count / 2] = new EventoDeAuditoria
        {
            Seq = alvo.Seq,
            Quando = alvo.Quando,
            Papel = alvo.Papel,
            Operador = alvo.Operador,
            Acao = "EventoForjado",
            AlvoTipo = alvo.AlvoTipo,
            AlvoId = alvo.AlvoId,
            DetalheJson = alvo.DetalheJson,
            Resultado = alvo.Resultado,
            HashAnterior = alvo.HashAnterior,
            Hash = alvo.Hash,
        };
        return new VerificacaoDaCadeia(CadeiaDeAuditoria.PrimeiroEloQuebrado(copia), true, copia.Count);
    }
}

public interface IEsquemaDoBanco
{
    /// <summary>Tabelas, índices e gatilhos desta sessão, lidos do catálogo do próprio SQLite.</summary>
    IReadOnlyList<(string Tipo, string Nome)> Objetos();
}

public sealed record ComandoDeTeste(string Programa, IReadOnlyList<string> Argumentos, TimeSpan TempoMaximo);

public interface IProcessos
{
    /// <summary>Roda o comando, entrega cada linha de saída e devolve o código de saída. Passou do tempo, mata a árvore e devolve nulo.</summary>
    Task<int?> Rodar(ComandoDeTeste comando, Action<string> aoLer, CancellationToken cancelar);
}

public sealed class LimitesDoExecutor
{
    /// <summary>Desligado por padrão: ligar é decisão explícita de quem hospeda (ou o ambiente de desenvolvimento).</summary>
    public bool Habilitado { get; init; }

    public int PorIpACada5Minutos { get; init; } = 1;

    public int PorHora { get; init; } = 10;

    public int TamanhoDaSaida { get; init; } = 256 * 1024;
}

public enum EstadoDoExecutor
{
    Pronto,
    Rodando,
    Concluido,
    Falhou,
}

public sealed record RetratoDoExecutor(EstadoDoExecutor Estado, string? Suite, string Saida, bool Cortada);

/// <summary>
/// Roda os testes a pedido de um visitante anônimo (vault: Executor-de-Testes). O cliente só escolhe "csharp" ou "js": o comando é
/// constante. Uma execução por vez, limite por IP e por hora, tempo máximo, saída limitada e desligamento por configuração.
/// </summary>
public sealed class ExecutorDeTestes(IProcessos processos, IRelogio relogio, LimitesDoExecutor limites)
{
    public static readonly Dictionary<string, ComandoDeTeste> Comandos = new()
    {
        ["csharp"] = new("dotnet", ["test", "Bancada.sln", "-c", "Release", "--no-build", "--nologo", "-p:CollectCoverage=false"], TimeSpan.FromSeconds(180)),
        ["js"] = new("node", ["--test", "tests/js/**/*.test.js"], TimeSpan.FromSeconds(60)),
    };

    private static readonly TimeSpan JanelaPorIp = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan JanelaGlobal = TimeSpan.FromHours(1);

    private readonly Lock _trava = new();
    private readonly List<DateTimeOffset> _execucoes = [];
    private readonly Dictionary<string, DateTimeOffset> _porIp = [];
    private readonly System.Text.StringBuilder _saida = new();
    private EstadoDoExecutor _estado;
    private string? _suite;
    private bool _cortada;

    public bool Habilitado => limites.Habilitado;

    public Task? EmCurso { get; private set; }

    public RetratoDoExecutor Retrato
    {
        get
        {
            lock (_trava)
            {
                return new RetratoDoExecutor(_estado, _suite, _saida.ToString(), _cortada);
            }
        }
    }

    /// <summary>Chave do motivo da recusa, ou nulo se a execução começou (ou se já havia uma em curso, à qual o visitante assiste).</summary>
    public string? Iniciar(string? suite, string ip)
    {
        if (!limites.Habilitado)
        {
            return "painel.erro.desligado";
        }

        if (suite is null || !Comandos.TryGetValue(suite, out var comando))
        {
            return "painel.erro.suite";
        }

        lock (_trava)
        {
            if (_estado == EstadoDoExecutor.Rodando)
            {
                return null;
            }

            var agora = relogio.Agora;
            _execucoes.RemoveAll(e => agora - e > JanelaGlobal);
            if (_execucoes.Count >= limites.PorHora || (_porIp.TryGetValue(ip, out var ultima) && agora - ultima < JanelaPorIp))
            {
                return "painel.erro.limite";
            }

            _execucoes.Add(agora);
            _porIp[ip] = agora;
            _estado = EstadoDoExecutor.Rodando;
            _suite = suite;
            _cortada = false;
            _saida.Clear();
        }

        EmCurso = Rodar(comando);
        return null;
    }

    private async Task Rodar(ComandoDeTeste comando)
    {
        using var limite = new CancellationTokenSource(comando.TempoMaximo);
        int? codigo = null;
        try
        {
            codigo = await processos.Rodar(comando, Acrescentar, limite.Token).ConfigureAwait(false);
        }
        catch (InvalidOperationException e)
        {
            Acrescentar(e.Message);
        }

        lock (_trava)
        {
            _estado = codigo == 0 ? EstadoDoExecutor.Concluido : EstadoDoExecutor.Falhou;
        }
    }

    private void Acrescentar(string linha)
    {
        lock (_trava)
        {
            if (_saida.Length + linha.Length + 1 > limites.TamanhoDaSaida)
            {
                _cortada = true;
                return;
            }

            _saida.AppendLine(linha);
        }
    }
}
