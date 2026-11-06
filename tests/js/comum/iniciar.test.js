import { test } from 'node:test';
import assert from 'node:assert/strict';
import { iniciar } from '../../../src/Bancada.Web/wwwroot/js/comum/iniciar.js';

function alvo(extra = {}) {
  const ouvintes = {};
  return { ouvintes, addEventListener(tipo, fn) { ouvintes[tipo] = fn; }, ...extra };
}

function docCom({ selects = [], botoes = [] } = {}) {
  return {
    documentElement: { dataset: {} },
    querySelectorAll: seletor => (seletor === '[data-auto-enviar]' ? selects : seletor === '[data-alternar-tema]' ? botoes : []),
  };
}

test('select com data-auto-enviar envia o formulário ao mudar', () => {
  let enviou = 0;
  const select = alvo({ form: { requestSubmit: () => { enviou++; } } });
  iniciar(docCom({ selects: [select] }), { matchMedia: () => ({ matches: false }) });
  select.ouvintes.change();
  assert.equal(enviou, 1);
});

test('botão de tema alterna o tema usando a preferência do sistema', () => {
  const botao = alvo();
  const doc = docCom({ botoes: [botao] });
  iniciar(doc, { matchMedia: consulta => ({ matches: consulta.includes('dark') }) });
  botao.ouvintes.click();
  assert.equal(doc.documentElement.dataset.theme, 'light');
});

test('página sem os atributos não quebra', () => {
  assert.doesNotThrow(() => iniciar(docCom(), { matchMedia: () => ({ matches: false }) }));
});
