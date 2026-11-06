using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Bancada.Dominio;
using Bancada.Protocolos;

namespace Bancada.Aplicacao;

public sealed record LinhaDoPedido(Pedido Pedido, Paciente Paciente);

public sealed class ServicoDePedidos(IPedidos pedidos, IPacientes pacientes, IUnidadeDeTrabalho unidade, ContextoDaSessao contexto, ServicoDeAuditoria auditoria) : CasoDeUso(contexto, auditoria)
{
    public IReadOnlyList<LinhaDoPedido> Listar() => pedidos.Listar().Select(p => new LinhaDoPedido(p, pacientes.Obter(p.PacienteId)!)).ToList();

    public IReadOnlyList<Alergia> AlergiasAtivas(string? codigo)
    {
        var p = pacientes.PorCodigo(codigo ?? string.Empty);
        return p is null ? [] : pacientes.Alergias(p.Id).Where(a => a.Ativa).ToList();
    }

    public Pedido Criar(string? codigoPaciente, int? agendamentoId, IReadOnlyCollection<string> exames, bool urgente)
    {
        var p = pacientes.PorCodigo(codigoPaciente ?? string.Empty) ?? throw new ViolacaoDeRegra("agenda.erro.paciente");
        var numero = pedidos.ProximoNumero();
        var novo = Pedido.Criar(string.Create(CultureInfo.InvariantCulture, $"PED-{numero:000000}"), p.Id, agendamentoId, Operador, urgente, exames, Agora);
        Sessao.ContarRegistro();
        pedidos.Adicionar(novo);
        unidade.Salvar();
        Auditar("PedidoCriado", "Pedido", novo.Codigo, p.Codigo);
        return novo;
    }

    public LinhaDoPedido Detalhe(int id)
    {
        var p = pedidos.Obter(id) ?? throw new NaoEncontrado("pedido");
        return new LinhaDoPedido(p, pacientes.Obter(p.PacienteId)!);
    }

    public void Cancelar(int id)
    {
        var p = pedidos.Obter(id) ?? throw new NaoEncontrado("pedido");
        p.Cancelar();
        unidade.Salvar();
        Auditar("PedidoCancelado", "Pedido", p.Codigo);
    }
}

public sealed record TuboPendente(Pedido Pedido, Paciente Paciente, Tubo Tubo);

public sealed record LinhaDaAmostra(Amostra Amostra, Paciente Paciente, Pedido Pedido)
{
    public string Barras => Code128B.Codificar(Amostra.Etiqueta);
}

public sealed class ServicoDeColeta(IPedidos pedidos, IPacientes pacientes, IAmostras amostras, IUnidadeDeTrabalho unidade, ContextoDaSessao contexto, ServicoDeAuditoria auditoria) : CasoDeUso(contexto, auditoria)
{
    public static readonly string[] MotivosDeRejeicao = ["Hemolise", "VolumeInsuficiente", "MaterialErrado", "EtiquetaIlegivel"];

    public IReadOnlyList<TuboPendente> Pendentes()
    {
        var lista = new List<TuboPendente>();
        foreach (var p in pedidos.Listar().Where(x => x.Estado == EstadoPedido.Solicitado))
        {
            var coletados = amostras.DoPedido(p.Id).Where(a => a.Estado != EstadoAmostra.Rejeitada).Select(a => a.Material).ToHashSet();
            var paciente = pacientes.Obter(p.PacienteId)!;
            lista.AddRange(p.Tubos().Where(t => !coletados.Contains(t.Material)).Select(t => new TuboPendente(p, paciente, t)));
        }

        return lista;
    }

    public IReadOnlyList<LinhaDaAmostra> Amostras() => amostras.Abertas().Select(Linha).ToList();

    /// <summary>Consulta de etiquetas: por código da etiqueta, código ou nome do paciente. Busca vazia lista todas.</summary>
    public IReadOnlyList<LinhaDaAmostra> Consultar(string? busca)
    {
        var texto = (busca ?? string.Empty).Trim();
        texto = texto.Length > 64 ? texto[..64] : texto;
        return Amostras().Where(l => texto.Length == 0 || Contem(l.Amostra.Etiqueta, texto) || Contem(l.Paciente.Codigo, texto) || Contem(l.Paciente.Nome, texto)).ToList();
    }

    private static bool Contem(string valor, string texto) => valor.Contains(texto, StringComparison.OrdinalIgnoreCase);

