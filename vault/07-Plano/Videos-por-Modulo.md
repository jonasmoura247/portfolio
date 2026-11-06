---
tipo: plano
projeto: Bancada
atualizado: 2026-11-01
---

# Vídeos por módulo

**Pedido do Jonas (01/11/2026):** depois do sistema pronto, ele quer criar **um vídeo explicando cada módulo** e anexá-los ao portfólio e ao próprio Bancada. A execução é dele; este plano só reserva o lugar e deixa o material pronto.

## O que já existe na máquina

| Recurso | O que faz | Serve para |
|---|---|---|
| Skill `brag` / `brag-slim` | Transforma um projeto (pasta ou site) em vídeo curto de lançamento, com música, movimento e texto de divulgação | Vídeo de apresentação do Bancada inteiro. Já há uma pasta `brag-output` no Bestia, então o fluxo é conhecido |
| Skill `video-resumos` | Gera vídeos verticais (TikTok/Shorts) a partir de resumos em `.md`, com voz `pf_dora` e prints das fontes | Formato vertical e narração em português; o fluxo é para resumos, não para telas de sistema |
| Playwright (já usado na verificação) | Controla o navegador e pode gravar a sessão em vídeo | Roteiro de tela de cada módulo |
| Ferramentas de vídeo por IA do servidor Higgsfield | Geram vídeo por créditos | Só com autorização, porque gastam créditos |

Nenhuma delas é um "gerador de vídeo explicativo de módulo" pronto. O caminho mais provável é **gravação de tela do roteiro do Playwright + narração + texto na tela**, montado pela skill escolhida. Decidir quando chegar a hora.

## O que o plano prepara desde já

1. **Roteiro por módulo** (`tools/e2e/<modulo>.md`) que já serve de guião do vídeo: passos, o que clicar, o que aparece, em ordem.
2. **Dados de cenário** que dão vídeo bom sem preparo: os cenários de [[Dados-de-Semente]] (valor crítico, hemólise, equipamento com erro, controle fora) e o relógio acelerado.
3. **Estado repetível**: semente fixa, então a gravação sai igual toda vez.
4. **Resumo de uma página por módulo** (o que é, regras principais, o que mostrar), escrito no fim de cada módulo para virar a fala do vídeo.

## Esqueleto de cada vídeo (60 a 90 s)

1. Abertura: o problema do laboratório que o módulo resolve (10 s).
2. Fluxo feliz na tela (30 s).
3. Uma regra de negócio sendo aplicada, com erro e mensagem (20 s).
4. O que há por trás: teste, segurança ou protocolo que protege o módulo (15 s).
5. Fecho com o nome do módulo e o próximo (5 s).

## Lista de vídeos

| # | Módulo | Destaque sugerido |
|---|---|---|
| 1 | Visão geral e papéis | Menu muda por papel, operador, sessão isolada |
| 2 | Pacientes e agenda | Dupla reserva recusada, grade por teclado |
| 3 | Atendimento e prontuário | Evolução imutável, correção encadeada, texto cifrado |
| 4 | Pedido de exame | Tubos agrupados por material, alerta de alergia |
| 5 | Coleta e etiqueta | Code 128 de verdade, jejum e volume |
| 6 | Soroteca | Mapa, cadeia de custódia, validade |
| 7 | Equipamento e protocolo | Quadros ASTM com checksum, fila, falha e reprocesso |
| 8 | Qualidade | Levey-Jennings e Westgard bloqueando liberação |
| 9 | Resultado e laudo | Crítico, delta check, separação de deveres, laudo imutável |
| 10 | Auditoria | Cadeia de hash e adulteração detectada |
| 11 | Painel técnico e laboratório de ataque | Testes rodando, cobertura, defesas |

## Onde os vídeos ficam

- Arquivos em `Bancada/videos/` (fora do repositório de código; vídeo é pesado).
- No portfólio: cada vídeo no cartão do módulo; o Bancada ganha uma galeria "Como funciona".
- Hospedagem do arquivo de vídeo: decidir junto com a hospedagem do site (fim do projeto). Vídeo não pode ser embutido de outro site no portfólio atual (artefato); precisa de arquivo próprio.

## Relacionadas

[[Verificacao-com-Playwright]] · [[Roadmap-Fases]] · [[Checklists-por-Fase]]
