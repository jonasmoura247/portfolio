// Roteiro Playwright (browser_run_code_unsafe): portfólio, agenda com modal, atendimento. Servidor em http://localhost:50900 (HTTP: Bancada__CookiesSeguros=false).
async (page) => {
  const log = [];
  const ok = (nome, cond) => log.push((cond ? 'OK     ' : 'FALHOU ') + nome);
  const base = 'http://localhost:50900';
  const aviso = async () => (await page.locator('.alerta').first().innerText().catch(() => '')).replace(/\s+/g, ' ');
  await page.setViewportSize({ width: 1440, height: 900 });

  await page.goto(base + '/');
  ok('portfólio abre na raiz', (await page.title()).includes('Jonas'));
  await page.click('a[href="/bancada"]');
  ok('botão do portfólio leva à Bancada', page.url().endsWith('/bancada'));

  await page.click('.menu a[href="/agenda"]');
  await page.click('a:has-text("Próxima semana")');
  await page.locator('td.livre.clicavel').first().click();
  await page.waitForSelector('dialog[open]');
  ok('modal da agenda abre numa célula livre', true);
  await page.selectOption('dialog select[name=paciente]', 'PAC-0002');
  await page.fill('dialog textarea[name=motivo]', 'Retorno E2E');
  await page.click('dialog button:has-text("Agendar")');
  ok('agendamento criado: ' + (await aviso()), (await aviso()).includes('Agendamento criado'));

  await page.locator('a.marc:has-text("PAC-0002")').first().click();
  await page.waitForSelector('dialog[open]');
  const modal = await page.locator('dialog').innerText();
  ok('modal ocupado mostra paciente, motivo e ficha', modal.includes('PAC-0002') && modal.includes('Retorno E2E') && modal.includes('Ver ficha do paciente'));
  await page.click('dialog button:has-text("Confirmar")');
  ok('agendamento confirmado: ' + (await aviso()), (await aviso()).includes('confirmado'));

  await page.locator('a.marc:has-text("PAC-0002")').first().click();
  await page.waitForSelector('dialog[open]');
  await page.keyboard.press('Escape');
  await page.waitForLoadState();
  ok('Esc fecha o modal', (await page.locator('dialog[open]').count()) === 0);

  await page.click('.menu a[href="/atendimentos"]');
  const linhas = await page.locator('tr.clicavel').count();
  ok('fila com linhas clicáveis: ' + linhas, linhas > 0);
  await page.locator('tr.clicavel td:nth-child(3)').first().click();
  await page.waitForSelector('dialog[open]');
  ok('clique fora do link abre o resumo', (await page.locator('dialog').innerText()).includes('Exames pedidos'));
  await page.click('dialog [data-fechar]');
  await page.waitForLoadState();
  ok('botão X fecha o resumo', (await page.locator('dialog[open]').count()) === 0);

  await page.locator('form[action$="/checkin"] button').first().click();
  ok('check-in: ' + (await aviso()), (await aviso()).includes('Check-in feito'));
  return log.join('\n');
}
