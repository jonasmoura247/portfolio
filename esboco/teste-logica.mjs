import * as L from './logica.js';
import assert from 'node:assert/strict';
let n = 0;
const ok = (nome, f) => { f(); n++; console.log('ok', nome); };
const okA = async (nome, f) => { await f(); n++; console.log('ok', nome); };

ok('code128 ida e volta', () => { for (const t of ['AMO-000123', 'PED-000045', 'A', ' ~', 'Bancada 128']) assert.equal(L.decodificar128(L.code128B(t).larguras), t); });
ok('code128 todos os ASCII imprimíveis', () => { const t = Array.from({ length: 95 }, (_, i) => String.fromCharCode(32 + i)).join(''); assert.equal(L.decodificar128(L.code128B(t).larguras), t); });
ok('code128 rejeita não ASCII', () => { assert.throws(() => L.code128B('çã')); assert.throws(() => L.code128B('')); });
ok('code128 módulos: 11 por símbolo + 13 da parada', () => { const { larguras, valores } = L.code128B('AMO'); const soma = [...larguras].reduce((s, d) => s + +d, 0); assert.equal(soma, 11 * (valores.length - 1) + 13); });
ok('svg tem barras', () => assert.match(L.code128SVG('AMO-000123'), /<rect /));
ok('classificar limites', () => { const f = L.faixaPara('POT', 'M'); assert.equal(L.classificar(3.5, f), 'N'); assert.equal(L.classificar(5.1, f), 'N'); assert.equal(L.classificar(5.2, f), 'H'); assert.equal(L.classificar(3.4, f), 'L'); assert.equal(L.classificar(6.6, f), 'CH'); assert.equal(L.classificar(2.4, f), 'CL'); assert.equal(L.classificar(6.5, f), 'H'); });
ok('faixa por sexo', () => { assert.equal(L.classificar(1.2, L.faixaPara('CRE', 'M')), 'N'); assert.equal(L.classificar(1.2, L.faixaPara('CRE', 'F')), 'H'); });
ok('faixa aberta', () => { assert.equal(L.classificar(150, L.faixaPara('COL', 'M')), 'N'); assert.equal(L.classificar(200, L.faixaPara('COL', 'M')), 'H'); assert.equal(L.textoFaixa(L.faixaPara('COL', 'M')), '< 190'); });
ok('delta check', () => { assert.equal(L.deltaSuspeito(7.1, 4.4, 25), true); assert.equal(L.deltaSuspeito(1.4, 1.2, 40), false); assert.equal(L.deltaSuspeito(5, null, 25), false); });
ok('tubos por material', () => { const t = L.agruparTubos(['GLI', 'URE', 'CRE', 'HEM']); assert.equal(t.length, 2); assert.equal(t.find(x => x.material === 'Soro').exames.length, 3); assert.equal(t.find(x => x.material === 'Soro').jejum, 8); });
ok('valores simulados ficam na faixa normal', () => { for (const c of Object.keys(L.ANALITOS)) for (const s of ['M', 'F']) assert.equal(L.classificar(L.valorSimulado(c, s, 'PED-1'), L.faixaPara(c, s)), 'N', c + s); });
ok('valores simulados são determinísticos', () => assert.equal(L.valorSimulado('GLI', 'M', 'x'), L.valorSimulado('GLI', 'M', 'x')));
ok('westgard 1-2s é só aviso', () => { const r = L.westgard([0.1, 2.3]); assert.deepEqual(r.aviso, ['1-2s']); assert.deepEqual(r.rejeitar, []); });
ok('westgard 1-3s', () => assert.deepEqual(L.westgard([0, 3.2]).rejeitar, ['1-3s']));
ok('westgard 2-2s', () => assert.ok(L.westgard([0, 2.4, 2.2]).rejeitar.includes('2-2s')));
ok('westgard R-4s', () => assert.ok(L.westgard([0, 2.4, -2.2]).rejeitar.includes('R-4s')));
ok('westgard 4-1s', () => assert.ok(L.westgard([0, 1.2, 1.5, 1.1, 1.3]).rejeitar.includes('4-1s')));
ok('westgard 10x', () => assert.ok(L.westgard(Array(10).fill(0.3)).rejeitar.includes('10x')));
ok('westgard série inicial aprovada', () => { const r = L.westgard(L.serieInicialCQ()); assert.deepEqual(r.rejeitar, []); });
ok('astm checksum', () => { const q = L.quadroASTM('1', 'L|1|N'); assert.ok(L.validarQuadroASTM(q.bruto)); assert.ok(!L.validarQuadroASTM(q.bruto.replace(q.ck + '\r\n', '00\r\n'))); assert.ok(!L.validarQuadroASTM('lixo')); });
ok('simulação conclui e respeita capacidade', () => {
  const ex = Array.from({ length: 6 }, (_, i) => ({ id: i, eq: 'EQ-BIO-01', estado: 'NaFila', prioridade: 0, restante: 6, total: 6, tentativas: 0 }));
  L.passo(ex, 0.1); assert.equal(ex.filter(e => e.estado === 'Processando').length, 4);
  let fin = 0; for (let i = 0; i < 400 && fin < 6; i++) fin += L.passo(ex, 0.5).concluidas.length; assert.equal(fin, 6);
});
ok('simulação: falha na 1ª tentativa e reprocesso conclui', () => {
  const e = { id: 1, eq: 'EQ-BIO-01', estado: 'NaFila', prioridade: 0, restante: 8, total: 8, tentativas: 0, falhar: true };
  for (let i = 0; i < 100 && e.estado !== 'Erro'; i++) L.passo([e], 0.5);
  assert.equal(e.estado, 'Erro'); assert.equal(e.tentativas, 1);
  e.estado = 'NaFila'; e.restante = e.total;
  for (let i = 0; i < 100 && e.estado !== 'Concluido'; i++) L.passo([e], 0.5);
  assert.equal(e.estado, 'Concluido');
});
ok('urgente passa na frente', () => {
  const mk = (id, prioridade) => ({ id, eq: 'EQ-HEM-01', estado: 'NaFila', prioridade, restante: 5, total: 5, tentativas: 0 });
  const ex = [mk(1, 0), mk(2, 0), mk(3, 1)];
  L.passo(ex, 0.1); assert.equal(ex.find(e => e.id === 3).estado, 'Processando'); assert.equal(ex.find(e => e.id === 2).estado, 'NaFila');
});
ok('esc', () => assert.equal(L.esc('<script>"&\''), '&lt;script&gt;&quot;&amp;&#39;'));
ok('idade', () => { assert.equal(L.idade('1974-03-12'), 52); assert.equal(L.idade('2000-12-31'), 25); });
ok('acesso por papel', () => { assert.ok(!L.ACESSO.Analista.includes('agenda')); assert.ok(!L.ACESSO.Medico.includes('coleta')); assert.equal(L.ACESSO.Admin.length, 15); });

