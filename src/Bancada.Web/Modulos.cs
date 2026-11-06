using System.Collections.Generic;
using System.Linq;
using Bancada.Dominio;

namespace Bancada.Web;

/// <summary>Módulo do menu. Aparece para o papel que pode exercer alguma das ações.</summary>
public sealed class Modulo
{
    public required string Id { get; init; }

    public required string Rota { get; init; }

    public required int Fase { get; init; }

    public required Acao[] Acoes { get; init; }

    public string TextoChave => "menu." + Id;

    public bool VisivelPara(Papel papel) => Acoes.Length == 0 || Acoes.Any(a => MatrizDeAutorizacao.Permite(papel, a));
}

public static class Modulos
{
    public static IReadOnlyList<Modulo> Todos { get; } =
    [
        new() { Id = "inicio", Rota = "/bancada", Fase = 1, Acoes = [] },
        new() { Id = "pacientes", Rota = "/pacientes", Fase = 3, Acoes = [Acao.ListarPacientes] },
        new() { Id = "agenda", Rota = "/agenda", Fase = 3, Acoes = [Acao.VerAgenda] },
        new() { Id = "atendimento", Rota = "/atendimentos", Fase = 3, Acoes = [Acao.VerFila] },
        new() { Id = "prontuario", Rota = "/prontuario", Fase = 4, Acoes = [Acao.VerEvolucoes, Acao.EditarAlergias] },
        new() { Id = "pedido", Rota = "/pedidos", Fase = 4, Acoes = [Acao.VerPedido] },
        new() { Id = "coleta", Rota = "/coletas", Fase = 5, Acoes = [Acao.ListarColetas] },
        new() { Id = "etiquetas", Rota = "/etiquetas", Fase = 5, Acoes = [Acao.ConsultarEtiqueta] },
        new() { Id = "soroteca", Rota = "/soroteca", Fase = 5, Acoes = [Acao.VerSoroteca] },
        new() { Id = "equipamento", Rota = "/equipamentos", Fase = 6, Acoes = [Acao.VerEquipamento] },
        new() { Id = "qualidade", Rota = "/qualidade", Fase = 7, Acoes = [Acao.VerQualidade] },
        new() { Id = "resultados", Rota = "/resultados", Fase = 7, Acoes = [Acao.VerLaudo] },
        new() { Id = "auditoria", Rota = "/auditoria", Fase = 1, Acoes = [Acao.VerAuditoria] },
        new() { Id = "operacao", Rota = "/operacao", Fase = 8, Acoes = [Acao.VerPainel] },
        new() { Id = "painel", Rota = "/painel", Fase = 2, Acoes = [Acao.VerPainel] },
    ];
}
