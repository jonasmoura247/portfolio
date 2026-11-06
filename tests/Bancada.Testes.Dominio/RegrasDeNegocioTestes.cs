using System;
using System.Collections.Generic;
using System.Linq;
using Bancada.Dominio;

namespace Bancada.Testes.Dominio;

/// <summary>Regras de negócio com o identificador do vault no nome do teste (vault: Regras-de-Negocio). O painel técnico lê estes nomes.</summary>
public class RegrasDeNegocioTestes
{
    private static readonly DateTime Agora = new(2026, 11, 4, 10, 15, 0);

    private static HorarioDaGrade Horario(DateTime inicio, string medico = "MED-01") => new(medico, inicio);

    private static Agendamento Marcado(DateTime inicio) => Agendamento.Criar(1, Horario(inicio), null, inicio.AddDays(-1));

    private static string Erro(Action acao) => Assert.Throws<ViolacaoDeRegra>(() => acao())!.Chave;

    private static Pedido PedidoCom(params string[] exames) => Pedido.Criar("PED-1", 1, null, "MED-01", false, exames, Agora);

    private static Amostra Soro(bool hemolisada = false, DateTime? quando = null)
    {
        var p = PedidoCom("ELE", "GLI");
        return Amostra.Coletar("AMO-1", p, p.Tubos()[0], 5, 12, hemolisada, quando ?? Agora);
    }

    private static Amostra SangueTotal(bool hemolisada = false)
    {
        var p = PedidoCom("HEM");
        return Amostra.Coletar("AMO-2", p, p.Tubos()[0], 5, 0, hemolisada, Agora);
    }

    private static Resultado Medido(string flag = "N", bool delta = false, string enviadoPor = "ANA-01") =>
        new(1, 1, 1, "POT", 4, flag, null, delta, Catalogo.Bioquimica, enviadoPor);

    [Test]
    public void RN_002_PassadoRecusado()
    {
        Assert.That(Erro(() => Agendamento.Criar(1, Horario(Agora.AddMinutes(-1)), null, Agora)), Is.EqualTo("agenda.erro.passado"));
        Assert.That(Agendamento.Criar(1, Horario(Agora), "m", Agora).MotivoCifrado, Is.EqualTo("m"));
    }

    [Test]
    public void RN_002_ReagendarParaOPassadoRecusado()
    {
        var a = Marcado(Agora.AddHours(1));
        Assert.That(Erro(() => a.Reagendar(Horario(Agora.AddHours(-1)), Agora)), Is.EqualTo("agenda.erro.passado"));
    }

    [Test]
    public void RN_004_CancelarLiberaHorario()
    {
        var a = Marcado(Agora.AddHours(1));
        a.Cancelar();
        Assert.That(a.Estado, Is.EqualTo(EstadoAgendamento.Cancelado));
        Assert.That(Erro(a.Confirmar), Is.EqualTo("regra.transicao"));
    }

    [Test]
    public void RN_005_HorarioPassadoViraFaltou()
    {
        var a = Marcado(Agora.AddHours(1));
        a.Confirmar();
        a.RegistrarFalta();
        Assert.That(a.Estado, Is.EqualTo(EstadoAgendamento.Faltou));
    }

    [Test]
    public void RN_006_CheckInForaDoDiaRecusado()
    {
        var h = Horario(Agora.AddDays(1));
        var a = Agendamento.Criar(1, h, null, Agora);
        Assert.That(Erro(() => a.FazerCheckIn(h, Agora)), Is.EqualTo("atendimento.erro.foradodia"));
        Assert.That(a.ChegadaEm, Is.Null);
    }

    [Test]
    public void RN_007_OutroProfissionalRecusado()
    {
        var h = Horario(Agora.AddHours(1));
        var a = Agendamento.Criar(1, h, null, Agora);
        a.FazerCheckIn(h, Agora);
        Assert.That(Erro(() => a.Iniciar(h, "MED-02", Agora)), Is.EqualTo("atendimento.erro.outroprofissional"));
        Assert.That(Erro(() => a.Concluir(h, "MED-02", true, Agora)), Is.EqualTo("atendimento.erro.outroprofissional"));
    }

