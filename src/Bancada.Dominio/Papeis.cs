using System;

namespace Bancada.Dominio;

public static class Papeis
{
    public static string Rotulo(Papel papel) => papel switch
    {
        Papel.Recepcao => "Recepção",
        Papel.Medico => "Médico(a)",
        Papel.Coleta => "Coleta",
        Papel.Analista => "Analista",
        Papel.Admin => "Admin",
        _ => throw new ArgumentOutOfRangeException(nameof(papel), papel, "Papel desconhecido."),
    };
}
