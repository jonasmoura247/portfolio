// Roteiro Playwright, parte 3: Admin abre todas as abas em 4 larguras (monitor, tablet, iPhone, Android), sem rolagem lateral nem erro de console; tema e volta ao portfólio.
async (page) => {
  const log = [];
  const ok = (nome, cond) => log.push((cond ? 'OK     ' : 'FALHOU ') + nome);
  const base = 'http://localhost:50900';
  const erros = [];
  page.on('console', m => { if (m.type() === 'error' && !m.text().includes('favicon')) erros.push(m.text()); });
  page.on('pageerror', e => erros.push(String(e)));
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(base + '/bancada');
  await Promise.all([page.waitForNavigation(), page.selectOption('select[name=papel]', 'Admin')]);
  const rotas = await page.locator('.menu a').evaluateAll(as => as.map(a => a.getAttribute('href')));
  ok('Admin vê ' + rotas.length + ' abas: ' + rotas.join(' '), rotas.includes('/etiquetas') && rotas.includes('/soroteca') && !rotas.includes('/lab-ataque'));

  const larguras = { monitor: [1920, 1080], notebook: [1366, 768], tablet: [768, 1024], iphone: [390, 844], android: [360, 800] };
  for (const [nome, [w, h]] of Object.entries(larguras)) {
    await page.setViewportSize({ width: w, height: h });
    const ruins = [];
    for (const rota of rotas) {
      await page.waitForTimeout(700);
      const r = await page.goto(base + rota);
      const extra = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      if (r.status() !== 200 || extra > 1) ruins.push(`${rota} (${r.status()}, +${extra}px)`);
    }
    ok(`${nome} ${w}px: ${rotas.length} telas sem rolagem lateral`, ruins.length === 0);
    if (ruins.length) log.push('         ' + ruins.join(', '));
  }

  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(base + '/auditoria');
  await page.click('button:has-text("Verificar")');
  ok('auditoria verifica a cadeia', (await page.locator('body').innerText()).toLowerCase().includes('íntegra') || (await page.locator('.alerta').count()) > 0);
  await page.goto(base + '/operacao');
  ok('operação mostra números e gráficos', (await page.locator('.numero').count()) === 4 && (await page.locator('.operacao svg').count()) === 3);
  await page.goto(base + '/atendimentos?semana=true');
  await page.locator('tr.clicavel td:nth-child(3)').nth(2).click();
  await page.waitForSelector('dialog[open]');
  ok('Admin vê evolução clínica no resumo', (await page.locator('dialog').innerText()).includes('Evoluções deste atendimento') && (await page.locator('dialog .evolucao, dialog a.btn:has-text("Abrir atendimento completo")').count()) > 0);
  await page.keyboard.press('Escape');
  await page.waitForURL(u => !u.search.includes('abrir'));

  await page.goto(base + '/bancada');
  await page.click('button[data-alternar-tema]');
  const tema = await page.evaluate(() => document.documentElement.dataset.theme);
  await page.click('a:has-text("Portfólio")');
  ok('botão Portfólio volta ao currículo', new URL(page.url()).pathname === '/' && (await page.title()).includes('Jonas'));
  ok('tema escolhido vale no portfólio (' + tema + ')', (await page.evaluate(() => document.documentElement.dataset.theme)) === tema);
  await page.click('a[href="/bancada"]');
  ok('e o botão do currículo volta à Bancada', page.url().endsWith('/bancada'));
  ok('console sem erros' + (erros.length ? ': ' + erros.join(' | ') : ''), erros.length === 0);
  return log.join('\n');
}