    [Test]
    public void RN_008_TodasAsTransicoes()
    {
        var permitidas = new HashSet<(EstadoAgendamento, EventoAgendamento)>
        {
            (EstadoAgendamento.Agendado, EventoAgendamento.Confirmar),
            (EstadoAgendamento.Agendado, EventoAgendamento.FazerCheckIn),
            (EstadoAgendamento.Confirmado, EventoAgendamento.FazerCheckIn),
            (EstadoAgendamento.ChegouCheckIn, EventoAgendamento.Iniciar),
            (EstadoAgendamento.EmAtendimento, EventoAgendamento.Concluir),
            (EstadoAgendamento.Agendado, EventoAgendamento.Cancelar),
            (EstadoAgendamento.Confirmado, EventoAgendamento.Cancelar),
            (EstadoAgendamento.ChegouCheckIn, EventoAgendamento.Cancelar),
            (EstadoAgendamento.Agendado, EventoAgendamento.RegistrarFalta),
            (EstadoAgendamento.Confirmado, EventoAgendamento.RegistrarFalta),
            (EstadoAgendamento.Agendado, EventoAgendamento.Reagendar),
            (EstadoAgendamento.Confirmado, EventoAgendamento.Reagendar),
        };

        foreach (var estado in Enum.GetValues<EstadoAgendamento>())
        {
            foreach (var evento in Enum.GetValues<EventoAgendamento>())
            {
                var esperado = permitidas.Contains((estado, evento));
                Assert.That(Agendamento.Maquina.Permite(estado, evento), Is.EqualTo(esperado), $"{estado} + {evento}");
                if (!esperado)
                {
                    Assert.That(Erro(() => Agendamento.Maquina.Proximo(estado, evento)), Is.EqualTo("regra.transicao"));
                }
            }
        }
    }

    [Test]
    public void Atendimento_CaminhoCompleto()
    {
        var h = Horario(Agora.AddMinutes(10));
        var a = Agendamento.Criar(1, h, null, Agora);
        a.Reagendar(h, Agora);
        a.FazerCheckIn(h, Agora);
        a.Iniciar(h, "MED-01", Agora);
        Assert.That(Erro(() => a.Concluir(h, "MED-01", false, Agora)), Is.EqualTo("atendimento.erro.semevolucao"));
        a.Concluir(h, "MED-01", true, Agora);

        Assert.Multiple(() =>
        {
            Assert.That(a.Estado, Is.EqualTo(EstadoAgendamento.Concluido));
            Assert.That(a.ChegadaEm, Is.EqualTo(Agora));
            Assert.That(a.IniciadoEm, Is.EqualTo(Agora));
            Assert.That(a.ConcluidoEm, Is.EqualTo(Agora));
            Assert.That(a.CriadoEm, Is.EqualTo(Agora));
            Assert.That(a.PacienteId, Is.EqualTo(1));
        });
    }

    [Test]
    public void Paciente_IdadeRespeitaOAniversario()
    {
        var p = new Paciente("PAC-1", "Nome", new DateOnly(2000, 11, 5), "F");
        Assert.That(p.IdadeEm(new DateOnly(2026, 11, 4)), Is.EqualTo(25));
        Assert.That(p.IdadeEm(new DateOnly(2026, 11, 5)), Is.EqualTo(26));
    }

    [Test]
    public void Alergia_AlternaEAmarraOsDadosAoRegistro()
    {
        var a = new Alergia(7, "Látex", "x", Agora);
        a.Alternar();
        Assert.That(a.Ativa, Is.False);
        Assert.That(a.DadosAssociados, Does.StartWith("alergia:7:"));
    }

    [Test]
    public void RN_012_TamanhoELimites()
    {
        Assert.That(Texto.Exigir("  ok  ", 1, 2000, "x"), Is.EqualTo("ok"));
        Assert.That(Erro(() => Texto.Exigir("   ", 1, 2000, "prontuario.erro.texto")), Is.EqualTo("prontuario.erro.texto"));
        Assert.That(Erro(() => Texto.Exigir(null, 1, 10, "e")), Is.EqualTo("e"));
        Assert.That(Erro(() => Texto.Exigir(new string('a', Evolucao.TamanhoMaximo + 1), 1, Evolucao.TamanhoMaximo, "e")), Is.EqualTo("e"));
        Assert.That(Texto.Exigir(null, 0, 10, "e"), Is.Empty);
    }

