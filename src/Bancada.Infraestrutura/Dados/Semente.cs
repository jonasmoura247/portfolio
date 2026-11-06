using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Bancada.Aplicacao;
using Bancada.Dominio;

namespace Bancada.Infraestrutura.Dados;

/// <summary>
/// Dados de toda sessão nova (vault: Dados-de-Semente). Sem sorteio: tudo sai do índice, então a mesma versão gera sempre o mesmo banco.
/// Nomes inventados. As datas partem do relógio do laboratório, que é fixo.
/// </summary>
public static class Semente
{
    public const string Prefixo = "claro:";

    private const string MedicoA = "MED-01";
    private const string MedicoB = "MED-02";

    private static readonly string[] PrenomesF = ["Helena", "Marina", "Sofia", "Clara", "Beatriz", "Lívia", "Júlia", "Teresa", "Cecília", "Irene", "Lúcia", "Rita", "Olívia", "Nina", "Vitória", "Elisa", "Alice", "Aurora", "Isis", "Dalva"];
    private static readonly string[] PrenomesM = ["Rafael", "Tiago", "Bruno", "Henrique", "Otávio", "Murilo", "Renato", "Sérgio", "Augusto", "Nelson", "Paulo", "Vítor", "Davi", "Elias", "Ícaro", "Danilo", "Ivan", "Tales", "Lauro", "Ruan"];
    private static readonly string[] Sobrenomes = ["Moreira", "Teixeira", "Barros", "Campos", "Pacheco", "Rezende", "Vieira", "Dantas", "Siqueira", "Macedo", "Azevedo", "Prado", "Queiroz", "Assis", "Tavares", "Bueno", "Coelho", "Fontes", "Guedes", "Lacerda", "Meireles", "Nogueira", "Paiva", "Sampaio"];
    private static readonly (string Substancia, string Reacao)[] AlergiasConhecidas = [("Dipirona", "Urticária"), ("Penicilina", "Edema de face"), ("Iodo", "Prurido"), ("Sulfa", "Erupção cutânea"), ("AAS", "Broncoespasmo leve")];
    private static readonly string[] TextosDeEvolucao =
    [
        "Paciente assintomático. Mantida a conduta atual.",
        "Queixa de cansaço há duas semanas. Solicitada avaliação laboratorial.",
        "Retorno com exames dentro do esperado. Orientado sobre hábitos.",
        "Relata melhora após ajuste da medicação. Reavaliar em 30 dias.",
        "Pressão controlada. Sem novas queixas no período.",
    ];

    private static readonly double[] SerieDoControle = [0.2, -0.5, 0.6, -0.1, 0.4, -0.7, 0.3, -0.2, 0.5, -0.4, 2.1, -0.3, 0.1, -0.6, 0.7, -0.2, 0.3, -0.5, 0.4, -0.1];

    public static void Povoar(BancadaDbContext db)
    {
        var agora = RelogioDeSimulacao.Inicio;
        var pacientes = CriarPacientes();
        db.AddRange(pacientes);
        var grade = CriarGrade(agora);
        db.AddRange(grade);
        db.SaveChanges();

        CriarAlergias(db, pacientes, agora);
        CriarAgenda(db, pacientes, grade, agora);
        var posicoes = CriarSoroteca(db);
        CriarPedidos(db, pacientes, posicoes, agora);
        for (var i = 0; i < SerieDoControle.Length; i++)
        {
            db.Add(Controle(SerieDoControle[i], agora.AddDays(i - SerieDoControle.Length)));
        }

        db.SaveChanges();
    }

    /// <summary>Cenário "CQ fora": duas medidas seguidas acima de 2 desvios violam a regra 2-2s.</summary>
    public static void ReprovarControle(BancadaDbContext db)
    {
        foreach (var z in new[] { 2.3, 2.4 })
        {
            db.Add(Controle(z, RelogioDeSimulacao.Inicio));
        }
    }

    private static Paciente Codigo(List<Paciente> pacientes, int numero) => pacientes[numero - 1];

    private static List<Paciente> CriarPacientes()
    {
        var lista = new List<Paciente>();
        for (var i = 1; i <= 60; i++)
        {
            var feminino = i * 11 % 20 < 11;
            var prenomes = feminino ? PrenomesF : PrenomesM;
            var nome = $"{prenomes[i * 3 % prenomes.Length]} {Sobrenomes[i * 5 % Sobrenomes.Length]} {Sobrenomes[i * 7 % Sobrenomes.Length]}";
            var nascimento = new DateOnly(1941 + (i * 13 % 67), 1 + (i % 12), 1 + (i * 3 % 28));
            lista.Add(new Paciente(string.Create(CultureInfo.InvariantCulture, $"PAC-{i:0000}"), nome, nascimento, feminino ? "F" : "M"));
        }

        return lista;
    }

