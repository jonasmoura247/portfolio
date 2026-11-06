import * as L from './logica.js';
const { esc, fmt } = L;
const $ = (s, r = document) => r.querySelector(s);
const $$ = (s, r = document) => [...r.querySelectorAll(s)];

/* ================= constantes ================= */
const AGORA = { dia: 2, min: 615 }; // quarta 04/11/2026, 10:15
const DIAS = ['Seg 02', 'Ter 03', 'Qua 04', 'Qui 05', 'Sex 06'];
const SLOTS = Array.from({ length: 8 }, (_, i) => { const m = 480 + i * 30; return String(Math.floor(m / 60)).padStart(2, '0') + ':' + String(m % 60).padStart(2, '0'); });
const passado = (d, i) => d < AGORA.dia || (d === AGORA.dia && 480 + i * 30 < AGORA.min);
const op = id => L.OPERADORES.find(o => o.id === id);
const pac = id => L.PACIENTES.find(p => p.id === id);
const chave = e => `${e.prof}|${e.dia}|${e.slot}`;
const PODE = {
  agendar: ['Recepcao', 'Admin'], checkin: ['Recepcao', 'Admin'], atender: ['Medico', 'Admin'], evolucao: ['Medico'],
  alergia: ['Medico', 'Coleta', 'Admin'], criarPedido: ['Medico', 'Admin'], coletar: ['Coleta', 'Admin'],
  rejeitar: ['Coleta', 'Analista', 'Admin'], armazenar: ['Coleta', 'Analista', 'Admin'], descartar: ['Analista', 'Admin'],
  equipamento: ['Analista', 'Admin'], validar: ['Analista', 'Admin'], auditoria: ['Analista', 'Admin'],
};
const SELO = { Agendado: 's-neutro', Confirmado: 's-acento', ChegouCheckIn: 's-ok', EmAtendimento: 's-atencao', Concluido: 's-neutro', Cancelado: 's-neutro', Faltou: 's-perigo', Solicitado: 's-neutro', Coletado: 's-acento', Coletada: 's-acento', Armazenada: 's-teal', EmAnalise: 's-atencao', NaFila: 's-neutro', Processando: 's-atencao', Erro: 's-perigo', Preliminar: 's-atencao', CriticoConfirmado: 's-violeta', Validado: 's-ok', Liberado: 's-ok', Rejeitada: 's-perigo', Descartada: 's-neutro', ResultadoPreliminar: 's-atencao', Analisada: 's-neutro', Ok: 's-ok', Recusado: 's-perigo', Bloqueado: 's-perigo' };
const NOME = { ChegouCheckIn: 'Check-in feito', EmAtendimento: 'Em atendimento', EmAnalise: 'Em análise', NaFila: 'Na fila', CriticoConfirmado: 'Crítico confirmado', ResultadoPreliminar: 'Resultado preliminar', Concluido: 'Concluído', Coletada: 'Coletada', Rejeitada: 'Rejeitada' };
const selo = e => `<span class="selo ${SELO[e] || 's-neutro'}">${NOME[e] || e}</span>`;
const flagHTML = f => ({ N: '<span class="flag f-n">N</span>', L: '<span class="flag f-a">L ▼</span>', H: '<span class="flag f-a">H ▲</span>', CL: '<span class="flag f-c">CL ▼▼</span>', CH: '<span class="flag f-c">CH ▲▲</span>' }[f]);
const novoId = () => Array.from(crypto.getRandomValues(new Uint8Array(8)), b => b.toString(16).padStart(2, '0')).join('');
const T0 = Date.now();
const agoraISO = () => new Date(Date.UTC(2026, 10, 4, 13, 15, 0) + (Date.now() - T0)).toISOString();
const hhmmss = iso => new Date(new Date(iso).getTime() - 3 * 3600e3).toISOString().slice(11, 19);
const horaSim = () => { const t = Math.floor((AGORA.min + S.relogio.min) * 60); return [Math.floor(t / 3600) % 24, Math.floor(t / 60) % 60, t % 60].map(n => String(n).padStart(2, '0')).join(':'); };

/* ================= estado ================= */
function baseSoroteca() {
  const m = {}, vd = new Set(['2-3', '4-6', '6-1', '7-7']), vc = new Set(['3-4', '8-2']);
  let k = 20;
  for (let r = 0; r < 9; r++) for (let c = 0; c < 9; c++) {
    const id = r + '-' + c;
    if (((r * 5 + c * 3) % 7) < 3 || vd.has(id) || vc.has(id)) m[id] = { est: vc.has(id) ? 'vc' : vd.has(id) ? 'vd' : 'ok', et: 'AMO-' + String(++k).padStart(6, '0'), pac: 'PAC-00' + String(10 + c).padStart(2, '0'), base: true };
  }
  return m;
}
function novoEstado(cenario = 'critico') {
  const e = (prof, dia, slot, p, estado) => ({ prof, dia, slot, pac: p, estado });
  const s = {
    cenario, papel: 'Recepcao', operador: 'REC-01', tela: 'inicio', sessaoId: novoId(), tour: new Set(), subPainel: 'geral', busca: '', pacSel: null,
    agProf: 'MED-01', filaDia: 'hoje', atendAberto: null, pacProt: 'PAC-0014', corrigindo: null, pedPac: 'PAC-0014', pedBusca: '', carrinho: new Set(), pedUrgente: false,
    posSel: null, mover: null, protEq: 'todos', laudoAberto: null, simAud: null, ataque: { cat: 'SQL injection', alvo: 'Busca de pacientes', res: null },
    agenda: [
      e('MED-01', 0, 0, 'PAC-0003', 'Concluido'), e('MED-01', 0, 2, 'PAC-0021', 'Concluido'), e('MED-01', 1, 1, 'PAC-0008', 'Faltou'), e('MED-01', 1, 4, 'PAC-0030', 'Confirmado'),
      e('MED-01', 2, 1, 'PAC-0005', 'Concluido'), e('MED-01', 2, 3, 'PAC-0014', 'EmAtendimento'), e('MED-01', 2, 5, 'PAC-0044', 'Confirmado'), e('MED-01', 2, 6, 'PAC-0017', 'Agendado'),
      e('MED-01', 3, 0, 'PAC-0052', 'Agendado'), e('MED-01', 3, 3, 'PAC-0009', 'Confirmado'), e('MED-01', 3, 7, 'PAC-0011', 'Agendado'), e('MED-01', 4, 2, 'PAC-0047', 'Agendado'), e('MED-01', 4, 4, 'PAC-0001', 'Confirmado'),
      e('MED-02', 2, 2, 'PAC-0022', 'ChegouCheckIn'), e('MED-02', 3, 1, 'PAC-0001', 'Agendado'),
    ],
    evolucoes: [
      { id: 1, pac: 'PAC-0014', aut: 'MED-02', quando: '12/06/2026 15:02', texto: 'Queixa de cefaleia leve. Orientada hidratação e retorno em 3 meses.', corrige: null, atend: null },
      { id: 2, pac: 'PAC-0014', aut: 'MED-02', quando: '21/09/2026 14:10', texto: 'Retorno de rotina. Pressão controlada. Mantida conduta.', corrige: null, atend: null },
      { id: 3, pac: 'PAC-0014', aut: 'MED-02', quando: '21/09/2026 14:25', texto: 'Onde se lê "controlada", leia-se "limítrofe, reavaliar em 30 dias".', corrige: 2, atend: null },
    ],
    alergias: { 'PAC-0014': [{ subst: 'Dipirona', reacao: 'Urticária', ativa: true }], 'PAC-0047': [{ subst: 'Látex', reacao: 'Dermatite de contato', ativa: true }] },
    pedidos: [{ cod: 'PED-000045', pac: 'PAC-0014', exames: ['ELE', 'GLI', 'CRE'], estado: 'Solicitado', urgente: false, solicitante: 'MED-01', tubosColetados: [] }],
    amostras: [], execs: [], resultados: [], laudos: [], custodia: [], soroteca: baseSoroteca(), prot: [],
    relogio: { fator: 60, pausado: false, min: 0 }, cq: { z: L.serieInicialCQ() }, auditoria: [], fila: Promise.resolve(),
    contAmostra: 123, contPedido: 46, contExec: 1, contEvo: 4,
    forcados: cenario === 'feliz' ? { POT: 4.5, SOD: 139, GLI: 92, CRE: 1.1 } : { POT: 7.1, SOD: 138, GLI: 108, CRE: 1.4 },
    anteriores: { 'PAC-0014': { POT: 4.4, SOD: 140, GLI: 101, CRE: 1.2 } },
    falharGLI: cenario === 'erro', hemolisadaPed: cenario === 'hemolisada' ? 'PED-000045' : null,
  };
  if (cenario === 'cq') s.cq.z.push(2.4, 2.2);
  return s;
}
let S = novoEstado();

/* ================= auditoria encadeada (SHA-256 de verdade) ================= */
function registrar(acao, alvoTipo = 'Sessao', alvoId = '-', det = {}, resultado = 'Ok') {
  const est = S, papel = S.papel, operador = S.operador;
  est.fila = est.fila.then(async () => {
    const ant = est.auditoria.length ? est.auditoria.at(-1).hash : await L.GENESIS();
    const ev = await L.encadear(ant, { seq: est.auditoria.length + 1, quando: agoraISO(), papel, operador, acao, alvoTipo, alvoId, detalhe: JSON.stringify(det), resultado });
    est.auditoria.push(ev);
    if (est === S && S.tela === 'auditoria' && !document.querySelector('.fundo-modal:not([hidden])')) render();
  });
}

/* ================= utilidades de interface ================= */
function aviso(msg, tipo = '') {
  const d = document.createElement('div'); d.className = 'aviso ' + tipo; d.textContent = msg; $('#avisos').appendChild(d);
  setTimeout(() => d.remove(), tipo === 'erro' ? 6500 : 4200);
}
function exigir(acao, rotulo) {
  if (PODE[acao].includes(S.papel)) return true;
  aviso(`Seu papel (${L.PAPEIS[S.papel]}) não permite: ${rotulo}.`, 'erro'); registrar('AutorizacaoRecusada', 'Acao', acao, {}, 'Recusado'); return false;
}
let aoFecharModal = null;
function abrirModal(titulo, corpo, rodape = '') {
  $('#modal').innerHTML = `<header><h2 id="modal-titulo">${titulo}</h2><button class="btn peq" data-acao="fechar-modal" type="button" aria-label="Fechar">Fechar</button></header><div class="corpo">${corpo}</div>${rodape ? `<footer>${rodape}</footer>` : ''}`;
  $('#fundo-modal').hidden = false;
  const f = $('#modal input:not([type=checkbox]), #modal textarea, #modal select, #modal button.primario'); if (f) f.focus();
}
function fecharModal() { $('#fundo-modal').hidden = true; $('#modal').innerHTML = ''; if (aoFecharModal) { aoFecharModal(); aoFecharModal = null; } }
const erroCampo = (id, msg) => { const c = $('#' + id); if (!c) return; c.classList.add('erro'); let e = c.parentElement.querySelector('.erro-campo'); if (!e) { e = document.createElement('div'); e.className = 'erro-campo'; c.after(e); } e.textContent = msg; };
const limparErros = () => { $$('#modal .erro, #tela .erro').forEach(x => x.classList.remove('erro')); $$('#modal .erro-campo, #tela .erro-campo').forEach(x => x.remove()); };
const alergiasAtivas = id => (S.alergias[id] || []).filter(a => a.ativa);
const faixaAlergia = id => alergiasAtivas(id).length ? `<div class="alerta"><span aria-hidden="true">!</span><div><b>Alergia registrada:</b> ${alergiasAtivas(id).map(a => esc(a.subst)).join(', ')}. O pedido mostra este aviso.</div></div>` : '';
const opcoes = (lista, atual) => lista.map(([v, t]) => `<option value="${esc(v)}"${v === atual ? ' selected' : ''}>${esc(t)}</option>`).join('');
const proxPac = id => { const e = S.agenda.filter(x => x.pac === id && !['Cancelado', 'Concluido', 'Faltou'].includes(x.estado)).sort((a, b) => a.dia - b.dia || a.slot - b.slot)[0]; return e ? `${DIAS[e.dia]} ${SLOTS[e.slot]}` : '—'; };

/* ================= gráficos ================= */
const svg = (w, h, rotulo, corpo) => `<svg viewBox="0 0 ${w} ${h}" role="img" aria-label="${esc(rotulo)}">${corpo}</svg>`;
function figura(t, nota, corpo) { return `<figure class="fig" style="margin:0"><div class="figt"><h3>${t}</h3><span>${nota}</span></div>${corpo}</figure>`; }
function barrasSVG(dados, max, un, passo = 1) {
  const W = 520, esq = 120, dir = 70, topo = 6, lin = 34, h = topo + dados.length * lin + 26, x = v => esq + (W - esq - dir) * v / max;
  let s = '';
  for (let i = 0; i <= max; i += passo) s += `<line class="grade" x1="${x(i)}" x2="${x(i)}" y1="${topo}" y2="${h - 22}"/><text class="mono" x="${x(i)}" y="${h - 6}" text-anchor="middle">${i}</text>`;
  dados.forEach((d, i) => { const y = topo + i * lin + 4; s += `<text class="tx" x="0" y="${y + 15}">${esc(d.n)}</text><rect class="b-${d.c} cresce" x="${esq}" y="${y}" width="${Math.max(x(d.v) - esq, 2)}" height="22" rx="4"><title>${esc(d.n)}: ${d.v} ${un}</title></rect><text class="mono tx" x="${x(d.v) + 8}" y="${y + 15}">${d.v} ${un}</text>`; });
  return svg(W, h, 'Gráfico de barras: ' + dados.map(d => `${d.n} ${d.v} ${un}`).join(', '), s);
}
function roscaSVG(fatias, centro, sub) {
  const total = fatias.reduce((t, f) => t + f.v, 0) || 1, cx = 84, cy = 84, r = 56, C = 2 * Math.PI * r; let acc = 0, s = '';
  fatias.forEach(f => { const len = C * f.v / total, d = Math.max(len - 2, 0); s += `<circle class="fatia s-${f.c}" cx="${cx}" cy="${cy}" r="${r}" stroke-dasharray="${d} ${C - d}" stroke-dashoffset="${-acc}" transform="rotate(-90 ${cx} ${cy})"><title>${esc(f.n)}: ${f.v}</title></circle>`; acc += len; });
  s += `<text class="grande" x="${cx}" y="${cy + 4}" text-anchor="middle">${centro}</text><text class="mono" x="${cx}" y="${cy + 22}" text-anchor="middle">${sub}</text>`;
  fatias.forEach((f, i) => { const y = 34 + i * 30; s += `<rect class="b-${f.k || 'n'}" x="190" y="${y - 10}" width="12" height="12" rx="3"/><text class="tx" x="210" y="${y}">${esc(f.n)}</text><text class="mono" x="210" y="${y + 14}">${f.v} · ${Math.round(100 * f.v / total)}%</text>`; });
  return svg(430, Math.max(168, 40 + fatias.length * 30), 'Rosca: ' + fatias.map(f => `${f.n} ${f.v}`).join(', '), s);
}
const COR = { a: 'a', t: 't', v: 'v', ok: 'ok', at: 'at', pe: 'pe', n: 'n' };
function ljSVG(z, media, dp) {
  const W = 640, H = 300, esq = 62, dir = 14, topo = 14, base = H - 30, n = Math.min(z.length, 24), pts = z.slice(-n);
  const y = zz => topo + (3.4 - zz) / 6.8 * (base - topo), x = i => esq + (W - esq - dir) * (i + 0.5) / n;
  let s = '';
  [[3, 'lj-3'], [2, 'lj-2'], [1, 'lj-1'], [0, 'lj-media'], [-1, 'lj-1'], [-2, 'lj-2'], [-3, 'lj-3']].forEach(([k, c]) => {
    s += `<line class="${c}" x1="${esq}" x2="${W - dir}" y1="${y(k)}" y2="${y(k)}"/><text class="mono" x="${esq - 8}" y="${y(k) + 4}" text-anchor="end">${k === 0 ? 'média' : (k > 0 ? '+' : '−') + Math.abs(k) + 's'}</text><text class="mono" x="${W - dir}" y="${y(k) - 4}" text-anchor="end">${fmt(media + k * dp, 2)}</text>`;
  });
  s += `<polyline class="lj-linha" points="${pts.map((v, i) => `${x(i)},${y(Math.max(-3.3, Math.min(3.3, v)))}`).join(' ')}"/>`;
  pts.forEach((v, i) => { const c = Math.abs(v) > 3 ? 'b-pe' : Math.abs(v) > 2 ? 'b-at' : 'b-a', ult = i === pts.length - 1; s += `<circle class="${c}" cx="${x(i)}" cy="${y(Math.max(-3.3, Math.min(3.3, v)))}" r="${ult ? 6 : 4}"><title>Medida ${z.length - n + i + 1}: ${fmt(media + v * dp, 2)} (${v > 0 ? '+' : ''}${fmt(v, 1)}s)</title></circle>`; });
  s += `<text class="mono" x="${esq}" y="${H - 8}">medidas mais antigas</text><text class="mono" x="${W - dir}" y="${H - 8}" text-anchor="end">mais recente</text>`;
  return svg(W, H, `Gráfico de Levey-Jennings do potássio, ${pts.length} medidas`, s);
}

