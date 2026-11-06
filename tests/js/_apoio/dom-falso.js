/* DOM de mentira, escrito à mão: só o que o código do Bancada usa. Cresce junto, e tudo nele é testado. */
export function elementoFalso(tag) {
  return {
    tag, atributos: {}, filhos: [], textContent: '',
    setAttribute(n, v) { this.atributos[n] = v; },
    getAttribute(n) { return this.atributos[n] ?? null; },
    appendChild(f) { this.filhos.push(f); return f; },
  };
}

export const docFalso = { createElement: elementoFalso };
