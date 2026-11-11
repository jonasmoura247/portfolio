import { test } from 'node:test';
import assert from 'node:assert/strict';

// O arquivo é um script clássico: roda ao ser carregado, com o document e o localStorage do navegador.
globalThis.document = { documentElement: { dataset: {} } };
globalThis.localStorage = { getItem: () => 'light' };
await import('../../../src/Bancada.Web/wwwroot/js/tema-cedo.js');
delete globalThis.document;
delete globalThis.localStorage;

const aplicar = armazenamento => {
  const raiz = { dataset: {} };
  globalThis.bancadaTemaCedo(raiz, armazenamento);
  return raiz.dataset.theme;
};

test('ao carregar, aplica o tema salvo antes da primeira pintura', () => {
  assert.equal(aplicar(() => ({ getItem: () => 'dark' })), 'dark');
});

test('sem tema salvo, deixa o do sistema', () => {
  assert.equal(aplicar(() => ({ getItem: () => null })), undefined);
});

test('armazenamento bloqueado não quebra a página', () => {
  assert.equal(aplicar(() => { throw new Error('SecurityError'); }), undefined);
});
