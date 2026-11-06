using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Bancada.Dominio;

namespace Bancada.Aplicacao;

public sealed class LimitesDeSessao
{
    public TimeSpan Ocioso { get; init; } = TimeSpan.FromMinutes(30);

    public TimeSpan Total { get; init; } = TimeSpan.FromHours(2);

    public int Maximo { get; init; } = 200;
}

public interface IGeradorDeId
{
    string Novo();
}

public interface IFabricaDeSessao
{
    Sessao Criar(string id, DateTimeOffset agora, string cenario);
}

/// <summary>
/// Guarda as sessões ativas (vault: Sessao-e-Isolamento). Expira por tempo ocioso e por tempo total,
/// limita o total de sessões descartando a mais ociosa e nunca adota um id que o cliente inventou.
/// </summary>
public sealed class RepositorioDeSessoes
{
    private readonly Dictionary<string, Sessao> _sessoes = [];
    private readonly Lock _trava = new();
    private readonly IRelogio _relogio;
    private readonly IGeradorDeId _gerador;
    private readonly IFabricaDeSessao _fabrica;
    private readonly LimitesDeSessao _limites;

    public RepositorioDeSessoes(IRelogio relogio, IGeradorDeId gerador, IFabricaDeSessao fabrica, LimitesDeSessao limites)
    {
        ArgumentNullException.ThrowIfNull(limites);
        if (limites.Maximo < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(limites), "O teto de sessões precisa ser pelo menos 1.");
        }

        _relogio = relogio;
        _gerador = gerador;
        _fabrica = fabrica;
        _limites = limites;
    }

    /// <summary>A sessão criada mais recentemente (operação e testes de ponta a ponta).</summary>
    public Sessao? UltimaCriada { get; private set; }

    public int Quantidade
    {
        get
        {
            lock (_trava)
            {
                return _sessoes.Count;
            }
        }
    }

    /// <summary>Devolve a sessão do id, se existir e estiver viva; senão cria uma nova (com id novo, nunca o recebido).</summary>
    public Sessao ObterOuCriar(string? id)
    {
        lock (_trava)
        {
            var agora = _relogio.Agora;
            if (id is not null && _sessoes.TryGetValue(id, out var existente))
            {
                if (!Expirada(existente, agora))
                {
                    existente.Tocar(agora);
                    return existente;
                }

                Descartar(existente);
            }

            return Criar(agora, Cenarios.Padrao);
        }
    }

    /// <summary>Procura sem criar nem tocar: usado para anotar eventos de quem já tem sessão.</summary>
    public Sessao? Encontrar(string? id)
    {
        lock (_trava)
        {
            return id is not null && _sessoes.TryGetValue(id, out var s) ? s : null;
        }
    }

    /// <summary>Descarta a sessão e cria outra já com o cenário pedido. Cada cenário começa do zero.</summary>
    public Sessao Recriar(Sessao atual, string cenario)
    {
        ArgumentNullException.ThrowIfNull(atual);
        lock (_trava)
        {
            Descartar(atual);
            return Criar(_relogio.Agora, cenario);
        }
    }

    /// <summary>Troca papel e operador e renova o id. O id antigo deixa de valer.</summary>
    public void Renovar(Sessao sessao, Papel papel, string operador)
    {
        ArgumentNullException.ThrowIfNull(sessao);
        if (!Operadores.Pertence(operador, papel))
        {
            throw new ArgumentException("O operador não pertence ao papel.", nameof(operador));
        }

        lock (_trava)
        {
            if (!_sessoes.Remove(sessao.Id))
            {
                throw new InvalidOperationException("A sessão não está mais ativa.");
            }

            sessao.DefinirIdentidade(_gerador.Novo(), papel, operador);
            sessao.Tocar(_relogio.Agora);
            _sessoes[sessao.Id] = sessao;
        }
    }

    public void Remover(string id)
    {
        lock (_trava)
        {
            if (_sessoes.TryGetValue(id, out var sessao))
            {
                Descartar(sessao);
            }
        }
    }

    /// <summary>Descarta as sessões vencidas. Chamado de vez em quando; ObterOuCriar também expira a que consulta.</summary>
    public int Varrer()
    {
        lock (_trava)
        {
            var agora = _relogio.Agora;
            var vencidas = _sessoes.Values.Where(s => Expirada(s, agora)).ToList();
            vencidas.ForEach(Descartar);
            return vencidas.Count;
        }
    }

    private Sessao Criar(DateTimeOffset agora, string cenario)
    {
        while (_sessoes.Count >= _limites.Maximo)
        {
            Descartar(_sessoes.Values.MinBy(s => s.UltimoAcesso)!);
        }

        var sessao = _fabrica.Criar(_gerador.Novo(), agora, cenario);
        _sessoes[sessao.Id] = sessao;
        UltimaCriada = sessao;
        return sessao;
    }

    private bool Expirada(Sessao s, DateTimeOffset agora) => agora - s.UltimoAcesso > _limites.Ocioso || agora - s.CriadaEm > _limites.Total;

    private void Descartar(Sessao sessao)
    {
        _sessoes.Remove(sessao.Id);
        sessao.Dispose();
    }
}