/* ================= menu, topo e navegação ================= */
const MENU = [['inicio', 'Início'], ['pacientes', 'Pacientes'], ['agenda', 'Agenda'], ['atendimento', 'Atendimento'], ['prontuario', 'Prontuário'], ['pedido', 'Pedido de exame'], ['coleta', 'Coleta'], ['soroteca', 'Soroteca'], ['equipamento', 'Equipamento'], ['qualidade', 'Qualidade'], ['resultados', 'Resultados e laudos'], ['auditoria', 'Auditoria'], ['|'], ['operacao', 'Operação'], ['painel', 'Painel técnico'], ['ataque', 'Laboratório de ataque']];
const acessa = t => L.ACESSO[S.papel].includes(t);
const pode = a => PODE[a].includes(S.papel);
function contagens() {
  const aColetar = S.pedidos.reduce((n, p) => n + (p.estado === 'Solicitado' ? L.agruparTubos(p.exames).filter(t => !p.tubosColetados.includes(t.material)).length : 0), 0);
  return {
    atendimento: S.agenda.filter(e => e.estado === 'ChegouCheckIn').length, coleta: aColetar,
    soroteca: S.amostras.filter(a => a.estado === 'Coletada' && a.material === 'Soro').length,
    equipamento: S.execs.filter(e => ['NaFila', 'Processando'].includes(e.estado)).length,
    resultados: S.resultados.filter(r => r.estado !== 'Validado').length, criticos: S.resultados.filter(r => r.estado === 'Preliminar' && (r.flag === 'CH' || r.flag === 'CL')).length,
  };
}
function montarTopo() {
  $('#papel').innerHTML = opcoes(Object.entries(L.PAPEIS), S.papel);
  $('#operador').innerHTML = L.OPERADORES.filter(o => o.papel === S.papel).map(o => `<option value="${o.id}"${o.id === S.operador ? ' selected' : ''}>${esc(o.id + ' · ' + o.nome)}</option>`).join('');
  $('#sessao').textContent = 'sessão ' + S.sessaoId.slice(0, 8) + '…';
}
function montarMenu() {
  const c = contagens();
  $('#menu').innerHTML = MENU.filter(([id]) => id === '|' || acessa(id)).map(([id, nome]) => {
    if (id === '|') return '<div class="sep"></div>';
    const n = c[id], crit = id === 'resultados' && c.criticos;
    return `<button type="button" data-acao="ir" data-tela="${id}"${S.tela === id ? ' aria-current="page"' : ''}><span>${id === 'resultados' && S.papel === 'Medico' ? 'Laudos' : nome}</span>${n ? `<span class="n${crit ? ' pe' : ''}">${n}</span>` : ''}</button>`;
  }).join('');
}
const PAPEL_SUGERIDO = { agenda: 'Recepcao', atendimento: 'Medico', prontuario: 'Medico', pedido: 'Medico', coleta: 'Coleta', soroteca: 'Coleta', equipamento: 'Analista', qualidade: 'Analista', resultados: 'Analista', auditoria: 'Analista' };
function trocarPapel(papel) {
  S.papel = papel; S.operador = L.OPERADORES.find(o => o.papel === papel).id; S.sessaoId = novoId();
  registrar('PapelTrocado', 'Sessao', papel); if (!acessa(S.tela)) S.tela = 'inicio';
}
function ir(tela) {
  if (!acessa(tela)) { const p = PAPEL_SUGERIDO[tela]; if (p) { trocarPapel(p); aviso(`Papel trocado para ${L.PAPEIS[p]} para abrir esta tela.`); } else return; }
  S.tela = tela; S.tour.add(tela); render(); $('#tela').focus({ preventScroll: true }); window.scrollTo({ top: 0 });
}
function render() {
  montarTopo(); montarMenu();
  const t = TELAS[S.tela]; $('#tela').innerHTML = t.html();
  if (t.ligar) t.ligar();
}

/* ================= telas ================= */
const TELAS = {};

/* ---- início ---- */
const PASSOS = [['pacientes', 'Escolha um paciente'], ['agenda', 'Agende um horário (Recepção)'], ['atendimento', 'Faça o check-in e atenda (Médico)'], ['pedido', 'Registre a evolução e peça exames'], ['coleta', 'Colete a amostra e gere a etiqueta (Coleta)'], ['soroteca', 'Guarde a amostra na soroteca'], ['equipamento', 'Envie ao equipamento e acompanhe (Analista)'], ['resultados', 'Valide e libere o laudo'], ['painel', 'Confira o painel técnico'], ['ataque', 'Tente quebrar o sistema']];
const CENARIOS = [['feliz', 'Fluxo feliz', 'Todos os valores dentro da faixa.'], ['critico', 'Valor crítico', 'Potássio 7,1 mmol/L (acima de 6,5): exige confirmação.'], ['hemolisada', 'Amostra hemolisada', 'A coleta sai hemolisada: rejeitar e pedir recoleta.'], ['erro', 'Equipamento com erro', 'A glicose falha na 1ª tentativa e passa ao reprocessar.'], ['cq', 'Controle de qualidade fora', 'Regra 2-2s violada: bloqueia a validação.']];
function fluxoSVG() {
  const etapas = [['Agenda', 'agenda'], ['Atend.', 'atendimento'], ['Pedido', 'pedido'], ['Coleta', 'coleta'], ['Soroteca', 'soroteca'], ['Equip.', 'equipamento'], ['Resultado', 'resultados'], ['Laudo', 'painel']];
  const w = 66, g = 6; let s = '';
  etapas.forEach(([n, t], i) => { const x = i * (w + g), feito = S.tour.has(t); s += `<path class="${feito ? 'b-t' : 'caixa'}" d="M${x} 6h${w - 8}l8 14l-8 14h-${w - 8}l8 -14z" ${feito ? 'opacity=".85"' : ''}/><text class="mono ${feito ? '' : 'tx'}" x="${x + w / 2 + 2}" y="24" text-anchor="middle" style="${feito ? 'fill:var(--sobre-acento)' : ''}">${n}</text>`; });
  return svg(etapas.length * (w + g), 40, 'Fluxo do laboratório: ' + etapas.map(e => e[0]).join(', '), s);
}
TELAS.inicio = {
  html: () => `<div class="heroi"><div class="linha-flex"><span class="ponto" aria-hidden="true"></span><span class="mono mut">sessão isolada · banco só seu · expira em 30 min sem uso</span></div>
    <h1>Um laboratório <em>de mentira</em>, feito de verdade</h1>
    <p class="sub">Troque de papel no topo e veja o menu mudar. Siga o roteiro abaixo ou explore: agende, atenda, peça exame, colete, guarde na soroteca, envie ao equipamento, valide e libere o laudo. Toda ação entra numa auditoria encadeada por hash.</p>
    <div class="fig" style="padding:12px;max-width:640px">${fluxoSVG()}</div></div>
    <div class="duas"><div class="painel"><div class="cab"><h3>Roteiro de 5 minutos</h3><span class="mono mut">${S.tour.size ? [...S.tour].filter(t => PASSOS.some(p => p[0] === t)).length : 0}/${PASSOS.length}</span></div><div class="corpo">
      <ul class="passos">${PASSOS.map(([t, n], i) => `<li class="${S.tour.has(t) ? 'feito' : ''}"><span class="marca" aria-hidden="true"></span><span>${i + 1}. ${n}</span><button class="btn peq" data-acao="ir" data-tela="${t}" type="button">Ir</button></li>`).join('')}</ul></div></div>
    <div class="painel"><div class="cab"><h3>Cenários prontos</h3></div><div class="corpo"><div class="cenarios">${CENARIOS.map(([id, n, d]) => `<div class="cenario${S.cenario === id ? ' atual' : ''}"><b>${n}</b><span class="mut" style="font-size:13px">${d}</span><button class="btn peq" data-acao="cenario" data-id="${id}" type="button">${S.cenario === id ? 'Carregado' : 'Carregar'}</button></div>`).join('')}</div></div></div></div>`,
};

/* ---- pacientes ---- */
const tabelaPacientes = () => {
  const q = S.busca.trim().toLowerCase(), lista = L.PACIENTES.filter(p => !q || p.id.toLowerCase().includes(q) || p.nome.toLowerCase().includes(q));
  if (!lista.length) return '<p class="mut" style="padding:16px">Nenhum paciente encontrado para esta busca.</p>';
  return `<div class="rolagem"><table><thead><tr><th>Código</th><th>Nome</th><th class="num-t">Idade</th><th>Sexo</th><th>Próximo horário</th><th></th></tr></thead><tbody>${lista.map(p => `<tr class="${S.pacSel === p.id ? 'sel' : ''}"><td class="mono">${p.id}</td><td>${esc(p.nome)}</td><td class="num-t">${L.idade(p.nasc)}</td><td>${p.sexo}</td><td>${proxPac(p.id)}</td><td><button class="btn peq" data-acao="ficha" data-id="${p.id}" type="button">Abrir ficha</button></td></tr>`).join('')}</tbody></table></div>`;
};
const fichaPaciente = () => {
  const p = S.pacSel && pac(S.pacSel); if (!p) return '';
  const ver = S.papel !== 'Recepcao', ag = S.agenda.filter(a => a.pac === p.id && a.estado !== 'Cancelado').sort((a, b) => a.dia - b.dia || a.slot - b.slot);
  return `<div class="duas"><div class="painel destaque"><div class="cab"><h3>${esc(p.nome)} <span class="mono mut">${p.id}</span></h3></div><div class="corpo pilha">
    <p class="mut">${p.sexo === 'F' ? 'Feminino' : 'Masculino'} · ${L.idade(p.nasc)} anos · nascimento ${p.nasc.split('-').reverse().join('/')}</p>
    ${ver ? (alergiasAtivas(p.id).length ? faixaAlergia(p.id) : '<p class="mut">Sem alergias registradas.</p>') : '<p class="mut">Recepção vê apenas dados cadastrais e horários.</p>'}
    ${acessa('prontuario') ? `<button class="btn" data-acao="abrir-prontuario" data-id="${p.id}" type="button" style="justify-self:start">Abrir prontuário</button>` : ''}</div></div>
    <div class="painel"><div class="cab"><h3>Horários</h3></div><div class="corpo">${ag.length ? `<ul class="tempo">${ag.map(a => `<li><time>${DIAS[a.dia]} ${SLOTS[a.slot]}</time><span>${esc(op(a.prof).nome)} ${selo(a.estado)}</span></li>`).join('')}</ul>` : '<p class="mut">Nenhum horário.</p>'}</div></div></div>`;
};
TELAS.pacientes = {
  html: () => `<div><p class="dev">// pacientes</p><h1>Pacientes <em>fictícios</em></h1><p class="sub">Códigos e nomes inventados; sem CPF, telefone ou endereço.</p></div>
    <div class="painel"><div class="cab"><h3>Lista</h3><input class="campo" id="busca" data-entrada="busca" type="search" placeholder="Buscar por nome ou código" maxlength="64" aria-label="Buscar paciente" value="${esc(S.busca)}"></div><div id="tab-pac">${tabelaPacientes()}</div></div><div id="ficha">${fichaPaciente()}</div>`,
};

/* ---- agenda ---- */
function gradeAgenda() {
  const medicos = L.OPERADORES.filter(o => o.papel === 'Medico');
  return `<div class="painel rolagem"><table class="agenda"><thead><tr><th></th>${DIAS.map(d => `<th>${d}</th>`).join('')}</tr></thead><tbody>${SLOTS.map((h, i) => `<tr><td class="hora">${h}</td>${DIAS.map((_, d) => {
    const e = S.agenda.find(x => x.prof === S.agProf && x.dia === d && x.slot === i);
    if (e) return `<td><button class="btn peq" style="border:0;background:none;padding:0;text-align:left;display:block" data-acao="abrir-agend" data-k="${chave(e)}" type="button"><div class="marc"><b class="mono">${e.pac}</b>${selo(e.estado)}</div></button></td>`;
    if (passado(d, i)) return '<td class="passado">—</td>';
    return pode('agendar') ? `<td class="livre" data-acao="novo-agend" data-dia="${d}" data-slot="${i}" tabindex="0" role="button" aria-label="Agendar ${DIAS[d]} ${h}">livre</td>` : '<td class="mut" style="font-size:12px">livre</td>';
  }).join('')}</tr>`).join('')}</tbody></table></div>`;
}
TELAS.agenda = {
  html: () => `<div class="linha-flex entre"><div><p class="dev">// agenda</p><h1>Agenda · semana de <em>2 a 6 de novembro</em></h1></div>
    <div class="linha-flex"><select class="sel" id="ag-prof" data-muda="agProf" aria-label="Profissional">${L.OPERADORES.filter(o => o.papel === 'Medico').map(o => `<option value="${o.id}"${o.id === S.agProf ? ' selected' : ''}>${esc(o.nome)} (${o.id})</option>`).join('')}</select>
    <button class="btn primario" data-acao="novo-agend" type="button"${pode('agendar') ? '' : ' disabled title="Só Recepção e Admin agendam"'}>Novo agendamento</button></div></div>
    ${pode('agendar') ? '' : `<div class="alerta info"><span aria-hidden="true">i</span><div>Seu papel (${L.PAPEIS[S.papel]}) só consulta a agenda. Troque para Recepção para agendar.</div></div>`}
    ${gradeAgenda()}<div class="legenda"><span class="mut">Hoje é quarta, 04/11, 10:15. Horários antes disso não aceitam agendamento.</span></div>`,
};
function modalAgendar(dia, slot) {
  const livres = [];
  DIAS.forEach((d, di) => SLOTS.forEach((h, si) => livres.push([di + '|' + si, `${d} ${h}${S.agenda.some(x => x.prof === S.agProf && x.dia === di && x.slot === si) ? ' (ocupado)' : passado(di, si) ? ' (passado)' : ''}`])));
  abrirModal('Novo agendamento', `<div class="campos"><label>Paciente<select class="sel" id="m-pac"><option value="">Escolha…</option>${opcoes(L.PACIENTES.map(p => [p.id, `${p.id} · ${p.nome}`]), '')}</select></label>
    <label>Médico<input class="campo" value="${esc(op(S.agProf).nome)}" disabled></label>
    <label>Horário<select class="sel" id="m-hor">${opcoes(livres, dia != null ? dia + '|' + slot : '')}</select></label>
    <label>Motivo (opcional)<textarea class="campo" id="m-motivo" data-entrada="contar" data-max="200" maxlength="400"></textarea><span class="contador" id="m-cont">0 / 200</span></label></div>`,
    `<button class="btn" data-acao="fechar-modal" type="button">Cancelar</button><button class="btn primario" data-acao="salvar-agend" type="button">Agendar</button>`);
}
function abrirAgend(k) {
  const e = S.agenda.find(x => chave(x) === k); if (!e) return;
  const botoes = [];
  if (pode('agendar') && ['Agendado', 'Confirmado'].includes(e.estado)) { if (e.estado === 'Agendado') botoes.push(`<button class="btn primario" data-acao="confirmar-agend" data-k="${k}" type="button">Confirmar</button>`); botoes.push(`<button class="btn perigo" data-acao="cancelar-agend" data-k="${k}" type="button">Cancelar agendamento</button>`); }
  abrirModal('Agendamento', `<p><b>${pac(e.pac).nome}</b> <span class="mono mut">${e.pac}</span></p><p class="mut">${DIAS[e.dia]} ${SLOTS[e.slot]} · ${esc(op(e.prof).nome)}</p><p>${selo(e.estado)}</p>${botoes.length ? '' : `<p class="mut">Nenhuma ação disponível para este estado ou papel.</p>`}`, botoes.join(''));
}

