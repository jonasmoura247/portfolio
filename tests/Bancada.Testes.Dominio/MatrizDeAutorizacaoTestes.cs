using System;
using System.Collections.Generic;
using System.Linq;
using Bancada.Dominio;

namespace Bancada.Testes.Dominio;

/// <summary>
/// A tabela esperada está escrita aqui por extenso (colunas Rec, Med, Col, Ana, Adm), copiada do vault,
/// de propósito diferente da implementação: se alguém mudar a matriz, este teste precisa mudar junto.
/// </summary>
public class MatrizDeAutorizacaoTestes
{
    private static readonly Papel[] Ordem = [Papel.Recepcao, Papel.Medico, Papel.Coleta, Papel.Analista, Papel.Admin];

    public static IEnumerable<TestCaseData> Esperada()
    {
        var linhas = new (Acao Acao, string Permitidos)[]
        {
            (Acao.TrocarPapel, "RMCAX"), (Acao.TrocarOperador, "RMCAX"), (Acao.CarregarCenario, "RMCAX"), (Acao.ReiniciarSessao, "RMCAX"),
            (Acao.TrocarPapel, "RMCAX"), (Acao.TrocarIdioma, "RMCAX"), (Acao.CarregarCenario, "RMCAX"), (Acao.ReiniciarSessao, "RMCAX"),
            (Acao.ListarPacientes, "RMCAX"), (Acao.VerPaciente, "RMCAX"),
            (Acao.VerAgenda, "RM..X"), (Acao.Agendar, "R...X"), (Acao.Reagendar, "R...X"), (Acao.CancelarAgendamento, "R...X"), (Acao.ConfirmarAgendamento, "R...X"),
            (Acao.VerFila, "RM..X"), (Acao.FazerCheckIn, "R...X"), (Acao.VerAtendimento, ".M..X"), (Acao.IniciarAtendimento, ".M..X"), (Acao.ConcluirAtendimento, ".M..X"),
            (Acao.VerEvolucoes, ".M..X"), (Acao.RegistrarEvolucao, ".M..."), (Acao.CorrigirEvolucao, ".M..."), (Acao.EditarAlergias, ".MC.X"),
            (Acao.CriarPedido, ".M..X"), (Acao.VerPedido, "RMCAX"), (Acao.CancelarPedido, ".M..X"),
            (Acao.ListarColetas, "..C.X"), (Acao.ColetarAmostra, "..C.X"), (Acao.ConsultarEtiqueta, "..CAX"), (Acao.RejeitarAmostra, "..CAX"),
            (Acao.VerSoroteca, "..CAX"), (Acao.ArmazenarAmostra, "..CAX"), (Acao.MoverAmostra, "..CAX"), (Acao.DescartarAmostra, "...AX"),
            (Acao.VerEquipamento, "...AX"), (Acao.VerQualidade, "...AX"), (Acao.EnviarAoEquipamento, "...AX"), (Acao.ControlarRelogio, "...AX"),
            (Acao.ReprocessarExecucao, "...AX"), (Acao.RegistrarControle, "...AX"),
            (Acao.VerPendentes, "...AX"), (Acao.ValidarResultado, "...AX"), (Acao.ConfirmarCritico, "...AX"), (Acao.LiberarLaudo, "...AX"), (Acao.RetificarLaudo, "...AX"),
            (Acao.VerLaudo, ".M.AX"),
            (Acao.VerAuditoria, "...AX"), (Acao.VerificarCadeia, "...AX"), (Acao.SimularAdulteracao, "...AX"),
            (Acao.VerPainel, "RMCAX"), (Acao.ExecutarTestes, "RMCAX"),
        };

        foreach (var (acao, permitidos) in linhas)
        {
            yield return new TestCaseData(acao, permitidos).SetName($"Matriz_{acao}");
        }
    }

    [TestCaseSource(nameof(Esperada))]
    public void Permite_CadaPapel_BateComATabelaDoVault(Acao acao, string permitidos)
    {
        for (var i = 0; i < Ordem.Length; i++)
        {
            Assert.That(MatrizDeAutorizacao.Permite(Ordem[i], acao), Is.EqualTo(permitidos[i] != '.'), $"{acao} para {Ordem[i]}");
        }
    }

    [Test]
    public void Esperada_CobreTodaAcaoDoEnum()
    {
        var cobertas = Esperada().Select(c => (Acao)c.Arguments[0]!).ToHashSet();

        Assert.That(cobertas, Is.EquivalentTo(Enum.GetValues<Acao>()));
    }

    [Test]
    public void Permite_AcaoForaDoEnum_NegaPorPadrao()
    {
        foreach (var papel in Ordem)
        {
            Assert.That(MatrizDeAutorizacao.Permite(papel, (Acao)999), Is.False);
        }
    }

    [Test]
    public void Permite_PapelForaDoEnum_NegaPorPadrao()
    {
        Assert.That(MatrizDeAutorizacao.Permite((Papel)99, Acao.VerPaciente), Is.False);
    }

    [Test]
    public void Admin_NaoEscreveEvolucao_SoMedico()
    {
        Assert.That(MatrizDeAutorizacao.Permite(Papel.Admin, Acao.RegistrarEvolucao), Is.False);
        Assert.That(MatrizDeAutorizacao.Permite(Papel.Admin, Acao.CorrigirEvolucao), Is.False);
    }
}
