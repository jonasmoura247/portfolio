/* Botão de ajuda: abre o modal da tela com o vídeo de exemplo (sem áudio) parado; o visitante dá o play quando quiser.
   Ao abrir, o vídeo carrega só o começo, para mostrar a primeira imagem. Ao fechar, ele pausa. */
export function ligarAjuda(botao, dialogo) {
  const video = dialogo.querySelector('video');
  botao.addEventListener('click', () => {
    if (video) video.preload = 'metadata';
    dialogo.showModal();
  });
  dialogo.addEventListener('close', () => {
    if (video) video.pause();
  });
  dialogo.addEventListener('click', evento => {
    if (evento.target === dialogo) dialogo.close();
  });
  for (const fechar of dialogo.querySelectorAll('[data-fechar]')) {
    fechar.addEventListener('click', () => dialogo.close());
  }
}
