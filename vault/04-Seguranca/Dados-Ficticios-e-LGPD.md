---
tipo: seguranca
projeto: Bancada
atualizado: 2026-11-01
---

# Dados fictícios e LGPD

## Princípio

Nenhum dado de pessoa real entra, fica ou sai. Mesmo assim o projeto trata o domínio como se fosse dado de saúde (dado pessoal sensível na LGPD), porque a demonstração é essa.

## Como os dados fictícios nascem

- Gerador **determinístico** com semente fixa, escrito no projeto (listas de nomes e sobrenomes inventados, sem pessoa pública). Mesma semente, mesmo banco, o que facilita teste e demonstração.
- Paciente identificado por `codigo` (`PAC-0001`), nascimento e sexo. **Sem CPF, telefone, endereço, e-mail.** CPF com dígito verificador válido poderia pertencer a uma pessoa real; por isso nem entra.
- Exames, faixas de referência e equipamentos vêm de um catálogo inventado e simplificado.

## O visitante também escreve

Evolução do prontuário, motivos, observações. Risco: alguém digitar dado real. Tratamento:

| Medida | Efeito |
|---|---|
| Aviso fixo "Dados fictícios. Não digite dado real." | Diretivo |
| Limite de tamanho | Reduz abuso |
| Cifra em repouso, chave por sessão | Protege enquanto existe |
| Banco só em memória, TTL | Some sozinho |
| Conteúdo nunca vai para log | Só ids e tipos de evento |
| Sem backup, sem cache compartilhado | Nada sobrevive à sessão |

## Princípios da LGPD aplicados

| Princípio | Aplicação |
|---|---|
| Finalidade | Demonstração de software, dito na entrada |
| Necessidade / minimização | Só os campos que o fluxo exige |
| Segurança | [[Controles-e-SecPlus]] |
| Prevenção | Modelo de ameaças e testes |
| Transparência | Página "Sobre os dados" explica o que existe |
| Eliminação | Sessão expira e apaga tudo |

Os direitos do titular (acesso, exclusão, portabilidade) não se aplicam porque não há titular real nem retenção. A página diz isso com essas palavras.

## Aviso médico

"Simulação educacional. Não é dispositivo médico, não interpreta resultados e não deve ser usado em decisão clínica." Aparece no rodapé de toda página e em todo laudo.

## Relacionadas

[[Modelo-de-Dados]] · [[Visao-e-Escopo]]
