using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Bancada.Infraestrutura;
using Bancada.Infraestrutura.Dados;

namespace Bancada.Testes.Infraestrutura;

public class InfraDaSessaoTestes
{
    private static readonly ModeloDoBanco Modelo = new();

    [Test]
    public void Fabrica_LigaBancoEAuditoriaNaSessao()
    {
        using var sessao = new FabricaDeSessao(Modelo).Criar("id", DateTimeOffset.UnixEpoch, Cenarios.Padrao);
        var auditoria = new ServicoDeAuditoria(new RelogioDoSistema());

        auditoria.Registrar(sessao, "SessaoCriada");

        Assert.That(sessao.Banco, Is.TypeOf<BancoDaSessao>());
        using var db = ((BancoDaSessao)sessao.Banco!).NovoContexto();
        IRegistroDeAuditoria registro = new Repositorios(db);
        Assert.That(registro.Eventos(), Has.Count.EqualTo(1));
    }

    [Test]
    public void Sessao_AoDescartarFechaOBanco()
    {
        var sessao = new FabricaDeSessao(Modelo).Criar("id", DateTimeOffset.UnixEpoch, "feliz");
        var banco = (BancoDaSessao)sessao.Banco!;

        sessao.Dispose();

        Assert.That(banco.Conexao.State, Is.EqualTo(System.Data.ConnectionState.Closed));
        Assert.That(sessao.Cenario, Is.EqualTo("feliz"));
    }

    [Test]
    public async Task Processo_RodaDeVerdadeESaiComOCodigo()
    {
        var linhas = new List<string>();
        var comando = new ComandoDeTeste("dotnet", ["--version"], TimeSpan.FromSeconds(30));

        var codigo = await new ProcessosDoSistema(AppContext.BaseDirectory).Rodar(comando, linhas.Add, CancellationToken.None);

        Assert.That(codigo, Is.Zero);
        Assert.That(linhas, Is.Not.Empty);
    }

    [Test]
    public async Task SEC06_Processo_NaoHerdaVariavelForaDaLista()
    {
        Environment.SetEnvironmentVariable("BANCADA_SEGREDO_DE_TESTE", "vazou");
        var linhas = new List<string>();
        var comando = new ComandoDeTeste("node", ["-e", "console.log(process.env.BANCADA_SEGREDO_DE_TESTE ?? 'ausente')"], TimeSpan.FromSeconds(30));

        try
        {
            await new ProcessosDoSistema(AppContext.BaseDirectory).Rodar(comando, linhas.Add, CancellationToken.None);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BANCADA_SEGREDO_DE_TESTE", null);
        }

        Assert.That(linhas, Is.EqualTo(new[] { "ausente" }));
    }

    [Test]
    public async Task Processo_PassouDoTempo_MataEDevolveNulo()
    {
        using var limite = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var comando = new ComandoDeTeste("ping", ["-n", "30", "127.0.0.1"], TimeSpan.FromSeconds(1));

        var codigo = await new ProcessosDoSistema(AppContext.BaseDirectory).Rodar(comando, _ => { }, limite.Token);

        Assert.That(codigo, Is.Null);
    }
}
