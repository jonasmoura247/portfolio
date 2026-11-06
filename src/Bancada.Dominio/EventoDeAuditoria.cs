using System;

namespace Bancada.Dominio;

/// <summary>Evento imutável da auditoria. O detalhe é JSON pequeno e nunca carrega texto livre do usuário.</summary>
public sealed class EventoDeAuditoria
{
    public required int Seq { get; init; }

    public required DateTimeOffset Quando { get; init; }

    public required string Papel { get; init; }

    public required string Operador { get; init; }

    public required string Acao { get; init; }

    public required string AlvoTipo { get; init; }

    public required string AlvoId { get; init; }

    public required string DetalheJson { get; init; }

    public required string Resultado { get; init; }

    public string HashAnterior { get; init; } = string.Empty;

    public string Hash { get; init; } = string.Empty;
}
