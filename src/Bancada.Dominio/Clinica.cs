using System;

namespace Bancada.Dominio;

public sealed class Paciente(string codigo, string nome, DateOnly nascimento, string sexo)
{
    public int Id { get; private set; }

    public string Codigo { get; private set; } = codigo;

    public string Nome { get; private set; } = nome;

    public DateOnly Nascimento { get; private set; } = nascimento;

    public string Sexo { get; private set; } = sexo;

    public int IdadeEm(DateOnly hoje)
    {
        var idade = hoje.Year - Nascimento.Year;
        return Nascimento.AddYears(idade) > hoje ? idade - 1 : idade;
    }
}

public sealed class Alergia(int pacienteId, string substancia, string? reacaoCifrada, DateTime registradaEm)
{
    public int Id { get; private set; }

    public int PacienteId { get; private set; } = pacienteId;

    public string Substancia { get; private set; } = substancia;

    public string? ReacaoCifrada { get; private set; } = reacaoCifrada;

    public DateTime RegistradaEm { get; private set; } = registradaEm;

    public bool Ativa { get; private set; } = true;

    public void Alternar() => Ativa = !Ativa;

    public string DadosAssociados => FormattableString.Invariant($"alergia:{PacienteId}:{RegistradaEm.Ticks}");
}

public sealed class HorarioDaGrade(string profissionalId, DateTime inicio)
{
    public int Id { get; private set; }

    public string ProfissionalId { get; private set; } = profissionalId;

    public DateTime Inicio { get; private set; } = inicio;
}

public enum EstadoAgendamento
{
    Agendado,
    Confirmado,
    ChegouCheckIn,
    EmAtendimento,
    Concluido,
    Cancelado,
    Faltou,
}

public enum EventoAgendamento
{
    Confirmar,
    FazerCheckIn,
    Iniciar,
    Concluir,
    Cancelar,
    RegistrarFalta,
    Reagendar,
}

/// <summary>Reserva de um horário e, depois do check-in, o próprio atendimento (vault: Fluxo-do-Laboratorio).</summary>
public sealed class Agendamento(int pacienteId, int horarioId, string? motivoCifrado, DateTime criadoEm)
{
    public static readonly MaquinaDeEstados<EstadoAgendamento, EventoAgendamento> Maquina = new(
        (EstadoAgendamento.Agendado, EventoAgendamento.Confirmar, EstadoAgendamento.Confirmado),
        (EstadoAgendamento.Agendado, EventoAgendamento.FazerCheckIn, EstadoAgendamento.ChegouCheckIn),
        (EstadoAgendamento.Confirmado, EventoAgendamento.FazerCheckIn, EstadoAgendamento.ChegouCheckIn),
        (EstadoAgendamento.ChegouCheckIn, EventoAgendamento.Iniciar, EstadoAgendamento.EmAtendimento),
        (EstadoAgendamento.EmAtendimento, EventoAgendamento.Concluir, EstadoAgendamento.Concluido),
        (EstadoAgendamento.Agendado, EventoAgendamento.Cancelar, EstadoAgendamento.Cancelado),
        (EstadoAgendamento.Confirmado, EventoAgendamento.Cancelar, EstadoAgendamento.Cancelado),
        (EstadoAgendamento.ChegouCheckIn, EventoAgendamento.Cancelar, EstadoAgendamento.Cancelado),
        (EstadoAgendamento.Agendado, EventoAgendamento.RegistrarFalta, EstadoAgendamento.Faltou),
        (EstadoAgendamento.Confirmado, EventoAgendamento.RegistrarFalta, EstadoAgendamento.Faltou),
        (EstadoAgendamento.Agendado, EventoAgendamento.Reagendar, EstadoAgendamento.Agendado),
        (EstadoAgendamento.Confirmado, EventoAgendamento.Reagendar, EstadoAgendamento.Agendado));

    public int Id { get; private set; }

