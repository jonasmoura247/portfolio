# Relatório de auditoria de cybersecurity: Portfólio + Bancada

Data: 07/11/2026 · Escopo: repositório inteiro (`src/`, `tests/`, `tools/`, `Dockerfile`, `.github/`) · Ambiente de ataque: só local (Kestrel em `localhost` e contêiner em `127.0.0.1`), nunca produção nem terceiros.

> Segurança absoluta não se prova. Este relatório diz o que foi atacado, o que cedeu, o que foi corrigido e o que ainda depende da hospedagem. Os riscos que sobraram estão na seção G.

---

## A. Resumo executivo

| | |
|---|---|
| Estado inicial | Base sólida: autorização no servidor, antifalsificação, CSP estrita, consultas parametrizadas e saída codificada resistiram. As falhas estavam em **padrões inseguros, limites de taxa e concorrência**, ou seja, no que só aparece com a aplicação publicada e sob carga hostil. |
| Achados | **11** (3 altos, 3 médios, 4 baixos, 1 informativo) |
| Corrigidos | **11 de 11**, cada um com teste de regressão (`SEC-xx`) que falhava antes e passa depois |
| Ataques caixa-preta | `tools/seguranca/ataques.mjs`: **8 falhas em 47 verificações antes**, **0 em 47 depois** (na aplicação local e no contêiner endurecido) |
| Testes | 573 C# (antes 521) + 39 JS, **100% de linhas e ramos**; `tools/verificar` verde |
| Risco residual principal | **.NET 9 sai de suporte em 10/11/2026** (daqui a 3 dias). O projeto fica no .NET 9 por decisão do dono: risco aceito, sem correções de segurança do runtime depois dessa data. Ver G-01. |
| Estado final | Pronto para publicar **depois** de cumprir o checklist da seção H. Nenhuma vulnerabilidade crítica ou alta conhecida está aberta no código. |

---

## B. Inventário da superfície de ataque

### Modelo de ameaça adotado

- **Não há login.** Todo visitante é anônimo, entra como Admin e pode assumir qualquer papel. Isso é proposital: é uma demonstração. **Papel não é fronteira de segurança.** A fronteira real é a **sessão**: cada visitante tem banco SQLite em memória, chave AES-256 e cadeia de auditoria próprios.
- Por isso, a matriz de papéis foi auditada como **regra de negócio**: o servidor recusa o que o papel atual não pode, mesmo por requisição direta. A pergunta de segurança passou a ser outra: "um visitante consegue afetar outro visitante, o servidor ou a infraestrutura?".
- Ativos: disponibilidade do site para todos, isolamento entre sessões, integridade das regras de negócio dentro da sessão, o processo e o host (executor de testes) e os segredos da hospedagem.

### Pontos de entrada

| Tipo | Rotas |
|---|---|
| Estático, sem sessão | `/` (portfólio), `/css`, `/js`, `/portfolio/*`, `/videos/*.mp4` |
| Saúde e erro (sem sessão, sem limite) | `GET /saude`, `/erro/{codigo}` |
| Sessão | `POST /sessao/{papel,operador,idioma,cenario,reiniciar}` |
| Clínico (GET) | `/bancada`, `/pacientes[/{id}]`, `/agenda`, `/atendimentos[/{id}]`, `/prontuario[/{id}]`, `/pedidos[/novo,/{id}]` |
| Clínico (POST) | `/agenda/agendar`, `/agenda/{id}/{reagendar,cancelar,confirmar}`, `/atendimentos/{id}/{checkin,iniciar,evolucao,concluir}`, `/prontuario/{id}/alergia`, `/prontuario/alergia/{id}/alternar`, `/prontuario/evolucao/{id}/corrigir`, `/pedidos/novo`, `/pedidos/{id}/cancelar` |
| Laboratório (GET) | `/coletas`, `/etiquetas`, `/amostras/{id}/etiqueta`, `/soroteca`, `/equipamentos`, `/qualidade`, `/resultados`, `/laudos/{id}` |
| Laboratório (POST) | `/coletas/{id}/coletar`, `/amostras/{id}/rejeitar`, `/soroteca/{armazenar,mover,descartar}`, `/equipamentos/{id}/enviar`, `/equipamentos/relogio`, `/execucoes/{id}/reprocessar`, `/qualidade/medicao`, `/resultados/{id}/{validar,confirmar-critico}`, `/laudos/{id}/{liberar,retificar}` |
| Técnico | `/auditoria`, `POST /auditoria/{verificar,simular}`, `/operacao`, `/painel?aba&arquivo`, `POST /painel/testes/executar`, `GET /painel/testes/estado` (JSON) |

