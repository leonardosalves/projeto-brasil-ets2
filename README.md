# ETS2 Projeto Brasil 1:4 Map Mod

Projeto de mapa mod brasileiro para Euro Truck Simulator 2 em escala-alvo 1:4.

## Objetivo

Construir uma versao jogavel do Brasil por cidades e zonas, com crescimento incremental. Porto Alegre sera a primeira cidade piloto do projeto, usada para validar escala, estilo visual, workflow tecnico, empresas e rotas urbanas.

## Estrutura

- `mod/`: pacote base do mod para ser colocado/compactado como `.scs`.
- `docs/`: pesquisa urbana, workflow tecnico e plano de zonas.
- `tools/`: scripts auxiliares para empacotar e organizar o projeto.

## Primeira cidade

Cidade 01: Porto Alegre, RS.

Zona 01-A: Centro Historico, Gasometro, Orla Moacyr Scliar e Praia de Belas.

Motivo: e pequena, reconhecivel, tem vias importantes, pontos turisticos fortes e permite entregas urbanas em shopping, mercado/centro, eventos e obras/servicos da orla.

## Status

Scaffold inicial criado. O Map Editor salvou o mapa com o nome interno `projeto_brasil`; os primeiros setores foram copiados para `mod/map/projeto_brasil/`.

## Fluxo rapido

Depois de editar e salvar no Map Editor:

```powershell
.\tools\sync_from_editor.ps1
.\tools\install_mod.ps1
```

O primeiro comando copia os setores do editor para o repositorio. O segundo empacota e instala o `.scs` na pasta de mods do ETS2.

Guia da primeira zona: `docs/zone_01a_editor_guide.md`.

## Geracao por codigo

Existe uma trilha experimental para gerar a malha inicial por codigo com TruckLib:

```powershell
.\tools\generate_map.ps1
.\tools\install_mod.ps1
```

Detalhes: `docs/programmatic_generation.md`.

## Status do projeto

Historico de fases, decisoes e proximos passos: `docs/project_status.md`.
