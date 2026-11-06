#requires -Version 7
# Roda localmente os mesmos passos do CI (.github/workflows/ci.yml). Falha no primeiro passo que falhar.
$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_UICULTURE = 'en-US'          # saída em inglês para o passo de vulnerabilidades
$env:DOTNET_NOLOGO = '1'
Set-Location (Split-Path $PSScriptRoot -Parent)

function Passo([string]$nome, [scriptblock]$acao) {
    Write-Host "`n== $nome" -ForegroundColor Cyan
    & $acao
    if ($LASTEXITCODE -ne 0) { Write-Host "FALHOU: $nome" -ForegroundColor Red; exit 1 }
}

Passo '1/6 restauração travada (packages.lock.json)'  { dotnet restore Bancada.sln --locked-mode }
Passo '2/6 formatação (.editorconfig)'                { dotnet format Bancada.sln --verify-no-changes --no-restore }
Passo '3/6 build Release (avisos como erro)'          { dotnet build Bancada.sln -c Release --no-restore }
Remove-Item coverage/trx -Recurse -Force -ErrorAction SilentlyContinue
Passo '4/6 testes C#: 100% de linhas e ramos, guardas de arquitetura e licenças' { dotnet test Bancada.sln -c Release --no-build --logger trx --results-directory coverage/trx }
Passo '5/6 testes JS: 100% de linhas, ramos e funções' { npm run test:js | Tee-Object coverage/js.txt }
Passo '6/6 pacotes vulneráveis'                       {
    $saida = dotnet list Bancada.sln package --vulnerable --include-transitive 2>&1 | Out-String
    Write-Host $saida
    if ($saida -match 'has the following vulnerable packages') { $global:LASTEXITCODE = 1 } else { $global:LASTEXITCODE = 0 }
}

Passo 'instantâneo do painel técnico'               { node tools/gerar-instantaneo.mjs }

Write-Host "`nTudo verde: pode seguir." -ForegroundColor Green
