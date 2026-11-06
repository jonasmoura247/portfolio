using System;
using Bancada.Aplicacao;
using Bancada.Dominio;

namespace Bancada.Testes.Aplicacao;

public class RepositorioDeSessoesTestes
{
    private RelogioManual _relogio = null!;
    private FabricaDeTeste _fabrica = null!;
    private RepositorioDeSessoes _repo = null!;

    [SetUp]
    public void Preparar()
    {
        _relogio = new RelogioManual();
        _fabrica = new FabricaDeTeste();
        _repo = Novo(new LimitesDeSessao { Maximo = 3 });
    }

    private RepositorioDeSessoes Novo(LimitesDeSessao limites) => new(_relogio, new GeradorSequencial(), _fabrica, limites);

    [Test]
    public void ObterOuCriar_SemId_CriaSessaoNova()
    {
        var s = _repo.ObterOuCriar(null);

        Assert.That(s.Id, Is.EqualTo("id-1"));
        Assert.That(s.Papel, Is.EqualTo(Papel.Recepcao));
        Assert.That(_repo.Quantidade, Is.EqualTo(1));
    }

    [Test]
    public void ObterOuCriar_IdExistente_DevolveAMesmaESlideOTempo()
    {
        var s = _repo.ObterOuCriar(null);
        _relogio.Avancar(TimeSpan.FromMinutes(20));

        var de_novo = _repo.ObterOuCriar(s.Id);

        Assert.That(de_novo, Is.SameAs(s));
        Assert.That(s.UltimoAcesso, Is.EqualTo(_relogio.Agora));
    }

    [Test]
    public void ObterOuCriar_IdInventadoPeloCliente_NuncaEAdotado()
    {
        var s = _repo.ObterOuCriar("id-que-eu-inventei");

        Assert.That(s.Id, Is.Not.EqualTo("id-que-eu-inventei"));
        Assert.That(_repo.Quantidade, Is.EqualTo(1));
    }

    [Test]
    public void ObterOuCriar_UsoContinuoAntesDoLimiteOcioso_MantemViva()
    {
        var s = _repo.ObterOuCriar(null);
        for (var i = 0; i < 3; i++)
        {
            _relogio.Avancar(TimeSpan.FromMinutes(29));
            Assert.That(_repo.ObterOuCriar(s.Id), Is.SameAs(s), $"passo {i}");
        }
    }

    [Test]
    public void ObterOuCriar_DepoisDoLimiteOcioso_ExpiraEDescartaOsRecursos()
    {
        var s = _repo.ObterOuCriar(null);
        _relogio.Avancar(TimeSpan.FromMinutes(31));

        var nova = _repo.ObterOuCriar(s.Id);

        Assert.That(nova, Is.Not.SameAs(s));
        Assert.That(s.Descartada, Is.True);
        Assert.That(_fabrica.Recursos[0].Descartes, Is.EqualTo(1));
        Assert.That(_repo.Quantidade, Is.EqualTo(1));
    }

    [Test]
    public void ObterOuCriar_DepoisDoLimiteTotal_ExpiraMesmoComUsoConstante()
    {
        var s = _repo.ObterOuCriar(null);
        for (var i = 0; i < 6; i++)
        {
            _relogio.Avancar(TimeSpan.FromMinutes(20));
            _repo.ObterOuCriar(s.Id);
        }

        _relogio.Avancar(TimeSpan.FromMinutes(20)); // 140 min desde a criação, nunca ocioso

        Assert.That(_repo.ObterOuCriar(s.Id), Is.Not.SameAs(s));
        Assert.That(s.Descartada, Is.True);
    }

    [Test]
    public void ObterOuCriar_NoTeto_DescartaAMaisOciosa()
    {
        var a = _repo.ObterOuCriar(null);
        _relogio.Avancar(TimeSpan.FromMinutes(1));
        var b = _repo.ObterOuCriar(null);
        _relogio.Avancar(TimeSpan.FromMinutes(1));
        var c = _repo.ObterOuCriar(null);
        _relogio.Avancar(TimeSpan.FromMinutes(1));
        _repo.ObterOuCriar(a.Id); // a volta a ser a mais recente; b vira a mais ociosa

        var d = _repo.ObterOuCriar(null);

        Assert.That(_repo.Quantidade, Is.EqualTo(3));
        Assert.That(b.Descartada, Is.True);
        Assert.That(new[] { a.Descartada, c.Descartada, d.Descartada }, Has.None.True);
    }