- **Entradas externas:** campos de formulário (texto livre limitado de 60 a 2000 caracteres e validado no servidor), query strings, ids de rota (`int`), o cookie `__Host-bancada.sid`, o cookie e o token de antifalsificação, e os cabeçalhos `X-Forwarded-*` (agora só de proxy configurado).
- **Protocolos HL7 e ASTM:** **não há listener de rede.** As mensagens são geradas e lidas pelo próprio servidor dentro da simulação. Nenhum visitante envia um quadro ASTM ou um segmento HL7. Mesmo assim, os leitores foram atacados como se recebessem entrada hostil (SEC-08).
- **Infraestrutura:** contêiner chiseled (sem shell), porta 8080, health check em `/saude`, CI no GitHub Actions.

---

## C. Vulnerabilidades encontradas

Os vetores CVSS 4.0 são estimativas para orientar a prioridade, com o modelo de ameaça acima.

| ID | Gravidade | Categoria (CWE) | Descrição | Situação |
|---|---|---|---|---|
| SEC-01 | **ALTA** | Padrão inseguro (CWE-1188) | Executor de testes **ligado por padrão**: qualquer visitante anônimo fazia o servidor rodar `node --test` ou `dotnet test` (até 180 s de CPU) fora do Docker | Corrigido |
| SEC-02 | **ALTA** | Limite contornável (CWE-799, CWE-770) | Cookie de sessão **inventado** furava o limite de novas sessões (10/min caía para 120/min): um cliente expulsava as sessões de todos (teto de 200) | Corrigido |
| SEC-03 | **ALTA** | Identificação do cliente (CWE-348, CWE-770) | Atrás de proxy reverso, todos os visitantes tinham o IP do proxy: **10 novas sessões por minuto para o site inteiro**, e ~15 requisições de um atacante **bloqueavam todos** por 10 min. IPv6 girando no /64 fugia do limite | Corrigido |
| SEC-04 | MÉDIA | Condição de corrida (CWE-362) | Eventos de segurança gravados **fora da vez da sessão**: com recusas simultâneas, a cadeia repetia `Seq` (alarme falso de adulteração), dava `UNIQUE constraint failed` (500) e usava a conexão SQLite em duas threads | Corrigido |
| SEC-06 | MÉDIA | Exposição de ambiente (CWE-526) | O processo do executor herdava **todas** as variáveis do servidor (só 3 eram removidas, embora a documentação dissesse "ambiente limpo"). Segredos da hospedagem chegariam aos testes e à saída exibida | Corrigido |
| SEC-07 | MÉDIA | Uso após descarte (CWE-362, CWE-416) | Sessão expulsa pelo teto (ou trocada de cenário) tinha o banco **fechado no meio de uma requisição em andamento**, e quem esperava a vez seguia com a sessão morta | Corrigido |
| SEC-05 | BAIXA | Tratamento de erro (CWE-755) | Verbos não suportados (PUT, DELETE, PATCH, OPTIONS, TRACE) e 413 viravam **500**: alarme falso no monitoramento e indício enganoso para quem ataca | Corrigido |
| SEC-08 | BAIXA | Validação de entrada (CWE-20) | Leitores ASTM e HL7 aceitavam `NaN`, `Infinity` e `1e999` como resultado | Corrigido |
| SEC-09 | BAIXA | Recurso sem teto (CWE-770) | Tabela de bloqueio de IP só crescia (uma entrada por IP recusado, para sempre) | Corrigido |
| SEC-10 | BAIXA | Cadeia de suprimentos (CWE-829) | CI sem `permissions` (token com o padrão do repositório), actions por tag móvel (`@v4`), checkout guardando a credencial | Corrigido |
| SEC-11 | INFORMATIVA | Endurecimento de contêiner | Imagens base por tag (sem digest), contexto de build sem `.dockerignore` (levava `.git`, `.vs`, `vault`), canal de diagnóstico do .NET ativo | Corrigido |

### Detalhe por achado