    [Test]
    public void Evolucao_DadosAssociadosPrendemATextoAoRegistro()
    {
        var e = new Evolucao(3, null, "MED-01", "cifra", null, Agora);
        Assert.That(e.DadosAssociados, Is.EqualTo(Evolucao.DadosDe(3, Agora)));
    }

    [Test]
    public void RN_020_LimitesEUnicidade()
    {
        Assert.That(Erro(() => PedidoCom()), Is.EqualTo("pedido.erro.quantidade"));
        Assert.That(Erro(() => Pedido.Criar("P", 1, null, "M", false, Enumerable.Repeat("GLI", 21).ToList(), Agora)), Is.EqualTo("pedido.erro.quantidade"));
        Assert.That(Erro(() => PedidoCom("GLI", "GLI")), Is.EqualTo("pedido.erro.repetido"));
        Assert.That(Erro(() => PedidoCom("XXX")), Is.EqualTo("pedido.erro.exame"));
    }

    [Test]
    public void RN_021_AgrupaPorMaterial()
    {
        var tubos = PedidoCom("GLI", "COL", "HEM").Tubos();
        Assert.That(tubos, Has.Count.EqualTo(2));
        var soro = tubos.Single(t => t.Material == Catalogo.Soro);
        Assert.Multiple(() =>
        {
            Assert.That(soro.Exames, Is.EqualTo(new[] { "GLI", "COL" }));
            Assert.That(soro.JejumHoras, Is.EqualTo(12));
            Assert.That(soro.VolumeMl, Is.EqualTo(0.5));
        });
    }

    [Test]
    public void Pedido_SoCancelaEnquantoSolicitado()
    {
        var p = PedidoCom("GLI");
        p.Cancelar();
        Assert.That(p.Estado, Is.EqualTo(EstadoPedido.Cancelado));
        Assert.That(Erro(p.Cancelar), Is.EqualTo("regra.transicao"));
    }

    [Test]
    public void Pedido_AndaPelosEstados()
    {
        var p = PedidoCom("GLI");
        p.MarcarColetado();
        p.Recoletar();
        Assert.That(p.Estado, Is.EqualTo(EstadoPedido.Solicitado));
        p.MarcarEmAnalise();
        p.MarcarResultado();
        p.Liberar(Agora);
        Assert.Multiple(() =>
        {
            Assert.That(p.Estado, Is.EqualTo(EstadoPedido.Liberado));
            Assert.That(p.LiberadoEm, Is.EqualTo(Agora));
            Assert.That(p.Itens[0].Exame, Is.EqualTo("GLI"));
        });
    }

    [Test]
    public void RN_022_JejumExigeConfirmacao()
    {
        var p = PedidoCom("GLI");
        Assert.That(Erro(() => Amostra.Coletar("A", p, p.Tubos()[0], 5, 7, false, Agora)), Is.EqualTo("coleta.erro.jejum"));
    }

    [Test]
    public void RN_023_VolumeInsuficiente()
    {
        var p = PedidoCom("GLI");
        Assert.That(Erro(() => Amostra.Coletar("A", p, p.Tubos()[0], 0.4, 8, false, Agora)), Is.EqualTo("coleta.erro.volume"));
    }

    [Test]
    public void RN_025_RejeicaoGuardaOMotivo()
    {
        var a = Soro();
        a.Rejeitar("Hemolise");
        Assert.That(a.Estado, Is.EqualTo(EstadoAmostra.Rejeitada));
        Assert.That(a.MotivoRejeicao, Is.EqualTo("Hemolise"));
        Assert.That(Erro(() => a.Rejeitar("x")), Is.EqualTo("regra.transicao"));
    }

    [Test]
    public void RN_025_ArmazenadaTambemPodeSerRejeitada()
    {
        var a = Soro();
        a.Armazenar();
        a.Rejeitar("VolumeInsuficiente");
        Assert.That(a.Estado, Is.EqualTo(EstadoAmostra.Rejeitada));
    }

    [Test]
    public void RN_025_HemolisadaNaoArmazena() =>
        Assert.That(Erro(Soro(hemolisada: true).Armazenar), Is.EqualTo("soroteca.erro.hemolisada"));

