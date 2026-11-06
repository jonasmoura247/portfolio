---
tipo: arquitetura
projeto: Bancada
atualizado: 2026-11-01
---

# Camada de aplicação

Um **caso de uso por classe**, com um método `Executar`. Recebe um comando (objeto simples), devolve um resultado. Orquestra domínio e portas, checa regra de papel quando necessário e **grava auditoria**.

## Portas (interfaces definidas aqui, implementadas na Infraestrutura)

| Porta | Responsabilidade |
|---|---|
| `IRelogio` | Hora atual (real ou de simulação) |
| `ISorteador` | Aleatório com semente (falha de equipamento, geração de dados) |
| `ICifra` | Cifrar e decifrar texto com chave da sessão |
| `IAuditoria` | Gravar evento na cadeia; verificar cadeia |
| `IPacientes` | Buscar, obter, listar com paginação |
| `IAgenda` | Grade da semana, reserva, cancelamento |
| `IAtendimentos` | Fila, check-in, estado |
| `IProntuario` | Evoluções (com correção encadeada), alergias |
| `IPedidos` | Criar, itens, estado |
| `IAmostras` | Coleta, rejeição, etiqueta |
| `ISoroteca` | Posições, armazenar, mover, descartar, validade |
| `IEquipamentos` | Fila de execução, estado, reprocesso |
| `IResultados` | Resultados, validação, laudos e versões |
| `IQualidade` | Medições de controle |
| `ISessoes` | Papel atual, cenário, cotas |
| `IExecutorDeTestes` | Rodar testes de C# e JS, transmitir saída |

Portas **pequenas e por assunto** (sem `IRepository<T>` genérico, [[Decisoes-ADR]] ADR-003). Se um caso de uso precisa de duas coisas de um mesmo agregado, a porta tem dois métodos; nunca expõe `IQueryable`.

## Casos de uso

| Módulo | Casos de uso |
|---|---|
| Sessão | `TrocarPapel`, `CarregarCenario`, `ReiniciarSessao` |
| Pacientes | `ListarPacientes`, `VerPaciente` |
| Agenda | `VerAgenda`, `Agendar`, `Reagendar`, `CancelarAgendamento`, `ConfirmarAgendamento`, `MarcarFaltas` (varredura periódica) |
| Atendimento | `FazerCheckIn`, `IniciarAtendimento`, `ConcluirAtendimento`, `VerFila` |
| Prontuário | `VerEvolucoes`, `RegistrarEvolucao`, `CorrigirEvolucao`, `EditarAlergias` |
| Pedido | `CriarPedido` (agrupa exames em tubos, alerta alergia), `CancelarPedido`, `VerPedido` |
| Coleta | `ColetarAmostra`, `RejeitarAmostra`, `GerarEtiqueta` |
| Soroteca | `ArmazenarAmostra`, `MoverAmostra`, `DescartarAmostra`, `VarrerVencimentos` |
| Equipamento | `EnviarAoEquipamento`, `AvancarSimulacao` (processa quem terminou no relógio), `ReprocessarExecucao`, `ControlarRelogio` |
| Qualidade | `RegistrarControle`, `AvaliarWestgard` |
| Resultado | `ReceberResultado`, `ValidarResultado`, `ConfirmarCritico`, `LiberarLaudo`, `RetificarLaudo` |
| Auditoria | `ConsultarAuditoria`, `VerificarCadeia`, `SimularAdulteracao` (sobre a cópia exibida) |
| Painel | `ObterInstantaneo`, `ExecutarTestes`, `ListarLicencas`, `ListarArquitetura`, `ListarEsquema` |
| Laboratório de ataque | `DispararPayload` (passa pelas mesmas defesas e registra) |

## Simulação sem processo em segundo plano

Não há serviço rodando por trás. O "tempo do equipamento" avança **quando há requisição**: `AvancarSimulacao` compara o relógio simulado com o horário previsto de cada execução e conclui as que venceram. A tela de equipamento consulta periodicamente (e o fluxo de eventos chama o mesmo caso de uso). Vantagens: determinístico, testável com `IRelogio` manual, sem threads soltas.

## Padrão de um caso de uso

```csharp
public sealed class ColetarAmostra(IPedidos pedidos, IAmostras amostras, IRelogio relogio, IAuditoria auditoria)
{
    public AmostraColetada Executar(ColetarAmostraComando c)
    {
        var pedido = pedidos.Obter(c.PedidoId) ?? throw new NaoEncontrado(nameof(Pedido), c.PedidoId);
        var amostra = pedido.Coletar(c.TuboId, c.JejumConfirmado, relogio.Agora);
        amostras.Salvar(amostra);
        auditoria.Registrar(Acao.ColetarAmostra, amostra.Etiqueta, c.Papel);
        return new AmostraColetada(amostra.Id, amostra.Etiqueta);
    }
}
```

## Testes

Cada caso de uso é testado com **dublês escritos à mão** das portas (ex.: `PedidosEmMemoria`), cobrindo caminho feliz, cada exceção e cada ramo. Os contratos das portas são testados também contra a implementação real em SQLite ([[Estrategia-de-Testes]]), para o dublê não mentir.

## Relacionadas

[[Rotas-e-Controllers]] · [[Regras-de-Negocio]] · [[Arquitetura-MVC]]
