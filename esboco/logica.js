/* Lógica pura do esboço do Bancada: sem DOM, testável em Node.
   Espelha o que o vault descreve (Protocolos, Regras-de-Negocio, Auditoria-Encadeada). */

export const esc = s => String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
export const fmt = (n, casas = 1) => Number(n).toLocaleString('pt-BR', { minimumFractionDigits: casas, maximumFractionDigits: casas });

/* ---------- Code 128 (conjunto B) ---------- */
export const PADROES_128 = '212222 222122 222221 121223 121322 131222 122213 122312 132212 221213 221312 231212 112232 122132 122231 113222 123122 123221 223211 221132 221231 213212 223112 312131 311222 321122 321221 312212 322112 322211 212123 212321 232121 111323 131123 131321 112313 132113 132311 211313 231113 231311 112133 112331 132131 113123 113321 133121 313121 211331 231131 213113 213311 213131 311123 311321 331121 312113 312311 332111 314111 221411 431111 111224 111422 121124 121421 141122 141221 112214 112412 122114 122411 142112 142211 241211 221114 413111 241112 134111 111242 121142 121241 114212 124112 124211 411212 421112 421211 212141 214121 412121 111143 111341 131141 114113 114311 411113 411311 113141 114131 311141 411131 211412 211214 211232 2331112'.split(' ');

export function code128B(texto) {
  const cods = [...texto].map(c => c.charCodeAt(0));
  if (!cods.length || cods.some(c => c < 32 || c > 126)) throw new RangeError('Code 128 B aceita ASCII imprimível');
  const valores = [104, ...cods.map(c => c - 32)];
  const soma = valores.reduce((s, v, i) => s + (i === 0 ? v : v * i), 0);
  valores.push(soma % 103, 106);
  return { valores, larguras: valores.map(v => PADROES_128[v]).join('') };
}
export function decodificar128(larguras) {
  const simbolos = []; let i = 0;
  while (i < larguras.length) { const n = i + 6 < larguras.length + 1 && larguras.slice(i, i + 7) === PADROES_128[106] ? 7 : 6; simbolos.push(larguras.slice(i, i + n)); i += n; }
  const valores = simbolos.map(s => PADROES_128.indexOf(s));
  if (valores.some(v => v < 0) || valores[0] !== 104 || valores.at(-1) !== 106) throw new Error('símbolo inválido');
  const dados = valores.slice(1, -2);
  const soma = valores.slice(0, -2).reduce((s, v, k) => s + (k === 0 ? v : v * k), 0) % 103;
  if (soma !== valores.at(-2)) throw new Error('dígito verificador não confere');
  return dados.map(v => String.fromCharCode(v + 32)).join('');
}
export function code128SVG(texto, altura = 56, modulo = 2) {
  const { larguras } = code128B(texto);
  let x = 10 * modulo, barras = '', bar = true;
  for (const d of larguras) { const w = +d * modulo; if (bar) barras += `<rect x="${x}" y="0" width="${w}" height="${altura}"/>`; x += w; bar = !bar; }
  const W = x + 10 * modulo;
  return `<svg class="code128" viewBox="0 0 ${W} ${altura}" role="img" aria-label="Código de barras ${esc(texto)}" preserveAspectRatio="xMidYMid meet">${barras}</svg>`;
}

