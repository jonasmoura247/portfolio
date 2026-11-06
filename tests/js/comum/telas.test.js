import { test } from 'node:test';
import assert from 'node:assert/strict';
import { agruparTubos, ligarCarrinho } from '../../../src/Bancada.Web/wwwroot/js/comum/carrinho.js';
import { ligarExecutor, ligarVivo } from '../../../src/Bancada.Web/wwwroot/js/comum/vivo.js';
import { iniciar } from '../../../src/Bancada.Web/wwwroot/js/comum/iniciar.js';
import { docFalso, elementoFalso } from '../_apoio/dom-falso.js';

function alvo(extra = {}) {
  const ouvintes = {};
  return { ouvintes, addEventListener(tipo, fn) { ouvintes[tipo] = fn; }, ...extra };
}

test('RN-021 no navegador: mesmo material vira um tubo só, com o maior jejum', () => {
  const tubos = agruparTubos([
    { material: 'Soro', jejum: 8, codigo: 'GLI' }, { material: 'Soro', jejum: 12, codigo: 'COL' }, { material: 'Soro', jejum: 0, codigo: 'URE' }, { material: 'EDTA', jejum: 0, codigo: 'HEM' },
  ]);
  assert.deepEqual(tubos, [{ material: 'Soro', jejum: 12, exames: ['GLI', 'COL', 'URE'] }, { material: 'EDTA', jejum: 0, exames: ['HEM'] }]);
});

test('carrinho desenha os tubos e volta para o texto de vazio', () => {
  const caixa = alvo({ checked: true, value: 'GLI', dataset: { material: 'Soro', jejum: '8' } });
  const destino = { dataset: { jejumRotulo: 'Jejum', vazio: 'Nada' }, itens: [], replaceChildren(...f) { this.itens = f; } };
  const form = { querySelectorAll: () => [caixa], querySelector: () => destino };
  ligarCarrinho(docFalso, form);
  caixa.ouvintes.change();
  assert.equal(destino.itens[0].atributos.class, 'tubo');
  caixa.checked = false;
  caixa.ouvintes.change();
  assert.equal(destino.itens[0].filhos[0].textContent, 'Nada');
});

function janelaViva(resposta) {
  const janela = {
    location: { href: '/equipamentos', reload() { janela.recarregou = true; } },
    limpou: 0,
    setInterval: fn => { janela.tique = fn; return 7; },
    clearInterval: () => { janela.limpou++; },
    fetch: async () => resposta,
  };
  janela.DOMParser = class { parseFromString() { return { getElementById: () => janela.trecho }; } };
  return janela;
}

test('vivo sem intervalo não liga nada', () => {
  assert.equal(ligarVivo({}, janelaViva(), { dataset: {} }), null);
});

test('vivo troca o trecho e para quando não há mais nada processando', async () => {
  const janela = janelaViva({ ok: true, text: async () => '<div></div>' });
  const trocas = [];
  const doc = { activeElement: null, importNode: n => n, getElementById: () => ({ replaceWith: n => trocas.push(n) }) };
  const el = { id: 'vivo', dataset: { vivo: '1500' } };
  janela.trecho = { dataset: { vivo: '1500' } };
  const atualizar = ligarVivo(doc, janela, el);
  await atualizar();
  assert.equal(trocas.length, 1);
  assert.equal(janela.limpou, 0);
  janela.trecho = { dataset: {} };
  await atualizar();
  assert.equal(janela.limpou, 1);
});

test('vivo espera quem está digitando num formulário e ignora resposta ruim', async () => {
  let pediu = 0;
  const janela = janelaViva({ ok: false });
  janela.fetch = async () => { pediu++; return { ok: false }; };
  const doc = { activeElement: { closest: () => ({}) } };
  const atualizar = ligarVivo(doc, janela, { id: 'v', dataset: { vivo: '1' } });
  await atualizar();
  assert.equal(pediu, 0);
  doc.activeElement = { closest: () => null };
  await atualizar();
  assert.equal(pediu, 1);
});

function executor(respostas) {
  const janela = janelaViva();
  janela.fetch = async () => respostas.shift();
  const saida = { textContent: '' };
  const el = { dataset: { executor: '/painel/testes/estado', executorInicial: 'Rodando' }, querySelector: () => saida };
  return { janela, saida, atualizar: ligarExecutor(janela, el) };
}

test('executor mostra a saída como texto e recarrega ao terminar', async () => {
  const json = r => ({ ok: true, json: async () => r });
  const { janela, saida, atualizar } = executor([json({ estado: 'Rodando', saida: '<b>1</b>' }), json({ estado: 'Concluido', saida: 'fim' })]);
  await atualizar();
  assert.equal(saida.textContent, '<b>1</b>');
  assert.equal(janela.recarregou, undefined);
  await atualizar();
  assert.equal(janela.limpou, 1);
  assert.equal(janela.recarregou, true);
});

test('execução rápida, que acaba antes da primeira consulta, também recarrega', async () => {
  const { janela, atualizar } = executor([{ ok: true, json: async () => ({ estado: 'Falhou', saida: '' }) }]);
  await atualizar();
  assert.equal(janela.recarregou, true);
});

test('executor parado não consulta nada e resposta ruim é ignorada', async () => {
  assert.equal(ligarExecutor(janelaViva(), { dataset: { executorInicial: 'Pronto' } }), null);
  const { saida, atualizar } = executor([{ ok: false }]);
  saida.textContent = 'x';
  await atualizar();
  assert.equal(saida.textContent, 'x');
});

test('iniciar liga imprimir, ir, contador, carrinho, vivo e executor', () => {
  const imprimir = alvo();
  const ir = alvo({ value: '14', form: { dataset: { irPara: '/prontuario/' } } });
  const irPedido = alvo({ value: 'PAC 1' });
  const campo = alvo({ value: 'abc', maxLength: 10, depois: null, after(n) { this.depois = n; } });
  const carrinho = { querySelectorAll: () => [], querySelector: () => ({ dataset: {} }) };
  const vivo = { dataset: {} };
  const exec = { dataset: {}, querySelector: () => ({ dataset: {} }) };
  const mapa = { '[data-imprimir]': [imprimir], '[data-ir]': [ir], '[data-ir-pedido]': [irPedido], '[data-contar]': [campo], '[data-carrinho]': [carrinho], '[data-vivo]': [vivo], '[data-executor]': [exec] };
  const doc = { documentElement: { dataset: {} }, createElement: elementoFalso, querySelectorAll: s => mapa[s] ?? [] };
  const idas = [];
  let impressoes = 0;
  const janela = { localStorage: { getItem: () => null }, print: () => { impressoes++; }, location: { assign: u => idas.push(u) }, setInterval: () => 1 };

  iniciar(doc, janela);
  imprimir.ouvintes.click();
  ir.ouvintes.change();
  irPedido.ouvintes.change();
  campo.value = 'abcd';
  campo.ouvintes.input();

  assert.equal(impressoes, 1);
  assert.deepEqual(idas, ['/prontuario/14', '/pedidos/novo?paciente=PAC%201']);
  assert.equal(campo.depois.textContent, '4 / 10');
});