    [Test]
    public void Renovar_TrocaPapelOperadorEIdEInvalidaOAntigo()
    {
        var s = _repo.ObterOuCriar(null);
        var antigo = s.Id;

        _repo.Renovar(s, Papel.Analista, "ANA-02");

        Assert.That(s.Id, Is.Not.EqualTo(antigo));
        Assert.That(s.Papel, Is.EqualTo(Papel.Analista));
        Assert.That(s.Operador, Is.EqualTo("ANA-02"));
        Assert.That(_repo.ObterOuCriar(s.Id), Is.SameAs(s));
        Assert.That(_repo.ObterOuCriar(antigo), Is.Not.SameAs(s), "id antigo deixa de valer");
    }

    [Test]
    public void Renovar_OperadorDeOutroPapel_LancaErro()
    {
        var s = _repo.ObterOuCriar(null);

        Assert.That(() => _repo.Renovar(s, Papel.Analista, "MED-01"), Throws.TypeOf<ArgumentException>());
        Assert.That(s.Papel, Is.EqualTo(Papel.Recepcao), "nada mudou");
    }

    [Test]
    public void Renovar_SessaoJaDescartada_LancaErro()
    {
        var s = _repo.ObterOuCriar(null);
        _repo.Remover(s.Id);

        Assert.That(() => _repo.Renovar(s, Papel.Admin, "ADM-01"), Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => _repo.Renovar(null!, Papel.Admin, "ADM-01"), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Remover_DescartaAExistenteEIgnoraAInexistente()
    {
        var s = _repo.ObterOuCriar(null);

        _repo.Remover("nao-existe");
        _repo.Remover(s.Id);

        Assert.That(_repo.Quantidade, Is.Zero);
        Assert.That(s.Descartada, Is.True);
    }

    [Test]
    public void Varrer_RemoveSoAsVencidas()
    {
        var velha = _repo.ObterOuCriar(null);
        _relogio.Avancar(TimeSpan.FromMinutes(25));
        var nova = _repo.ObterOuCriar(null);
        _relogio.Avancar(TimeSpan.FromMinutes(10)); // velha: 35 min ociosa; nova: 10 min

        Assert.That(_repo.Varrer(), Is.EqualTo(1));
        Assert.That(velha.Descartada, Is.True);
        Assert.That(nova.Descartada, Is.False);
        Assert.That(_repo.Varrer(), Is.Zero);
    }

    [Test]
    public void Construtor_TetoMenorQueUm_LancaErro()
    {
        Assert.That(() => Novo(new LimitesDeSessao { Maximo = 0 }), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Novo(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void LimitesDeSessao_PadroesSaoOsDoVault()
    {
        var l = new LimitesDeSessao();

        Assert.That(l.Ocioso, Is.EqualTo(TimeSpan.FromMinutes(30)));
        Assert.That(l.Total, Is.EqualTo(TimeSpan.FromHours(2)));
        Assert.That(l.Maximo, Is.EqualTo(200));
    }

    [Test]
    public void Encontrar_NaoCriaNemToca()
    {
        var s = _repo.ObterOuCriar(null);
        _relogio.Avancar(TimeSpan.FromMinutes(1));

        Assert.That(_repo.Encontrar(s.Id), Is.SameAs(s));
        Assert.That(s.UltimoAcesso, Is.Not.EqualTo(_relogio.Agora));
        Assert.That(_repo.Encontrar("inventado"), Is.Null);
        Assert.That(_repo.Encontrar(null), Is.Null);
        Assert.That(_repo.Quantidade, Is.EqualTo(1));
    }

    [Test]
    public void Recriar_DescartaAAntigaECriaOutra()
    {
        var s = _repo.ObterOuCriar(null);

        var nova = _repo.Recriar(s, "feliz");

        Assert.That(nova.Id, Is.Not.EqualTo(s.Id));
        Assert.That(s.Descartada, Is.True);
        Assert.That(_repo.UltimaCriada, Is.SameAs(nova));
        Assert.That(_repo.Encontrar(s.Id), Is.Null);
    }
}