    public LinhaDaAmostra Etiqueta(int id) => Linha(amostras.Obter(id) ?? throw new NaoEncontrado("amostra"));

    public Amostra Coletar(int pedidoId, string? material, double volumeMl, int jejumHoras, bool hemolisada)
    {
        var p = pedidos.Obter(pedidoId) ?? throw new NaoEncontrado("pedido");
        var tubo = p.Tubos().FirstOrDefault(t => t.Material == material) ?? throw new ViolacaoDeRegra("coleta.erro.tubo");
        if (amostras.DoPedido(pedidoId).Any(a => a.Material == material && a.Estado != EstadoAmostra.Rejeitada))
        {
            throw new ViolacaoDeRegra("coleta.erro.jacoletado");
        }

        var etiqueta = string.Create(CultureInfo.InvariantCulture, $"AMO-{amostras.ProximoNumero():000000}");
        var amostra = Amostra.Coletar(etiqueta, p, tubo, volumeMl, jejumHoras, hemolisada, Agora);
        Sessao.ContarRegistro();
        amostras.Adicionar(amostra);
        unidade.Salvar();
        amostras.Adicionar(new CustodiaDaAmostra(amostra.Id, "Coletada", Operador, "Coleta", Agora));
        var feitos = amostras.DoPedido(pedidoId).Where(a => a.Estado != EstadoAmostra.Rejeitada).Select(a => a.Material).ToHashSet();
        if (p.Tubos().All(t => feitos.Contains(t.Material)))
        {
            p.MarcarColetado();
        }

        unidade.Salvar();
        Auditar("AmostraColetada", "Amostra", amostra.Etiqueta, pacientes.Obter(p.PacienteId)!.Codigo);
        return amostra;
    }

    /// <summary>RN-025: rejeição exige motivo e devolve o pedido para recoleta.</summary>
    public void Rejeitar(int id, string? motivo)
    {
        var a = amostras.Obter(id) ?? throw new NaoEncontrado("amostra");
        if (Array.IndexOf(MotivosDeRejeicao, motivo) < 0)
        {
            throw new ViolacaoDeRegra("coleta.erro.motivo");
        }

        a.Rejeitar(motivo!);
        amostras.PosicaoDa(id)?.Liberar();
        pedidos.Obter(a.PedidoId)!.Recoletar();
        amostras.Adicionar(new CustodiaDaAmostra(id, "Rejeitada", Operador, "Coleta", Agora));
        unidade.Salvar();
        Auditar("AmostraRejeitada", "Amostra", a.Etiqueta);
    }

    private LinhaDaAmostra Linha(Amostra a) => new(a, pacientes.Obter(a.PacienteId)!, pedidos.Obter(a.PedidoId)!);
}

public sealed record CelulaDaSoroteca(PosicaoDaSoroteca Posicao, Amostra? Amostra, bool Vencida, bool Vencendo);

public sealed record DetalheDaPosicao(PosicaoDaSoroteca Posicao, Amostra? Amostra, Paciente? Paciente, IReadOnlyList<CustodiaDaAmostra> Custodia);

public sealed record VisaoDaSoroteca(string Caixa, IReadOnlyList<string> Caixas, IReadOnlyList<CelulaDaSoroteca> Celulas, IReadOnlyList<Amostra> Aguardando, DetalheDaPosicao? Selecionada);

public sealed class ServicoDeSoroteca(IAmostras amostras, IPacientes pacientes, IUnidadeDeTrabalho unidade, ContextoDaSessao contexto, ServicoDeAuditoria auditoria) : CasoDeUso(contexto, auditoria)
{
    public VisaoDaSoroteca Ver(string? caixa, int? posicaoId)
    {
        var caixas = amostras.Caixas();
        var atual = caixas.Contains(caixa ?? string.Empty) ? caixa! : caixas[0];
        var posicoes = amostras.Posicoes(atual);
        var porId = amostras.Porcoes(posicoes.Where(p => p.AmostraId is not null).Select(p => p.AmostraId!.Value).ToList()).ToDictionary(a => a.Id);
        var celulas = posicoes.Select(p =>
        {
            var a = p.AmostraId is int id ? porId[id] : null;
            return new CelulaDaSoroteca(p, a, a?.VencidaEm(Agora) ?? false, a?.VenceEmUmDia(Agora) ?? false);
        }).ToList();
        var aguardando = amostras.Abertas().Where(a => a.Estado == EstadoAmostra.Coletada && a.Material == Catalogo.Soro).ToList();
        return new VisaoDaSoroteca(atual, caixas, celulas, aguardando, posicaoId is int s ? Detalhe(s) : null);
    }

