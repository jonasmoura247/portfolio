using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bancada.Aplicacao;

namespace Bancada.Testes.Aplicacao;

/// <summary>Salvaguardas do executor de testes (vault: Executor-de-Testes), com processo falso: nada roda de verdade aqui.</summary>
public class ExecutorTestes
{
    private sealed class ProcessoFalso(Func<ComandoDeTeste, Action<string>, CancellationToken, Task<int?>> rodar) : IProcessos
    {
        public int Chamadas { get; private set; }

        public ComandoDeTeste? Ultimo { get; private set; }

        public Task<int?> Rodar(ComandoDeTeste comando, Action<string> aoLer, CancellationToken cancelar)
        {
            Chamadas++;
            Ultimo = comando;
            return rodar(comando, aoLer, cancelar);
        }
    }

    private static ExecutorDeTestes Novo(ProcessoFalso processo, RelogioManual relogio, LimitesDoExecutor? limites = null) => new(processo, relogio, limites ?? new LimitesDoExecutor { Habilitado = true });

    [Test]
    public void Executor_PorPadrao_NasceDesligado()
    {
        var p = new ProcessoFalso((_, _, _) => Task.FromResult<int?>(0));

        Assert.That(new ExecutorDeTestes(p, new RelogioManual(), new LimitesDoExecutor()).Iniciar("js", "ip"), Is.EqualTo("painel.erro.desligado"));
        Assert.That(p.Chamadas, Is.Zero);
    }

    [Test]
    public async Task Executor_RodaOComandoConstanteEEntregaASaida()
    {
        var p = new ProcessoFalso((_, ler, _) => { ler("linha"); return Task.FromResult<int?>(0); });
        var e = Novo(p, new RelogioManual());

        Assert.That(e.Iniciar("js", "1.1.1.1"), Is.Null);
        await e.EmCurso!;

        Assert.Multiple(() =>
        {
            Assert.That(e.Retrato.Estado, Is.EqualTo(EstadoDoExecutor.Concluido));
            Assert.That(e.Retrato.Suite, Is.EqualTo("js"));
            Assert.That(e.Retrato.Saida, Does.Contain("linha"));
            Assert.That(p.Ultimo, Is.SameAs(ExecutorDeTestes.Comandos["js"]));
            Assert.That(e.Habilitado, Is.True);
        });
    }

    [TestCase("rm -rf /")]
    [TestCase(null)]
    public void Executor_SuiteForaDaListaNuncaCriaProcesso(string? suite)
    {
        var p = new ProcessoFalso((_, _, _) => Task.FromResult<int?>(0));

        Assert.That(Novo(p, new RelogioManual()).Iniciar(suite, "ip"), Is.EqualTo("painel.erro.suite"));
        Assert.That(p.Chamadas, Is.Zero);
    }

    [Test]
    public void Executor_Desligado_NuncaCriaProcesso()
    {
        var p = new ProcessoFalso((_, _, _) => throw new AssertionException("não podia rodar"));

        Assert.That(Novo(p, new RelogioManual(), new LimitesDoExecutor { Habilitado = false }).Iniciar("js", "ip"), Is.EqualTo("painel.erro.desligado"));
    }

    [Test]
    public async Task Executor_SegundaChamadaAssisteAMesmaExecucao()
    {
        var fim = new TaskCompletionSource<int?>();
        var p = new ProcessoFalso((_, _, _) => fim.Task);
        var e = Novo(p, new RelogioManual());

        Assert.That(e.Iniciar("csharp", "a"), Is.Null);
        Assert.That(e.Iniciar("js", "b"), Is.Null);
        Assert.That(e.Retrato.Estado, Is.EqualTo(EstadoDoExecutor.Rodando));
        fim.SetResult(1);
        await e.EmCurso!;

        Assert.That(p.Chamadas, Is.EqualTo(1));
        Assert.That(e.Retrato.Estado, Is.EqualTo(EstadoDoExecutor.Falhou));
    }

    [Test]
    public async Task Executor_LimitePorIpEPorHora()
    {
        var relogio = new RelogioManual();
        var e = Novo(new ProcessoFalso((_, _, _) => Task.FromResult<int?>(0)), relogio, new LimitesDoExecutor { Habilitado = true, PorHora = 2 });

        Assert.That(e.Iniciar("js", "a"), Is.Null);
        await e.EmCurso!;
        Assert.That(e.Iniciar("js", "a"), Is.EqualTo("painel.erro.limite"), "mesmo IP dentro de 5 minutos");
        Assert.That(e.Iniciar("js", "b"), Is.Null);
        await e.EmCurso!;
        Assert.That(e.Iniciar("js", "c"), Is.EqualTo("painel.erro.limite"), "teto por hora");

        relogio.Avancar(TimeSpan.FromMinutes(61));
        Assert.That(e.Iniciar("js", "a"), Is.Null);
        await e.EmCurso!;
    }

    [Test]
    public async Task Executor_SaidaGrandeECortada()
    {
        var p = new ProcessoFalso((_, ler, _) =>
        {
            foreach (var _ in Enumerable.Range(0, 100))
            {
                ler(new string('x', 10));
            }

            return Task.FromResult<int?>(0);
        });
        var e = Novo(p, new RelogioManual(), new LimitesDoExecutor { Habilitado = true, TamanhoDaSaida = 64 });
        e.Iniciar("js", "a");
        await e.EmCurso!;

        Assert.That(e.Retrato.Cortada, Is.True);
        Assert.That(e.Retrato.Saida, Has.Length.LessThanOrEqualTo(64));
    }

    [Test]
    public async Task Executor_ProgramaQueNaoSobeViraFalhaComMensagem()
    {
        var e = Novo(new ProcessoFalso((_, _, _) => throw new InvalidOperationException("sem node")), new RelogioManual());
        e.Iniciar("js", "a");
        await e.EmCurso!;

        Assert.That(e.Retrato.Estado, Is.EqualTo(EstadoDoExecutor.Falhou));
        Assert.That(e.Retrato.Saida, Does.Contain("sem node"));
    }

    [Test]
    public async Task Executor_PassouDoTempoFalha()
    {
        var e = Novo(new ProcessoFalso((_, _, _) => Task.FromResult<int?>(null)), new RelogioManual());
        e.Iniciar("js", "a");
        await e.EmCurso!;

        Assert.That(e.Retrato.Estado, Is.EqualTo(EstadoDoExecutor.Falhou));
    }
}
