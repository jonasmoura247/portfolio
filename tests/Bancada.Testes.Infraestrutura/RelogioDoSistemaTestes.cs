using System;
using Bancada.Infraestrutura;

namespace Bancada.Testes.Infraestrutura;

public class RelogioDoSistemaTestes
{
    [Test]
    public void Agora_EstaProximoDaHoraDoSistemaEmUtc()
    {
        var relogio = new RelogioDoSistema();

        var diferenca = (DateTimeOffset.UtcNow - relogio.Agora).Duration();

        Assert.That(diferenca, Is.LessThan(TimeSpan.FromSeconds(5)));
        Assert.That(relogio.Agora.Offset, Is.EqualTo(TimeSpan.Zero));
    }
}