    public void Armazenar(int amostraId, int posicaoId)
    {
        var a = amostras.Obter(amostraId) ?? throw new NaoEncontrado("amostra");
        var pos = amostras.ObterPosicao(posicaoId) ?? throw new NaoEncontrado("posicao");
        a.Armazenar();
        pos.Ocupar(a.Id);
        amostras.Adicionar(new CustodiaDaAmostra(a.Id, "Recebida", Operador, "Soroteca", Agora));
        amostras.Adicionar(new CustodiaDaAmostra(a.Id, "Armazenada", Operador, pos.Nome, Agora));
        unidade.Salvar();
        Auditar("AmostraArmazenada", "Amostra", a.Etiqueta);
    }

    public void Mover(int origemId, int destinoId)
    {
        var origem = amostras.ObterPosicao(origemId) ?? throw new NaoEncontrado("posicao");
        var destino = amostras.ObterPosicao(destinoId) ?? throw new NaoEncontrado("posicao");
        var amostra = origem.AmostraId ?? throw new ViolacaoDeRegra("soroteca.erro.vazia");
        if (destino.AmostraId is not null)
        {
            throw new ViolacaoDeRegra("soroteca.erro.ocupada");
        }

        // Em dois passos: o índice único não aceita a mesma amostra em duas posições nem por um instante.
        origem.Liberar();
        unidade.Salvar();
        destino.Ocupar(amostra);
        amostras.Adicionar(new CustodiaDaAmostra(amostra, "Movida", Operador, destino.Nome, Agora));
        unidade.Salvar();
        Auditar("AmostraMovida", "Amostra", amostras.Obter(amostra)!.Etiqueta);
    }

    /// <summary>RN-034: descarte registra quem, quando e motivo, e libera a posição.</summary>
    public void Descartar(int amostraId, string? motivo)
    {
        var a = amostras.Obter(amostraId) ?? throw new NaoEncontrado("amostra");
        var limpo = Texto.Exigir(motivo, 1, 200, "soroteca.erro.motivo");
        a.Descartar(Sessao.Cifra.Cifrar(limpo, $"descarte:{a.Etiqueta}"));
        amostras.PosicaoDa(amostraId)!.Liberar();
        amostras.Adicionar(new CustodiaDaAmostra(a.Id, "Descartada", Operador, "Soroteca", Agora));
        unidade.Salvar();
        Auditar("AmostraDescartada", "Amostra", a.Etiqueta);
    }

    private DetalheDaPosicao? Detalhe(int posicaoId)
    {
        var pos = amostras.ObterPosicao(posicaoId);
        if (pos is null)
        {
            return null;
        }

        var a = pos.AmostraId is int id ? amostras.Obter(id) : null;
        return new DetalheDaPosicao(pos, a, a is null ? null : pacientes.Obter(a.PacienteId), a is null ? [] : amostras.Custodia(a.Id));
    }
}

public sealed record LinhaDaExecucao(Execucao Execucao, Amostra Amostra);

public sealed record VisaoDoEquipamento(Equipamento Equipamento, IReadOnlyList<LinhaDaExecucao> Fila, int EmUso);

public sealed record VisaoDosEquipamentos(DateTime Relogio, int Fator, bool Pausado, IReadOnlyList<VisaoDoEquipamento> Equipamentos, IReadOnlyList<Amostra> Prontas, IReadOnlyList<MensagemDeProtocolo> Mensagens);

/// <summary>
/// Simulação sem processo em segundo plano: o tempo anda quando alguém consulta. Pedido e resultado passam pelo protocolo
/// de verdade (ASTM ou HL7): o valor gravado é o que foi lido de volta da mensagem.
/// </summary>
public sealed class ServicoDeEquipamentos(IEquipamentos equipamentos, IAmostras amostras, IPedidos pedidos, IPacientes pacientes, IResultados resultados, IRelogio relogio, IUnidadeDeTrabalho unidade, ContextoDaSessao contexto, ServicoDeAuditoria auditoria) : CasoDeUso(contexto, auditoria)
{
    public VisaoDosEquipamentos Ver()
    {
        Avancar();
        var abertas = equipamentos.Abertas();
        var linhas = abertas.Select(e => new LinhaDaExecucao(e, amostras.Obter(e.AmostraId)!)).ToList();
        var visao = Catalogo.Equipamentos.Select(q =>
        {
            var fila = linhas.Where(l => l.Execucao.Equipamento == q.Codigo).ToList();
            return new VisaoDoEquipamento(q, fila, fila.Count(l => l.Execucao.Estado == EstadoExecucao.Processando));
        }).ToList();
        var prontas = amostras.Abertas().Where(PodeIr).ToList();
        var r = Sessao.Simulacao;
        return new VisaoDosEquipamentos(r.Agora, r.Fator, r.Pausado, visao, prontas, equipamentos.Mensagens(60));
    }

