using System;
using Bancada.Dominio;

namespace Bancada.Aplicacao;

/// <summary>Grava eventos na cadeia de auditoria da sessão. O detalhe é JSON pequeno e nunca leva texto livre do usuário.</summary>
public sealed class ServicoDeAuditoria(IRelogio relogio)
{
    public EventoDeAuditoria Registrar(Sessao sessao, string acao, string alvoTipo = "Sessao", string alvoId = "-", string detalheJson = "{}", string resultado = "Ok")
    {
        ArgumentNullException.ThrowIfNull(sessao);
        var eventos = sessao.Eventos;
        var anterior = eventos.Count == 0 ? CadeiaDeAuditoria.HashGenesis : eventos[^1].Hash;
        var evento = CadeiaDeAuditoria.Encadear(anterior, new EventoDeAuditoria
        {
            Seq = eventos.Count + 1,
            Quando = relogio.Agora,
            Papel = sessao.Papel.ToString(),
            Operador = sessao.Operador,
            Acao = acao,
            AlvoTipo = alvoTipo,
            AlvoId = alvoId,
            DetalheJson = detalheJson,
            Resultado = resultado,
        });
        sessao.AdicionarEvento(evento);
        return evento;
    }

    /// <summary>Seq do primeiro elo quebrado, ou nulo se a cadeia da sessão estiver íntegra.</summary>
    public static int? Verificar(Sessao sessao)
    {
        ArgumentNullException.ThrowIfNull(sessao);
        return CadeiaDeAuditoria.PrimeiroEloQuebrado(sessao.Eventos);
    }
}
