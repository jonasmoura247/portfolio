using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bancada.Dominio;
using Bancada.Web;

namespace Bancada.Testes.Guardas;

/// <summary>Idiomas (paridade, chave usada, texto solto) e contraste do tema (vault: Design-do-Sistema e Convencoes-de-Codigo).</summary>
public partial class TextosETemaTestes
{
    private static readonly string[] CodigosDeErro = ["400", "403", "404", "429", "500"];
    private static readonly string[] PrefixosDinamicos = ["menu.", "ajuda.", "papel.", "erro.", "estado.", "dia.", "sexo.", "material.", "exame.", "rejeicao.", "custodia.", "cenario.", "roteiro.", "executor.", "fluxo.", "auditoria.grupo.", "painel.aba."];

    private static IEnumerable<string> Views() => Repositorio.Arquivos(Path.Combine("src", "Bancada.Web", "Views"), "*.cshtml");

    [Test]
    public void Catalogo_TodaChaveTemTodosOsIdiomasPreenchidos()
    {
        foreach (var chave in CatalogoDeTextos.Chaves)
        {
            foreach (var idioma in CatalogoDeTextos.Idiomas)
            {
                Assert.That(CatalogoDeTextos.Obter(chave, idioma), Is.Not.Empty, $"{chave} em {idioma}");
            }
        }
    }

    [Test]
    public void Catalogo_NaoTemChaveSobrando()
    {
        var fonte = string.Concat(Repositorio.Arquivos("src", "*.cs").Concat(Views()).Select(File.ReadAllText));

        var sobrando = CatalogoDeTextos.Chaves
            .Where(c => !PrefixosDinamicos.Any(p => c.StartsWith(p, StringComparison.Ordinal)))
            .Where(c => !fonte.Contains($"\"{c}\"", StringComparison.Ordinal))
            .ToList();

        Assert.That(sobrando, Is.Empty, "chave não usada em nenhuma View nem código");
    }

    [Test]
    public void Views_TodaChaveCitadaExisteNoCatalogo()
    {
        var citadas = Views().SelectMany(v => ChaveCitada().Matches(File.ReadAllText(v)).Select(m => m.Groups[1].Value)).Distinct().ToList();

        Assert.That(citadas, Is.Not.Empty);
        Assert.That(citadas.Except(CatalogoDeTextos.Chaves), Is.Empty);
    }

    [Test]
    public void Regras_TodaChaveDeErroOuAvisoExisteNoCatalogo()
    {
        var citadas = Repositorio.Arquivos("src", "*.cs").SelectMany(a => ChaveDeRegra().Matches(File.ReadAllText(a)).Select(m => m.Groups[1].Value)).Distinct().ToList();

        Assert.That(citadas, Has.Count.GreaterThan(30));
        Assert.That(citadas.Except(CatalogoDeTextos.Chaves), Is.Empty);
    }

    [Test]
    public void Views_TodoFormularioQueEscreveLevaOTokenAntifalsificacao()
    {
        var sem = Views().SelectMany(v => FormularioPost().Matches(File.ReadAllText(v)).Select(m => (v, m.Value)))
            .Where(x => !x.Value.Contains("asp-antiforgery", StringComparison.Ordinal) && !x.Value.Contains("asp-controller", StringComparison.Ordinal))
            .Select(x => $"{Path.GetFileName(x.v)}: {x.Value}")
            .ToList();

        Assert.That(sem, Is.Empty, "form com action direto não ganha token sozinho");
    }

    [Test]
    public void Views_ExpressaoColadaEmLetraUsaParenteses()
    {
        var coladas = Views().SelectMany(v => ExpressaoComoEmail().Matches(File.ReadAllText(v)).Select(m => $"{Path.GetFileName(v)}: {m.Value}")).ToList();

        Assert.That(coladas, Is.Empty, "o Razor lê x@y como e-mail e imprime o código; use x@(y)");
    }

    [Test]
    public void Views_NaoUsamEstiloInline()
    {
        var com = Views().Where(v => File.ReadAllText(v).Contains(" style=", StringComparison.Ordinal)).ToList();

        Assert.That(com, Is.Empty, "a CSP não permite style inline");
    }

    [Test]
    public void Catalogo_CobreModulosPapeisECodigosDeErro()
    {
        var esperadas = Modulos.Todos.Select(m => m.TextoChave)
            .Concat(Enum.GetNames<Papel>().Select(p => "papel." + p))
            .Concat(CodigosDeErro.Select(c => "erro." + c));

        Assert.That(esperadas.Except(CatalogoDeTextos.Chaves), Is.Empty);
    }

