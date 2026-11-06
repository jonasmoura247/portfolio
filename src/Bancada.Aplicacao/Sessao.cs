using System;
using System.Collections.Generic;
using Bancada.Dominio;

namespace Bancada.Aplicacao;

/// <summary>
/// Um visitante: papel, operador e os recursos só dele (cifra, banco em memória). Nasce no primeiro acesso e some ao expirar.
/// O id é o valor do cookie; é renovado a cada troca de papel ou operador (defesa contra fixação de sessão).
/// </summary>
public sealed class Sessao : IDisposable
{
    public const int CotaDeRegistros = 500;

    private readonly List<IDisposable> _recursos;
    private readonly List<EventoDeAuditoria> _eventos = [];
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

    public void Dispose()
    {
        if (Descartada)
        {
            return;
        }

        Descartada = true;
        foreach (var recurso in _recursos)
        {
            recurso.Dispose();
        }
    }
}
