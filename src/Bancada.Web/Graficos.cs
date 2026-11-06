using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Html;

namespace Bancada.Web;

public sealed record Dado(string Nome, double Valor, string Cor);

/// <summary>Gráficos em SVG montados no servidor: só atributos, sem style, então a CSP segue sem inline. Rótulos sempre escapados.</summary>
public static class Graficos
{
    public static IHtmlContent Barras(IReadOnlyList<Dado> dados, string unidade, string rotulo)
    {
        const int Largura = 520, Esquerda = 130, Direita = 80, Altura = 34;
        var maximo = Math.Max(dados.Select(d => d.Valor).DefaultIfEmpty(0).Max(), 1);
        var s = new StringBuilder();
        for (var i = 0; i < dados.Count; i++)
        {
            var d = dados[i];
            var y = (i * Altura) + 4;
            var w = Math.Max((Largura - Esquerda - Direita) * d.Valor / maximo, 2);
            s.Append(Inv($"<text class=\"tx\" x=\"0\" y=\"{y + 15}\">{Esc(d.Nome)}</text>"));
            s.Append(Inv($"<rect class=\"b-{d.Cor} cresce\" x=\"{Esquerda}\" y=\"{y}\" width=\"{w:0.#}\" height=\"22\" rx=\"4\"><title>{Esc(d.Nome)}: {d.Valor} {Esc(unidade)}</title></rect>"));
            s.Append(Inv($"<text class=\"mono tx\" x=\"{Esquerda + w + 8:0.#}\" y=\"{y + 15}\">{d.Valor} {Esc(unidade)}</text>"));
        }

        return Svg(Largura, Math.Max((dados.Count * Altura) + 8, 40), rotulo, s.ToString());
    }

    public static IHtmlContent Rosca(IReadOnlyList<Dado> fatias, string centro, string rodape, string rotulo)
    {
        const int Cx = 84, Cy = 84, Raio = 56;
        var circunferencia = 2 * Math.PI * Raio;
        var total = Math.Max(fatias.Sum(f => f.Valor), 1);
        var s = new StringBuilder();
        var acumulado = 0.0;
        for (var i = 0; i < fatias.Count; i++)
        {
            var f = fatias[i];
            var tamanho = circunferencia * f.Valor / total;
            var traco = Math.Max(tamanho - 2, 0);
            s.Append(Inv($"<circle class=\"fatia s-{f.Cor}\" cx=\"{Cx}\" cy=\"{Cy}\" r=\"{Raio}\" stroke-dasharray=\"{traco:0.##} {circunferencia - traco:0.##}\" stroke-dashoffset=\"{-acumulado:0.##}\" transform=\"rotate(-90 {Cx} {Cy})\"><title>{Esc(f.Nome)}: {f.Valor}</title></circle>"));
            acumulado += tamanho;
            var y = 34 + (i * 30);
            s.Append(Inv($"<rect class=\"b-{f.Cor}\" x=\"190\" y=\"{y - 10}\" width=\"12\" height=\"12\" rx=\"3\"/><text class=\"tx\" x=\"210\" y=\"{y}\">{Esc(f.Nome)}</text><text class=\"mono\" x=\"210\" y=\"{y + 14}\">{f.Valor} · {100 * f.Valor / total:0}%</text>"));
        }

        s.Append(Inv($"<text class=\"grande\" x=\"{Cx}\" y=\"{Cy + 4}\" text-anchor=\"middle\">{Esc(centro)}</text><text class=\"mono\" x=\"{Cx}\" y=\"{Cy + 22}\" text-anchor=\"middle\">{Esc(rodape)}</text>"));
        return Svg(430, Math.Max(168, 40 + (fatias.Count * 30)), rotulo, s.ToString());
    }

