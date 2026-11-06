using System;
using System.IO;
using Bancada.Aplicacao;
using Bancada.Infraestrutura.Dados;
using Microsoft.AspNetCore.Http;
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
