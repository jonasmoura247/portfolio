using System;
using System.Linq;
using System.Threading;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Infraestrutura.Dados;

/// <summary>Banco em memória de uma sessão: nasce da cópia do modelo e some com a sessão (vault: Sessao-e-Isolamento).</summary>
public sealed class BancoDaSessao(SqliteConnection conexao) : IDisposable
{
    public SqliteConnection Conexao => conexao;

    public BancadaDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<BancadaDbContext>().UseSqlite(conexao).Options);

    public void Registrar(EventoDeAuditoria evento)
    {
        using var db = NovoContexto();
        db.Add(evento);
        db.SaveChanges();
    }

    public void Dispose() => conexao.Dispose();
}

/// <summary>Banco-modelo: migrado e semeado uma vez na subida. Cada sessão recebe uma cópia, com os textos cifrados na chave dela.</summary>
public sealed class ModeloDoBanco : IDisposable
{
    private readonly SqliteConnection _modelo = Nova();
    private readonly Lock _trava = new();

    public ModeloDoBanco()
    {
        using var db = new BancoDaSessao(_modelo).NovoContexto();
        db.Database.Migrate();
        Semente.Povoar(db);
    }

    public BancoDaSessao Copiar(ICifra cifra, string cenario)
    {
        var conexao = Nova();
        lock (_trava)
        {
            _modelo.BackupDatabase(conexao);
        }

        var banco = new BancoDaSessao(conexao);
        using var db = banco.NovoContexto();
        CifrarTextos(db, cifra);
        if (cenario == "cq")
        {
            Semente.ReprovarControle(db);
        }

        db.SaveChanges();
        return banco;
    }

    public void Dispose() => _modelo.Dispose();

    private static SqliteConnection Nova()
    {
        var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();
        return conexao;
    }

    /// <summary>A semente guarda texto com o prefixo "claro:"; aqui ele vira cifra com a chave da sessão, atrelada ao registro.</summary>
    private static void CifrarTextos(BancadaDbContext db, ICifra cifra)
    {
        foreach (var e in db.Set<Evolucao>().ToList())
        {
            db.Entry(e).Property(x => x.TextoCifrado).CurrentValue = cifra.Cifrar(e.TextoCifrado[Semente.Prefixo.Length..], e.DadosAssociados);
        }

        foreach (var a in db.Set<Alergia>().ToList())
        {
            db.Entry(a).Property(x => x.ReacaoCifrada).CurrentValue = cifra.Cifrar(a.ReacaoCifrada![Semente.Prefixo.Length..], a.DadosAssociados);
        }
    }
}
