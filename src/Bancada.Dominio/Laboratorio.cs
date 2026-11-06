using System;
using System.Collections.Generic;
using System.Linq;

namespace Bancada.Dominio;

public enum EstadoPedido
{
    Solicitado,
    Coletado,
    EmAnalise,
    ResultadoPreliminar,
    Liberado,
    Cancelado,
}

public sealed class PedidoItem(string exame)
{
    public int Id { get; private set; }

    public int PedidoId { get; private set; }

    public string Exame { get; private set; } = exame;
}

public sealed class Pedido(string codigo, int pacienteId, int? agendamentoId, string solicitanteId, bool urgente, DateTime criadoEm)
{
    public const int MaximoDeExames = 20;

    public int Id { get; private set; }

    public string Codigo { get; private set; } = codigo;

    public int PacienteId { get; private set; } = pacienteId;

    public int? AgendamentoId { get; private set; } = agendamentoId;

    public string SolicitanteId { get; private set; } = solicitanteId;

    public bool Urgente { get; private set; } = urgente;

    public DateTime CriadoEm { get; private set; } = criadoEm;

    public DateTime? LiberadoEm { get; private set; }

    public EstadoPedido Estado { get; private set; }

    public List<PedidoItem> Itens { get; private set; } = [];

    /// <summary>RN-020: de 1 a 20 exames, sem repetição.</summary>
    public static Pedido Criar(string codigo, int pacienteId, int? agendamentoId, string solicitanteId, bool urgente, IReadOnlyCollection<string> exames, DateTime agora)
    {
        if (exames.Count is 0 or > MaximoDeExames)
        {
            throw new ViolacaoDeRegra("pedido.erro.quantidade");
        }

        if (exames.Distinct().Count() != exames.Count)
        {
            throw new ViolacaoDeRegra("pedido.erro.repetido");
        }

        var pedido = new Pedido(codigo, pacienteId, agendamentoId, solicitanteId, urgente, agora);
        foreach (var exame in exames)
        {
            pedido.Itens.Add(new PedidoItem(Catalogo.ExameDe(exame).Codigo));
        }

        return pedido;
    }

    public IReadOnlyList<Tubo> Tubos() => Catalogo.Tubos(Itens.Select(i => i.Exame));

    public void Cancelar()
    {
        if (Estado != EstadoPedido.Solicitado)
        {
            throw new ViolacaoDeRegra("regra.transicao");
        }

        Estado = EstadoPedido.Cancelado;
    }

    public void MarcarColetado() => Estado = EstadoPedido.Coletado;

    public void Recoletar() => Estado = EstadoPedido.Solicitado;

    public void MarcarEmAnalise() => Estado = EstadoPedido.EmAnalise;

    public void MarcarResultado() => Estado = EstadoPedido.ResultadoPreliminar;

    public void Liberar(DateTime agora)
    {
        Estado = EstadoPedido.Liberado;
        LiberadoEm = agora;
    }
}

public enum EstadoAmostra
{
    Coletada,
    Armazenada,
    EmAnalise,
    Analisada,
    Rejeitada,
    Descartada,
}

public sealed class Amostra(string etiqueta, int pedidoId, int pacienteId, string material, string exames, double volumeMl, int jejumHoras, bool hemolisada, DateTime coletadaEm, DateTime validadeAte)
{
    public const int DiasDeValidade = 7;

    public int Id { get; private set; }

    public string Etiqueta { get; private set; } = etiqueta;

    public int PedidoId { get; private set; } = pedidoId;

    public int PacienteId { get; private set; } = pacienteId;

    public string Material { get; private set; } = material;

    public string Exames { get; private set; } = exames;

    public double VolumeMl { get; private set; } = volumeMl;

    public int JejumHoras { get; private set; } = jejumHoras;

    public bool Hemolisada { get; private set; } = hemolisada;

    public DateTime ColetadaEm { get; private set; } = coletadaEm;

    public DateTime ValidadeAte { get; private set; } = validadeAte;

    public EstadoAmostra Estado { get; private set; }

    public string? MotivoRejeicao { get; private set; }

    public string? MotivoDescarteCifrado { get; private set; }