    private static List<HorarioDaGrade> CriarGrade(DateTime agora)
    {
        var segunda = agora.Date.AddDays(-(((int)agora.DayOfWeek + 6) % 7));
        var lista = new List<HorarioDaGrade>();
        foreach (var medico in new[] { MedicoA, MedicoB })
        {
            foreach (var dia in new[] { 0, 1, 2, 3, 4, 7, 8, 9, 10, 11 })
            {
                lista.AddRange(Enumerable.Range(0, 18).Select(m => new HorarioDaGrade(medico, segunda.AddDays(dia).AddHours(8).AddMinutes(30 * m))));
            }
        }

        return lista;
    }

    private static void CriarAlergias(BancadaDbContext db, List<Paciente> pacientes, DateTime agora)
    {
        db.Add(new Alergia(Codigo(pacientes, 47).Id, "Látex", Prefixo + "Dermatite de contato", agora.AddDays(-90)));
        for (var i = 0; i < pacientes.Count; i += 3)
        {
            var (substancia, reacao) = AlergiasConhecidas[i % AlergiasConhecidas.Length];
            db.Add(new Alergia(pacientes[i].Id, substancia, Prefixo + reacao, agora.AddDays(-(i + 10))));
        }
    }

    private static void CriarAgenda(BancadaDbContext db, List<Paciente> pacientes, List<HorarioDaGrade> grade, DateTime agora)
    {
        Agendamento Marcar(HorarioDaGrade h, Paciente p)
        {
            var a = Agendamento.Criar(p.Id, h, null, h.Inicio.AddDays(-3));
            db.Add(a);
            return a;
        }

        HorarioDaGrade Horario(string medico, int hora, int minuto) => grade.First(h => h.ProfissionalId == medico && h.Inicio == agora.Date.AddHours(hora).AddMinutes(minuto));

        // Cena do dia: o que a fila e a agenda mostram quando a sessão abre.
        var (emAtendimento, chegou, confirmado) = (Horario(MedicoA, 10, 0), Horario(MedicoA, 10, 30), Horario(MedicoA, 11, 0));
        var (tarde1, tarde2, tarde3) = (Horario(MedicoB, 11, 0), Horario(MedicoB, 14, 0), Horario(MedicoB, 15, 0));
        Iniciar(Marcar(emAtendimento, Codigo(pacientes, 47)), emAtendimento);
        Marcar(chegou, Codigo(pacientes, 14)).FazerCheckIn(chegou, agora);
        Marcar(confirmado, Codigo(pacientes, 1)).Confirmar();
        Marcar(tarde1, Codigo(pacientes, 22));
        Marcar(tarde2, Codigo(pacientes, 31)).Confirmar();
        Marcar(tarde3, Codigo(pacientes, 40));
        HorarioDaGrade[] reservados = [emAtendimento, chegou, confirmado, tarde1, tarde2, tarde3];

        var indice = 0;
        foreach (var h in grade.Except(reservados))
        {
            indice++;
            if (indice % 4 != 0)
            {
                continue;
            }

            var paciente = pacientes[indice / 4 * 7 % pacientes.Count];
            var a = Marcar(h, paciente);
            if (h.Inicio >= agora)
            {
                if (indice % 8 == 0)
                {
                    a.Confirmar();
                }
            }
            else if (indice % 20 == 0)
            {
                a.RegistrarFalta();
            }
            else
            {
                Iniciar(a, h);
                db.SaveChanges();
                db.Add(new Evolucao(paciente.Id, a.Id, h.ProfissionalId, Prefixo + TextosDeEvolucao[indice % TextosDeEvolucao.Length], null, h.Inicio.AddMinutes(20)));
                a.Concluir(h, h.ProfissionalId, true, h.Inicio.AddMinutes(25));
            }
        }

        db.SaveChanges();
    }

    private static void Iniciar(Agendamento a, HorarioDaGrade h)
    {
        a.FazerCheckIn(h, h.Inicio);
        a.Iniciar(h, h.ProfissionalId, h.Inicio);
    }

    private static List<PosicaoDaSoroteca> CriarSoroteca(BancadaDbContext db)
    {
        var lista = new List<PosicaoDaSoroteca>();
        foreach (var caixa in new[] { "R1-C1", "R1-C2", "R1-C3", "R1-C4", "R2-C1", "R2-C2", "R2-C3", "R2-C4" })
        {
            for (var linha = 1; linha <= PosicaoDaSoroteca.Lado; linha++)
            {
                lista.AddRange(Enumerable.Range(1, PosicaoDaSoroteca.Lado).Select(coluna => new PosicaoDaSoroteca(caixa, linha, coluna)));
            }
        }

        db.AddRange(lista);
        return lista;
    }