    [Test]
    public void Views_NaoTemTextoSoltoForaDoCatalogo()
    {
        var soltos = new List<string>();
        foreach (var view in Views())
        {
            var texto = Comentarios().Replace(File.ReadAllText(view), string.Empty).Replace("=>", string.Empty, StringComparison.Ordinal);
            texto = BlocoDeCodigo().Replace(texto, string.Empty);
            soltos.AddRange(TextoEntreTags().Matches(texto).Select(m => $"{Path.GetFileName(view)}: {m.Groups[1].Value.Trim()}"));
            soltos.AddRange(AtributoComTexto().Matches(texto).Select(m => $"{Path.GetFileName(view)}: {m.Value}"));
        }

        Assert.That(soltos, Is.Empty, "todo texto visível vem do catálogo (T[\"chave\"])");
    }

    [Test]
    public void Tema_TextoEAcentosPassamNoContrasteAA_NosDoisTemas()
    {
        var css = File.ReadAllText(Path.Combine(Repositorio.Raiz, "src", "Bancada.Web", "wwwroot", "css", "estilo.css"));
        var claro = Tokens(Bloco(css, @":root \{"));
        var escuro = Tokens(Bloco(css, @":root\[data-theme=""dark""\] \{"));
        var texto = new[] { "--texto", "--texto-2", "--acento-texto", "--acento-2", "--acento-3", "--ok", "--atencao", "--perigo" };
        var fundos = new[] { "--fundo", "--superficie", "--superficie-2" };

        foreach (var (nome, tema) in new[] { ("claro", claro), ("escuro", escuro) })
        {
            foreach (var t in texto)
            {
                foreach (var f in fundos)
                {
                    Assert.That(Contraste(tema[t], tema[f]), Is.GreaterThanOrEqualTo(4.5), $"{nome}: {t} sobre {f}");
                }
            }

            Assert.That(Contraste(tema["--sobre-acento"], tema["--acento"]), Is.GreaterThanOrEqualTo(4.5), $"{nome}: botão primário");
        }
    }

    [Test]
    public void Contraste_CalculoConfereComValoresConhecidos()
    {
        Assert.That(Contraste("#000000", "#ffffff"), Is.EqualTo(21).Within(0.01));
        Assert.That(Contraste("#ffffff", "#ffffff"), Is.EqualTo(1).Within(0.01));
        Assert.That(Contraste("#777777", "#ffffff"), Is.EqualTo(4.48).Within(0.02));
    }

    private static string Bloco(string css, string abertura)
    {
        var inicio = Regex.Match(css, abertura).Index;
        return css[inicio..css.IndexOf('}', inicio)];
    }

    private static Dictionary<string, string> Tokens(string bloco) =>
        TokenDeCor().Matches(bloco).ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value, StringComparer.Ordinal);

    private static double Contraste(string a, string b)
    {
        var (la, lb) = (Luminancia(a), Luminancia(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminancia(string hex)
    {
        double Canal(int i)
        {
            var c = int.Parse(hex.AsSpan(1 + (i * 2), 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Canal(0)) + (0.7152 * Canal(1)) + (0.0722 * Canal(2));
    }

    [GeneratedRegex(@"\bT\[""([^""]+)""\]")]
    private static partial Regex ChaveCitada();

    [GeneratedRegex(@"""([a-z]+\.(?:erro|ok)\.[a-zA-Z]+)""")]
    private static partial Regex ChaveDeRegra();

    [GeneratedRegex(@"[A-Za-z0-9]@[A-Za-z]")]
    private static partial Regex ExpressaoComoEmail();

    [GeneratedRegex(@"<form\b[^>]*method=""post""[^>]*>")]
    private static partial Regex FormularioPost();

    [GeneratedRegex(@"@\*.*?\*@", RegexOptions.Singleline)]
    private static partial Regex Comentarios();

    [GeneratedRegex(@"@\{.*?\n\}", RegexOptions.Singleline)]
    private static partial Regex BlocoDeCodigo();

    [GeneratedRegex(@">([^<>@{}]*\p{L}[^<>@{}]*)<")]
    private static partial Regex TextoEntreTags();

    [GeneratedRegex(@"\b(?:title|alt|aria-label|placeholder)=""[^""@]*\p{L}[^""@]*""")]
    private static partial Regex AtributoComTexto();

    [GeneratedRegex(@"(--[a-z0-9-]+):\s*(#[0-9a-fA-F]{6})")]
    private static partial Regex TokenDeCor();
}