**SEC-01 · Executor ligado por padrão** · `Program.cs` (`GetValue("Bancada:Executor:Habilitado", true)`), `ServicosTecnicos.cs` (`Habilitado = true`)
- **Vetor estimado:** `CVSS:4.0/AV:N/AC:L/AT:P/PR:N/UI:N/VC:L/VI:N/VA:H/SC:N/SI:N/SA:N`.
- **Pré-condição:** publicar sem passar pelo Dockerfile, que é o único lugar que desligava o executor (por exemplo, App Service com código, `dotnet run` numa VPS ou outro Dockerfile).
- **Exploração:** `POST /painel/testes/executar suite=js`. Na instância Production sem configuração: `estado=Concluido suite=js saída=2683 bytes`, ou seja, o processo rodou no servidor.
- **Correção:** o executor vem desligado. Só liga com `Bancada:Executor:Habilitado=true` ou no ambiente Development.
- **Testes:** `SEC01_Executor_EmProducaoSemConfiguracao_NaoRodaNada`, `SEC01_Executor_EmProducao_SoRodaSeLigadoDePropositoNaConfiguracao`, `Executor_PorPadrao_NasceDesligado`.

**SEC-02 · Limite de novas sessões contornável** · `LimitesDeTaxa.cs`
- **Vetor estimado:** `CVSS:4.0/AV:N/AC:L/AT:N/PR:N/UI:N/VC:N/VI:N/VA:L/SC:N/SI:N/SA:H`.
- **Causa:** a partição perguntava só "tem cookie?". Com um cookie qualquer, a requisição caía no balde de leitura (120/min), e o servidor criava uma sessão nova para o id desconhecido.
- **Exploração:** 16 GETs com `__Host-bancada.sid=falso…` criaram 16 sessões, sem nenhuma recusa. Uma sessão custa ~0,44 MB (medido: 100 sessões, +44 MB). O teto de 200 protege a memória, mas expulsa as sessões mais ociosas, então um único cliente derrubava o trabalho de todos os visitantes em menos de 2 minutos.
- **Correção:** só conta como "com sessão" o cookie de uma **sessão viva** no repositório.
- **Testes:** `SEC02_CookieDeSessaoInventado_ContaNoLimiteDeNovasSessoes`, `SEC02_Particao_CookieDeSessaoQueNaoExiste_VaiProBaldeDeNovaSessao`.

**SEC-03 · Cliente atrás de proxy e IPv6** · `Program.cs`, `Registro.cs`, `Seguranca.cs`, `LimitesDeTaxa.cs`
- **Vetor estimado:** `CVSS:4.0/AV:N/AC:L/AT:P/PR:N/UI:N/VC:N/VI:N/VA:N/SC:N/SI:N/SA:H`.
- **Causa:** limites e bloqueio usavam `RemoteIpAddress`. Nas hospedagens recomendadas (Fly, Render, Azure, Caddy), esse IP é o do proxy. Comprovado no contêiner: pela porta publicada, todo cliente aparece com o IP do gateway do Docker.
- **Correção:**
  - `UseForwardedHeaders` **só** com `Bancada:ProxiesConfiaveis` (IPs e CIDRs) e `ForwardLimit = 1`. Sem a lista, o `X-Forwarded-For` é ignorado, então forjá-lo não abre balde novo. Entrada inválida derruba a subida (falha fechado).
  - IPv6 conta por /64.
  - IPv4 mapeado em IPv6 vira IPv4.
- **Testes:** `SEC03_XForwardedFor_SemProxyConfigurado_EIgnorado`, `SEC03_XForwardedFor_DoProxyConfigurado_SeparaOsVisitantes`, `SEC03_XForwardedFor_DeQuemNaoEOProxy_EIgnorado`, `SEC03_ChaveDoCliente_*`, `SEC03_ProxiesConfiaveis_SoOsListados`, `SEC03_Particao_IPv6_ContaPorPrefixo64`.