    public IReadOnlyList<string> ListaDeExames => Exames.Split(',');

    /// <summary>RN-022 (jejum) e RN-023 (volume).</summary>
    public static Amostra Coletar(string etiqueta, Pedido pedido, Tubo tubo, double volumeMl, int jejumHoras, bool hemolisada, DateTime agora)
    {
        if (jejumHoras < tubo.JejumHoras)
        {
            throw new ViolacaoDeRegra("coleta.erro.jejum");
        }

        if (volumeMl < tubo.VolumeMl)
        {
            throw new ViolacaoDeRegra("coleta.erro.volume");
        }

        return new Amostra(etiqueta, pedido.Id, pedido.PacienteId, tubo.Material, string.Join(',', tubo.Exames), volumeMl, jejumHoras, hemolisada, agora, agora.AddDays(DiasDeValidade));
    }

    public bool VencidaEm(DateTime agora) => agora > ValidadeAte;

    public bool VenceEmUmDia(DateTime agora) => !VencidaEm(agora) && ValidadeAte <= agora.AddDays(1);

    /// <summary>RN-025: amostra hemolisada é rejeitada. RN-031: só soro vai para a soroteca.</summary>
    public void Armazenar()
    {
        if (Estado != EstadoAmostra.Coletada)
        {
            throw new ViolacaoDeRegra("regra.transicao");
        }

        if (Hemolisada)
        {
            throw new ViolacaoDeRegra("soroteca.erro.hemolisada");
        }

        if (Material != Catalogo.Soro)
        {
            throw new ViolacaoDeRegra("soroteca.erro.sosoro");
        }

        Estado = EstadoAmostra.Armazenada;
    }

    /// <summary>RN-033: amostra vencida não vai para análise.</summary>
    public void EnviarParaAnalise(DateTime agora)
    {
        var podeIr = Estado == EstadoAmostra.Armazenada || (Estado == EstadoAmostra.Coletada && Material != Catalogo.Soro && !Hemolisada);
        if (!podeIr)
        {
            throw new ViolacaoDeRegra("regra.transicao");
        }

        if (VencidaEm(agora))
        {
            throw new ViolacaoDeRegra("soroteca.erro.vencida");
        }

        Estado = EstadoAmostra.EmAnalise;
    }

    public void ConcluirAnalise() => Estado = Material == Catalogo.Soro ? EstadoAmostra.Armazenada : EstadoAmostra.Analisada;

    public void Rejeitar(string motivo)
    {
        if (Estado is not (EstadoAmostra.Coletada or EstadoAmostra.Armazenada))
        {
            throw new ViolacaoDeRegra("regra.transicao");
        }

        MotivoRejeicao = motivo;
        Estado = EstadoAmostra.Rejeitada;
    }

    public void Descartar(string motivoCifrado)
    {
        if (Estado != EstadoAmostra.Armazenada)
        {
            throw new ViolacaoDeRegra("regra.transicao");
        }

        MotivoDescarteCifrado = motivoCifrado;
        Estado = EstadoAmostra.Descartada;
    }
}

/// <summary>Cadeia de custódia da amostra: quem manuseou, quando e onde. Só inclusão (RN-035).</summary>
public sealed class CustodiaDaAmostra(int amostraId, string evento, string operadorId, string local, DateTime quando)
{
    public int Id { get; private set; }

    public int AmostraId { get; private set; } = amostraId;

    public string Evento { get; private set; } = evento;

    public string OperadorId { get; private set; } = operadorId;

    public string Local { get; private set; } = local;

    public DateTime Quando { get; private set; } = quando;
}

public sealed class PosicaoDaSoroteca(string caixa, int linha, int coluna)
{
    public const int Lado = 9;

    public int Id { get; private set; }

    public string Caixa { get; private set; } = caixa;

    public int Linha { get; private set; } = linha;

    public int Coluna { get; private set; } = coluna;

    public int? AmostraId { get; private set; }

    public string Nome => FormattableString.Invariant($"{Caixa}-{Linha}-{Coluna}");

