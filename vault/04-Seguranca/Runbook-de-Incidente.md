---
tipo: seguranca
projeto: Bancada
atualizado: 2026-11-01
---

# Runbook de incidente

Segue o ciclo do Security+ (domínio 4): **preparação → identificação → contenção → erradicação → recuperação → lições aprendidas**. Escrito para ser seguido com pressa.

## Preparação (antes de qualquer problema)

- Kill switches prontos e testados: `Bancada:Executor:Habilitado`, `Bancada:LabAtaque:Habilitado`.
- Limites de taxa e de sessão em configuração (não no código).
- Log estruturado com `sessaoId` truncado, `acao`, `resultado`, código de evento.
- Imagem anterior marcada para reimplantação rápida.
- Quem decide: o Jonas. Onde olhar: tela "Operação" do [[Painel-Tecnico]] e o log do provedor.

## Identificação (como saber que algo está errado)

| Sinal | Onde aparece | Suspeita |
|---|---|---|
| Muitos `LimiteDeTaxaExcedido` ou `IpBloqueado` | Tela de eventos, métricas | Varredura ou abuso |
| Sessões ativas no teto o tempo todo | Métrica `sessoes_ativas` | Inundação de sessões |
| `TestesIniciados` seguido de timeout repetido | Eventos do painel | Abuso do executor ou defeito |
| Pico de CPU/memória sem pico de uso legítimo | Métricas do provedor | DoS ou laço |
| `PayloadBloqueado` com payload **diferente** do corpus | Eventos | Ataque novo: investigar |
| Qualquer 500 com `traceId` | Log | Defeito ou entrada não prevista |
| Cadeia de auditoria inválida **no banco real** | Verificação | Defeito grave (só deve falhar na simulação) |

## Contenção (parar o dano, nesta ordem)

1. Se envolve o executor: **desligar** `Executor:Habilitado`. O painel passa a mostrar o instantâneo do build; nada mais quebra.
2. Se envolve o laboratório de ataque: desligar `LabAtaque:Habilitado`.
3. Reduzir `Sessao:Maximo` e os limites de taxa.
4. Se a origem é um IP ou faixa: bloquear no provedor/firewall da hospedagem.
5. Se nada disso bastar: tirar o app do ar (página estática de manutenção) e seguir para a erradicação. O site é demonstração; **disponibilidade nunca vale mais que segurança** aqui.

## Erradicação

1. Achar a causa: reproduzir com o evento/`traceId`, olhar o payload, ler o código do caminho.
2. Escrever **primeiro um teste que falha** reproduzindo o problema (e acrescentar o payload ao [[Corpus-de-Ataque]], se for entrada).
3. Corrigir no ponto que serve a todos os chamadores (não só no caminho reportado).
4. Rodar a suíte inteira com 100% de cobertura.

## Recuperação

1. Publicar a imagem corrigida.
2. Religar os kill switches em ordem inversa à do desligamento, observando métricas por alguns minutos.
3. Confirmar que o instantâneo do painel e o `/saude` estão normais.
4. Estado é efêmero: não há dado a restaurar (RPO zero), e o RTO é o tempo de reimplantar.

## Lições aprendidas (obrigatório depois de qualquer incidente)

Registrar em nota nova de `04-Seguranca/Incidentes/AAAA-MM-DD-titulo.md`:

- O que aconteceu, em ordem cronológica.
- Como foi detectado e quanto demorou.
- Causa raiz.
- O que foi feito (contenção, correção).
- Que teste novo passou a existir.
- Que controle, limite ou alerta mudou.
- O que ainda é risco residual ([[Controles-e-SecPlus]], registro de riscos).

## Cadeia de evidência

Se um incidente exigir guardar prova: copiar log e eventos relevantes para um arquivo com data e hora, calcular o SHA-256 do arquivo, anotar no registro quem copiou, quando e de onde. Coletar primeiro o que se perde mais rápido (sessões em memória e métricas) antes do que persiste (logs do provedor).

## Relacionadas

[[Modelo-de-Ameacas]] · [[Cabecalhos-e-Configuracao-Segura]] · [[Pipeline-CI-e-Container]]
