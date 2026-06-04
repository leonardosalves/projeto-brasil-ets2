param(
    [string]$MapName = "projeto_brasil",
    [string]$EditorModFolder = "$env:USERPROFILE\Documents\Euro Truck Simulator 2\mod\user_map"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectMapRoot = Join-Path $repoRoot "mod\map"
$editorMapRoot = Join-Path $EditorModFolder "map"

$requiredFiles = @("$MapName.mbd")
$optionalFiles = @("$MapName.set", "$MapName.expa")

if (-not (Test-Path $editorMapRoot)) {
    throw "Pasta de mapa do editor nao encontrada: $editorMapRoot"
}

if (-not (Test-Path $projectMapRoot)) {
    New-Item -ItemType Directory -Path $projectMapRoot | Out-Null
}

foreach ($file in $requiredFiles) {
    $src = Join-Path $editorMapRoot $file
    if (-not (Test-Path $src)) {
        throw "Arquivo do editor nao encontrado: $src"
    }
    Copy-Item -LiteralPath $src -Destination $projectMapRoot -Force
}

foreach ($file in $optionalFiles) {
    $src = Join-Path $editorMapRoot $file
    $dst = Join-Path $projectMapRoot $file

    if (Test-Path $src) {
        Copy-Item -LiteralPath $src -Destination $projectMapRoot -Force
    } elseif (Test-Path $dst) {
        Remove-Item -LiteralPath $dst -Force
    }
}

$srcSectorDir = Join-Path $editorMapRoot $MapName
$dstSectorDir = Join-Path $projectMapRoot $MapName

if (-not (Test-Path $srcSectorDir)) {
    throw "Pasta de setores do editor nao encontrada: $srcSectorDir"
}

if (-not (Test-Path $dstSectorDir)) {
    New-Item -ItemType Directory -Path $dstSectorDir | Out-Null
}

Get-ChildItem -LiteralPath $dstSectorDir -File -Filter "sec*" | ForEach-Object {
    Remove-Item -LiteralPath $_.FullName -Force
}

Get-ChildItem -LiteralPath $srcSectorDir -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $dstSectorDir -Force
}

Write-Host "Mapa sincronizado do editor para o projeto: $MapName"
