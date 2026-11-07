using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Bancada.Dominio;

namespace Bancada.Aplicacao;

/// <summary>Evento de segurança que chegou enquanto outra requisição usava a sessão: espera a vez para entrar na cadeia.</summary>
public sealed record EventoPendente(string Acao, string AlvoTipo, string AlvoId, string DetalheJson, string Resultado);

/// <summary>
/// Um visitante: papel, operador e os recursos só dele (cifra, banco em memória). Nasce no primeiro acesso e some ao expirar.
/// O id é o valor do cookie; é renovado a cada troca de papel ou operador (defesa contra fixação de sessão).
/// Uma requisição por vez usa a sessão (Entrar/Sair): o banco em memória e a cadeia de auditoria não aceitam uso simultâneo.
/// </summary>
public sealed class Sessao : IDisposable
{
    public const int CotaDeRegistros = 500;

    /// <summary>Teto da fila de eventos pendentes: uma enxurrada de recusas não vira memória sem fim.</summary>
    public const int MaximoDePendentes = 100;

    private readonly List<IDisposable> _recursos;
    private readonly List<EventoDeAuditoria> _eventos = [];
    private readonly SemaphoreSlim _vez = new(1, 1);
    private readonly Lock _estado = new();
    private readonly Queue<EventoPendente> _pendentes = new();
    private bool _emUso;
    private bool _recursosLiberados;
    private int _registros;

    public Sessao(string id, Papel papel, string operador, DateTimeOffset agora, ICifra cifra, IEnumerable<IDisposable> recursos)
    {
        Id = id;
        Papel = papel;
        Operador = operador;
        CriadaEm = agora;
        UltimoAcesso = agora;
        Cifra = cifra;
        _recursos = [.. recursos];
    }

    public string Id { get; private set; }

    public Papel Papel { get; private set; }

    public string Operador { get; private set; }

    public DateTimeOffset CriadaEm { get; }

    public DateTimeOffset UltimoAcesso { get; private set; }

    public ICifra Cifra { get; }

    public bool Descartada { get; private set; }

    /// <summary>Idioma da interface: "pt" ou "en" (a troca de idioma entra na fase de acabamento).</summary>
    public string Idioma { get; set; } = "pt";

    /// <summary>Auditoria desta sessão, em ordem, encadeada por hash. Só cresce (vault: Auditoria-Encadeada).</summary>
    public IReadOnlyList<EventoDeAuditoria> Eventos => _eventos;

    /// <summary>Relógio do laboratório: data fictícia fixa, acelerada só enquanto há exame processando.</summary>
    public RelogioDeSimulacao Simulacao { get; } = new();

    /// <summary>Cenário carregado (vault: Dados-de-Semente).</summary>
    public string Cenario { get; set; } = Cenarios.Padrao;

    /// <summary>Banco em memória da sessão. Quem conhece o tipo concreto é a Infraestrutura.</summary>
    public object? Banco { get; set; }

    /// <summary>Grava cada evento também na tabela de auditoria do banco da sessão.</summary>
    public Action<EventoDeAuditoria>? GravadorDeAuditoria { get; set; }

    /// <summary>Telas já visitadas, para marcar o roteiro guiado.</summary>
    public ISet<string> Visitados { get; } = new HashSet<string>();

    /// <summary>Mensagem para a próxima página (padrão Post/Redirect/Get). A chave é um texto do catálogo.</summary>
    public (string Chave, bool Sucesso)? Aviso { get; set; }

    /// <summary>RN-060: cota de registros criados por sessão.</summary>
    public void ContarRegistro()
    {
        if (++_registros > CotaDeRegistros)
        {
            throw new ViolacaoDeRegra("sessao.erro.cota");
        }
    }

    public void Tocar(DateTimeOffset agora) => UltimoAcesso = agora;

    internal void AdicionarEvento(EventoDeAuditoria evento)
    {
        _eventos.Add(evento);
        GravadorDeAuditoria?.Invoke(evento);
    }

    internal void Assumir(Papel papel, string operador) => DefinirIdentidade(Id, papel, operador);

    internal void DefinirIdentidade(string id, Papel papel, string operador)
    {
        Id = id;
        Papel = papel;
        Operador = operador;
    }

    /// <summary>Eventos na fila, à espera de quem estiver com a vez.</summary>
    public int Pendentes
    {
        get
        {
            lock (_estado)
            {
                return _pendentes.Count;
            }
        }
    }

    /// <summary>Espera a vez de usar a sessão. Toda entrada termina em <see cref="Sair"/>.</summary>
    public async Task Entrar(CancellationToken cancelar)
    {
        await _vez.WaitAsync(cancelar).ConfigureAwait(false);
        lock (_estado)
        {
            _emUso = true;
        }
    }

    /// <summary>Entra só se ninguém estiver usando (sem esperar). Sessão descartada não aceita ninguém.</summary>
    public bool TentarEntrar()
    {
        if (!_vez.Wait(0))
        {
            return false;
        }

        lock (_estado)
        {
            if (!Descartada)
            {
                _emUso = true;
                return true;
            }
        }

        _vez.Release();
        return false;
    }

    /// <summary>Devolve a vez. Se a sessão foi descartada durante o uso, é agora que os recursos são liberados.</summary>
    public void Sair()
    {
        lock (_estado)
        {
            Devolver();
        }
    }

    /// <summary>
    /// Devolve a vez só se a fila estiver vazia; senão continua com ela (falso) para quem chamou gravar os pendentes.
    /// A conferência e a devolução acontecem juntas, sob a mesma trava da fila: quem enfileira depois disso encontra a vez livre.
    /// </summary>
    public bool SairSeNadaPendente()
    {
        lock (_estado)
        {
            if (_pendentes.Count > 0 && !Descartada)
            {
                return false;
            }

            Devolver();
            return true;
        }
    }

    private void Devolver()
    {
        _emUso = false;
        if (Descartada)
        {
            LiberarRecursos();
        }

        _vez.Release();
    }

    /// <summary>Põe um evento na fila. Recusa (falso) com a fila cheia ou a sessão descartada.</summary>
    public bool Enfileirar(EventoPendente evento)
    {
        lock (_estado)
        {
            if (Descartada || _pendentes.Count >= MaximoDePendentes)
            {
                return false;
            }

            _pendentes.Enqueue(evento);
            return true;
        }
    }

    public bool TirarPendente([NotNullWhen(true)] out EventoPendente? evento)
    {
        lock (_estado)
        {
            return _pendentes.TryDequeue(out evento);
        }
    }

    /// <summary>Descarta a sessão. Se uma requisição está usando, os recursos só são liberados quando ela sair.</summary>
    public void Dispose()
    {
        lock (_estado)
        {
            Descartada = true;
            if (!_emUso)
            {
                LiberarRecursos();
            }
        }
    }

    private void LiberarRecursos()
    {
        if (_recursosLiberados)
        {
            return;
        }

        _recursosLiberados = true;
        foreach (var recurso in _recursos)
        {
            recurso.Dispose();
        }
    }
}