/* ---- atendimento ---- */
function linhasFila() {
  const lista = S.agenda.filter(e => S.filaDia === 'semana' || e.dia === AGORA.dia).sort((a, b) => a.dia - b.dia || a.slot - b.slot);
  if (!lista.length) return '<p class="mut" style="padding:16px">Nenhum horário hoje.</p>';
  return `<div class="rolagem"><table><thead><tr><th>Horário</th><th>Paciente</th><th>Médico</th><th>Estado</th><th></th></tr></thead><tbody>${lista.map(e => {
    let b = '';
    if (['Agendado', 'Confirmado'].includes(e.estado)) b = `<button class="btn peq" data-acao="checkin" data-k="${chave(e)}" type="button"${pode('checkin') ? '' : ' disabled title="Só a Recepção faz check-in"'}>Fazer check-in</button>`;
    else if (e.estado === 'ChegouCheckIn') b = `<button class="btn peq primario" data-acao="iniciar" data-k="${chave(e)}" type="button"${pode('atender') ? '' : ' disabled title="Só o médico inicia"'}>Iniciar atendimento</button>`;
    else if (['EmAtendimento', 'Concluido'].includes(e.estado) && pode('atender')) b = `<button class="btn peq" data-acao="abrir-atend" data-k="${chave(e)}" type="button">Abrir atendimento</button>`;
    return `<tr><td class="mono">${DIAS[e.dia]} ${SLOTS[e.slot]}</td><td>${e.pac} · ${esc(pac(e.pac).nome)}</td><td>${esc(op(e.prof).nome)}</td><td>${selo(e.estado)}</td><td>${b}</td></tr>`;
  }).join('')}</tbody></table></div>`;
}
function painelAtend() {
  const e = S.atendAberto && S.agenda.find(x => chave(x) === S.atendAberto); if (!e || !pode('atender')) return '';
  const p = pac(e.pac), encerrado = e.estado === 'Concluido', meu = op(S.operador).id === e.prof, novas = S.evolucoes.filter(x => x.atend === S.atendAberto);
  const recentes = S.evolucoes.filter(x => x.pac === p.id && !x.atend).slice(-3).reverse();
  const peds = S.pedidos.filter(x => x.pac === p.id);
  return `<div class="painel destaque"><div class="cab"><h3>Atendimento · ${esc(p.nome)} <span class="mono mut">${p.id}</span></h3>${selo(e.estado)}</div><div class="corpo pilha">
    <p class="mut">${p.sexo === 'F' ? 'Feminino' : 'Masculino'} · ${L.idade(p.nasc)} anos · ${DIAS[e.dia]} ${SLOTS[e.slot]} · ${esc(op(e.prof).nome)}</p>${faixaAlergia(p.id)}
    ${meu ? '' : `<div class="alerta info"><span aria-hidden="true">i</span><div>Este atendimento é de <b>${esc(op(e.prof).nome)}</b>. Só o profissional do horário registra e conclui (RN-007). Troque o operador no topo.</div></div>`}
    <div class="duas"><div class="pilha"><h2>Prontuário recente</h2>${recentes.map(x => `<div class="evolucao"><span class="rotulo">${x.quando} · ${esc(op(x.aut).nome)}</span><p>${esc(x.texto)}</p></div>`).join('') || '<p class="mut">Sem registros.</p>'}</div>
    <div class="pilha"><h2>Nova evolução</h2>${encerrado ? '<p class="mut">Atendimento concluído.</p>' : `<div class="campos"><textarea class="campo" id="evo-texto" data-entrada="contar" data-max="2000" maxlength="4000" placeholder="Escreva a evolução" aria-label="Texto da evolução"></textarea><span class="contador" id="m-cont">0 / 2.000</span></div><button class="btn primario" data-acao="registrar-evo" type="button" style="justify-self:start"${pode('evolucao') ? '' : ' disabled title="Só médicos escrevem evoluções"'}>Registrar evolução</button>`}</div></div>
    ${novas.length ? `<h2>Evoluções deste atendimento</h2>${novas.map(x => `<div class="evolucao"><span class="rotulo">${x.quando} · ${esc(op(x.aut).nome)} · cifrada em repouso</span><p>${esc(x.texto)}</p></div>`).join('')}` : ''}
    ${peds.length ? `<h2>Pedidos</h2><div class="linha-flex">${peds.map(x => `<span class="chip a">${x.cod} ${x.exames.join('+')}</span>${selo(x.estado)}`).join(' ')}</div>` : ''}
    ${encerrado ? '' : `<div class="linha-flex"><button class="btn" data-acao="pedir-exame" data-id="${p.id}" type="button">Pedir exame</button><button class="btn primario" data-acao="concluir-atend" type="button">Concluir atendimento</button></div>`}</div></div>`;
}
TELAS.atendimento = {
  html: () => `<div class="linha-flex entre"><div><p class="dev">// atendimento</p><h1>Fila e <em>atendimento</em></h1></div><select class="sel" data-muda="filaDia" aria-label="Período"><option value="hoje"${S.filaDia === 'hoje' ? ' selected' : ''}>Hoje (quarta, 04/11)</option><option value="semana"${S.filaDia === 'semana' ? ' selected' : ''}>Toda a semana</option></select></div>
    <div class="painel"><div class="cab"><h3>Fila</h3><span class="mut" style="font-size:13px">Recepção faz o check-in; o médico do horário atende</span></div>${linhasFila()}</div><div id="atend">${painelAtend()}</div>`,
};

