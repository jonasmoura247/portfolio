// Roteiro Playwright, parte 2: médico atende e pede exame, coleta, etiqueta, soroteca, equipamento, validação e laudo. Continua da parte 1 (mesma sessão).
async (page) => {
  const log = [];
  const ok = (nome, cond) => log.push((cond ? 'OK     ' : 'FALHOU ') + nome);
  const base = 'http://localhost:50900';
  const aviso = async () => (await page.locator('.alerta').first().innerText().catch(() => '')).replace(/\s+/g, ' ');
  const trocar = async (papel, operador) => {
    await Promise.all([page.waitForNavigation(), page.selectOption('select[name=papel]', papel)]);
    if (operador) await Promise.all([page.waitForNavigation(), page.selectOption('select[name=operador]', operador)]);
  };
  await page.setViewportSize({ width: 1440, height: 900 });

  await trocar('Medico');
  await page.goto(base + '/atendimentos');
  const medico = (await page.locator('tr:has(form[action$="/iniciar"])').first().innerText()).match(/MED-0\d/)[0];
  await Promise.all([page.waitForNavigation(), page.selectOption('select[name=operador]', medico)]);
  await page.goto(base + '/atendimentos');
  await page.locator('form[action$="/iniciar"] button').first().click();
  ok(`médico ${medico} inicia o atendimento: ` + (await aviso()), (await aviso()).includes('Atendimento iniciado'));
  const urlAtendimento = page.url();
  await page.fill('textarea[name=texto]', 'Evolução E2E: paciente estável.');
  await page.click('button:has-text("Registrar evolução")');
  ok('evolução registrada', (await page.locator('body').innerText()).includes('Evolução E2E'));

  await page.click('a:has-text("Pedir exame")');
  await page.check('input[name=exames][value=GLI]');
  await page.check('input[name=exames][value=HEM]');
  ok('carrinho mostra tubos', (await page.locator('[data-tubos]').innerText()).length > 10);
  await page.click('button:has-text("Enviar pedido")');
  ok('pedido criado: ' + (await aviso()), (await aviso()).includes('Pedido criado'));
  await page.goto(urlAtendimento);
  ok('pedido aparece no atendimento', (await page.locator('a.chip.a').count()) > 0);
  await page.click('button:has-text("Concluir atendimento")');
  ok('atendimento concluído: ' + (await aviso()), (await aviso()).includes('Atendimento concluído'));

  await page.click('.menu a[href="/pedidos"]');
  await page.locator('tr.clicavel td:nth-child(3)').first().click();
  ok('linha do pedido abre o detalhe', /\/pedidos\/\d+$/.test(page.url()));

  await trocar('Coleta');
  await page.click('.menu a[href="/coletas"]');
  await page.locator('tr:has-text("PED-000045") button:has-text("Coletar")').first().click();
  ok('coleta gera etiqueta: ' + (await aviso()), (await page.locator('.etiqueta svg').count()) === 1);
  const codigo = (await page.locator('.etiqueta b').innerText()).trim();

  await page.click('.menu a[href="/etiquetas"]');
  ok('aba lateral "Consultar etiqueta" existe', (await page.locator('.menu a[href="/etiquetas"]').innerText()).includes('Consultar etiqueta'));
  await page.fill('input[name=busca]', codigo.toLowerCase());
  await page.click('button:has-text("Buscar")');
  ok(`busca por ${codigo} acha 1 linha`, (await page.locator('tr.clicavel').count()) === 1);
  await page.locator('tr.clicavel td:nth-child(2)').first().click();
  ok('linha leva à etiqueta', page.url().includes('/etiqueta') && (await page.locator('.etiqueta b').innerText()).trim() === codigo);

  await page.click('.menu a[href="/soroteca"]');
  ok('aba lateral "Consultar soroteca" existe', (await page.locator('.menu a[href="/soroteca"]').innerText()).includes('Consultar soroteca'));
  await page.goto(base + '/soroteca?caixa=R2-C4');
  await page.locator('a.pos:not(.oc)').first().click();
  await page.click('button:has-text("Armazenar")');
  ok('soroteca armazena: ' + (await aviso()), (await aviso()).includes('Amostra armazenada'));

  await trocar('Analista', 'ANA-01');
  await page.click('.menu a[href="/equipamentos"]');
  await page.locator('form[action$="/enviar"] button').first().click();
  ok('equipamento recebe a amostra: ' + (await aviso()), (await aviso()).includes('enviada ao equipamento'));
  await page.click('button[name=comando][value="600"]');
  await page.waitForTimeout(3500);

  await trocar('Analista', 'ANA-02');
  await page.goto(base + '/resultados');
  for (let i = 0; i < 25; i++) {
    const critico = page.locator('form[action$="/confirmar-critico"] button').first();
    if (await critico.count()) { await critico.click(); continue; }
    const f = page.locator('form[action$="/validar"]').first();
    if (!(await f.count())) break;
    const j = f.locator('input[name=justificativa]');
    if (await j.count()) await j.fill('Conferido E2E');
    await f.locator('button').click();
  }
  await page.locator('form[action$="/liberar"] button').first().click();
  ok('laudo liberado: ' + (await aviso()), (await aviso()).includes('Laudo liberado') || (await page.locator('.laudo').count()) > 0);
  return log.join('\n');
}
