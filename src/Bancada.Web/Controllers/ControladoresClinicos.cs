using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Bancada.Web.Controllers;

/// <summary>Regra violada volta como faixa na página seguinte (Post/Redirect/Get), sem exceção escapar para o visitante.</summary>
public abstract class ControladorDaBancada : Controller
{
    protected Sessao Sessao => HttpContext.Sessao();

    protected IActionResult Executar(Action acao, string chaveOk, string destino)
    {
        try
        {
            acao();
            Sessao.Aviso = (chaveOk, true);
        }
        catch (ViolacaoDeRegra e)
        {
            Sessao.Aviso = (e.Chave, false);
            if (e.Chave == "sessao.erro.cota")
            {
                HttpContext.RequestServices.GetRequiredService<ServicoDeAuditoria>().Registrar(Sessao, "CotaExcedida", "Sessao", "-", "{}", "Bloqueado");
            }
        }

        return LocalRedirect(destino);
    }

    protected static string Num(int id) => id.ToString(CultureInfo.InvariantCulture);
}

public sealed record AgendaVm(SemanaDaAgenda Semana, CelulaDaAgenda? Selecionada, IReadOnlyList<Paciente> Pacientes, DadosDoAgendamento? Dados);

public sealed class AgendaController(ServicoDeAgenda agenda, IPacientes pacientes) : ControladorDaBancada
{
    [HttpGet("/agenda"), Autorizar(Acao.VerAgenda)]
    public IActionResult Index(string? profissional, int semana, int? horario)
    {
        var s = agenda.Semana(profissional, semana);
        var selecionada = s.Celulas.FirstOrDefault(c => c.Horario.Id == horario);
        var dados = selecionada is { Agendamento: { } a } ? agenda.Dados(a, selecionada.Paciente!) : null;
        return View(new AgendaVm(s, selecionada, pacientes.Todos(), dados));
    }

    [HttpPost("/agenda/agendar"), Autorizar(Acao.Agendar)]
    public IActionResult Agendar([FromForm] int horario, [FromForm] string? paciente, [FromForm] string? motivo, [FromForm] string? profissional, [FromForm] int semana) =>
        Executar(() => agenda.Agendar(horario, paciente, motivo), "agenda.ok.agendado", Volta(profissional, semana));

    [HttpPost("/agenda/{id:int}/reagendar"), Autorizar(Acao.Reagendar)]
    public IActionResult Reagendar(int id, [FromForm] int horario, [FromForm] string? profissional, [FromForm] int semana) =>
        Executar(() => agenda.Reagendar(id, horario), "agenda.ok.reagendado", Volta(profissional, semana));

    [HttpPost("/agenda/{id:int}/cancelar"), Autorizar(Acao.CancelarAgendamento)]
    public IActionResult Cancelar(int id, [FromForm] string? profissional, [FromForm] int semana) =>
        Executar(() => agenda.Cancelar(id), "agenda.ok.cancelado", Volta(profissional, semana));

    [HttpPost("/agenda/{id:int}/confirmar"), Autorizar(Acao.ConfirmarAgendamento)]
    public IActionResult Confirmar(int id, [FromForm] string? profissional, [FromForm] int semana) =>
        Executar(() => agenda.Confirmar(id), "agenda.ok.confirmado", Volta(profissional, semana));

    private static string Volta(string? profissional, int semana) =>
        Operadores.Pertence(profissional ?? string.Empty, Papel.Medico) ? $"/agenda?profissional={profissional}&semana={Num(semana)}" : "/agenda";
}

public sealed class PacientesController(ServicoDePacientes pacientes) : ControladorDaBancada
{
    [HttpGet("/pacientes"), Autorizar(Acao.ListarPacientes)]
    public IActionResult Index(string? busca, int pagina = 1)
    {
        ViewBag.Busca = busca;
        return View(pacientes.Buscar(busca, pagina));
    }

    [HttpGet("/pacientes/{id:int}"), Autorizar(Acao.VerPaciente)]
    public IActionResult Ficha(int id) => View(pacientes.Ficha(id));
}

public sealed class AtendimentosController(ServicoDeAtendimento atendimento, ServicoDeProntuario prontuario) : ControladorDaBancada
{
    [HttpGet("/atendimentos"), Autorizar(Acao.VerFila)]
    public IActionResult Index(bool semana, int? abrir)
    {
        ViewBag.Semana = semana;
        ViewBag.Resumo = abrir is int id ? atendimento.Resumo(id, MatrizDeAutorizacao.Permite(Sessao.Papel, Acao.VerEvolucoes)) : null;
        return View(atendimento.Fila(semana));
    }

    [HttpGet("/atendimentos/{id:int}"), Autorizar(Acao.VerAtendimento)]
    public IActionResult Detalhe(int id) => View(atendimento.Detalhe(id));

    [HttpPost("/atendimentos/{id:int}/checkin"), Autorizar(Acao.FazerCheckIn)]
    public IActionResult CheckIn(int id) => Executar(() => atendimento.CheckIn(id), "atendimento.ok.checkin", "/atendimentos");

