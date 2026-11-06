import { test } from 'node:test';
import assert from 'node:assert/strict';
import { ligarLinhaClicavel } from '../../../src/Bancada.Web/wwwroot/js/comum/linhas.js';
import { ligarModal } from '../../../src/Bancada.Web/wwwroot/js/comum/modal.js';
import { iniciar } from '../../../src/Bancada.Web/wwwroot/js/comum/iniciar.js';

function alvo(extra = {}) {
  const ouvintes = {};
  return { ouvintes, addEventListener(tipo, fn) { ouvintes[tipo] = fn; }, ...extra };
}

test('clicar na linha vai para o data-href', () => {
  const destinos = [];
  const linha = alvo({ dataset: { href: '/pedidos/7' } });
  ligarLinhaClicavel({ location: { assign: url => destinos.push(url) } }, linha);
  linha.ouvintes.click({ target: { closest: () => null } });
  assert.deepEqual(destinos, ['/pedidos/7']);
});

test('clicar num controle de dentro da linha não navega por ela', () => {
  const destinos = [];
  const linha = alvo({ dataset: { href: '/pedidos/7' } });
  ligarLinhaClicavel({ location: { assign: url => destinos.push(url) } }, linha);
  linha.ouvintes.click({ target: { closest: () => ({}) } });
  assert.deepEqual(destinos, []);
});

function dialogoFalso() {
  const botao = alvo();
  const d = alvo({ dataset: { volta: '/agenda?semana=1' }, aberto: false, fechou: 0, querySelectorAll: () => [botao], showModal() { d.aberto = true; }, close() { d.fechou++; } });
  d.botao = botao;
  return d;
}

test('modal abre ao ligar e volta ao endereço de origem quando fecha', () => {
  const destinos = [];
  const d = dialogoFalso();
  ligarModal({ location: { assign: url => destinos.push(url) } }, d);
  assert.equal(d.aberto, true);
  d.ouvintes.close();
  assert.deepEqual(destinos, ['/agenda?semana=1']);
});

test('modal fecha no botão e no clique no fundo, mas não no clique dentro', () => {
  const d = dialogoFalso();
  ligarModal({ location: { assign() {} } }, d);
  d.botao.ouvintes.click();
  d.ouvintes.click({ target: d });
  d.ouvintes.click({ target: {} });
  assert.equal(d.fechou, 2);
});

test('iniciar liga linhas clicáveis e modais da página', () => {
  const linha = alvo({ dataset: { href: '/x' } });
  const d = dialogoFalso();
  const doc = {
    documentElement: { dataset: {} },
    querySelectorAll: seletor => (seletor === '[data-href]' ? [linha] : seletor === '[data-modal]' ? [d] : []),
  };
  iniciar(doc, { matchMedia: () => ({ matches: false }) });
  assert.equal(typeof linha.ouvintes.click, 'function');
  assert.equal(d.aberto, true);
});