    public void Enviar(int amostraId)
    {
        var a = amostras.Obter(amostraId) ?? throw new NaoEncontrado("amostra");
        var p = pedidos.Obter(a.PedidoId)!;
        var pac = pacientes.Obter(a.PacienteId)!;
        if (!PedidoAguardaAnalise(a))
        {
            throw new ViolacaoDeRegra("regra.transicao");
        }

        a.EnviarParaAnalise(Agora);
        p.MarcarEmAnalise();
        foreach (var codigo in a.ListaDeExames)
        {
            var exame = Catalogo.ExameDe(codigo);
            equipamentos.Adicionar(new Execucao(a.Id, p.Id, codigo, exame.Equipamento, p.Urgente ? 1 : 0, exame.TempoMin, Cenarios.ExameFalha(Sessao.Cenario, codigo), Operador, Agora));
            RegistrarPedido(exame, a, p, pac);
        }

        amostras.Adicionar(new CustodiaDaAmostra(a.Id, "EnviadaAoEquipamento", Operador, "Equipamento", Agora));
        Sessao.Simulacao.Marcar(relogio.Agora);
        unidade.Salvar();
        Auditar("ExecucaoEnviada", "Amostra", a.Etiqueta, pac.Codigo);
    }

    public void Reprocessar(int id)
    {
        var e = equipamentos.Obter(id) ?? throw new NaoEncontrado("execucao");
        e.Reprocessar();
        unidade.Salvar();
        Auditar("ExecucaoReprocessada", "Execucao", e.Exame);
    }

    public void Relogio(string? comando)
    {
        if (comando == "pausar")
        {
            Sessao.Simulacao.Pausar();
        }
        else
        {
            Sessao.Simulacao.Definir(int.TryParse(comando, CultureInfo.InvariantCulture, out var f) ? f : 0);
        }

        Sessao.Simulacao.Marcar(relogio.Agora);
    }

    private bool PedidoAguardaAnalise(Amostra a) =>
        pedidos.Obter(a.PedidoId)!.Estado is EstadoPedido.Coletado or EstadoPedido.EmAnalise
        && equipamentos.DoPedido(a.PedidoId).All(e => e.AmostraId != a.Id);

    private bool PodeIr(Amostra a) => PedidoAguardaAnalise(a) && (a.Estado == EstadoAmostra.Armazenada || (a.Material != Catalogo.Soro && !a.Hemolisada));

    private void Avancar()
    {
        var abertas = equipamentos.Abertas();
        var ativas = abertas.Where(e => e.Ativa).ToList();
        if (ativas.Count == 0)
        {
            Sessao.Simulacao.Marcar(relogio.Agora);
            return;
        }

        var minutos = Sessao.Simulacao.Avancar(relogio.Agora);
        if (minutos <= 0)
        {
            return;
        }

        foreach (var e in Simulador.Passo(abertas, minutos))
        {
            Concluir(e);
        }

        foreach (var e in ativas.Where(e => e.Estado == EstadoExecucao.Erro))
        {
            Auditar("ExecucaoComErro", "Execucao", e.Exame);
        }

        unidade.Salvar();
    }

