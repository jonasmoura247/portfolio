---
tipo: visao
projeto: Bancada
atualizado: 2026-11-01
---

# Personas e jornada

## Quem usa o site

| Persona | Quer | Precisa ver |
|---|---|---|
| Recrutador | Entender rápido o que o Jonas sabe fazer | Jornada guiada, resultado final, selos de teste/cobertura |
| Tech lead | Julgar qualidade de código e decisão | Painel técnico, ADRs, testes rodando, camadas reais |
| Dev curioso / "pentester" | Tentar quebrar | Que não quebra, e o log registrando a tentativa |

O terceiro caso é esperado. O site é público e vai ser testado. Ver [[Modelo-de-Ameacas]].

## Papéis dentro do sistema (simulados)

Sem login. O visitante troca de papel num seletor no topo. O servidor guarda o papel na sessão e **toda action checa a política**. Serve para demonstrar autorização de verdade sem credencial.

| Papel | Agenda | Atendimento | Prontuário | Pedido | Coleta/Soroteca | Equipamento | Validar/Liberar | Painel técnico |
|---|---|---|---|---|---|---|---|---|
| Recepção | criar, editar | check-in | só dados cadastrais | ver | — | — | — | ver |
| Médico(a) | ver | atender | ler e escrever | criar | — | — | ver laudo | ver |
| Coleta | — | — | só alergias e jejum | ver | coletar, armazenar | — | — | ver |
| Analista | — | — | — | ver | receber, descartar | operar | validar e liberar | ver |
| Admin | tudo | tudo | ler | tudo | tudo | tudo | liberar, sem executar+validar o mesmo exame | executar testes |

> A tabela mostra as permissões de negócio. O detalhe completo, ação por ação, está na [[Matriz-de-Autorizacao]]. Painel técnico, executor de testes e laboratório de ataque ficam abertos a **todos** os papéis (qualquer visitante pode trocar de papel); a proteção deles são os limites de taxa, o comando fixo e o kill switch, não o papel.

**Operador:** dentro de cada papel o visitante escolhe quem está "logado" (por exemplo `ANA-01` ou `ANA-02`), sem senha. Cada ação é gravada com papel e operador ([[Sessao-e-Isolamento]]).

**Separação de deveres:** quem executou o exame no equipamento não valida o mesmo resultado. Regra de domínio, com teste.

## Jornada guiada (5 minutos)

Botão "Fazer o tour" abre uma barra lateral com o roteiro. Cada passo leva à tela certa e marca como feito.

1. Escolha uma paciente (Recepção) e veja os dados.
2. Agende um horário na agenda.
3. Faça o check-in e inicie o atendimento (Médico).
4. Leia o prontuário; registre uma evolução; peça um exame.
5. Colete a amostra, imprima a etiqueta, guarde na soroteca (Coleta).
6. Envie ao equipamento; acelere o relógio e veja processar (Analista).
7. Valide e libere; abra o laudo.
8. Abra o painel técnico; rode os testes.
9. (Opcional) Abra o "Laboratório de ataque" e tente quebrar o sistema.

## Cenários prontos

Botão "Carregar cenário" no tour, cada um monta o estado certo na sessão:

- **Fluxo feliz** — paciente saudável, tudo normal.
- **Valor crítico** — potássio alto, exige confirmação e notificação.
- **Amostra hemolisada** — rejeitada na triagem, pede recoleta.
- **Equipamento com erro** — falha no meio, reprocessamento.
- **Controle de qualidade fora** — regra de Westgard violada, bloqueia liberação.

## Relacionadas

[[Modulos]] · [[Fluxo-do-Laboratorio]] · [[Controles-e-SecPlus]]