    /// <summary>RN-030: uma posição guarda no máximo uma amostra.</summary>
    public void Ocupar(int amostraId)
    {
        if (AmostraId is not null)
        {
            throw new ViolacaoDeRegra("soroteca.erro.ocupada");
        }

        AmostraId = amostraId;
    }

    public void Liberar() => AmostraId = null;
}

public enum EstadoExecucao
{
    NaFila,
    Processando,
    Concluido,
    Erro,
}

public sealed class Execucao(int amostraId, int pedidoId, string exame, string equipamento, int prioridade, double totalMin, bool falhaForcada, string enviadaPorId, DateTime enviadaEm)
{
    public const int MaximoDeTentativas = 3;

    public int Id { get; private set; }

    public int AmostraId { get; private set; } = amostraId;

    public int PedidoId { get; private set; } = pedidoId;

    public string Exame { get; private set; } = exame;

    public string Equipamento { get; private set; } = equipamento;

    public int Prioridade { get; private set; } = prioridade;

    public double TotalMin { get; private set; } = totalMin;

    public double RestanteMin { get; private set; } = totalMin;

    public bool FalhaForcada { get; private set; } = falhaForcada;

    public string EnviadaPorId { get; private set; } = enviadaPorId;

    public DateTime EnviadaEm { get; private set; } = enviadaEm;

    public int Tentativas { get; private set; }

    public EstadoExecucao Estado { get; private set; }

    public bool Ativa => Estado is EstadoExecucao.NaFila or EstadoExecucao.Processando;

    public double Progresso => 1 - (RestanteMin / TotalMin);

    public void Iniciar() => Estado = EstadoExecucao.Processando;

    /// <summary>Avança o tempo simulado. A falha forçada do cenário acontece na metade da primeira tentativa.</summary>
    public void Avancar(double minutos)
    {
        RestanteMin = Math.Max(0, RestanteMin - minutos);
        if (FalhaForcada && Tentativas == 0 && RestanteMin <= TotalMin / 2)
        {
            Estado = EstadoExecucao.Erro;
            Tentativas = 1;
        }
        else if (RestanteMin == 0)
        {
            Estado = EstadoExecucao.Concluido;
        }
    }

    /// <summary>RN-042: até 3 tentativas no total.</summary>
    public void Reprocessar()
    {
        if (Estado != EstadoExecucao.Erro || Tentativas >= MaximoDeTentativas)
        {
            throw new ViolacaoDeRegra("equipamento.erro.reprocesso");
        }

        Estado = EstadoExecucao.NaFila;
        RestanteMin = TotalMin;
    }
}

public static class Simulador
{
    /// <summary>Um passo do relógio: promove da fila respeitando a capacidade (RN-041, urgente primeiro) e avança quem processa.</summary>
    public static IReadOnlyList<Execucao> Passo(IReadOnlyCollection<Execucao> execucoes, double minutos)
    {
        foreach (var equipamento in Catalogo.Equipamentos)
        {
            var doEquipamento = execucoes.Where(e => e.Equipamento == equipamento.Codigo).ToList();
            var livres = equipamento.Capacidade - doEquipamento.Count(e => e.Estado == EstadoExecucao.Processando);
            foreach (var e in doEquipamento.Where(e => e.Estado == EstadoExecucao.NaFila).OrderByDescending(e => e.Prioridade).ThenBy(e => e.EnviadaEm).Take(livres))
            {
                e.Iniciar();
            }
        }

        var concluidas = new List<Execucao>();
        foreach (var e in execucoes.Where(e => e.Estado == EstadoExecucao.Processando))
        {
            e.Avancar(minutos);
            if (e.Estado == EstadoExecucao.Concluido)
            {
                concluidas.Add(e);
            }
        }

        return concluidas;
    }
}

public enum EstadoResultado
{
    Preliminar,
    CriticoConfirmado,
    Validado,
}

public sealed class Resultado(int? execucaoId, int pedidoId, int pacienteId, string analito, double valor, string flag, double? anterior, bool deltaSuspeito, string equipamento, string enviadoPorId)
{
    public int Id { get; private set; }

    public int? ExecucaoId { get; private set; } = execucaoId;

    public int PedidoId { get; private set; } = pedidoId;

