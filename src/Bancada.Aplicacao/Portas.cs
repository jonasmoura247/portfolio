using System;
using System.Collections.Generic;
using Bancada.Dominio;

namespace Bancada.Aplicacao;

/// <summary>Sessão do visitante atual, disponível para os casos de uso da requisição.</summary>
public sealed class ContextoDaSessao
{
    public Sessao Sessao { get; set; } = null!;
}

public interface IUnidadeDeTrabalho
{
    void Salvar();
}

public interface IPacientes
{
    Paciente? Obter(int id);

    Paciente? PorCodigo(string codigo);

    IReadOnlyList<Paciente> Todos();

    (IReadOnlyList<Paciente> Itens, int Total) Buscar(string busca, int pagina, int tamanho);

    IReadOnlyList<Alergia> Alergias(int pacienteId);

    Alergia? ObterAlergia(int id);

    void Adicionar(Alergia alergia);
}

public interface IAgenda
{
    HorarioDaGrade? ObterHorario(int id);

    IReadOnlyList<HorarioDaGrade> Horarios(string profissional, DateTime de, DateTime ate);

    Agendamento? Obter(int id);

    Agendamento? AtivoNoHorario(int horarioId);

    IReadOnlyList<Agendamento> Ativos(IReadOnlyCollection<int> horarios);

    IReadOnlyList<Agendamento> Vencidos(DateTime agora);

    IReadOnlyList<(Agendamento Agendamento, HorarioDaGrade Horario)> Periodo(DateTime de, DateTime ate);

    IReadOnlyList<(Agendamento Agendamento, HorarioDaGrade Horario)> DoPaciente(int pacienteId);

    void Adicionar(Agendamento agendamento);
}

public interface IProntuario
{
    IReadOnlyList<Evolucao> Evolucoes(int pacienteId);

    Evolucao? Obter(int id);

    bool TemEvolucao(int agendamentoId);

    void Adicionar(Evolucao evolucao);
}

public interface IPedidos
{
    Pedido? Obter(int id);

    IReadOnlyList<Pedido> Listar();

    IReadOnlyList<Pedido> DoPaciente(int pacienteId);

    int ProximoNumero();

    void Adicionar(Pedido pedido);
}

public interface IAmostras
{
    Amostra? Obter(int id);

    IReadOnlyList<Amostra> Abertas();

    IReadOnlyList<Amostra> DoPedido(int pedidoId);

    IReadOnlyList<Amostra> Porcoes(IReadOnlyCollection<int> ids);

    int ProximoNumero();

    void Adicionar(Amostra amostra);

    IReadOnlyList<CustodiaDaAmostra> Custodia(int amostraId);

    void Adicionar(CustodiaDaAmostra custodia);

    IReadOnlyList<PosicaoDaSoroteca> Posicoes(string caixa);

    IReadOnlyList<string> Caixas();

    PosicaoDaSoroteca? ObterPosicao(int id);

    PosicaoDaSoroteca? PosicaoDa(int amostraId);
}

public interface IEquipamentos
{
    Execucao? Obter(int id);

    IReadOnlyList<Execucao> Abertas();

    IReadOnlyList<Execucao> DoPedido(int pedidoId);

    void Adicionar(Execucao execucao);

    IReadOnlyList<MensagemDeProtocolo> Mensagens(int quantidade);

    void Adicionar(MensagemDeProtocolo mensagem);
}

public interface IResultados
{
    Resultado? Obter(int id);

    IReadOnlyList<Resultado> DoPedido(int pedidoId);

    IReadOnlyList<int> PedidosComPendencia();

    double? UltimoValidado(int pacienteId, string analito);

    void Adicionar(Resultado resultado);

    IReadOnlyList<Laudo> Laudos();

    Laudo? ObterLaudo(int id);

    Laudo? UltimoLaudo(int pedidoId);

    void Adicionar(Laudo laudo);
}

public interface IQualidade
{
    IReadOnlyList<ControleDeQualidade> Serie(string equipamento, string analito);

    void Adicionar(ControleDeQualidade controle);
}

public interface IRegistroDeAuditoria
{
    IReadOnlyList<EventoDeAuditoria> Eventos();
}
