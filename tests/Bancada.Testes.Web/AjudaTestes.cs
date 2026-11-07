using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bancada.Web;

namespace Bancada.Testes.Web;

/// <summary>Botão de ajuda em toda tela: explicação do módulo e, onde já foi gravado, o vídeo de exemplo.</summary>
public class AjudaTestes
{
    [TestCase("/bancada", "inicio")]
    [TestCase("/pacientes", "pacientes")]
    [TestCase("/pacientes/14", "pacientes")]
    [TestCase("/agenda", "agenda")]
    [TestCase("/atendimentos/5", "atendimento")]
    [TestCase("/prontuario/8", "prontuario")]
    [TestCase("/pedidos/novo", "pedido")]
    [TestCase("/coletas", "coleta")]
    [TestCase("/etiquetas", "etiquetas")]
    [TestCase("/amostras/3/etiqueta", "etiquetas")]
    [TestCase("/soroteca", "soroteca")]
    [TestCase("/equipamentos", "equipamento")]
    [TestCase("/execucoes/2/reprocessar", "equipamento")]
    [TestCase("/qualidade", "qualidade")]
    [TestCase("/resultados", "resultados")]
    [TestCase("/laudos/1", "resultados")]
    [TestCase("/auditoria", "auditoria")]
    [TestCase("/operacao", "operacao")]
    [TestCase("/painel", "painel")]
    [TestCase("/erro/404", "inicio")]
    [TestCase("/agendamento", "inicio")]
    public void IdDaRota_EscolheAAjudaDoModulo(string caminho, string esperado) =>
        Assert.That(Modulos.IdDaRota(caminho), Is.EqualTo(esperado));

    [Test]
    public void ToDoModuloTemTextoDeAjudaNosDoisIdiomas()
    {
        foreach (var id in Modulos.Todos.Select(m => m.Id))
        {
            foreach (var parte in new[] { "resumo", "passos" })
            {
                Assert.That(CatalogoDeTextos.Obter($"ajuda.{id}.{parte}", "pt"), Is.Not.Empty, $"{id}.{parte} pt");
                Assert.That(CatalogoDeTextos.Obter($"ajuda.{id}.{parte}", "en"), Is.Not.Empty, $"{id}.{parte} en");
            }
        }
    }

    [Test]
    public async Task BotaoDeAjuda_EstaEmCadaTela_ComVideoDeExemplo()
    {
        using var app = new AppDeTeste(Navegador.SemLimite, papelInicial: null);
        using var n = new Navegador(app.NovoCliente());

        foreach (var m in Modulos.Todos)
        {
            var (status, html) = await n.Ver(m.Rota);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), m.Rota);
            Assert.That(html, Does.Contain("data-abrir-ajuda"), m.Rota);
            Assert.That(html, Does.Contain("data-ajuda"), m.Rota);
            Assert.That(html, Does.Contain($"src=\"/videos/{m.Id}.mp4\""), m.Rota);

            var video = await n.Cliente.GetAsync($"/videos/{m.Id}.mp4");
            Assert.That(video.StatusCode, Is.EqualTo(HttpStatusCode.OK), m.Id);
            Assert.That((await video.Content.ReadAsByteArrayAsync()).Length, Is.GreaterThan(10_000), m.Id);
            Assert.That(video.Headers.GetValues("Content-Security-Policy").Single(), Does.Contain("media-src 'self'"));
        }
    }
}