/* ---------- dados de domínio (fictícios) ---------- */
export const PAPEIS = { Recepcao: 'Recepção', Medico: 'Médico(a)', Coleta: 'Coleta', Analista: 'Analista', Admin: 'Admin' };
export const OPERADORES = [
  { id: 'REC-01', nome: 'Ana Lopes', papel: 'Recepcao' }, { id: 'REC-02', nome: 'Diego Pires', papel: 'Recepcao' },
  { id: 'MED-01', nome: 'Dra. Marta Leão', papel: 'Medico' }, { id: 'MED-02', nome: 'Dr. Caio Brandão', papel: 'Medico' },
  { id: 'COL-01', nome: 'Bete Nunes', papel: 'Coleta' }, { id: 'COL-02', nome: 'Léo Farias', papel: 'Coleta' },
  { id: 'ANA-01', nome: 'Vera Salgado', papel: 'Analista' }, { id: 'ANA-02', nome: 'Hugo Neves', papel: 'Analista' },
  { id: 'ADM-01', nome: 'Admin Demo', papel: 'Admin' },
];
export const PACIENTES = [
  { id: 'PAC-0001', nome: 'Helena Prado', sexo: 'F', nasc: '1988-04-12' }, { id: 'PAC-0003', nome: 'Marcos Tavares', sexo: 'M', nasc: '1975-09-02' },
  { id: 'PAC-0005', nome: 'Lívia Moraes', sexo: 'F', nasc: '1992-01-30' }, { id: 'PAC-0008', nome: 'Tiago Andrade', sexo: 'M', nasc: '1983-06-18' },
  { id: 'PAC-0009', nome: 'Renata Siqueira', sexo: 'F', nasc: '1969-11-23' }, { id: 'PAC-0011', nome: 'Paulo Ferraz', sexo: 'M', nasc: '1957-03-07' },
  { id: 'PAC-0014', nome: 'Rui Almeida', sexo: 'M', nasc: '1974-03-12' }, { id: 'PAC-0017', nome: 'Camila Duarte', sexo: 'F', nasc: '1996-08-15' },
  { id: 'PAC-0021', nome: 'Otávio Brito', sexo: 'M', nasc: '1990-12-01' }, { id: 'PAC-0022', nome: 'Joana Veloso', sexo: 'F', nasc: '2000-05-27' },
  { id: 'PAC-0030', nome: 'Bruno Calado', sexo: 'M', nasc: '1964-02-19' }, { id: 'PAC-0044', nome: 'Silvia Matos', sexo: 'F', nasc: '1981-07-04' },
  { id: 'PAC-0047', nome: 'Marina Teixeira', sexo: 'F', nasc: '1979-10-09' }, { id: 'PAC-0052', nome: 'Edu Rangel', sexo: 'M', nasc: '2001-02-11' },
];
export const idade = (nasc, ref = '2026-11-04') => {
  const [a, m, d] = nasc.split('-').map(Number), [ra, rm, rd] = ref.split('-').map(Number);
  return ra - a - (rm < m || (rm === m && rd < d) ? 1 : 0);
};
export const EQUIPAMENTOS = {
  'EQ-BIO-01': { nome: 'Bioquímica Alfa', protocolo: 'ASTM', cap: 4 },
  'EQ-HEM-01': { nome: 'Hematologia Beta', protocolo: 'HL7', cap: 2 },
  'EQ-IMU-01': { nome: 'Imunoensaio Gama', protocolo: 'HL7', cap: 6 },
};
export const EXAMES = {
  GLI: { nome: 'Glicose', material: 'Soro', vol: 0.5, jejum: 8, tempo: 8, eq: 'EQ-BIO-01', analitos: ['GLI'] },
  URE: { nome: 'Ureia', material: 'Soro', vol: 0.5, jejum: 0, tempo: 8, eq: 'EQ-BIO-01', analitos: ['URE'] },
  CRE: { nome: 'Creatinina', material: 'Soro', vol: 0.5, jejum: 0, tempo: 8, eq: 'EQ-BIO-01', analitos: ['CRE'] },
  COL: { nome: 'Colesterol total', material: 'Soro', vol: 0.5, jejum: 12, tempo: 10, eq: 'EQ-BIO-01', analitos: ['COL'] },
  TRI: { nome: 'Triglicerídeos', material: 'Soro', vol: 0.5, jejum: 12, tempo: 10, eq: 'EQ-BIO-01', analitos: ['TRI'] },
  ELE: { nome: 'Eletrólitos (sódio e potássio)', material: 'Soro', vol: 0.5, jejum: 0, tempo: 6, eq: 'EQ-BIO-01', analitos: ['SOD', 'POT'] },
  HEM: { nome: 'Hemograma simplificado', material: 'Sangue total EDTA', vol: 2, jejum: 0, tempo: 12, eq: 'EQ-HEM-01', analitos: ['HGB', 'LEU', 'PLQ'] },
  TSH: { nome: 'TSH', material: 'Soro', vol: 0.5, jejum: 0, tempo: 20, eq: 'EQ-IMU-01', analitos: ['TSH'] },
  T4L: { nome: 'T4 livre', material: 'Soro', vol: 0.5, jejum: 0, tempo: 20, eq: 'EQ-IMU-01', analitos: ['T4L'] },
  FER: { nome: 'Ferritina', material: 'Soro', vol: 0.5, jejum: 0, tempo: 20, eq: 'EQ-IMU-01', analitos: ['FER'] },
};
/* faixas: [sexo, nmin, nmax, cmin, cmax] ; delta = % aceito */
export const ANALITOS = {
  GLI: { nome: 'Glicose em jejum', un: 'mg/dL', casas: 0, delta: 50, faixas: [['A', 70, 99, 40, 500]] },
  URE: { nome: 'Ureia', un: 'mg/dL', casas: 0, delta: 50, faixas: [['A', 15, 45, null, 180]] },
  CRE: { nome: 'Creatinina', un: 'mg/dL', casas: 1, delta: 40, faixas: [['M', 0.7, 1.3, null, 7], ['F', 0.6, 1.1, null, 7]] },
  COL: { nome: 'Colesterol total', un: 'mg/dL', casas: 0, delta: 50, faixas: [['A', null, 190, null, null]] },
  TRI: { nome: 'Triglicerídeos', un: 'mg/dL', casas: 0, delta: 50, faixas: [['A', null, 150, null, 1000]] },
  SOD: { nome: 'Sódio', un: 'mmol/L', casas: 0, delta: 50, faixas: [['A', 135, 145, 120, 160]] },
  POT: { nome: 'Potássio', un: 'mmol/L', casas: 1, delta: 25, faixas: [['A', 3.5, 5.1, 2.5, 6.5]] },
  HGB: { nome: 'Hemoglobina', un: 'g/dL', casas: 1, delta: 20, faixas: [['M', 13.5, 17.5, 7, 20], ['F', 12, 15.5, 7, 20]] },
  LEU: { nome: 'Leucócitos', un: '/µL', casas: 0, delta: 50, faixas: [['A', 4000, 11000, 1000, 50000]] },
  PLQ: { nome: 'Plaquetas', un: '/µL', casas: 0, delta: 50, faixas: [['A', 150000, 450000, 20000, 1000000]] },
  TSH: { nome: 'TSH', un: 'mUI/L', casas: 2, delta: 50, faixas: [['A', 0.4, 4, null, null]] },
  T4L: { nome: 'T4 livre', un: 'ng/dL', casas: 1, delta: 50, faixas: [['A', 0.8, 1.8, null, null]] },
  FER: { nome: 'Ferritina', un: 'ng/mL', casas: 0, delta: 50, faixas: [['M', 30, 400, null, null], ['F', 15, 150, null, null]] },
};
export function faixaPara(cod, sexo) {
  const f = ANALITOS[cod].faixas.find(x => x[0] === sexo) || ANALITOS[cod].faixas.find(x => x[0] === 'A');
  return { nmin: f[1], nmax: f[2], cmin: f[3], cmax: f[4] };
}
export function classificar(v, f) {
  if (f.cmin != null && v < f.cmin) return 'CL';
  if (f.cmax != null && v > f.cmax) return 'CH';
  if (f.nmin != null && v < f.nmin) return 'L';
  if (f.nmax != null && v > f.nmax) return 'H';
  return 'N';
}
export const textoFaixa = f => f.nmin != null && f.nmax != null ? `${fmt(f.nmin, f.nmin % 1 ? 1 : 0)} – ${fmt(f.nmax, f.nmax % 1 ? 1 : 0)}` : f.nmax != null ? `< ${fmt(f.nmax, 0)}` : `> ${fmt(f.nmin, 1)}`;
export const deltaSuspeito = (v, ant, pct) => ant != null && ant !== 0 && Math.abs(v - ant) / Math.abs(ant) * 100 > pct;

