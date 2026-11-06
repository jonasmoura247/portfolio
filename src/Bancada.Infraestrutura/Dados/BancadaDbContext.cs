using Bancada.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bancada.Infraestrutura.Dados;

/// <summary>Modelo do banco de cada sessão (vault: Modelo-de-Dados). Catálogo de exames e operadores são estáticos, fora do banco.</summary>
public sealed class BancadaDbContext(DbContextOptions<BancadaDbContext> opcoes) : DbContext(opcoes)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<EstadoAgendamento>().HaveConversion<string>();
        configurationBuilder.Properties<EstadoPedido>().HaveConversion<string>();
        configurationBuilder.Properties<EstadoAmostra>().HaveConversion<string>();
        configurationBuilder.Properties<EstadoExecucao>().HaveConversion<string>();
        configurationBuilder.Properties<EstadoResultado>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Paciente>().HasIndex(p => p.Codigo).IsUnique();
        modelBuilder.Entity<Alergia>().HasOne<Paciente>().WithMany().HasForeignKey(a => a.PacienteId);
        modelBuilder.Entity<HorarioDaGrade>().HasIndex(h => new { h.ProfissionalId, h.Inicio }).IsUnique();

        var agendamento = modelBuilder.Entity<Agendamento>();
        agendamento.HasOne<Paciente>().WithMany().HasForeignKey(a => a.PacienteId);
        agendamento.HasOne<HorarioDaGrade>().WithMany().HasForeignKey(a => a.HorarioId);
        // RN-001: um horário tem no máximo uma reserva ativa. Cancelar libera (RN-004).
        agendamento.HasIndex(a => a.HorarioId).IsUnique().HasFilter("\"Estado\" <> 'Cancelado'");

        modelBuilder.Entity<Evolucao>().HasOne<Paciente>().WithMany().HasForeignKey(e => e.PacienteId);
        modelBuilder.Entity<Evolucao>().HasOne<Agendamento>().WithMany().HasForeignKey(e => e.AgendamentoId);
        modelBuilder.Entity<Evolucao>().HasOne<Evolucao>().WithMany().HasForeignKey(e => e.CorrigeId);

        var pedido = modelBuilder.Entity<Pedido>();
        pedido.HasIndex(p => p.Codigo).IsUnique();
        pedido.HasOne<Paciente>().WithMany().HasForeignKey(p => p.PacienteId);
        pedido.HasOne<Agendamento>().WithMany().HasForeignKey(p => p.AgendamentoId);
        pedido.HasMany(p => p.Itens).WithOne().HasForeignKey(i => i.PedidoId);
        pedido.Navigation(p => p.Itens).AutoInclude();

        var amostra = modelBuilder.Entity<Amostra>();
        amostra.HasIndex(a => a.Etiqueta).IsUnique();
        amostra.HasOne<Pedido>().WithMany().HasForeignKey(a => a.PedidoId);
        amostra.HasOne<Paciente>().WithMany().HasForeignKey(a => a.PacienteId);

        modelBuilder.Entity<CustodiaDaAmostra>().HasOne<Amostra>().WithMany().HasForeignKey(c => c.AmostraId);

        var posicao = modelBuilder.Entity<PosicaoDaSoroteca>();
        posicao.HasIndex(p => new { p.Caixa, p.Linha, p.Coluna }).IsUnique();
        posicao.HasOne<Amostra>().WithMany().HasForeignKey(p => p.AmostraId);
        // RN-030: a mesma amostra não ocupa duas posições.
        posicao.HasIndex(p => p.AmostraId).IsUnique().HasFilter("\"AmostraId\" IS NOT NULL");

        var execucao = modelBuilder.Entity<Execucao>();
        execucao.HasOne<Amostra>().WithMany().HasForeignKey(e => e.AmostraId);
        execucao.HasOne<Pedido>().WithMany().HasForeignKey(e => e.PedidoId);

        var resultado = modelBuilder.Entity<Resultado>();
        resultado.HasOne<Pedido>().WithMany().HasForeignKey(r => r.PedidoId);
        resultado.HasOne<Paciente>().WithMany().HasForeignKey(r => r.PacienteId);
        resultado.HasOne<Execucao>().WithMany().HasForeignKey(r => r.ExecucaoId);

        var laudo = modelBuilder.Entity<Laudo>();
        laudo.HasOne<Pedido>().WithMany().HasForeignKey(l => l.PedidoId);
        laudo.HasIndex(l => new { l.PedidoId, l.Versao }).IsUnique();

        modelBuilder.Entity<ControleDeQualidade>().HasIndex(c => new { c.Equipamento, c.Analito, c.MedidoEm });
        modelBuilder.Entity<MensagemDeProtocolo>();

        var evento = modelBuilder.Entity<EventoDeAuditoria>();
        evento.ToTable("Auditoria");
        evento.HasKey(e => e.Seq);
        evento.Property(e => e.Seq).ValueGeneratedNever();
    }
}

/// <summary>Usada só pelo "dotnet ef" para gerar as migrations.</summary>
public sealed class FabricaEmDesign : IDesignTimeDbContextFactory<BancadaDbContext>
{
    public BancadaDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<BancadaDbContext>().UseSqlite("Data Source=:memory:").Options);
}