    /// <summary>Levey-Jennings: uma linha por desvio padrão, pontos coloridos pela distância da média.</summary>
    public static IHtmlContent LeveyJennings(IReadOnlyList<double> z, double media, double desvio, string rotulo)
    {
        const int Largura = 640, Alto = 300, Esquerda = 62, Direita = 14, Topo = 14, Base = 270;
        const double Limite = 3.4;
        var pontos = z.TakeLast(24).ToList();
        double Y(double v) => Topo + ((Limite - Math.Clamp(v, -Limite + 0.1, Limite - 0.1)) / (2 * Limite) * (Base - Topo));
        double X(int i) => Esquerda + ((Largura - Esquerda - Direita) * (i + 0.5) / pontos.Count);

        var s = new StringBuilder();
        foreach (var (k, classe) in new[] { (3, "lj-3"), (2, "lj-2"), (1, "lj-1"), (0, "lj-media"), (-1, "lj-1"), (-2, "lj-2"), (-3, "lj-3") })
        {
            s.Append(Inv($"<line class=\"{classe}\" x1=\"{Esquerda}\" x2=\"{Largura - Direita}\" y1=\"{Y(k):0.#}\" y2=\"{Y(k):0.#}\"/><text class=\"mono\" x=\"{Esquerda - 8}\" y=\"{Y(k) + 4:0.#}\" text-anchor=\"end\">{k:+0;-0;0}s</text><text class=\"mono\" x=\"{Largura - Direita}\" y=\"{Y(k) - 4:0.#}\" text-anchor=\"end\">{media + (k * desvio):0.00}</text>"));
        }

        s.Append("<polyline class=\"lj-linha\" points=\"").AppendJoin(' ', pontos.Select((v, i) => Inv($"{X(i):0.#},{Y(v):0.#}"))).Append("\"/>");
        for (var i = 0; i < pontos.Count; i++)
        {
            var v = pontos[i];
            var cor = Math.Abs(v) > 3 ? "b-pe" : Math.Abs(v) > 2 ? "b-at" : "b-a";
            s.Append(Inv($"<circle class=\"{cor}\" cx=\"{X(i):0.#}\" cy=\"{Y(v):0.#}\" r=\"{(i == pontos.Count - 1 ? 6 : 4)}\"><title>{media + (v * desvio):0.00} ({v:+0.0;-0.0;0.0}s)</title></circle>"));
        }

        return Svg(Largura, Alto, rotulo, s.ToString());
    }

    /// <summary>Etapas do laboratório em setas; as visitadas ficam preenchidas.</summary>
    public static IHtmlContent Fluxo(IReadOnlyList<(string Nome, bool Feito)> etapas, string rotulo)
    {
        const int Largura = 66, Vao = 6;
        var s = new StringBuilder();
        for (var i = 0; i < etapas.Count; i++)
        {
            var x = i * (Largura + Vao);
            var (nome, feito) = etapas[i];
            s.Append(Inv($"<path class=\"{(feito ? "b-t" : "caixa")}\" d=\"M{x} 6h{Largura - 8}l8 14l-8 14h-{Largura - 8}l8 -14z\"/><text class=\"mono {(feito ? "sobre" : "tx")}\" x=\"{x + (Largura / 2) + 2}\" y=\"24\" text-anchor=\"middle\">{Esc(nome)}</text>"));
        }

        return Svg(etapas.Count * (Largura + Vao), 40, rotulo, s.ToString());
    }

    /// <summary>Camadas e setas de dependência, a partir das referências reais dos assemblies.</summary>
    public static IHtmlContent Dependencias(IReadOnlyList<(string Nome, IReadOnlyList<string> Usa)> camadas, string rotulo)
    {
        const int Largura = 150, Alto = 34, Passo = 60;
        var posicao = camadas.Select((c, i) => (c.Nome, X: 10 + ((i % 2) * 190), Y: 6 + (i * Passo))).ToDictionary(c => c.Nome);
        var s = new StringBuilder("<defs><marker id=\"ponta\" viewBox=\"0 0 8 8\" refX=\"7\" refY=\"4\" markerWidth=\"8\" markerHeight=\"8\" orient=\"auto\"><path class=\"pt\" d=\"M0 0L8 4L0 8z\"/></marker></defs>");
        foreach (var (nome, usa) in camadas)
        {
            var de = posicao[nome];
            foreach (var alvo in usa)
            {
                var para = posicao[alvo];
                s.Append(Inv($"<path class=\"lig\" d=\"M{de.X + (Largura / 2)} {de.Y + Alto}L{para.X + (Largura / 2)} {para.Y}\" marker-end=\"url(#ponta)\"/>"));
            }
        }

        foreach (var (nome, c) in posicao)
        {
            s.Append(Inv($"<rect class=\"caixa\" x=\"{c.X}\" y=\"{c.Y}\" width=\"{Largura}\" height=\"{Alto}\" rx=\"7\"/><text class=\"mono tx\" x=\"{c.X + (Largura / 2)}\" y=\"{c.Y + 21}\" text-anchor=\"middle\">{Esc(nome)}</text>"));
        }

        return Svg(360, (camadas.Count * Passo) + 10, rotulo, s.ToString());
    }

