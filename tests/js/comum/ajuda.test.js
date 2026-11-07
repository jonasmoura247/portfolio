import { test } from 'node:test';
import assert from 'node:assert/strict';
import { ligarAjuda } from '../../../src/Bancada.Web/wwwroot/js/comum/ajuda.js';
import { iniciar } from '../../../src/Bancada.Web/wwwroot/js/comum/iniciar.js';

function alvo(extra = {}) {
  const ouvintes = {};
  return { ouvintes, addEventListener(tipo, fn) { ouvintes[tipo] = fn; }, ...extra };
}

function montar({ comVideo = true, play = () => Promise.resolve() } = {}) {
  const video = { tocou: 0, pausou: 0, play() { this.tocou++; return play(); }, pause() { this.pausou++; } };
  const fechar = alvo();
  const dialogo = alvo({ abriu: 0, fechou: 0, querySelector: () => (comVideo ? video : null), querySelectorAll: () => [fechar], showModal() { this.abriu++; }, close() { this.fechou++; } });
  const botao = alvo();
  ligarAjuda(botao, dialogo);
  return { botao, dialogo, video, fechar };
}

test('o botão abre o modal e toca o vídeo', () => {
  const { botao, dialogo, video } = montar();
  botao.ouvintes.click();
  assert.equal(dialogo.abriu, 1);
  assert.equal(video.tocou, 1);
});

test('autoplay barrado não quebra a abertura', async () => {
  const { botao, dialogo } = montar({ play: () => Promise.reject(new Error('barrado')) });
  botao.ouvintes.click();
  await new Promise(resolve => setImmediate(resolve));
  assert.equal(dialogo.abriu, 1);
});

test('ao fechar o vídeo pausa; fundo e botão X fecham', () => {
  const { dialogo, video, fechar } = montar();
  dialogo.ouvintes.close();
  assert.equal(video.pausou, 1);
  dialogo.ouvintes.click({ target: dialogo });
  dialogo.ouvintes.click({ target: {} });
  fechar.ouvintes.click();
  assert.equal(dialogo.fechou, 2);
});

test('tela sem vídeo abre e fecha normalmente', () => {
  const { botao, dialogo } = montar({ comVideo: false });
  botao.ouvintes.click();
  dialogo.ouvintes.close();
  assert.equal(dialogo.abriu, 1);
});

test('iniciar liga o botão de ajuda da página', () => {
  const botao = alvo();
  const dialogo = alvo({ querySelector: () => null, querySelectorAll: () => [], showModal() {}, close() {} });
  const doc = {
    documentElement: { dataset: {} },
    querySelector: seletor => (seletor === '[data-ajuda]' ? dialogo : null),
    querySelectorAll: seletor => (seletor === '[data-abrir-ajuda]' ? [botao] : []),
  };
  iniciar(doc, { matchMedia: () => ({ matches: false }) });
  assert.equal(typeof botao.ouvintes.click, 'function');
});
