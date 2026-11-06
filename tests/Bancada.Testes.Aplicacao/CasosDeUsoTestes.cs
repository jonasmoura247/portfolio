using System;
using System.Collections.Generic;
using System.Linq;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Bancada.Infraestrutura.Dados;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Testes.Aplicacao;

public class SementeTestes
{
    [Test]
    public void Semente_TemOQueOsCenariosPrometem()
    {
        using var m = new Mesa();
        var pacientes = m.Repos.Todos();
        var pedido = m.PedidoDoCenario;

        Assert.Multiple(() =>
        {
            Assert.That(pacientes, Has.Count.EqualTo(60));
            Assert.That(pedido.Estado, Is.EqualTo(EstadoPedido.Solicitado));
            Assert.That(m.Repos.PorCodigo("PAC-0014")!.Id, Is.EqualTo(pedido.PacienteId));
            Assert.That(m.Repos.UltimoValidado(pedido.PacienteId, "POT"), Is.EqualTo(4.4));
            Assert.That(m.Repos.Alergias(m.Repos.PorCodigo("PAC-0047")!.Id).Select(a => a.Substancia), Does.Contain("Látex"));
            Assert.That(m.Repos.Caixas(), Has.Count.EqualTo(8));
            Assert.That(pacientes.Count(p => p.Sexo == "F"), Is.InRange(25, 40));
            Assert.That(pacientes.Select(p => p.Nome).Distinct().Count(), Is.EqualTo(60));
        });
    }

