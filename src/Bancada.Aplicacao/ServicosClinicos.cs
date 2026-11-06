using System;
using System.Collections.Generic;
using System.Linq;
using Bancada.Dominio;

namespace Bancada.Aplicacao;

/// <summary>Base dos casos de uso: sessão, hora simulada, operador atual e auditoria.</summary>
public abstract class CasoDeUso(ContextoDaSessao contexto, ServicoDeAuditoria auditoria)
{
    protected Sessao Sessao => contexto.Sessao;

    protected DateTime Agora => Sessao.Simulacao.Agora;

    protected string Operador => Sessao.Operador;

    protected string? Decifrar(string? valor, string dados) => valor is null ? null : Sessao.Cifra.Decifrar(valor, dados);

    protected string? Cifrar(string? texto, string dados) => string.IsNullOrEmpty(texto) ? null : Sessao.Cifra.Cifrar(texto, dados);

    protected void Auditar(string acao, string alvoTipo, string alvoId, string? paciente = null) =>
        auditoria.Registrar(Sessao, acao, alvoTipo, alvoId, paciente is null ? "{}" : $"{{\"paciente\":\"{paciente}\"}}");
}

public sealed record PaginaDePacientes(IReadOnlyList<Paciente> Itens, int Total, int Pagina, int Paginas);

public sealed record AlergiaLegivel(Alergia Alergia, string? Reacao);

public sealed record EvolucaoLegivel(Evolucao Evolucao, string Texto, bool FoiCorrigida);

public sealed record LinhaDoAgendamento(Agendamento Agendamento, HorarioDaGrade Horario, Paciente Paciente);

public sealed record FichaDoPaciente(Paciente Paciente, int Idade, IReadOnlyList<AlergiaLegivel> Alergias, IReadOnlyList<LinhaDoAgendamento> Agendamentos, IReadOnlyList<Pedido> Pedidos, IReadOnlyList<EventoDeAuditoria> LinhaDoTempo);

public sealed class ServicoDePacientes(IPacientes pacientes, IAgenda agenda, IPedidos pedidos, ContextoDaSessao contexto, ServicoDeAuditoria auditoria) : CasoDeUso(contexto, auditoria)
{
    public const int TamanhoDaPagina = 20;

    public PaginaDePacientes Buscar(string? busca, int pagina)
    {
        var texto = (busca ?? string.Empty).Trim();
        texto = texto.Length > 64 ? texto[..64] : texto;
        var (itens, total) = pacientes.Buscar(texto, Math.Max(pagina, 1), TamanhoDaPagina);
        return new PaginaDePacientes(itens, total, Math.Max(pagina, 1), Math.Max(1, (total + TamanhoDaPagina - 1) / TamanhoDaPagina));
    }

    public FichaDoPaciente Ficha(int id)
    {
        var p = pacientes.Obter(id) ?? throw new NaoEncontrado("paciente");
        var alergias = pacientes.Alergias(id).Select(a => new AlergiaLegivel(a, Decifrar(a.ReacaoCifrada, a.DadosAssociados))).ToList();
        var ags = agenda.DoPaciente(id).Select(x => new LinhaDoAgendamento(x.Agendamento, x.Horario, p)).ToList();
        var eventos = Sessao.Eventos.Where(e => e.DetalheJson.Contains(p.Codigo, StringComparison.Ordinal)).ToList();
        return new FichaDoPaciente(p, p.IdadeEm(Sessao.Simulacao.Hoje), alergias, ags, pedidos.DoPaciente(id), eventos);
    }
}

public sealed record CelulaDaAgenda(HorarioDaGrade Horario, Agendamento? Agendamento, Paciente? Paciente, bool Passado);

public sealed record DadosDoAgendamento(string? Motivo, int Idade);

public sealed record SemanaDaAgenda(string Profissional, int Deslocamento, DateOnly Segunda, IReadOnlyList<CelulaDaAgenda> Celulas);

public sealed class ServicoDeAgenda(IAgenda agenda, IPacientes pacientes, IUnidadeDeTrabalho unidade, ContextoDaSessao contexto, ServicoDeAuditoria auditoria) : CasoDeUso(contexto, auditoria)
{
    public const int SemanasVisiveis = 2;

