using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Bancada.Web;

/// <summary>
/// Cabeçalhos de segurança em toda resposta (vault: Cabecalhos-e-Configuracao-Segura).
/// CSP sem 'unsafe-inline': nenhum script nem estilo inline no sistema.
/// </summary>
public sealed class CabecalhosDeSeguranca(RequestDelegate proximo)
{
    public static readonly (string Nome, string Valor)[] Padrao =
    [
        ("Content-Security-Policy",
            "default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; media-src 'self'; " +
            "connect-src 'self'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'; object-src 'none'"),
        ("Strict-Transport-Security", "max-age=31536000; includeSubDomains"),
        ("X-Content-Type-Options", "nosniff"),
        ("Referrer-Policy", "no-referrer"),
        ("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=(), usb=()"),
        ("Cross-Origin-Opener-Policy", "same-origin"),
        ("Cross-Origin-Resource-Policy", "same-origin"),
        ("Cache-Control", "no-store"),
    ];

    public Task InvokeAsync(HttpContext contexto)
    {
        foreach (var (nome, valor) in Padrao)
        {
            contexto.Response.Headers[nome] = valor;
        }

        return proximo(contexto);
    }
}