await okA('cadeia de auditoria válida e detecta adulteração', async () => {
  let ant = await L.GENESIS(); const ev = [];
  for (let i = 1; i <= 6; i++) {
    const e = await L.encadear(ant, { seq: i, quando: '2026-11-04T10:00:0' + i + '.000Z', papel: 'Medico', operador: 'MED-01', acao: 'Evento' + i, alvoTipo: 'Pedido', alvoId: 'PED-1|\\x', detalhe: '{}', resultado: 'Ok' });
    ev.push(e); ant = e.hash;
  }
  assert.deepEqual(await L.verificarCadeia(ev), { ok: true, quebrouEm: null });
  for (const campo of ['papel', 'operador', 'acao', 'alvoId', 'detalhe', 'resultado', 'quando']) {
    const c = ev.map(x => ({ ...x })); c[2][campo] = c[2][campo] + 'x';
    assert.deepEqual(await L.verificarCadeia(c), { ok: false, quebrouEm: 3 }, campo);
  }
  assert.equal((await L.verificarCadeia(ev.filter(x => x.seq !== 4))).ok, false);
  assert.equal((await L.verificarCadeia([ev[0], ev[2], ev[1], ...ev.slice(3)])).ok, false);
});
await okA('canônico escapa | e \\ (sem colisão)', async () => {
  const a = { seq: 1, quando: 'q', papel: 'p', operador: 'o', acao: 'a|b', alvoTipo: 'c', alvoId: 'd', detalhe: 'e', resultado: 'r' };
  assert.notEqual(L.canonico(a), L.canonico({ ...a, acao: 'a', alvoTipo: 'b|c' }));
});
console.log('\n' + n + ' testes passaram');
