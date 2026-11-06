---
tipo: dominio
projeto: Bancada
atualizado: 2026-11-01
---

# Dados de semente

O que existe em toda sessão nova. Gerado por código determinístico (mesma semente, mesmo banco), nunca de arquivo vindo de fora.

## Volumes

| Entidade | Quantidade | Observação |
|---|---|---|
| Pacientes | 60 | `PAC-0001` a `PAC-0060`; 55% F, 45% M; 18 a 85 anos |
| Profissionais (operadores) | 11 | 4 médicos (`MED-01`..), 2 analistas (`ANA-01`, `ANA-02`), 2 recepção (`REC-01`..), 2 coleta (`COL-01`..), 1 admin (`ADM-01`). Dois analistas são necessários para a separação de deveres |
| Grade de agenda | 4 semanas × 6 profissionais | Horários de 30 min, 08:00–17:00 |
| Agendamentos | ~120 | Mistura de estados, alguns passados |
| Atendimentos | ~40 | Concluídos, com evoluções |
| Evoluções | ~120 | Texto curto, cifrado após a cópia |
| Alergias | ~25 | Em ~20 pacientes |
| Pedidos / itens | ~35 / ~90 | Em estados variados |
| Amostras | ~50 | Várias já na soroteca |
| Soroteca | 2 racks × 4 caixas × 81 posições | ~35% ocupadas, algumas perto de vencer |
| Equipamentos | 3 | [[Equipamentos-Simulados]] |
| Medições de CQ | 20 por equipamento e nível | Séries estáveis, com 1 desvio isolado |
| Eventos de auditoria | gerados pelo próprio povoamento | Cadeia válida desde o início |

## Nomes

Listas **inventadas** no código: 80 prenomes e 80 sobrenomes brasileiros comuns, combinados pela semente. Sem pessoa pública, sem combinação vinda de base real. Teste confere que nenhum nome gerado aparece numa lista de bloqueio (nomes de figuras públicas conhecidas mantida em teste).

Identificação do paciente na tela: **código** (`PAC-0007`) e nome fictício. Sem CPF, telefone, endereço, e-mail ([[Dados-Ficticios-e-LGPD]]).

## Cenários

Cada cenário parte do estado base e aplica mudanças fixas.

| Cenário | Paciente | O que está pronto |
|---|---|---|
| **Fluxo feliz** | `PAC-0001` | Consulta marcada para hoje; todos os exames dentro da faixa |
| **Valor crítico** | `PAC-0014` | Pedido ELE na soroteca pronto para processar; potássio sairá 7,1 mmol/L (> 6,5) |
| **Amostra hemolisada** | `PAC-0022` | Tubo coletado marcado hemolisado; recebimento deve rejeitar e pedir recoleta |
| **Equipamento com erro** | `PAC-0031` | EQ-BIO-01 falha na 1ª tentativa e passa na 2ª |
| **CQ fora** | `PAC-0040` | Controle do EQ-BIO-01 viola 2-2s; liberação bloqueada |
| **Alergia no pedido** | `PAC-0047` | Alergia a látex registrada; pedido mostra alerta |

## Regras de geração

1. Tudo passa por `ISorteador` com semente fixa (`Bancada:Semente`, padrão `20261101`).
2. Datas relativas a `IRelogio.Agora` (a semente "anda" com o tempo, para a agenda nunca ficar toda no passado).
3. Cada cenário tem teste que monta o estado e confere os fatos listados acima.
4. O modelo semeado é criado **uma vez** na subida e só copiado depois ([[Sessao-e-Isolamento]]).

## Relacionadas

[[Catalogo-de-Exames]] · [[Personas-e-Jornada]] · [[Modelo-de-Dados]]
