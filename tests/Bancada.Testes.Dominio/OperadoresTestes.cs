using System;
using System.Linq;
using Bancada.Dominio;

namespace Bancada.Testes.Dominio;

public class OperadoresTestes
{
    private static readonly string[] IdsDosMedicos = ["MED-01", "MED-02"];

    [Test]
    public void Todos_TemIdsUnicos()
    {
        Assert.That(Operadores.Todos.Select(o => o.Id).Distinct().Count(), Is.EqualTo(Operadores.Todos.Count));
    }

    [Test]
    public void DoPapel_TodoPapelTemPeloMenosUmOperador_EAnalistaTemDois()
    {
        foreach (var papel in Enum.GetValues<Papel>())
        {
            Assert.That(Operadores.DoPapel(papel), Is.Not.Empty, papel.ToString());
        }

        Assert.That(Operadores.DoPapel(Papel.Analista), Has.Count.EqualTo(2), "separação de deveres exige dois analistas");
    }

    [Test]
    public void DoPapel_SoDevolveOperadoresDaquelePapel()
    {
        Assert.That(Operadores.DoPapel(Papel.Medico).Select(o => o.Id), Is.EquivalentTo(IdsDosMedicos));
    }

    [TestCase("MED-01", Papel.Medico, true)]
    [TestCase("MED-01", Papel.Analista, false)]
    [TestCase("XXX-99", Papel.Admin, false)]
    public void Pertence_ConfereIdEPapel(string id, Papel papel, bool esperado)
    {
        Assert.That(Operadores.Pertence(id, papel), Is.EqualTo(esperado));
    }
}
