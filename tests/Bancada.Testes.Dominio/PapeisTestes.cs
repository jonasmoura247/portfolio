using System;
using Bancada.Dominio;

namespace Bancada.Testes.Dominio;

public class PapeisTestes
{
    [TestCase(Papel.Recepcao, "Recepção")]
    [TestCase(Papel.Medico, "Médico(a)")]
    [TestCase(Papel.Coleta, "Coleta")]
    [TestCase(Papel.Analista, "Analista")]
    [TestCase(Papel.Admin, "Admin")]
    public void Rotulo_PapelConhecido_DevolveTextoEmPortugues(Papel papel, string esperado)
    {
        Assert.That(Papeis.Rotulo(papel), Is.EqualTo(esperado));
    }

    [Test]
    public void Rotulo_PapelInexistente_LancaErro()
    {
        Assert.That(() => Papeis.Rotulo((Papel)99), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Rotulo_TodoPapelDoEnum_TemRotulo()
    {
        foreach (var papel in Enum.GetValues<Papel>())
        {
            Assert.That(Papeis.Rotulo(papel), Is.Not.Empty);
        }
    }
}
