param(
    [string]$ModName = "projeto_brasil_1_4_map"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$modRoot = Join-Path $repoRoot "mod"
$distRoot = Join-Path $repoRoot "dist"
$zipPath = Join-Path $distRoot "$ModName.zip"
$scsPath = Join-Path $distRoot "$ModName.scs"

if (-not (Test-Path $modRoot)) {
    throw "Pasta mod nao encontrada: $modRoot"
}

if (-not (Test-Path $distRoot)) {
    New-Item -ItemType Directory -Path $distRoot | Out-Null
}

Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $scsPath -Force -ErrorAction SilentlyContinue

Compress-Archive -Path (Join-Path $modRoot "*") -DestinationPath $zipPath -Force
Move-Item -LiteralPath $zipPath -Destination $scsPath

Write-Host "Pacote criado: $scsPath"
