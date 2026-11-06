using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bancada.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Bancada.Testes.Guardas;

/// <summary>Toda action de controller declara a ação que exerce ([Autorizar]); só as páginas públicas ficam numa lista fechada.</summary>
public class AutorizacaoTestes
{
    private static readonly HashSet<string> Publicas = ["InicioController.Index", "ErroController.Index"];

    private static IEnumerable<MethodInfo> Actions() =>
        typeof(AutorizarAttribute).Assembly.GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any() || m.GetCustomAttributes<RouteAttribute>().Any());

    [Test]
    public void Controllers_ExistemActionsParaConferir()
    {
        Assert.That(Actions().Count(), Is.GreaterThanOrEqualTo(5));
    }

    [Test]
    public void Actions_TodasDeclaramAutorizarOuEstaoNaListaPublica()
    {
        var sem = Actions()
            .Where(m => !Publicas.Contains($"{m.DeclaringType!.Name}.{m.Name}"))
            .Where(m => m.GetCustomAttribute<AutorizarAttribute>() is null && m.DeclaringType!.GetCustomAttribute<AutorizarAttribute>() is null)
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
            .ToList();

        Assert.That(sem, Is.Empty, "action sem [Autorizar(Acao.X)]: entra na matriz ou na lista de páginas públicas");
    }

    [Test]
    public void ListaPublica_SoTemActionsQueExistem()
    {
        var existentes = Actions().Select(m => $"{m.DeclaringType!.Name}.{m.Name}").ToHashSet(StringComparer.Ordinal);

        Assert.That(Publicas.Except(existentes), Is.Empty);
    }

    [Test]
    public void Actions_DeEscritaNuncaSaoGet()
    {
        var gets = Actions()
            .Where(m => m.Name is "Papel" or "Operador" or "Reiniciar")
            .Where(m => m.GetCustomAttributes<HttpGetAttribute>().Any())
            .ToList();

        Assert.That(gets, Is.Empty);
    }
}
