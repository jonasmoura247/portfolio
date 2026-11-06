using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Bancada.Dominio;

/// <summary>
/// Cadeia de hash da auditoria (vault: Auditoria-Encadeada). Cada evento guarda
/// SHA-256(hashAnterior + "\n" + conteúdo canônico); alterar, remover ou trocar a ordem de um evento antigo quebra os seguintes.
/// </summary>
public static class CadeiaDeAuditoria
{
    public static string HashGenesis { get; } = Sha256("BANCADA-GENESIS");

    public static string Canonico(EventoDeAuditoria e) => string.Join(
        '|',
        e.Seq.ToString(CultureInfo.InvariantCulture),
        e.Quando.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
        Escapar(e.Papel),
        Escapar(e.Operador),
        Escapar(e.Acao),
        Escapar(e.AlvoTipo),
        Escapar(e.AlvoId),
        Escapar(e.DetalheJson),
        Escapar(e.Resultado));

    /// <summary>Devolve uma cópia do evento com os hashes preenchidos, ligada ao evento anterior.</summary>
    public static EventoDeAuditoria Encadear(string hashAnterior, EventoDeAuditoria e) => new()
    {
        Seq = e.Seq,
        Quando = e.Quando,
        Papel = e.Papel,
        Operador = e.Operador,
        Acao = e.Acao,
        AlvoTipo = e.AlvoTipo,
        AlvoId = e.AlvoId,
        DetalheJson = e.DetalheJson,
        Resultado = e.Resultado,
        HashAnterior = hashAnterior,
        Hash = Calcular(hashAnterior, e),
    };

    /// <summary>Confere a cadeia inteira. Devolve o seq do primeiro elo quebrado, ou nulo se estiver íntegra.</summary>
    public static int? PrimeiroEloQuebrado(IReadOnlyList<EventoDeAuditoria> eventos)
    {
        var anterior = HashGenesis;
        foreach (var e in eventos)
        {
            if (e.HashAnterior != anterior || e.Hash != Calcular(anterior, e))
            {
                return e.Seq;
            }

            anterior = e.Hash;
        }

        return null;
    }

    private static string Calcular(string hashAnterior, EventoDeAuditoria e) => Sha256(hashAnterior + "\n" + Canonico(e));

    private static string Escapar(string campo) => campo.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("|", "\\|", StringComparison.Ordinal);

    private static string Sha256(string texto) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));
}
