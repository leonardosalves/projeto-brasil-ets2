param(
    [string]$ModName = "projeto_brasil_1_4_map",
    [string]$Ets2ModFolder = "$env:USERPROFILE\Documents\Euro Truck Simulator 2\mod"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$packageScript = Join-Path $PSScriptRoot "package_mod.ps1"
$distFile = Join-Path $repoRoot "dist\$ModName.scs"

& $packageScript -ModName $ModName

if (-not (Test-Path $distFile)) {
    throw "Pacote nao encontrado apos empacotar: $distFile"
}

if (-not (Test-Path $Ets2ModFolder)) {
    New-Item -ItemType Directory -Path $Ets2ModFolder | Out-Null
}

$destinationFile = Join-Path $Ets2ModFolder "$ModName.scs"

try {
    Copy-Item -LiteralPath $distFile -Destination $Ets2ModFolder -Force
} catch {
    throw "Nao foi possivel instalar o mod em $destinationFile. Feche o ETS2/Map Editor se ele estiver aberto e rode este script novamente. Erro original: $($_.Exception.Message)"
}

Write-Host "Mod instalado em: $destinationFile"
