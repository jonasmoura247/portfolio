using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bancada.Web;

/// <summary>Registro não encontrado é 404, sem dizer se existe em outra sessão (nunca existe: o banco é de cada um).</summary>
public sealed class FiltroDeNaoEncontrado : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is NaoEncontrado)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status404NotFound);
            context.ExceptionHandled = true;
        }
    }
}

/// <summary>Token de antifalsificação inválido vira evento de segurança na auditoria da sessão.</summary>
public sealed class FiltroDeAntifalsificacao(ServicoDeAuditoria auditoria) : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is IAntiforgeryValidationFailedResult)
        {
            auditoria.Registrar(context.HttpContext.Sessao(), "AntifalsificacaoInvalido", "Sessao", "-", "{}", "Bloqueado");
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}

/// <summary>Quem estoura o limite de taxa cinco vezes em um minuto fica dez minutos bloqueado.</summary>
public sealed class BloqueioDeIp(IRelogio relogio)
{
    public const int Tolerancia = 5;

    /// <summary>Acima disso, as entradas vencidas são podadas: muitos IPs diferentes não viram memória sem fim.</summary>
    public const int PodarAcimaDe = 10_000;

    private static readonly TimeSpan Janela = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Duracao = TimeSpan.FromMinutes(10);

    private readonly Dictionary<string, (int Vezes, DateTimeOffset Inicio, DateTimeOffset? Ate)> _ips = [];

    public int Quantidade
    {
        get
        {
            lock (_ips)
            {
                return _ips.Count;
            }
        }
    }

    public bool Bloqueado(string ip)
    {
        lock (_ips)
        {
            return _ips.TryGetValue(ip, out var e) && e.Ate > relogio.Agora;
        }
    }

    /// <summary>Conta uma recusa. Devolve verdadeiro quando esta recusa acabou de causar o bloqueio.</summary>
    public bool Contar(string ip)
    {
        lock (_ips)
        {
            var agora = relogio.Agora;
            if (_ips.Count >= PodarAcimaDe)
            {
                foreach (var vencido in _ips.Where(x => agora - x.Value.Inicio >= Janela && !(x.Value.Ate > agora)).Select(x => x.Key).ToList())
                {
                    _ips.Remove(vencido);
                }
            }

            var atual = _ips.TryGetValue(ip, out var e) && agora - e.Inicio < Janela ? e : (0, agora, null);
            var vezes = atual.Item1 + 1;
            var bloqueia = vezes >= Tolerancia;
            _ips[ip] = (vezes, atual.Item2, bloqueia ? agora + Duracao : null);
            return bloqueia;
        }
    }
}

/// <summary>Barreiras antes de tudo: IP bloqueado (429) e corpo grande demais (413), sem chegar a processar nada.</summary>
public sealed class ProtecaoDeEntrada(RequestDelegate proximo, BloqueioDeIp bloqueio, RepositorioDeSessoes sessoes, ServicoDeAuditoria auditoria)
{
    public const long TamanhoMaximoDoCorpo = 16 * 1024;

    public Task InvokeAsync(HttpContext contexto)
    {
        if (bloqueio.Bloqueado(Ip(contexto)) && !contexto.Request.Path.StartsWithSegments("/saude"))
        {
            contexto.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return Task.CompletedTask;
        }

        if (contexto.Request.ContentLength > TamanhoMaximoDoCorpo)
        {
            AnotarEvento(contexto, sessoes, auditoria, "CorpoGrandeDemais");
            contexto.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return Task.CompletedTask;
        }

        return proximo(contexto);
    }

    /// <summary>
    /// Quem é o cliente para limites e bloqueio. Atrás de proxy, o IP já vem do X-Forwarded-For pelo UseForwardedHeaders
    /// (só de proxies configurados). IPv6 conta por /64: quem recebe um /64 do provedor tem bilhões de endereços para girar.
    /// </summary>
    public static string Ip(HttpContext contexto) => ChaveDoCliente(contexto.Connection.RemoteIpAddress);

    public static string ChaveDoCliente(IPAddress? ip)
    {
        if (ip is null)
        {
            return "desconhecido";
        }

        if (ip.IsIPv4MappedToIPv6)
        {
            return ip.MapToIPv4().ToString();
        }

        if (ip.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return ip.ToString();
        }

        var bytes = ip.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return new IPAddress(bytes).ToString() + "/64";
    }

    /// <summary>Anota na sessão do cookie, se houver. Quem nem tem sessão não gera estado novo.</summary>
    public static void AnotarEvento(HttpContext contexto, RepositorioDeSessoes sessoes, ServicoDeAuditoria auditoria, string evento)
    {
        contexto.Request.Cookies.TryGetValue(SessaoMiddleware.NomeDoCookie, out var id);
        if (sessoes.Encontrar(id) is { } sessao)
        {
            auditoria.Anotar(sessao, evento);
        }
    }
}
