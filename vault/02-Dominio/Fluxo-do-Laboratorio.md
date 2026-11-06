---
tipo: dominio
projeto: Bancada
atualizado: 2026-11-01
---

# Fluxo do laboratório

Cada entidade tem uma **máquina de estados** no domínio. Transição inválida lança exceção de domínio. Há um teste que percorre todos os pares (estado, evento) e confere permitidos e proibidos.

## Agendamento

```
Agendado → Confirmado → ChegouCheckIn → EmAtendimento → Concluido
   │            │              │
   └────────────┴──────────────┴──→ Cancelado
Agendado/Confirmado ──(horário passou)──→ Faltou
```

## Pedido de exame

```
Solicitado → Coletado → Recebido → EmAnalise → ResultadoPreliminar → Validado → Liberado
     │           │          │           │
     │           │          └──→ Rejeitado ──→ (gera novo pedido de recoleta)
     └───────────┴──────────────→ Cancelado
Liberado ──(correção)──→ Retificado (nova versão; a anterior fica imutável)
```

## Amostra

```
Etiquetada → Coletada → Recebida → Armazenada → EmAnalise → Armazenada → Descartada
                │           │
                └───────────┴──→ Rejeitada (hemólise, volume, material errado)
```

A amostra volta à soroteca depois da análise e fica até vencer a validade. Descarte é registrado com quem, quando e motivo.

## Execução no equipamento

```
NaFila → Enviado → Processando → Concluido
                       │
                       └──→ Erro ──→ NaFila (reprocessar, até 2 vezes)
```

## Regras de negócio (cada uma vira teste)

| Regra | Onde vive |
|---|---|
| Não agendar dois pacientes no mesmo horário do mesmo profissional | Agenda |
| Não agendar no passado nem fora da grade | Agenda |
| Check-in só no dia do agendamento | Agendamento |
| Evolução do prontuário não pode ser apagada; correção cria nova evolução que referencia a anterior | Prontuário |
| Alergia registrada aparece como alerta em qualquer pedido | Pedido |
| Exame exige material e volume mínimo definidos no catálogo | Pedido/Coleta |
| Exame com jejum só é coletado se o jejum foi confirmado | Coleta |
| Posição da soroteca guarda no máximo uma amostra | Soroteca (UNIQUE no banco e no domínio) |
| Amostra vencida não vai para análise e é marcada para descarte | Soroteca |
| Equipamento só aceita exames que ele sabe fazer | Equipamento |
| Resultado fora da faixa de referência recebe flag H ou L; fora da faixa crítica recebe flag C e exige confirmação | Resultado |
| Valor de referência depende de sexo e idade | Resultado |
| Controle de qualidade reprovado bloqueia liberação dos resultados do equipamento | Qualidade |
| Quem executou não valida o mesmo resultado | Liberação |
| Laudo liberado é imutável; retificação gera versão nova | Laudo |

## Linha do tempo do paciente

Toda transição grava um evento na auditoria ([[Controles-e-SecPlus]]). A tela do paciente lê esses eventos e mostra o caminho completo, incluindo a **cadeia de custódia** de cada amostra (quem pegou, quando, onde guardou).

## Relacionadas

[[Modelo-de-Dados]] · [[Modulos]] · [[Estrategia-de-Testes]]
