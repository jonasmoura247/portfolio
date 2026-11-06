---
tipo: painel
projeto: Bancada
atualizado: 2026-11-01
---

# Instantâneo do painel

Arquivo `painel-instantaneo.json` gerado pelo CI (passo 9 de [[Pipeline-CI-e-Container]]) e embutido na imagem. É o que o painel mostra quando o executor está desligado e a fonte da cobertura de C#.

Gerado por programa em `tools/` que lê **arquivos que o próprio build já produz**: resultado do NUnit (XML), cobertura (Cobertura XML), saída do `node --test`, auditoria de licenças e lista de testes. Nada é digitado à mão.

## Formato

```json
{
  "versao": 1,
  "gerado_em": "2026-11-01T14:03:22Z",
  "commit": "3f9c1ab",
  "ramo": "main",
  "dotnet": "9.0.x",
  "node": "24.x",
  "csharp": {
    "projetos": [
      { "nome": "Bancada.Dominio", "testes": 312, "aprovados": 312, "falhas": 0, "ignorados": 0, "duracao_ms": 1840 }
    ],
    "cobertura": {
      "linhas": { "cobertas": 4120, "total": 4120, "pct": 100 },
      "ramos":  { "cobertos": 1388, "total": 1388, "pct": 100 },
      "por_projeto": [ { "nome": "Bancada.Dominio", "linhas_pct": 100, "ramos_pct": 100 } ],
      "por_arquivo": [ { "arquivo": "Dominio/Agendamento.cs", "linhas_pct": 100, "ramos_pct": 100 } ]
    }
  },
  "javascript": {
    "testes": 204, "aprovados": 204, "falhas": 0, "duracao_ms": 920,
    "cobertura": { "linhas_pct": 100, "ramos_pct": 100, "funcoes_pct": 100 }
  },
  "regras_de_negocio": { "total": 45, "com_teste": 45 },
  "licencas": [
    { "pacote": "NUnit", "versao": "4.x", "licenca": "MIT", "origem": "direto", "excecao": false },
    { "pacote": "SQLitePCLRaw.core", "versao": "2.x", "licenca": "Apache-2.0", "origem": "Microsoft.Data.Sqlite", "excecao": true }
  ],
  "vulnerabilidades": { "pacotes_vulneraveis": 0 },
  "catalogo_de_testes": [
    { "projeto": "Bancada.Testes.Dominio", "classe": "AgendamentoTestes", "nome": "RN_001_HorarioOcupadoRecusaSegundaReserva", "rn": "RN-001" }
  ],
  "seguranca": { "matriz_secplus": "ver seguranca.json", "casos_de_abuso": { "total": 12, "com_teste": 12 } }
}
```

(Os números do exemplo são ilustrativos.)

## Regras

1. O gerador **falha** se faltar qualquer arquivo de entrada, se o JSON resultante não validar contra o esquema embutido, ou se algum percentual de cobertura vier de dado ausente (não existe "100% por padrão").
2. O painel mostra `commit` e `gerado_em` ao lado de qualquer número vindo daqui, para ninguém confundir instantâneo com execução ao vivo.
3. Selos da visão geral saem deste arquivo: testes, cobertura, licenças, vulnerabilidades, regras com teste.
4. `seguranca.json` é gerado a partir da tabela de [[Controles-e-SecPlus]] (cada linha com o nome do teste que a prova); o teste confere que todo nome citado existe de verdade.

## Testes

- Gera a partir de arquivos de exemplo e confere cada campo.
- Entrada faltando ou corrompida faz o gerador falhar com mensagem clara.
- Percentual calculado de totais zero não divide por zero.
- Esquema recusa campo desconhecido ou tipo errado.

## Relacionadas

[[Painel-Tecnico]] · [[Executor-de-Testes]] · [[Estrategia-de-Testes]]
