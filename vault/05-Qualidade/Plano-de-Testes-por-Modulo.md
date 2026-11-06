---
tipo: qualidade
projeto: Bancada
atualizado: 2026-11-01
---

# Plano de testes por módulo

Complementa [[Estrategia-de-Testes]]: aqui está **o que testar em cada projeto e em cada módulo**. A numeração `RN-###` liga ao [[Regras-de-Negocio]].

## Projetos de teste

| Projeto | Cobre | Dependências |
|---|---|---|
| `Bancada.Testes.Dominio` | Entidades, estados, regras | Nenhuma além do NUnit |
| `Bancada.Testes.Protocolos` | ASTM, HL7, Code 128, cálculos | Nenhuma |
| `Bancada.Testes.Aplicacao` | Casos de uso com dublês à mão | Dominio, Aplicacao |
| `Bancada.Testes.Infraestrutura` | EF Core, SQLite real, cifra, auditoria, sessões | SQLite em memória |
| `Bancada.Testes.Web` | Pipeline completo, Views, segurança, matriz, painel | `WebApplicationFactory` |
| `tests/js` | Todo o JavaScript | `node --test` |

## Por módulo

### Pacientes
- Busca vazia, com acento, com maiúsculas, com 64 e 65 caracteres, com payloads do corpus.
- Paginação: primeira, última, página além do fim, tamanho 0 e negativo.
- JS: busca com atraso cancela a anterior; resposta antiga não sobrescreve a nova.

### Agenda e atendimento
- RN-001 a RN-008; dupla reserva concorrente (duas tarefas, uma vence).
- Grade da agenda (JS): semana com feriado/lacuna, horário nos limites 08:00 e 16:30, fuso do navegador diferente do servidor.
- Estados: teste exaustivo de todos os pares (estado, evento).

### Prontuário
- RN-010 a RN-013; tentativa de `UPDATE`/`DELETE` direto no banco falha.
- Correção encadeada de 3 níveis aparece em ordem.
- Texto cifrado: coluna ilegível, leitura devolve o original, AAD errada falha.
- XSS: `<script>` aparece como texto na View e no JSON.

### Pedido e coleta
- RN-020 a RN-025; pedido com 0, 1, 20 e 21 exames; repetido.
- Agrupamento: GLI+URE+CRE em um tubo; HEM em outro; TSH+HEM em dois.
- Jejum: confirmação ausente, parcial, suficiente.
- Etiqueta: unicidade sob criação concorrente; Code 128 ida e volta.

### Soroteca
- RN-030 a RN-035; guardar duas amostras na mesma posição (domínio e banco); mover para posição ocupada.
- Validade no limite exato (7 dias e 0 segundos, 7 dias e 1 segundo).
- Mapa (JS): caixa vazia, cheia, ocupação parcial, foco por teclado.
- Cadeia de custódia contém toda movimentação, em ordem.

### Equipamento e qualidade
- RN-040 a RN-044; capacidade, fila, prioridade, reprocesso, semente fixa.
- ASTM: checksum, quadro sem `ETX`, `ETB`, quadro duplicado, `NAK` com 6 reenvios, `EOT`.
- HL7: `MSH` sem versão, `OBX` sem unidade, segmento extra, campo vazio, escape de `|`.
- Westgard: uma série que viola e uma que não viola para cada regra; série curta demais.
- Levey-Jennings (JS): escala com zero desvio, ponto fora do gráfico.

### Resultado e laudo
- RN-050 a RN-056; faixa por sexo e idade (17, 18, 85 anos), faixa aberta, valor exatamente no limite.
- Flags H/L/C em todos os limites (<, =, >).
- Delta check com valor anterior ausente.
- Separação de deveres: mesmo papel, mesmo usuário lógico.
- Laudo: imutável, retificação mantém a versão anterior.
- Impressão: View do laudo contém o aviso médico.

### Auditoria
- Ver [[Auditoria-Encadeada]]: cadeia, forma canônica estável por cultura, imutabilidade no banco.

### Segurança transversal
- Matriz de autorização completa ([[Matriz-de-Autorizacao]]).
- Corpus de ataque em todo campo ([[Corpus-de-Ataque]]).
- Cabeçalhos e limites ([[Cabecalhos-e-Configuracao-Segura]]).
- Isolamento entre sessões e TTL ([[Sessao-e-Isolamento]]).
- Cifra ([[Cifra-e-Chaves]]).
- Arquitetura (dependências, `DateTime.Now`, `Random`, SQL montado, `[ExcludeFromCodeCoverage]`).
- Licenças, texto solto, paridade de idiomas, contraste de cores, `alt` e rótulos.

### Painel técnico
- Instantâneo: formato válido, selos corretos.
- Executor: comando constante, parâmetro extra ignorado, uma execução por vez, taxa, tempo máximo, saída limitada e escapada, kill switch.
- Visualizador de código: lista permitida, travessia de caminho, arquivo grande.

## Rastreabilidade

Um script em `tools/` lê `Regras-de-Negocio.md`, extrai cada `RN-###` e confere que existe teste com esse prefixo no nome. Regra sem teste falha o CI. O painel mostra "regras com teste: N/N".

## Convenções de teste

- Nome: `Metodo_Cenario_Resultado` ou `RN_###_Descricao` ([[Convencoes-de-Codigo]]).
- Um assunto por teste; preparar, agir, conferir.
- Dados de teste construídos por métodos de fábrica pequenos, nunca por arquivo externo (exceto o corpus).
- Teste não depende de ordem, de relógio real, de rede nem de arquivo fora do projeto.
- Teste lento (> 1 s) é marcado e o painel mostra os mais lentos.

## Relacionadas

[[Estrategia-de-Testes]] · [[Roadmap-Fases]]
