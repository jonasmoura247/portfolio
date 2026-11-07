// Gera as chaves ajuda.* de src/Bancada.Web/Recursos/textos.json (texto de ajuda de cada módulo, pt e en).
// Cada módulo: "resumo|passo 1|passo 2|passo 3". Rode: node tools/ajuda-textos.mjs
import { readFileSync, writeFileSync } from 'node:fs';

const A = {
  inicio: [
    'Ponto de partida da Bancada: um laboratório de mentira, feito de verdade. Cada visitante tem o próprio banco de dados, que some quando a sessão expira.|Troque o papel no topo (campo verde) e veja o menu mudar|Siga o roteiro de 5 minutos ou carregue um cenário pronto|Tudo o que você faz entra na auditoria encadeada por hash',
    'The starting point of Bancada: a pretend lab, built for real. Every visitor gets their own database, which disappears when the session expires.|Switch the role at the top (green field) and watch the menu change|Follow the 5-minute tour or load a ready-made scenario|Everything you do goes into the hash-chained audit log'],
  pacientes: [
    'Os 60 pacientes fictícios, com busca por nome ou código e a ficha de cada um.|Digite parte do nome ou o código (por exemplo PAC-0014) e busque|Abra a ficha pelo código do paciente|Na ficha: alergias, agendamentos, pedidos e linha do tempo',
    'The 60 fictional patients, searchable by name or code, with a record for each.|Type part of the name or the code (for example PAC-0014) and search|Open the record from the patient code|On the record: allergies, appointments, orders and timeline'],
  agenda: [
    'A grade semanal de cada médico, com horários de 30 em 30 minutos. Clique em qualquer lugar de uma célula para agendar ou ver o agendamento.|Escolha o médico e a semana|Clique numa célula livre, escolha o paciente e agende|Clique num horário ocupado para ver os dados do paciente, confirmar, cancelar ou reagendar',
    'The weekly grid of each physician, in 30-minute slots. Click anywhere on a cell to book or to see the appointment.|Pick the physician and the week|Click a free cell, choose the patient and book|Click a taken slot to see the patient data, confirm, cancel or reschedule'],
  atendimento: [
    'A fila do dia (ou da semana): a Recepção faz o check-in e o médico do horário atende.|Clique em qualquer lugar da linha para ver o resumo do atendimento|Recepção: Fazer check-in, só no dia do agendamento|Médico: Iniciar atendimento, registrar a evolução e concluir',
    'The queue for the day (or the week): the front desk checks in and the physician of the slot sees the patient.|Click anywhere on a row to see the visit summary|Front desk: Check in, only on the appointment day|Physician: Start the visit, write the note and finish'],
  prontuario: [
    'O histórico clínico do paciente: evoluções, alergias e correções. Uma evolução nunca se apaga: corrige-se com uma nova, ligada à original.|Escolha o paciente|Veja as evoluções (cifradas em repouso) e as alergias|Corrija uma evolução: a original continua visível, encadeada à correção',
    'The clinical history of the patient: notes, allergies and corrections. A note is never erased: you fix it with a new one, linked to the original.|Choose the patient|See the notes (encrypted at rest) and the allergies|Correct a note: the original stays visible, chained to the correction'],
  pedido: [
    'Os pedidos de exame. O catálogo agrupa os exames em tubos por material e avisa sobre as alergias do paciente.|Clique em Novo pedido e escolha o paciente|Marque os exames: os tubos e o jejum aparecem ao lado|Envie o pedido. Clique em qualquer lugar da linha para ver o detalhe',
    'Test orders. The catalog groups tests into tubes by material and warns about the allergies of the patient.|Click New order and choose the patient|Tick the tests: tubes and fasting show up beside them|Submit the order. Click anywhere on a row to see the detail'],
  coleta: [
    'A coleta dos tubos pedidos, com conferência de jejum e volume antes de gerar a etiqueta.|Em Tubos a coletar, confira o jejum e o volume|Clique em Coletar: a etiqueta Code 128 é gerada|Amostra com problema? Rejeite com um motivo e o pedido volta para recoleta',
    'Collection of the ordered tubes, checking fasting and volume before the label is generated.|Under Tubes to collect, check fasting and volume|Click Collect: the Code 128 label is generated|Bad sample? Reject it with a reason and the order goes back for recollection'],
  etiquetas: [
    'A consulta das etiquetas de amostra já geradas. Procure e abra a etiqueta para ver o código de barras e imprimir.|Digite o código da etiqueta (AMO-000001), o código ou o nome do paciente|Clique em qualquer lugar da linha ou em Ver etiqueta|Use Imprimir para a versão em papel',
    'Lookup of the sample labels already generated. Search and open a label to see the barcode and print it.|Type the label code (AMO-000001), or the patient code or name|Click anywhere on the row or on View label|Use Print for the paper version'],
  soroteca: [
    'O mapa das caixas da soroteca, onde as amostras ficam guardadas, com validade e cadeia de custódia.|Escolha a caixa e clique numa posição|Posição livre: armazene uma amostra que está aguardando|Posição ocupada: veja a custódia, mova ou descarte a amostra',
    'The box map of the serum bank, where samples are stored, with expiry and chain of custody.|Choose the box and click a position|Free position: store a waiting sample|Taken position: see the custody trail, move or discard the sample'],
  equipamento: [
    'A fila dos equipamentos, que conversam em ASTM e HL7. Envie amostras, acompanhe o progresso e reprocesse as falhas.|Envie ao equipamento uma amostra pronta|Acelere o relógio do laboratório (x60, x600) para ver o processamento|Execução com erro: use Reprocessar',
    'The instrument queue; they talk ASTM and HL7. Send samples, follow the progress and reprocess failures.|Send a ready sample to the instrument|Speed up the lab clock (x60, x600) to watch the processing|Failed run: use Reprocess'],
  qualidade: [
    'O controle de qualidade, com gráfico de Levey-Jennings e regras de Westgard. Controle fora das regras bloqueia a liberação.|Veja o gráfico e a média do controle|Registre um novo controle|Violou uma regra (por exemplo 2-2s)? A validação fica bloqueada até um controle aprovado',
    'Quality control, with a Levey-Jennings chart and Westgard rules. A control outside the rules blocks release.|See the chart and the mean of the control|Record a new control|Broke a rule (for example 2-2s)? Validation stays blocked until a control passes'],
  resultados: [
    'Validação e laudos. Um resultado só vale depois de validado por outro operador, e o laudo liberado não muda.|Analista: valide cada resultado (crítico pede confirmação, delta suspeito pede justificativa)|Quem enviou a amostra não valida: troque o operador no topo|Com tudo validado, libere o laudo. Correção é por retificação',
    'Validation and reports. A result only counts once validated by another operator, and a released report never changes.|Analyst: validate each result (critical asks for confirmation, suspicious delta asks for a reason)|Whoever sent the sample cannot validate it: switch the operator at the top|With everything validated, release the report. Fixes go through amendment'],
  auditoria: [
    'O registro, encadeado por hash, de tudo o que acontece na sessão. Mexeu em algo? A cadeia quebra.|Filtre por grupo|Clique em Verificar para recalcular a cadeia|Simule uma adulteração e veja a linha quebrada',
    'The hash-chained record of everything that happens in the session. Tampered with something? The chain breaks.|Filter by group|Click Verify to recompute the chain|Simulate tampering and see the broken row'],
  operacao: [
    'O painel operacional: tempo de resposta por exame, ocupação da soroteca e fila dos equipamentos.|Veja os números no topo|Compare o tempo de cada exame|Os gráficos acompanham o que você faz nas outras telas',
    'The operations board: response time per test, serum bank occupancy and the instrument queue.|Read the numbers at the top|Compare the time of each test|The charts follow what you do on the other screens'],
  painel: [
    'O painel técnico: arquitetura, cobertura de testes, protocolos e o executor que roda os testes de verdade.|Navegue pelas abas|Veja a cobertura de linhas e ramos|Rode os testes e acompanhe a saída ao vivo',
    'The technical panel: architecture, test coverage, protocols and the runner that really runs the tests.|Browse the tabs|See line and branch coverage|Run the tests and watch the output live'],
};

