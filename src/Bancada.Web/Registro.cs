using System;
using System.IO;
using System.Net;
using Bancada.Aplicacao;
using Bancada.Infraestrutura.Dados;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Bancada.Web;

public static class Registro
{
    public static void Kestrel(KestrelServerOptions opcoes)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        opcoes.AddServerHeader = false;
        opcoes.Limits.MaxRequestBodySize = ProtecaoDeEntrada.TamanhoMaximoDoCorpo;
    }

    /// <summary>
    /// X-Forwarded-For e X-Forwarded-Proto só valem vindos dos proxies listados em Bancada:ProxiesConfiaveis (IPs ou redes
    /// CIDR, separados por vírgula). Sem a lista, os cabeçalhos são ignorados: qualquer um poderia forjá-los para fugir do
    /// limite de taxa. Entrada inválida derruba a subida (falha fechado).
    /// </summary>
    public static ForwardedHeadersOptions? CabecalhosEncaminhados(string? proxies)
    {
        var itens = (proxies ?? string.Empty).Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (itens.Length == 0)
        {
            return null;
        }

        var opcoes = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, ForwardLimit = 1 };
        opcoes.KnownProxies.Clear();
        opcoes.KnownNetworks.Clear();
        foreach (var item in itens)
        {
            if (item.Contains('/', StringComparison.Ordinal))
            {
                var rede = System.Net.IPNetwork.Parse(item);
                opcoes.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(rede.BaseAddress, rede.PrefixLength));
            }
            else
            {
                opcoes.KnownProxies.Add(IPAddress.Parse(item));
            }
        }

        return opcoes;
    }

    /// <summary>Pasta onde o executor roda os testes: a configurada ou a raiz do repositório.</summary>
    public static string RaizDosTestes(string? configurada, string conteudo) => Path.GetFullPath(configurada ?? Path.Combine(conteudo, "..", ".."));

    /// <summary>Casos de uso e repositórios vivem por requisição, ligados ao banco da sessão do visitante.</summary>
    public static void Casos(IServiceCollection servicos)
    {
        servicos.AddScoped(p => new ContextoDaSessao { Sessao = p.GetRequiredService<IHttpContextAccessor>().HttpContext!.Sessao() });
        servicos.AddScoped(p => ((BancoDaSessao)p.GetRequiredService<ContextoDaSessao>().Sessao.Banco!).NovoContexto());
        servicos.AddScoped<Repositorios>();
        Porta<IUnidadeDeTrabalho>(servicos);
        Porta<IPacientes>(servicos);
        Porta<IAgenda>(servicos);
        Porta<IProntuario>(servicos);
        Porta<IPedidos>(servicos);
        Porta<IAmostras>(servicos);
        Porta<IEquipamentos>(servicos);
        Porta<IResultados>(servicos);
        Porta<IQualidade>(servicos);
        Porta<IRegistroDeAuditoria>(servicos);
        Porta<IEsquemaDoBanco>(servicos);
        foreach (var caso in new[]
        {
            typeof(ServicoDePacientes), typeof(ServicoDeAgenda), typeof(ServicoDeAtendimento), typeof(ServicoDeProntuario), typeof(ServicoDePedidos), typeof(ServicoDeColeta),
            typeof(ServicoDeSoroteca), typeof(ServicoDeEquipamentos), typeof(ServicoDeQualidade), typeof(ServicoDeResultados), typeof(ServicoDeOperacao), typeof(ConsultaDeAuditoria),
        })
        {
            servicos.AddScoped(caso);
        }
    }

    private static void Porta<T>(IServiceCollection servicos)
        where T : class => servicos.AddScoped<T>(p => (T)(object)p.GetRequiredService<Repositorios>());
}
