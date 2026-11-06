---
tipo: seguranca
projeto: Bancada
atualizado: 2026-11-01
---

# Auditoria encadeada

Registro de eventos em que cada linha carrega o hash da anterior. Mexer numa linha antiga quebra todas as seguintes e a verificação acusa. É o controle de **integridade** e **não repúdio** do Bancada (Security+ domínio 1).

## Formato

Cada evento ([[Catalogo-de-Eventos]]) tem:

```
canonico = seq | quandoIso | papel | acao | alvoTipo | alvoId | detalheJson | resultado
hash     = SHA-256( hashAnterior ‖ "\n" ‖ canonico )     // em hexadecimal minúsculo
```

- `quandoIso`: UTC, milissegundos, formato fixo `yyyy-MM-ddTHH:mm:ss.fffZ`.
- `detalheJson`: chaves ordenadas, sem espaços, números sem notação científica (forma canônica).
- Campos separados por `|`; qualquer `|` ou `\` dentro de um campo é escapado com `\`.
- O primeiro evento usa `hashAnterior = SHA-256("BANCADA-GENESIS")`.

SHA-256 vem de `System.Security.Cryptography` (plataforma, MIT).

## Garantias e limites

| Garante | Não garante |
|---|---|
| Detecta alteração, remoção ou troca de ordem de qualquer evento antigo | Quem tem controle total do servidor pode recalcular a cadeia inteira (para isso seria preciso ancorar o hash final fora do sistema) |
| Detecta inserção no meio | — |

Por ser demonstração, o limite é documentado na própria tela: "a cadeia prova consistência interna, não resiste a quem controla o servidor". Dizer isso com clareza vale mais que prometer demais.

## Imutabilidade no banco

Gatilhos `BEFORE UPDATE` e `BEFORE DELETE` na tabela `auditoria` abortam com mensagem fixa ([[Modelo-de-Dados]]). Teste tenta `UPDATE` e `DELETE` e espera falha.

## Verificação

`VerificarCadeia` percorre a tabela em ordem e recalcula; devolve `Valida` ou o `seq` do primeiro elo quebrado. Linear, rápido para até 5.000 eventos.

## Simular adulteração

Botão da tela "Auditoria". Como o banco recusa alteração, a simulação age numa **cópia em memória da lista exibida**: altera um campo de um evento antigo, roda a verificação sobre a cópia e mostra "quebrou no evento 37". Nunca altera o banco real, e a tela diz isso. Registra `AdulteracaoSimulada`.

## Testes

- Cadeia de 1, 2 e 1.000 eventos válida.
- Alterar cada campo, em eventos do início, meio e fim, faz a verificação falhar **no elo certo**.
- Remover um evento do meio; trocar dois de lugar.
- Forma canônica estável (mesmos dados → mesmo hash, em qualquer cultura do sistema: teste roda com cultura `pt-BR` e `en-US`).
- Campo com `|` e `\` não gera colisão.
- `UPDATE`/`DELETE` no banco falham.

## Relacionadas

[[Catalogo-de-Eventos]] · [[Controles-e-SecPlus]] · [[Regras-de-Negocio]]
