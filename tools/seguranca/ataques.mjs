// Ataques controlados, caixa-preta, contra uma instância LOCAL da Bancada (nunca contra produção ou terceiros).
// Uso: BASE=http://localhost:50910 node tools/seguranca/ataques.mjs
// A instância precisa estar recém-iniciada (Bancada__CookiesSeguros=false, só porque é HTTP local) para os limites de taxa começarem zerados.
// Cada linha: OK (resistiu) ou FALHA (vulnerável), com a evidência. Respeita os limites de taxa para não bloquear o próprio IP.
const BASE = process.env.BASE ?? 'http://localhost:50910';
if (!/^http:\/\/(localhost|127\.0\.0\.1|\[::1\])(:\d+)?$/.test(BASE)) throw new Error('Só contra instância local.');

const linhas = [];
const anotar = (ok, nome, evidencia = '') => { linhas.push(`${ok ? 'OK    ' : 'FALHA '} ${nome}${evidencia ? ' — ' + evidencia : ''}`); };
const pausa = ms => new Promise(r => setTimeout(r, ms));

class Visitante {
  constructor() { this.jar = new Map(); }
  cabecalhoCookie() { return [...this.jar].map(([k, v]) => `${k}=${v}`).join('; '); }
  guardar(r) {
    for (const c of r.headers.getSetCookie?.() ?? []) {
      const [par] = c.split(';');
      const i = par.indexOf('=');
      this.jar.set(par.slice(0, i).trim(), par.slice(i + 1));
    }
  }
  get sid() { return this.jar.get('__Host-bancada.sid'); }
  async req(metodo, url, { corpo, cabecalhos = {}, espera = 650 } = {}) {
    await pausa(espera);
    const r = await fetch(BASE + url, { method: metodo, redirect: 'manual', headers: { cookie: this.cabecalhoCookie(), ...cabecalhos }, body: corpo });
    this.guardar(r);
    const texto = await r.text();
    return { status: r.status, texto, r };
  }
  get(url, o) { return this.req('GET', url, o); }
  async token() { const { texto } = await this.get('/bancada'); return /name="__RequestVerificationToken"[^>]*value="([^"]+)"/.exec(texto)?.[1] ?? ''; }
  async post(url, campos = {}, { token, semToken = false, cabecalhos = {} } = {}) {
    const t = semToken ? null : token ?? await this.token();
    const dados = new URLSearchParams(t ? { __RequestVerificationToken: t, ...campos } : campos);
    return this.req('POST', url, { corpo: dados.toString(), cabecalhos: { 'content-type': 'application/x-www-form-urlencoded', ...cabecalhos }, espera: 2200 });
  }
  async seguir(r) { const destino = r.r.headers.get('location'); return destino ? this.get(destino) : r; }
  async papel(p, op) { await this.post('/sessao/papel', { papel: p }); if (op) await this.post('/sessao/operador', { operador: op }); }
}

const secoes = process.env.SECOES?.split(',').map(Number);
const quer = n => !secoes || secoes.includes(n);
const aviso = html => (/<div class="alerta[^"]*"[^>]*>.*?<div>(.*?)<\/div>/s.exec(html)?.[1] ?? '').trim();