    [Test]
    public void RN_030_PosicaoUnica()
    {
        var p = new PosicaoDaSoroteca("R1-C1", 2, 3);
        p.Ocupar(1);
        Assert.That(Erro(() => p.Ocupar(2)), Is.EqualTo("soroteca.erro.ocupada"));
        Assert.That(p.Nome, Is.EqualTo("R1-C1-2-3"));
        p.Liberar();
        p.Ocupar(2);
        Assert.That(p.AmostraId, Is.EqualTo(2));
    }

    [Test]
    public void RN_031_SoSoroArmazena() => Assert.That(Erro(SangueTotal().Armazenar), Is.EqualTo("soroteca.erro.sosoro"));

    [Test]
    public void Armazenar_SoDeQuemFoiColetada()
    {
        var a = Soro();
        a.Armazenar();
        Assert.That(Erro(a.Armazenar), Is.EqualTo("regra.transicao"));
    }

    [Test]
    public void RN_032_Validade()
    {
        var a = Soro();
        Assert.Multiple(() =>
        {
            Assert.That(a.ValidadeAte, Is.EqualTo(Agora.AddDays(Amostra.DiasDeValidade)));
            Assert.That(a.VencidaEm(a.ValidadeAte), Is.False);
            Assert.That(a.VencidaEm(a.ValidadeAte.AddSeconds(1)), Is.True);
            Assert.That(a.VenceEmUmDia(a.ValidadeAte.AddHours(-23)), Is.True);
            Assert.That(a.VenceEmUmDia(a.ValidadeAte.AddHours(-25)), Is.False);
            Assert.That(a.VenceEmUmDia(a.ValidadeAte.AddHours(1)), Is.False);
        });
    }

    [Test]
    public void RN_033_VencidaNaoAnalisa()
    {
        var a = Soro();
        a.Armazenar();
        Assert.That(Erro(() => a.EnviarParaAnalise(Agora.AddDays(8))), Is.EqualTo("soroteca.erro.vencida"));
    }

    [Test]
    public void Analise_SoroVoltaParaASorotecaESangueTerminaAnalisado()
    {
        var soro = Soro();
        soro.Armazenar();
        soro.EnviarParaAnalise(Agora);
        Assert.That(soro.Estado, Is.EqualTo(EstadoAmostra.EmAnalise));
        soro.ConcluirAnalise();
        Assert.That(soro.Estado, Is.EqualTo(EstadoAmostra.Armazenada));

        var sangue = SangueTotal();
        sangue.EnviarParaAnalise(Agora);
        sangue.ConcluirAnalise();
        Assert.That(sangue.Estado, Is.EqualTo(EstadoAmostra.Analisada));
    }

    [Test]
    public void Analise_RecusaQuemNaoEstaPronto()
    {
        Assert.That(Erro(() => Soro().EnviarParaAnalise(Agora)), Is.EqualTo("regra.transicao"));
        Assert.That(Erro(() => SangueTotal(hemolisada: true).EnviarParaAnalise(Agora)), Is.EqualTo("regra.transicao"));
        var rejeitada = SangueTotal();
        rejeitada.Rejeitar("x");
        Assert.That(Erro(() => rejeitada.EnviarParaAnalise(Agora)), Is.EqualTo("regra.transicao"));
    }

    [Test]
    public void RN_034_DescarteRegistrado()
    {
        var a = Soro();
        Assert.That(Erro(() => a.Descartar("m")), Is.EqualTo("regra.transicao"));
        a.Armazenar();
        a.Descartar("m");
        Assert.That(a.Estado, Is.EqualTo(EstadoAmostra.Descartada));
        Assert.That(a.MotivoDescarteCifrado, Is.EqualTo("m"));
    }

    [Test]
    public void Amostra_ListaOsExamesDoTubo() => Assert.That(Soro().ListaDeExames, Is.EqualTo(new[] { "ELE", "GLI" }));

    [Test]
    public void RN_041_Capacidade()
    {
        var execucoes = Enumerable.Range(0, 6).Select(i => Execucao(Catalogo.Bioquimica, prioridade: i == 5 ? 1 : 0)).ToList();
        Simulador.Passo(execucoes, 1);
        Assert.That(execucoes.Count(e => e.Estado == EstadoExecucao.Processando), Is.EqualTo(4));
        Assert.That(execucoes[5].Estado, Is.EqualTo(EstadoExecucao.Processando), "urgente passa na frente");
    }