/* tubos por material (RN-021) */
export function agruparTubos(cods) {
  const por = {};
  cods.forEach(c => { const e = EXAMES[c]; (por[e.material] ||= { material: e.material, exames: [], vol: 0, jejum: 0 }); const t = por[e.material]; t.exames.push(c); t.vol = Math.max(t.vol, e.vol); t.jejum = Math.max(t.jejum, e.jejum); });
  return Object.values(por);
}

/* ---------- valores simulados determinísticos ---------- */
const fnv = s => { let h = 2166136261; for (const c of s) { h ^= c.charCodeAt(0); h = Math.imul(h, 16777619); } return (h >>> 0) / 4294967295; };
export function valorSimulado(cod, sexo, semente) {
  const f = faixaPara(cod, sexo);
  const base = f.nmin != null && f.nmax != null ? (f.nmin + f.nmax) / 2 : f.nmax != null ? f.nmax * 0.7 : f.nmin * 1.4;
  const v = base * (1 + (fnv(semente + cod) - 0.5) * 0.06);
  const k = 10 ** ANALITOS[cod].casas;
  return Math.round(v * k) / k;
}

/* ---------- controle de qualidade: regras de Westgard ---------- */
export function westgard(z) {
  const n = z.length, u = z[n - 1], p = z[n - 2], rej = [], aviso = [];
  if (Math.abs(u) > 2) aviso.push('1-2s');
  if (Math.abs(u) > 3) rej.push('1-3s');
  if (n >= 2 && ((u > 2 && p > 2) || (u < -2 && p < -2))) rej.push('2-2s');
  if (n >= 2 && ((u > 2 && p < -2) || (u < -2 && p > 2))) rej.push('R-4s');
  if (n >= 4 && (z.slice(-4).every(x => x > 1) || z.slice(-4).every(x => x < -1))) rej.push('4-1s');
  if (n >= 10 && (z.slice(-10).every(x => x > 0) || z.slice(-10).every(x => x < 0))) rej.push('10x');
  return { rejeitar: rej, aviso };
}
export const serieInicialCQ = () => [0.3, -0.4, 0.6, -0.2, 0.1, 0.8, -0.7, 0.2, -0.1, 0.5, -0.5, 2.3, 0.4, -0.3, 0.7, -0.6, 0.2, -0.2, 0.5, -0.4];

