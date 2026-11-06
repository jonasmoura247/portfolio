import { test } from 'node:test';
import assert from 'node:assert/strict';
import { formatarNumero, rotuloDaFlag } from '../../../src/Bancada.Web/wwwroot/js/comum/formato.js';

test('formatarNumero usa vírgula decimal em pt-BR e ponto em en', () => {
  assert.equal(formatarNumero(7.1), '7,1');
  assert.equal(formatarNumero(1284, 0), '1.284');
  assert.equal(formatarNumero(7.1, 1, 'en'), '7.1');
});

test('formatarNumero aceita número em texto e arredonda pelas casas', () => {
  assert.equal(formatarNumero('4.25', 1), '4,3');
  assert.equal(formatarNumero(2, 2), '2,00');
});

test('rotuloDaFlag devolve letra e seta para cada flag', () => {
  assert.deepEqual(['N', 'L', 'H', 'CL', 'CH'].map(rotuloDaFlag), ['N', 'L ▼', 'H ▲', 'CL ▼▼', 'CH ▲▲']);
});

test('rotuloDaFlag recusa flag desconhecida', () => {
  assert.throws(() => rotuloDaFlag('X'), RangeError);
});
