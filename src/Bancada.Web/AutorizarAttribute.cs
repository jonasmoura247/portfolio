using System;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Bancada.Web;

/// <summary>
/// Toda action protegida declara a ação que exerce (vault: Matriz-de-Autorizacao). Papel sem permissão recebe 403
/// e o evento AutorizacaoRecusada entra na auditoria da sessão.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class AutorizarAttribute(Acao acao) : Attribute, IAuthorizationFilter
{
    public Acao Acao { get; } = acao;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var sessao = context.HttpContext.Sessao();
        if (MatrizDeAutorizacao.Permite(sessao.Papel, Acao))
        {
            return;
        }

        context.HttpContext.RequestServices.GetRequiredService<ServicoDeAuditoria>()
            .Registrar(sessao, "AutorizacaoRecusada", "Acao", Acao.ToString(), "{}", "Recusado");
        context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
    }
}
