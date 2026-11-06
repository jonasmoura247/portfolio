using System.Collections.Generic;

namespace Bancada.Dominio;

/// <summary>
/// Quem pode o quê. Negação por padrão: ação ou papel fora da tabela nunca é permitido.
/// A restrição por operador (ex.: só o médico do horário atende, RN-007) fica nas regras de cada caso de uso.
/// </summary>
public static class MatrizDeAutorizacao
{
    private static readonly Papel[] Todos = [Papel.Recepcao, Papel.Medico, Papel.Coleta, Papel.Analista, Papel.Admin];
    private static readonly Papel[] RecepcaoEAdmin = [Papel.Recepcao, Papel.Admin];
    private static readonly Papel[] RecepcaoMedicoAdmin = [Papel.Recepcao, Papel.Medico, Papel.Admin];
    private static readonly Papel[] MedicoEAdmin = [Papel.Medico, Papel.Admin];
    private static readonly Papel[] SoMedico = [Papel.Medico];
    private static readonly Papel[] MedicoColetaAdmin = [Papel.Medico, Papel.Coleta, Papel.Admin];
    private static readonly Papel[] ColetaEAdmin = [Papel.Coleta, Papel.Admin];
    private static readonly Papel[] ColetaAnalistaAdmin = [Papel.Coleta, Papel.Analista, Papel.Admin];
    private static readonly Papel[] AnalistaEAdmin = [Papel.Analista, Papel.Admin];
    private static readonly Papel[] MedicoAnalistaAdmin = [Papel.Medico, Papel.Analista, Papel.Admin];

    private static readonly Dictionary<Acao, Papel[]> Tabela = new()
    {
        [Acao.TrocarPapel] = Todos,
        [Acao.TrocarOperador] = Todos,
        [Acao.TrocarIdioma] = Todos,
        [Acao.CarregarCenario] = Todos,
        [Acao.ReiniciarSessao] = Todos,
        [Acao.ListarPacientes] = Todos,
        [Acao.VerPaciente] = Todos,
        [Acao.VerAgenda] = RecepcaoMedicoAdmin,
        [Acao.Agendar] = RecepcaoEAdmin,
        [Acao.Reagendar] = RecepcaoEAdmin,
        [Acao.CancelarAgendamento] = RecepcaoEAdmin,
        [Acao.ConfirmarAgendamento] = RecepcaoEAdmin,
        [Acao.VerFila] = RecepcaoMedicoAdmin,
        [Acao.FazerCheckIn] = RecepcaoEAdmin,
        [Acao.VerAtendimento] = MedicoEAdmin,
        [Acao.IniciarAtendimento] = MedicoEAdmin,
        [Acao.ConcluirAtendimento] = MedicoEAdmin,
        [Acao.VerEvolucoes] = MedicoEAdmin,
        [Acao.RegistrarEvolucao] = SoMedico,
        [Acao.CorrigirEvolucao] = SoMedico,
        [Acao.EditarAlergias] = MedicoColetaAdmin,
        [Acao.CriarPedido] = MedicoEAdmin,
        [Acao.VerPedido] = Todos,
        [Acao.CancelarPedido] = MedicoEAdmin,
        [Acao.ListarColetas] = ColetaEAdmin,
        [Acao.ColetarAmostra] = ColetaEAdmin,
        [Acao.ConsultarEtiqueta] = ColetaAnalistaAdmin,
        [Acao.RejeitarAmostra] = ColetaAnalistaAdmin,
        [Acao.VerSoroteca] = ColetaAnalistaAdmin,
        [Acao.ArmazenarAmostra] = ColetaAnalistaAdmin,
        [Acao.MoverAmostra] = ColetaAnalistaAdmin,
        [Acao.DescartarAmostra] = AnalistaEAdmin,
        [Acao.VerEquipamento] = AnalistaEAdmin,
        [Acao.VerQualidade] = AnalistaEAdmin,
        [Acao.EnviarAoEquipamento] = AnalistaEAdmin,
        [Acao.ControlarRelogio] = AnalistaEAdmin,
        [Acao.ReprocessarExecucao] = AnalistaEAdmin,
        [Acao.RegistrarControle] = AnalistaEAdmin,
        [Acao.VerPendentes] = AnalistaEAdmin,
        [Acao.ValidarResultado] = AnalistaEAdmin,
        [Acao.ConfirmarCritico] = AnalistaEAdmin,
        [Acao.LiberarLaudo] = AnalistaEAdmin,
        [Acao.RetificarLaudo] = AnalistaEAdmin,
        [Acao.VerLaudo] = MedicoAnalistaAdmin,
        [Acao.VerAuditoria] = AnalistaEAdmin,
        [Acao.VerificarCadeia] = AnalistaEAdmin,
        [Acao.SimularAdulteracao] = AnalistaEAdmin,
        [Acao.VerPainel] = Todos,
        [Acao.ExecutarTestes] = Todos,
    };

    public static bool Permite(Papel papel, Acao acao) =>
        Tabela.TryGetValue(acao, out var papeis) && System.Array.IndexOf(papeis, papel) >= 0;
}
