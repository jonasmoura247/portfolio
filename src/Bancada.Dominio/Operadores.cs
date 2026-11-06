using System.Collections.Generic;
using System.Linq;

namespace Bancada.Dominio;

/// <summary>Profissional fictício que o visitante "assume" dentro de um papel. Não existe senha nem conta.</summary>
public sealed class Operador
{
    public required string Id { get; init; }

    public required string Nome { get; init; }

    public required Papel Papel { get; init; }
}

public static class Operadores
{
    public static IReadOnlyList<Operador> Todos { get; } =
    [
        new() { Id = "REC-01", Nome = "Ana Lopes", Papel = Papel.Recepcao },
        new() { Id = "REC-02", Nome = "Diego Pires", Papel = Papel.Recepcao },
        new() { Id = "MED-01", Nome = "Dra. Marta Leão", Papel = Papel.Medico },
        new() { Id = "MED-02", Nome = "Dr. Caio Brandão", Papel = Papel.Medico },
        new() { Id = "COL-01", Nome = "Bete Nunes", Papel = Papel.Coleta },
        new() { Id = "COL-02", Nome = "Léo Farias", Papel = Papel.Coleta },
        new() { Id = "ANA-01", Nome = "Vera Salgado", Papel = Papel.Analista },
        new() { Id = "ANA-02", Nome = "Hugo Neves", Papel = Papel.Analista },
        new() { Id = "ADM-01", Nome = "Admin Demo", Papel = Papel.Admin },
    ];

    public static IReadOnlyList<Operador> DoPapel(Papel papel) => Todos.Where(o => o.Papel == papel).ToList();

    public static bool Pertence(string id, Papel papel) => Todos.Any(o => o.Id == id && o.Papel == papel);
}
