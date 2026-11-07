// Roteiro Playwright completo, sempre no modo escuro, com uma captura por tela em tools/e2e/capturas/ (ignorada pelo git).
// Servidor em http://localhost:50900 (HTTP: Bancada__CookiesSeguros=false). Rode com um servidor recém-iniciado.
async (page) => {
  const base = 'http://localhost:50900';
  const dir = 'tools/e2e/capturas/';
  const log = [];
  let n = 0;
  const aviso = async () => (await page.locator('.alerta').first().innerText().catch(() => '')).replace(/\s+/g, ' ');
  const snap = async nome => {
    n++;
    await page.waitForTimeout(450);
    await page.screenshot({ path: `${dir}${String(n).padStart(2, '0')}-${nome}.png` });
  };
  const ir = async url => { await page.waitForTimeout(550); await page.goto(base + url); };
  const trocar = async (papel, operador) => {
    await Promise.all([page.waitForNavigation(), page.selectOption('select[name=papel]', papel)]);
    if (operador) await Promise.all([page.waitForNavigation(), page.selectOption('select[name=operador]', operador)]);
  };
  const passo = async (nome, fn) => {
    try { await fn(); log.push('OK     ' + nome); } catch (e) { log.push('FALHOU ' + nome + ': ' + String(e).split('\n')[0]); }
  };

  await page.emulateMedia({ colorScheme: 'dark' });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(base + '/');
  await page.evaluate(() => localStorage.removeItem('bancada.tema'));

  await passo('portfólio: 4 abas', async () => {
    await page.goto(base + '/');
    await snap('portfolio-curriculo');
    for (const [aba, nome] of [['perfil', 'perfil'], ['exp', 'experiencia'], ['proj', 'projetos'], ['stack', 'stack'], ['contato', 'contato']]) {
      await page.click('#t-' + aba);
      await page.evaluate(() => window.scrollTo(0, 0));
      await snap('portfolio-' + nome);
    }
    await page.click('#t-cv');
  });
  await passo('portfólio leva à Bancada como Admin', async () => {
    await page.click('a[href="/bancada"]');
    await page.waitForURL('**/bancada');
    if ((await page.locator('select[name=papel]').inputValue()) !== 'Admin') throw new Error('não entrou como Admin');
    await snap('bancada-inicio-admin');
  });

  await passo('pacientes e ficha', async () => {
    await ir('/pacientes'); await snap('pacientes');
    await ir('/pacientes/14'); await snap('paciente-ficha');
  });

  await passo('agenda: modal livre, agendar, modal ocupado, confirmar', async () => {
    await ir('/agenda?profissional=MED-01&semana=1'); await snap('agenda');
    await page.locator('td.livre.clicavel').first().click();
    await page.waitForSelector('dialog[open]');
    await page.selectOption('dialog select[name=paciente]', 'PAC-0002');
    await page.fill('dialog textarea[name=motivo]', 'Retorno para conferir exames');
    await snap('agenda-modal-livre');
    await page.click('dialog button:has-text("Agendar")');
    await snap('agenda-agendado');
    await page.locator('a.marc:has-text("PAC-0002")').first().click();
    await page.waitForSelector('dialog[open]');
    await snap('agenda-modal-ocupado');
    await page.click('dialog button:has-text("Confirmar")');
    if (!(await aviso()).includes('confirmado')) throw new Error('sem confirmação');
    await snap('agenda-confirmado');
  });

  await passo('atendimento: fila, resumo, check-in', async () => {
    await ir('/atendimentos'); await snap('atendimento-fila');
    await page.locator('tr.clicavel td:nth-child(3)').first().click();
    await page.waitForSelector('dialog[open]');
    await snap('atendimento-resumo-modal');
    await page.click('dialog [data-fechar]');
    await page.waitForLoadState();
    await page.locator('form[action$="/checkin"] button').first().click();
    if (!(await aviso()).includes('Check-in feito')) throw new Error('sem check-in');
    await snap('atendimento-checkin');
  });

  let urlAtendimento = '';
  await passo('médico atende e pede exame', async () => {
    await trocar('Medico');
    await ir('/atendimentos');
    const medico = (await page.locator('tr:has(form[action$="/iniciar"])').first().innerText()).match(/MED-0\d/)[0];
    await Promise.all([page.waitForNavigation(), page.selectOption('select[name=operador]', medico)]);
    await ir('/atendimentos'); await snap('medico-fila');
    await page.locator('form[action$="/iniciar"] button').first().click();
    urlAtendimento = page.url();
    await page.fill('textarea[name=texto]', 'Paciente estável, exames de rotina solicitados.');
    await page.click('button:has-text("Registrar evolução")');
    await snap('atendimento-detalhe-evolucao');
    await page.click('a:has-text("Pedir exame")');
    await page.check('input[name=exames][value=GLI]');
    await page.check('input[name=exames][value=HEM]');
    await snap('pedido-novo');
    await page.click('button:has-text("Enviar pedido")');
    await snap('pedido-detalhe');
    await page.goto(urlAtendimento);
    await page.click('button:has-text("Concluir atendimento")');
    if (!(await aviso()).includes('concluído')) throw new Error('não concluiu');
    await snap('atendimento-concluido');
  });

  await passo('pedidos e prontuário', async () => {
    await ir('/pedidos'); await snap('pedidos-lista');
    await ir('/prontuario'); await snap('prontuario');
  });

  await passo('coleta, etiqueta e consulta', async () => {
    await trocar('Coleta');
    await ir('/coletas'); await snap('coleta');
    await page.locator('tr:has-text("PED-000045") button:has-text("Coletar")').first().click();
    await snap('coleta-etiqueta');
    const codigo = (await page.locator('.etiqueta b').innerText()).trim();
    await ir('/etiquetas'); await snap('consultar-etiqueta');
    await page.fill('input[name=busca]', codigo);
    await page.click('button:has-text("Buscar")');
    await snap('consultar-etiqueta-busca');
  });

  await passo('soroteca', async () => {
    await ir('/soroteca?caixa=R2-C4');
    await page.locator('a.pos:not(.oc)').first().click();
    await snap('soroteca-posicao');
    await page.click('button:has-text("Armazenar")');
    if (!(await aviso()).includes('armazenada')) throw new Error('não armazenou');
    await snap('soroteca-armazenada');
  });

  await passo('equipamento e qualidade', async () => {
    await trocar('Analista', 'ANA-01');
    await ir('/equipamentos');
    await page.locator('form[action$="/enviar"] button').first().click();
    await snap('equipamento-enviado');
    await page.click('button[name=comando][value="600"]');
    await page.waitForTimeout(3500);
    await ir('/equipamentos'); await snap('equipamento-processado');
    await ir('/qualidade'); await snap('qualidade');
  });

  await passo('resultados, validação e laudo', async () => {
    await trocar('Analista', 'ANA-02');
    await ir('/resultados'); await snap('resultados-pendentes');
    for (let i = 0; i < 25; i++) {
      const critico = page.locator('form[action$="/confirmar-critico"] button').first();
      if (await critico.count()) { await critico.click(); continue; }
      const f = page.locator('form[action$="/validar"]').first();
      if (!(await f.count())) break;
      const j = f.locator('input[name=justificativa]');
      if (await j.count()) await j.fill('Conferido');
      await f.locator('button').click();
    }
    await snap('resultados-validados');
    await page.locator('form[action$="/liberar"] button').first().click();
    await snap('laudo');
  });

  await passo('admin: auditoria, operação, painel técnico, início', async () => {
    await trocar('Admin');
    await ir('/auditoria'); await snap('auditoria');
    await page.click('button:has-text("Verificar")'); await snap('auditoria-verificada');
    await ir('/operacao'); await snap('operacao');
    await ir('/painel'); await snap('painel-tecnico');
    await ir('/bancada'); await snap('inicio-final');
  });

  await passo('celular (iPhone 390x844)', async () => {
    await page.setViewportSize({ width: 390, height: 844 });
    await ir('/'); await snap('iphone-portfolio');
    await ir('/bancada'); await snap('iphone-bancada');
    await ir('/agenda?profissional=MED-01&semana=1'); await snap('iphone-agenda');
    await page.locator('td.livre.clicavel').first().click();
    await page.waitForSelector('dialog[open]');
    await snap('iphone-agenda-modal');
    await page.keyboard.press('Escape');
    await page.waitForLoadState();
    await ir('/operacao'); await snap('iphone-operacao');
    await ir('/etiquetas'); await snap('iphone-etiquetas');
  });

  return log.join('\n') + '\n\n' + n + ' capturas';
}