    [Test]
    public void Semente_NaoGuardaTextoEmClaroNaCopiaDaSessao()
    {
        using var m = new Mesa();
        using var db = m.Banco.NovoContexto();

        Assert.That(db.Set<Evolucao>().Count(), Is.GreaterThan(10));
        Assert.That(db.Set<Evolucao>().ToList().Concat<object>(db.Set<Alergia>().ToList()).Count(e => e.ToString()!.Contains(Semente.Prefixo, StringComparison.Ordinal)), Is.Zero);
        Assert.That(db.Set<Evolucao>().ToList().All(e => !e.TextoCifrado.StartsWith(Semente.Prefixo, StringComparison.Ordinal)), Is.True);
        Assert.That(db.Set<Alergia>().ToList().All(a => !a.ReacaoCifrada!.StartsWith(Semente.Prefixo, StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void Isolamento_OQueUmaSessaoGravaAOutraNaoVe()
    {
        using var a = new Mesa();
        using var b = new Mesa();
        var id = a.Repos.PorCodigo("PAC-0002")!.Id;
        var antes = b.Repos.Alergias(id).Count;

        a.Prontuario.AdicionarAlergia(id, "Mofo", null);

        Assert.That(a.Repos.Alergias(id), Has.Count.EqualTo(antes + 1));
        Assert.That(b.Repos.Alergias(id), Has.Count.EqualTo(antes));
    }

    [Test]
    public void Cenario_CqJaNasceReprovado()
    {
        using var m = new Mesa("cq");
        var v = m.Qualidade.Ver();

        Assert.That(v.Reprovado, Is.True);
        Assert.That(v.Regras, Does.Contain("2-2s"));
    }

    [TestCase("Auditoria")]
    [TestCase("Laudo")]
    [TestCase("CustodiaDaAmostra")]
    public void Gatilhos_RecusamAlterarEApagar(string tabela)
    {
        using var m = new Mesa();
        m.Auditoria.Registrar(m.Sessao, "SessaoCriada");
        var coluna = tabela == "Auditoria" ? "Seq" : "Id";

        Assert.That(() => Rodar(m, $"UPDATE {tabela} SET {coluna} = {coluna}"), Throws.TypeOf<SqliteException>().With.Message.Contains("imutavel"));
        Assert.That(() => Rodar(m, $"DELETE FROM {tabela}"), Throws.TypeOf<SqliteException>().With.Message.Contains("imutavel"));
    }

    [Test]
    public void RN_056_LaudoImutavel()
    {
        using var m = new Mesa();

        Assert.That(() => Rodar(m, "UPDATE Laudo SET ConteudoJson = 'x'"), Throws.TypeOf<SqliteException>());
    }

    [Test]
    public void RN_010_EvolucaoImutavel()
    {
        using var m = new Mesa();

        Assert.That(() => Rodar(m, "UPDATE Evolucao SET TextoCifrado = 'x'"), Throws.TypeOf<SqliteException>());
        Assert.That(() => Rodar(m, "DELETE FROM Evolucao"), Throws.TypeOf<SqliteException>());
    }

    [Test]
    public void RN_001_BancoRecusaSegundaReservaAtiva()
    {
        using var m = new Mesa();
        var ocupado = m.Repos.Ativos(m.Repos.Horarios("MED-01", RelogioDeSimulacao.Inicio.Date, RelogioDeSimulacao.Inicio.Date.AddDays(1)).Select(h => h.Id).ToList())[0];

        Assert.That(() => Rodar(m, $"INSERT INTO Agendamento (PacienteId, HorarioId, CriadoEm, Estado) VALUES (1, {ocupado.HorarioId}, '2026-11-04', 'Agendado')"), Throws.TypeOf<SqliteException>());
    }

    [Test]
    public void RN_024_EtiquetaUnica()
    {
        using var m = new Mesa();

        Assert.That(() => Rodar(m, "INSERT INTO Amostra (Etiqueta, PedidoId, PacienteId, Material, Exames, VolumeMl, JejumHoras, Hemolisada, ColetadaEm, ValidadeAte, Estado) SELECT Etiqueta, PedidoId, PacienteId, Material, Exames, VolumeMl, JejumHoras, Hemolisada, ColetadaEm, ValidadeAte, Estado FROM Amostra LIMIT 1"), Throws.TypeOf<SqliteException>());
    }

    [Test]
    public void Esquema_ListaTabelasIndicesEGatilhos()
    {
        using var m = new Mesa();
        var objetos = m.Repos.Objetos();

        Assert.That(objetos.Select(o => o.Tipo).Distinct(), Is.EquivalentTo(new[] { "table", "index", "trigger" }));
        Assert.That(objetos.Select(o => o.Nome), Does.Contain("evolucao_sem_edicao"));
    }

    [Test]
    public void FabricaEmDesign_CriaOContexto()
    {
        using var db = new FabricaEmDesign().CreateDbContext([]);

        Assert.That(db.Model.FindEntityType(typeof(Paciente)), Is.Not.Null);
    }

    private static void Rodar(Mesa m, string sql)
    {
        using var comando = m.Banco.Conexao.CreateCommand();
        comando.CommandText = sql;
        comando.ExecuteNonQuery();
    }
}

public class ClinicaTestes
{
    private static HorarioDaGrade Livre(Mesa m, int dias = 1, string medico = "MED-01")
    {
        var inicio = RelogioDeSimulacao.Inicio.Date.AddDays(dias);
        var horarios = m.Repos.Horarios(medico, inicio, inicio.AddDays(1));
        var ocupados = m.Repos.Ativos(horarios.Select(h => h.Id).ToList()).Select(a => a.HorarioId).ToHashSet();
        return horarios.First(h => !ocupados.Contains(h.Id));
    }

    [Test]
    public void Busca_AchaPorNomeOuCodigoEPagina()
    {
        using var m = new Mesa();

        Assert.That(m.Pacientes.Buscar("PAC-0014", 1).Total, Is.EqualTo(1));
        Assert.That(m.Pacientes.Buscar(m.Repos.Todos()[0].Nome[..4], 0).Pagina, Is.EqualTo(1));
        var tudo = m.Pacientes.Buscar(null, 2);
        Assert.Multiple(() =>
        {
            Assert.That(tudo.Total, Is.EqualTo(60));
            Assert.That(tudo.Paginas, Is.EqualTo(3));
            Assert.That(tudo.Itens, Has.Count.EqualTo(ServicoDePacientes.TamanhoDaPagina));
        });
    }

    [TestCase("' OR '1'='1")]
    [TestCase("'; DROP TABLE Paciente;--")]
    [TestCase("100%")]
    [TestCase("_")]
    [TestCase("\\")]
    public void Busca_EntradaHostilEDadoNuncaSql(string entrada)
    {
        using var m = new Mesa();
        var antes = m.Repos.Objetos();

        Assert.That(m.Pacientes.Buscar(entrada, 1).Total, Is.Zero);
        Assert.That(m.Repos.Objetos(), Is.EqualTo(antes));
    }

    [Test]
    public void Busca_CortaEm64Caracteres()
    {
        using var m = new Mesa();

        Assert.That(m.Pacientes.Buscar(new string('a', 500), 1).Total, Is.Zero);
    }

    [Test]
    public void Ficha_MontaAlergiasHorariosELinhaDoTempo()
    {
        using var m = new Mesa();
        var p = m.Repos.PorCodigo("PAC-0047")!;
        m.Auditoria.Registrar(m.Sessao, "Teste", "Paciente", p.Codigo, $"{{\"paciente\":\"{p.Codigo}\"}}");

        var f = m.Pacientes.Ficha(p.Id);

        Assert.Multiple(() =>
        {
            Assert.That(f.Alergias.Single(a => a.Alergia.Substancia == "Látex").Reacao, Is.EqualTo("Dermatite de contato"));
            Assert.That(f.Agendamentos, Is.Not.Empty);
            Assert.That(f.LinhaDoTempo, Has.Count.EqualTo(1));
            Assert.That(f.Idade, Is.GreaterThan(0));
        });
        Assert.That(() => m.Pacientes.Ficha(9999), Throws.TypeOf<NaoEncontrado>());
    }

    [Test]
    public void Semana_MostraAGradeEMarcaFaltas()
    {
        using var m = new Mesa();
        var s = m.Agenda.Semana("MED-02", 0);

        Assert.Multiple(() =>
        {
            Assert.That(s.Profissional, Is.EqualTo("MED-02"));
            Assert.That(s.Segunda, Is.EqualTo(new DateOnly(2026, 11, 2)));
            Assert.That(s.Celulas, Has.Count.EqualTo(90));
            Assert.That(s.Celulas.Any(c => c.Passado), Is.True);
            Assert.That(s.Celulas.Any(c => c.Paciente is not null), Is.True);
        });
        Assert.That(m.Agenda.Semana("inventado", 9).Profissional, Is.EqualTo("MED-01"));
        Assert.That(m.Agenda.Semana(null, 9).Deslocamento, Is.EqualTo(ServicoDeAgenda.SemanasVisiveis - 1));
    }

    [Test]
    public void RN_005_FaltaEMarcadaQuandoOHorarioPassa()
    {
        using var m = new Mesa();
        var ontem = RelogioDeSimulacao.Inicio.Date.AddHours(8);
        var h = m.Repos.Horarios("MED-02", ontem, ontem.AddHours(10)).First(x => x.Inicio < RelogioDeSimulacao.Inicio && m.Repos.AtivoNoHorario(x.Id) is null);
        var a = new Agendamento(1, h.Id, null, ontem.AddDays(-1));
        m.Repos.Adicionar(a);
        m.Repos.Salvar();

        m.Agenda.Semana("MED-02", 0);

        Assert.That(a.Estado, Is.EqualTo(EstadoAgendamento.Faltou));
        Assert.That(m.Sessao.Eventos.Select(e => e.Acao), Does.Contain("FaltaRegistrada"));
    }

    [Test]
    public void RN_001_HorarioOcupadoRecusaSegundaReserva()
    {
        using var m = new Mesa();
        var h = Livre(m);
        m.Agenda.Agendar(h.Id, "PAC-0002", "retorno");

        Assert.That(Mesa.Erro(() => m.Agenda.Agendar(h.Id, "PAC-0003", null)), Is.EqualTo("agenda.erro.ocupado"));
    }

    [Test]
    public void RN_003_ForaDaGradeRecusado()
    {
        using var m = new Mesa();

        Assert.That(Mesa.Erro(() => m.Agenda.Agendar(99999, "PAC-0002", null)), Is.EqualTo("agenda.erro.foradagrade"));
        Assert.That(Mesa.Erro(() => m.Agenda.Agendar(Livre(m).Id, "PAC-9999", null)), Is.EqualTo("agenda.erro.paciente"));
        Assert.That(Mesa.Erro(() => m.Agenda.Agendar(Livre(m).Id, null, null)), Is.EqualTo("agenda.erro.paciente"));
        Assert.That(Mesa.Erro(() => m.Agenda.Agendar(Livre(m).Id, "PAC-0002", new string('x', 201))), Is.EqualTo("comum.erro.textolongo"));
    }

    [Test]
    public void RN_004_CancelarLiberaOHorarioParaOutraReserva()
    {
        using var m = new Mesa();
        var h = Livre(m);
        var a = m.Agenda.Agendar(h.Id, "PAC-0002", null);
        m.Agenda.Confirmar(a.Id);
        m.Agenda.Cancelar(a.Id);

        Assert.That(m.Agenda.Agendar(h.Id, "PAC-0003", null).HorarioId, Is.EqualTo(h.Id));
        Assert.That(() => m.Agenda.Confirmar(99999), Throws.TypeOf<NaoEncontrado>());
    }

    [Test]
    public void Reagendar_MoveEExigeHorarioLivreEValido()
    {
        using var m = new Mesa();
        var a = m.Agenda.Agendar(Livre(m).Id, "PAC-0002", null);
        var novo = Livre(m, 2);
        m.Agenda.Reagendar(a.Id, novo.Id);
        var outro = m.Agenda.Agendar(Livre(m).Id, "PAC-0003", null);

        Assert.That(a.HorarioId, Is.EqualTo(novo.Id));
        Assert.That(Mesa.Erro(() => m.Agenda.Reagendar(outro.Id, novo.Id)), Is.EqualTo("agenda.erro.ocupado"));
        Assert.That(Mesa.Erro(() => m.Agenda.Reagendar(a.Id, 99999)), Is.EqualTo("agenda.erro.foradagrade"));
    }

    [Test]
    public void RN_060_CotaDeRegistros()
    {
        using var m = new Mesa();
        for (var i = 0; i < Sessao.CotaDeRegistros; i++)
        {
            m.Sessao.ContarRegistro();
        }

        Assert.That(Mesa.Erro(() => m.Agenda.Agendar(Livre(m).Id, "PAC-0002", null)), Is.EqualTo("sessao.erro.cota"));
    }

    [Test]
    public void Fila_HojeESemana()
    {
        using var m = new Mesa();

        Assert.That(m.Atendimento.Fila(false), Is.Not.Empty);
        Assert.That(m.Atendimento.Fila(true).Count, Is.GreaterThan(m.Atendimento.Fila(false).Count));
    }

    private static Agendamento Linha(Mesa m, string paciente) => m.Atendimento.Fila(false).Single(l => l.Paciente.Codigo == paciente).Agendamento;

    [Test]
    public void Atendimento_JornadaCompletaComEvolucaoCifrada()
    {
        using var m = new Mesa();
        var a = Linha(m, "PAC-0001");
        m.Atendimento.CheckIn(a.Id);
        m.Operar(Papel.Medico, "MED-01");
        m.Atendimento.Iniciar(a.Id);
        Assert.That(Mesa.Erro(() => m.Atendimento.Concluir(a.Id)), Is.EqualTo("atendimento.erro.semevolucao"));
        m.Prontuario.Registrar(a.Id, "  Texto da consulta.  ");
        m.Atendimento.Concluir(a.Id);

        var d = m.Atendimento.Detalhe(a.Id);
        Assert.Multiple(() =>
        {
            Assert.That(a.Estado, Is.EqualTo(EstadoAgendamento.Concluido));
            Assert.That(d.Deste.Single().Texto, Is.EqualTo("Texto da consulta."));
            Assert.That(d.Deste.Single().Evolucao.TextoCifrado, Does.Not.Contain("consulta"));
            Assert.That(d.Pedidos, Is.Empty);
            Assert.That(d.Idade, Is.GreaterThan(0));
        });
    }

    [Test]
    public void Dados_DecifraOMotivoECalculaAIdade()
    {
        using var m = new Mesa();
        var a = m.Agenda.Agendar(Livre(m).Id, "PAC-0002", "retorno do exame");
        var p = m.Repos.PorCodigo("PAC-0002")!;
        var vazio = m.Agenda.Agendar(Livre(m, 2).Id, "PAC-0003", null);

        var d = m.Agenda.Dados(a, p);

        Assert.That(d.Motivo, Is.EqualTo("retorno do exame"));
        Assert.That(d.Idade, Is.GreaterThan(0));
        Assert.That(m.Agenda.Dados(vazio, m.Repos.PorCodigo("PAC-0003")!).Motivo, Is.Null);
    }

    [Test]
    public void Resumo_EvolucaoSoSaiParaQuemPodeVerEvolucoes()
    {
        using var m = new Mesa();
        var a = Linha(m, "PAC-0001");
        m.Atendimento.CheckIn(a.Id);
        m.Operar(Papel.Medico, "MED-01");
        m.Atendimento.Iniciar(a.Id);
        m.Prontuario.Registrar(a.Id, "Texto sigiloso.");
        m.Atendimento.Concluir(a.Id);

        var clinico = m.Atendimento.Resumo(a.Id, true);
        var semAcesso = m.Atendimento.Resumo(a.Id, false);

        Assert.Multiple(() =>
        {
            Assert.That(clinico.Evolucoes.Single().Texto, Is.EqualTo("Texto sigiloso."));
            Assert.That(semAcesso.Evolucoes, Is.Empty);
            Assert.That(semAcesso.Linha.Paciente.Codigo, Is.EqualTo("PAC-0001"));
            Assert.That(semAcesso.Idade, Is.GreaterThan(0));
            Assert.That(semAcesso.Pedidos, Is.Empty);
        });
        Assert.That(() => m.Atendimento.Resumo(99999, true), Throws.TypeOf<NaoEncontrado>());
    }

    [Test]
    public void Consultar_AchaEtiquetaPorCodigoPacienteOuNome()
    {
        using var m = new Mesa();
        var amostra = m.Coleta.Coletar(m.PedidoDoCenario.Id, Catalogo.Soro, 5, 12, false);
        var paciente = m.Coleta.Etiqueta(amostra.Id).Paciente;

        Assert.Multiple(() =>
        {
            Assert.That(m.Coleta.Consultar(null).Select(l => l.Amostra.Id), Does.Contain(amostra.Id));
            Assert.That(m.Coleta.Consultar("  " + amostra.Etiqueta.ToLowerInvariant() + " ").Single().Amostra.Id, Is.EqualTo(amostra.Id));
            Assert.That(m.Coleta.Consultar(paciente.Codigo).Select(l => l.Amostra.Id), Does.Contain(amostra.Id));
            Assert.That(m.Coleta.Consultar(paciente.Nome[..5]).Select(l => l.Amostra.Id), Does.Contain(amostra.Id));
            Assert.That(m.Coleta.Consultar("nao-existe"), Is.Empty);
            Assert.That(m.Coleta.Consultar(new string('x', 500)), Is.Empty);
        });
    }

    [Test]
    public void RN_007_SoOMedicoDoHorarioEscreve()
    {
        using var m = new Mesa();
        var a = Linha(m, "PAC-0014");
        m.Operar(Papel.Medico, "MED-02");

        Assert.That(Mesa.Erro(() => m.Prontuario.Registrar(a.Id, "x")), Is.EqualTo("atendimento.erro.outroprofissional"));
    }

    [Test]
    public void Evolucao_SoDuranteOAtendimento()
    {
        using var m = new Mesa();
        var a = Linha(m, "PAC-0014");
        m.Operar(Papel.Medico, "MED-01");

        Assert.That(Mesa.Erro(() => m.Prontuario.Registrar(a.Id, "x")), Is.EqualTo("regra.transicao"));
        Assert.That(() => m.Prontuario.Registrar(99999, "x"), Throws.TypeOf<NaoEncontrado>());
        Assert.That(() => m.Atendimento.Detalhe(99999), Throws.TypeOf<NaoEncontrado>());
    }

    [Test]
    public void RN_011_CorrecaoEncadeada()
    {
        using var m = new Mesa();
        var a = Linha(m, "PAC-0047");
        m.Operar(Papel.Medico, "MED-01");
        m.Prontuario.Registrar(a.Id, "Original");
        var original = m.Repos.Evolucoes(a.PacienteId).Single(e => e.AgendamentoId == a.Id);

        m.Prontuario.Corrigir(original.Id, "Corrigido");

        var v = m.Prontuario.Ver(a.PacienteId, true);
        Assert.Multiple(() =>
        {
            Assert.That(v.Evolucoes.First(e => e.Evolucao.Id == original.Id).FoiCorrigida, Is.True);
            Assert.That(v.Evolucoes.Single(e => e.Texto == "Corrigido").Evolucao.CorrigeId, Is.EqualTo(original.Id));
        });
        Assert.That(() => m.Prontuario.Corrigir(99999, "x"), Throws.TypeOf<NaoEncontrado>());
        Assert.That(Mesa.Erro(() => m.Prontuario.Corrigir(original.Id, " ")), Is.EqualTo("prontuario.erro.texto"));
    }

    [Test]
    public void Prontuario_ColetaNaoDecifraEvolucoes()
    {
        using var m = new Mesa();
        var id = m.Repos.Evolucoes(47).Count > 0 ? 47 : m.Repos.Todos()[0].Id;

        var v = m.Prontuario.Ver(id, false);

        Assert.That(v.Evolucoes, Is.Empty);
        Assert.That(v.Pedidos, Is.Empty);
        Assert.That(() => m.Prontuario.Ver(99999, true), Throws.TypeOf<NaoEncontrado>());
    }

    [Test]
    public void Alergia_AdicionaInativaEValida()
    {
        using var m = new Mesa();
        var id = m.Repos.PorCodigo("PAC-0002")!.Id;
        m.Prontuario.AdicionarAlergia(id, "Mofo", "Rinite");
        var v = m.Prontuario.Ver(id, true).Alergias.Single(a => a.Alergia.Substancia == "Mofo");
        m.Prontuario.AlternarAlergia(v.Alergia.Id);

        Assert.That(v.Reacao, Is.EqualTo("Rinite"));
        Assert.That(v.Alergia.Ativa, Is.False);
        Assert.That(Mesa.Erro(() => m.Prontuario.AdicionarAlergia(id, " ", null)), Is.EqualTo("prontuario.erro.substancia"));
        Assert.That(() => m.Prontuario.AdicionarAlergia(9999, "x", null), Throws.TypeOf<NaoEncontrado>());
        Assert.That(() => m.Prontuario.AlternarAlergia(9999), Throws.TypeOf<NaoEncontrado>());
    }

    [Test]
    public void Alergia_SemReacaoNaoGuardaCifra()
    {
        using var m = new Mesa();
        var id = m.Repos.PorCodigo("PAC-0003")!.Id;
        m.Prontuario.AdicionarAlergia(id, "Pólen", null);

        Assert.That(m.Prontuario.Ver(id, false).Alergias.Single(a => a.Alergia.Substancia == "Pólen").Reacao, Is.Null);
    }

    [Test]
    public void RN_013_AlergiaGeraAlerta()
    {
        using var m = new Mesa();

        Assert.That(m.Pedidos.AlergiasAtivas("PAC-0047").Select(a => a.Substancia), Does.Contain("Látex"));
        Assert.That(m.Pedidos.AlergiasAtivas(null), Is.Empty);
    }
}

public class LaboratorioTestes
{
    [Test]
    public void Pedido_CriaCancelaEValida()
    {
        using var m = new Mesa();
        var p = m.Pedidos.Criar("PAC-0002", null, ["GLI", "HEM"], true);

        Assert.That(p.Codigo, Is.EqualTo("PED-000047"));
        Assert.That(m.Pedidos.Detalhe(p.Id).Paciente.Codigo, Is.EqualTo("PAC-0002"));
        m.Pedidos.Cancelar(p.Id);
        Assert.That(p.Estado, Is.EqualTo(EstadoPedido.Cancelado));
        Assert.That(Mesa.Erro(() => m.Pedidos.Criar("XXX", null, ["GLI"], false)), Is.EqualTo("agenda.erro.paciente"));
        Assert.That(Mesa.Erro(() => m.Pedidos.Criar(null, null, ["GLI"], false)), Is.EqualTo("agenda.erro.paciente"));
        Assert.That(() => m.Pedidos.Detalhe(9999), Throws.TypeOf<NaoEncontrado>());
        Assert.That(() => m.Pedidos.Cancelar(9999), Throws.TypeOf<NaoEncontrado>());
    }

    [Test]
    public void Coleta_SoMostraOsTubosQueFaltam()
    {
        using var m = new Mesa();
        var p = m.Pedidos.Criar("PAC-0002", null, ["GLI", "HEM"], false);
        Assert.That(m.Coleta.Pendentes().Count(t => t.Pedido.Id == p.Id), Is.EqualTo(2));

        m.Coleta.Coletar(p.Id, Catalogo.Soro, 5, 12, false);

        Assert.That(m.Coleta.Pendentes().Single(t => t.Pedido.Id == p.Id).Tubo.Material, Is.EqualTo(Catalogo.SangueTotal));
        Assert.That(p.Estado, Is.EqualTo(EstadoPedido.Solicitado), "falta um tubo");
        Assert.That(Mesa.Erro(() => m.Coleta.Coletar(p.Id, Catalogo.Soro, 5, 12, false)), Is.EqualTo("coleta.erro.jacoletado"));
        Assert.That(Mesa.Erro(() => m.Coleta.Coletar(p.Id, "Urina", 5, 12, false)), Is.EqualTo("coleta.erro.tubo"));
        Assert.That(() => m.Coleta.Coletar(9999, Catalogo.Soro, 5, 12, false), Throws.TypeOf<NaoEncontrado>());

        m.Coleta.Coletar(p.Id, Catalogo.SangueTotal, 5, 0, false);
        Assert.That(p.Estado, Is.EqualTo(EstadoPedido.Coletado));
    }

    [Test]
    public void RN_025_RejeicaoGeraRecoleta()
    {
        using var m = new Mesa();
        var pedido = m.PedidoDoCenario;
        var a = m.Coleta.Coletar(pedido.Id, Catalogo.Soro, 5, 12, true);
        Assert.That(pedido.Estado, Is.EqualTo(EstadoPedido.Coletado));
        Assert.That(m.Coleta.Etiqueta(a.Id).Barras, Is.Not.Empty);
        Assert.That(m.Coleta.Amostras().Select(l => l.Amostra.Id), Does.Contain(a.Id));
        Assert.That(Mesa.Erro(() => m.Coleta.Rejeitar(a.Id, "Inventado")), Is.EqualTo("coleta.erro.motivo"));

        m.Coleta.Rejeitar(a.Id, "Hemolise");

        Assert.That(pedido.Estado, Is.EqualTo(EstadoPedido.Solicitado));
        Assert.That(m.Coleta.Pendentes().Any(t => t.Pedido.Id == pedido.Id), Is.True);
        Assert.That(m.Repos.Custodia(a.Id).Select(c => c.Evento), Is.EqualTo(new[] { "Coletada", "Rejeitada" }));
        Assert.That(() => m.Coleta.Rejeitar(9999, "Hemolise"), Throws.TypeOf<NaoEncontrado>());
        Assert.That(() => m.Coleta.Etiqueta(9999), Throws.TypeOf<NaoEncontrado>());
    }

    [Test]
    public void Rejeitar_ArmazenadaLiberaAPosicao()
    {
        using var m = new Mesa();
        var a = m.Coleta.Coletar(m.PedidoDoCenario.Id, Catalogo.Soro, 5, 12, false);
        var pos = m.Repos.Posicoes("R2-C1")[0];
        m.Soroteca.Armazenar(a.Id, pos.Id);

        m.Coleta.Rejeitar(a.Id, "VolumeInsuficiente");

        Assert.That(pos.AmostraId, Is.Null);
    }

    [Test]
    public void RN_035_CustodiaCompleta()
    {
        using var m = new Mesa();
        var a = m.Coleta.Coletar(m.PedidoDoCenario.Id, Catalogo.Soro, 5, 12, false);
        var (origem, destino) = (m.Repos.Posicoes("R2-C1")[0], m.Repos.Posicoes("R2-C1")[1]);
        m.Soroteca.Armazenar(a.Id, origem.Id);
        m.Soroteca.Mover(origem.Id, destino.Id);
        m.Soroteca.Descartar(a.Id, "Fim do prazo");

        Assert.Multiple(() =>
        {
            Assert.That(m.Repos.Custodia(a.Id).Select(c => c.Evento), Is.EqualTo(new[] { "Coletada", "Recebida", "Armazenada", "Movida", "Descartada" }));
            Assert.That(destino.AmostraId, Is.Null);
            Assert.That(a.MotivoDescarteCifrado, Does.Not.Contain("prazo"));
        });
    }

    [Test]
    public void Soroteca_ValidaPosicaoEAmostra()
    {
        using var m = new Mesa();
        var ocupada = m.Soroteca.Ver("R1-C1", null).Celulas.First(c => c.Amostra is not null).Posicao;
        var outra = m.Soroteca.Ver("R1-C1", null).Celulas.Last(c => c.Amostra is not null).Posicao;
        var livre = m.Soroteca.Ver("R2-C1", null).Celulas[0].Posicao;

        Assert.Multiple(() =>
        {
            Assert.That(() => m.Soroteca.Armazenar(9999, livre.Id), Throws.TypeOf<NaoEncontrado>());
            Assert.That(() => m.Soroteca.Armazenar(1, 99999), Throws.TypeOf<NaoEncontrado>());
            Assert.That(() => m.Soroteca.Mover(99999, livre.Id), Throws.TypeOf<NaoEncontrado>());
            Assert.That(() => m.Soroteca.Mover(ocupada.Id, 99999), Throws.TypeOf<NaoEncontrado>());
            Assert.That(() => m.Soroteca.Descartar(9999, "x"), Throws.TypeOf<NaoEncontrado>());
            Assert.That(Mesa.Erro(() => m.Soroteca.Mover(livre.Id, ocupada.Id)), Is.EqualTo("soroteca.erro.vazia"));
            Assert.That(Mesa.Erro(() => m.Soroteca.Mover(ocupada.Id, outra.Id)), Is.EqualTo("soroteca.erro.ocupada"));
            Assert.That(Mesa.Erro(() => m.Soroteca.Descartar(ocupada.AmostraId!.Value, " ")), Is.EqualTo("soroteca.erro.motivo"));
        });
    }

    [Test]
    public void Soroteca_VisaoEDetalheDaPosicao()
    {
        using var m = new Mesa();
        var v = m.Soroteca.Ver("inventada", null);
        var ocupada = v.Celulas.First(c => c.Amostra is not null).Posicao;
        var livre = v.Celulas.First(c => c.Amostra is null).Posicao;

        Assert.Multiple(() =>
        {
            Assert.That(v.Caixa, Is.EqualTo("R1-C1"));
            Assert.That(v.Selecionada, Is.Null);
            Assert.That(v.Celulas.Any(c => c.Vencida), Is.True);
            Assert.That(v.Celulas.Any(c => c.Vencendo), Is.True);
            Assert.That(m.Soroteca.Ver("R1-C1", ocupada.Id).Selecionada!.Paciente, Is.Not.Null);
            Assert.That(m.Soroteca.Ver("R1-C1", ocupada.Id).Selecionada!.Custodia, Is.Not.Empty);
            Assert.That(m.Soroteca.Ver("R1-C1", livre.Id).Selecionada!.Amostra, Is.Null);
            Assert.That(m.Soroteca.Ver("R1-C1", 99999).Selecionada, Is.Null);
            Assert.That(m.Soroteca.Ver(null, null).Caixa, Is.EqualTo("R1-C1"));
        });
    }

    [Test]
    public void Equipamento_ProcessaPassandoPeloProtocoloEResultadoSaiDaMensagem()
    {
        using var m = new Mesa();
        var pedido = m.ProcessarCenario();
        var rs = m.ResultadosDe(pedido.Id);
        var mensagens = m.Equipamentos.Ver().Mensagens;

        Assert.Multiple(() =>
        {
            Assert.That(pedido.Estado, Is.EqualTo(EstadoPedido.ResultadoPreliminar));
            Assert.That(rs.Select(r => r.Analito), Is.EquivalentTo(new[] { "SOD", "POT", "GLI", "CRE" }));
            Assert.That(rs.Single(r => r.Analito == "POT").Valor, Is.EqualTo(7.1));
            Assert.That(rs.Single(r => r.Analito == "POT").Flag, Is.EqualTo("CH"));
            Assert.That(rs.Single(r => r.Analito == "POT").DeltaSuspeito, Is.True);
            Assert.That(rs.Single(r => r.Analito == "POT").Anterior, Is.EqualTo(4.4));
            Assert.That(mensagens.Select(x => x.Texto), Does.Contain("ENQ"));
            Assert.That(m.Coleta.Amostras().Single(l => l.Pedido.Id == pedido.Id).Amostra.Estado, Is.EqualTo(EstadoAmostra.Armazenada));
        });
    }

    [Test]
    public void Equipamento_Hl7EUrgenteEProntasDeSangueTotal()
    {
        using var m = new Mesa();
        var p = m.Pedidos.Criar("PAC-0002", null, ["HEM"], true);
        var a = m.Coleta.Coletar(p.Id, Catalogo.SangueTotal, 5, 0, false);
        m.Operar(Papel.Analista, "ANA-01");
        Assert.That(m.Equipamentos.Ver().Prontas.Select(x => x.Id), Does.Contain(a.Id));

        m.Equipamentos.Enviar(a.Id);
        Assert.That(m.Equipamentos.Ver().Prontas.Select(x => x.Id), Does.Not.Contain(a.Id));
        m.AndarOTempo(TimeSpan.FromSeconds(13));

        Assert.Multiple(() =>
        {
            Assert.That(p.Estado, Is.EqualTo(EstadoPedido.ResultadoPreliminar));
            Assert.That(a.Estado, Is.EqualTo(EstadoAmostra.Analisada));
            Assert.That(m.ResultadosDe(p.Id), Has.Count.EqualTo(3));
            Assert.That(m.Repos.Mensagens(50).Any(x => x.Texto.StartsWith("OBX", StringComparison.Ordinal)), Is.True);
        });
        Assert.That(Mesa.Erro(() => m.Equipamentos.Enviar(a.Id)), Is.EqualTo("regra.transicao"));
    }

    [Test]
    public void Equipamento_RecusaAmostraInexistenteOuDePedidoEncerrado()
    {
        using var m = new Mesa();
        var ja = m.Soroteca.Ver("R1-C1", null).Celulas.First(c => c.Amostra is not null).Amostra!;

        Assert.That(() => m.Equipamentos.Enviar(9999), Throws.TypeOf<NaoEncontrado>());
        Assert.That(Mesa.Erro(() => m.Equipamentos.Enviar(ja.Id)), Is.EqualTo("regra.transicao"));
        Assert.That(() => m.Equipamentos.Reprocessar(9999), Throws.TypeOf<NaoEncontrado>());
    }

    [Test]
    public void Equipamento_ComEsperaDePedidoIncompletoNaoLiberaResultado()
    {
        using var m = new Mesa();
        var p = m.Pedidos.Criar("PAC-0002", null, ["ELE", "HEM"], false);
        var soro = m.Coleta.Coletar(p.Id, Catalogo.Soro, 5, 0, false);
        var sangue = m.Coleta.Coletar(p.Id, Catalogo.SangueTotal, 5, 0, false);
        m.Soroteca.Armazenar(soro.Id, m.Repos.Posicoes("R2-C2")[0].Id);
        m.Equipamentos.Enviar(soro.Id);
        m.AndarOTempo(TimeSpan.FromSeconds(7));

        Assert.That(p.Estado, Is.EqualTo(EstadoPedido.EmAnalise), "falta o hemograma");
        m.Equipamentos.Enviar(sangue.Id);
        m.AndarOTempo(TimeSpan.FromSeconds(13));
        Assert.That(p.Estado, Is.EqualTo(EstadoPedido.ResultadoPreliminar));
    }

    [Test]
    public void Equipamento_ErroDoCenarioEReprocesso()
    {
        using var m = new Mesa("erro");
        var pedido = m.PedidoDoCenario;
        var a = m.Coleta.Coletar(pedido.Id, Catalogo.Soro, 5, 12, false);
        m.Soroteca.Armazenar(a.Id, m.Repos.Posicoes("R2-C2")[0].Id);
        m.Equipamentos.Enviar(a.Id);
        m.AndarOTempo(TimeSpan.FromSeconds(9));

        var erro = m.Equipamentos.Ver().Equipamentos[0].Fila.Single(l => l.Execucao.Estado == EstadoExecucao.Erro);
        Assert.That(m.Sessao.Eventos.Count(e => e.Acao == "ExecucaoComErro"), Is.EqualTo(1));
        m.Equipamentos.Reprocessar(erro.Execucao.Id);
        m.AndarOTempo(TimeSpan.FromSeconds(9));

        Assert.That(pedido.Estado, Is.EqualTo(EstadoPedido.ResultadoPreliminar));
    }

    [Test]
    public void Equipamento_SoListaQuemPodeIr()
    {
        using var m = new Mesa();
        var p = m.Pedidos.Criar("PAC-0002", null, ["GLI"], false);
        var soroSoltO = m.Coleta.Coletar(p.Id, Catalogo.Soro, 5, 12, false);
        var h = m.Pedidos.Criar("PAC-0003", null, ["HEM"], false);
        var hemolisada = m.Coleta.Coletar(h.Id, Catalogo.SangueTotal, 5, 0, true);

        var prontas = m.Equipamentos.Ver().Prontas.Select(a => a.Id).ToList();

        Assert.That(prontas, Does.Not.Contain(soroSoltO.Id), "soro precisa passar pela soroteca");
        Assert.That(prontas, Does.Not.Contain(hemolisada.Id));
        Assert.That(m.Soroteca.Ver(null, null).Aguardando.Select(a => a.Id), Does.Contain(soroSoltO.Id).And.Not.Contain(hemolisada.Id));
    }

    [Test]
    public void Equipamento_ErroAntigoNaoGeraEventoDeNovoEOperacaoSoContaAtivas()
    {
        using var m = new Mesa("erro");
        var pedido = m.PedidoDoCenario;
        var a = m.Coleta.Coletar(pedido.Id, Catalogo.Soro, 5, 12, false);
        m.Soroteca.Armazenar(a.Id, m.Repos.Posicoes("R2-C2")[0].Id);
        Assert.That(m.Equipamentos.Ver().Prontas.Select(x => x.Id), Does.Contain(a.Id));
        m.Equipamentos.Enviar(a.Id);
        Assert.That(m.Operacao.Ver().Filas[0].Itens, Is.EqualTo(3));

        m.AndarOTempo(TimeSpan.FromSeconds(5));
        m.AndarOTempo(TimeSpan.FromSeconds(5));

        Assert.That(m.Sessao.Eventos.Count(e => e.Acao == "ExecucaoComErro"), Is.EqualTo(1));
        Assert.That(m.Operacao.Ver().Filas[0].Itens, Is.Zero);
    }

    [Test]
    public void Relogio_PausaEVelocidade()
    {
        using var m = new Mesa();
        m.Equipamentos.Relogio("pausar");
        Assert.That(m.Sessao.Simulacao.Pausado, Is.True);
        m.Equipamentos.Relogio("600");
        Assert.That(m.Sessao.Simulacao.Fator, Is.EqualTo(600));
        Assert.That(Mesa.Erro(() => m.Equipamentos.Relogio("7")), Is.EqualTo("equipamento.erro.fator"));
        Assert.That(Mesa.Erro(() => m.Equipamentos.Relogio("texto")), Is.EqualTo("equipamento.erro.fator"));
    }

    [Test]
    public void Relogio_PausadoNaoAvancaANalise()
    {
        using var m = new Mesa();
        var pedido = m.PedidoDoCenario;
        var a = m.Coleta.Coletar(pedido.Id, Catalogo.Soro, 5, 12, false);
        m.Soroteca.Armazenar(a.Id, m.Repos.Posicoes("R2-C2")[0].Id);
        m.Equipamentos.Enviar(a.Id);
        m.Equipamentos.Relogio("pausar");
        m.Relogio.Avancar(TimeSpan.FromMinutes(30));

        m.Equipamentos.Ver();

        Assert.That(pedido.Estado, Is.EqualTo(EstadoPedido.EmAnalise));
        Assert.That(m.Sessao.Simulacao.Agora, Is.EqualTo(RelogioDeSimulacao.Inicio));
    }

    [Test]
    public void RelogioDeSimulacao_SemLeituraAnteriorNaoAnda()
    {
        var r = new RelogioDeSimulacao();

        Assert.That(r.Avancar(DateTimeOffset.UnixEpoch), Is.Zero);
        Assert.That(r.Avancar(DateTimeOffset.UnixEpoch.AddSeconds(60)), Is.EqualTo(60));
    }

    [Test]
    public void Cenarios_ValoresForcados()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Cenarios.ValorForcado("critico", "PED-1", "POT"), Is.Null);
            Assert.That(Cenarios.ValorForcado("feliz", Cenarios.PedidoDoCenario, "POT"), Is.EqualTo(4.5));
            Assert.That(Cenarios.ValorForcado("erro", Cenarios.PedidoDoCenario, "POT"), Is.EqualTo(7.1));
            Assert.That(Cenarios.ValorForcado("feliz", Cenarios.PedidoDoCenario, "HGB"), Is.Null);
            Assert.That(Cenarios.ExameFalha("erro", "GLI"), Is.True);
            Assert.That(Cenarios.ExameFalha("erro", "CRE"), Is.False);
            Assert.That(Cenarios.ExameFalha("critico", "GLI"), Is.False);
            Assert.That(Cenarios.SugereHemolise("hemolisada", Cenarios.PedidoDoCenario), Is.True);
            Assert.That(Cenarios.SugereHemolise("critico", Cenarios.PedidoDoCenario), Is.False);
            Assert.That(Cenarios.SugereHemolise("hemolisada", "PED-1"), Is.False);
        });
    }

