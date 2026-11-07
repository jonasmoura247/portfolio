using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bancada.Aplicacao;
using Bancada.Dominio;

namespace Bancada.Testes.Aplicacao;

public class SessaoTestes
{
    [Test]
    public void Dispose_DescartaCadaRecursoUmaSoVezMesmoSeChamadoDuasVezes()
    {
        var a = new RecursoRastreado();
        var b = new RecursoRastreado();
        var sessao = new Sessao("x", Papel.Medico, "MED-01", DateTimeOffset.UnixEpoch, new CifraFalsa(), [a, b]);

        sessao.Dispose();
        sessao.Dispose();

        Assert.That(sessao.Descartada, Is.True);
        Assert.That(a.Descartes, Is.EqualTo(1));
        Assert.That(b.Descartes, Is.EqualTo(1));
    }

    [Test]
    public void Tocar_AtualizaOUltimoAcessoSemMexerNaCriacao()
    {
        var inicio = new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.Zero);
        var sessao = new Sessao("x", Papel.Medico, "MED-01", inicio, new CifraFalsa(), []);

        sessao.Tocar(inicio.AddMinutes(5));

        Assert.That(sessao.UltimoAcesso, Is.EqualTo(inicio.AddMinutes(5)));
        Assert.That(sessao.CriadaEm, Is.EqualTo(inicio));
    }

    [Test]
    public void Cifra_EAMesmaInjetadaNaCriacao()
    {
        var cifra = new CifraFalsa();
        var sessao = new Sessao("x", Papel.Coleta, "COL-01", DateTimeOffset.UnixEpoch, cifra, []);

        Assert.That(sessao.Cifra, Is.SameAs(cifra));
    }

    [Test]
    public void CifraFalsa_IdaEVolta()
    {
        var cifra = new CifraFalsa();

        Assert.That(cifra.Decifrar(cifra.Cifrar("oi", "aad"), "aad"), Is.EqualTo("oi"));
    }

    [Test]
    public void Aviso_GuardaAChaveAteAProximaPagina()
    {
        using var s = new FabricaDeTeste().Criar("s", DateTimeOffset.UnixEpoch, Cenarios.Padrao);
        s.Aviso = ("a.b", true);

        Assert.That(s.Aviso, Is.EqualTo(("a.b", true)));
    }

    private static (Sessao Sessao, RecursoRastreado Recurso) Nova()
    {
        var recurso = new RecursoRastreado();
        return (new Sessao("x", Papel.Admin, "ADM-01", DateTimeOffset.UnixEpoch, new CifraFalsa(), [recurso]), recurso);
    }

    [Test]
    public async Task SEC07_DescartadaEmUso_SoLiberaOsRecursosQuandoARequisicaoSai()
    {
        var (sessao, recurso) = Nova();
        await sessao.Entrar(CancellationToken.None);

        sessao.Dispose();

        Assert.That(sessao.Descartada, Is.True);
        Assert.That(recurso.Descartes, Is.Zero, "o banco não fecha no meio da requisição");
        sessao.Sair();
        Assert.That(recurso.Descartes, Is.EqualTo(1));
        sessao.Dispose();
        Assert.That(recurso.Descartes, Is.EqualTo(1));
    }

    [Test]
    public async Task SEC07_TentarEntrar_SoComAVezLivreESessaoViva()
    {
        var (sessao, _) = Nova();
        await sessao.Entrar(CancellationToken.None);

        Assert.That(sessao.TentarEntrar(), Is.False, "ocupada");
        sessao.Sair();
        Assert.That(sessao.TentarEntrar(), Is.True, "livre");
        sessao.Sair();
        sessao.Dispose();
        Assert.That(sessao.TentarEntrar(), Is.False, "descartada");
        Assert.That(sessao.TentarEntrar(), Is.False, "a vez foi devolvida mesmo recusando");
    }

    [Test]
    public void SEC04_Fila_TemTetoERecusaDepoisDeDescartada()
    {
        var (sessao, _) = Nova();
        var evento = new EventoPendente("LimiteDeTaxaExcedido", "Sessao", "-", "{}", "Bloqueado");

        var aceitos = Enumerable.Range(0, Sessao.MaximoDePendentes + 20).Count(_ => sessao.Enfileirar(evento));

        Assert.That(aceitos, Is.EqualTo(Sessao.MaximoDePendentes));
        Assert.That(sessao.Pendentes, Is.EqualTo(Sessao.MaximoDePendentes));
        Assert.That(sessao.TirarPendente(out var primeiro), Is.True);
        Assert.That(primeiro, Is.EqualTo(evento));
        sessao.Dispose();
        Assert.That(sessao.Enfileirar(evento), Is.False);
        new ServicoDeAuditoria(new RelogioManual()).Anotar(sessao, "IpBloqueado");
        Assert.That(sessao.Eventos, Is.Empty, "sessão descartada não grava mais nada");
    }

    [Test]
    public async Task SEC04_SairSeNadaPendente_ComFilaFicaComAVez_DescartadaSaiMesmoAssim()
    {
        var (sessao, recurso) = Nova();
        var evento = new EventoPendente("X", "Sessao", "-", "{}", "Ok");
        await sessao.Entrar(CancellationToken.None);
        sessao.Enfileirar(evento);

        Assert.That(sessao.SairSeNadaPendente(), Is.False, "há pendente: continua com a vez");
        Assert.That(sessao.TentarEntrar(), Is.False);
        sessao.Dispose();
        Assert.That(sessao.SairSeNadaPendente(), Is.True, "descartada: sai e larga a fila");
        Assert.That(recurso.Descartes, Is.EqualTo(1));
    }

    [Test]
    public async Task SEC04_GravacaoFalhou_DevolveAVezMesmoAssim()
    {
        var (sessao, _) = Nova();
        sessao.GravadorDeAuditoria = _ => throw new InvalidOperationException("banco fora");
        var auditoria = new ServicoDeAuditoria(new RelogioManual());
        await sessao.Entrar(CancellationToken.None);
        sessao.Enfileirar(new EventoPendente("X", "Sessao", "-", "{}", "Ok"));

        Assert.That(() => auditoria.SairDescarregando(sessao), Throws.InvalidOperationException);
        Assert.That(sessao.TentarEntrar(), Is.True, "a sessão não fica travada para sempre");
    }

    [Test]
    public async Task SEC04_Anotar_ComASessaoOcupada_EsperaNaFilaEQuemSaiGrava()
    {
        var (sessao, _) = Nova();
        var auditoria = new ServicoDeAuditoria(new RelogioManual());
        await sessao.Entrar(CancellationToken.None);

        auditoria.Anotar(sessao, "LimiteDeTaxaExcedido");

        Assert.That(sessao.Eventos, Is.Empty);
        Assert.That(sessao.Pendentes, Is.EqualTo(1));
        auditoria.SairDescarregando(sessao);
        Assert.That(sessao.Eventos.Select(e => (e.Acao, e.Resultado)), Is.EqualTo(new[] { ("LimiteDeTaxaExcedido", "Bloqueado") }));
        Assert.That(sessao.TentarEntrar(), Is.True, "a vez foi devolvida");
    }

    [Test]
    public void SEC04_Anotar_ComASessaoLivre_GravaNaHora()
    {
        var (sessao, _) = Nova();
        var auditoria = new ServicoDeAuditoria(new RelogioManual());

        auditoria.Anotar(sessao, "CorpoGrandeDemais");

        Assert.That(sessao.Eventos.Select(e => e.Acao), Is.EqualTo(new[] { "CorpoGrandeDemais" }));
        Assert.That(sessao.Pendentes, Is.Zero);
    }

    [Test]
    public async Task SEC04_MuitasAnotacoesSimultaneas_CadeiaIntegraSemPerderNenhuma()
    {
        var (sessao, _) = Nova();
        var auditoria = new ServicoDeAuditoria(new RelogioManual());

        var dono = Task.Run(async () =>
        {
            for (var i = 0; i < 50; i++)
            {
                await sessao.Entrar(CancellationToken.None);
                auditoria.Registrar(sessao, "Requisicao");
                auditoria.SairDescarregando(sessao);
            }
        });
        var anotadores = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 10; i++)
            {
                auditoria.Anotar(sessao, "LimiteDeTaxaExcedido");
            }
        }));
        await Task.WhenAll([dono, .. anotadores]);

        Assert.That(ServicoDeAuditoria.Verificar(sessao), Is.Null);
        Assert.That(sessao.Eventos.Select(e => e.Seq), Is.EqualTo(Enumerable.Range(1, 130)));
        Assert.That(sessao.Eventos.Count(e => e.Acao == "LimiteDeTaxaExcedido"), Is.EqualTo(80));
        Assert.That(sessao.Pendentes, Is.Zero);
    }

    [Test]
    public void ModeloDoBanco_Descarta()
    {
        new Bancada.Infraestrutura.Dados.ModeloDoBanco().Dispose();
        Assert.Pass();
    }
}