    private void Concluir(Execucao e)
    {
        var a = amostras.Obter(e.AmostraId)!;
        var p = pedidos.Obter(e.PedidoId)!;
        var pac = pacientes.Obter(p.PacienteId)!;
        var exame = Catalogo.ExameDe(e.Exame);
        var medidos = exame.Analitos.Select(cod =>
        {
            var analito = Catalogo.AnalitoDe(cod);
            var valor = Cenarios.ValorForcado(Sessao.Cenario, p.Codigo, cod) ?? GeradorDeValores.Normal(analito, pac.Sexo, p.Codigo);
            return (Analito: analito, Valor: valor);
        }).ToList();

        foreach (var (codigo, valor) in TransmitirResultado(exame, a, p, medidos))
        {
            var analito = Catalogo.AnalitoDe(codigo);
            var f = analito.FaixaPara(pac.Sexo);
            var anterior = resultados.UltimoValidado(pac.Id, codigo);
            resultados.Adicionar(new Resultado(e.Id, p.Id, pac.Id, codigo, valor, Analise.Classificar(valor, f.NormalMin, f.NormalMax, f.CriticoMin, f.CriticoMax), anterior, Analise.DeltaSuspeito(valor, anterior, analito.DeltaPct), e.Equipamento, e.EnviadaPorId));
        }

        Auditar("ExecucaoConcluida", "Execucao", e.Exame, pac.Codigo);
        unidade.Salvar();
        if (equipamentos.DoPedido(p.Id).All(x => x.Estado == EstadoExecucao.Concluido) && equipamentos.DoPedido(p.Id).Count == p.Itens.Count)
        {
            p.MarcarResultado();
        }

        if (equipamentos.DoPedido(p.Id).Where(x => x.AmostraId == a.Id).All(x => x.Estado == EstadoExecucao.Concluido))
        {
            a.ConcluirAnalise();
        }
    }

    private void RegistrarPedido(Exame exame, Amostra a, Pedido p, Paciente pac)
    {
        var quando = Agora.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var nascimento = pac.Nascimento.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        if (Catalogo.EquipamentoDe(exame.Equipamento).Protocolo == "ASTM")
        {
            Gravar(exame.Equipamento, false, "ENQ");
            foreach (var quadro in Astm.Quadros([Astm.Cabecalho("Bancada", quando), Astm.Paciente(pac.Codigo, nascimento, pac.Sexo), Astm.Pedido(a.Etiqueta, exame.Codigo), Astm.Fim()]))
            {
                Gravar(exame.Equipamento, false, quadro);
                Gravar(exame.Equipamento, true, "ACK");
            }

            Gravar(exame.Equipamento, false, "EOT");
        }
        else
        {
            foreach (var s in Hl7.Pedido(exame.Equipamento.Replace("-", string.Empty, StringComparison.Ordinal), p.Codigo, pac.Codigo, nascimento, pac.Sexo, exame.Codigo, quando, a.Id))
            {
                Gravar(exame.Equipamento, false, s);
            }

            Gravar(exame.Equipamento, true, "MSA|AA|ACK");
        }
    }

    private IReadOnlyList<(string Analito, double Valor)> TransmitirResultado(Exame exame, Amostra a, Pedido p, IReadOnlyList<(Analito Analito, double Valor)> medidos)
    {
        var quando = Agora.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        if (Catalogo.EquipamentoDe(exame.Equipamento).Protocolo == "ASTM")
        {
            var registros = new List<string> { Astm.Cabecalho(exame.Equipamento, quando), Astm.Pedido(a.Etiqueta, exame.Codigo) };
            registros.AddRange(medidos.Select((m, i) => Astm.Resultado(i + 1, m.Analito.Codigo, m.Valor, m.Analito.Unidade, "F")));
            registros.Add(Astm.Fim());
            var quadros = Astm.Quadros(registros);
            Gravar(exame.Equipamento, true, "ENQ");
            foreach (var q in quadros)
            {
                Gravar(exame.Equipamento, true, q);
                Gravar(exame.Equipamento, false, "ACK");
            }

            Gravar(exame.Equipamento, true, "EOT");
            return Astm.LerResultados(Astm.Registros(quadros));
        }

        var obx = medidos.Select((m, i) => Hl7.Observacao(i + 1, m.Analito.Codigo, m.Valor, m.Analito.Unidade, "F"));
        var mensagem = Hl7.Resultado(exame.Equipamento.Replace("-", string.Empty, StringComparison.Ordinal), p.Codigo, exame.Codigo, quando, a.Id, obx);
        foreach (var s in mensagem)
        {
            Gravar(exame.Equipamento, true, s);
        }

        Gravar(exame.Equipamento, false, "MSA|AA|ACK");
        return Hl7.LerResultados(mensagem);
    }

    private void Gravar(string equipamento, bool doEquipamento, string texto) => equipamentos.Adicionar(new MensagemDeProtocolo(equipamento, doEquipamento, Agora, texto));
}

