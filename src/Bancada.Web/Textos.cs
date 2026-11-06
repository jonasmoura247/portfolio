using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Bancada.Web;

/// <summary>Textos da interface por chave e idioma. Nada de texto fixo nas Views: chave que não existe é erro, não texto vazio.</summary>
public interface ITextos
{
    string this[string chave] { get; }
}

public sealed class TextosDaSessao(IHttpContextAccessor acessor) : ITextos
{
    public string this[string chave] => CatalogoDeTextos.Obter(chave, acessor.HttpContext!.Sessao().Idioma);
}

public static class CatalogoDeTextos
{
    public static readonly string[] Idiomas = ["pt", "en"];

    private static readonly Lazy<Dictionary<string, string[]>> Dados = new(Carregar);

    public static IReadOnlyCollection<string> Chaves => Dados.Value.Keys;

    public static string Obter(string chave, string idioma)
    {
        if (!Dados.Value.TryGetValue(chave, out var porIdioma))
        {
            throw new KeyNotFoundException($"Texto sem chave: {chave}");
        }

        var indice = Array.IndexOf(Idiomas, idioma);
        return indice >= 0 ? porIdioma[indice] : throw new KeyNotFoundException($"Texto '{chave}' sem idioma '{idioma}'");
    }

    private static Dictionary<string, string[]> Carregar()
    {
        using var fluxo = Assembly.GetExecutingAssembly().GetManifestResourceStream("Bancada.Web.Recursos.textos.json")!;
        using var leitor = new StreamReader(fluxo);
        return JsonSerializer.Deserialize<Dictionary<string, string[]>>(leitor.ReadToEnd())!;
    }
}