    /// <summary>Barras do Code 128: a sequência de larguras alterna barra e espaço, começando por barra.</summary>
    public static IHtmlContent CodigoDeBarras(string larguras, string rotulo)
    {
        var s = new StringBuilder();
        var x = 0;
        for (var i = 0; i < larguras.Length; i++)
        {
            var largura = larguras[i] - '0';
            if (i % 2 == 0)
            {
                s.Append(Inv($"<rect x=\"{x}\" y=\"0\" width=\"{largura}\" height=\"40\"/>"));
            }

            x += largura;
        }

        return new HtmlString(Inv($"<svg class=\"code128\" viewBox=\"0 0 {x} 40\" preserveAspectRatio=\"none\" role=\"img\" aria-label=\"{Esc(rotulo)}\">{s}</svg>"));
    }

    private static HtmlString Svg(int largura, int altura, string rotulo, string corpo) =>
        new(Inv($"<svg viewBox=\"0 0 {largura} {altura}\" role=\"img\" aria-label=\"{Esc(rotulo)}\">{corpo}</svg>"));

    private static string Esc(string texto) => WebUtility.HtmlEncode(texto);

    private static string Inv(FormattableString texto) => texto.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Cor do selo por estado: a mesma lógica do esboço aprovado. Estado desconhecido é neutro.</summary>
public static class Apresentacao
{
    private static readonly Dictionary<string, string> Selos = new()
    {
        ["Confirmado"] = "s-acento",
        ["ChegouCheckIn"] = "s-ok",
        ["EmAtendimento"] = "s-atencao",
        ["Faltou"] = "s-perigo",
        ["Coletado"] = "s-acento",
        ["Coletada"] = "s-acento",
        ["Armazenada"] = "s-teal",
        ["EmAnalise"] = "s-atencao",
        ["Processando"] = "s-atencao",
        ["Erro"] = "s-perigo",
        ["Preliminar"] = "s-atencao",
        ["ResultadoPreliminar"] = "s-atencao",
        ["CriticoConfirmado"] = "s-violeta",
        ["Validado"] = "s-ok",
        ["Liberado"] = "s-ok",
        ["Rejeitada"] = "s-perigo",
        ["Ok"] = "s-ok",
        ["Recusado"] = "s-perigo",
        ["Bloqueado"] = "s-perigo",
    };

    private static readonly Dictionary<char, string> Controles = new() { ['\x02'] = "STX", ['\x03'] = "ETX", ['\x17'] = "ETB", ['\r'] = "CR", ['\n'] = "LF" };

    public static string Selo(object estado) => Selos.GetValueOrDefault(estado.ToString()!, "s-neutro");

    public static string Flag(string flag) => flag switch
    {
        "N" => "f-n",
        "CL" or "CH" => "f-c",
        _ => "f-a",
    };

    public static string Rotulo(string flag) => flag switch
    {
        "L" => "L ▼",
        "H" => "H ▲",
        "CL" => "CL ▼▼",
        "CH" => "CH ▲▲",
        _ => flag,
    };

    /// <summary>Caracteres de controle do ASTM viram rótulo legível no monitor do protocolo.</summary>
    public static string Protocolo(string texto) => string.Concat(texto.Select(c => c < ' ' ? $"⟨{Controles.GetValueOrDefault(c, "?")}⟩" : c.ToString()));

    public static string Valor(double valor, int casas) => valor.ToString("N" + casas.ToString(CultureInfo.InvariantCulture), CultureInfo.GetCultureInfo("pt-BR"));

    public static string Faixa(double? minimo, double? maximo, int casas) => (minimo, maximo) switch
    {
        (double a, double b) => $"{Valor(a, casas)} a {Valor(b, casas)}",
        (null, double b) => $"< {Valor(b, casas)}",
        (double a, null) => $"> {Valor(a, casas)}",
        _ => "—",
    };
}
