using System;
using Bancada.Aplicacao;
using Bancada.Dominio;

namespace Bancada.Testes.Aplicacao;

public class ServicoDeAuditoriaTestes
{
    private RelogioManual _relogio = null!;
    private ServicoDeAuditoria _servico = null!;
    private Sessao _sessao = null!;

    [SetUp]
    public void Preparar()
    {
        _relogio = new RelogioManual();
        _servico = new ServicoDeAuditoria(_relogio);
        _sessao = new FabricaDeTeste().Criar("s", _relogio.Agora, Cenarios.Padrao);
    }

    [TearDown]
    public void Limpar() => _sessao.Dispose();

    [Test]
    public void Registrar_PrimeiroEvento_LigaNoGenesisComSeqUm()
    {
        var e = _servico.Registrar(_sessao, "SessaoCriada");

        Assert.That(e.Seq, Is.EqualTo(1));
        Assert.That(e.HashAnterior, Is.EqualTo(CadeiaDeAuditoria.HashGenesis));
        Assert.That(_sessao.Eventos, Has.Count.EqualTo(1));
    }

    [Test]
    public void Registrar_CapturaPapelOperadorEHoraDoRelogio()
    {
        _relogio.Avancar(TimeSpan.FromMinutes(7));

        var e = _servico.Registrar(_sessao, "PedidoCriado", "Pedido", "PED-000045", "{\"exames\":3}", "Ok");

        Assert.That(e.Papel, Is.EqualTo("Recepcao"));
        Assert.That(e.Operador, Is.EqualTo("REC-01"));
        Assert.That(e.Quando, Is.EqualTo(_relogio.Agora));
        Assert.That(e.AlvoTipo, Is.EqualTo("Pedido"));
        Assert.That(e.AlvoId, Is.EqualTo("PED-000045"));
        Assert.That(e.DetalheJson, Is.EqualTo("{\"exames\":3}"));
    }

    [Test]
    public void Registrar_VariosEventos_FormamCadeiaIntegra()
    {
        for (var i = 0; i < 25; i++)
        {
            _servico.Registrar(_sessao, "Evento" + i);
        }

        Assert.That(_sessao.Eventos[^1].Seq, Is.EqualTo(25));
        Assert.That(_sessao.Eventos[5].HashAnterior, Is.EqualTo(_sessao.Eventos[4].Hash));
        Assert.That(ServicoDeAuditoria.Verificar(_sessao), Is.Null);
    }

    [Test]
    public void Verificar_SessaoSemEventos_EIntegra()
    {
        Assert.That(ServicoDeAuditoria.Verificar(_sessao), Is.Null);
    }

    [Test]
    public void Argumentos_Nulos_LancamErro()
    {
        Assert.That(() => _servico.Registrar(null!, "x"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => ServicoDeAuditoria.Verificar(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