/* ---------- ASTM: quadro com checksum (soma de FN..ETX módulo 256) ---------- */
export function quadroASTM(n, texto) {
  const corpo = n + texto + '\x03';
  const ck = ([...corpo].reduce((s, c) => s + c.charCodeAt(0), 0) % 256).toString(16).toUpperCase().padStart(2, '0');
  return { corpo, ck, bruto: '\x02' + corpo + ck + '\r\n' };
}
export function validarQuadroASTM(bruto) {
  const m = /^\x02([0-7])([^\x03]*)\x03([0-9A-F]{2})\r\n$/.exec(bruto);
  if (!m) return false;
  return quadroASTM(m[1], m[2]).ck === m[3];
}

/* ---------- auditoria encadeada por hash (SHA-256) ---------- */
export async function sha256(txt) {
  const b = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(txt));
  return [...new Uint8Array(b)].map(x => x.toString(16).padStart(2, '0')).join('');
}
const escCampo = s => String(s).replace(/\\/g, '\\\\').replace(/\|/g, '\\|');
export const canonico = e => [e.seq, e.quando, e.papel, e.operador, e.acao, e.alvoTipo, e.alvoId, e.detalhe, e.resultado].map(escCampo).join('|');
export const GENESIS = () => sha256('BANCADA-GENESIS');
export async function encadear(anterior, ev) { return { ...ev, hashAnterior: anterior, hash: await sha256(anterior + '\n' + canonico(ev)) }; }
export async function verificarCadeia(eventos) {
  let ant = await GENESIS();
  for (const e of eventos) {
    if (e.hashAnterior !== ant || e.hash !== await sha256(ant + '\n' + canonico(e))) return { ok: false, quebrouEm: e.seq };
    ant = e.hash;
  }
  return { ok: true, quebrouEm: null };
}

/* ---------- simulação do equipamento (sem threads: avança com o relógio) ---------- */
export function passo(execs, dtMin) {
  const cap = Object.fromEntries(Object.entries(EQUIPAMENTOS).map(([k, v]) => [k, v.cap]));
  for (const [eq, c] of Object.entries(cap)) {
    const livres = c - execs.filter(e => e.eq === eq && e.estado === 'Processando').length;
    execs.filter(e => e.eq === eq && e.estado === 'NaFila').sort((a, b) => b.prioridade - a.prioridade || a.id - b.id).slice(0, Math.max(livres, 0)).forEach(e => { e.estado = 'Processando'; });
  }
  const concluidas = [], falhas = [];
  execs.filter(e => e.estado === 'Processando').forEach(e => {
    e.restante = Math.max(0, e.restante - dtMin);
    if (e.falhar && e.tentativas === 0 && e.restante <= e.total * 0.5) { e.estado = 'Erro'; e.tentativas = 1; falhas.push(e); }
    else if (e.restante === 0) { e.estado = 'Concluido'; concluidas.push(e); }
  });
  return { concluidas, falhas };
}

/* ---------- acesso por papel (espelha a Matriz-de-Autorizacao) ---------- */
export const ACESSO = {
  Recepcao: ['inicio', 'pacientes', 'agenda', 'atendimento', 'pedido', 'operacao', 'painel', 'ataque'],
  Medico: ['inicio', 'pacientes', 'agenda', 'atendimento', 'prontuario', 'pedido', 'resultados', 'operacao', 'painel', 'ataque'],
  Coleta: ['inicio', 'pacientes', 'prontuario', 'pedido', 'coleta', 'soroteca', 'operacao', 'painel', 'ataque'],
  Analista: ['inicio', 'pacientes', 'pedido', 'soroteca', 'equipamento', 'qualidade', 'resultados', 'auditoria', 'operacao', 'painel', 'ataque'],
  Admin: ['inicio', 'pacientes', 'agenda', 'atendimento', 'prontuario', 'pedido', 'coleta', 'soroteca', 'equipamento', 'qualidade', 'resultados', 'auditoria', 'operacao', 'painel', 'ataque'],
};