    [TestCase("POT", "F")]
    [TestCase("COL", "M")]
    [TestCase("CRE", "F")]
    public void Gerador_ValorFicaNaFaixaEERepetivel(string codigo, string sexo)
    {
        var analito = Catalogo.AnalitoDe(codigo);
        var f = analito.FaixaPara(sexo);
        var v = GeradorDeValores.Normal(analito, sexo, "PED-000001");

        Assert.That(v, Is.EqualTo(GeradorDeValores.Normal(analito, sexo, "PED-000001")));
        Assert.That(v, Is.GreaterThanOrEqualTo(f.NormalMin ?? 0));
        Assert.That(v, Is.LessThanOrEqualTo(f.NormalMax ?? double.MaxValue));
    }

    [Test]
    public void Gerador_FaixaSoComMinimo()
    {
        var analito = new Analito { Codigo = "X", Unidade = "u", Casas = 1, DeltaPct = 10, Faixas = [new Faixa { Sexo = "A", NormalMin = 10 }] };

        Assert.That(GeradorDeValores.Normal(analito, "A", "s"), Is.GreaterThan(10));
    }

    [Test]
    public void RN_043_ControleReprovadoBloqueiaOEquipamentoEUmaMedidaBoaLibera()
    {
        using var m = new Mesa("cq");
        var pedido = m.ProcessarCenario();
        m.Operar(Papel.Analista, "ANA-02");
        var r = m.ResultadosDe(pedido.Id).First(x => x.Analito == "SOD");

        Assert.That(Mesa.Erro(() => m.Resultados.Validar(r.Id, null)), Is.EqualTo("resultado.erro.cq"));
        Assert.That(m.Qualidade.Reprovado(Catalogo.Hematologia), Is.False);
        m.Qualidade.Registrar();
        m.Resultados.Validar(r.Id, "ok");
        Assert.That(r.Estado, Is.EqualTo(EstadoResultado.Validado));
    }

