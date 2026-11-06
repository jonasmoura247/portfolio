import { test } from 'node:test';
import assert from 'node:assert/strict';
import { criar } from '../../../src/Bancada.Web/wwwroot/js/comum/dom.js';
import { docFalso, elementoFalso } from '../_apoio/dom-falso.js';

test('criar monta o elemento com atributos e filhos', () => {
  const filho = elementoFalso('b');
  const el = criar(docFalso, 'div', { class: 'x', 'data-n': 3 }, ['texto', filho]);
  assert.equal(el.tag, 'div');
  assert.deepEqual(el.atributos, { class: 'x', 'data-n': '3' });
  assert.equal(el.getAttribute('class'), 'x');
  assert.equal(el.getAttribute('nada'), null);
  assert.equal(el.filhos.length, 2);
  assert.equal(el.filhos[0].textContent, 'texto');
  assert.equal(el.filhos[1], filho);
});

test('criar aceita filho único e usa textContent, nunca HTML', () => {
  const el = criar(docFalso, 'p', {}, '<script>alert(1)</script>');
  assert.equal(el.filhos[0].textContent, '<script>alert(1)</script>');
  assert.equal(el.innerHTML, undefined);
});

test('criar sem atributos nem filhos devolve elemento vazio', () => {
  const el = criar(docFalso, 'hr');
  assert.deepEqual(el.atributos, {});
  assert.equal(el.filhos.length, 0);
});

test('criar recusa atributos que a CSP proíbe', () => {
  for (const nome of ['onclick', 'onerror', 'style', 'srcdoc']) {
    assert.throws(() => criar(docFalso, 'div', { [nome]: 'x' }), /proibido/);
  }
});

test('o DOM falso guarda e devolve o que recebe', () => {
  const el = elementoFalso('i');
  el.setAttribute('a', 'b');
  assert.equal(el.getAttribute('a'), 'b');
  assert.equal(el.appendChild(elementoFalso('u')).tag, 'u');
});