    private static void CriarPedidos(BancadaDbContext db, List<Paciente> pacientes, List<PosicaoDaSoroteca> posicoes, DateTime agora)
    {
        string[][] combinacoes = [["ELE", "GLI", "CRE"], ["GLI", "COL", "TRI"], ["TSH", "T4L"], ["URE", "CRE"], ["FER", "TSH"], ["ELE"]];
        var numero = 0;
        var etiqueta = 0;
        Pedido Novo(Paciente p, IReadOnlyList<string> exames, DateTime quando, bool urgente)
        {
            numero++;
            var pedido = Pedido.Criar(string.Create(CultureInfo.InvariantCulture, $"PED-{numero:000000}"), p.Id, null, MedicoA, urgente, exames, quando);
            db.Add(pedido);
            db.SaveChanges();
            return pedido;
        }

        // Laudos antigos, com a amostra ainda guardada: dão histórico ao delta check, ao prontuário e aos gráficos de tempo.
        for (var i = 0; i < 36; i++)
        {
            var paciente = pacientes[i * 11 % pacientes.Count];
            var quando = agora.AddHours(-20 - (i * 5));
            var pedido = Novo(paciente, combinacoes[i % combinacoes.Length], quando, false);
            var tubo = pedido.Tubos()[0];
            etiqueta++;
            var amostra = Amostra.Coletar(string.Create(CultureInfo.InvariantCulture, $"AMO-{etiqueta:000000}"), pedido, tubo, 5, 12, false, quando.AddMinutes(40));
            db.Add(amostra);
            db.SaveChanges();
            db.Add(new CustodiaDaAmostra(amostra.Id, "Coletada", "COL-01", "Coleta", amostra.ColetadaEm));
            amostra.Armazenar();
            var posicao = posicoes[i * 2];
            posicao.Ocupar(amostra.Id);
            db.Add(new CustodiaDaAmostra(amostra.Id, "Armazenada", "COL-01", posicao.Nome, quando.AddHours(1)));
            var itens = tubo.Exames.SelectMany(e => Catalogo.ExameDe(e).Analitos).Select(codigo =>
            {
                var analito = Catalogo.AnalitoDe(codigo);
                var faixa = analito.FaixaPara(paciente.Sexo);
                var valor = GeradorDeValores.Normal(analito, paciente.Sexo, pedido.Codigo);
                db.Add(Resultado.Historico(pedido.Id, paciente.Id, codigo, valor));
                return new ItemDoLaudo(codigo, valor, analito.Unidade, "N", analito.Casas, faixa.NormalMin, faixa.NormalMax);
            }).ToList();
            Liberar(db, pedido, JsonSerializer.Serialize(itens), quando.AddMinutes(45 + (i % combinacoes.Length * 18) + (i % 5 * 3)));
        }

        // Pedidos à espera de coleta, com materiais e urgência variados.
        for (var i = 0; i < 8; i++)
        {
            Novo(pacientes[20 + (i * 3)], i % 2 == 0 ? ["HEM"] : ["TSH", "HEM"], agora.AddHours(-1 - i), i == 3);
        }

        // PED-000045: a jornada do cenário padrão, com o pedido anterior do mesmo paciente alimentando o delta check.
        var doCenario = Novo(Codigo(pacientes, 14), ["ELE", "GLI", "CRE"], agora.AddMinutes(-30), false);
        var anterior = Novo(Codigo(pacientes, 14), ["ELE", "GLI", "CRE"], agora.AddDays(-30), false);
        foreach (var (codigo, valor) in new[] { ("POT", 4.4), ("SOD", 140.0), ("GLI", 101.0), ("CRE", 1.2) })
        {
            db.Add(Resultado.Historico(anterior.Id, doCenario.PacienteId, codigo, valor));
        }

        Liberar(db, anterior, "[]", agora.AddDays(-30).AddHours(2));
        db.SaveChanges();
    }

    private static void Liberar(BancadaDbContext db, Pedido pedido, string conteudo, DateTime quando)
    {
        db.Add(new Laudo(pedido.Id, 1, conteudo, "ANA-02", quando, null));
        pedido.Liberar(quando);
    }

    private static ControleDeQualidade Controle(double z, DateTime quando) =>
        new(ServicoDeQualidade.Equipamento, ServicoDeQualidade.Analito, ServicoDeQualidade.Media + (z * ServicoDeQualidade.Desvio), ServicoDeQualidade.Media, ServicoDeQualidade.Desvio, quando, null, true);
}
