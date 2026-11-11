/* Alterna claro/escuro. Sem preferência escolhida, parte do que o sistema operacional pede. */
const CHAVE = 'bancada.tema';

export function alternarTema(raiz, sistemaPrefereEscuro, janela) {
  const escuro = raiz.dataset.theme ? raiz.dataset.theme === 'dark' : sistemaPrefereEscuro;
  raiz.dataset.theme = escuro ? 'light' : 'dark';
  try {
    janela.localStorage.setItem(CHAVE, raiz.dataset.theme);
  } catch {
    /* Armazenamento bloqueado: o tema só não sobrevive à próxima página. */
  }
  return raiz.dataset.theme;
}

/* Com View Transitions, a tela antiga esmaece na nova; navegador sem suporte troca direto. */
export function comTransicao(doc, acao) {
  if (doc.startViewTransition) doc.startViewTransition(acao);
  else acao();
}

export function restaurarTema(raiz, janela) {
  try {
    const salvo = janela.localStorage.getItem(CHAVE);
    if (salvo) raiz.dataset.theme = salvo;
  } catch {
    /* Armazenamento bloqueado: fica o tema do sistema. */
  }
}
