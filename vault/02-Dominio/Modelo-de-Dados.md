---
tipo: dominio
projeto: Bancada
atualizado: 2026-11-01
---

# Modelo de dados

Modelado com EF Core code-first; o esquema chega ao banco por migrations ([[Decisoes-ADR]] ADR-003). Dialeto SQLite. Nomes em português, no singular. Os blocos SQL abaixo mostram o **resultado esperado** de cada migration (e o que vai em `migrationBuilder.Sql`).

> Todas as colunas, tipos e observações estão no [[Dicionario-de-Dados]]. Os campos de cada tela estão em [[Especificacao-das-Telas]].

## Tabelas

| Tabela | Papel | Chaves e restrições principais |
|---|---|---|
| `paciente` | Paciente fictício | PK `id`; `codigo` UNIQUE (`PAC-0001`); `sexo` CHECK; `nascimento` |
| `alergia` | Alergias do paciente | FK `paciente_id` |
| `profissional` | Médicos e analistas fictícios | PK; `papel` CHECK |
| `agenda_grade` | Horários oferecidos por profissional | FK profissional; UNIQUE (profissional, inicio) |
| `agendamento` | Reserva de um horário | FK paciente, grade; UNIQUE (grade_id) para impedir dupla reserva; `estado` CHECK |
| `atendimento` | Consulta em andamento ou concluída | FK agendamento (1:1) |
| `evolucao` | Registro no prontuário | FK atendimento; `corrige_id` FK opcional; texto cifrado |
| `exame_catalogo` | Exames que o laboratório faz | `codigo` UNIQUE; material; volume mínimo; jejum |
| `analito` | O que se mede dentro de um exame (ver [[Catalogo-de-Exames]]) | FK exame; `codigo` UNIQUE; unidade |
| `faixa_referencia` | Referência por sexo e idade | FK analito; faixa normal e crítica (limites anuláveis para faixa aberta) |
| `pedido` | Pedido do médico | FK atendimento; `estado` CHECK |
| `pedido_item` | Um exame dentro do pedido | FK pedido, exame; UNIQUE (pedido, exame) |
| `amostra` | Tubo coletado | `etiqueta` UNIQUE; FK pedido; `estado`; `validade` |
| `soroteca_rack` / `soroteca_caixa` | Estrutura física | FK em cascata lógica |
| `soroteca_posicao` | Posição na caixa | UNIQUE (caixa, linha, coluna); `amostra_id` UNIQUE e nulo quando livre |
| `equipamento` | Analisador simulado | `codigo` UNIQUE; protocolo |
| `equipamento_exame` | O que cada equipamento processa | PK composta |
| `execucao` | Item enviado ao equipamento | FK pedido_item, equipamento; `estado`; tentativas |
| `resultado` | Valor medido de um analito | FK execucao, analito; `valor`; `flag`; estado; quem executou e quem validou |
| `amostra_custodia` | Cadeia de custódia da amostra | FK amostra; evento; papel; local; quando |
| `laudo` | Documento liberado | FK pedido; `versao`; imutável |
| `controle_qualidade` | Medições de controle | FK equipamento, exame; valor; regra violada |
| `auditoria` | Eventos | `hash_anterior`, `hash`; sem UPDATE nem DELETE |

## Trechos

```sql
-- migration: agendamento
CREATE TABLE agendamento (
  id          INTEGER PRIMARY KEY,
  paciente_id INTEGER NOT NULL REFERENCES paciente(id),
  grade_id    INTEGER NOT NULL UNIQUE REFERENCES agenda_grade(id),
  estado      TEXT    NOT NULL CHECK (estado IN
    ('Agendado','Confirmado','ChegouCheckIn','EmAtendimento','Concluido','Cancelado','Faltou')),
  criado_em   TEXT    NOT NULL
);

-- migration: soroteca
CREATE TABLE soroteca_posicao (
  id         INTEGER PRIMARY KEY,
  caixa_id   INTEGER NOT NULL REFERENCES soroteca_caixa(id),
  linha      INTEGER NOT NULL CHECK (linha BETWEEN 1 AND 9),
  coluna     INTEGER NOT NULL CHECK (coluna BETWEEN 1 AND 9),
  amostra_id INTEGER UNIQUE REFERENCES amostra(id),
  UNIQUE (caixa_id, linha, coluna)
);

-- migration: auditoria (imutável por gatilho, via migrationBuilder.Sql)
CREATE TRIGGER auditoria_sem_update BEFORE UPDATE ON auditoria
BEGIN SELECT RAISE(ABORT, 'auditoria é somente de inclusão'); END;
CREATE TRIGGER auditoria_sem_delete BEFORE DELETE ON auditoria
BEGIN SELECT RAISE(ABORT, 'auditoria é somente de inclusão'); END;
```

## Regras de acesso a dados

- **Sempre parametrizado.** LINQ e `FromSql` interpolado geram parâmetros. Teste de arquitetura falha se achar `FromSqlRaw`/`ExecuteSqlRaw` com string montada.
- Foreign keys ligadas em toda conexão (`PRAGMA foreign_keys = ON`), com teste.
- Cada `CHECK`, `UNIQUE` e `FK` tem um teste que tenta violar e espera falha.
- Texto livre do prontuário é cifrado antes de gravar ([[Controles-e-SecPlus]]).
- Sem CPF, telefone, endereço ou e-mail. Identificação por `codigo` fictício ([[Dados-Ficticios-e-LGPD]]).

## Um banco por visitante

Existe um banco-modelo já migrado e populado (semente fixa). Cada sessão recebe uma **cópia em memória** desse modelo (API de backup do SQLite). Nada persiste depois que a sessão expira, e um visitante nunca enxerga dados de outro: o isolamento é estrutural, não depende de filtro por `WHERE sessao_id`.

## Relacionadas

[[Fluxo-do-Laboratorio]] · [[Arquitetura-MVC]] · [[Estrategia-de-Testes]]