**SEC-04 · Corrida na cadeia de auditoria** · `Seguranca.cs` (`AnotarEvento`), `LimitesDeTaxa.cs` (`OnRejected`), `SessaoMiddleware.cs`
- **Causa:** o limitador de taxa e a proteção de corpo grande gravavam na cadeia **sem a trava da sessão**, ao mesmo tempo que a requisição legítima gravava. O registro de `SessaoCriada` também acontecia antes da trava.
- **Exploração:** 40 requisições simultâneas com o mesmo cookie, acima do limite, enquanto requisições lentas seguravam a sessão. Resultado: cadeia quebrada no elo 2 e 8 erros `SQLite Error 19: 'UNIQUE constraint failed: Auditoria.Seq'`.
- **Correção:** a vez da sessão passou para dentro da `Sessao` (`Entrar`, `TentarEntrar`, `Sair`). Quem está fora da requisição **enfileira** o evento (fila com teto de 100). O evento é gravado na hora, se a sessão estiver livre, ou por quem estiver com a vez, ao sair. A conferência da fila e a devolução da vez são atômicas (`SairSeNadaPendente`), então nenhum evento fica esquecido e nenhuma recusa espera a requisição legítima.
- **Testes:** `SEC04_Auditoria_RecusasSimultaneasComRequisicoesEmCurso_CadeiaContinuaIntegra` (web), `SEC04_MuitasAnotacoesSimultaneas_CadeiaIntegraSemPerderNenhuma`, `SEC04_Fila_*`, `SEC04_Anotar_*`, `SEC04_SairSeNadaPendente_*`, `SEC04_GravacaoFalhou_DevolveAVezMesmoAssim`.

**SEC-05 · 405 e 413 viravam 500** · `ErroController.cs`
- **Correção:** a página de erro conhece 405 e 413, e um código desconhecido mantém a classe (4xx vira 400, nunca 500).
- **Testes:** `SEC05_VerboNaoSuportado_Devolve405ENao500` (5 verbos) e casos novos em `PaginaDeErro_CodigoConhecidoOuNao_UsaOStatusCerto`.

**SEC-06 · Ambiente herdado pelo processo** · `ProcessosDoSistema.cs`
- **Correção:** `Environment.Clear()` e depois uma **lista de permissão** (PATH, HOME, pastas do sistema e do .NET).
- **Teste:** `SEC06_Processo_NaoHerdaVariavelForaDaLista` (uma variável `BANCADA_SEGREDO_DE_TESTE` não chega ao processo filho).

**SEC-07 · Sessão descartada em uso** · `Sessao.cs`, `SessaoMiddleware.cs`
- **Correção:** descartar uma sessão em uso só marca o descarte. Os recursos (banco e chave) são liberados quando a requisição sai. Quem estava esperando a vez de uma sessão descartada segue numa sessão nova, como se ela tivesse expirado.
- **Testes:** `SEC07_DescartadaEmUso_SoLiberaOsRecursosQuandoARequisicaoSai`, `SEC07_TentarEntrar_SoComAVezLivreESessaoViva`, `SEC07_SessaoDescartadaEnquantoAEsperava_SegueNumaSessaoNova`.

**SEC-08 · Leitores de protocolo aceitavam não finitos** · `Astm.cs`, `Hl7.cs`
- **Correção:** `double.IsFinite`.
- **Testes:** casos `NaN`, `Infinity`, `-Infinity` e `1e999`, mais a classe `ProtocolosHostisTestes`: 5.000 transmissões ASTM mutadas, 5.000 de lixo e 5.000 mensagens HL7 aleatórias, com semente fixa. O único erro aceito é `FormatException`; não há valor não finito nem laço sem fim; 2.000 registros são remontados sem problema.

**SEC-09 · Bloqueio de IP sem teto** · `Seguranca.cs`
- **Correção:** acima de 10.000 entradas, as vencidas são podadas (bloqueio ainda válido fica).
- **Teste:** `SEC09_BloqueioDeIp_MuitosIpsDiferentes_PodaOsVencidos`.

**SEC-10 · CI** · `.github/workflows/ci.yml`, `.github/dependabot.yml`
- **Correção:** `permissions: contents: read`, actions fixadas por SHA de commit, `persist-credentials: false`, `timeout-minutes`, `concurrency`, e Dependabot para NuGet, Actions e Docker.
- **Teste:** `SEC10_Ci_TokenSoDeLeituraEActionsFixadasPorCommit` (guarda de arquitetura).

**SEC-11 · Contêiner** · `Dockerfile`, `.dockerignore`
- **Correção:** imagens base fixadas por digest, `DOTNET_EnableDiagnostics=0`, `.dockerignore`.
- **Teste:** `SEC11_ContextoDoDocker_NaoLevaGitNemArtefatosLocais`.
- **Validação em execução:** ver a seção F.

---

## D. Ataques simulados

