using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bancada.Web;

namespace Bancada.Testes.Web;

internal static class Inteiros
{
    public static string Txt(this int valor) => valor.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>Um visitante pela rede: pega a página, lê o token do formulário e posta como um navegador faria.</summary>
internal sealed partial class Navegador(HttpClient cliente) : IDisposable
{
    public static LimitesDeTaxa SemLimite { get; } = new() { Leitura = 100000, Escrita = 100000, NovaSessao = 100000 };

    public HttpClient Cliente => cliente;

    public string UltimaPagina { get; private set; } = string.Empty;

    public async Task<(HttpStatusCode Status, string Html)> Ver(string url)
    {
        var r = await cliente.GetAsync(url);
        UltimaPagina = await r.Content.ReadAsStringAsync();
        return (r.StatusCode, UltimaPagina);
    }

    public async Task<HttpResponseMessage> Postar(string url, params (string Chave, string Valor)[] campos)
    {
        var (_, html) = await Ver("/bancada");
        return await AppDeTeste.Postar(cliente, url, AppDeTeste.TokenAntifalsificacao(html), campos);
    }

    /// <summary>Posta e segue o redirecionamento, devolvendo a página com a faixa de aviso.</summary>
    public async Task<string> PostarESeguir(string url, params (string Chave, string Valor)[] campos)
    {
        var r = await Postar(url, campos);
        return r.Headers.Location is { } destino ? (await Ver(destino.ToString())).Html : await r.Content.ReadAsStringAsync();
    }

    public async Task Assumir(string papel, string? operador = null)
    {
        await Postar("/sessao/papel", ("papel", papel));
        if (operador is not null)
        {
            await Postar("/sessao/operador", ("operador", operador));
        }
    }

    public static IReadOnlyList<int> Numeros(string html, string padrao)
    {
        var lista = new List<int>();
        foreach (Match m in Regex.Matches(html, padrao))
        {
            lista.Add(int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
        }

        return lista;
    }

    public void Dispose() => cliente.Dispose();
}
