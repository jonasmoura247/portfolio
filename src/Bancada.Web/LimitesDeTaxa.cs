using System;
using System.Globalization;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Bancada.Aplicacao;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Bancada.Web;

/// <summary>Limites por IP e por minuto (vault: Cabecalhos-e-Configuracao-Segura). Valores de partida; calibrar na Fase 1.</summary>
public sealed class LimitesDeTaxa
{
    public int Leitura { get; init; } = 120;

    public int Escrita { get; init; } = 30;

    public int NovaSessao { get; init; } = 10;

    public TimeSpan Janela { get; init; } = TimeSpan.FromMinutes(1);
}

public static class ConfiguracaoDeTaxa
{
    public static void Aplicar(RateLimiterOptions opcoes, LimitesDeTaxa limites)
    {
        opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        opcoes.OnRejected = (contexto, _) =>
        {
            var http = contexto.HttpContext;
            http.Response.Headers.RetryAfter = ((int)limites.Janela.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            var sessoes = http.RequestServices.GetRequiredService<RepositorioDeSessoes>();
            var auditoria = http.RequestServices.GetRequiredService<ServicoDeAuditoria>();
            ProtecaoDeEntrada.AnotarEvento(http, sessoes, auditoria, "LimiteDeTaxaExcedido");
            if (http.RequestServices.GetRequiredService<BloqueioDeIp>().Contar(ProtecaoDeEntrada.Ip(http)))
            {
                ProtecaoDeEntrada.AnotarEvento(http, sessoes, auditoria, "IpBloqueado");
            }

            return ValueTask.CompletedTask;
        };
        opcoes.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto => Particao(contexto, limites));
    }

    /// <summary>
    /// Sem cookie de sessão: balde "nova sessão" (conter quem cria sessão em massa). Com cookie: leitura ou escrita.
    /// O health check e as páginas de erro (reexecutadas pelo próprio servidor) ficam fora do limite.
    /// </summary>
    public static RateLimitPartition<string> Particao(HttpContext contexto, LimitesDeTaxa limites)
    {
        if (contexto.Request.Path.StartsWithSegments("/saude") || contexto.Request.Path.StartsWithSegments("/erro"))
        {
            return RateLimitPartition.GetNoLimiter("sem-limite");
        }

        var ip = contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        if (!contexto.Request.Cookies.ContainsKey(SessaoMiddleware.NomeDoCookie))
        {
            return Janela("s:" + ip, limites.NovaSessao, limites.Janela);
        }

        return HttpMethods.IsGet(contexto.Request.Method) || HttpMethods.IsHead(contexto.Request.Method)
            ? Janela("r:" + ip, limites.Leitura, limites.Janela)
            : Janela("w:" + ip, limites.Escrita, limites.Janela);
    }

    private static RateLimitPartition<string> Janela(string chave, int limite, TimeSpan janela) =>
        RateLimitPartition.GetFixedWindowLimiter(chave, _ => new FixedWindowRateLimiterOptions { PermitLimit = limite, Window = janela, QueueLimit = 0 });
}