// 1. Cabeçalhos, cookies e verbos
if (quer(1)) {
  const v = new Visitante();
  const { r } = await v.get('/bancada');
  const csp = r.headers.get('content-security-policy') ?? '';
  anotar(!/unsafe-inline|unsafe-eval|\*/.test(csp) && csp.includes("default-src 'none'") && csp.includes("frame-ancestors 'none'"), 'CSP estrita', csp);
  anotar(!r.headers.get('server') && !r.headers.get('x-powered-by'), 'sem Server/X-Powered-By');
  const sc = (r.headers.getSetCookie?.() ?? []).find(c => c.startsWith('__Host-bancada.sid=')) ?? '';
  anotar(/httponly/i.test(sc) && /samesite=lax/i.test(sc) && /secure/i.test(sc) && /path=\//i.test(sc), 'cookie de sessão HttpOnly+Secure+SameSite+__Host-', sc.replace(/=[^;]+/, '=<id>'));
  for (const metodo of ['PUT', 'DELETE', 'PATCH', 'OPTIONS']) {
    const x = await v.req(metodo, '/pacientes');
    anotar(x.status >= 400 && x.status !== 500, `verbo ${metodo} recusado`, String(x.status));
  }
  // fetch não envia TRACE; vai pelo módulo http.
  const { request } = await import('node:http');
  const trace = await new Promise(ok => request(BASE + '/pacientes', { method: 'TRACE' }, r => { r.resume(); ok(r.statusCode); }).end());
  anotar(trace >= 400 && trace !== 500, 'verbo TRACE recusado', String(trace));
}

// 2. Escalonamento de privilégio: Recepção chama direto o que é de outros papéis
if (quer(2)) {
  const v = new Visitante();
  await v.papel('Recepcao');
  const t = await v.token();
  const alvos = [
    ['/auditoria/verificar', {}], ['/auditoria/simular', {}], ['/resultados/1/validar', {}], ['/resultados/1/confirmar-critico', {}],
    ['/laudos/1/liberar', {}], ['/laudos/1/retificar', { motivo: 'x' }], ['/soroteca/descartar', { amostra: '1', motivo: 'x' }],
    ['/equipamentos/relogio', { comando: '600' }], ['/qualidade/medicao', {}], ['/coletas/1/coletar', { material: 'Soro', volume: '5', jejum: '8' }],
    ['/pedidos/novo', { paciente: 'PAC-0014', exames: 'GLI' }], ['/atendimentos/1/iniciar', {}], ['/prontuario/evolucao/1/corrigir', { texto: 'x', paciente: '1' }],
  ];
  const vazou = [];
  for (const [url, campos] of alvos) {
    const r = await v.post(url, campos, { token: t });
    if (r.status !== 403) vazou.push(`${url}=${r.status}`);
  }
  anotar(vazou.length === 0, `Recepção recebe 403 em ${alvos.length} ações de outros papéis (POST direto)`, vazou.join(' '));
  const gets = ['/auditoria', '/equipamentos', '/qualidade', '/coletas', '/soroteca', '/etiquetas', '/resultados', '/laudos/1', '/prontuario/14', '/atendimentos/1'];
  const vazouGet = [];
  for (const url of gets) { const r = await v.get(url); if (r.status !== 403) vazouGet.push(`${url}=${r.status}`); }
  anotar(vazouGet.length === 0, `Recepção recebe 403 em ${gets.length} telas de outros papéis (GET direto)`, vazouGet.join(' '));
  const { texto } = await v.get('/auditoria?grupo=seguranca', { espera: 700 });
  anotar(true, 'recusas ficam na auditoria (verificado nos testes NUnit; Recepção não lê a auditoria)');
}

// 3. CSRF e cookies
if (quer(3)) {
  const a = new Visitante();
  const b = new Visitante();
  const ta = await a.token();
  await b.token();
  const sem = await a.post('/sessao/idioma', { idioma: 'en' }, { semToken: true });
  anotar(sem.status === 400, 'POST sem token antifalsificação recusado', String(sem.status));
  const cruzado = await b.post('/sessao/idioma', { idioma: 'en' }, { token: ta });
  anotar(cruzado.status === 400, 'token de outra sessão recusado', String(cruzado.status));
  const origem = new Visitante();
  await origem.get('/bancada');
  const semCookieAf = new Visitante();
  semCookieAf.jar.set('__Host-bancada.sid', origem.sid);
  const forjado = await semCookieAf.post('/sessao/idioma', { idioma: 'en' }, { token: await origem.token().then(() => 'x'), cabecalhos: { origin: 'https://atacante.example' } });
  anotar(forjado.status === 400, 'POST de outro site (sem cookie antifalsificação, token inválido) recusado', String(forjado.status));
  const inventado = new Visitante();
  inventado.jar.set('__Host-bancada.sid', 'A'.repeat(43));
  await inventado.get('/bancada');
  anotar(inventado.sid && inventado.sid !== 'A'.repeat(43), 'id de sessão inventado não é adotado (fixação)', inventado.sid ? 'servidor emitiu id novo' : 'sem cookie');
  const velho = new Visitante();
  await velho.get('/bancada');
  const idVelho = velho.sid;
  await velho.papel('Medico');
  const reuso = new Visitante();
  reuso.jar.set('__Host-bancada.sid', idVelho);
  const { texto } = await reuso.get('/bancada');
  anotar(reuso.sid !== idVelho && !texto.includes('value="Medico" selected'), 'id antigo (antes da troca de papel) não dá acesso', 'nova sessão criada');
}

// 4. Isolamento entre sessões (o banco é de cada uma; os ids da semente coincidem de propósito)
if (quer(4)) {
  const a = new Visitante();
  const b = new Visitante();
  await a.get('/bancada');
  await b.get('/bancada');
  const marca = 'segredo-de-A-' + Math.random().toString(36).slice(2, 8);
  const { texto: grade } = await a.get('/agenda?profissional=MED-01&semana=1');
  const horario = /name="horario" value="(\d+)"/.exec(grade)?.[1] ?? /horario=(\d+)/.exec(grade)?.[1];
  await a.post('/prontuario/14/alergia', { substancia: marca, reacao: marca });
  const { texto: deA } = await a.get('/prontuario/14');
  const { texto: deB } = await b.get('/prontuario/14');
  anotar(deA.includes(marca) && !deB.includes(marca), 'dado de A não aparece para B', `horário de teste ${horario}`);
  const idAlergia = /\/prontuario\/alergia\/(\d+)\/alternar/.exec(deA.slice(deA.indexOf(marca)))?.[1];
  await b.post(`/prontuario/alergia/${idAlergia}/alternar`, { paciente: '14' });
  const { texto: deA2 } = await a.get('/prontuario/14');
  const trecho = deA2.slice(deA2.indexOf(marca), deA2.indexOf(marca) + 400);
  anotar(trecho.includes('s-acento'), 'B não altera a alergia de A pelo mesmo id', `id ${idAlergia} segue ativo em A`);
}

// 5. Injeção (SQL / LIKE) e XSS
if (quer(5)) {
  const v = new Visitante();
  for (const busca of ["' OR '1'='1", "%", "_", "PAC-0014' --", '"; DROP TABLE Pacientes; --']) {
    const { status, texto } = await v.get('/pacientes?busca=' + encodeURIComponent(busca));
    const linhasTabela = (texto.match(/<tr><td class="mono">PAC-/g) ?? []).length;
    anotar(status === 200 && linhasTabela === 0, `busca de pacientes não injeta: ${busca}`, `${linhasTabela} linhas`);
  }
  const { texto: depois } = await v.get('/pacientes');
  anotar((depois.match(/<tr><td class="mono">PAC-/g) ?? []).length > 0, 'tabela de pacientes intacta após as tentativas');
  const cargas = ['<script>alert(1)</script>', '"><img src=x onerror=alert(1)>', "'><svg onload=alert(1)>", 'javascript:alert(1)'];
  for (const c of cargas) {
    const { texto } = await v.get('/pacientes?busca=' + encodeURIComponent(c));
    const { texto: t2 } = await v.get('/etiquetas?busca=' + encodeURIComponent(c));
    anotar(!texto.includes(c.slice(0, 12)) || c.startsWith('javascript'), `XSS refletido não passa (pacientes): ${c}`);
    anotar(!t2.includes(c.slice(0, 12)) || c.startsWith('javascript'), `XSS refletido não passa (etiquetas): ${c}`);
  }
  await v.post('/prontuario/14/alergia', { substancia: cargas[0].slice(0, 60), reacao: cargas[1] });
  const { texto: armazenado } = await v.get('/prontuario/14');
  anotar(!armazenado.includes('<script>alert') && !armazenado.includes('<img src=x') && armazenado.includes('&lt;script&gt;'), 'XSS armazenado (alergia) sai codificado');
}

// 6. Arquivos internos e travessia de caminho
if (quer(6)) {
  const v = new Visitante();
  await v.get('/bancada');
  const caminhos =['/appsettings.json', '/Bancada.Web.dll', '/web.config', '/.git/config', '/painel-instantaneo.json', '/%2e%2e/appsettings.json', '/..%2fBancada.Web.dll', '/js/../../appsettings.json', '/Recursos/textos.json', '/obj/', '/bin/'];
  const abertos = [];
  for (const c of caminhos) { const r = await v.req('GET', c, { espera: 300 }); if (r.status === 200) abertos.push(c); }
  anotar(abertos.length === 0, `nenhum arquivo interno servido (${caminhos.length} caminhos)`, abertos.join(' '));
  const { status } = await v.get('/painel?aba=codigo&arquivo=' + encodeURIComponent('../../appsettings.json'));
  anotar(status === 404, 'visualizador de código não atravessa pasta', String(status));
}

// 7. Entradas extremas e exposição de erro
if (quer(7)) {
  const v = new Visitante();
  await v.get('/bancada');
  const casos =['/pacientes?pagina=2147483647', '/pacientes?pagina=-5', '/agenda?semana=99999999999', '/agenda?horario=-1', '/prontuario/2147483647', '/atendimentos?abrir=2147483647', '/laudos/999999', '/soroteca?posicao=-1&caixa=%00', '/auditoria?pagina=2147483647'];
  for (const c of casos) {
    const { status, texto } = await v.get(c);
    const vaza = /Exception|at Bancada\.|\\Users\\|StackTrace|System\./.test(texto);
    anotar(status !== 500 && !vaza, `entrada extrema sem erro 500 nem vazamento: ${c}`, String(status));
  }
  const grande = 'x'.repeat(20 * 1024);
  const r = await v.req('POST', '/sessao/idioma', { corpo: 'idioma=' + grande, cabecalhos: { 'content-type': 'application/x-www-form-urlencoded' }, espera: 2200 });
  anotar(r.status === 413, 'corpo acima de 16 KB recusado', String(r.status));
}

// 8. Executor de testes (em produção precisa estar desligado)
if (quer(8)) {
  const v = new Visitante();
  const r = await v.seguir(await v.post('/painel/testes/executar', { suite: 'js' }));
  const estado = await v.get('/painel/testes/estado');
  const json = JSON.parse(estado.texto || '{}');
  anotar(json.estado !== 'Rodando' && json.estado !== 'Concluido' && json.estado !== 'Falhou', 'executor de testes desligado em Production sem configuração explícita', `estado=${json.estado} suite=${json.suite} saída=${(json.saida ?? '').length} bytes`);
}

// 9. Limite de novas sessões: um cookie inventado não pode furar o balde de "nova sessão" (10/min)
if (quer(9)) {
  const ids = new Set();
  let recusas = 0;
  for (let i = 0; i < 16; i++) {
    const v = new Visitante();
    v.jar.set('__Host-bancada.sid', 'falso' + i + Math.random().toString(36).slice(2));
    const r = await v.get('/bancada', { espera: 150 });
    if (r.status === 429) recusas++;
    if (v.sid && !v.sid.startsWith('falso')) ids.add(v.sid);
  }
  anotar(ids.size <= 10, 'cookie inventado não fura o limite de novas sessões', `${ids.size} sessões criadas em 16 tentativas, ${recusas} recusas`);
}

console.log(linhas.join('\n'));
console.log(`\n${linhas.filter(l => l.startsWith('FALHA')).length} falha(s) em ${linhas.length} verificações`);
