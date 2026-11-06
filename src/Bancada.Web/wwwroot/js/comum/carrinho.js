/* Prévia dos tubos do pedido: exames do mesmo material vão no mesmo tubo, com o maior jejum (RN-021). A regra de verdade é a do servidor. */
import { criar } from './dom.js';

export function agruparTubos(marcados) {
  const tubos = new Map();
  for (const { material, jejum, codigo } of marcados) {
    const t = tubos.get(material) ?? { material, jejum: 0, exames: [] };
    t.jejum = Math.max(t.jejum, jejum);
    t.exames.push(codigo);
    tubos.set(material, t);
  }
  return [...tubos.values()];
}

export function ligarCarrinho(doc, form) {
  const caixas = [...form.querySelectorAll('input[name="exames"]')];
  const destino = form.querySelector('[data-tubos]');
  const desenhar = () => {
    const marcados = caixas.filter(c => c.checked).map(c => ({ material: c.dataset.material, jejum: Number(c.dataset.jejum), codigo: c.value }));
    const tubos = agruparTubos(marcados);
    destino.replaceChildren(...(tubos.length
      ? tubos.map(t => criar(doc, 'div', { class: 'tubo' }, [t.material, t.exames.join(' + '), `${destino.dataset.jejumRotulo}: ${t.jejum} h`]))
      : [criar(doc, 'p', { class: 'mut' }, [destino.dataset.vazio])]));
  };
  caixas.forEach(c => c.addEventListener('change', desenhar));
}
