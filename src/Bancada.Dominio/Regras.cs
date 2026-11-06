using System;
using System.Collections.Generic;

namespace Bancada.Dominio;

/// <summary>Regra de negócio violada. A chave é um texto do catálogo; a interface a mostra no idioma do visitante.</summary>
public sealed class ViolacaoDeRegra(string chave) : Exception(chave)
{
    public string Chave { get; } = chave;
}

public sealed class NaoEncontrado(string alvo) : Exception(alvo);

/// <summary>Transições permitidas de um agregado. O que não está na tabela é proibido (RN-008).</summary>
public sealed class MaquinaDeEstados<TEstado, TEvento>(params (TEstado De, TEvento Evento, TEstado Para)[] arestas)
    where TEstado : struct, Enum
    where TEvento : struct, Enum
{
    private readonly Dictionary<(TEstado, TEvento), TEstado> _tabela = Montar(arestas);

    public bool Permite(TEstado atual, TEvento evento) => _tabela.ContainsKey((atual, evento));

    public TEstado Proximo(TEstado atual, TEvento evento) =>
        _tabela.TryGetValue((atual, evento), out var para) ? para : throw new ViolacaoDeRegra("regra.transicao");

    private static Dictionary<(TEstado, TEvento), TEstado> Montar((TEstado De, TEvento Evento, TEstado Para)[] arestas)
    {
        var tabela = new Dictionary<(TEstado, TEvento), TEstado>();
        foreach (var (de, evento, para) in arestas)
        {
            tabela[(de, evento)] = para;
        }

        return tabela;
    }
}

public static class Texto
{
    /// <summary>Exige tamanho entre mínimo e máximo depois de aparar os espaços. Devolve o texto aparado.</summary>
    public static string Exigir(string? texto, int minimo, int maximo, string chaveDoErro)
    {
        var aparado = (texto ?? string.Empty).Trim();
        return aparado.Length < minimo || aparado.Length > maximo ? throw new ViolacaoDeRegra(chaveDoErro) : aparado;
    }
}
