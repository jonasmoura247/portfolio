using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bancada.Infraestrutura.Dados.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Auditoria",
                columns: table => new
                {
                    Seq = table.Column<int>(type: "INTEGER", nullable: false),
                    Quando = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Papel = table.Column<string>(type: "TEXT", nullable: false),
                    Operador = table.Column<string>(type: "TEXT", nullable: false),
                    Acao = table.Column<string>(type: "TEXT", nullable: false),
                    AlvoTipo = table.Column<string>(type: "TEXT", nullable: false),
                    AlvoId = table.Column<string>(type: "TEXT", nullable: false),
                    DetalheJson = table.Column<string>(type: "TEXT", nullable: false),
                    Resultado = table.Column<string>(type: "TEXT", nullable: false),
                    HashAnterior = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditoria", x => x.Seq);
                });

            migrationBuilder.CreateTable(
                name: "ControleDeQualidade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Equipamento = table.Column<string>(type: "TEXT", nullable: false),
                    Analito = table.Column<string>(type: "TEXT", nullable: false),
                    Valor = table.Column<double>(type: "REAL", nullable: false),
                    Media = table.Column<double>(type: "REAL", nullable: false),
                    Desvio = table.Column<double>(type: "REAL", nullable: false),
                    MedidoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Regra = table.Column<string>(type: "TEXT", nullable: true),
                    Aprovado = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControleDeQualidade", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HorarioDaGrade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfissionalId = table.Column<string>(type: "TEXT", nullable: false),
                    Inicio = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorarioDaGrade", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MensagemDeProtocolo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Equipamento = table.Column<string>(type: "TEXT", nullable: false),
                    DoEquipamento = table.Column<bool>(type: "INTEGER", nullable: false),
                    Quando = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Texto = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensagemDeProtocolo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Paciente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Nascimento = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Sexo = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paciente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Agendamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PacienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    HorarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    MotivoCifrado = table.Column<string>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    ChegadaEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IniciadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConcluidoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agendamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agendamento_HorarioDaGrade_HorarioId",
                        column: x => x.HorarioId,
                        principalTable: "HorarioDaGrade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Agendamento_Paciente_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Paciente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alergia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PacienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Substancia = table.Column<string>(type: "TEXT", nullable: false),
                    ReacaoCifrada = table.Column<string>(type: "TEXT", nullable: true),
                    RegistradaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alergia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alergia_Paciente_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Paciente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Evolucao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PacienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    AgendamentoId = table.Column<int>(type: "INTEGER", nullable: true),
                    AutorId = table.Column<string>(type: "TEXT", nullable: false),
                    TextoCifrado = table.Column<string>(type: "TEXT", nullable: false),
                    CorrigeId = table.Column<int>(type: "INTEGER", nullable: true),
                    CriadaEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evolucao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Evolucao_Agendamento_AgendamentoId",
                        column: x => x.AgendamentoId,
                        principalTable: "Agendamento",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Evolucao_Evolucao_CorrigeId",
                        column: x => x.CorrigeId,
                        principalTable: "Evolucao",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Evolucao_Paciente_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Paciente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pedido",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", nullable: false),
                    PacienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    AgendamentoId = table.Column<int>(type: "INTEGER", nullable: true),
                    SolicitanteId = table.Column<string>(type: "TEXT", nullable: false),
                    Urgente = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LiberadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Estado = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pedido_Agendamento_AgendamentoId",
                        column: x => x.AgendamentoId,
                        principalTable: "Agendamento",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Pedido_Paciente_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Paciente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Amostra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Etiqueta = table.Column<string>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<int>(type: "INTEGER", nullable: false),
                    PacienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Material = table.Column<string>(type: "TEXT", nullable: false),
                    Exames = table.Column<string>(type: "TEXT", nullable: false),
                    VolumeMl = table.Column<double>(type: "REAL", nullable: false),
                    JejumHoras = table.Column<int>(type: "INTEGER", nullable: false),
                    Hemolisada = table.Column<bool>(type: "INTEGER", nullable: false),
                    ColetadaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidadeAte = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    MotivoRejeicao = table.Column<string>(type: "TEXT", nullable: true),
                    MotivoDescarteCifrado = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amostra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Amostra_Paciente_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Paciente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Amostra_Pedido_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Laudo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PedidoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Versao = table.Column<int>(type: "INTEGER", nullable: false),
                    ConteudoJson = table.Column<string>(type: "TEXT", nullable: false),
                    LiberadoPorId = table.Column<string>(type: "TEXT", nullable: false),
                    LiberadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MotivoCifrado = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Laudo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Laudo_Pedido_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PedidoItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PedidoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Exame = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoItem_Pedido_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustodiaDaAmostra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AmostraId = table.Column<int>(type: "INTEGER", nullable: false),
                    Evento = table.Column<string>(type: "TEXT", nullable: false),
                    OperadorId = table.Column<string>(type: "TEXT", nullable: false),
                    Local = table.Column<string>(type: "TEXT", nullable: false),
                    Quando = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodiaDaAmostra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustodiaDaAmostra_Amostra_AmostraId",
                        column: x => x.AmostraId,
                        principalTable: "Amostra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Execucao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AmostraId = table.Column<int>(type: "INTEGER", nullable: false),
                    PedidoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Exame = table.Column<string>(type: "TEXT", nullable: false),
                    Equipamento = table.Column<string>(type: "TEXT", nullable: false),
                    Prioridade = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalMin = table.Column<double>(type: "REAL", nullable: false),
                    RestanteMin = table.Column<double>(type: "REAL", nullable: false),
                    FalhaForcada = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnviadaPorId = table.Column<string>(type: "TEXT", nullable: false),
                    EnviadaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Tentativas = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Execucao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Execucao_Amostra_AmostraId",
                        column: x => x.AmostraId,
                        principalTable: "Amostra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Execucao_Pedido_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PosicaoDaSoroteca",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Caixa = table.Column<string>(type: "TEXT", nullable: false),
                    Linha = table.Column<int>(type: "INTEGER", nullable: false),
                    Coluna = table.Column<int>(type: "INTEGER", nullable: false),
                    AmostraId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosicaoDaSoroteca", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosicaoDaSoroteca_Amostra_AmostraId",
                        column: x => x.AmostraId,
                        principalTable: "Amostra",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Resultado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExecucaoId = table.Column<int>(type: "INTEGER", nullable: true),
                    PedidoId = table.Column<int>(type: "INTEGER", nullable: false),
                    PacienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Analito = table.Column<string>(type: "TEXT", nullable: false),
                    Valor = table.Column<double>(type: "REAL", nullable: false),
                    Flag = table.Column<string>(type: "TEXT", nullable: false),
                    Anterior = table.Column<double>(type: "REAL", nullable: true),
                    DeltaSuspeito = table.Column<bool>(type: "INTEGER", nullable: false),
                    Equipamento = table.Column<string>(type: "TEXT", nullable: false),
                    EnviadoPorId = table.Column<string>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    ValidadoPorId = table.Column<string>(type: "TEXT", nullable: true),
                    JustificativaCifrada = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resultado", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resultado_Execucao_ExecucaoId",
                        column: x => x.ExecucaoId,
                        principalTable: "Execucao",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resultado_Paciente_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Paciente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Resultado_Pedido_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agendamento_HorarioId",
                table: "Agendamento",
                column: "HorarioId",
                unique: true,
                filter: "\"Estado\" <> 'Cancelado'");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamento_PacienteId",
                table: "Agendamento",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Alergia_PacienteId",
                table: "Alergia",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Amostra_Etiqueta",
                table: "Amostra",
                column: "Etiqueta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Amostra_PacienteId",
                table: "Amostra",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Amostra_PedidoId",
                table: "Amostra",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_ControleDeQualidade_Equipamento_Analito_MedidoEm",
                table: "ControleDeQualidade",
                columns: new[] { "Equipamento", "Analito", "MedidoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_CustodiaDaAmostra_AmostraId",
                table: "CustodiaDaAmostra",
                column: "AmostraId");

            migrationBuilder.CreateIndex(
                name: "IX_Evolucao_AgendamentoId",
                table: "Evolucao",
                column: "AgendamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Evolucao_CorrigeId",
                table: "Evolucao",
                column: "CorrigeId");

            migrationBuilder.CreateIndex(
                name: "IX_Evolucao_PacienteId",
                table: "Evolucao",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Execucao_AmostraId",
                table: "Execucao",
                column: "AmostraId");

            migrationBuilder.CreateIndex(
                name: "IX_Execucao_PedidoId",
                table: "Execucao",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_HorarioDaGrade_ProfissionalId_Inicio",
                table: "HorarioDaGrade",
                columns: new[] { "ProfissionalId", "Inicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Laudo_PedidoId_Versao",
                table: "Laudo",
                columns: new[] { "PedidoId", "Versao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Paciente_Codigo",
                table: "Paciente",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_AgendamentoId",
                table: "Pedido",
                column: "AgendamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_Codigo",
                table: "Pedido",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_PacienteId",
                table: "Pedido",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoItem_PedidoId",
                table: "PedidoItem",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_PosicaoDaSoroteca_AmostraId",
                table: "PosicaoDaSoroteca",
                column: "AmostraId",
                unique: true,
                filter: "\"AmostraId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PosicaoDaSoroteca_Caixa_Linha_Coluna",
                table: "PosicaoDaSoroteca",
                columns: new[] { "Caixa", "Linha", "Coluna" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resultado_ExecucaoId",
                table: "Resultado",
                column: "ExecucaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Resultado_PacienteId",
                table: "Resultado",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Resultado_PedidoId",
                table: "Resultado",
                column: "PedidoId");

            // Registros imutáveis (RN-010, RN-035, RN-056, auditoria). A semente entra em claro e é cifrada uma vez, na cópia da sessão.
            migrationBuilder.Sql("""
                CREATE TRIGGER evolucao_sem_edicao BEFORE UPDATE ON Evolucao WHEN OLD.TextoCifrado NOT LIKE 'claro:%'
                BEGIN SELECT RAISE(ABORT, 'evolucao imutavel'); END;
                """);
            foreach (var tabela in new[] { "Evolucao", "Laudo", "CustodiaDaAmostra", "Auditoria" })
            {
                migrationBuilder.Sql($"CREATE TRIGGER {tabela}_sem_apagar BEFORE DELETE ON {tabela} BEGIN SELECT RAISE(ABORT, 'registro imutavel'); END;");
            }

            foreach (var tabela in new[] { "Laudo", "CustodiaDaAmostra", "Auditoria" })
            {
                migrationBuilder.Sql($"CREATE TRIGGER {tabela}_sem_editar BEFORE UPDATE ON {tabela} BEGIN SELECT RAISE(ABORT, 'registro imutavel'); END;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alergia");

            migrationBuilder.DropTable(
                name: "Auditoria");

            migrationBuilder.DropTable(
                name: "ControleDeQualidade");

            migrationBuilder.DropTable(
                name: "CustodiaDaAmostra");

            migrationBuilder.DropTable(
                name: "Evolucao");

            migrationBuilder.DropTable(
                name: "Laudo");

            migrationBuilder.DropTable(
                name: "MensagemDeProtocolo");

            migrationBuilder.DropTable(
                name: "PedidoItem");

            migrationBuilder.DropTable(
                name: "PosicaoDaSoroteca");

            migrationBuilder.DropTable(
                name: "Resultado");

            migrationBuilder.DropTable(
                name: "Execucao");

            migrationBuilder.DropTable(
                name: "Amostra");

            migrationBuilder.DropTable(
                name: "Pedido");

            migrationBuilder.DropTable(
                name: "Agendamento");

            migrationBuilder.DropTable(
                name: "HorarioDaGrade");

            migrationBuilder.DropTable(
                name: "Paciente");
        }
    }
}