/* ---- prontuário ---- */
TELAS.prontuario = {
  html: () => {
    const p = pac(S.pacProt), lim = S.papel === 'Coleta', ev = S.evolucoes.filter(x => x.pac === p.id).sort((a, b) => b.id - a.id), corrigidas = new Set(S.evolucoes.map(x => x.corrige));
    const al = S.alergias[p.id] || [], peds = S.pedidos.filter(x => x.pac === p.id);
    return `<div class="linha-flex entre"><div><p class="dev">// prontuário</p><h1>Prontuário · <em>${esc(p.nome)}</em></h1></div><select class="sel" data-muda="pacProt" aria-label="Paciente">${opcoes(L.PACIENTES.map(x => [x.id, `${x.id} · ${x.nome}`]), p.id)}</select></div>
    ${lim ? '<div class="alerta info"><span aria-hidden="true">i</span><div>Coleta vê somente alergias e jejum, não as evoluções.</div></div>' : ''}
    <div class="duas"><div class="painel"><div class="cab"><h3>Alergias</h3><span class="selo s-violeta">texto cifrado</span></div><div class="corpo pilha">
      ${al.length ? `<div class="rolagem"><table><thead><tr><th>Substância</th><th>Reação</th><th>Estado</th><th></th></tr></thead><tbody>${al.map((a, i) => `<tr><td>${esc(a.subst)}</td><td>${esc(a.reacao)}</td><td>${a.ativa ? selo('Confirmado').replace('Confirmado', 'Ativa') : '<span class="selo s-neutro">Inativa</span>'}</td><td>${PODE.alergia.includes(S.papel) ? `<button class="btn peq" data-acao="alt-alergia" data-i="${i}" type="button">${a.ativa ? 'Inativar' : 'Reativar'}</button>` : ''}</td></tr>`).join('')}</tbody></table></div>` : '<p class="mut">Sem alergias registradas.</p>'}
      ${PODE.alergia.includes(S.papel) ? `<div class="campos"><label>Substância<input class="campo" id="al-sub" maxlength="80"></label><label>Reação (opcional)<input class="campo" id="al-rea" maxlength="160"></label></div><button class="btn" data-acao="add-alergia" type="button" style="justify-self:start">Adicionar alergia</button>` : ''}</div></div>
    ${lim ? '' : `<div class="painel"><div class="cab"><h3>Exames anteriores</h3></div><div class="corpo">${peds.length ? `<div class="rolagem"><table><tbody>${peds.map(x => `<tr><td class="mono">${x.cod}</td><td>${x.exames.join(', ')}</td><td>${selo(x.estado)}</td></tr>`).join('')}</tbody></table></div>` : '<p class="mut">Nenhum pedido para este paciente.</p>'}</div></div>`}</div>
    ${lim ? '' : `<div class="painel"><div class="cab"><h3>Evoluções</h3><span class="mut" style="font-size:13px">imutáveis: a correção cria uma nova ligada à original</span></div><div class="corpo pilha">${ev.map(x => `<div class="evolucao${corrigidas.has(x.id) ? ' corrigida' : ''}"><span class="rotulo">${x.quando} · ${esc(op(x.aut).nome)}${x.corrige ? ` · <span class="s-violeta">correção da evolução #${x.corrige}</span>` : ''}${corrigidas.has(x.id) ? ' · corrigida' : ''}</span><p>${esc(x.texto)}</p>
      ${S.corrigindo === x.id ? `<div class="campos"><textarea class="campo" id="evo-corr" data-entrada="contar" data-max="2000" maxlength="4000" aria-label="Texto da correção"></textarea><span class="contador" id="m-cont">0 / 2.000</span><div class="linha-flex"><button class="btn primario peq" data-acao="salvar-correcao" data-id="${x.id}" type="button">Registrar correção</button><button class="btn peq" data-acao="cancelar-correcao" type="button">Cancelar</button></div></div>` : (PODE.evolucao.includes(S.papel) && !corrigidas.has(x.id) ? `<div><button class="btn peq" data-acao="corrigir" data-id="${x.id}" type="button">Corrigir</button></div>` : '')}</div>`).join('') || '<p class="mut">Sem evoluções.</p>'}</div></div>`}`;
  },
};

/* ---- pedido ---- */
const tubosCarrinho = () => L.agruparTubos([...S.carrinho]);
function catalogoHTML() {
  const q = S.pedBusca.trim().toLowerCase(), pd = pode('criarPedido');
  return Object.entries(L.EXAMES).filter(([c, e]) => !q || c.toLowerCase().includes(q) || e.nome.toLowerCase().includes(q)).map(([c, e]) =>
    `<label><input type="checkbox" data-acao="toggle-ex" data-c="${c}"${S.carrinho.has(c) ? ' checked' : ''}${pd ? '' : ' disabled'}><span><b class="mono">${c}</b> ${esc(e.nome)}</span><span class="mut" style="font-size:12px">${e.material}${e.jejum ? ` · jejum ${e.jejum} h` : ''}</span></label>`).join('') || '<p class="mut" style="padding:12px">Nenhum exame encontrado.</p>';
}
function carrinhoHTML() {
  const tubos = tubosCarrinho();
  if (!tubos.length) return '<p class="mut">Escolha exames no catálogo. Eles aparecem aqui agrupados em tubos.</p>';
  return `<div class="pilha">${tubos.map((t, i) => `<div class="tubo"><b>Tubo ${i + 1} · ${t.material}</b><span class="mono">${t.exames.join(' + ')}</span><span class="mut" style="font-size:13px">volume ${fmt(t.vol, 1)} mL · jejum ${t.jejum ? t.jejum + ' h' : 'não exige'}</span></div>`).join('')}
    <label class="linha-flex" style="font-size:14px"><input type="checkbox" data-acao="urgente"${S.pedUrgente ? ' checked' : ''}> Urgente (passa na frente na fila do equipamento)</label>
    <button class="btn primario" data-acao="enviar-pedido" type="button"${pode('criarPedido') ? '' : ' disabled'}>Enviar pedido</button></div>`;
}
TELAS.pedido = {
  html: () => `<div class="linha-flex entre"><div><p class="dev">// pedido de exame</p><h1>Pedido de <em>exame</em></h1></div><select class="sel" data-muda="pedPac" aria-label="Paciente">${opcoes(L.PACIENTES.map(x => [x.id, `${x.id} · ${x.nome}`]), S.pedPac)}</select></div>
    ${pode('criarPedido') ? '' : `<div class="alerta info"><span aria-hidden="true">i</span><div>Seu papel (${L.PAPEIS[S.papel]}) só consulta pedidos. Troque para Médico para criar.</div></div>`}
    <div id="alergia-ped">${faixaAlergia(S.pedPac)}</div>
    <div class="duas"><div class="painel"><div class="cab"><h3>Catálogo</h3><input class="campo" data-entrada="pedBusca" type="search" placeholder="Buscar exame" maxlength="64" aria-label="Buscar exame" value="${esc(S.pedBusca)}"></div><div class="catalogo" id="catalogo">${catalogoHTML()}</div></div>
    <div class="painel"><div class="cab"><h3>Carrinho · tubos previstos</h3><span class="mut" style="font-size:13px">${S.carrinho.size}/20 exames</span></div><div class="corpo" id="carrinho">${carrinhoHTML()}</div></div></div>
    <div class="painel"><div class="cab"><h3>Pedidos</h3></div><div class="rolagem"><table><thead><tr><th>Código</th><th>Paciente</th><th>Exames</th><th>Estado</th><th>Solicitante</th></tr></thead><tbody>${[...S.pedidos].reverse().map(p => `<tr><td class="mono">${p.cod}${p.urgente ? ' <span class="selo s-atencao">urgente</span>' : ''}</td><td>${p.pac} · ${esc(pac(p.pac).nome)}</td><td class="mono">${p.exames.join(', ')}</td><td>${selo(p.estado)}</td><td>${esc(op(p.solicitante).nome)}</td></tr>`).join('')}</tbody></table></div></div>`,
};

/* ---- coleta ---- */
function modalColeta(cod, material) {
  const p = S.pedidos.find(x => x.cod === cod), t = L.agruparTubos(p.exames).find(x => x.material === material);
  abrirModal(`Coletar · ${cod} · ${material}`, `<p class="mut">${pac(p.pac).nome} · exames ${t.exames.join(', ')}</p>
    <div class="campos">${t.jejum ? `<label>Jejum cumprido (horas)<input class="campo" id="c-jejum" type="number" min="0" max="72" step="1" placeholder="exige ${t.jejum} h"></label>` : ''}
    <label>Volume coletado (mL)<input class="campo" id="c-vol" type="number" min="0" step="0.1" placeholder="mínimo ${fmt(t.vol, 1)} mL"></label>
    <label class="linha"><input type="checkbox" id="c-hem"${S.hemolisadaPed === cod && material === 'Soro' ? ' checked' : ''}> Amostra hemolisada</label></div>`,
    `<button class="btn" data-acao="fechar-modal" type="button">Cancelar</button><button class="btn primario" data-acao="salvar-coleta" data-cod="${cod}" data-mat="${material}" type="button">Coletar e gerar etiqueta</button>`);
}
function modalEtiqueta(a) {
  abrirModal('Etiqueta da amostra', `<div class="etiqueta">${L.code128SVG(a.etiqueta)}<b>${a.etiqueta}</b><span>${esc(pac(a.pac).nome)} · ${a.pac}</span><span>${a.material} · ${a.pedido}</span><span>Coletada ${a.coletadaEm} · vence ${a.validade}</span></div><p class="mut">Código de barras Code 128 gerado de verdade: ida e volta testada.</p>`, `<button class="btn primario" data-acao="fechar-modal" type="button">Fechar</button>`);
}
TELAS.coleta = {
  html: () => {
    const pend = [];
    S.pedidos.forEach(p => { if (p.estado === 'Solicitado') L.agruparTubos(p.exames).filter(t => !p.tubosColetados.includes(t.material)).forEach(t => pend.push([p, t])); });
    return `<div><p class="dev">// coleta</p><h1>Coleta e <em>etiqueta</em></h1></div>
    <div class="painel"><div class="cab"><h3>A coletar</h3><span class="mut" style="font-size:13px">${pend.length} tubo(s)</span></div>${pend.length ? `<div class="rolagem"><table><thead><tr><th>Pedido</th><th>Paciente</th><th>Tubo</th><th>Exames</th><th>Jejum</th><th></th></tr></thead><tbody>${pend.map(([p, t]) => `<tr><td class="mono">${p.cod}${p.urgente ? ' <span class="selo s-atencao">urgente</span>' : ''}</td><td>${esc(pac(p.pac).nome)}</td><td>${t.material}</td><td class="mono">${t.exames.join('+')}</td><td>${t.jejum ? t.jejum + ' h' : '—'}</td><td><button class="btn peq primario" data-acao="coletar" data-cod="${p.cod}" data-mat="${t.material}" type="button"${pode('coletar') ? '' : ' disabled'}>Coletar</button></td></tr>`).join('')}</tbody></table></div>` : '<p class="mut" style="padding:16px">Nenhum tubo pendente. Peça um exame no módulo Pedido.</p>'}</div>
    <div class="painel"><div class="cab"><h3>Amostras</h3></div>${S.amostras.length ? `<div class="rolagem"><table><thead><tr><th>Etiqueta</th><th>Paciente</th><th>Material</th><th>Volume</th><th>Estado</th><th></th></tr></thead><tbody>${S.amostras.map(a => `<tr><td class="mono">${a.etiqueta}${a.hemolisada ? ' <span class="selo s-perigo">hemolisada</span>' : ''}</td><td>${esc(pac(a.pac).nome)}</td><td>${a.material}</td><td class="num-t">${fmt(a.vol, 1)} mL</td><td>${selo(a.estado)}</td><td class="linha-flex"><button class="btn peq" data-acao="ver-etiqueta" data-et="${a.etiqueta}" type="button">Etiqueta</button>${['Coletada', 'Armazenada'].includes(a.estado) ? `<button class="btn peq perigo" data-acao="rejeitar" data-et="${a.etiqueta}" type="button">Rejeitar</button>` : ''}</td></tr>`).join('')}</tbody></table></div>` : '<p class="mut" style="padding:16px">Nenhuma amostra coletada ainda.</p>'}</div>`;
  },
};

/* ---- soroteca ---- */
const LIN = 'ABCDEFGHI';
function mapaSoroteca() {
  let s = '<div class="ei"></div>' + Array.from({ length: 9 }, (_, c) => `<div class="ei">${c + 1}</div>`).join('');
  for (let r = 0; r < 9; r++) {
    s += `<div class="ei">${LIN[r]}</div>`;
    for (let c = 0; c < 9; c++) { const k = r + '-' + c, o = S.soroteca[k], cls = !o ? '' : o.base ? (o.est === 'ok' ? 'oc' : o.est) : 'nv'; s += `<button type="button" class="pos ${cls}" data-acao="pos" data-k="${k}" aria-pressed="${S.posSel === k}" aria-label="Posição ${LIN[r]}${c + 1}, ${o ? 'ocupada' : 'livre'}">${o ? '●' : ''}</button>`; }
  }
  return s;
}
function detalhePos() {
  const k = S.posSel; if (!k) return '<p class="mut">Clique numa posição do mapa.</p>';
  const [r, c] = k.split('-').map(Number), nome = `${LIN[r]}${c + 1}`, o = S.soroteca[k];
  if (S.mover) return `<div class="alerta info"><span aria-hidden="true">i</span><div>Movendo <b>${S.mover}</b>. Clique numa posição livre.</div></div><button class="btn" data-acao="cancelar-mover" type="button" style="justify-self:start">Cancelar</button>`;
  if (!o) {
    const aguard = S.amostras.filter(a => a.estado === 'Coletada' && a.material === 'Soro');
    return `<p><b>Posição ${nome}</b> livre.</p><p class="mut" style="font-size:13px">Uma posição guarda no máximo uma amostra; o banco também impede duplicidade.</p>${aguard.length ? `<label class="campos"><label>Amostra aguardando<select class="sel" id="s-am">${aguard.map(a => `<option value="${a.etiqueta}">${a.etiqueta} · ${esc(pac(a.pac).nome)}${a.hemolisada ? ' (hemolisada)' : ''}</option>`).join('')}</select></label></label><button class="btn primario" data-acao="armazenar" type="button" style="justify-self:start"${pode('armazenar') ? '' : ' disabled'}>Receber e armazenar em ${nome}</button>` : '<p class="mut">Nenhuma amostra de soro aguardando. Colete uma no módulo Coleta.</p>'}`;
  }
  const am = o.base ? null : S.amostras.find(a => a.etiqueta === o.et), cust = S.custodia.filter(x => x.et === o.et);
  const rot = o.est === 'vc' ? ['Vencida', 's-perigo'] : o.est === 'vd' ? ['Vence em 1 dia', 's-atencao'] : ['Dentro da validade', 's-ok'];
  return `<div class="linha-flex entre"><b class="mono">${o.et}</b><span class="selo ${o.base ? rot[1] : 's-teal'}">${o.base ? rot[0] : 'Armazenada'}</span></div>
    <table><tbody><tr><td class="mut">Posição</td><td>Rack 1 · Caixa A · ${nome}</td></tr><tr><td class="mut">Paciente</td><td class="mono">${o.pac}</td></tr><tr><td class="mut">Material</td><td>Soro</td></tr><tr><td class="mut">Validade</td><td>7 dias após a coleta</td></tr></tbody></table>
    ${o.base ? '<p class="mut" style="font-size:13px">Amostra de exemplo da semente. Só as amostras que você coletar podem ser movidas ou descartadas.</p>' : `<div class="linha-flex"><button class="btn" data-acao="mover" data-et="${o.et}" type="button"${pode('armazenar') ? '' : ' disabled'}>Mover</button><button class="btn perigo" data-acao="descartar" data-et="${o.et}" type="button"${pode('descartar') ? '' : ' disabled title="Só Analista e Admin descartam"'}>Descartar</button></div>`}
    ${cust.length ? `<h2>Cadeia de custódia</h2><ul class="tempo">${cust.map(x => `<li><time>${x.quando}</time><span>${x.evento} · ${esc(x.local)} · ${x.op}</span></li>`).join('')}</ul>` : ''}`;
}
TELAS.soroteca = {
  html: () => {
    const v = Object.values(S.soroteca), total = 81, oc = v.length, vd = v.filter(x => x.est === 'vd').length, vc = v.filter(x => x.est === 'vc').length;
    return `<div><p class="dev">// soroteca</p><h1>Soroteca · <em>Rack 1, Caixa A</em></h1></div>
    <div class="numeros"><div class="numero"><span class="rotulo">Ocupação</span><b>${oc}/${total}</b><span class="mut" style="font-size:13px">${Math.round(100 * oc / total)}% da caixa</span></div><div class="numero"><span class="rotulo">Vencem em 1 dia</span><b>${vd}</b></div><div class="numero"><span class="rotulo">Vencidas</span><b>${vc}</b><span class="mut" style="font-size:13px">marcadas para descarte</span></div><div class="numero"><span class="rotulo">Aguardando</span><b>${contagens().soroteca}</b><span class="mut" style="font-size:13px">para armazenar</span></div></div>
    <div class="duas"><div class="painel"><div class="corpo pilha"><div class="mapa" id="mapa" role="group" aria-label="Posições da caixa, 9 por 9">${mapaSoroteca()}</div>
      <div class="legenda"><span><i style="background:var(--superficie-2)"></i>Livre</span><span><i style="background:color-mix(in srgb,var(--acento) 28%,var(--superficie))"></i>Ocupada</span><span><i style="background:color-mix(in srgb,var(--acento-2) 40%,var(--superficie))"></i>Sua amostra</span><span><i style="background:color-mix(in srgb,var(--atencao) 32%,var(--superficie))"></i>Vence em 1 dia</span><span><i style="background:color-mix(in srgb,var(--perigo) 30%,var(--superficie))"></i>Vencida</span></div></div></div>
    <div class="painel"><div class="cab"><h3>Posição selecionada</h3></div><div class="corpo pilha" id="det-pos">${detalhePos()}</div></div></div>`;
  },
};

/* ---- equipamento ---- */
function cartoesEquip() {
  return Object.entries(L.EQUIPAMENTOS).map(([id, q]) => {
    const ex = S.execs.filter(e => e.eq === id && e.estado !== 'Concluido'), ativos = ex.filter(e => e.estado === 'Processando').length;
    return `<div class="painel"><div class="cab"><h3>${id} · ${q.nome}</h3><span class="linha-flex"><span class="selo s-teal">${q.protocolo}</span>${ativos ? '<span class="selo s-atencao">Processando</span>' : '<span class="selo s-ok">Livre</span>'}</span></div><div class="corpo fila"><span class="mut" style="font-size:13px">Capacidade ${ativos}/${q.cap}</span>
      ${ex.length ? ex.map(e => `<div class="item-fila"><div class="linha-flex entre"><span><b class="mono">${e.etiqueta}</b> · ${e.exame}</span>${e.estado === 'Erro' ? `<span class="linha-flex"><span class="selo s-perigo">Erro</span><button class="btn peq" data-acao="reprocessar" data-id="${e.id}" type="button"${e.tentativas < 3 && pode('equipamento') ? '' : ' disabled'}>Reprocessar</button></span>` : e.estado === 'NaFila' ? selo('NaFila') : `<span class="mut">${fmt(e.restante, 1)} min restantes</span>`}</div>${e.estado === 'Processando' ? `<div class="barra-prog"><span style="width:${Math.round(100 * (1 - e.restante / e.total))}%"></span></div>` : ''}</div>`).join('') : '<p class="mut">Fila vazia.</p>'}</div></div>`;
  }).join('');
}
const linhasProt = () => S.prot.filter(l => S.protEq === 'todos' || l.eq === S.protEq).map(l => `<span class="tm">${l.t}</span> <span class="${l.dir === '↑' ? 'cima' : 'baixo'}">${l.dir}</span> <span class="tm">${l.eq.slice(3, 6)}</span> ${l.html}`).join('\n') || '<span class="tm">Sem mensagens ainda. Envie uma amostra ao equipamento.</span>';
function proto(eq, dir, html) { S.prot.push({ t: horaSim(), eq, dir, html }); }
function astmFrame(n, texto) { const q = L.quadroASTM(String(n % 8), texto); return `<span class="tm">‹STX›</span>${esc(q.corpo[0])}<span class="reg">${esc(q.corpo[1])}</span>${esc(q.corpo.slice(2, -1))}<span class="tm">‹ETX›</span><span class="ck">${q.ck}</span><span class="tm">‹CR›‹LF›</span>`; }
function msgPedido(eq, ex) {
  const q = L.EQUIPAMENTOS[eq], p = pac(ex.pacId);
  if (q.protocolo === 'ASTM') { proto(eq, '↑', 'ENQ'); proto(eq, '↓', 'ACK'); [`H|\\^&|||Bancada^1.0|||||||P|1|20261104${horaSim().replace(/:/g, '')}`, `P|1||${p.id}||||${p.nasc.replace(/-/g, '')}|${p.sexo}`, `O|1|${ex.etiqueta}||^^^${ex.exame}|R`, 'L|1|N'].forEach((t, i) => { proto(eq, '↑', astmFrame(i + 1, t)); proto(eq, '↓', 'ACK'); }); proto(eq, '↑', 'EOT'); }
  else { [`MSH|^~\\&|BANCADA|LAB|${eq.replace(/-/g, '')}|LAB|20261104${horaSim().replace(/:/g, '')}||ORM^O01|MSG${ex.id}|P|2.5.1`, `PID|1||${p.id}||||${p.nasc.replace(/-/g, '')}|${p.sexo}`, `ORC|NW|${ex.pedido}`, `OBR|1|${ex.pedido}||${ex.exame}^${L.EXAMES[ex.exame].nome}^BANCADA`].forEach(t => proto(eq, '↑', `<span class="reg">${esc(t.slice(0, 3))}</span>${esc(t.slice(3))}`)); proto(eq, '↓', '<span class="reg">MSA</span>|AA|ACK'); }
}
function msgResultado(eq, ex, itens) {
  const q = L.EQUIPAMENTOS[eq];
  if (q.protocolo === 'ASTM') { proto(eq, '↓', 'ENQ'); proto(eq, '↑', 'ACK'); [`H|\\^&|||${eq}|||||||P|1`, `O|1|${ex.etiqueta}||^^^${ex.exame}|R`, ...itens.map((r, i) => `R|${i + 1}|^^^${r.analito}|${r.valor}|${L.ANALITOS[r.analito].un}||${r.flag === 'N' ? 'N' : r.flag.slice(-1)}||F`), 'L|1|N'].forEach((t, i) => { proto(eq, '↓', astmFrame(i + 1, t)); proto(eq, '↑', 'ACK'); }); proto(eq, '↓', 'EOT'); }
  else { [`MSH|^~\\&|${eq.replace(/-/g, '')}|LAB|BANCADA|LAB|20261104${horaSim().replace(/:/g, '')}||ORU^R01|MSG9${ex.id}|P|2.5.1`, `OBR|1|${ex.pedido}||${ex.exame}`, ...itens.map((r, i) => `OBX|${i + 1}|NM|${r.analito}||${r.valor}|${L.ANALITOS[r.analito].un}|||${r.flag === 'N' ? 'N' : r.flag.slice(-1)}|||F`)].forEach(t => proto(eq, '↓', `<span class="reg">${esc(t.slice(0, 3))}</span>${esc(t.slice(3))}`)); proto(eq, '↑', '<span class="reg">MSA</span>|AA|ACK'); }
}
function enviarAoEquip(et) {
  const a = S.amostras.find(x => x.etiqueta === et); if (!a) return;
  const p = S.pedidos.find(x => x.cod === a.pedido);
  a.exames.forEach(c => {
    const e = { id: S.contExec++, pedido: a.pedido, etiqueta: a.etiqueta, pacId: a.pac, exame: c, eq: L.EXAMES[c].eq, estado: 'NaFila', prioridade: p.urgente ? 1 : 0, restante: L.EXAMES[c].tempo, total: L.EXAMES[c].tempo, tentativas: 0, falhar: S.falharGLI && c === 'GLI', enviadaPor: S.operador };
    S.execs.push(e); msgPedido(e.eq, e);
  });
  a.estado = 'EmAnalise'; p.estado = 'EmAnalise'; S.custodia.push({ et: a.etiqueta, evento: 'EnviadaAoEquipamento', local: 'Equipamento', op: S.operador, quando: hhmmss(agoraISO()) });
  registrar('ExecucaoEnviada', 'Amostra', a.etiqueta, { exames: a.exames });
}
function concluirExec(e) {
  const a = S.amostras.find(x => x.etiqueta === e.etiqueta), p = S.pedidos.find(x => x.cod === e.pedido), sexo = pac(e.pacId).sexo, itens = [];
  L.EXAMES[e.exame].analitos.forEach(c => {
    const valor = S.forcados[c] != null && e.pedido === 'PED-000045' ? S.forcados[c] : L.valorSimulado(c, sexo, e.pedido), f = L.faixaPara(c, sexo), ant = (S.anteriores[e.pacId] || {})[c];
    const r = { pedido: e.pedido, pacId: e.pacId, analito: c, valor, flag: L.classificar(valor, f), ant, delta: L.deltaSuspeito(valor, ant, L.ANALITOS[c].delta), estado: 'Preliminar', execId: e.id, eq: e.eq, enviadaPor: e.enviadaPor, validadoPor: null, justificativa: '' };
    S.resultados.push(r); itens.push(r);
  });
  msgResultado(e.eq, e, itens);
  registrar('ExecucaoConcluida', 'Execucao', e.etiqueta + ':' + e.exame, { equipamento: e.eq });
  if (S.execs.filter(x => x.etiqueta === e.etiqueta).every(x => x.estado === 'Concluido')) a.estado = a.material === 'Soro' ? 'Armazenada' : 'Analisada';
  if (p.estado !== 'ResultadoPreliminar' && S.execs.filter(x => x.pedido === e.pedido && x.estado === 'Concluido').length === p.exames.length) { p.estado = 'ResultadoPreliminar'; aviso(`Resultados de ${p.cod} prontos para validação.`, 'ok'); }
}
function tick() {
  if (S.relogio.pausado || !S.execs.some(e => ['NaFila', 'Processando'].includes(e.estado))) return;
  const dt = { 1: 0.25 / 60, 60: 0.25, 600: 2.5 }[S.relogio.fator];
  S.relogio.min += dt; const r = L.passo(S.execs, dt);
  r.falhas.forEach(e => { aviso(`${e.eq}: erro ao processar ${e.exame} (${e.etiqueta}). Reprocesse.`, 'erro'); registrar('ExecucaoComErro', 'Execucao', e.etiqueta + ':' + e.exame, { tentativa: e.tentativas }, 'Recusado'); });
  r.concluidas.forEach(concluirExec);
  if (S.tela === 'equipamento') { const c = $('#cartoes'), p = $('#prot'); if (c) c.innerHTML = cartoesEquip(); if (p) p.innerHTML = linhasProt(); const rl = $('#relogio'); if (rl) rl.textContent = horaSim(); }
  if (r.concluidas.length || r.falhas.length) montarMenu();
}
setInterval(() => tick(), 250);
TELAS.equipamento = {
  html: () => {
    const el = S.amostras.filter(a => (a.estado === 'Armazenada' && a.material === 'Soro') || (a.estado === 'Coletada' && a.material !== 'Soro')).filter(a => !a.hemolisada);
    return `<div class="linha-flex entre"><div><p class="dev">// equipamento</p><h1>Equipamentos e <em>protocolo</em></h1></div>
      <div class="linha-flex"><span class="mut" style="font-size:13px">Relógio de simulação <b class="mono" id="relogio">${horaSim()}</b></span>${[['pausar', 'Pausar'], ['1', '×1'], ['60', '×60'], ['600', '×600']].map(([f, n]) => `<button class="btn peq${(f === 'pausar' ? S.relogio.pausado : !S.relogio.pausado && S.relogio.fator === +f) ? ' primario' : ''}" data-acao="relogio" data-f="${f}" type="button">${n}</button>`).join('')}</div></div>
      <div class="painel"><div class="cab"><h3>Enviar à análise</h3></div><div class="corpo">${el.length ? `<div class="linha-flex"><select class="sel" id="eq-am" aria-label="Amostra">${el.map(a => `<option value="${a.etiqueta}">${a.etiqueta} · ${esc(pac(a.pac).nome)} · ${a.exames.join('+')}</option>`).join('')}</select><button class="btn primario" data-acao="enviar-equip" type="button"${pode('equipamento') ? '' : ' disabled title="Só Analista e Admin"'}>Enviar ao equipamento</button></div>` : '<p class="mut">Nenhuma amostra pronta. Colete e guarde uma amostra de soro (ou colete sangue total para hemograma).</p>'}</div></div>
      <div class="trio" id="cartoes">${cartoesEquip()}</div>
      <div class="painel"><div class="cab"><h3>Monitor do protocolo</h3><select class="sel" data-muda="protEq" aria-label="Equipamento"><option value="todos">Todos</option>${Object.keys(L.EQUIPAMENTOS).map(k => `<option value="${k}"${S.protEq === k ? ' selected' : ''}>${k}</option>`).join('')}</select></div><div class="corpo"><div class="prot" id="prot">${linhasProt()}</div><p class="mut" style="font-size:12px;margin-top:8px">↑ host → equipamento · ↓ equipamento → host · o checksum ASTM (amarelo) é calculado de verdade: soma dos bytes de FN a ETX, módulo 256.</p></div></div>`;
  },
};

/* ---- qualidade ---- */
const CQ_MEDIA = 4.2, CQ_DP = 0.12;
TELAS.qualidade = {
  html: () => {
    const w = L.westgard(S.cq.z), bloq = w.rejeitar.length > 0;
    const regras = [['1-2s', 'Uma medida além de ±2s', 'aviso'], ['1-3s', 'Uma medida além de ±3s', 'rej'], ['2-2s', 'Duas seguidas além de 2s, mesmo lado', 'rej'], ['R-4s', 'Duas seguidas, lados opostos, além de 2s', 'rej'], ['4-1s', 'Quatro seguidas além de 1s, mesmo lado', 'rej'], ['10x', 'Dez seguidas do mesmo lado da média', 'rej']];
    return `<div class="linha-flex entre"><div><p class="dev">// qualidade</p><h1>Controle de <em>qualidade</em></h1></div><button class="btn primario" data-acao="novo-cq" type="button"${pode('equipamento') ? '' : ' disabled'}>Registrar controle</button></div>
    <div class="alerta ${bloq ? 'critico' : 'ok'}"><span aria-hidden="true">${bloq ? '!!' : 'ok'}</span><div>${bloq ? `<b>Controle reprovado</b> (${w.rejeitar.join(', ')}). A validação dos resultados do EQ-BIO-01 está bloqueada até um novo controle passar.` : `<b>Controle aprovado.</b> ${w.aviso.length ? 'Há um aviso ' + w.aviso.join(', ') + ' na última medida.' : 'Nenhuma regra violada.'}`}</div></div>
    ${figura('// levey-jennings · EQ-BIO-01 · potássio, nível normal', 'média 4,20 mmol/L · desvio 0,12', ljSVG(S.cq.z, CQ_MEDIA, CQ_DP))}
    <div class="painel"><div class="cab"><h3>Regras de Westgard</h3></div><div class="rolagem"><table><thead><tr><th>Regra</th><th>Descrição</th><th>Efeito</th><th>Última medida</th></tr></thead><tbody>${regras.map(([r, d, t]) => { const viol = w.rejeitar.includes(r) || w.aviso.includes(r); return `<tr><td class="mono">${r}</td><td>${d}</td><td>${t === 'aviso' ? 'aviso' : 'rejeita a série'}</td><td>${viol ? (t === 'aviso' ? '<span class="selo s-atencao">violada</span>' : '<span class="selo s-perigo">violada</span>') : '<span class="selo s-ok">ok</span>'}</td></tr>`; }).join('')}</tbody></table></div></div>`;
  },
};

/* ---- resultados e laudos ---- */
function bloqueioValidar(r) {
  if (op(S.operador).id === r.enviadaPor) return `Você (${S.operador}) enviou este exame ao equipamento. Outro operador precisa validar (RN-054). Troque o operador no topo.`;
  if (r.eq === 'EQ-BIO-01' && L.westgard(S.cq.z).rejeitar.length) return 'Controle de qualidade do EQ-BIO-01 reprovado: validação bloqueada (RN-043).';
  if ((r.flag === 'CH' || r.flag === 'CL') && r.estado === 'Preliminar') return 'Valor crítico: confirme antes de validar (RN-052).';
  return null;
}
function documentoLaudo(l) {
  const ped = S.pedidos.find(x => x.cod === l.pedido), p = pac(ped.pac), rs = S.resultados.filter(x => x.pedido === l.pedido);
  return `<div class="laudo"><div class="linha-flex entre"><h3>Laudo laboratorial · versão ${l.versao}</h3><span class="mono">${l.pedido}</span></div>
    <div><b>${esc(p.nome)}</b> <span class="mono">${p.id}</span><br><span style="font-size:13px">${p.sexo === 'F' ? 'Feminino' : 'Masculino'} · ${L.idade(p.nasc)} anos · liberado por ${esc(op(l.liberadoPor).nome)} em ${l.quando}</span></div>
    <table><thead><tr><th>Analito</th><th class="num-t">Resultado</th><th>Unid.</th><th>Referência</th><th>Flag</th></tr></thead><tbody>${rs.map(r => `<tr><td>${L.ANALITOS[r.analito].nome}</td><td class="num-t"><b>${fmt(r.valor, L.ANALITOS[r.analito].casas)}</b></td><td>${L.ANALITOS[r.analito].un}</td><td>${L.textoFaixa(L.faixaPara(r.analito, p.sexo))}</td><td>${flagHTML(r.flag)}</td></tr>`).join('')}</tbody></table>
    ${l.motivo ? `<p style="font-size:13px"><b>Motivo da retificação:</b> ${esc(l.motivo)}</p>` : ''}
    <div class="rodape">Simulação educacional com dados fictícios. Não é dispositivo médico e não deve ser usada em decisão clínica. Laudo liberado é imutável; correções geram nova versão.</div></div>`;
}
TELAS.resultados = {
  html: () => {
    const medico = S.papel === 'Medico', peds = [...new Set(S.resultados.map(r => r.pedido))];
    const tabelas = medico ? '<div class="alerta info"><span aria-hidden="true">i</span><div>Médico consulta apenas laudos liberados. A validação é do Analista.</div></div>' : (peds.length ? peds.map(cod => {
      const rs = S.resultados.filter(r => r.pedido === cod), p = pac(rs[0].pacId), tudo = rs.every(r => r.estado === 'Validado'), lib = S.laudos.some(l => l.pedido === cod);
      return `<div class="painel"><div class="cab"><h3>${cod} · ${esc(p.nome)}</h3>${lib ? '<span class="selo s-ok">Laudo liberado</span>' : `<button class="btn primario peq" data-acao="liberar" data-cod="${cod}" type="button"${pode('validar') ? '' : ' disabled'}>Liberar laudo</button>`}</div><div class="rolagem"><table><thead><tr><th>Analito</th><th class="num-t">Valor</th><th>Unid.</th><th>Referência</th><th>Flag</th><th class="num-t">Anterior</th><th>Estado</th><th></th></tr></thead><tbody>${rs.map(r => { const i = S.resultados.indexOf(r); return `<tr><td>${L.ANALITOS[r.analito].nome}<div class="mono mut" style="font-size:11px">${r.eq} · enviado por ${r.enviadaPor}</div></td><td class="num-t"><b>${fmt(r.valor, L.ANALITOS[r.analito].casas)}</b></td><td>${L.ANALITOS[r.analito].un}</td><td>${L.textoFaixa(L.faixaPara(r.analito, p.sexo))}</td><td>${flagHTML(r.flag)}</td><td class="num-t mut">${r.ant != null ? fmt(r.ant, L.ANALITOS[r.analito].casas) : '—'}${r.delta ? ' <span class="selo s-atencao">delta</span>' : ''}</td><td>${selo(r.estado)}</td><td class="linha-flex">${r.estado === 'Preliminar' && (r.flag === 'CH' || r.flag === 'CL') ? `<button class="btn peq perigo" data-acao="conf-critico" data-i="${i}" type="button">Confirmar crítico</button>` : ''}${r.estado !== 'Validado' ? `<button class="btn peq" data-acao="validar" data-i="${i}" type="button">Validar</button>` : ''}</td></tr>`; }).join('')}</tbody></table></div>${tudo && !lib ? '<p class="mut" style="padding:10px 16px;font-size:13px">Todos os resultados validados: pode liberar o laudo.</p>' : ''}</div>`;
    }).join('') : '<div class="painel"><div class="corpo"><p class="mut">Nenhum resultado ainda. Envie uma amostra ao equipamento e acompanhe a simulação.</p></div></div>');
    return `<div><p class="dev">// resultados</p><h1>${medico ? 'Laudos' : 'Validação e <em>laudos</em>'}</h1></div>${tabelas}
    <div class="painel"><div class="cab"><h3>Laudos liberados</h3></div>${S.laudos.length ? `<div class="rolagem"><table><thead><tr><th>Pedido</th><th>Paciente</th><th>Versão</th><th>Liberado por</th><th>Quando</th><th></th></tr></thead><tbody>${S.laudos.map((l, i) => `<tr><td class="mono">${l.pedido}</td><td>${esc(pac(S.pedidos.find(x => x.cod === l.pedido).pac).nome)}</td><td>v${l.versao}</td><td>${esc(op(l.liberadoPor).nome)}</td><td class="mono">${l.quando}</td><td><button class="btn peq" data-acao="abrir-laudo" data-i="${i}" type="button">Abrir</button></td></tr>`).join('')}</tbody></table></div>` : '<p class="mut" style="padding:16px">Nenhum laudo liberado.</p>'}</div>`;
  },
};

/* ---- auditoria ---- */
TELAS.auditoria = {
  html: () => {
    const lista = S.simAud ? S.simAud.copia : S.auditoria;
    return `<div class="linha-flex entre"><div><p class="dev">// auditoria</p><h1>Auditoria <em>encadeada</em></h1><p class="sub">Cada evento carrega o hash SHA-256 do anterior. Mexer num evento antigo quebra todos os seguintes.</p></div>
      <div class="linha-flex"><button class="btn primario" data-acao="verificar" type="button"${pode('auditoria') ? '' : ' disabled'}>Verificar cadeia</button><button class="btn" data-acao="adulterar" type="button"${pode('auditoria') ? '' : ' disabled'}>Simular adulteração</button>${S.simAud ? '<button class="btn" data-acao="limpar-sim" type="button">Voltar ao real</button>' : ''}</div></div>
    <div id="res-aud">${S.verif && !S.simAud ? `<div class="alerta ${S.verif.ok ? 'ok' : 'critico'}"><span aria-hidden="true">${S.verif.ok ? 'ok' : '!!'}</span><div>${S.verif.ok ? `<b>Cadeia íntegra.</b> ${S.verif.n} eventos verificados, do primeiro ao último, com SHA-256.` : `<b>Cadeia quebrada no evento ${S.verif.quebrouEm}.</b>`}</div></div>` : ''}${S.simAud ? `<div class="alerta critico"><span aria-hidden="true">!!</span><div><b>Cadeia quebrada no evento ${S.simAud.quebrouEm}.</b> A alteração foi feita só na <b>cópia exibida</b>; o registro real está intacto e continua verificável.</div></div>` : ''}</div>
    <div class="painel"><div class="cab"><h3>${lista.length} eventos${S.simAud ? ' · cópia adulterada' : ''}</h3><span class="mut" style="font-size:13px">gatilhos do banco impedem UPDATE e DELETE</span></div><div class="rolagem"><table><thead><tr><th>#</th><th>Hora</th><th>Papel · operador</th><th>Ação</th><th>Alvo</th><th>Resultado</th><th>Hash (início)</th></tr></thead><tbody>${[...lista].reverse().map(e => `<tr class="${S.simAud && e.seq === S.simAud.quebrouEm ? 'quebrada' : ''}"><td class="num-t">${e.seq}</td><td class="mono">${hhmmss(e.quando)}</td><td>${L.PAPEIS[e.papel]} · <span class="mono">${e.operador}</span></td><td class="mono">${esc(e.acao)}</td><td class="mono">${esc(e.alvoTipo)} ${esc(e.alvoId)}</td><td>${selo(e.resultado)}</td><td class="hash">${e.hash.slice(0, 10)}…</td></tr>`).join('')}</tbody></table></div></div>`;
  },
};

/* ---- operação ---- */
TELAS.operacao = {
  html: () => {
    const v = Object.values(S.soroteca), livre = 81 - v.length, ok = v.filter(x => x.est === 'ok' || !x.base).length, vd = v.filter(x => x.est === 'vd').length, vc = v.filter(x => x.est === 'vc').length;
    const fila = Object.entries(L.EQUIPAMENTOS).map(([id, q]) => ({ n: id, v: S.execs.filter(e => e.eq === id && ['NaFila', 'Processando'].includes(e.estado)).length, c: 't' }));
    return `<div><p class="dev">// operação</p><h1>Painel <em>operacional</em></h1></div>
    <div class="duas">${figura('// tempo de resposta por exame', 'minutos entre pedido e liberação · valores de exemplo', barrasSVG([{ n: 'Eletrólitos', v: 31, c: 't' }, { n: 'Glicose', v: 38, c: 't' }, { n: 'Creatinina', v: 41, c: 't' }, { n: 'Hemograma', v: 52, c: 'a' }, { n: 'TSH', v: 96, c: 'v' }, { n: 'Ferritina', v: 104, c: 'v' }], 120, 'min', 30))}
    ${figura('// ocupação da soroteca', 'ao vivo, caixa A', roscaSVG([{ n: 'Livres', v: livre, c: 'n', k: 'n' }, { n: 'Ocupadas', v: ok, c: 'a', k: 'a' }, { n: 'Vencem em 1 dia', v: vd, c: 'at', k: 'at' }, { n: 'Vencidas', v: vc, c: 'pe', k: 'pe' }], Math.round(100 * v.length / 81) + '%', 'ocupada'))}</div>
    ${figura('// fila por equipamento', 'ao vivo: itens esperando ou processando', barrasSVG(fila.map(f => ({ ...f, v: f.v })), Math.max(4, ...fila.map(f => f.v)), 'itens'))}`;
  },
};

/* ---- painel técnico ---- */
const PROJ = [['Bancada.Dominio', 312], ['Bancada.Protocolos', 186], ['Bancada.Aplicacao', 341], ['Bancada.Infraestrutura', 228], ['Bancada.Web', 217], ['JavaScript', 412]];
const SEG = [['Consulta parametrizada (LINQ e FromSql)', 'D2', 'Corpus_SqlInjection_TodosOsCampos'], ['CSP sem inline, sem unsafe-inline', 'D2', 'Cabecalhos_CspExato'], ['Antifalsificação em todo POST', 'D2', 'Csrf_PostSemToken_Retorna400'], ['Autorização por ação (matriz)', 'D1', 'Matriz_TodasAsAcoesTodosOsPapeis'], ['Auditoria encadeada por hash', 'D1', 'Cadeia_AlterarQualquerCampo_Detecta'], ['Banco isolado por sessão', 'D1', 'Isolamento_SessaoANaoLeSessaoB'], ['Limite de taxa e teto de sessões', 'D1/D4', 'Limites_Excesso_Retorna429'], ['Cifra AES-256-GCM de texto livre', 'D3', 'Cifra_AadErrada_Falha'], ['Defesa em profundidade', 'D3', 'Arquitetura_SetasDeDependencia'], ['Hardening: sem Server, sem stack trace', 'D4', 'Erro_NaoVazaDetalhe'], ['Kill switch do executor', 'D4', 'Executor_Desligado_NuncaCriaProcesso'], ['Licenças só MIT (1 exceção)', 'D5', 'Licencas_TodasNaLista']];
const RNS = [['RN-001', 'RN_001_HorarioOcupadoRecusaSegundaReserva'], ['RN-002', 'RN_002_PassadoRecusado'], ['RN-006', 'RN_006_CheckInForaDoDiaRecusado'], ['RN-007', 'RN_007_OutroProfissionalRecusado'], ['RN-010', 'RN_010_EvolucaoImutavel'], ['RN-011', 'RN_011_CorrecaoEncadeada'], ['RN-021', 'RN_021_AgrupaPorMaterial'], ['RN-022', 'RN_022_JejumExigeConfirmacao'], ['RN-030', 'RN_030_PosicaoUnica'], ['RN-043', 'RN_043_CQReprovadoBloqueia'], ['RN-044', 'RN_044_Westgard_2_2s'], ['RN-052', 'RN_052_CriticoExigeConfirmacao'], ['RN-053', 'RN_053_DeltaCheck'], ['RN-054', 'RN_054_SeparacaoDeDeveres'], ['RN-055', 'RN_055_LiberacaoCompleta'], ['RN-056', 'RN_056_LaudoImutavel'], ['RN-062', 'RN_062_CadeiaDetectaAdulteracao']];
const ADRS = [['001', '.NET 9 (migrar para o 10 LTS antes de publicar)'], ['002', 'SQLite, um banco em memória por visitante'], ['003', 'EF Core code-first, SQL cru parametrizado só onde agrega'], ['004', 'NUnit e dublês escritos à mão'], ['005', 'JavaScript sem dependência'], ['006', 'Sem contas, sem upload, sem saída de rede'], ['007', 'CSP estrita sem inline'], ['008', 'Execução de testes pelo painel, com salvaguardas'], ['009', 'Sem CPF nem dado pessoal'], ['010', 'pt-BR e en'], ['011', 'Auditoria encadeada por hash']];
const arquiteturaSVG = () => {
  const cx = (x, y, w, nome, d) => `<rect class="caixa${d ? ' dest' : ''}" x="${x}" y="${y}" width="${w}" height="34" rx="7"/><text class="mono tx" x="${x + w / 2}" y="${y + 21}" text-anchor="middle">${nome}</text>`, a = d => `<path class="lig" d="${d}" marker-end="url(#ponta)"/>`;
  return svg(560, 226, 'Dependências entre projetos: Web usa Aplicacao e Infraestrutura; Infraestrutura e Aplicacao usam Dominio; Protocolos usa Dominio', `<defs><marker id="ponta" viewBox="0 0 8 8" refX="7" refY="4" markerWidth="8" markerHeight="8" orient="auto"><path class="pt" d="M0 0L8 4L0 8z"/></marker></defs>` + cx(200, 6, 160, 'Bancada.Web') + cx(30, 88, 170, 'Bancada.Aplicacao') + cx(360, 88, 170, 'Bancada.Infraestrutura') + cx(200, 170, 160, 'Bancada.Dominio', true) + cx(10, 170, 150, 'Bancada.Protocolos') + a('M240 40L130 86') + a('M320 40L430 86') + a('M358 105H204') + a('M160 122L226 168') + a('M440 122L334 168') + a('M160 187H196') + a('M90 122V166'));
};
const PAINEL_ABAS = [['geral', 'Visão geral'], ['arquitetura', 'Arquitetura'], ['seguranca', 'Segurança'], ['licencas', 'Licenças'], ['catalogo', 'Catálogo de testes'], ['decisoes', 'Decisões']];
function painelConteudo() {
  const tot = PROJ.slice(0, 5).reduce((s, p) => s + p[1], 0);
  if (S.subPainel === 'geral') return `<div class="numeros"><div class="numero"><span class="rotulo">Testes C#</span><b>${fmt(tot, 0)}</b><span class="selo s-ok">todos passam</span></div><div class="numero"><span class="rotulo">Testes JS</span><b>412</b><span class="selo s-ok">todos passam</span></div><div class="numero"><span class="rotulo">Cobertura</span><b>100%</b><span class="mut" style="font-size:13px">linhas e ramos</span></div><div class="numero"><span class="rotulo">Regras com teste</span><b>45/45</b></div><div class="numero"><span class="rotulo">Licenças</span><b>MIT</b><span class="mut" style="font-size:13px">1 exceção</span></div></div>
    <div class="duas"><div class="painel"><div class="cab"><h3>Rodar testes</h3><button class="btn primario" id="rodar" data-acao="rodar-testes" type="button">Rodar testes JS</button></div><div class="corpo"><div class="console" id="console" aria-live="polite"><span class="tm">Pronto. Esboço: nenhum teste roda de verdade aqui. No sistema real, o servidor executa o comando fixo com limites de taxa, tempo e saída.</span></div></div></div>
    <div class="painel"><div class="cab"><h3>Cobertura por projeto</h3><span class="mut" style="font-size:13px">C#: medida no build</span></div><div class="corpo cobertura">${PROJ.map(p => `<div class="cob"><span class="mono">${p[0]}</span><div class="trilho"><span style="width:100%"></span></div><span class="pc">100%</span></div>`).join('')}</div></div></div>
    <div class="duas">${figura('// testes por projeto', 'valores de exemplo', roscaSVG(PROJ.map((p, i) => ({ n: p[0].replace('Bancada.', ''), v: p[1], c: ['a', 't', 'v', 'ok', 'at', 'pe'][i], k: ['a', 't', 'v', 'ok', 'at', 'pe'][i] })), fmt(tot + 412, 0), 'testes'))}
    ${figura('// testes mais lentos', 'milissegundos · valores de exemplo', barrasSVG([{ n: 'Sessão 200 simult.', v: 920, c: 'a' }, { n: 'Executor: tempo máx.', v: 840, c: 'v' }, { n: 'Cadeia 1.000 eventos', v: 610, c: 't' }, { n: 'Migrations em vazio', v: 480, c: 't' }, { n: 'Matriz papel × ação', v: 450, c: 'a' }], 1000, 'ms', 250))}</div>`;
  if (S.subPainel === 'arquitetura') return `<div class="duas">${figura('// projetos e dependências', 'lido dos assemblies · seta = referência', arquiteturaSVG())}<div class="painel"><div class="cab"><h3>Regras por camada</h3></div><div class="corpo camadas"><div class="camada"><span>Controller</span><span>valida entrada, chama 1 caso de uso</span></div><div class="camada"><span>Caso de uso</span><span>orquestra domínio e portas</span></div><div class="camada"><span>Domínio</span><span>regras e estados, sem I/O</span></div><div class="camada"><span>Infraestrutura</span><span>EF Core, SQL, cifra, relógio</span></div><div class="camada"><span>View</span><span>exibe ViewModel, escapa saída</span></div></div></div></div>
    <div class="painel"><div class="cab"><h3>Banco desta sessão</h3><span class="mut" style="font-size:13px">24 tabelas · lido de sqlite_master</span></div><div class="corpo"><div class="chips">${['paciente', 'alergia', 'profissional', 'agenda_grade', 'agendamento', 'atendimento', 'evolucao', 'exame_catalogo', 'analito', 'faixa_referencia', 'pedido', 'pedido_item', 'amostra', 'amostra_custodia', 'soroteca_rack', 'soroteca_caixa', 'soroteca_posicao', 'equipamento', 'equipamento_exame', 'execucao', 'resultado', 'laudo', 'controle_qualidade', 'auditoria'].map((t, i) => `<span class="chip ${['a', 't', 'v'][i % 3]}">${t}</span>`).join('')}</div></div></div>`;
  if (S.subPainel === 'seguranca') return `<div class="painel"><div class="cab"><h3>Mapa de segurança (Security+)</h3><span class="mut" style="font-size:13px">cada controle aponta o teste que o prova</span></div><div class="rolagem"><table><thead><tr><th>Controle</th><th>Domínio</th><th>Teste</th><th>Estado</th></tr></thead><tbody>${SEG.map(s => `<tr><td>${s[0]}</td><td><span class="chip v">${s[1]}</span></td><td class="mono">${s[2]}</td><td><span class="selo s-ok">passa</span></td></tr>`).join('')}</tbody></table></div></div>
    <div class="duas"><div class="painel"><div class="cab"><h3>Cabeçalhos de segurança</h3></div><div class="corpo"><div class="codigo">Content-Security-Policy: default-src 'none'; script-src 'self';\n  style-src 'self'; img-src 'self' data:; connect-src 'self';\n  base-uri 'none'; form-action 'self'; frame-ancestors 'none'\nStrict-Transport-Security: max-age=31536000\nX-Content-Type-Options: nosniff\nReferrer-Policy: no-referrer\n(Server e X-Powered-By removidos)</div></div></div>
    <div class="painel"><div class="cab"><h3>Registro de riscos</h3></div><div class="rolagem"><table><tbody><tr><td>Upload malicioso</td><td><span class="selo s-acento">evitar</span></td></tr><tr><td>Roubo de credencial</td><td><span class="selo s-acento">evitar</span></td></tr><tr><td>XSS e SQL injection</td><td><span class="selo s-ok">mitigar</span></td></tr><tr><td>Abuso do executor de testes</td><td><span class="selo s-ok">mitigar</span></td></tr><tr><td>Queda da hospedagem</td><td><span class="selo s-violeta">transferir</span></td></tr><tr><td>DDoS volumétrico</td><td><span class="selo s-atencao">aceitar</span></td></tr></tbody></table></div></div></div>`;
  if (S.subPainel === 'licencas') return `<div class="painel"><div class="cab"><h3>Licenças das dependências</h3><span class="mut" style="font-size:13px">política estrita: só MIT</span></div><div class="rolagem"><table><thead><tr><th>Pacote</th><th>Licença</th><th>Origem</th><th>Estado</th></tr></thead><tbody>${[['.NET 9, ASP.NET Core, Microsoft.Extensions.*', 'MIT', 'direto'], ['Microsoft.EntityFrameworkCore, .Sqlite', 'MIT', 'direto'], ['Microsoft.Data.Sqlite', 'MIT', 'EF Core Sqlite'], ['NUnit, NUnit3TestAdapter, NUnit.Analyzers', 'MIT', 'testes'], ['Microsoft.NET.Test.Sdk', 'MIT', 'testes'], ['coverlet.msbuild', 'MIT', 'testes'], ['Microsoft.AspNetCore.Mvc.Testing', 'MIT', 'testes'], ['SQLite (a biblioteca)', 'domínio público', 'nativo']].map(l => `<tr><td>${l[0]}</td><td>${l[1]}</td><td class="mut">${l[2]}</td><td><span class="selo s-ok">ok</span></td></tr>`).join('')}<tr><td>SQLitePCLRaw</td><td>Apache-2.0</td><td class="mut">Microsoft.Data.Sqlite</td><td><span class="selo s-atencao">exceção aceita em 01/11</span></td></tr></tbody></table></div></div>`;
  if (S.subPainel === 'catalogo') return `<div class="painel"><div class="cab"><h3>Catálogo de testes por regra de negócio</h3><span class="mut" style="font-size:13px">trecho · 45 regras, 45 com teste</span></div><div class="rolagem"><table><thead><tr><th>Regra</th><th>Teste</th><th>Estado</th></tr></thead><tbody>${RNS.map(r => `<tr><td class="mono">${r[0]}</td><td class="mono">${r[1]}</td><td><span class="selo s-ok">passa</span></td></tr>`).join('')}</tbody></table></div></div>`;
  return `<div class="painel"><div class="cab"><h3>Decisões de arquitetura</h3></div><div class="corpo"><ul class="tempo">${ADRS.map(a => `<li><time>ADR-${a[0]}</time><span>${a[1]}</span></li>`).join('')}</ul></div></div>`;
}
TELAS.painel = {
  html: () => `<div class="linha-flex entre"><div><p class="dev">// painel técnico</p><h1>Tudo que está <em>por trás</em></h1></div><span class="selo s-neutro">valores de exemplo · commit 3f9c1ab</span></div>
    <div class="abas" role="tablist">${PAINEL_ABAS.map(([id, n]) => `<button class="aba" role="tab" data-acao="sub-painel" data-id="${id}" aria-selected="${S.subPainel === id}" type="button">${n}</button>`).join('')}</div><div class="pilha" id="painel-corpo">${painelConteudo()}</div>`,
};
function rodarTestes() {
  const b = $('#rodar'), c = $('#console'); if (!b || b.disabled) return;
  b.disabled = true; c.textContent = '';
  const linhas = [['pr', '$ node --test --experimental-test-coverage tests/js'], ['ok', '✔ agenda › montarGrade › semana com lacunas'], ['ok', '✔ agenda › teclado › setas movem o foco'], ['ok', '✔ soroteca › mapa › posição ocupada não aceita segunda amostra'], ['ok', '✔ etiqueta › code128 › ida e volta de AMO-000123'], ['ok', '✔ qualidade › westgard › regra 2-2s violada'], ['ok', '✔ comum › dom › proíbe innerHTML'], ['tm', 'testes 412 · aprovados 412 · falhas 0 · 0,9 s'], ['tm', 'cobertura  linhas 100%  ramos 100%  funções 100%'], ['tm', '(exemplo: nenhum teste foi executado)']];
  registrar('TestesIniciados', 'Painel', 'js', { exemplo: true });
  let i = 0; const passo = () => { if (i >= linhas.length) { b.disabled = false; return; } const d = document.createElement('div'); d.className = linhas[i][0]; d.textContent = linhas[i][1]; c.appendChild(d); c.scrollTop = c.scrollHeight; i++; setTimeout(passo, 220); }; passo();
}

/* ---- laboratório de ataque ---- */
const CORPUS = {
  'SQL injection': ["' OR '1'='1", "'; DROP TABLE paciente;--", '1 UNION SELECT NULL--', '" OR ""="'],
  'XSS': ['<script>alert(1)</script>', '"><img src=x onerror=alert(1)>', '<svg/onload=alert(1)>', 'javascript:alert(1)'],
  'Injeção de template': ['{{7*7}}', '${7*7}', '@(7*7)'],
  'Travessia de caminho': ['../../etc/passwd', '..\\..\\appsettings.json', '%2e%2e%2f', '....//'],
  'Injeção de linha (CRLF)': ['x%0d%0aSet-Cookie:a=b'],
  'Corpo grande': ['(1 MB de "A")'],
};
const RESPOSTA = {
  'SQL injection': [200, 'Consulta parametrizada', 'O texto vira dado, nunca SQL. A busca devolve 0 resultados e o esquema do banco fica intacto.'],
  'XSS': [200, 'Escape de saída + CSP sem inline', 'Aparece como texto. Mesmo se algo escapasse, a CSP bloqueia script inline.'],
  'Injeção de template': [200, 'Sem avaliação de expressão', 'Mostrado literal. Se fosse vulnerável apareceria 49.'],
  'Travessia de caminho': [400, 'Lista permitida + caminho canônico', 'O visualizador de código só abre arquivos de uma lista fechada; o resto é recusado.'],
  'Injeção de linha (CRLF)': [400, 'Validação de entrada', 'Quebra de linha não entra em cabeçalho nem em linha de log.'],
  'Corpo grande': [413, 'Limite de 16 KB no corpo', 'A requisição é recusada antes de ser processada.'],
};
TELAS.ataque = {
  html: () => {
    const A = S.ataque, r = A.res;
    return `<div><p class="dev">// laboratório de ataque</p><h1>Tente <em>quebrar</em></h1><p class="sub">Você dispara contra a defesa de verdade, não contra um endpoint de brinquedo. O que aparece abaixo é o que o sistema faria, e o evento vai para a auditoria.</p></div>
    <div class="duas"><div class="painel"><div class="cab"><h3>Montar o ataque</h3></div><div class="corpo campos">
      <label>Categoria<select class="sel" data-muda="ataqueCat">${opcoes(Object.keys(CORPUS).map(k => [k, k]), A.cat)}</select></label>
      <label>Alvo<select class="sel" data-muda="ataqueAlvo">${opcoes(['Busca de pacientes', 'Campo de evolução', 'Rota do visualizador de código'].map(k => [k, k]), A.alvo)}</select></label>
      <div class="rotulo">Payloads prontos</div><div class="linha-flex">${CORPUS[A.cat].map((p, i) => `<button class="btn peq" data-acao="disparar" data-i="${i}" type="button"><span class="mono">${esc(p.length > 26 ? p.slice(0, 26) + '…' : p)}</span></button>`).join('')}</div>
      <label>Ou digite o seu (até 500 caracteres)<input class="campo" id="payload-livre" maxlength="500"></label>
      <div class="linha-flex"><button class="btn primario" data-acao="disparar-livre" type="button">Disparar o meu</button><button class="btn" data-acao="rajada" type="button">200 requisições em 10 s</button></div></div></div>
    <div class="painel"><div class="cab"><h3>O que o sistema faz</h3>${r ? `<span class="selo ${r.status === 200 ? 's-ok' : 's-perigo'}">HTTP ${r.status}</span>` : ''}</div><div class="corpo pilha">${r ? `
      <div><div class="rotulo">Enviado (${esc(A.alvo)})</div><div class="codigo" style="white-space:pre-wrap">${esc(r.payload)}</div></div>
      <div class="alerta ok"><span aria-hidden="true">ok</span><div><b>${r.defesa}.</b> ${r.texto}</div></div>
      ${r.sql ? `<div><div class="rotulo">Como a consulta é montada</div><div class="codigo">SELECT id, nome FROM paciente\n WHERE nome LIKE @busca\n-- @busca = '${esc(r.payload.slice(0, 60))}'  (parâmetro, não texto de SQL)</div></div>` : ''}
      <div><div class="rotulo">Como apareceria na tela</div><div class="codigo" style="white-space:pre-wrap">${esc(r.payload)}</div></div>
      <div class="mono mut">evento gravado: <b>${r.evento}</b> (veja na Auditoria)</div>` : '<p class="mut">Escolha um payload e dispare.</p>'}</div></div></div>`;
  },
};
function disparar(payload) {
  const A = S.ataque, base = RESPOSTA[A.cat], grande = A.cat === 'Corpo grande';
  const caminho = A.cat === 'Travessia de caminho' && A.alvo !== 'Rota do visualizador de código';
  A.res = { payload: grande ? 'A'.repeat(48) + '… (1.048.576 bytes)' : payload, status: caminho ? 200 : base[0], defesa: caminho ? 'Tratado como texto' : base[1], texto: caminho ? 'Fora da rota do visualizador, o caminho é apenas texto e não toca no disco.' : base[2], sql: A.alvo === 'Busca de pacientes' && ['SQL injection', 'XSS', 'Injeção de template'].includes(A.cat), evento: base[0] === 200 ? 'PayloadBloqueado (neutralizado)' : 'PayloadBloqueado' };
  registrar('PayloadBloqueado', 'LabAtaque', A.cat, { alvo: A.alvo, status: A.res.status }, 'Bloqueado'); render();
}

/* ================= ações ================= */
const ACOES = {
  'fechar-modal': () => fecharModal(),
  ir: a => ir(a.dataset.tela),
  cenario: a => { const id = a.dataset.id; const op0 = S.papel; S = novoEstado(id); S.papel = 'Recepcao'; S.operador = 'REC-01'; registrar('CenarioCarregado', 'Sessao', id); render(); aviso(`Cenário carregado: ${CENARIOS.find(c => c[0] === id)[1]}.`, 'ok'); void op0; },
  ficha: a => { S.pacSel = a.dataset.id; $('#tab-pac').innerHTML = tabelaPacientes(); $('#ficha').innerHTML = fichaPaciente(); },
  'abrir-prontuario': a => { S.pacProt = a.dataset.id; ir('prontuario'); },
  'novo-agend': a => { if (!exigir('agendar', 'agendar horário')) return; modalAgendar(a.dataset.dia != null ? +a.dataset.dia : null, a.dataset.slot != null ? +a.dataset.slot : null); },
  'salvar-agend': () => {
    limparErros(); const pid = $('#m-pac').value, [d, s] = $('#m-hor').value.split('|').map(Number), motivo = $('#m-motivo').value; let ok = true;
    if (!pid) { erroCampo('m-pac', 'Escolha um paciente.'); ok = false; }
    if (S.agenda.some(x => x.prof === S.agProf && x.dia === d && x.slot === s)) { erroCampo('m-hor', 'Esse horário já está ocupado (RN-001).'); ok = false; }
    else if (passado(d, s)) { erroCampo('m-hor', 'Não é possível agendar no passado (RN-002).'); ok = false; }
    if (motivo.length > 200) { erroCampo('m-motivo', 'Texto longo demais (máximo 200).'); ok = false; }
    if (!ok) { registrar('AgendamentoCriado', 'Agendamento', `${S.agProf}|${d}|${s}`, {}, 'Recusado'); return; }
    S.agenda.push({ prof: S.agProf, dia: d, slot: s, pac: pid, estado: 'Agendado' }); registrar('AgendamentoCriado', 'Agendamento', `${S.agProf}|${d}|${s}`, { paciente: pid });
    fecharModal(); render(); aviso('Agendamento criado.', 'ok');
  },
  'abrir-agend': a => abrirAgend(a.dataset.k),
  'confirmar-agend': a => { const e = S.agenda.find(x => chave(x) === a.dataset.k); e.estado = 'Confirmado'; registrar('AgendamentoConfirmado', 'Agendamento', a.dataset.k); fecharModal(); render(); aviso('Agendamento confirmado.', 'ok'); },
  'cancelar-agend': a => { S.agenda = S.agenda.filter(x => chave(x) !== a.dataset.k); registrar('AgendamentoCancelado', 'Agendamento', a.dataset.k); fecharModal(); render(); aviso('Agendamento cancelado; o horário foi liberado (RN-004).', 'ok'); },
  checkin: a => {
    if (!exigir('checkin', 'fazer check-in')) return; const e = S.agenda.find(x => chave(x) === a.dataset.k);
    if (e.dia !== AGORA.dia) { aviso('Check-in só no dia do agendamento (RN-006).', 'erro'); registrar('CheckInFeito', 'Agendamento', a.dataset.k, {}, 'Recusado'); return; }
    e.estado = 'ChegouCheckIn'; registrar('CheckInFeito', 'Agendamento', a.dataset.k); render(); aviso(`Check-in de ${pac(e.pac).nome} feito.`, 'ok');
  },
  iniciar: a => {
    if (!exigir('atender', 'iniciar atendimento')) return; const e = S.agenda.find(x => chave(x) === a.dataset.k);
    if (S.operador !== e.prof) { aviso(`Este horário é de ${op(e.prof).nome}. Só o profissional do horário atende (RN-007).`, 'erro'); registrar('AtendimentoIniciado', 'Agendamento', a.dataset.k, {}, 'Recusado'); return; }
    e.estado = 'EmAtendimento'; S.atendAberto = chave(e); registrar('AtendimentoIniciado', 'Agendamento', a.dataset.k); render();
  },
  'abrir-atend': a => { S.atendAberto = a.dataset.k; $('#atend').innerHTML = painelAtend(); $('#atend').scrollIntoView({ behavior: 'smooth', block: 'start' }); },
  'registrar-evo': () => {
    if (!exigir('evolucao', 'registrar evolução')) return; limparErros(); const e = S.agenda.find(x => chave(x) === S.atendAberto), t = $('#evo-texto').value.trim();
    if (S.operador !== e.prof) { aviso(`Este atendimento é de ${op(e.prof).nome} (RN-007).`, 'erro'); return; }
    if (!t) { erroCampo('evo-texto', 'Escreva a evolução antes de registrar.'); return; }
    if (t.length > 2000) { erroCampo('evo-texto', 'Texto longo demais (máximo 2.000).'); return; }
    S.evolucoes.push({ id: S.contEvo++, pac: e.pac, aut: S.operador, quando: '04/11/2026 ' + horaSim().slice(0, 5), texto: t, corrige: null, atend: S.atendAberto });
    registrar('EvolucaoRegistrada', 'Evolucao', e.pac, { tamanho: t.length }); $('#atend').innerHTML = painelAtend(); aviso('Evolução registrada (cifrada em repouso).', 'ok');
  },
  'concluir-atend': () => {
    if (!exigir('atender', 'concluir atendimento')) return; const e = S.agenda.find(x => chave(x) === S.atendAberto);
    if (S.operador !== e.prof) { aviso(`Este atendimento é de ${op(e.prof).nome} (RN-007).`, 'erro'); return; }
    if (!S.evolucoes.some(x => x.atend === S.atendAberto)) { aviso('Registre pelo menos uma evolução antes de concluir.', 'erro'); return; }
    e.estado = 'Concluido'; registrar('AtendimentoConcluido', 'Agendamento', S.atendAberto); render(); aviso('Atendimento concluído.', 'ok');
  },
  'pedir-exame': a => { S.pedPac = a.dataset.id; ir('pedido'); },
  corrigir: a => { S.corrigindo = +a.dataset.id; render(); },
  'cancelar-correcao': () => { S.corrigindo = null; render(); },
  'salvar-correcao': a => {
    if (!exigir('evolucao', 'corrigir evolução')) return; limparErros(); const t = $('#evo-corr').value.trim();
    if (!t) { erroCampo('evo-corr', 'Escreva a correção.'); return; } if (t.length > 2000) { erroCampo('evo-corr', 'Texto longo demais (máximo 2.000).'); return; }
    S.evolucoes.push({ id: S.contEvo++, pac: S.pacProt, aut: S.operador, quando: '04/11/2026 ' + horaSim().slice(0, 5), texto: t, corrige: +a.dataset.id, atend: null });
    S.corrigindo = null; registrar('EvolucaoCorrigida', 'Evolucao', a.dataset.id); render(); aviso('Correção registrada; a original continua visível.', 'ok');
  },
  'add-alergia': () => {
    if (!exigir('alergia', 'editar alergias')) return; limparErros(); const sub = $('#al-sub').value.trim(), rea = $('#al-rea').value.trim();
    if (!sub) { erroCampo('al-sub', 'Informe a substância.'); return; } if (sub.length > 60) { erroCampo('al-sub', 'Máximo de 60 caracteres.'); return; } if (rea.length > 120) { erroCampo('al-rea', 'Máximo de 120 caracteres.'); return; }
    (S.alergias[S.pacProt] ||= []).push({ subst: sub, reacao: rea || '—', ativa: true }); registrar('AlergiaAlterada', 'Paciente', S.pacProt, { acao: 'adicionar' }); render(); aviso('Alergia adicionada.', 'ok');
  },
  'alt-alergia': a => { if (!exigir('alergia', 'editar alergias')) return; const x = S.alergias[S.pacProt][+a.dataset.i]; x.ativa = !x.ativa; registrar('AlergiaAlterada', 'Paciente', S.pacProt, { acao: x.ativa ? 'reativar' : 'inativar' }); render(); },
  'toggle-ex': a => {
    if (!exigir('criarPedido', 'criar pedido')) { a.checked = false; return; }
    if (a.checked) { if (S.carrinho.size >= 20) { a.checked = false; aviso('Um pedido aceita no máximo 20 exames.', 'erro'); return; } S.carrinho.add(a.dataset.c); } else S.carrinho.delete(a.dataset.c);
    $('#carrinho').innerHTML = carrinhoHTML(); $('.painel .cab .mut', $('#carrinho').parentElement.parentElement).textContent = S.carrinho.size + '/20 exames';
  },
  urgente: a => { S.pedUrgente = a.checked; },
  'enviar-pedido': () => {
    if (!exigir('criarPedido', 'criar pedido')) return; if (!S.carrinho.size) { aviso('Escolha ao menos um exame.', 'erro'); return; }
    const cod = 'PED-' + String(S.contPedido++).padStart(6, '0');
    S.pedidos.push({ cod, pac: S.pedPac, exames: [...S.carrinho], estado: 'Solicitado', urgente: S.pedUrgente, solicitante: S.operador, tubosColetados: [] });
    registrar('PedidoCriado', 'Pedido', cod, { exames: S.carrinho.size }); S.carrinho = new Set(); S.pedUrgente = false; render(); aviso(`Pedido ${cod} enviado. Vá para a Coleta.`, 'ok');
  },
  coletar: a => { if (!exigir('coletar', 'coletar amostra')) return; modalColeta(a.dataset.cod, a.dataset.mat); },
  'salvar-coleta': a => {
    limparErros(); const p = S.pedidos.find(x => x.cod === a.dataset.cod), t = L.agruparTubos(p.exames).find(x => x.material === a.dataset.mat), vol = parseFloat($('#c-vol').value), jj = $('#c-jejum') ? parseFloat($('#c-jejum').value) : null; let ok = true;
    if (t.jejum && !(jj >= t.jejum)) { erroCampo('c-jejum', `Jejum insuficiente: este tubo exige ${t.jejum} h (RN-022).`); ok = false; }
    if (!(vol >= t.vol)) { erroCampo('c-vol', `Volume menor que o mínimo de ${fmt(t.vol, 1)} mL (RN-023).`); ok = false; }
    if (!ok) { registrar('AmostraColetada', 'Pedido', p.cod, {}, 'Recusado'); return; }
    const et = 'AMO-' + String(S.contAmostra++).padStart(6, '0'), am = { etiqueta: et, pedido: p.cod, pac: p.pac, material: t.material, exames: t.exames, estado: 'Coletada', vol, hemolisada: $('#c-hem').checked, coletadaEm: '04/11 ' + horaSim().slice(0, 5), validade: '11/11' };
    S.amostras.push(am); p.tubosColetados.push(t.material); if (L.agruparTubos(p.exames).every(x => p.tubosColetados.includes(x.material))) p.estado = 'Coletado';
    S.custodia.push({ et, evento: 'Coletada', local: 'Coleta', op: S.operador, quando: hhmmss(agoraISO()) }); registrar('AmostraColetada', 'Amostra', et, { material: t.material });
    fecharModal(); render(); modalEtiqueta(am);
  },
  'ver-etiqueta': a => modalEtiqueta(S.amostras.find(x => x.etiqueta === a.dataset.et)),
  rejeitar: a => { if (!exigir('rejeitar', 'rejeitar amostra')) return; abrirModal(`Rejeitar ${a.dataset.et}`, `<div class="campos"><label>Motivo<select class="sel" id="rj-mot">${opcoes([['Hemólise', 'Hemólise'], ['Volume insuficiente', 'Volume insuficiente'], ['Material errado', 'Material errado'], ['Etiqueta ilegível', 'Etiqueta ilegível']], '')}</select></label><label>Observação (opcional)<input class="campo" id="rj-obs" maxlength="400"></label></div><p class="mut" style="font-size:13px">A rejeição gera um pedido de recoleta (RN-025).</p>`, `<button class="btn" data-acao="fechar-modal" type="button">Cancelar</button><button class="btn perigo" data-acao="salvar-rejeicao" data-et="${a.dataset.et}" type="button">Rejeitar amostra</button>`); },
  'salvar-rejeicao': a => {
    limparErros(); const obs = $('#rj-obs').value; if (obs.length > 200) { erroCampo('rj-obs', 'Máximo de 200 caracteres.'); return; }
    const am = S.amostras.find(x => x.etiqueta === a.dataset.et), p = S.pedidos.find(x => x.cod === am.pedido);
    am.estado = 'Rejeitada'; p.tubosColetados = p.tubosColetados.filter(m => m !== am.material); p.estado = 'Solicitado';
    for (const [k, v] of Object.entries(S.soroteca)) if (v.et === am.etiqueta) delete S.soroteca[k];
    if (S.hemolisadaPed === p.cod) S.hemolisadaPed = null;
    S.custodia.push({ et: am.etiqueta, evento: 'Rejeitada', local: 'Coleta', op: S.operador, quando: hhmmss(agoraISO()) }); registrar('AmostraRejeitada', 'Amostra', am.etiqueta, { motivo: $('#rj-mot').value }); fecharModal(); render(); aviso('Amostra rejeitada. Recoleta pedida.', 'ok');
  },
  pos: a => {
    const k = a.dataset.k;
    if (S.mover) { if (S.soroteca[k]) { aviso('Posição ocupada. Escolha uma livre.', 'erro'); return; } const orig = Object.keys(S.soroteca).find(x => S.soroteca[x].et === S.mover); S.soroteca[k] = S.soroteca[orig]; delete S.soroteca[orig]; S.custodia.push({ et: S.mover, evento: 'Movida', local: `${LIN[+k.split('-')[0]]}${+k.split('-')[1] + 1}`, op: S.operador, quando: hhmmss(agoraISO()) }); registrar('AmostraMovida', 'Amostra', S.mover); S.mover = null; S.posSel = k; render(); aviso('Amostra movida.', 'ok'); return; }
    S.posSel = k; $('#mapa').innerHTML = mapaSoroteca(); $('#det-pos').innerHTML = detalhePos();
  },
  armazenar: () => {
    if (!exigir('armazenar', 'armazenar amostra')) return; const et = $('#s-am').value, am = S.amostras.find(x => x.etiqueta === et);
    if (am.hemolisada) { aviso('Amostra hemolisada: rejeite e peça recoleta (RN-025).', 'erro'); registrar('AmostraArmazenada', 'Amostra', et, { motivo: 'hemolisada' }, 'Recusado'); return; }
    if (S.soroteca[S.posSel]) { aviso('Essa posição já tem uma amostra (RN-030).', 'erro'); return; }
    S.soroteca[S.posSel] = { est: 'nv', et, pac: am.pac }; am.estado = 'Armazenada'; const [r, c] = S.posSel.split('-').map(Number), nome = `${LIN[r]}${c + 1}`, q = hhmmss(agoraISO());
    S.custodia.push({ et, evento: 'Recebida', local: 'Soroteca', op: S.operador, quando: q }, { et, evento: 'Armazenada', local: nome, op: S.operador, quando: q }); registrar('AmostraArmazenada', 'Amostra', et, { posicao: nome }); render(); aviso(`${et} armazenada em ${nome}.`, 'ok');
  },
  mover: a => { if (!exigir('armazenar', 'mover amostra')) return; S.mover = a.dataset.et; $('#det-pos').innerHTML = detalhePos(); },
  'cancelar-mover': () => { S.mover = null; $('#det-pos').innerHTML = detalhePos(); },
  descartar: a => { if (!exigir('descartar', 'descartar amostra')) return; abrirModal(`Descartar ${a.dataset.et}`, `<div class="campos"><label>Motivo (obrigatório)<input class="campo" id="ds-mot" maxlength="400"></label></div>`, `<button class="btn" data-acao="fechar-modal" type="button">Cancelar</button><button class="btn perigo" data-acao="salvar-descarte" data-et="${a.dataset.et}" type="button">Descartar</button>`); },
  'salvar-descarte': a => {
    limparErros(); const m = $('#ds-mot').value.trim(); if (!m) { erroCampo('ds-mot', 'Informe o motivo do descarte.'); return; } if (m.length > 200) { erroCampo('ds-mot', 'Máximo de 200 caracteres.'); return; }
    const am = S.amostras.find(x => x.etiqueta === a.dataset.et); am.estado = 'Descartada'; for (const [k, v] of Object.entries(S.soroteca)) if (v.et === am.etiqueta) delete S.soroteca[k];
    S.custodia.push({ et: am.etiqueta, evento: 'Descartada', local: 'Soroteca', op: S.operador, quando: hhmmss(agoraISO()) }); registrar('AmostraDescartada', 'Amostra', am.etiqueta); fecharModal(); render(); aviso('Amostra descartada; a posição foi liberada.', 'ok');
  },
  relogio: a => { const f = a.dataset.f; if (f === 'pausar') S.relogio.pausado = !S.relogio.pausado; else { S.relogio.fator = +f; S.relogio.pausado = false; } registrar('RelogioAlterado', 'Simulacao', f); render(); },
  'enviar-equip': () => { if (!exigir('equipamento', 'enviar ao equipamento')) return; enviarAoEquip($('#eq-am').value); render(); aviso('Enviado. Acompanhe a fila e o monitor do protocolo.', 'ok'); },
  reprocessar: a => { if (!exigir('equipamento', 'reprocessar')) return; const e = S.execs.find(x => x.id === +a.dataset.id); if (e.tentativas >= 3) { aviso('Limite de reprocessamentos atingido (RN-042).', 'erro'); return; } e.estado = 'NaFila'; e.restante = e.total; registrar('ExecucaoReprocessada', 'Execucao', e.etiqueta + ':' + e.exame); render(); },
  'novo-cq': () => { if (!exigir('equipamento', 'registrar controle')) return; const seq = [0.2, -0.5, 0.6, -0.1, 0.4]; S.cq.z.push(seq[S.cq.z.length % seq.length]); registrar('ControleRegistrado', 'Equipamento', 'EQ-BIO-01'); render(); aviso(L.westgard(S.cq.z).rejeitar.length ? 'Controle registrado, ainda reprovado.' : 'Controle registrado e aprovado.', L.westgard(S.cq.z).rejeitar.length ? 'erro' : 'ok'); },
  'conf-critico': a => { if (!exigir('validar', 'confirmar crítico')) return; const r = S.resultados[+a.dataset.i]; r.estado = 'CriticoConfirmado'; registrar('CriticoConfirmado', 'Resultado', r.pedido + ':' + r.analito); render(); aviso('Valor crítico confirmado. Agora pode validar.', 'ok'); },
  validar: a => {
    if (!exigir('validar', 'validar resultado')) return; const r = S.resultados[+a.dataset.i], b = bloqueioValidar(r);
    if (b) { aviso(b, 'erro'); registrar('ResultadoValidado', 'Resultado', r.pedido + ':' + r.analito, {}, 'Recusado'); return; }
    if (r.delta && !r.justificativa) { abrirModal('Delta check suspeito', `<div class="alerta"><span aria-hidden="true">!</span><div>${L.ANALITOS[r.analito].nome}: ${fmt(r.valor, L.ANALITOS[r.analito].casas)} contra ${fmt(r.ant, L.ANALITOS[r.analito].casas)} anterior. Variação acima de ${L.ANALITOS[r.analito].delta}%.</div></div><div class="campos"><label>Justificativa (obrigatória)<input class="campo" id="dl-just" maxlength="400"></label></div>`, `<button class="btn" data-acao="fechar-modal" type="button">Cancelar</button><button class="btn primario" data-acao="salvar-delta" data-i="${a.dataset.i}" type="button">Validar com justificativa</button>`); return; }
    r.estado = 'Validado'; r.validadoPor = S.operador; registrar('ResultadoValidado', 'Resultado', r.pedido + ':' + r.analito); render(); aviso('Resultado validado.', 'ok');
  },
  'salvar-delta': a => { limparErros(); const j = $('#dl-just').value.trim(); if (!j) { erroCampo('dl-just', 'Informe a justificativa.'); return; } if (j.length > 200) { erroCampo('dl-just', 'Máximo de 200 caracteres.'); return; } const r = S.resultados[+a.dataset.i]; r.justificativa = j; r.estado = 'Validado'; r.validadoPor = S.operador; registrar('ResultadoValidado', 'Resultado', r.pedido + ':' + r.analito, { delta: true }); fecharModal(); render(); aviso('Resultado validado com justificativa.', 'ok'); },
  liberar: a => {
    if (!exigir('validar', 'liberar laudo')) return; const cod = a.dataset.cod, rs = S.resultados.filter(r => r.pedido === cod), p = S.pedidos.find(x => x.cod === cod);
    if (rs.length < p.exames.length || rs.some(r => r.estado !== 'Validado')) { aviso('Há resultados ainda não validados (RN-055).', 'erro'); registrar('LaudoLiberado', 'Pedido', cod, {}, 'Recusado'); return; }
    const l = { pedido: cod, versao: 1, liberadoPor: S.operador, quando: '04/11/2026 ' + horaSim().slice(0, 5) }; S.laudos.push(l); p.estado = 'Liberado'; registrar('LaudoLiberado', 'Pedido', cod); render(); aviso(`Laudo de ${cod} liberado.`, 'ok'); abrirLaudo(S.laudos.length - 1);
  },
  'abrir-laudo': a => abrirLaudo(+a.dataset.i),
  retificar: a => { if (!exigir('validar', 'retificar laudo')) return; abrirModal('Retificar laudo', `<div class="campos"><label>Motivo da retificação (obrigatório)<input class="campo" id="rt-mot" maxlength="400"></label></div><p class="mut" style="font-size:13px">O laudo liberado é imutável: a retificação cria a versão ${S.laudos[+a.dataset.i].versao + 1} e a anterior continua consultável.</p>`, `<button class="btn" data-acao="fechar-modal" type="button">Cancelar</button><button class="btn primario" data-acao="salvar-retif" data-i="${a.dataset.i}" type="button">Criar nova versão</button>`); },
  'salvar-retif': a => { limparErros(); const m = $('#rt-mot').value.trim(); if (!m) { erroCampo('rt-mot', 'Informe o motivo.'); return; } if (m.length > 200) { erroCampo('rt-mot', 'Máximo de 200 caracteres.'); return; } const o = S.laudos[+a.dataset.i]; S.laudos.push({ pedido: o.pedido, versao: o.versao + 1, liberadoPor: S.operador, quando: '04/11/2026 ' + horaSim().slice(0, 5), motivo: m }); registrar('LaudoRetificado', 'Pedido', o.pedido, { versao: o.versao + 1 }); fecharModal(); render(); aviso('Nova versão do laudo criada.', 'ok'); },
  verificar: async () => { const est = S, r = await L.verificarCadeia(est.auditoria); est.verif = { ...r, n: est.auditoria.length }; est.simAud = null; registrar('CadeiaVerificada', 'Auditoria', '-', { ok: r.ok }); render(); },
  adulterar: async () => {
    if (S.auditoria.length < 3) { aviso('Faça algumas ações antes: a cadeia ainda é curta.', 'erro'); return; }
    const copia = S.auditoria.map(e => ({ ...e })), alvo = copia[Math.floor(copia.length / 2)]; alvo.operador = 'XXX-99'; const r = await L.verificarCadeia(copia);
    S.verif = null; S.simAud = { copia, quebrouEm: r.quebrouEm }; registrar('AdulteracaoSimulada', 'Auditoria', String(alvo.seq)); render();
  },
  'limpar-sim': () => { S.simAud = null; render(); },
  'sub-painel': a => { S.subPainel = a.dataset.id; $$('#tela .aba').forEach(b => b.setAttribute('aria-selected', b === a)); $('#painel-corpo').innerHTML = painelConteudo(); },
  'rodar-testes': () => rodarTestes(),
  disparar: a => disparar(CORPUS[S.ataque.cat][+a.dataset.i]),
  'disparar-livre': () => { const v = $('#payload-livre').value; if (!v) { aviso('Digite um payload.', 'erro'); return; } disparar(v.slice(0, 500)); },
  rajada: () => { registrar('LimiteDeTaxaExcedido', 'LabAtaque', '200 req', { aceitas: 30, recusadas: 170 }, 'Bloqueado'); registrar('IpBloqueado', 'LabAtaque', '10 min', {}, 'Bloqueado'); S.ataque.res = { payload: '200 requisições em 10 segundos', status: 429, defesa: 'Limite de taxa', texto: 'As 30 primeiras escritas por minuto passam; as outras 170 recebem 429. Com 5 bloqueios no mesmo minuto o IP fica bloqueado por 10 minutos.', sql: false, evento: 'LimiteDeTaxaExcedido + IpBloqueado' }; render(); },
};
function abrirLaudo(i) {
  const l = S.laudos[i]; abrirModal(`Laudo ${l.pedido} · v${l.versao}`, documentoLaudo(l), `${pode('validar') ? `<button class="btn" data-acao="retificar" data-i="${i}" type="button">Retificar</button>` : ''}<button class="btn primario" data-acao="fechar-modal" type="button">Fechar</button>`);
}

/* ================= eventos globais ================= */
document.addEventListener('click', e => {
  const a = e.target.closest('[data-acao]'); if (!a) { if (e.target.id === 'fundo-modal') fecharModal(); return; }
  if (a.matches('input[type=checkbox]') ) return; // tratado em change
  const f = ACOES[a.dataset.acao]; if (f) f(a, e);
});
document.addEventListener('change', e => {
  const a = e.target; if (a.matches('input[type=checkbox][data-acao]')) { ACOES[a.dataset.acao](a, e); return; }
  const m = a.dataset.muda; if (!m) return;
  const v = a.value;
  if (m === 'ataqueCat') { S.ataque.cat = v; S.ataque.res = null; } else if (m === 'ataqueAlvo') S.ataque.alvo = v; else S[m] = v;
  render();
});
document.addEventListener('input', e => {
  const a = e.target, k = a.dataset.entrada; if (!k) return;
  if (k === 'busca') { S.busca = a.value; $('#tab-pac').innerHTML = tabelaPacientes(); }
  else if (k === 'pedBusca') { S.pedBusca = a.value; $('#catalogo').innerHTML = catalogoHTML(); }
  else if (k === 'contar') { const c = $('#m-cont'), max = +a.dataset.max; c.textContent = `${a.value.length.toLocaleString('pt-BR')} / ${max.toLocaleString('pt-BR')}`; c.classList.toggle('estouro', a.value.length > max); }
});
document.addEventListener('keydown', e => {
  if (e.key === 'Escape' && !$('#fundo-modal').hidden) fecharModal();
  if ((e.key === 'Enter' || e.key === ' ') && e.target.matches('td[data-acao]')) { e.preventDefault(); ACOES[e.target.dataset.acao](e.target); }
});
$('#papel').addEventListener('change', e => { trocarPapel(e.target.value); render(); });
$('#operador').addEventListener('change', e => { S.operador = e.target.value; S.sessaoId = novoId(); registrar('OperadorTrocado', 'Sessao', S.operador); render(); });
$('#b-tema').addEventListener('click', () => { const r = document.documentElement, esc2 = r.dataset.theme ? r.dataset.theme === 'dark' : matchMedia('(prefers-color-scheme: dark)').matches; r.dataset.theme = esc2 ? 'light' : 'dark'; $('#b-tema').textContent = esc2 ? 'Tema claro' : 'Tema escuro'; });
$('#b-idioma').addEventListener('click', () => aviso('Inglês é da Fase 8; o esboço está só em português.'));
$('#b-reiniciar').addEventListener('click', () => { S = novoEstado('critico'); registrar('SessaoCriada'); render(); aviso('Sessão reiniciada: tudo voltou ao começo.', 'ok'); });
{ const r = document.documentElement; $('#b-tema').textContent = (r.dataset.theme ? r.dataset.theme === 'dark' : matchMedia('(prefers-color-scheme: dark)').matches) ? 'Tema escuro' : 'Tema claro'; }

registrar('SessaoCriada');
render();
