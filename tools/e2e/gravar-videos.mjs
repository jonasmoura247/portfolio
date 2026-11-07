// Grava os vídeos de exemplo da ajuda (sem áudio, modo escuro, com cursor e legenda na tela) e converte para mp4 em wwwroot/videos.
// Uso, com um servidor recém-iniciado em http://localhost:50900 (Bancada__CookiesSeguros=false):
//   PLAYWRIGHT_CORE=<pasta do playwright-core> node tools/e2e/gravar-videos.mjs [agenda] [etiquetas]
// Precisa do ffmpeg no PATH. As ferramentas ficam fora do projeto: nada disso vira dependência do Bancada.
import { createRequire } from 'node:module';
import { mkdirSync, renameSync, rmSync } from 'node:fs';
import { execFileSync } from 'node:child_process';

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_CORE ?? 'playwright-core');
const BASE = process.env.BASE ?? 'http://localhost:50900';
const SAIDA = 'src/Bancada.Web/wwwroot/videos';
const TEMP = 'tools/e2e/capturas/videos-tmp';
const alvos = process.argv.slice(2);

// Cursor verde e legenda desenhados pela página (por CSSOM: a CSP não aceita estilo inline).
const sobreposicao = () => {
  const montar = () => {
    if (document.getElementById('__cursor')) return;
    const cursor = document.createElement('div');
    cursor.id = '__cursor';
    Object.assign(cursor.style, { position: 'fixed', left: '-40px', top: '-40px', width: '26px', height: '26px', marginLeft: '-13px', marginTop: '-13px', borderRadius: '50%', background: 'rgba(32,214,118,.35)', border: '2px solid #20d676', zIndex: '2147483647', pointerEvents: 'none', transition: 'transform .12s' });
    const legenda = document.createElement('div');
    legenda.id = '__legenda';
    Object.assign(legenda.style, { position: 'fixed', left: '50%', bottom: '22px', transform: 'translateX(-50%)', maxWidth: '86%', padding: '10px 20px', borderRadius: '10px', background: 'rgba(14,18,25,.94)', border: '1px solid #20d676', color: '#e4e8f0', font: '600 20px system-ui, sans-serif', zIndex: '2147483646', pointerEvents: 'none', display: 'none', textAlign: 'center' });
    document.body.append(cursor, legenda);
    addEventListener('mousemove', e => { cursor.style.left = e.clientX + 'px'; cursor.style.top = e.clientY + 'px'; }, true);
    addEventListener('mousedown', () => { cursor.style.transform = 'scale(.65)'; }, true);
    addEventListener('mouseup', () => { cursor.style.transform = 'scale(1)'; }, true);
    window.__legenda = texto => { legenda.textContent = texto; legenda.style.display = texto ? 'block' : 'none'; };
  };
  if (document.body) montar(); else addEventListener('DOMContentLoaded', montar);
};

const VISTA = { viewport: { width: 1280, height: 720 }, colorScheme: 'dark' };

// Preparo: roda sem gravar, numa sessão nova, e entrega a sessão (cookies) ao contexto que grava.
// Espera a página nova pelo marcador (o documento antigo o tem, o novo não), sem depender de eventos de navegação.
async function selecionar(pg, seletor, valor) {
  await pg.waitForLoadState('load');
  await pg.evaluate(() => { window.__antiga = true; });
  await pg.selectOption(seletor, valor);
  await pg.waitForFunction(() => !window.__antiga && document.readyState === 'complete');
}
async function trocar(pg, papel, operador) {
  await selecionar(pg, 'select[name=papel]', papel);
  if (operador) await selecionar(pg, 'select[name=operador]', operador);
}
const ir0 = async (pg, url) => { await pg.waitForTimeout(600); await pg.goto(BASE + url); };

