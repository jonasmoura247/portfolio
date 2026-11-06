using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Infraestrutura.Dados;

/// <summary>Uma classe só: os repositórios são consultas curtas sobre o mesmo contexto. As portas separam por módulo quem usa o quê.</summary>
public sealed class Repositorios(BancadaDbContext db) : IUnidadeDeTrabalho, IPacientes, IAgenda, IProntuario, IPedidos, IAmostras, IEquipamentos, IResultados, IQualidade, IRegistroDeAuditoria, IEsquemaDoBanco
{
    public void Salvar() => db.SaveChanges();

    Paciente? IPacientes.Obter(int id) => db.Find<Paciente>(id);

    public Paciente? PorCodigo(string codigo) => db.Set<Paciente>().FirstOrDefault(p => p.Codigo == codigo);

    public IReadOnlyList<Paciente> Todos() => db.Set<Paciente>().OrderBy(p => p.Codigo).ToList();

    public (IReadOnlyList<Paciente> Itens, int Total) Buscar(string busca, int pagina, int tamanho)
    {
        // Parâmetro do EF, nunca concatenação; os curingas do LIKE ficam escapados.
        var padrao = "%" + busca.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal) + "%";
        var consulta = db.Set<Paciente>().Where(p => EF.Functions.Like(p.Nome, padrao, "\\") || EF.Functions.Like(p.Codigo, padrao, "\\"));
        return (consulta.OrderBy(p => p.Codigo).Skip((pagina - 1) * tamanho).Take(tamanho).ToList(), consulta.Count());
    }

    public IReadOnlyList<Alergia> Alergias(int pacienteId) => db.Set<Alergia>().Where(a => a.PacienteId == pacienteId).OrderBy(a => a.Id).ToList();

    public Alergia? ObterAlergia(int id) => db.Find<Alergia>(id);

    public void Adicionar(Alergia alergia) => db.Add(alergia);

    public HorarioDaGrade? ObterHorario(int id) => db.Find<HorarioDaGrade>(id);

    public IReadOnlyList<HorarioDaGrade> Horarios(string profissional, DateTime de, DateTime ate) =>
        db.Set<HorarioDaGrade>().Where(h => h.ProfissionalId == profissional && h.Inicio >= de && h.Inicio < ate).OrderBy(h => h.Inicio).ToList();

    Agendamento? IAgenda.Obter(int id) => db.Find<Agendamento>(id);

    public Agendamento? AtivoNoHorario(int horarioId) => db.Set<Agendamento>().FirstOrDefault(a => a.HorarioId == horarioId && a.Estado != EstadoAgendamento.Cancelado);

    public IReadOnlyList<Agendamento> Ativos(IReadOnlyCollection<int> horarios) =>
        db.Set<Agendamento>().Where(a => horarios.Contains(a.HorarioId) && a.Estado != EstadoAgendamento.Cancelado).ToList();

    public IReadOnlyList<Agendamento> Vencidos(DateTime agora) =>
        db.Set<Agendamento>()
            .Where(a => a.Estado == EstadoAgendamento.Agendado || a.Estado == EstadoAgendamento.Confirmado)
            .Join(db.Set<HorarioDaGrade>().Where(h => h.Inicio < agora), a => a.HorarioId, h => h.Id, (a, _) => a)
            .ToList();

    public IReadOnlyList<(Agendamento Agendamento, HorarioDaGrade Horario)> Periodo(DateTime de, DateTime ate) =>
        Linhas(db.Set<Agendamento>().Where(a => a.Estado != EstadoAgendamento.Cancelado), h => h.Inicio >= de && h.Inicio < ate);

    public IReadOnlyList<(Agendamento Agendamento, HorarioDaGrade Horario)> DoPaciente(int pacienteId) =>
        Linhas(db.Set<Agendamento>().Where(a => a.PacienteId == pacienteId), _ => true);

    public void Adicionar(Agendamento agendamento) => db.Add(agendamento);

    public IReadOnlyList<Evolucao> Evolucoes(int pacienteId) => db.Set<Evolucao>().Where(e => e.PacienteId == pacienteId).ToList();

    Evolucao? IProntuario.Obter(int id) => db.Find<Evolucao>(id);

    public bool TemEvolucao(int agendamentoId) => db.Set<Evolucao>().Any(e => e.AgendamentoId == agendamentoId);

    public void Adicionar(Evolucao evolucao) => db.Add(evolucao);

    Pedido? IPedidos.Obter(int id) => db.Set<Pedido>().FirstOrDefault(p => p.Id == id);

    public IReadOnlyList<Pedido> Listar() => db.Set<Pedido>().OrderByDescending(p => p.Id).ToList();

    IReadOnlyList<Pedido> IPedidos.DoPaciente(int pacienteId) => db.Set<Pedido>().Where(p => p.PacienteId == pacienteId).OrderByDescending(p => p.Id).ToList();

    int IPedidos.ProximoNumero() => db.Set<Pedido>().Count() + 1;

    public void Adicionar(Pedido pedido) => db.Add(pedido);

    Amostra? IAmostras.Obter(int id) => db.Find<Amostra>(id);

    public IReadOnlyList<Amostra> Abertas() =>
        db.Set<Amostra>().Where(a => a.Estado == EstadoAmostra.Coletada || a.Estado == EstadoAmostra.Armazenada || a.Estado == EstadoAmostra.EmAnalise).OrderBy(a => a.Id).ToList();

    IReadOnlyList<Amostra> IAmostras.DoPedido(int pedidoId) => db.Set<Amostra>().Where(a => a.PedidoId == pedidoId).ToList();

    public IReadOnlyList<Amostra> Porcoes(IReadOnlyCollection<int> ids) => db.Set<Amostra>().Where(a => ids.Contains(a.Id)).ToList();

    int IAmostras.ProximoNumero() => db.Set<Amostra>().Count() + 1;

    public void Adicionar(Amostra amostra) => db.Add(amostra);

    public IReadOnlyList<CustodiaDaAmostra> Custodia(int amostraId) => db.Set<CustodiaDaAmostra>().Where(c => c.AmostraId == amostraId).OrderBy(c => c.Id).ToList();

    public void Adicionar(CustodiaDaAmostra custodia) => db.Add(custodia);

    public IReadOnlyList<PosicaoDaSoroteca> Posicoes(string caixa) => db.Set<PosicaoDaSoroteca>().Where(p => p.Caixa == caixa).OrderBy(p => p.Linha).ThenBy(p => p.Coluna).ToList();

    public IReadOnlyList<string> Caixas() => db.Set<PosicaoDaSoroteca>().Select(p => p.Caixa).Distinct().OrderBy(c => c).ToList();

    public PosicaoDaSoroteca? ObterPosicao(int id) => db.Find<PosicaoDaSoroteca>(id);

    public PosicaoDaSoroteca? PosicaoDa(int amostraId) => db.Set<PosicaoDaSoroteca>().FirstOrDefault(p => p.AmostraId == amostraId);

    Execucao? IEquipamentos.Obter(int id) => db.Find<Execucao>(id);

    IReadOnlyList<Execucao> IEquipamentos.Abertas() =>
        db.Set<Execucao>().Where(e => e.Estado == EstadoExecucao.NaFila || e.Estado == EstadoExecucao.Processando || e.Estado == EstadoExecucao.Erro).OrderBy(e => e.Id).ToList();

    IReadOnlyList<Execucao> IEquipamentos.DoPedido(int pedidoId) => db.Set<Execucao>().Where(e => e.PedidoId == pedidoId).ToList();

    public void Adicionar(Execucao execucao) => db.Add(execucao);

    public IReadOnlyList<MensagemDeProtocolo> Mensagens(int quantidade) => db.Set<MensagemDeProtocolo>().OrderByDescending(m => m.Id).Take(quantidade).ToList();

    public void Adicionar(MensagemDeProtocolo mensagem) => db.Add(mensagem);

    Resultado? IResultados.Obter(int id) => db.Find<Resultado>(id);

    IReadOnlyList<Resultado> IResultados.DoPedido(int pedidoId) => db.Set<Resultado>().Where(r => r.PedidoId == pedidoId).OrderBy(r => r.Id).ToList();

    public IReadOnlyList<int> PedidosComPendencia() => db.Set<Pedido>().Where(p => p.Estado == EstadoPedido.ResultadoPreliminar).OrderBy(p => p.Id).Select(p => p.Id).ToList();

    public double? UltimoValidado(int pacienteId, string analito) =>
        db.Set<Resultado>().Where(r => r.PacienteId == pacienteId && r.Analito == analito && r.Estado == EstadoResultado.Validado)
            .OrderByDescending(r => r.Id).Select(r => (double?)r.Valor).FirstOrDefault();

    public void Adicionar(Resultado resultado) => db.Add(resultado);

    public IReadOnlyList<Laudo> Laudos() => db.Set<Laudo>().OrderByDescending(l => l.Id).ToList();

    public Laudo? ObterLaudo(int id) => db.Find<Laudo>(id);

    public Laudo? UltimoLaudo(int pedidoId) => db.Set<Laudo>().Where(l => l.PedidoId == pedidoId).OrderByDescending(l => l.Versao).FirstOrDefault();

    public void Adicionar(Laudo laudo) => db.Add(laudo);

    public IReadOnlyList<ControleDeQualidade> Serie(string equipamento, string analito) =>
        db.Set<ControleDeQualidade>().Where(c => c.Equipamento == equipamento && c.Analito == analito).OrderBy(c => c.Id).ToList();

    public void Adicionar(ControleDeQualidade controle) => db.Add(controle);

    IReadOnlyList<EventoDeAuditoria> IRegistroDeAuditoria.Eventos() => db.Set<EventoDeAuditoria>().AsNoTracking().OrderBy(e => e.Seq).ToList();

    public IReadOnlyList<(string Tipo, string Nome)> Objetos()
    {
        using var comando = db.Database.GetDbConnection().CreateCommand();
        comando.CommandText = "SELECT type, name FROM sqlite_master WHERE name NOT LIKE 'sqlite_%' ORDER BY type, name";
        using var leitor = comando.ExecuteReader();
        var lista = new List<(string, string)>();
        while (leitor.Read())
        {
            lista.Add((leitor.GetString(0), leitor.GetString(1)));
        }

        return lista;
    }

    private List<(Agendamento, HorarioDaGrade)> Linhas(IQueryable<Agendamento> agendamentos, Expression<Func<HorarioDaGrade, bool>> filtro) =>
        agendamentos.Join(db.Set<HorarioDaGrade>().Where(filtro), a => a.HorarioId, h => h.Id, (a, h) => new { a, h })
            .OrderBy(x => x.h.Inicio).AsEnumerable().Select(x => (x.a, x.h)).ToList();
}
