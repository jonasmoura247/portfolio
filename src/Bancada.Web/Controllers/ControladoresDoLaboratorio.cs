using System.Collections.Generic;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace Bancada.Web.Controllers;

public sealed record ColetasVm(IReadOnlyList<TuboPendente> Pendentes, IReadOnlyList<LinhaDaAmostra> Amostras, bool SugereHemolise);

public sealed class ColetasController(ServicoDeColeta coleta) : ControladorDaBancada
{
    [HttpGet("/coletas"), Autorizar(Acao.ListarColetas)]
    public IActionResult Index() => View(new ColetasVm(coleta.Pendentes(), coleta.Amostras(), Cenarios.SugereHemolise(Sessao.Cenario, Cenarios.PedidoDoCenario)));

    [HttpPost("/coletas/{pedidoId:int}/coletar"), Autorizar(Acao.ColetarAmostra)]
    public IActionResult Coletar(int pedidoId, [FromForm] string? material, [FromForm] double volume, [FromForm] int jejum, [FromForm] bool hemolisada)
    {
        var destino = "/coletas";
        return Executar(() => destino = $"/amostras/{Num(coleta.Coletar(pedidoId, material, volume, jejum, hemolisada).Id)}/etiqueta", "coleta.ok.coletada", destino).Redirecionar(() => destino);
    }

    [HttpPost("/amostras/{id:int}/rejeitar"), Autorizar(Acao.RejeitarAmostra)]
    public IActionResult Rejeitar(int id, [FromForm] string? motivo) => Executar(() => coleta.Rejeitar(id, motivo), "coleta.ok.rejeitada", "/soroteca");

    [HttpGet("/etiquetas"), Autorizar(Acao.ConsultarEtiqueta)]
    public IActionResult Consultar(string? busca)
    {
        ViewBag.Busca = busca;
        return View(coleta.Consultar(busca));
    }

    [HttpGet("/amostras/{id:int}/etiqueta"), Autorizar(Acao.ConsultarEtiqueta)]
    public IActionResult Etiqueta(int id) => View(coleta.Etiqueta(id));
}

public sealed class SorotecaController(ServicoDeSoroteca soroteca) : ControladorDaBancada
{
    [HttpGet("/soroteca"), Autorizar(Acao.VerSoroteca)]
    public IActionResult Index(string? caixa, int? posicao) => View(soroteca.Ver(caixa, posicao));

    [HttpPost("/soroteca/armazenar"), Autorizar(Acao.ArmazenarAmostra)]
    public IActionResult Armazenar([FromForm] int amostra, [FromForm] int posicao, [FromForm] string? caixa) =>
        Executar(() => soroteca.Armazenar(amostra, posicao), "soroteca.ok.armazenada", Volta(caixa));

    [HttpPost("/soroteca/mover"), Autorizar(Acao.MoverAmostra)]
    public IActionResult Mover([FromForm] int origem, [FromForm] int destino, [FromForm] string? caixa) =>
        Executar(() => soroteca.Mover(origem, destino), "soroteca.ok.movida", Volta(caixa));

    [HttpPost("/soroteca/descartar"), Autorizar(Acao.DescartarAmostra)]
    public IActionResult Descartar([FromForm] int amostra, [FromForm] string? motivo, [FromForm] string? caixa) =>
        Executar(() => soroteca.Descartar(amostra, motivo), "soroteca.ok.descartada", Volta(caixa));

    private string Volta(string? caixa) => Url.Action("Index", new { caixa })!;
}

public sealed class EquipamentosController(ServicoDeEquipamentos equipamentos) : ControladorDaBancada
{
    private const string Tela = "/equipamentos";

    [HttpGet("/equipamentos"), Autorizar(Acao.VerEquipamento)]
    public IActionResult Index() => View(equipamentos.Ver());

    [HttpPost("/equipamentos/{amostra:int}/enviar"), Autorizar(Acao.EnviarAoEquipamento)]
    public IActionResult Enviar(int amostra) => Executar(() => equipamentos.Enviar(amostra), "equipamento.ok.enviada", Tela);

    [HttpPost("/equipamentos/relogio"), Autorizar(Acao.ControlarRelogio)]
    public IActionResult Relogio([FromForm] string? comando) => Executar(() => equipamentos.Relogio(comando), "equipamento.ok.relogio", Tela);

    [HttpPost("/execucoes/{id:int}/reprocessar"), Autorizar(Acao.ReprocessarExecucao)]
    public IActionResult Reprocessar(int id) => Executar(() => equipamentos.Reprocessar(id), "equipamento.ok.reprocesso", Tela);
}

public sealed class QualidadeController(ServicoDeQualidade qualidade) : ControladorDaBancada
{
    [HttpGet("/qualidade"), Autorizar(Acao.VerQualidade)]
    public IActionResult Index() => View(qualidade.Ver());

    [HttpPost("/qualidade/medicao"), Autorizar(Acao.RegistrarControle)]
    public IActionResult Medicao() => Executar(qualidade.Registrar, "qualidade.ok.registrado", "/qualidade");
}

public sealed record ResultadosVm(IReadOnlyList<PedidoParaValidar> Pendentes, IReadOnlyList<LinhaDoLaudo> Laudos);

public sealed class ResultadosController(ServicoDeResultados resultados) : ControladorDaBancada
{
    private const string Tela = "/resultados";

    [HttpGet("/resultados"), Autorizar(Acao.VerLaudo)]
    public IActionResult Index() =>
        View(new ResultadosVm(MatrizDeAutorizacao.Permite(Sessao.Papel, Acao.VerPendentes) ? resultados.Pendentes() : [], resultados.Laudos()));

    [HttpPost("/resultados/{id:int}/confirmar-critico"), Autorizar(Acao.ConfirmarCritico)]
    public IActionResult ConfirmarCritico(int id) => Executar(() => resultados.ConfirmarCritico(id), "resultado.ok.critico", Tela);

    [HttpPost("/resultados/{id:int}/validar"), Autorizar(Acao.ValidarResultado)]
    public IActionResult Validar(int id, [FromForm] string? justificativa) => Executar(() => resultados.Validar(id, justificativa), "resultado.ok.validado", Tela);

    [HttpGet("/laudos/{id:int}"), Autorizar(Acao.VerLaudo)]
    public IActionResult Laudo(int id) => View(resultados.Ver(id));

    [HttpPost("/laudos/{pedidoId:int}/liberar"), Autorizar(Acao.LiberarLaudo)]
    public IActionResult Liberar(int pedidoId)
    {
        var destino = Tela;
        return Executar(() => destino = "/laudos/" + Num(resultados.Liberar(pedidoId).Id), "laudo.ok.liberado", destino).Redirecionar(() => destino);
    }

    [HttpPost("/laudos/{pedidoId:int}/retificar"), Autorizar(Acao.RetificarLaudo)]
    public IActionResult Retificar(int pedidoId, [FromForm] string? motivo)
    {
        var destino = Tela;
        return Executar(() => destino = "/laudos/" + Num(resultados.Retificar(pedidoId, motivo).Id), "laudo.ok.retificado", destino).Redirecionar(() => destino);
    }
}