    public SemanaDaAgenda Semana(string? profissional, int deslocamento)
    {
        RegistrarFaltas();
        var medico = Operadores.DoPapel(Papel.Medico).FirstOrDefault(o => o.Id == profissional)?.Id ?? Operadores.DoPapel(Papel.Medico)[0].Id;
        var semana = Math.Clamp(deslocamento, 0, SemanasVisiveis - 1);
        var hoje = Sessao.Simulacao.Hoje;
        var segunda = hoje.AddDays(-(((int)hoje.DayOfWeek + 6) % 7) + (7 * semana));
        var de = segunda.ToDateTime(TimeOnly.MinValue);
        var horarios = agenda.Horarios(medico, de, de.AddDays(5));
        var ativos = agenda.Ativos(horarios.Select(h => h.Id).ToList()).ToDictionary(a => a.HorarioId);
        var celulas = horarios.Select(h =>
        {
            ativos.TryGetValue(h.Id, out var a);
            return new CelulaDaAgenda(h, a, a is null ? null : pacientes.Obter(a.PacienteId), h.Inicio < Agora);
        }).ToList();
        return new SemanaDaAgenda(medico, semana, segunda, celulas);
    }

    /// <summary>O que o modal da agenda mostra além da célula: motivo decifrado e idade do paciente.</summary>
    public DadosDoAgendamento Dados(Agendamento a, Paciente p) =>
        new(Decifrar(a.MotivoCifrado, $"agendamento:{p.Id}"), p.IdadeEm(Sessao.Simulacao.Hoje));

