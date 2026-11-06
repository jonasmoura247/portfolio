using System;
using System.Collections.Generic;
using Bancada.Aplicacao;
using Bancada.Dominio;

namespace Bancada.Testes.Aplicacao;

/// <summary>Dublês escritos à mão (sem biblioteca de mock): relógio manual, ids sequenciais e fábrica que rastreia o descarte.</summary>
internal sealed class RelogioManual : IRelogio
{
    public DateTimeOffset Agora { get; set; } = new(2026, 11, 4, 13, 15, 0, TimeSpan.Zero);

    public void Avancar(TimeSpan tempo) => Agora += tempo;
}

internal sealed class GeradorSequencial : IGeradorDeId
{
    private int _n;

    public string Novo() => $"id-{++_n}";
}

internal sealed class CifraFalsa : ICifra
{
    public string Cifrar(string texto, string dadosAssociados) => "cifrado:" + texto;

    public string Decifrar(string valor, string dadosAssociados) => valor["cifrado:".Length..];
}

internal sealed class RecursoRastreado : IDisposable
{
    public int Descartes { get; private set; }

    public void Dispose() => Descartes++;
}

internal sealed class FabricaDeTeste : IFabricaDeSessao
{
    public List<RecursoRastreado> Recursos { get; } = [];

    public Sessao Criar(string id, DateTimeOffset agora, string cenario)
    {
        var recurso = new RecursoRastreado();
        Recursos.Add(recurso);
        return new Sessao(id, Papel.Recepcao, "REC-01", agora, new CifraFalsa(), [recurso]);
    }
}