Ferramentas: Node 24 (`fetch` e `http`) em `tools/seguranca/ataques.mjs`, NUnit + `WebApplicationFactory` (`AtaquesTestes`, `RegrasSobAtaqueTestes`), Playwright/Chromium em modo escuro, Docker, Trivy e gitleaks. OWASP ZAP e Burp ficaram de fora: o ZAP é proibido pela política de licenças do projeto, e o Burp não estava disponível. A cobertura equivalente veio dos roteiros acima.

| # | Ataque | Método | Resultado | Evidência |
|---|---|---|---|---|
| 1 | Escalonamento de privilégio | Como Recepção, 13 POSTs diretos com token válido em ações de Médico, Coleta, Analista e Admin, mais 10 GETs de telas de outros papéis | **Resistiu**: 403 em todas, e a recusa entra na auditoria. Porém qualquer visitante troca de papel quando quer (por desenho: ver o modelo de ameaça) | ataques.mjs §2; `AcaoProtegida_PapelSemPermissao_Devolve403EAuditaARecusa` |
| 2 | Acesso a outra sessão | Sessões A e B. A grava uma alergia marcada; B lê e tenta alternar a alergia **pelo mesmo id** (os ids da semente coincidem entre sessões). Depois, 20 escritas paralelas de A e B | **Resistiu**: B não vê nem altera nada de A. O mesmo id aponta para o banco de B | ataques.mjs §4; `SEC12_DuasSessoesEmParalelo_NenhumaVeOuMexeNoDadoDaOutra` |
| 3 | Manipulação de requisição | Ids negativos e `int.MaxValue`, página `2147483647`, `%00` na caixa, semana `99999999999`, verbos trocados, corpo de 20 KB | **Cedeu parcialmente** (SEC-05: 500 nos verbos). O resto respondeu 200 ou 404 sem vazar nada | ataques.mjs §1, §7 |
| 4 | XSS | Refletido em `/pacientes` e `/etiquetas` (4 cargas). Armazenado na alergia (substância e reação, 3 cargas) e verificado **no Chromium**, com detector `window.__xss` e escuta de `alert` | **Resistiu**: tudo sai codificado como texto, nenhum script roda, nenhum diálogo abre. Não há `Html.Raw` nas views. O JS da Bancada não usa `innerHTML`, e o do portfólio só o usa com constantes | ataques.mjs §5; Playwright: `{prontuario:null, ficha:null, busca:null, dialogos:[]}` |
| 5 | CSRF | POST sem token, com token de outra sessão e de outra origem sem o cookie de antifalsificação (`SameSite=Strict`) | **Resistiu**: 400 em todos | ataques.mjs §3 |
| 6 | SQL injection | `' OR '1'='1`, `%`, `_`, `PAC-0014' --`, `"; DROP TABLE Pacientes; --` | **Resistiu**: 0 linhas, a tabela fica intacta. O EF parametriza e os curingas do LIKE são escapados | ataques.mjs §5 |
| 7 | Cookie | Id inventado de 43 caracteres; id antigo depois da troca de papel | **Resistiu à fixação e à reutilização** (o servidor emite id novo). **Cedeu no limite de taxa** (SEC-02) | ataques.mjs §3, §9 |
| 8 | Executor | `POST /painel/testes/executar` em Production sem configuração | **Cedeu** (SEC-01): o processo rodou | ataques.mjs §8 |
| 9 | Auditoria | Corrida com recusas simultâneas; os gatilhos `BEFORE UPDATE/DELETE` impedem editar ou apagar via SQL; verificação da cadeia | **Cedeu na concorrência** (SEC-04). Adulterar, remover e reordenar é **detectado** (`CadeiaDeAuditoriaTestes`). Recalcular a cadeia inteira não é detectável (ver G-03) | `SEC04_*`; `CadeiaDeAuditoriaTestes` |
| 10 | Leitores HL7 e ASTM | Fuzz com 15.000 entradas, quadros mutados, fora de ordem, truncados, checksum errado, controle e Unicode, valores não finitos | **Cedeu nos não finitos** (SEC-08). Sem exceção inesperada nem laço | `ProtocolosHostisTestes` |
| 11 | Negação de serviço controlada | Inundação de sessões (100 em 52 s, com medição de memória), corpo acima de 16 KB, rajada acima do limite, bloqueio por IP | **Cedeu** em SEC-02, SEC-03 e SEC-09. Memória contida pelo teto. 413 antes de processar | medição: +44 MB por 100 sessões; ataques.mjs §7, §9 |
| 12 | Exposição de informação | Erros 500, 404 e 405, cabeçalhos `Server` e `X-Powered-By`, entradas extremas procurando `Exception`, `at Bancada.` e `\Users\` | **Resistiu** (página genérica, sem stack trace nem caminho). O 405 virava 500 (SEC-05) | ataques.mjs §1, §7; `ExcecaoNaoTratada_Devolve500SemVazarDetalhe` |
| 13 | Arquivos internos | `/appsettings.json`, `/Bancada.Web.dll`, `/web.config`, `/.git/config`, `/painel-instantaneo.json`, `%2e%2e`, `..%2f`, `/js/../../`, `/obj/`, `/bin/`; visualizador de código com `../../appsettings.json` | **Resistiu**: nenhum arquivo servido. O visualizador lê só recursos embutidos (lista fechada) e responde 404 | ataques.mjs §6 |
| 14 | Contornar regras de negócio | Por POST direto: validar sendo quem enviou (o botão está desabilitado na tela); validar crítico sem confirmar; delta sem justificativa e com justificativa só de espaços; liberar com pendência; validar duas vezes; **duas liberações simultâneas**; retificar sem motivo; validar com CQ reprovado (cenário `cq`); verbo DELETE para apagar | **Resistiu em todas**: um laudo só na corrida, a v1 continua igual depois da retificação, a evolução não tem rota de exclusão (DELETE dá 405) | `RegrasSobAtaqueTestes` (3 testes), `SEC05_*` |

---

## E. Correções implementadas

| Arquivo | Alteração | Por quê |
|---|---|---|
| `src/Bancada.Web/Program.cs` | Executor desligado fora de Development; `UseForwardedHeaders` só com proxies configurados | SEC-01, SEC-03 |
| `src/Bancada.Aplicacao/ServicosTecnicos.cs` | `LimitesDoExecutor.Habilitado` passa a ser falso por padrão | SEC-01 |
| `src/Bancada.Web/LimitesDeTaxa.cs` | A partição exige sessão viva e usa a chave do cliente | SEC-02, SEC-03 |
| `src/Bancada.Web/Registro.cs` | `CabecalhosEncaminhados` (IPs e CIDRs, falha fechado) | SEC-03 |
| `src/Bancada.Web/Seguranca.cs` | `ChaveDoCliente` (IPv6 /64, IPv4 mapeado), poda do bloqueio, eventos pela fila | SEC-03, SEC-09, SEC-04 |
| `src/Bancada.Aplicacao/Sessao.cs` | Vez da sessão (`Entrar`, `TentarEntrar`, `Sair`, `SairSeNadaPendente`), fila de eventos com teto, descarte adiado | SEC-04, SEC-07 |
| `src/Bancada.Aplicacao/ServicoDeAuditoria.cs` | `Anotar`, `Descarregar`, `SairDescarregando` | SEC-04 |
| `src/Bancada.Web/SessaoMiddleware.cs` | Usa a vez da sessão, troca a sessão descartada e grava `SessaoCriada` dentro da vez | SEC-04, SEC-07 |
| `src/Bancada.Infraestrutura/ProcessosDoSistema.cs` | Ambiente por lista de permissão | SEC-06 |
| `src/Bancada.Web/Controllers/ErroController.cs`, `Recursos/textos.json` | 405 e 413, mais mapeamento por classe (textos `erro.405` e `erro.413` em pt e en) | SEC-05 |
| `src/Bancada.Protocolos/Astm.cs`, `Hl7.cs` | `double.IsFinite` | SEC-08 |
| `.github/workflows/ci.yml`, `.github/dependabot.yml` | Permissões mínimas, SHA, Dependabot | SEC-10 |
| `Dockerfile`, `.dockerignore` | Digest, `DOTNET_EnableDiagnostics=0`, contexto enxuto | SEC-11 |
| `README.md`, `vault/07-Plano/Checklists-por-Fase.md` | Configurações novas e registro da auditoria | documentação |

**Testes adicionados:**
- `tests/Bancada.Testes.Web/AtaquesTestes.cs`, com as classes `AtaquesTestes` e `RegrasSobAtaqueTestes`.
- `SEC04`/`SEC07` em `SessaoTestes`, `Executor_PorPadrao_NasceDesligado`.
- `SEC06` em `InfraDaSessaoTestes`.
- `ProtocolosHostisTestes` e casos não finitos.
- `SEC02`/`SEC03` em `LimiteDeTaxaTestes`.
- `SEC10`/`SEC11` em `ContainerETestes`.
- Casos 405, 413, 418, 503 e 302 na página de erro.
- Roteiro caixa-preta em `tools/seguranca/ataques.mjs`, que só aceita alvo local.

As regras de negócio não mudaram: nenhuma validação foi removida, nenhum recurso foi desligado para passar teste, e a cobertura continua 100%. A única mudança visível é o executor do painel técnico, que agora aparece "desligado" em produção, a menos que seja ligado de propósito.

---

## F. Validação pós-correção

**Ataques repetidos**
- `tools/seguranca/ataques.mjs`:
  - **Antes:** 8 falhas em 47. Seis eram vulnerabilidades: 5 verbos com 500 e o executor ligado. As outras duas verificações (visualizador de código e corpo grande) caíram no bloqueio de IP disparado pelo próprio roteiro; refeitas numa instância nova, passaram. Nessa repetição apareceu a sexta vulnerabilidade: o cookie inventado criou 16 sessões em 16 tentativas.
  - **Depois, na aplicação local:** **0 em 47**.
  - **Depois, no contêiner endurecido:** **0 em 47**.
- Os 9 testes de regressão novos da camada Web **falharam antes** da correção (incluindo o `UNIQUE constraint failed` do SEC-04) e **passam depois**.

**Regressão**
- `tools/verificar.ps1` verde:
  - Restauração travada, formatação e build com avisos como erro.
  - C#: Domínio 130, Protocolos 73, Guardas 36, Infraestrutura 26, Aplicação 114, Web 194 = **573**.
  - JS: **39**.
  - **100% de linhas e ramos** em todos os projetos.
  - Nenhum pacote vulnerável (inclusive os transitivos).
- O Playwright em modo escuro conferiu o XSS armazenado.
- A suíte de ataques roda no `dotnet test` do CI, então entra a cada push.

**Build e execução do Docker**
- A imagem foi construída e roda com: `--read-only`, `--tmpfs /tmp:rw,noexec,nosuid`, `--cap-drop ALL`, `--security-opt no-new-privileges`, `--memory 512m`, `--cpus 1`, `--pids-limit 200`.
- O processo roda com uid 1654 (não root).
- `docker exec … sh` falha porque não há shell na imagem: escape trivial indisponível.
- A aplicação funciona com o sistema de arquivos só de leitura. A chave de proteção de dados fica efêmera, o que é esperado com sessões em memória.

**Dependências**
- `dotnet list package --vulnerable --include-transitive`: limpo. Os pacotes estão no último patch do 9.0 (9.0.22).
- **Trivy** na imagem: pacotes .NET limpos. Há 2 CVEs **médias** na glibc do Ubuntu 24.04 (CVE-2026-18374 e CVE-2026-89092), **sem versão corrigida** publicada (ver G-02).
- JavaScript: **nenhuma dependência** (o `package.json` só tem o script de teste).

**Segredos**
- **gitleaks** no histórico do git: "no leaks found".
- Nos arquivos atuais, só `.vs/` (configuração local do Visual Studio, ignorada pelo git e pelo `.dockerignore`, nunca versionada).

---

## G. Riscos residuais

| ID | Risco | Gravidade | Tratamento |
|---|---|---|---|
| G-01 | **.NET 9 termina o suporte em 10/11/2026.** A partir daí, falhas novas no runtime e no ASP.NET Core não recebem correção | **ALTA** (risco aceito) | **Aceito pelo dono em 07/11/2026:** o projeto fica no .NET 9. Mitigação: manter o último patch 9.0.x e a imagem base atualizada (Dependabot). Se um dia migrar para o .NET 10 LTS, `KnownNetworks` vira `KnownIPNetworks` |
| G-02 | 2 CVEs médias na glibc da imagem base, sem correção publicada | MÉDIA | A aplicação não chama `fopen` com modo vindo de fora e não usa `nscd`. O Dependabot avisa quando sair o digest corrigido; reconstruir a imagem |
| G-03 | A cadeia de auditoria usa SHA-256 **sem chave**: ela **detecta** adulteração parcial, mas quem tiver escrita no armazenamento pode recalcular a cadeia inteira | BAIXA | O armazenamento é a memória do processo, e quem chega nela já tem tudo (inclusive a chave da sessão), então HMAC não mudaria o quadro. Para auditoria real: HMAC com chave fora do processo ou ancoragem externa dos hashes |
| G-04 | Papéis são autoatribuídos (todo visitante pode ser Admin) | INFORMATIVA (por desenho) | A fronteira é a sessão. Nada que o Admin faz sai da própria sessão. O executor, que era a exceção, agora vem desligado |
| G-05 | Um atacante com **muitos IPs** (ou muitos /64) ainda enche o teto de 200 sessões e expulsa os mais ociosos | MÉDIA | Limite por IP e /64 já aplicado. Contra ataque distribuído: limite na borda (WAF ou proxy da hospedagem) e alerta de saturação do teto |
| G-06 | Uma única instância: sessões, chaves de proteção de dados e limites ficam em memória | INFORMATIVA | Escalar horizontalmente exigiria sessão compartilhada e chaves persistidas. Mantenha **1 instância** |
| G-07 | HTTPS, redirecionamento HTTP para HTTPS e o certificado dependem da hospedagem. O HSTS é enviado, mas só vale sobre HTTPS | Depende do ambiente | Checklist H |
| G-08 | CSP sem `require-trusted-types-for` e sem `upgrade-insecure-requests` | INFORMATIVA | O JS da Bancada já não tem sinks HTML. O do portfólio usa `innerHTML` com constantes, então Trusted Types exigiria reescrever os gráficos do portfólio. `upgrade-insecure-requests` quebraria o modo local em HTTP |
| G-09 | O dado associado da cifra do motivo do agendamento liga o texto ao **paciente**, não ao agendamento, então cifras do mesmo paciente seriam trocáveis por quem escreve no banco | INFORMATIVA | Exige escrita direta no banco (fora do modelo). Trocar o AAD para `agendamento:{id}` numa próxima versão do formato |
| G-10 | Não foram executados: ZAP (proibido pela política de licenças), Burp, CodeQL (precisa ser ligado no GitHub), teste de carga distribuído, teste contra a hospedagem real | — | CodeQL: ativar o "default setup" nas configurações do repositório. Repetir `ataques.mjs` contra o staging autorizado |

---

## H. Checklist de publicação

**Antes de publicar**
- [ ] Publicar **pela imagem do Dockerfile** (ela já leva `Bancada__Executor__Habilitado=false` e `ASPNETCORE_ENVIRONMENT=Production`).
- [ ] **Não** definir `Bancada__CookiesSeguros=false` (isso é só para rodar sem HTTPS na própria máquina).
- [ ] Definir `Bancada__ProxiesConfiaveis` com o IP ou a rede do proxy da hospedagem. Sem isso, todos os visitantes dividem um único limite de taxa (SEC-03). Na dúvida, consulte a documentação da hospedagem sobre o IP do proxy de borda.
- [ ] **HTTPS obrigatório** na hospedagem, com redirecionamento HTTP para HTTPS na borda e certificado automático.
- [ ] Antes de manter `includeSubDomains` no HSTS, confirmar que **todos** os subdomínios do seu domínio servem HTTPS.

**Execução**
- [ ] Apenas **1 instância** (G-06), com a porta 8080 exposta só para o proxy.
- [ ] Health check em `GET /saude`.
- [ ] Limites no contêiner: memória 512 MB, CPU 1, `pids-limit` 200.
- [ ] Se a hospedagem permitir: sistema de arquivos só de leitura com `/tmp` em tmpfs, `cap-drop ALL` e `no-new-privileges`.
- [ ] Nenhum segredo é necessário. Se a hospedagem injetar variáveis próprias, elas não chegam ao executor (SEC-06), mas ele deve continuar desligado.

**Repositório**
- [ ] Ativar o Dependabot (o arquivo já está no repositório), o **CodeQL default setup** e o **secret scanning com push protection** no GitHub.
- [ ] Proteger a branch `main`: exigir o CI verde antes de merge.

**Depois de no ar**
- [ ] Conferir os cabeçalhos em securityheaders.com.
- [ ] Rodar `tools/seguranca/ataques.mjs` **só** contra um staging autorizado. O roteiro recusa alvo que não seja local; para staging, ajuste a trava conscientemente.
- [ ] Monitorar respostas 429 e 5xx e reconstruir a imagem quando o Dependabot propuser um digest novo (G-02).