    /// <summary>RN-001 (horário livre), RN-002 (não no passado) e RN-003 (horário da grade).</summary>
    public Agendamento Agendar(int horarioId, string? codigoPaciente, string? motivo)
    {
        var horario = agenda.ObterHorario(horarioId) ?? throw new ViolacaoDeRegra("agenda.erro.foradagrade");
        var paciente = pacientes.PorCodigo(codigoPaciente ?? string.Empty) ?? throw new ViolacaoDeRegra("agenda.erro.paciente");
        if (agenda.AtivoNoHorario(horarioId) is not null)
        {
            throw new ViolacaoDeRegra("agenda.erro.ocupado");
        }

        var texto = Texto.Exigir(motivo, 0, 200, "comum.erro.textolongo");
        Sessao.ContarRegistro();
        var novo = Agendamento.Criar(paciente.Id, horario, Cifrar(texto, $"agendamento:{paciente.Id}"), Agora);
        agenda.Adicionar(novo);
        unidade.Salvar();
        Auditar("AgendamentoCriado", "Agendamento", novo.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), paciente.Codigo);
        return novo;
    }

    public void Reagendar(int id, int novoHorarioId)
    {
        var a = Exigir(id);
        var novo = agenda.ObterHorario(novoHorarioId) ?? throw new ViolacaoDeRegra("agenda.erro.foradagrade");
        if (agenda.AtivoNoHorario(novoHorarioId) is not null)
        {
            throw new ViolacaoDeRegra("agenda.erro.ocupado");
        }

        a.Reagendar(novo, Agora);
        Concluir(a, "AgendamentoReagendado");
    }

    public void Confirmar(int id)
    {
        var a = Exigir(id);
        a.Confirmar();
        Concluir(a, "AgendamentoConfirmado");
    }

    /// <summary>RN-004: cancelar libera o horário.</summary>
    public void Cancelar(int id)
    {
        var a = Exigir(id);
        a.Cancelar();
        Concluir(a, "AgendamentoCancelado");
    }

    /// <summary>RN-005: horário que passou sem check-in vira falta.</summary>
    private void RegistrarFaltas()
    {
        var vencidos = agenda.Vencidos(Agora);
        foreach (var a in vencidos)
        {
            a.RegistrarFalta();
            Auditar("FaltaRegistrada", "Agendamento", a.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        unidade.Salvar();
    }

    private Agendamento Exigir(int id) => agenda.Obter(id) ?? throw new NaoEncontrado("agendamento");

    private void Concluir(Agendamento a, string evento)
    {
        unidade.Salvar();
        Auditar(evento, "Agendamento", a.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}

public sealed record DetalheDoAtendimento(LinhaDoAgendamento Linha, int Idade, IReadOnlyList<AlergiaLegivel> Alergias, IReadOnlyList<EvolucaoLegivel> Recentes, IReadOnlyList<EvolucaoLegivel> Deste, IReadOnlyList<Pedido> Pedidos);

public sealed record ResumoDoAtendimento(LinhaDoAgendamento Linha, int Idade, IReadOnlyList<Pedido> Pedidos, IReadOnlyList<EvolucaoLegivel> Evolucoes);

public sealed class ServicoDeAtendimento(IAgenda agenda, IPacientes pacientes, IProntuario prontuario, IPedidos pedidos, IUnidadeDeTrabalho unidade, ContextoDaSessao contexto, ServicoDeAuditoria auditoria) : CasoDeUso(contexto, auditoria)
{
    public IReadOnlyList<LinhaDoAgendamento> Fila(bool semanaToda)
    {
        var hoje = Sessao.Simulacao.Hoje.ToDateTime(TimeOnly.MinValue);
        var de = semanaToda ? hoje.AddDays(-7) : hoje;
        var ate = semanaToda ? hoje.AddDays(7) : hoje.AddDays(1);
        return agenda.Periodo(de, ate).Select(x => new LinhaDoAgendamento(x.Agendamento, x.Horario, pacientes.Obter(x.Agendamento.PacienteId)!)).ToList();
    }

    public void CheckIn(int id)
    {
        var (a, h, p) = Carregar(id);
        a.FazerCheckIn(h, Agora);
        Salvar("CheckInFeito", a, p);
    }

    public void Iniciar(int id)
    {
        var (a, h, p) = Carregar(id);
        a.Iniciar(h, Operador, Agora);
        Salvar("AtendimentoIniciado", a, p);
    }

    public void Concluir(int id)
    {
        var (a, h, p) = Carregar(id);
        a.Concluir(h, Operador, prontuario.TemEvolucao(id), Agora);
        Salvar("AtendimentoConcluido", a, p);
    }

    public DetalheDoAtendimento Detalhe(int id)
    {
        var (a, h, p) = Carregar(id);
        var legiveis = LeitorDoProntuario.Legiveis(prontuario.Evolucoes(p.Id), Sessao);
        return new DetalheDoAtendimento(
            new LinhaDoAgendamento(a, h, p),
            p.IdadeEm(Sessao.Simulacao.Hoje),
            pacientes.Alergias(p.Id).Where(x => x.Ativa).Select(x => new AlergiaLegivel(x, Decifrar(x.ReacaoCifrada, x.DadosAssociados))).ToList(),
            legiveis.Where(e => e.Evolucao.AgendamentoId != id).Take(3).ToList(),
            legiveis.Where(e => e.Evolucao.AgendamentoId == id).ToList(),
            pedidos.DoPaciente(p.Id).Where(x => x.AgendamentoId == id).ToList());
    }

    /// <summary>Resumo do atendimento para a fila. A evolução é dado clínico: só sai quando quem pergunta pode ver evoluções.</summary>
    public ResumoDoAtendimento Resumo(int id, bool comEvolucoes)
    {
        var (a, h, p) = Carregar(id);
        var evolucoes = comEvolucoes ? LeitorDoProntuario.Legiveis(prontuario.Evolucoes(p.Id), Sessao).Where(e => e.Evolucao.AgendamentoId == id).ToList() : [];
        return new ResumoDoAtendimento(new LinhaDoAgendamento(a, h, p), p.IdadeEm(Sessao.Simulacao.Hoje), pedidos.DoPaciente(p.Id).Where(x => x.AgendamentoId == id).ToList(), evolucoes);
    }

    private (Agendamento, HorarioDaGrade, Paciente) Carregar(int id)
    {
        var a = agenda.Obter(id) ?? throw new NaoEncontrado("agendamento");
        return (a, agenda.ObterHorario(a.HorarioId)!, pacientes.Obter(a.PacienteId)!);
    }

    private void Salvar(string evento, Agendamento a, Paciente p)
    {
        unidade.Salvar();
        Auditar(evento, "Agendamento", a.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), p.Codigo);
    }
}

public static class LeitorDoProntuario
{
    /// <summary>Evoluções da mais nova para a mais antiga, decifradas, marcando as que já têm correção.</summary>
    public static IReadOnlyList<EvolucaoLegivel> Legiveis(IReadOnlyList<Evolucao> evolucoes, Sessao sessao)
    {
        var corrigidas = evolucoes.Where(e => e.CorrigeId is not null).Select(e => e.CorrigeId!.Value).ToHashSet();
        return evolucoes.OrderByDescending(e => e.CriadaEm).ThenByDescending(e => e.Id)
            .Select(e => new EvolucaoLegivel(e, sessao.Cifra.Decifrar(e.TextoCifrado, e.DadosAssociados), corrigidas.Contains(e.Id)))
            .ToList();
    }
}

public sealed record VisaoDoProntuario(Paciente Paciente, IReadOnlyList<AlergiaLegivel> Alergias, IReadOnlyList<EvolucaoLegivel> Evolucoes, IReadOnlyList<Pedido> Pedidos);

public sealed class ServicoDeProntuario(IPacientes pacientes, IProntuario prontuario, IAgenda agenda, IPedidos pedidos, IUnidadeDeTrabalho unidade, ContextoDaSessao contexto, ServicoDeAuditoria auditoria) : CasoDeUso(contexto, auditoria)
{
    public VisaoDoProntuario Ver(int pacienteId, bool comEvolucoes)
    {
        var p = pacientes.Obter(pacienteId) ?? throw new NaoEncontrado("paciente");
        var alergias = pacientes.Alergias(pacienteId).Select(a => new AlergiaLegivel(a, Decifrar(a.ReacaoCifrada, a.DadosAssociados))).ToList();
        // Só decifra o que o papel pode ler (a Coleta vê alergias, não evoluções).
        return comEvolucoes
            ? new VisaoDoProntuario(p, alergias, LeitorDoProntuario.Legiveis(prontuario.Evolucoes(pacienteId), Sessao), pedidos.DoPaciente(pacienteId))
            : new VisaoDoProntuario(p, alergias, [], []);
    }

    /// <summary>RN-007 e RN-012: só o médico do horário escreve, durante o atendimento, de 1 a 2.000 caracteres.</summary>
    public void Registrar(int agendamentoId, string? texto)
    {
        var a = agenda.Obter(agendamentoId) ?? throw new NaoEncontrado("agendamento");
        Agendamento.ExigirProfissional(agenda.ObterHorario(a.HorarioId)!, Operador);
        if (a.Estado != EstadoAgendamento.EmAtendimento)
        {
            throw new ViolacaoDeRegra("regra.transicao");
        }

        Gravar(a.PacienteId, agendamentoId, texto, null, "EvolucaoRegistrada");
    }

    /// <summary>RN-011: a correção é uma evolução nova que aponta a original.</summary>
    public void Corrigir(int evolucaoId, string? texto)
    {
        var original = prontuario.Obter(evolucaoId) ?? throw new NaoEncontrado("evolucao");
        Gravar(original.PacienteId, original.AgendamentoId, texto, original.Id, "EvolucaoCorrigida");
    }

    public void AdicionarAlergia(int pacienteId, string? substancia, string? reacao)
    {
        var p = pacientes.Obter(pacienteId) ?? throw new NaoEncontrado("paciente");
        var nome = Texto.Exigir(substancia, 1, 60, "prontuario.erro.substancia");
        var detalhe = Texto.Exigir(reacao, 0, 120, "comum.erro.textolongo");
        Sessao.ContarRegistro();
        var dados = new Alergia(pacienteId, nome, null, Agora).DadosAssociados;
        pacientes.Adicionar(new Alergia(pacienteId, nome, Cifrar(detalhe, dados), Agora));
        unidade.Salvar();
        Auditar("AlergiaAlterada", "Paciente", p.Codigo, p.Codigo);
    }

    public void AlternarAlergia(int alergiaId)
    {
        var a = pacientes.ObterAlergia(alergiaId) ?? throw new NaoEncontrado("alergia");
        a.Alternar();
        unidade.Salvar();
        Auditar("AlergiaAlterada", "Alergia", a.Substancia);
    }

    private void Gravar(int pacienteId, int? agendamentoId, string? texto, int? corrigeId, string evento)
    {
        var limpo = Texto.Exigir(texto, 1, Evolucao.TamanhoMaximo, "prontuario.erro.texto");
        Sessao.ContarRegistro();
        var quando = Agora;
        prontuario.Adicionar(new Evolucao(pacienteId, agendamentoId, Operador, Sessao.Cifra.Cifrar(limpo, Evolucao.DadosDe(pacienteId, quando)), corrigeId, quando));
        unidade.Salvar();
        Auditar(evento, "Evolucao", pacientes.Obter(pacienteId)!.Codigo, pacientes.Obter(pacienteId)!.Codigo);
    }
}
