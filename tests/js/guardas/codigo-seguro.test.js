import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative, resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

// app.js liga a página ao carregar; aqui o navegador é de mentira.
globalThis.document = { documentElement: { dataset: {} }, querySelectorAll: () => [] };
globalThis.window = { matchMedia: () => ({ matches: false }) };

const RAIZ_JS = 'src/Bancada.Web/wwwroot/js';
const PROIBIDOS = ['innerHTML', 'outerHTML', 'insertAdjacentHTML', 'document.write', 'eval(', 'new Function'];

function listar(dir) {
  return readdirSync(dir).flatMap(n => { const p = join(dir, n); return statSync(p).isDirectory() ? listar(p) : p.endsWith('.js') ? [p] : []; });
}

test('nenhum arquivo do front usa API que injeta HTML ou executa texto', () => {
  const achados = [];
  for (const arq of listar(RAIZ_JS)) {
    const texto = readFileSync(arq, 'utf8').replace(/\/\*[\s\S]*?\*\//g, '').replace(/\/\/.*$/gm, '');
    for (const p of PROIBIDOS) if (texto.includes(p)) achados.push(`${relative('.', arq)}: ${p}`);
  }
  assert.deepEqual(achados, []);
});

test('todo arquivo do front é importado: nenhum fica fora da conta de cobertura', async () => {
  const arquivos = listar(RAIZ_JS);
  assert.ok(arquivos.length > 0);
  for (const arq of arquivos) await import(pathToFileURL(resolve(arq)).href);
});