    [Test]
    public void Simulador_ConcluiQuandoOTempoAcaba()
    {
        var e = Execucao(Catalogo.Hematologia);
        Assert.That(Simulador.Passo([e], 3), Is.Empty);
        Assert.That(e.Progresso, Is.EqualTo(0.3).Within(0.001));
        Assert.That(Simulador.Passo([e], 8), Is.EqualTo(new[] { e }));
        Assert.That(e.Estado, Is.EqualTo(EstadoExecucao.Concluido));
        Assert.That(e.Ativa, Is.False);
    }

    [Test]
    public void RN_042_LimiteDeReprocesso()
    {
        var e = Execucao(Catalogo.Bioquimica, falha: true);
        Assert.That(Erro(e.Reprocessar), Is.EqualTo("equipamento.erro.reprocesso"));
        Simulador.Passo([e], 6);
        Assert.That(e.Estado, Is.EqualTo(EstadoExecucao.Erro));
        e.Reprocessar();
        Assert.That(e.Estado, Is.EqualTo(EstadoExecucao.NaFila));
        Simulador.Passo([e], 11);
        Assert.That(e.Estado, Is.EqualTo(EstadoExecucao.Concluido), "a falha do cenário só vale na primeira tentativa");
    }

    [Test]
    public void RN_042_TerceiraTentativaEsgotaOReprocesso()
    {
        var e = Execucao(Catalogo.Bioquimica, falha: true, tentativas: Bancada.Dominio.Execucao.MaximoDeTentativas);
        Assert.That(Erro(e.Reprocessar), Is.EqualTo("equipamento.erro.reprocesso"));
    }

    [Test]
    public void RN_043_CQReprovadoBloqueia() =>
        Assert.That(Erro(() => Medido().Validar("ANA-02", true, null)), Is.EqualTo("resultado.erro.cq"));

    [Test]
    public void RN_052_CriticoExigeConfirmacao()
    {
        var r = Medido("CH");
        Assert.That(Erro(() => r.Validar("ANA-02", false, null)), Is.EqualTo("resultado.erro.critico"));
        r.ConfirmarCritico();
        Assert.That(Erro(r.ConfirmarCritico), Is.EqualTo("regra.transicao"));
        r.Validar("ANA-02", false, null);
        Assert.That(r.Estado, Is.EqualTo(EstadoResultado.Validado));
        Assert.That(Medido("CL").Critico, Is.True);
    }

    [Test]
    public void RN_052_ValorNormalNaoTemConfirmacao() => Assert.That(Erro(Medido().ConfirmarCritico), Is.EqualTo("regra.transicao"));

    [Test]
    public void RN_053_DeltaCheck()
    {
        var r = Medido(delta: true);
        Assert.That(Erro(() => r.Validar("ANA-02", false, null)), Is.EqualTo("resultado.erro.delta"));
        r.Validar("ANA-02", false, "cifra");
        Assert.That(r.JustificativaCifrada, Is.EqualTo("cifra"));
    }

    [Test]
    public void RN_054_SeparacaoDeDeveres() =>
        Assert.That(Erro(() => Medido().Validar("ANA-01", false, null)), Is.EqualTo("resultado.erro.mesmooperador"));

    [Test]
    public void Resultado_ValidadoNaoValidaDeNovo()
    {
        var r = Resultado.Historico(1, 1, "POT", 4.4);
        Assert.That(r.ValidadoPorId, Is.EqualTo("ANA-02"));
        Assert.That(Erro(() => r.Validar("ANA-02", false, null)), Is.EqualTo("regra.transicao"));
    }

    [Test]
    public void Controle_CalculaODesvioEmZ()
    {
        var c = new ControleDeQualidade("EQ", "POT", 4.44, 4.2, 0.12, Agora, null, true);
        Assert.That(c.Z, Is.EqualTo(2).Within(0.001));
    }

    [Test]
    public void RN_040_ExameNaoSuportado()
    {
        Assert.That(Erro(() => Catalogo.ExameDe("XYZ")), Is.EqualTo("pedido.erro.exame"));
        Assert.That(Catalogo.ExameDe("HEM").Equipamento, Is.EqualTo(Catalogo.Hematologia));
    }

