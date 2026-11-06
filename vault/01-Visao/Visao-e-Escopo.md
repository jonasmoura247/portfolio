---
tipo: visao
projeto: Bancada
atualizado: 2026-11-01
---

# Visão e escopo

## Para que serve

Peça de portfólio para vagas de desenvolvedor .NET pleno. Em cinco minutos, um recrutador ou tech lead deve conseguir:

1. **Usar** o sistema: seguir a jornada de um exame do agendamento ao laudo.
2. **Ver** como foi feito: arquitetura, banco, segurança, licenças.
3. **Provar**: rodar os testes e ver a cobertura.

## Por que laboratório

É o domínio em que o Jonas já trabalhou: prontuário, agenda, faturamento TISS e SADT na Moderna/Benner; integração de equipamentos médicos por rede e porta serial na C4. O projeto mostra o conhecimento de domínio, não só código.

## Dentro do escopo

- Cadastro e consulta de pacientes fictícios.
- Agenda e agendamento; check-in e atendimento.
- Prontuário (evoluções, alergias, histórico).
- Pedido de exame, coleta e etiqueta com código de barras.
- Soroteca (racks, caixas, posições, validade, descarte).
- Equipamento simulado que processa o exame (protocolo de verdade, relógio acelerado).
- Resultado, validação em duas etapas e laudo.
- Painel técnico com testes e cobertura executáveis.
- Perfis simulados (recepção, médico, coleta, analista, admin) para mostrar autorização.

## Fora do escopo (de propósito)

| Item | Por quê |
|---|---|
| Login real, senha, recuperação de senha | Sem credencial não existe vazamento de credencial. Menos superfície de ataque. |
| Upload de arquivos | Maior fonte de ataque em app web. Nada a ganhar na demo. |
| E-mail, SMS, integração externa | Sem saída de rede, sem SSRF, sem custo. |
| Dados reais de qualquer pessoa | LGPD e bom senso. Ver [[Dados-Ficticios-e-LGPD]]. |
| Apoio à decisão clínica | Não é dispositivo médico. Aviso visível em toda tela. |
| Pagamento, planos, multiusuário persistente | Não agrega ao que o portfólio precisa provar. |

## Critérios de sucesso

- Uma pessoa completa a jornada guiada em até 5 minutos sem instrução externa.
- `dotnet test` e `node --test` passam com 100% de linhas e ramos; o build falha abaixo disso.
- Teste de auditoria confirma que toda dependência está na lista de licenças aceitas.
- Painel técnico roda a suíte completa em menos de 2 minutos.
- Todos os casos de abuso AC-1 a AC-12 do [[Modelo-de-Ameacas]] têm teste e passam.
- Layout usável em celular (400 px).

## Relacionadas

[[Personas-e-Jornada]] · [[Modulos]] · [[Roadmap-Fases]]
