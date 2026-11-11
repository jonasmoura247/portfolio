/* No celular o menu é uma faixa que rola de lado. Cada tela é uma página nova e a faixa voltaria para o começo:
   ela reabre onde estava e desliza até deixar a tela atual no meio. No desktop o menu não rola de lado e nada muda. */
const CHAVE = 'bancada.menu';

export function manterMenu(janela, menu) {
  try {
    menu.scrollLeft = Number(janela.sessionStorage.getItem(CHAVE)) || 0;
  } catch {
    /* Armazenamento bloqueado: a faixa só não lembra onde estava. */
  }
  const atual = menu.querySelector('[aria-current="page"]');
  if (atual) menu.scrollTo({ left: atual.offsetLeft - (menu.clientWidth - atual.offsetWidth) / 2, behavior: 'smooth' });
  janela.addEventListener('pagehide', () => {
    try {
      janela.sessionStorage.setItem(CHAVE, String(menu.scrollLeft));
    } catch {
      /* idem */
    }
  });
}