    [Test]
    public void Qualidade_AvisoNaoBloqueia()
    {
        using var m = new Mesa();
        var v = m.Qualidade.Ver();

        Assert.That(v.Reprovado, Is.False);
        Assert.That(v.Serie, Has.Count.EqualTo(20));
    }

    [Test]
    public void RN_055_LiberacaoCompletaERetificacaoCriaVersao()
    {
        using var m = new Mesa();
        var pedido = m.ProcessarCenario();
        Assert.That(Mesa.Erro(() => m.Resultados.Liberar(pedido.Id)), Is.EqualTo("laudo.erro.incompleto"));
        m.Operar(Papel.Analista, "ANA-02");
        var pendentes = m.Resultados.Pendentes().Single(p => p.Pedido.Id == pedido.Id);
        foreach (var r in pendentes.Resultados)
        {
            if (r.Critico)
            {
                m.Resultados.ConfirmarCritico(r.Id);
            }

            m.Resultados.Validar(r.Id, r.DeltaSuspeito ? "Conferido" : null);
        }

        var laudo = m.Resultados.Liberar(pedido.Id);
        var visao = m.Resultados.Ver(laudo.Id);
        Assert.Multiple(() =>
        {
            Assert.That(pedido.Estado, Is.EqualTo(EstadoPedido.Liberado));
            Assert.That(visao.Itens, Has.Count.EqualTo(4));
            Assert.That(visao.Motivo, Is.Null);
            Assert.That(visao.Idade, Is.GreaterThan(0));
        });

        var nova = m.Resultados.Retificar(pedido.Id, "Erro de digitação");
        var ver2 = m.Resultados.Ver(nova.Id);
        Assert.Multiple(() =>
        {
            Assert.That(nova.Versao, Is.EqualTo(2));
            Assert.That(ver2.Motivo, Is.EqualTo("Erro de digitação"));
            Assert.That(ver2.Versoes, Has.Count.EqualTo(2));
            Assert.That(m.Resultados.Laudos()[0].Laudo.Id, Is.EqualTo(nova.Id));
        });
        Assert.That(Mesa.Erro(() => m.Resultados.Retificar(pedido.Id, " ")), Is.EqualTo("laudo.erro.motivo"));
    }

