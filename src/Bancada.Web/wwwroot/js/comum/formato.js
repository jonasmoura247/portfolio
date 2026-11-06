/* Formatação pt-BR/en de números, sem dependência. */
export function formatarNumero(valor, casas = 1, locale = 'pt-BR') {
  return Number(valor).toLocaleString(locale, { minimumFractionDigits: casas, maximumFractionDigits: casas });
}

const FLAGS = { N: 'N', L: 'L ▼', H: 'H ▲', CL: 'CL ▼▼', CH: 'CH ▲▲' };

/** Texto da bandeira de resultado: nunca só cor (acessibilidade). */
export function rotuloDaFlag(flag) {
  if (!(flag in FLAGS)) throw new RangeError(`Flag desconhecida: ${flag}`);
  return FLAGS[flag];
}
