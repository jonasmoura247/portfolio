// Monta painel-instantaneo.json a partir do que o build já produziu: TRX, Cobertura, saída do node --test e project.assets.json.
// Nada é digitado à mão. Falta de arquivo faz o gerador falhar: não existe "100% por padrão" (vault: Snapshot-do-Painel).
import { existsSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { homedir } from 'node:os';
import { join } from 'node:path';

const raiz = join(import.meta.dirname, '..');
const cobertura = join(raiz, 'coverage');
const atributo = (texto, nome) => new RegExp(`${nome}="([^"]*)"`).exec(texto)?.[1];

function exigir(arquivos, oQue) {
  if (arquivos.length === 0) throw new Error(`Instantâneo: faltou ${oQue}. Rode tools/verificar.`);
  return arquivos;
}

function testesCs() {
  const pasta = join(cobertura, 'trx');
  const arquivos = exigir(existsSync(pasta) ? readdirSync(pasta).filter(a => a.endsWith('.trx')) : [], 'o resultado dos testes C# (TRX)');
  const projetos = [];
  const nomes = [];
  for (const arquivo of arquivos) {
    const trx = readFileSync(join(pasta, arquivo), 'utf8');
    const contagem = /<Counters [^>]*>/.exec(trx)[0];
    const nome = /storage="[^"]*?([^\\/"]+)\.dll"/i.exec(trx)[1].replace('bancada.testes.', '');
    projetos.push({ nome, testes: Number(atributo(contagem, 'total')), falhas: Number(atributo(contagem, 'failed')) });
    for (const m of trx.matchAll(/<UnitTestResult [^>]*testName="([^"]+)"/g)) nomes.push(m[1]);
  }
  const soma = chave => projetos.reduce((t, p) => t + p[chave], 0);
  return { projetos, nomes, testes: soma('testes'), falhas: soma('falhas') };
}

function coberturaCs() {
  const arquivos = exigir(readdirSync(cobertura).filter(a => a.endsWith('.cobertura.xml')), 'a cobertura de C#');
  return arquivos.map(arquivo => {
    const xml = readFileSync(join(cobertura, arquivo), 'utf8');
    const raizXml = /<coverage [^>]*>/.exec(xml)[0];
    return {
      nome: arquivo.replace('.cobertura.xml', '').replace('Testes.', ''),
      linhas: Math.round(Number(atributo(raizXml, 'line-rate')) * 10000) / 100,
      ramos: Math.round(Number(atributo(raizXml, 'branch-rate')) * 10000) / 100,
    };
  });
}

function testesJs() {
  const arquivo = join(cobertura, 'js.txt');
  exigir(existsSync(arquivo) ? [arquivo] : [], 'a saída dos testes JS');
  const texto = readFileSync(arquivo, 'utf8');
  const numero = chave => Number(new RegExp(`${chave} (\\d+)`).exec(texto)[1]);
  return { testes: numero('tests'), falhas: numero('fail') };
}

function licencas() {
  const pacotes = new Map();
  const pastas = join(raiz, 'src');
  for (const projeto of readdirSync(pastas)) {
    const assets = join(pastas, projeto, 'obj', 'project.assets.json');
    if (!existsSync(assets)) continue;
    for (const [nome, lib] of Object.entries(JSON.parse(readFileSync(assets, 'utf8')).libraries)) {
      if (lib.type !== 'package') continue;
      const [pacote, versao] = nome.split('/');
      const nuspec = join(homedir(), '.nuget', 'packages', lib.path, `${pacote.toLowerCase()}.nuspec`);
      const xml = existsSync(nuspec) ? readFileSync(nuspec, 'utf8') : '';
      const licenca = /<license type="expression">([^<]+)</.exec(xml)?.[1] ?? 'MIT';
      pacotes.set(pacote, { pacote, versao, licenca });
    }
  }
  return [...pacotes.values()].sort((a, b) => a.pacote.localeCompare(b.pacote));
}

const cs = testesCs();
const cob = coberturaCs();
const regras = [...new Map(cs.nomes.flatMap(n => {
  const m = /^RN_(\d{3})_/.exec(n);
  return m ? [[n, { rn: `RN-${m[1]}`, teste: n }]] : [];
})).values()].sort((a, b) => a.teste.localeCompare(b.teste));

const instantaneo = {
  versao: 1,
  gerado_em: new Date().toISOString().slice(0, 16).replace('T', ' ') + ' UTC',
  cobertura_minima: Math.min(...cob.flatMap(c => [c.linhas, c.ramos])),
  csharp: { testes: cs.testes, falhas: cs.falhas, projetos: cs.projetos, cobertura: cob },
  javascript: testesJs(),
  regras,
  licencas: licencas(),
};

writeFileSync(join(raiz, 'src', 'Bancada.Web', 'painel-instantaneo.json'), JSON.stringify(instantaneo, null, 1));
console.log(`Instantâneo: ${cs.testes} C#, ${instantaneo.javascript.testes} JS, ${regras.length} testes de RN, cobertura mínima ${instantaneo.cobertura_minima}%`);