    [Test]
    public void Laudo_ErrosDeBusca()
    {
        using var m = new Mesa();

        Assert.That(() => m.Resultados.Liberar(9999), Throws.TypeOf<NaoEncontrado>());
        Assert.That(() => m.Resultados.Retificar(9999, "x"), Throws.TypeOf<NaoEncontrado>());
        Assert.That(() => m.Resultados.Ver(9999), Throws.TypeOf<NaoEncontrado>());
        Assert.That(() => m.Resultados.ConfirmarCritico(99999), Throws.TypeOf<NaoEncontrado>());
        Assert.That(Mesa.Erro(() => m.Resultados.Retificar(m.PedidoDoCenario.Id, "x")), Is.EqualTo("laudo.erro.incompleto"));
        Assert.That(Mesa.Erro(() => m.Resultados.Liberar(m.PedidoDoCenario.Id)), Is.EqualTo("laudo.erro.incompleto"));
    }

    [Test]
    public void Operacao_ResumeTemposFilasESoroteca()
    {
        using var m = new Mesa();
        var v = m.Operacao.Ver();

        Assert.Multiple(() =>
        {
            Assert.That(v.Tempos, Is.Not.Empty);
            Assert.That(v.Filas, Has.Count.EqualTo(3));
            Assert.That(v.Livres + v.Ocupadas + v.Vencendo + v.Vencidas, Is.EqualTo(81));
        });
    }