public sealed record VisaoDaQualidade(IReadOnlyList<ControleDeQualidade> Serie, IReadOnlyList<string> Regras, bool Reprovado, double Media, double Desvio);

public sealed class ServicoDeQualidade(IQualidade qualidade, IUnidadeDeTrabalho unidade, ContextoDaSessao contexto, ServicoDeAuditoria auditoria) : CasoDeUso(contexto, auditoria)
{
    public const string Equipamento = Catalogo.Bioquimica;
    public const string Analito = "POT";
    public const double Media = 4.2;
    public const double Desvio = 0.12;

    private static readonly double[] Proximos = [0.2, -0.5, 0.6, -0.1, 0.4];

    public VisaoDaQualidade Ver()
    {
        var serie = qualidade.Serie(Equipamento, Analito);
        var regras = Analise.Westgard(serie.Select(c => c.Z).ToList(), out var soAviso);
        return new VisaoDaQualidade(serie, regras, regras.Count > 0 && !soAviso, Media, Desvio);
    }

    public bool Reprovado(string equipamento) => equipamento == Equipamento && Ver().Reprovado;

    /// <summary>Nova medida de controle: em sequência fixa, para a demonstração ser repetível.</summary>
    public void Registrar()
    {
        var serie = qualidade.Serie(Equipamento, Analito);
        var z = Proximos[serie.Count % Proximos.Length];
        qualidade.Adicionar(new ControleDeQualidade(Equipamento, Analito, Media + (z * Desvio), Media, Desvio, Agora, null, true));
        unidade.Salvar();
        Auditar("ControleRegistrado", "Equipamento", Equipamento);
    }
}

public sealed record PedidoParaValidar(Pedido Pedido, Paciente Paciente, IReadOnlyList<Resultado> Resultados);

public sealed record ItemDoLaudo(string Analito, double Valor, string Unidade, string Flag, int Casas, double? NormalMin, double? NormalMax);

public sealed record VisaoDoLaudo(Laudo Laudo, Pedido Pedido, Paciente Paciente, int Idade, IReadOnlyList<ItemDoLaudo> Itens, string? Motivo, IReadOnlyList<int> Versoes);

public sealed record LinhaDoLaudo(Laudo Laudo, Pedido Pedido, Paciente Paciente);

public sealed class ServicoDeResultados(IResultados resultados, IPedidos pedidos, IPacientes pacientes, ServicoDeQualidade qualidade, IUnidadeDeTrabalho unidade, ContextoDaSessao contexto, ServicoDeAuditoria auditoria) : CasoDeUso(contexto, auditoria)
{
    public IReadOnlyList<PedidoParaValidar> Pendentes() => resultados.PedidosComPendencia().Select(id =>
    {
        var p = pedidos.Obter(id)!;
        return new PedidoParaValidar(p, pacientes.Obter(p.PacienteId)!, resultados.DoPedido(id));
    }).ToList();

    public IReadOnlyList<LinhaDoLaudo> Laudos() => resultados.Laudos().Select(l =>
    {
        var p = pedidos.Obter(l.PedidoId)!;
        return new LinhaDoLaudo(l, p, pacientes.Obter(p.PacienteId)!);
    }).ToList();

    public void ConfirmarCritico(int id)
    {
        var r = Exigir(id);
        r.ConfirmarCritico();
        unidade.Salvar();
        Auditar("CriticoConfirmado", "Resultado", r.Analito);
    }

    public void Validar(int id, string? justificativa)
    {
        var r = Exigir(id);
        var texto = Texto.Exigir(justificativa, 0, 200, "comum.erro.textolongo");
        r.Validar(Operador, qualidade.Reprovado(r.Equipamento), Cifrar(texto, $"justificativa:{r.Id}"));
        unidade.Salvar();
        Auditar("ResultadoValidado", "Resultado", r.Analito);
    }

    /// <summary>RN-055: laudo só sai com todos os resultados do pedido validados.</summary>
    public Laudo Liberar(int pedidoId)
    {
        var p = pedidos.Obter(pedidoId) ?? throw new NaoEncontrado("pedido");
        var rs = resultados.DoPedido(pedidoId);
        if (p.Estado != EstadoPedido.ResultadoPreliminar || rs.Any(r => r.Estado != EstadoResultado.Validado))
        {
            throw new ViolacaoDeRegra("laudo.erro.incompleto");
        }

        var laudo = NovaVersao(p, rs, 1, null);
        p.Liberar(Agora);
        unidade.Salvar();
        Auditar("LaudoLiberado", "Pedido", p.Codigo, pacientes.Obter(p.PacienteId)!.Codigo);
        return laudo;
    }