    [HttpPost("/atendimentos/{id:int}/iniciar"), Autorizar(Acao.IniciarAtendimento)]
    public IActionResult Iniciar(int id) => Executar(() => atendimento.Iniciar(id), "atendimento.ok.iniciado", "/atendimentos/" + Num(id));

    [HttpPost("/atendimentos/{id:int}/evolucao"), Autorizar(Acao.RegistrarEvolucao)]
    public IActionResult Evolucao(int id, [FromForm] string? texto) => Executar(() => prontuario.Registrar(id, texto), "prontuario.ok.evolucao", "/atendimentos/" + Num(id));

    [HttpPost("/atendimentos/{id:int}/concluir"), Autorizar(Acao.ConcluirAtendimento)]
    public IActionResult Concluir(int id) => Executar(() => atendimento.Concluir(id), "atendimento.ok.concluido", "/atendimentos");
}

public sealed record ProntuarioVm(VisaoDoProntuario Visao, IReadOnlyList<Paciente> Pacientes, int? Corrigindo);

public sealed class ProntuarioController(ServicoDeProntuario prontuario, IPacientes pacientes) : ControladorDaBancada
{
    private const int PacienteDoRoteiro = 14;

    [HttpGet("/prontuario/{pacienteId:int?}"), Autorizar(Acao.EditarAlergias)]
    public IActionResult Index(int pacienteId = PacienteDoRoteiro, int? corrigir = null) =>
        View(new ProntuarioVm(prontuario.Ver(pacienteId, MatrizDeAutorizacao.Permite(Sessao.Papel, Acao.VerEvolucoes)), pacientes.Todos(), corrigir));

    [HttpPost("/prontuario/evolucao/{id:int}/corrigir"), Autorizar(Acao.CorrigirEvolucao)]
    public IActionResult Corrigir(int id, [FromForm] string? texto, [FromForm] int paciente) =>
        Executar(() => prontuario.Corrigir(id, texto), "prontuario.ok.correcao", "/prontuario/" + Num(paciente));

    [HttpPost("/prontuario/{pacienteId:int}/alergia"), Autorizar(Acao.EditarAlergias)]
    public IActionResult Alergia(int pacienteId, [FromForm] string? substancia, [FromForm] string? reacao) =>
        Executar(() => prontuario.AdicionarAlergia(pacienteId, substancia, reacao), "prontuario.ok.alergia", "/prontuario/" + Num(pacienteId));

    [HttpPost("/prontuario/alergia/{id:int}/alternar"), Autorizar(Acao.EditarAlergias)]
    public IActionResult Alternar(int id, [FromForm] int paciente) =>
        Executar(() => prontuario.AlternarAlergia(id), "prontuario.ok.alergia", "/prontuario/" + Num(paciente));
}

public sealed record NovoPedidoVm(IReadOnlyList<Paciente> Pacientes, Paciente Paciente, IReadOnlyList<Alergia> Alergias, int? Agendamento);

public sealed class PedidosController(ServicoDePedidos pedidos, IPacientes pacientes) : ControladorDaBancada
{
    private const string PacientePadrao = "PAC-0014";

    [HttpGet("/pedidos"), Autorizar(Acao.VerPedido)]
    public IActionResult Index() => View(pedidos.Listar());

    [HttpGet("/pedidos/novo"), Autorizar(Acao.CriarPedido)]
    public IActionResult Novo(string? paciente, int? atendimento)
    {
        var p = pacientes.PorCodigo(paciente ?? PacientePadrao) ?? pacientes.PorCodigo(PacientePadrao)!;
        return View(new NovoPedidoVm(pacientes.Todos(), p, pedidos.AlergiasAtivas(p.Codigo), atendimento));
    }

    [HttpPost("/pedidos/novo"), Autorizar(Acao.CriarPedido)]
    public IActionResult Criar([FromForm] string? paciente, [FromForm] int? atendimento, [FromForm] string[] exames, [FromForm] bool urgente)
    {
        var destino = "/pedidos";
        return Executar(() => destino += "/" + Num(pedidos.Criar(paciente, atendimento, exames, urgente).Id), "pedido.ok.criado", destino).Redirecionar(() => destino);
    }

    [HttpGet("/pedidos/{id:int}"), Autorizar(Acao.VerPedido)]
    public IActionResult Detalhe(int id) => View(pedidos.Detalhe(id));

    [HttpPost("/pedidos/{id:int}/cancelar"), Autorizar(Acao.CancelarPedido)]
    public IActionResult Cancelar(int id) => Executar(() => pedidos.Cancelar(id), "pedido.ok.cancelado", "/pedidos");
}

internal static class ResultadoDeAcao
{
    /// <summary>Quando o destino só se conhece depois da ação (ex.: id do registro criado).</summary>
    public static IActionResult Redirecionar(this IActionResult _, Func<string> destino) => new LocalRedirectResult(destino());
}
