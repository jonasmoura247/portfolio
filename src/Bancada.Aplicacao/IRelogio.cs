using System;

namespace Bancada.Aplicacao;

/// <summary>Hora atual. O relógio real só existe na Infraestrutura: tudo passa por aqui, para ser testável.</summary>
public interface IRelogio
{
    DateTimeOffset Agora { get; }
}
