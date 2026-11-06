/* Modal nativo (<dialog>): abre já ao carregar, fecha no botão, no fundo ou no Esc, e a página volta ao endereço sem o item aberto. */
export function ligarModal(janela, dialogo) {
  dialogo.addEventListener('close', () => janela.location.assign(dialogo.dataset.volta));
  dialogo.addEventListener('click', evento => {
    if (evento.target === dialogo) dialogo.close();
  });
  for (const botao of dialogo.querySelectorAll('[data-fechar]')) {
    botao.addEventListener('click', () => dialogo.close());
  }
  dialogo.showModal();
}
