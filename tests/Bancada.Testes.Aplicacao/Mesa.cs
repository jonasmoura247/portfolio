using System;
using System.Collections.Generic;
using System.Linq;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Bancada.Infraestrutura;
using Bancada.Infraestrutura.Dados;

namespace Bancada.Testes.Aplicacao;

/// <summary>Bancada de testes: casos de uso de verdade sobre o banco de verdade (SQLite em memória, cópia do modelo), sem mock de repositório.</summary>
internal sealed class Mesa : IDisposable
{
    private static readonly ModeloDoBanco Modelo = new();

    private readonly BancadaDbContext _db;

    public Mesa(string cenario = Cenarios.Padrao)
    {
        Sessao = new FabricaDeSessao(Modelo, Papel.Recepcao).Criar("id-teste", Relogio.Agora, cenario);
        Banco = (BancoDaSessao)Sessao.Banco!;
        _db = Banco.NovoContexto();
        Repos = new Repositorios(_db);
        Contexto = new ContextoDaSessao { Sessao = Sessao };
        Auditoria = new ServicoDeAuditoria(Relogio);
        Pacientes = new ServicoDePacientes(Repos, Repos, Repos, Contexto, Auditoria);
        Agenda = new ServicoDeAgenda(Repos, Repos, Repos, Contexto, Auditoria);
        Atendimento = new ServicoDeAtendimento(Repos, Repos, Repos, Repos, Repos, Contexto, Auditoria);
        Prontuario = new ServicoDeProntuario(Repos, Repos, Repos, Repos, Repos, Contexto, Auditoria);
        Pedidos = new ServicoDePedidos(Repos, Repos, Repos, Contexto, Auditoria);
        Coleta = new ServicoDeColeta(Repos, Repos, Repos, Repos, Contexto, Auditoria);
        Soroteca = new ServicoDeSoroteca(Repos, Repos, Repos, Contexto, Auditoria);
        Equipamentos = new ServicoDeEquipamentos(Repos, Repos, Repos, Repos, Repos, Relogio, Repos, Contexto, Auditoria);
        Qualidade = new ServicoDeQualidade(Repos, Repos, Contexto, Auditoria);
        Resultados = new ServicoDeResultados(Repos, Repos, Repos, Qualidade, Repos, Contexto, Auditoria);
        Operacao = new ServicoDeOperacao(Repos, Repos, Soroteca);
        ConsultaAuditoria = new ConsultaDeAuditoria(Repos, Contexto, Auditoria);
    }

    public RelogioManual Relogio { get; } = new();

    public Sessao Sessao { get; }

    public BancoDaSessao Banco { get; }

    public Repositorios Repos { get; }

    public ContextoDaSessao Contexto { get; }

    public ServicoDeAuditoria Auditoria { get; }

    public ServicoDePacientes Pacientes { get; }

    public ServicoDeAgenda Agenda { get; }

    public ServicoDeAtendimento Atendimento { get; }

    public ServicoDeProntuario Prontuario { get; }

    public ServicoDePedidos Pedidos { get; }

    public ServicoDeColeta Coleta { get; }

    public ServicoDeSoroteca Soroteca { get; }

    public ServicoDeEquipamentos Equipamentos { get; }

    public ServicoDeQualidade Qualidade { get; }

    public ServicoDeResultados Resultados { get; }

    public ServicoDeOperacao Operacao { get; }

    public ConsultaDeAuditoria ConsultaAuditoria { get; }

    public IReadOnlyList<Resultado> ResultadosDe(int pedido)
    {
        IResultados r = Repos;
        return r.DoPedido(pedido);
    }

    public Pedido PedidoDoCenario => Pedidos.Listar().Single(l => l.Pedido.Codigo == Cenarios.PedidoDoCenario).Pedido;

    public static string Erro(Action acao) => Assert.Throws<ViolacaoDeRegra>(() => acao())!.Chave;

    public void Operar(Papel papel, string operador) => Sessao.Assumir(papel, operador);

    /// <summary>Do pedido do cenário até os resultados na tela de validação, como ANA-01.</summary>
    public Pedido ProcessarCenario()
    {
        var pedido = PedidoDoCenario;
        var amostra = Coleta.Coletar(pedido.Id, Catalogo.Soro, 5, 12, false);
        var livre = Repos.Posicoes("R2-C4")[0];
        Soroteca.Armazenar(amostra.Id, livre.Id);
        Operar(Papel.Analista, "ANA-01");
        Equipamentos.Enviar(amostra.Id);
        AndarOTempo(TimeSpan.FromSeconds(9));
        return pedido;
    }

    /// <summary>Passa tempo real e deixa a tela do equipamento promover e avançar a simulação (fator ×60: 1 s vira 1 min).</summary>
    public void AndarOTempo(TimeSpan tempo)
    {
        Equipamentos.Ver();
        Relogio.Avancar(TimeSpan.FromMilliseconds(1));
        Equipamentos.Ver();
        Relogio.Avancar(tempo);
        Equipamentos.Ver();
    }

    public void Dispose()
    {
        _db.Dispose();
        Sessao.Dispose();
    }
}
