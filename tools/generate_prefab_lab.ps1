$Mode = if ($args.Count -gt 0) { $args[0] } else { "prefab-only" }
$MapName = switch ($Mode) {
    "prefab-only" { "prefab_lab" }
    "node0" { "prefab_lab_node0" }
    "node1" { "prefab_lab_node1" }
    "node2" { "prefab_lab_node2" }
    "node0_node1" { "prefab_lab_node0_node1" }
    "node0_node1_radial" { "prefab_lab_node0_node1_radial" }
    "node0_node1_rotation" { "prefab_lab_node0_node1_rotation" }
    "all" { "prefab_lab_all" }
    default { "prefab_lab_custom" }
}

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$dotnet = Join-Path $repoRoot ".dotnet\dotnet.exe"
$project = Join-Path $PSScriptRoot "PrefabLabGenerator\PrefabLabGenerator.csproj"
$editorMap = Join-Path $env:USERPROFILE "Documents\Euro Truck Simulator 2\mod\user_map\map"
$mapName = $MapName

if (-not (Test-Path $dotnet)) {
    throw "SDK local nao encontrado em $dotnet."
}

if (Test-Path $editorMap) {
    Get-ChildItem -LiteralPath $editorMap -File -Filter "$mapName.*" | ForEach-Object {
        Remove-Item -LiteralPath $_.FullName -Force
    }

    foreach ($dirName in @($mapName, "$mapName.bak")) {
        $dir = Join-Path $editorMap $dirName
        if (Test-Path $dir) {
            Remove-Item -LiteralPath $dir -Recurse -Force
        }
    }

    $autosaveMapDir = Join-Path (Join-Path $editorMap "autosave") $mapName
    if (Test-Path $autosaveMapDir) {
        Remove-Item -LiteralPath $autosaveMapDir -Recurse -Force
    }
}

& $dotnet run --project $project -- $editorMap "C:\Program Files (x86)\Steam\steamapps\common\Euro Truck Simulator 2" $Mode $MapName
if ($LASTEXITCODE -ne 0) {
    throw "Gerador do prefab lab falhou com codigo $LASTEXITCODE"
}