    public int PacienteId { get; private set; } = pacienteId;

    public string Analito { get; private set; } = analito;

    public double Valor { get; private set; } = valor;

    public string Flag { get; private set; } = flag;

    public double? Anterior { get; private set; } = anterior;

    public bool DeltaSuspeito { get; private set; } = deltaSuspeito;

    public string Equipamento { get; private set; } = equipamento;

    public string EnviadoPorId { get; private set; } = enviadoPorId;

    public EstadoResultado Estado { get; private set; }

    public string? ValidadoPorId { get; private set; }

    public string? JustificativaCifrada { get; private set; }

    public bool Critico => Flag is "CL" or "CH";

    /// <summary>RN-052.</summary>
    public void ConfirmarCritico()
    {
        if (!Critico || Estado != EstadoResultado.Preliminar)
        {
            throw new ViolacaoDeRegra("regra.transicao");
        }

        Estado = EstadoResultado.CriticoConfirmado;
    }

    /// <summary>RN-043 (controle de qualidade), RN-052 (crítico), RN-053 (delta) e RN-054 (separação de deveres).</summary>
    public void Validar(string operador, bool controleReprovado, string? justificativaCifrada)
    {
        if (Estado == EstadoResultado.Validado)
        {
            throw new ViolacaoDeRegra("regra.transicao");
        }

        if (operador == EnviadoPorId)
        {
            throw new ViolacaoDeRegra("resultado.erro.mesmooperador");
        }

        if (controleReprovado)
        {
            throw new ViolacaoDeRegra("resultado.erro.cq");
        }

        if (Critico && Estado == EstadoResultado.Preliminar)
        {
            throw new ViolacaoDeRegra("resultado.erro.critico");
        }

        if (DeltaSuspeito && justificativaCifrada is null)
        {
            throw new ViolacaoDeRegra("resultado.erro.delta");
        }

        Estado = EstadoResultado.Validado;
        ValidadoPorId = operador;
        JustificativaCifrada = justificativaCifrada;
    }

    /// <summary>Marcado pela semente: histórico já validado por outro analista.</summary>
    public static Resultado Historico(int pedidoId, int pacienteId, string analito, double valor)
    {
        var r = new Resultado(null, pedidoId, pacienteId, analito, valor, "N", null, false, Catalogo.Bioquimica, "ANA-01");
        r.Validar("ANA-02", false, null);
        return r;
    }
}

/// <summary>Laudo liberado: imutável. Retificação é uma versão nova (RN-056).</summary>
public sealed class Laudo(int pedidoId, int versao, string conteudoJson, string liberadoPorId, DateTime liberadoEm, string? motivoCifrado)
{
    public int Id { get; private set; }

    public int PedidoId { get; private set; } = pedidoId;

    public int Versao { get; private set; } = versao;

    public string ConteudoJson { get; private set; } = conteudoJson;

    public string LiberadoPorId { get; private set; } = liberadoPorId;

    public DateTime LiberadoEm { get; private set; } = liberadoEm;

    public string? MotivoCifrado { get; private set; } = motivoCifrado;
}

public sealed class ControleDeQualidade(string equipamento, string analito, double valor, double media, double desvio, DateTime medidoEm, string? regra, bool aprovado)
{
    public int Id { get; private set; }

    public string Equipamento { get; private set; } = equipamento;

    public string Analito { get; private set; } = analito;

    public double Valor { get; private set; } = valor;

    public double Media { get; private set; } = media;

    public double Desvio { get; private set; } = desvio;

    public DateTime MedidoEm { get; private set; } = medidoEm;

    public string? Regra { get; private set; } = regra;

    public bool Aprovado { get; private set; } = aprovado;

    public double Z => (Valor - Media) / Desvio;
}

public sealed class MensagemDeProtocolo(string equipamento, bool doEquipamento, DateTime quando, string texto)
{
    public int Id { get; private set; }

    public string Equipamento { get; private set; } = equipamento;

    public bool DoEquipamento { get; private set; } = doEquipamento;

    public DateTime Quando { get; private set; } = quando;

    public string Texto { get; private set; } = texto;
}
