$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$dotnet = Join-Path $repoRoot ".dotnet\dotnet.exe"
$project = Join-Path $PSScriptRoot "ProjetoBrasilMapGenerator\ProjetoBrasilMapGenerator.csproj"
$editorMap = Join-Path $env:USERPROFILE "Documents\Euro Truck Simulator 2\mod\user_map\map"
$mapName = "projeto_brasil"

if (-not (Test-Path $dotnet)) {
    throw "SDK local nao encontrado em $dotnet. Instale com o script dotnet-install antes de gerar."
}

if (Test-Path $editorMap) {
    Get-ChildItem -LiteralPath $editorMap -File -Filter "$mapName.*" | ForEach-Object {
        Remove-Item -LiteralPath $_.FullName -Force
    }

    $sectorDir = Join-Path $editorMap $mapName
    if (Test-Path $sectorDir) {
        Remove-Item -LiteralPath $sectorDir -Recurse -Force
    }

    $backupDir = Join-Path $editorMap "$mapName.bak"
    if (Test-Path $backupDir) {
        Remove-Item -LiteralPath $backupDir -Recurse -Force
    }

    $autosaveDir = Join-Path $editorMap "autosave"
    if (Test-Path $autosaveDir) {
        Get-ChildItem -LiteralPath $autosaveDir -File -Filter "$mapName.*" | ForEach-Object {
            Remove-Item -LiteralPath $_.FullName -Force
        }

        $autosaveMapDir = Join-Path $autosaveDir $mapName
        if (Test-Path $autosaveMapDir) {
            Remove-Item -LiteralPath $autosaveMapDir -Recurse -Force
        }
    }
}

& $dotnet run --project $project -- $editorMap "C:\Program Files (x86)\Steam\steamapps\common\Euro Truck Simulator 2" @args
if ($LASTEXITCODE -ne 0) {
    throw "Gerador de mapa falhou com codigo $LASTEXITCODE"
}

# Flags suportadas (passadas via @args):
#   --with-junction / --real-junction     -> MELHOR FLUXO ATUAL: traçado real + T-junction (prefab 56) + primeiro acesso a empresa (side branch com 2 pontos perpendicular)
#   --junction-index N   (ou --junc N)    -> escolhe qual ponto do CSV será o centro da T-junction (padrão 5, bom para Orla/Gasômetro)
#   --side-length M      (ou --side M)    -> comprimento do acesso lateral para a empresa (padrão ~180-280m)
#   --with-start-prefab                   -> usa o experimento de prefab no "início" do corredor (curto, para testes)
#   --no-start-prefab                     -> força traçado contínuo sem prefab inicial (padrão atual / rollback)
#   --enable-prefab-experiment            -> adiciona prefab extra de teste (AddFirstTjunction)
& (Join-Path $PSScriptRoot "sync_from_editor.ps1")
