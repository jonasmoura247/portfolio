/* Liga os comportamentos da página. O DOM e a janela entram por parâmetro, para testar sem navegador. */
import { alternarTema, restaurarTema } from './tema.js';
import { ligarCarrinho } from './carrinho.js';
import { ligarExecutor, ligarVivo } from './vivo.js';
import { ligarLinhaClicavel } from './linhas.js';
import { ligarModal } from './modal.js';

function ligarContador(doc, campo) {
  const contador = doc.createElement('span');
  contador.className = 'contador';
  const atualizar = () => { contador.textContent = `${campo.value.length} / ${campo.maxLength}`; };
  campo.addEventListener('input', atualizar);
  atualizar();
  campo.after(contador);
}

export function iniciar(doc, janela) {
  restaurarTema(doc.documentElement, janela);
  for (const el of doc.querySelectorAll('[data-auto-enviar]')) {
    el.addEventListener('change', () => el.form.requestSubmit());
  }
  for (const botao of doc.querySelectorAll('[data-alternar-tema]')) {
    botao.addEventListener('click', () => alternarTema(doc.documentElement, janela.matchMedia('(prefers-color-scheme: dark)').matches, janela));
  }
  for (const botao of doc.querySelectorAll('[data-imprimir]')) {
    botao.addEventListener('click', () => janela.print());
  }
  for (const seletor of doc.querySelectorAll('[data-ir]')) {
    seletor.addEventListener('change', () => janela.location.assign(seletor.form.dataset.irPara + seletor.value));
  }
  for (const seletor of doc.querySelectorAll('[data-ir-pedido]')) {
    seletor.addEventListener('change', () => janela.location.assign(`/pedidos/novo?paciente=${encodeURIComponent(seletor.value)}`));
  }
  for (const campo of doc.querySelectorAll('[data-contar]')) {
    ligarContador(doc, campo);
  }
  for (const form of doc.querySelectorAll('[data-carrinho]')) {
    ligarCarrinho(doc, form);
  }
  for (const el of doc.querySelectorAll('[data-vivo]')) {
    ligarVivo(doc, janela, el);
  }
  for (const el of doc.querySelectorAll('[data-executor]')) {
    ligarExecutor(janela, el);
  }
  for (const el of doc.querySelectorAll('[data-href]')) {
    ligarLinhaClicavel(janela, el);
  }
  for (const dialogo of doc.querySelectorAll('[data-modal]')) {
    ligarModal(janela, dialogo);
  }
}
