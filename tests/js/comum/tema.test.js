import { test } from 'node:test';
import assert from 'node:assert/strict';
import { alternarTema, comTransicao, restaurarTema } from '../../../src/Bancada.Web/wwwroot/js/comum/tema.js';

const guardado = (inicial = null) => ({ valor: inicial, localStorage: { getItem() { return guardado.ultimo; }, setItem(_, v) { guardado.ultimo = v; } } });
const bloqueado = { get localStorage() { throw new Error('SecurityError'); } };

test('sem escolha, parte do que o sistema pede: sistema escuro vira claro', () => {
  const raiz = { dataset: {} };
  assert.equal(alternarTema(raiz, true, guardado()), 'light');
  assert.equal(raiz.dataset.theme, 'light');
});

test('sem escolha, sistema claro vira escuro', () => {
  assert.equal(alternarTema({ dataset: {} }, false, guardado()), 'dark');
});

test('com escolha, alterna independente do sistema', () => {
  const raiz = { dataset: { theme: 'dark' } };
  assert.equal(alternarTema(raiz, false, guardado()), 'light');
  assert.equal(alternarTema(raiz, false, guardado()), 'dark');
});

test('a escolha sobrevive à troca de página', () => {
  const janela = guardado();
  alternarTema({ dataset: {} }, false, janela);
  const outra = { dataset: {} };
  restaurarTema(outra, janela);
  assert.equal(outra.dataset.theme, 'dark');
});

test('sem nada salvo, deixa o tema do sistema', () => {
  guardado.ultimo = null;
  const raiz = { dataset: {} };
  restaurarTema(raiz, guardado());
  assert.equal(raiz.dataset.theme, undefined);
});

test('com View Transitions, a troca passa pela transição; sem suporte, roda direto', () => {
  let rodou = 0;
  const acao = () => { rodou++; };
  const recebidas = [];
  comTransicao({ startViewTransition: fn => { recebidas.push(fn); } }, acao);
  assert.deepEqual(recebidas, [acao]);
  assert.equal(rodou, 0);
  comTransicao({}, acao);
  assert.equal(rodou, 1);
});

test('armazenamento bloqueado não quebra a página', () => {
  const raiz = { dataset: {} };
  assert.equal(alternarTema(raiz, false, bloqueado), 'dark');
  assert.doesNotThrow(() => restaurarTema(raiz, bloqueado));
});