async function prepPedido(pg) {
  await ir0(pg, '/atendimentos');
  await pg.locator('form[action$="/checkin"] button').first().click();
  await pg.waitForSelector('.alerta.ok');
  await trocar(pg, 'Medico');
  await ir0(pg, '/atendimentos');
  const linha = await pg.locator('tr:has(form[action$="/iniciar"])').first().innerText();
  const medico = linha.match(/MED-0\d/)[0];
  const paciente = linha.match(/PAC-\d{4}/)[0];
  await selecionar(pg, 'select[name=operador]', medico);
  await ir0(pg, '/atendimentos');
  await pg.locator('form[action$="/iniciar"] button').first().click();
  const atendimento = pg.url();
  await pg.fill('textarea[name=texto]', 'Paciente estável, exames de rotina solicitados.');
  await pg.click('button:has-text("Registrar evolução")');
  await pg.click('a:has-text("Pedir exame")');
  await pg.check('input[name=exames][value=GLI]');
  await pg.check('input[name=exames][value=HEM]');
  await pg.click('button:has-text("Enviar pedido")');
  await pg.goto(atendimento);
  await pg.click('button:has-text("Concluir atendimento")');
  await pg.waitForSelector('.alerta.ok:has-text("concluído")');
  await pg.waitForLoadState('networkidle');
  return { paciente };
}
async function prepColeta(pg) {
  const info = await prepPedido(pg);
  await trocar(pg, 'Coleta');
  await ir0(pg, '/coletas');
  await pg.locator('tr:has-text("PED-000045") button:has-text("Coletar")').first().click();
  await pg.waitForSelector('.etiqueta svg');
  return info;
}
async function prepArmazenada(pg) {
  const info = await prepColeta(pg);
  await ir0(pg, '/soroteca?caixa=R2-C4');
  await pg.locator('a.pos:not(.oc)').first().click();
  await pg.click('button:has-text("Armazenar")');
  await pg.waitForSelector('.alerta.ok');
  return info;
}
async function prepProcessada(pg) {
  const info = await prepArmazenada(pg);
  await trocar(pg, 'Analista', 'ANA-01');
  await ir0(pg, '/equipamentos');
  await pg.locator('form[action$="/enviar"] button').first().click();
  await pg.click('button[name=comando][value="600"]');
  await pg.waitForTimeout(4000);
  return info;
}
const preparos = { prontuario: prepPedido, pedido: prepPedido, coleta: prepPedido, soroteca: prepColeta, equipamento: prepArmazenada, resultados: prepProcessada };

