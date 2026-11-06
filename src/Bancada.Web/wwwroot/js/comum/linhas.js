/* Linha (ou célula) inteira clicável: vai para data-href, a menos que o clique tenha sido num controle de dentro dela. */
const CONTROLES = 'a, button, input, select, textarea, label, form';

export function ligarLinhaClicavel(janela, el) {
  el.addEventListener('click', evento => {
    if (evento.target.closest(CONTROLES)) return;
    janela.location.assign(el.dataset.href);
  });
}
