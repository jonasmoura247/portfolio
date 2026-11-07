using System;
using Bancada.Dominio;

namespace Bancada.Aplicacao;

/// <summary>
/// Grava eventos na cadeia de auditoria da sessão. O detalhe é JSON pequeno e nunca leva texto livre do usuário.
/// <see cref="Registrar"/> só roda com a vez da sessão (Sessao.Entrar): dois registros ao mesmo tempo repetiriam o número
/// de sequência e quebrariam a cadeia. Quem está fora da requisição (limite de taxa, corpo grande) usa <see cref="Anotar"/>.
/// </summary>
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

    /// <summary>
    /// Evento de quem não tem a vez da sessão: entra na fila e é gravado já (se a sessão estiver livre) ou por quem a
    /// estiver usando, ao sair. Nunca espera: uma enxurrada de recusas não trava a requisição legítima.
    /// </summary>
    public void Anotar(Sessao sessao, string acao, string alvoTipo = "Sessao", string alvoId = "-", string detalheJson = "{}", string resultado = "Bloqueado")
    {
        ArgumentNullException.ThrowIfNull(sessao);
        if (sessao.Enfileirar(new EventoPendente(acao, alvoTipo, alvoId, detalheJson, resultado)) && sessao.TentarEntrar())
        {
            SairDescarregando(sessao);
        }
    }

    /// <summary>Grava os eventos da fila. Só com a vez da sessão.</summary>
    public void Descarregar(Sessao sessao)
    {
        ArgumentNullException.ThrowIfNull(sessao);
        while (sessao.TirarPendente(out var p))
        {
            Registrar(sessao, p.Acao, p.AlvoTipo, p.AlvoId, p.DetalheJson, p.Resultado);
        }
    }

    /// <summary>
    /// Grava a fila e devolve a vez. Se alguém enfileirou durante a gravação, grava de novo antes de sair: nenhum evento
    /// fica esquecido (quem enfileira depois da saída encontra a vez livre e grava ele mesmo).
    /// </summary>
    public void SairDescarregando(Sessao sessao)
    {
        ArgumentNullException.ThrowIfNull(sessao);
        try
        {
            do
            {
                Descarregar(sessao);
            }
            while (!sessao.SairSeNadaPendente());
        }
        catch
        {
            sessao.Sair();
            throw;
        }
    }

    /// <summary>Seq do primeiro elo quebrado, ou nulo se a cadeia da sessão estiver íntegra.</summary>
    public static int? Verificar(Sessao sessao)
    {
        ArgumentNullException.ThrowIfNull(sessao);
        return CadeiaDeAuditoria.PrimeiroEloQuebrado(sessao.Eventos);
    }
}
