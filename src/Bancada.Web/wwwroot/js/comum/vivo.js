/* Atualização por polling: mais simples que SSE e sem conexão presa no servidor. Nada de innerHTML. */

/* Troca o trecho pela versão nova da própria página enquanto houver exame processando. Com um formulário em foco, espera. */
export function ligarVivo(doc, janela, el) {
  const intervalo = Number(el.dataset.vivo);
  if (!intervalo) return null;
  const atualizar = async () => {
    if (doc.activeElement?.closest?.('form')) return;
    const resposta = await janela.fetch(janela.location.href, { credentials: 'same-origin' });
    if (!resposta.ok) return;
    const novo = new janela.DOMParser().parseFromString(await resposta.text(), 'text/html');
    const trecho = novo.getElementById(el.id);
    doc.getElementById(el.id).replaceWith(doc.importNode(trecho, true));
    if (!trecho.dataset.vivo) janela.clearInterval(relogio);
  };
  const relogio = janela.setInterval(atualizar, intervalo);
  return atualizar;
}

/* Saída do executor de testes: o texto entra por textContent, porque é saída não confiável. */
export function ligarExecutor(janela, el) {
  if (el.dataset.executorInicial !== 'Rodando') return null;
  const saida = el.querySelector('[data-executor-saida]');
  const atualizar = async () => {
    const resposta = await janela.fetch(el.dataset.executor, { credentials: 'same-origin' });
    if (!resposta.ok) return;
    const r = await resposta.json();
    saida.textContent = r.saida;
    if (r.estado !== 'Rodando') {
      janela.clearInterval(relogio);
      janela.location.reload();
    }
  };
  const relogio = janela.setInterval(atualizar, 1500);
  return atualizar;
}