    [Test]
    public void RN_062_CadeiaDetectaAdulteracao()
    {
        using var m = new Mesa();
        m.Auditoria.Registrar(m.Sessao, "SessaoCriada");
        m.Auditoria.Registrar(m.Sessao, "PapelTrocado");
        m.Auditoria.Registrar(m.Sessao, "AutorizacaoRecusada");

        var boa = m.ConsultaAuditoria.Verificar();
        Assert.That(boa.QuebraEm, Is.Null);
        Assert.That(boa.Simulada, Is.False);

        var ruim = m.ConsultaAuditoria.SimularAdulteracao();
        Assert.That(ruim.QuebraEm, Is.Not.Null);
        Assert.That(ruim.Simulada, Is.True);
        Assert.That(m.ConsultaAuditoria.Verificar().QuebraEm, Is.Null, "o banco real não foi tocado");
    }

    [Test]
    public void RN_061_TudoAuditado_FiltroPorGrupo()
    {
        using var m = new Mesa();
        m.Auditoria.Registrar(m.Sessao, "SessaoCriada");
        m.Auditoria.Registrar(m.Sessao, "AutorizacaoRecusada");

        Assert.That(m.ConsultaAuditoria.Listar(null).Select(e => e.Seq), Is.EqualTo(new[] { 2, 1 }));
        Assert.That(m.ConsultaAuditoria.Listar("seguranca").Single().Acao, Is.EqualTo("AutorizacaoRecusada"));
        Assert.That(m.ConsultaAuditoria.Listar("inventado"), Has.Count.EqualTo(2));
    }
}