const EXTRAS = {
  'ajuda.botao': ['Ajuda', 'Help'],
  'ajuda.titulo': ['Ajuda', 'Help'],
  'ajuda.comoFazer': ['Como fazer', 'How to'],
  'ajuda.videoRotulo': ['Vídeo de exemplo, sem áudio', 'Example video, no audio'],
};

const arquivo = 'src/Bancada.Web/Recursos/textos.json';
const bruto = readFileSync(arquivo, 'utf8');
const eol = bruto.includes('\r\n') ? '\r\n' : '\n';
const itens = new Map(Object.entries(JSON.parse(bruto)));
for (const chave of [...itens.keys()]) if (chave.startsWith('ajuda.')) itens.delete(chave);
for (const [id, [pt, en]] of Object.entries(A)) {
  const [resumoPt, ...passosPt] = pt.split('|');
  const [resumoEn, ...passosEn] = en.split('|');
  itens.set(`ajuda.${id}.resumo`, [resumoPt, resumoEn]);
  itens.set(`ajuda.${id}.passos`, [passosPt.join('|'), passosEn.join('|')]);
}
for (const [chave, par] of Object.entries(EXTRAS)) itens.set(chave, par);
const ordenadas = [...itens.entries()].sort(([a], [b]) => (a < b ? -1 : a > b ? 1 : 0));
const linhas = ordenadas.map(([k, v]) => `  ${JSON.stringify(k)}: ${JSON.stringify(v)}`);
writeFileSync(arquivo, `{${eol}${linhas.join(`,${eol}`)}${eol}}${eol}`);
console.log(`${ordenadas.length} chaves`);
