using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Bancada.Aplicacao;
using Bancada.Infraestrutura;
using Bancada.Infraestrutura.Dados;
using Bancada.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Números de formulário sempre com ponto: em servidor pt-BR, "0.5" viraria 5 (ponto de milhar).
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var construtor = WebApplication.CreateBuilder(args);
construtor.WebHost.ConfigureKestrel(Registro.Kestrel);

construtor.Services.AddSingleton<IRelogio, RelogioDoSistema>();
construtor.Services.AddSingleton<IGeradorDeId, GeradorDeId>();
construtor.Services.AddSingleton<ModeloDoBanco>();
// Quem entra na Bancada começa como Admin (vê todos os módulos); Bancada:PapelInicial muda isso.
construtor.Services.AddSingleton<IFabricaDeSessao>(servicos => new FabricaDeSessao(servicos.GetRequiredService<ModeloDoBanco>(), construtor.Configuration.GetValue("Bancada:PapelInicial", Bancada.Dominio.Papel.Admin)));
construtor.Services.AddSingleton(new LimitesDeSessao());
construtor.Services.AddSingleton<RepositorioDeSessoes>();
construtor.Services.AddSingleton<ServicoDeAuditoria>();
construtor.Services.AddSingleton(new LimitesDeTaxa());
construtor.Services.AddSingleton<BloqueioDeIp>();
construtor.Services.AddSingleton(new LimitesDoExecutor { Habilitado = construtor.Configuration.GetValue("Bancada:Executor:Habilitado", true) });
construtor.Services.AddSingleton<IProcessos>(_ => new ProcessosDoSistema(Registro.RaizDosTestes(construtor.Configuration["Bancada:Executor:Raiz"], construtor.Environment.ContentRootPath)));
construtor.Services.AddSingleton<ExecutorDeTestes>();
Registro.Casos(construtor.Services);
construtor.Services.AddHttpContextAccessor();
// Acentos do português saem como caracteres, não como entidades; < > & " ' continuam codificados.
construtor.Services.AddWebEncoders(opcoes => opcoes.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.BasicLatin, UnicodeRanges.Latin1Supplement, UnicodeRanges.LatinExtendedA, UnicodeRanges.GeometricShapes, UnicodeRanges.Arrows, UnicodeRanges.GeneralPunctuation));
construtor.Services.AddScoped<ITextos, TextosDaSessao>();
// Padrão seguro: a antifalsificação só funciona em HTTPS. Só para rodar na máquina local, sem certificado: Bancada__CookiesSeguros=false.
var cookiesSeguros = construtor.Configuration.GetValue("Bancada:CookiesSeguros", true);
construtor.Services.AddAntiforgery(opcoes =>
{
    // O prefixo __Host- exige o atributo Secure; sem HTTPS (modo local) o navegador descartaria o cookie.
    opcoes.Cookie.Name = cookiesSeguros ? "__Host-bancada.af" : "bancada.af";
    opcoes.Cookie.HttpOnly = true;
    opcoes.Cookie.SecurePolicy = cookiesSeguros ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    opcoes.Cookie.SameSite = SameSiteMode.Strict;
    opcoes.HeaderName = "X-CSRF";
});
construtor.Services.AddControllersWithViews(opcoes =>
{
    opcoes.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    opcoes.Filters.Add<FiltroDeNaoEncontrado>();
    opcoes.Filters.Add<FiltroDeAntifalsificacao>();
});
construtor.Services.AddRateLimiter(_ => { });
construtor.Services.AddOptions<RateLimiterOptions>().Configure<LimitesDeTaxa>(ConfiguracaoDeTaxa.Aplicar);

var app = construtor.Build();
app.UseMiddleware<CabecalhosDeSeguranca>();
app.UseExceptionHandler("/erro/500");
app.UseStatusCodePagesWithReExecute("/erro/{0}");
app.UseDefaultFiles(); // "/" é o portfólio (wwwroot/index.html); o sistema fica em /bancada
app.UseStaticFiles();
app.UseMiddleware<ProtecaoDeEntrada>();
app.UseRouting();
app.UseRateLimiter();
app.UseWhen(PipelineDaSessao.PrecisaDeSessao, ramo => ramo.UseMiddleware<SessaoMiddleware>());
app.MapGet("/saude", () => Results.Text("ok"));
app.MapControllers();
app.Run();

/// <summary>Marcador para os testes de integração (WebApplicationFactory).</summary>
public partial class Program;
