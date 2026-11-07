using System.Security.Claims;
using System.Threading.Tasks;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Microsoft.AspNetCore.Http;

namespace Bancada.Web;

/// <summary>
/// Resolve a sessão do visitante a partir do cookie (HttpOnly, Secure, SameSite=Lax, prefixo __Host-), serializa as
/// requisições da mesma sessão e liga a identidade da sessão ao usuário da requisição (a antifalsificação usa isso).
/// O cookie é regravado sempre que o id muda (nova sessão, troca de papel ou de operador).
/// </summary>
public sealed class SessaoMiddleware(RequestDelegate proximo, RepositorioDeSessoes repositorio, ServicoDeAuditoria auditoria)
{
    public const string NomeDoCookie = "__Host-bancada.sid";
    public const string ChaveNoContexto = "Bancada.Sessao";

    private static readonly CookieOptions Opcoes = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        IsEssential = true,
    };

    public async Task InvokeAsync(HttpContext contexto)
    {
        contexto.Request.Cookies.TryGetValue(NomeDoCookie, out var idRecebido);
        var sessao = repositorio.ObterOuCriar(idRecebido);
        await sessao.Entrar(contexto.RequestAborted);

        // Descartada enquanto esperava a vez (teto de sessões): segue numa sessão nova, como se tivesse expirado.
        while (sessao.Descartada)
        {
            sessao.Sair();
            sessao = repositorio.ObterOuCriar(null);
            await sessao.Entrar(contexto.RequestAborted);
        }

        try
        {
            if (sessao.Eventos.Count == 0)
            {
                auditoria.Registrar(sessao, "SessaoCriada");
            }

            auditoria.Descarregar(sessao);
            await Atender(contexto, sessao, idRecebido);
        }
        finally
        {
            auditoria.SairDescarregando(sessao);
        }
    }

    private Task Atender(HttpContext contexto, Sessao sessao, string? idRecebido)
    {
        contexto.Items[ChaveNoContexto] = sessao;
        contexto.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, sessao.Id)], "Sessao"));
        contexto.Response.OnStarting(() =>
        {
            var atual = contexto.Sessao();
            if (atual.Id != idRecebido)
            {
                contexto.Response.Cookies.Append(NomeDoCookie, atual.Id, Opcoes);
            }

            return Task.CompletedTask;
        });

        sessao.Visitados.Add(contexto.Request.Path.Value!);
        return proximo(contexto);
    }
}

public static class ContextoExtensoes
{
    public static bool Pode(this HttpContext contexto, Acao acao) => MatrizDeAutorizacao.Permite(contexto.Sessao().Papel, acao);

    /// <summary>Sessão do visitante desta requisição. O SessaoMiddleware sempre a coloca; sem ele é erro de configuração.</summary>
    public static Sessao Sessao(this HttpContext contexto) =>
        contexto.Items.TryGetValue(SessaoMiddleware.ChaveNoContexto, out var s) && s is Sessao sessao
            ? sessao
            : throw new System.InvalidOperationException("SessaoMiddleware não está no pipeline.");
}
