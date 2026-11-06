using System;
using Bancada.Aplicacao;

namespace Bancada.Infraestrutura;

public sealed class RelogioDoSistema : IRelogio
{
    public DateTimeOffset Agora => DateTimeOffset.UtcNow;
}