async function gravar(nome, roteiro) {
  const navegador = await chromium.launch(process.env.CHROMIUM ? { executablePath: process.env.CHROMIUM } : {});
  let info = {};
  let estado;
  if (preparos[nome]) {
    const previo = await navegador.newContext(VISTA);
    const pg0 = await previo.newPage();
    await pg0.goto(`${BASE}/bancada`);
    info = await preparos[nome](pg0);
    estado = await previo.storageState();
    await previo.close();
  }
  const contexto = await navegador.newContext({ ...VISTA, storageState: estado, recordVideo: { dir: TEMP, size: { width: 1280, height: 720 } } });
  const pagina = await contexto.newPage();
  await pagina.addInitScript(sobreposicao);
  const dizer = async (texto, ms = 2200) => { await pagina.evaluate(t => window.__legenda?.(t), texto); await pagina.waitForTimeout(ms); };
  const ir = async (seletor, opcoes = {}) => {
    const alvo = pagina.locator(seletor).first();
    await alvo.scrollIntoViewIfNeeded();
    const caixa = await alvo.boundingBox();
    await pagina.mouse.move(caixa.x + caixa.width / 2, caixa.y + caixa.height / 2, { steps: 28 });
    await pagina.waitForTimeout(opcoes.espera ?? 450);
    return alvo;
  };
  const papel = async (valor, operador) => {
    await ir('select[name=papel]');
    await selecionar(pagina, 'select[name=papel]', valor);
    if (operador) {
      await ir('select[name=operador]');
      await selecionar(pagina, 'select[name=operador]', operador);
    }
  };
  await roteiro({ pagina, dizer, ir, papel, info });
  await pagina.waitForTimeout(1500);
  const arquivo = await pagina.video().path();
  await contexto.close();
  await navegador.close();
  mkdirSync(SAIDA, { recursive: true });
  const webm = `${TEMP}/${nome}.webm`;
  renameSync(arquivo, webm);
  execFileSync('ffmpeg', ['-y', '-loglevel', 'error', '-i', webm, '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '27', '-preset', 'slow', '-movflags', '+faststart', '-an', `${SAIDA}/${nome}.mp4`]);
  console.log(`${nome}.mp4 gravado`);
}

const roteiros = {
  async agenda({ pagina, dizer, ir }) {
    await pagina.goto(`${BASE}/agenda?profissional=MED-01&semana=1`);
    await dizer('Agenda: a grade semanal do médico, de 30 em 30 minutos', 2800);
    await dizer('Para agendar, clique numa célula livre', 800);
    await (await ir('td.livre.clicavel')).click();
    await pagina.waitForSelector('dialog[open]');
    await dizer('Escolha o paciente e, se quiser, escreva o motivo', 1200);
    await (await ir('dialog select[name=paciente]')).selectOption('PAC-0002');
    await (await ir('dialog textarea[name=motivo]')).click();
    await pagina.keyboard.type('Retorno para conferir exames', { delay: 70 });
    await dizer('Clique em Agendar', 900);
    await (await ir('dialog button:has-text("Agendar")')).click();
    await pagina.waitForSelector('.alerta.ok');
    await dizer('Pronto: o horário agora está ocupado', 2600);
    await dizer('Clique num horário ocupado para ver os dados do paciente', 900);
    await (await ir('a.marc:has-text("PAC-0002")')).click();
    await pagina.waitForSelector('dialog[open]');
    await dizer('Aqui estão o estado, o horário, o médico e o motivo', 3200);
    await dizer('Confirme, cancele ou reagende por este modal', 900);
    await (await ir('dialog button:has-text("Confirmar")')).click();
    await pagina.waitForSelector('.alerta.ok');
    await dizer('Agendamento confirmado. Fim do exemplo', 2600);
  },
  async etiquetas({ pagina, dizer, ir }) {
    await pagina.goto(`${BASE}/etiquetas`);
    await dizer('Consultar etiqueta: todas as etiquetas de amostra já geradas', 2800);
    await dizer('Digite o código da etiqueta, ou o código ou nome do paciente', 800);
    await (await ir('input[name=busca]')).click();
    await pagina.keyboard.type('AMO-000012', { delay: 90 });
    await (await ir('button:has-text("Buscar")')).click();
    await pagina.waitForSelector('tr.clicavel');
    await dizer('Achou: clique em qualquer lugar da linha, ou em Ver etiqueta', 1000);
    await (await ir('tr.clicavel td:nth-child(3)')).click();
    await pagina.waitForSelector('.etiqueta svg');
    await dizer('A etiqueta, com o código de barras Code 128 gerado no servidor', 3200);
    await dizer('Imprimir abre a versão em papel; Voltar leva à consulta', 900);
    await ir('button:has-text("Imprimir")', { espera: 1800 });
    await (await ir('a:has-text("Voltar")')).click();
    await dizer('Fim do exemplo', 2200);
  },
  async inicio({ pagina, dizer, ir, papel }) {
    await pagina.goto(`${BASE}/bancada`);
    await dizer('Início: o ponto de partida da Bancada, um laboratório de mentira', 2800);
    await dizer('O campo verde no topo troca o papel de quem usa o sistema', 900);
    await papel('Recepcao');
    await dizer('Como Recepção, o menu mostra só o que essa pessoa pode fazer', 3200);
    await papel('Admin');
    await dizer('De volta ao Admin: o menu completo', 2200);
    await dizer('O roteiro de 5 minutos leva você pelas telas, uma a uma', 900);
    await ir('ul.passos li:nth-child(3) a', { espera: 2200 });
    await dizer('Cada passo marcado mostra por onde você já passou', 2600);
    await dizer('E os cenários prontos carregam situações para testar', 900);
    await ir('.cenario:nth-child(2) button', { espera: 2200 });
    await dizer('Fim do exemplo', 2200);
  },
  async pacientes({ pagina, dizer, ir }) {
    await pagina.goto(`${BASE}/pacientes`);
    await dizer('Pacientes: 60 pessoas fictícias, com busca por nome ou código', 2800);
    await dizer('Digite o código ou parte do nome', 800);
    await (await ir('input[name=busca]')).click();
    await pagina.keyboard.type('PAC-0014', { delay: 90 });
    await (await ir('button:has-text("Buscar")')).click();
    await pagina.waitForSelector('tbody tr');
    await dizer('Achou o paciente. Clique em Abrir para ver a ficha', 1200);
    await (await ir('tbody tr a.btn')).click();
    await pagina.waitForSelector('.tempo');
    await dizer('A ficha: dados, alergias, últimos horários e a linha do tempo', 3600);
    await pagina.mouse.wheel(0, 500);
    await dizer('A linha do tempo mostra tudo o que aconteceu com o paciente', 3200);
    await dizer('Fim do exemplo', 2000);
  },
  async atendimento({ pagina, dizer, ir }) {
    await pagina.goto(`${BASE}/atendimentos`);
    await dizer('Atendimento: a fila do dia, com o estado de cada paciente', 2800);
    await dizer('Clique em qualquer lugar da linha para ver o resumo', 900);
    await (await ir('tr.clicavel td:nth-child(3)')).click();
    await pagina.waitForSelector('dialog[open]');
    await dizer('O resumo: estado, horário, médico, exames pedidos e evoluções', 3600);
    await dizer('Feche e faça o check-in do paciente', 800);
    await (await ir('dialog [data-fechar]')).click();
    await pagina.waitForURL(u => !u.search.includes('abrir'));
    await pagina.waitForTimeout(600);
    await (await ir('form[action$="/checkin"] button')).click();
    await pagina.waitForSelector('.alerta.ok');
    await dizer('Check-in feito: agora o médico pode iniciar o atendimento', 3000);
    await dizer('Fim do exemplo', 2000);
  },
  async prontuario({ pagina, dizer, ir, info }) {
    const id = Number(info.paciente.slice(4));
    await pagina.goto(`${BASE}/prontuario/${id}`);
    await dizer('Prontuário: alergias, exames e evoluções do paciente', 2800);
    await dizer('Para registrar uma alergia, preencha a substância e a reação', 800);
    await (await ir('input[name=substancia]')).click();
    await pagina.keyboard.type('Dipirona', { delay: 80 });
    await (await ir('input[name=reacao]')).click();
    await pagina.keyboard.type('Urticária', { delay: 80 });
    await (await ir('form[action$="/alergia"] button')).click();
    await pagina.waitForSelector('.alerta.ok');
    await dizer('Alergia registrada: ela avisa em todo novo pedido', 2800);
    await pagina.mouse.wheel(0, 600);
    await dizer('As evoluções nunca se apagam: corrige-se com uma nova', 3000);
    const corrigir = pagina.locator('a[href*="?corrigir="]').first();
    if (await corrigir.count()) {
      await dizer('Clique em Corrigir e escreva a correção', 900);
      await (await ir('a[href*="?corrigir="]')).click();
      await (await ir('textarea[name=texto]')).click();
      await pagina.keyboard.type('Complemento: paciente refere melhora.', { delay: 60 });
      await (await ir('form[action*="/corrigir"] button')).click();
      await pagina.waitForSelector('.alerta.ok');
      await dizer('A original continua visível, ligada à correção', 3000);
    }
    await dizer('Fim do exemplo', 2000);
  },
  async pedido({ pagina, dizer, ir }) {
    await pagina.goto(`${BASE}/pedidos`);
    await dizer('Pedido de exame: a lista de todos os pedidos', 2600);
    await dizer('Clique em qualquer lugar da linha para ver o detalhe', 800);
    await (await ir('tr.clicavel td:nth-child(3)')).click();
    await pagina.waitForURL(/\/pedidos\/\d+$/);
    await dizer('O detalhe do pedido, com os exames e o estado', 3000);
    await pagina.goto(`${BASE}/pedidos`);
    await dizer('Para pedir exames, clique em Novo pedido', 800);
    await (await ir('a[href="/pedidos/novo"]')).click();
    await pagina.waitForSelector('[data-carrinho]');
    await dizer('Escolha o paciente', 800);
    await ir('select[name=paciente]');
    await dizer('Marque os exames: os tubos e o jejum aparecem ao lado', 1000);
    await (await ir('input[name=exames][value=GLI]')).check();
    await (await ir('input[name=exames][value=HEM]')).check();
    await pagina.waitForTimeout(2800);
    await dizer('Se for urgente, marque a caixa antes de enviar', 800);
    await (await ir('input[name=urgente]')).check();
    await (await ir('form[data-carrinho] button[type=submit]')).click();
    await pagina.waitForSelector('.alerta');
    await dizer('Pedido criado. Fim do exemplo', 2800);
  },
  async coleta({ pagina, dizer, ir, papel }) {
    await pagina.goto(`${BASE}/bancada`);
    await papel('Coleta');
    await pagina.goto(`${BASE}/coletas`);
    await dizer('Coleta: os tubos pedidos que ainda esperam a coleta', 2800);
    await dizer('Confira o jejum e o volume que o tubo exige', 900);
    const linha = 'tr:has-text("PED-000045")';
    await (await ir(`${linha} input[name=jejum]`)).click();
    await (await ir(`${linha} input[name=volume]`)).click();
    await dizer('Amostra hemolisada? Marque a caixa antes de coletar', 2200);
    await dizer('Clique em Coletar: a etiqueta é gerada', 800);
    await (await ir(`${linha} button:has-text("Coletar")`)).click();
    await pagina.waitForSelector('.etiqueta svg');
    await dizer('A etiqueta, com código de barras Code 128', 3200);
    await pagina.goto(`${BASE}/coletas`);
    await pagina.mouse.wheel(0, 500);
    await dizer('Amostra com problema? Escolha o motivo e rejeite: o pedido volta para recoleta', 3600);
    await ir('form[action$="/rejeitar"] select');
    await dizer('Fim do exemplo', 2000);
  },
  async soroteca({ pagina, dizer, ir }) {
    await pagina.goto(`${BASE}/soroteca?caixa=R2-C4`);
    await dizer('Soroteca: o mapa das caixas onde as amostras ficam guardadas', 2800);
    await dizer('Clique numa posição livre', 800);
    await (await ir('a.pos:not(.oc)')).click();
    await dizer('Escolha a amostra que está aguardando e clique em Armazenar', 1200);
    await ir('select[name=amostra]');
    await (await ir('button:has-text("Armazenar")')).click();
    await pagina.waitForSelector('.alerta.ok');
    await dizer('Amostra armazenada. Clique numa posição ocupada para ver a custódia', 1000);
    await (await ir('a.pos.oc')).click();
    await pagina.waitForSelector('.tempo');
    await dizer('A custódia: quem mexeu, quando e onde. Dá para mover ou descartar', 3800);
    await dizer('Fim do exemplo', 2000);
  },
  async equipamento({ pagina, dizer, ir, papel }) {
    await pagina.goto(`${BASE}/bancada`);
    await papel('Analista', 'ANA-01');
    await pagina.goto(`${BASE}/equipamentos`);
    await dizer('Equipamentos: a fila de cada máquina, que fala ASTM e HL7', 2800);
    await dizer('Amostras prontas esperam o envio ao equipamento', 900);
    await (await ir('form[action$="/enviar"] button')).click();
    await pagina.waitForSelector('.alerta.ok');
    await dizer('Enviada: a execução aparece na fila do equipamento', 2600);
    await dizer('Acelere o relógio do laboratório com ×600', 900);
    await (await ir('button[name=comando][value="600"]')).click();
    await dizer('As barras avançam sozinhas até o fim do processamento', 4200);
    await pagina.mouse.wheel(0, 700);
    await dizer('Embaixo, o monitor mostra as mensagens do protocolo', 3200);
    await dizer('Execução com erro? Use Reprocessar. Fim do exemplo', 2400);
  },
  async qualidade({ pagina, dizer, ir, papel }) {
    await pagina.goto(`${BASE}/bancada`);
    await papel('Analista');
    await pagina.goto(`${BASE}/qualidade`);
    await dizer('Qualidade: o controle do equipamento e as regras de Westgard', 3000);
    await dizer('O gráfico de Levey-Jennings mostra cada controle em desvios da média', 3400);
    await dizer('Para registrar um novo controle, clique no botão', 900);
    await (await ir('form[action$="/qualidade/medicao"] button')).click();
    await pagina.waitForSelector('.alerta');
    await dizer('Controle fora das regras bloqueia a validação dos resultados', 3400);
    await dizer('Fim do exemplo', 2000);
  },
  async resultados({ pagina, dizer, ir, papel }) {
    await pagina.goto(`${BASE}/bancada`);
    await papel('Analista', 'ANA-02');
    await pagina.goto(`${BASE}/resultados`);
    await dizer('Resultados: valide cada resultado e libere o laudo', 2800);
    await dizer('Quem enviou a amostra não valida: por isso o outro analista', 3000);
    let primeiro = true;
    for (let i = 0; i < 25; i++) {
      const critico = pagina.locator('form[action$="/confirmar-critico"] button').first();
      if (await critico.count()) {
        if (primeiro) await dizer('Valor crítico: confirme antes de validar', 700);
        await (await ir('form[action$="/confirmar-critico"] button', { espera: primeiro ? 1200 : 250 })).click();
        primeiro = false;
        continue;
      }
      const f = pagina.locator('form[action$="/validar"]').first();
      if (!(await f.count())) break;
      if (primeiro) await dizer('Clique em Validar em cada linha', 700);
      const just = f.locator('input[name=justificativa]');
      if (await just.count()) {
        await dizer('Delta suspeito: escreva a justificativa', 700);
        await just.click();
        await pagina.keyboard.type('Conferido com o paciente', { delay: 50 });
      }
      await (await ir('form[action$="/validar"] button', { espera: primeiro ? 1200 : 250 })).click();
      primeiro = false;
    }
    await dizer('Tudo validado: agora é possível liberar o laudo', 900);
    await (await ir('form[action$="/liberar"] button')).click();
    await pagina.waitForSelector('.laudo, .alerta');
    await dizer('Laudo liberado: ele não muda mais, só se retifica', 3600);
    await dizer('Fim do exemplo', 2000);
  },
  async auditoria({ pagina, dizer, ir }) {
    await pagina.goto(`${BASE}/auditoria`);
    await dizer('Auditoria: o registro de tudo, encadeado por hash', 2800);
    await dizer('Filtre por grupo para achar o que procura', 800);
    await (await ir('a.btn.peq[href*="grupo="]')).click();
    await dizer('Só os eventos do grupo escolhido', 2200);
    await (await ir('a.btn.peq[href="/auditoria"]')).click();
    await dizer('Clique em Verificar para recalcular a cadeia', 800);
    await (await ir('form[action$="/auditoria/verificar"] button')).click();
    await pagina.waitForSelector('.alerta.ok');
    await dizer('Íntegra: nenhum registro foi alterado', 2800);
    await dizer('Agora simule uma adulteração', 800);
    await (await ir('form[action$="/auditoria/simular"] button')).click();
    await pagina.waitForSelector('.alerta.critico');
    await dizer('A cadeia quebra e mostra onde. Fim do exemplo', 3200);
  },
  async operacao({ pagina, dizer, ir }) {
    await pagina.goto(`${BASE}/operacao`);
    await dizer('Operação: o painel dos números do laboratório', 2800);
    await (await ir('.numero')).hover();
    await dizer('No topo, os números principais', 2600);
    await pagina.mouse.wheel(0, 500);
    await dizer('Embaixo, os gráficos: tempo por exame, soroteca e fila', 3600);
    await pagina.mouse.wheel(0, 500);
    await dizer('Eles acompanham o que você faz nas outras telas', 3000);
    await dizer('Fim do exemplo', 2000);
  },
  async painel({ pagina, dizer, ir }) {
    await pagina.goto(`${BASE}/painel`);
    await dizer('Painel técnico: como a Bancada é feita e testada', 2800);
    const abas = await pagina.locator('a.aba').count();
    for (let i = 1; i < abas; i++) {
      const nome = (await pagina.locator('a.aba').nth(i).innerText()).trim();
      await (await ir(`a.aba >> nth=${i}`, { espera: 300 })).click();
      await pagina.waitForLoadState();
      await dizer(`Aba: ${nome}`, 2200);
    }
    await dizer('Fim do exemplo', 2000);
  },
};

mkdirSync(TEMP, { recursive: true });
for (const nome of alvos.length ? alvos : Object.keys(roteiros)) await gravar(nome, roteiros[nome]);
rmSync(TEMP, { recursive: true, force: true });
