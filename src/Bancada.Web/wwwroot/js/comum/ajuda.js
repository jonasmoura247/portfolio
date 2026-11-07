/* Botão de ajuda: abre o modal da tela e toca o vídeo de exemplo (sem áudio). Ao fechar, o vídeo pausa. */
export function ligarAjuda(botao, dialogo) {
  const video = dialogo.querySelector('video');
  botao.addEventListener('click', () => {
    dialogo.showModal();
    if (video) Promise.resolve(video.play()).catch(() => { /* autoplay barrado: o visitante usa o play */ });
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
