using Microsoft.AspNetCore.Http;

namespace Bancada.Web;

public static class PipelineDaSessao
{
    /// <summary>
    /// Só rota que existe cria sessão: varredura de caminhos inventados (404) não enche a memória.
    /// Health check e páginas de erro nunca criam sessão.
    /// </summary>
    public static bool PrecisaDeSessao(HttpContext contexto) =>
        contexto.GetEndpoint() is not null
        && !contexto.Request.Path.StartsWithSegments("/saude")
        && !contexto.Request.Path.StartsWithSegments("/erro");
}
