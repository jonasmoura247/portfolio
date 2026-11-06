---
tipo: arquitetura
projeto: Bancada
atualizado: 2026-11-01
---

# Arquitetura MVC

ASP.NET Core MVC com o **M** em camadas. A mesma separação do Bestia, que já provou funcionar com cobertura de 100%.

## Projetos

```
Bancada.sln
src/
  Bancada.Dominio/          entidades, máquinas de estado, regras. Sem dependência nenhuma.
  Bancada.Protocolos/       ASTM/HL7, Code 128, cálculos (Westgard). Funções puras.
  Bancada.Aplicacao/        casos de uso, portas (IRepositorio...), ViewModels de entrada/saída.
  Bancada.Infraestrutura/   EF Core (DbContext, mapeamentos, migrations), cifra, relógio, sessões, auditoria.
  Bancada.Web/              MVC: Controllers, Views, wwwroot (js, css), Program.cs.
tests/
  Bancada.Testes.Guardas/   arquitetura (setas, proibições), licenças, container e CI. Sem cobertura: só testa o repositório.
  Bancada.Testes.Dominio/   Bancada.Testes.Protocolos/   Bancada.Testes.Aplicacao/
  Bancada.Testes.Infraestrutura/   Bancada.Testes.Web/   (integração com WebApplicationFactory)
  js/                       testes de JavaScript (node --test)
tools/                      scripts de cobertura, auditoria de licenças, geração de dados do painel
vault/                      este vault
```

## Direção das dependências

```
Web ──► Aplicacao ──► Dominio
 │           ▲            ▲
 └─► Infraestrutura ──────┘     Protocolos ◄── Dominio, Aplicacao
```

Dominio não conhece ninguém. Aplicacao define as portas, Infraestrutura as implementa. **Teste de arquitetura** por reflexão (escrito à mão) falha o build se alguém quebrar a seta.

## Regras por camada

| Camada | Pode | Não pode |
|---|---|---|
| Controller | validar entrada HTTP, chamar **um** caso de uso, devolver View ou JSON | acessar banco, conter regra de negócio, montar SQL |
| Caso de uso | orquestrar domínio e portas, checar autorização | conhecer HTTP, SQL ou JSON |
| Domínio | regras, estados, exceções de domínio | I/O, relógio do sistema, aleatório sem semente |
| Infraestrutura | SQL, cifra, arquivos, relógio | regra de negócio |
| View | exibir ViewModel, escapar saída | lógica além de laço e condição de exibição |

Tempo (`IRelogio`), aleatório (`ISorteador`), cifra e armazenamento entram por interface. Por isso tudo é testável sem mock de biblioteca.

## JavaScript

- Módulos ES nativos, uma pasta por tela, **sem framework, sem bundler, sem dependência**.
- Cada tela separa **lógica pura** (testada direto) de **ligação com o DOM** (recebe `document` por parâmetro). Os testes passam um DOM de mentira escrito à mão. Assim os 100% de JS não dependem de jsdom.
- **Nada inline**: nem `<script>` nem `style=""`. Isso permite uma CSP sem `unsafe-inline` e sem nonce ([[Decisoes-ADR]] ADR-007).
- Chamadas ao servidor sempre com o token antifalsificação no cabeçalho.

## Texto e idiomas

Todo texto vem de recursos (pt-BR e en). Teste de paridade garante as mesmas chaves nos dois. Teste de "texto solto" varre as views atrás de texto fora de recurso.

## Estilo

Tema próprio (claro e escuro), fontes do sistema (nenhuma fonte externa, assim não há licença nem requisição de terceiros). Sem biblioteca de CSS. Paleta, tipografia e componentes em [[Design-do-Sistema]].

## Detalhamento

[[Convencoes-de-Codigo]] · [[Camada-de-Aplicacao]] · [[Rotas-e-Controllers]] · [[Sessao-e-Isolamento]] · [[Protocolos]] · [[Javascript-Modulos]] · [[Pipeline-CI-e-Container]]

## Relacionadas

[[Decisoes-ADR]] · [[Estrategia-de-Testes]] · [[Modulos]]
