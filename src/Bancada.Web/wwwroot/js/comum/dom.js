/* Criação de elementos sem innerHTML: texto sempre por textContent. O DOM entra por parâmetro (testável). */
const PROIBIDOS = /^(on|style$|srcdoc$)/i;

export function criar(doc, tag, atributos = {}, filhos = []) {
  const el = doc.createElement(tag);
  for (const [nome, valor] of Object.entries(atributos)) {
    if (PROIBIDOS.test(nome)) throw new Error(`Atributo proibido pela CSP: ${nome}`);
    el.setAttribute(nome, String(valor));
  }
  for (const filho of [].concat(filhos)) {
    if (typeof filho === 'string') {
      const t = doc.createElement('span');
      t.textContent = filho;
      el.appendChild(t);
    } else {
      el.appendChild(filho);
    }
  }
  return el;
}