    /// <summary>RN-056: laudo liberado é imutável; retificar cria a versão seguinte.</summary>
    public Laudo Retificar(int pedidoId, string? motivo)
    {
        var p = pedidos.Obter(pedidoId) ?? throw new NaoEncontrado("pedido");
        var ultimo = resultados.UltimoLaudo(pedidoId) ?? throw new ViolacaoDeRegra("laudo.erro.incompleto");
        var texto = Texto.Exigir(motivo, 1, 200, "laudo.erro.motivo");
        var laudo = NovaVersao(p, resultados.DoPedido(pedidoId), ultimo.Versao + 1, texto);
        unidade.Salvar();
        Auditar("LaudoRetificado", "Pedido", p.Codigo);
        return laudo;
    }

    public VisaoDoLaudo Ver(int laudoId)
    {
        var l = resultados.ObterLaudo(laudoId) ?? throw new NaoEncontrado("laudo");
        var p = pedidos.Obter(l.PedidoId)!;
        var pac = pacientes.Obter(p.PacienteId)!;
        var versoes = resultados.Laudos().Where(x => x.PedidoId == p.Id).Select(x => x.Id).OrderBy(x => x).ToList();
        return new VisaoDoLaudo(l, p, pac, pac.IdadeEm(Sessao.Simulacao.Hoje), JsonSerializer.Deserialize<List<ItemDoLaudo>>(l.ConteudoJson)!, Decifrar(l.MotivoCifrado, Dados(p.Id, l.Versao)), versoes);
    }

    private Laudo NovaVersao(Pedido p, IReadOnlyList<Resultado> rs, int versao, string? motivo)
    {
        var sexo = pacientes.Obter(p.PacienteId)!.Sexo;
        var itens = rs.Select(r =>
        {
            var a = Catalogo.AnalitoDe(r.Analito);
            var f = a.FaixaPara(sexo);
            return new ItemDoLaudo(r.Analito, r.Valor, a.Unidade, r.Flag, a.Casas, f.NormalMin, f.NormalMax);
        }).ToList();
        var laudo = new Laudo(p.Id, versao, JsonSerializer.Serialize(itens), Operador, Agora, Cifrar(motivo, Dados(p.Id, versao)));
        resultados.Adicionar(laudo);
        return laudo;
    }

    private static string Dados(int pedidoId, int versao) => string.Create(CultureInfo.InvariantCulture, $"laudo:{pedidoId}:{versao}");

    private Resultado Exigir(int id) => resultados.Obter(id) ?? throw new NaoEncontrado("resultado");
}

public sealed record TempoPorExame(string Exame, double Minutos);

public sealed record VisaoDaOperacao(IReadOnlyList<TempoPorExame> Tempos, IReadOnlyList<(string Equipamento, int Itens)> Filas, int Livres, int Ocupadas, int Vencendo, int Vencidas);

public sealed class ServicoDeOperacao(IPedidos pedidos, IEquipamentos equipamentos, ServicoDeSoroteca soroteca)
{
    public VisaoDaOperacao Ver()
    {
        var tempos = pedidos.Listar().Where(p => p.LiberadoEm is not null)
            .SelectMany(p => p.Itens.Select(i => (i.Exame, Minutos: (p.LiberadoEm!.Value - p.CriadoEm).TotalMinutes)))
            .GroupBy(x => x.Exame)
            .Select(g => new TempoPorExame(g.Key, Math.Round(g.Average(x => x.Minutos))))
            .OrderBy(t => t.Minutos).ToList();
        var abertas = equipamentos.Abertas();
        var filas = Catalogo.Equipamentos.Select(q => (q.Codigo, abertas.Count(e => e.Equipamento == q.Codigo && e.Ativa))).ToList();
        var celulas = soroteca.Ver(null, null).Celulas;
        var ocupadas = celulas.Where(c => c.Amostra is not null).ToList();
        return new VisaoDaOperacao(tempos, filas, celulas.Count - ocupadas.Count, ocupadas.Count(c => !c.Vencida && !c.Vencendo), ocupadas.Count(c => c.Vencendo), ocupadas.Count(c => c.Vencida));
    }
}