    [Test]
    public void RN_050_FaixaPorSexo()
    {
        Assert.That(Catalogo.AnalitoDe("HGB").FaixaPara("M").NormalMin, Is.EqualTo(13.5));
        Assert.That(Catalogo.AnalitoDe("HGB").FaixaPara("F").NormalMin, Is.EqualTo(12));
    }

    [Test]
    public void Catalogo_FaixaPorSexoComPadrao()
    {
        Assert.That(Catalogo.AnalitoDe("CRE").FaixaPara("F").NormalMax, Is.EqualTo(1.1));
        Assert.That(Catalogo.AnalitoDe("POT").FaixaPara("F").Sexo, Is.EqualTo("A"));
        Assert.That(Catalogo.EquipamentoDe(Catalogo.Hematologia).Protocolo, Is.EqualTo("HL7"));
    }

    [Test]
    public void Catalogo_TodoExameApontaParaEquipamentoEAnalitosExistentes()
    {
        foreach (var e in Catalogo.Exames)
        {
            Assert.That(Catalogo.EquipamentoDe(e.Equipamento), Is.Not.Null);
            Assert.That(e.Analitos.Select(Catalogo.AnalitoDe).Count(), Is.EqualTo(e.Analitos.Length));
        }
    }

    [Test]
    public void Erros_CarregamAChave()
    {
        Assert.That(new ViolacaoDeRegra("a.b").Message, Is.EqualTo("a.b"));
        Assert.That(new NaoEncontrado("pedido").Message, Is.EqualTo("pedido"));
    }

    [Test]
    public void Registros_GuardamOQueReceberam()
    {
        var c = new CustodiaDaAmostra(1, "Coletada", "COL-01", "Coleta", Agora);
        var l = new Laudo(1, 2, "[]", "ANA-02", Agora, "m");
        var m = new MensagemDeProtocolo("EQ", true, Agora, "ACK");
        Assert.Multiple(() =>
        {
            Assert.That(c.Evento, Is.EqualTo("Coletada"));
            Assert.That(l.Versao, Is.EqualTo(2));
            Assert.That(m.DoEquipamento, Is.True);
        });
    }

    [Test]
    public void Entidades_NascemSemIdAteOBancoAtribuir()
    {
        var pedido = PedidoCom("GLI");
        int[] ids =
        [
            new Paciente("P", "N", new DateOnly(2000, 1, 1), "F").Id, new Alergia(1, "x", null, Agora).Id, Marcado(Agora).Id, new Evolucao(1, null, "M", "c", null, Agora).Id,
            pedido.Itens[0].Id, pedido.Itens[0].PedidoId, Soro().Id, new CustodiaDaAmostra(1, "e", "o", "l", Agora).Id, new PosicaoDaSoroteca("C", 1, 1).Id,
            Execucao(Catalogo.Bioquimica).Id, Medido().Id, new Laudo(1, 1, "[]", "A", Agora, null).Id, new ControleDeQualidade("E", "A", 1, 1, 1, Agora, null, true).Id,
            new MensagemDeProtocolo("E", false, Agora, "t").Id,
        ];

        Assert.That(ids, Is.All.Zero);
    }

    [Test]
    public void Execucao_NaFilaEProcessandoSaoAtivas()
    {
        var e = Execucao(Catalogo.Bioquimica);
        Assert.That(e.Ativa, Is.True);
        e.Iniciar();
        Assert.That(e.Ativa, Is.True);
    }

    private static Execucao Execucao(string equipamento, int prioridade = 0, bool falha = false, int tentativas = 0)
    {
        var e = new Execucao(1, 1, "GLI", equipamento, prioridade, 10, falha, "ANA-01", Agora);
        for (var i = 0; i < tentativas; i++)
        {
            ForcarErro(e);
        }

        return e;
    }

    private static void ForcarErro(Execucao e)
    {
        typeof(Execucao).GetProperty(nameof(e.Tentativas))!.SetValue(e, e.Tentativas + 1);
        typeof(Execucao).GetProperty(nameof(e.Estado))!.SetValue(e, EstadoExecucao.Erro);
    }
}