    public int PacienteId { get; private set; } = pacienteId;

    public int HorarioId { get; private set; } = horarioId;

    public string? MotivoCifrado { get; private set; } = motivoCifrado;

    public DateTime CriadoEm { get; private set; } = criadoEm;

    public EstadoAgendamento Estado { get; private set; }

    public DateTime? ChegadaEm { get; private set; }

    public DateTime? IniciadoEm { get; private set; }

    public DateTime? ConcluidoEm { get; private set; }

    /// <summary>RN-002: não agendar no passado.</summary>
    public static Agendamento Criar(int pacienteId, HorarioDaGrade horario, string? motivoCifrado, DateTime agora) =>
        horario.Inicio < agora
            ? throw new ViolacaoDeRegra("agenda.erro.passado")
            : new Agendamento(pacienteId, horario.Id, motivoCifrado, agora);

    public void Confirmar() => Estado = Maquina.Proximo(Estado, EventoAgendamento.Confirmar);

    public void Cancelar() => Estado = Maquina.Proximo(Estado, EventoAgendamento.Cancelar);

    public void RegistrarFalta() => Estado = Maquina.Proximo(Estado, EventoAgendamento.RegistrarFalta);

    public void Reagendar(HorarioDaGrade novo, DateTime agora)
    {
        Estado = Maquina.Proximo(Estado, EventoAgendamento.Reagendar);
        HorarioId = novo.Inicio < agora ? throw new ViolacaoDeRegra("agenda.erro.passado") : novo.Id;
    }

    /// <summary>RN-006: check-in só no dia do agendamento.</summary>
    public void FazerCheckIn(HorarioDaGrade horario, DateTime agora)
    {
        if (horario.Inicio.Date != agora.Date)
        {
            throw new ViolacaoDeRegra("atendimento.erro.foradodia");
        }

        Estado = Maquina.Proximo(Estado, EventoAgendamento.FazerCheckIn);
        ChegadaEm = agora;
    }

    /// <summary>RN-007: só o profissional do horário atende.</summary>
    public void Iniciar(HorarioDaGrade horario, string operador, DateTime agora)
    {
        ExigirProfissional(horario, operador);
        Estado = Maquina.Proximo(Estado, EventoAgendamento.Iniciar);
        IniciadoEm = agora;
    }

    public void Concluir(HorarioDaGrade horario, string operador, bool temEvolucao, DateTime agora)
    {
        ExigirProfissional(horario, operador);
        if (!temEvolucao)
        {
            throw new ViolacaoDeRegra("atendimento.erro.semevolucao");
        }

        Estado = Maquina.Proximo(Estado, EventoAgendamento.Concluir);
        ConcluidoEm = agora;
    }

    public static void ExigirProfissional(HorarioDaGrade horario, string operador)
    {
        if (horario.ProfissionalId != operador)
        {
            throw new ViolacaoDeRegra("atendimento.erro.outroprofissional");
        }
    }
}

/// <summary>Registro imutável do prontuário: correção é uma evolução nova que aponta a anterior (RN-010, RN-011).</summary>
public sealed class Evolucao(int pacienteId, int? agendamentoId, string autorId, string textoCifrado, int? corrigeId, DateTime criadaEm)
{
    public const int TamanhoMaximo = 2000;

    public int Id { get; private set; }

    public int PacienteId { get; private set; } = pacienteId;

    public int? AgendamentoId { get; private set; } = agendamentoId;

    public string AutorId { get; private set; } = autorId;

    public string TextoCifrado { get; private set; } = textoCifrado;

    public int? CorrigeId { get; private set; } = corrigeId;

    public DateTime CriadaEm { get; private set; } = criadaEm;

    public string DadosAssociados => DadosDe(PacienteId, CriadaEm);

    public static string DadosDe(int pacienteId, DateTime criadaEm) => FormattableString.Invariant($"evolucao:{pacienteId}:{criadaEm.Ticks}");
}
