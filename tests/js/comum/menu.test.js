import { test } from 'node:test';
import assert from 'node:assert/strict';
import { manterMenu } from '../../../src/Bancada.Web/wwwroot/js/comum/menu.js';

function janelaCom(salvo) {
  const ouvintes = {};
  const guardado = { 'bancada.menu': salvo };
  return {
    guardado, ouvintes,
    sessionStorage: { getItem: k => guardado[k] ?? null, setItem: (k, v) => { guardado[k] = v; } },
    addEventListener: (tipo, fn) => { ouvintes[tipo] = fn; },
  };
}

function menuCom(atual) {
  return { scrollLeft: 0, clientWidth: 400, rolagens: [], querySelector: () => atual, scrollTo(o) { this.rolagens.push(o); } };
}

test('reabre a faixa onde estava e desliza até a tela atual ficar no meio', () => {
  const janela = janelaCom('250');
  const menu = menuCom({ offsetLeft: 700, offsetWidth: 100 });
  manterMenu(janela, menu);
  assert.equal(menu.scrollLeft, 250);
  assert.deepEqual(menu.rolagens, [{ left: 550, behavior: 'smooth' }]);
  menu.scrollLeft = 550;
  janela.ouvintes.pagehide();
  assert.equal(janela.guardado['bancada.menu'], '550');
});

test('primeira visita e tela fora do menu: começa do início e não rola', () => {
  const menu = menuCom(null);
  manterMenu(janelaCom(undefined), menu);
  assert.equal(menu.scrollLeft, 0);
  assert.deepEqual(menu.rolagens, []);
});

test('armazenamento bloqueado não quebra a página', () => {
  const ouvintes = {};
  const janela = { get sessionStorage() { throw new Error('SecurityError'); }, addEventListener: (t, fn) => { ouvintes[t] = fn; } };
  const menu = menuCom({ offsetLeft: 0, offsetWidth: 100 });
  assert.doesNotThrow(() => manterMenu(janela, menu));
  assert.doesNotThrow(() => ouvintes.pagehide());
});
