/* Script clássico no <head> (não module, não defer): aplica o tema salvo antes da primeira pintura.
   Sem isso a página nasce no tema do sistema e troca depois, e as transições deixam a troca visível.
   Fica exposto em globalThis só para o teste chamar com casos diferentes. */
globalThis.bancadaTemaCedo = (raiz, armazenamento) => {
  try {
    const tema = armazenamento().getItem('bancada.tema');
    if (tema) raiz.dataset.theme = tema;
  } catch {
    /* Armazenamento bloqueado: fica o tema do sistema. */
  }
};
globalThis.bancadaTemaCedo(document.documentElement, () => localStorage);
