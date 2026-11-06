using System;
using Bancada.Aplicacao;
using Bancada.Dominio;
using Bancada.Infraestrutura.Dados;

namespace Bancada.Infraestrutura;

/// <summary>Monta a sessão de um visitante: papel inicial (Admin, para o visitante ver tudo e trocar à vontade), cifra com chave só dele e a cópia particular do banco.</summary>
public sealed class FabricaDeSessao(ModeloDoBanco modelo, Papel papelInicial = Papel.Admin) : IFabricaDeSessao
{
    public Sessao Criar(string id, DateTimeOffset agora, string cenario)
    {
        var cifra = new CifraAesGcm(CifraAesGcm.NovaChave());
        var banco = modelo.Copiar(cifra, cenario);
        var operador = Operadores.DoPapel(papelInicial)[0].Id;
        return new Sessao(id, papelInicial, operador, agora, cifra, [banco, cifra])
        {
            Cenario = cenario,
            Banco = banco,
            GravadorDeAuditoria = banco.Registrar,
        };
    }
}
