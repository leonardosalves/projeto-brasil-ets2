# Workflow tecnico ETS2 map mod

## Identidade do mapa

Nome interno do mapa no ETS2: `projeto_brasil`.

Nome publico do mod: Projeto Brasil 1:4 Map.

Primeira cidade piloto: Porto Alegre, RS.

## O que o jogo fornece

O Euro Truck Simulator 2 inclui um Map Editor dentro do proprio jogo. A SCS documenta duas formas comuns de abrir:

- adicionar `-edit` no atalho do executavel `eurotrucks2.exe`;
- abrir o console do jogo e usar o comando `edit`.

Para editar diretamente este mapa, usar `-edit projeto_brasil`.

Exemplo:

```text
"C:\Program Files (x86)\Steam\steamapps\common\Euro Truck Simulator 2\bin\win_x64\eurotrucks2.exe" -edit projeto_brasil -noworkshop
```

## Arquivos que o editor gera

Segundo a documentacao da SCS, ao salvar um mapa o editor cria uma pasta dentro de:

```text
C:\Users\<usuario>\Documents\Euro Truck Simulator 2\mod
```

O mapa moderno usa um arquivo `.mbd` com metadados e uma subpasta de setores em `map/<nome_do_mapa>/`. Os setores sao a parte realmente editada e devem ser versionados depois que forem criados pelo editor.

Estrutura esperada:

```text
mod/
  manifest.sii
  mod_description.txt
  map/
    projeto_brasil.mbd
    projeto_brasil.set
    projeto_brasil.expa
    projeto_brasil/
      sec-*.base
      sec-*.aux
      sec-*.data
      sec-*.desc
      sec-*.layer
```

Os nomes exatos dos setores dependem da posicao do mapa no grid do editor.

## Estrutura do pacote deste projeto

```text
mod/
  manifest.sii
  mod_description.txt
  def/
    city.portoalegre.sii
    country.portoalegre.sii
  map/
    projeto_brasil.mbd
    projeto_brasil.set
    projeto_brasil.expa
    projeto_brasil/
      sec-*
```

O `manifest.sii` faz o mod aparecer com nome/autor/categoria no Mod Manager.

## Empacotamento local

1. Copiar ou salvar os setores do editor para `mod/map/projeto_brasil/`.
2. Rodar `tools/package_mod.ps1`.
3. Copiar `dist/projeto_brasil_1_4_map.scs` para:

```text
C:\Users\<usuario>\Documents\Euro Truck Simulator 2\mod
```

4. Ativar no Mod Manager do ETS2.

## Sincronizacao automatica

Depois de salvar no Map Editor, rode:

```powershell
.\tools\sync_from_editor.ps1
```

Para empacotar e instalar na pasta de mods do ETS2:

```powershell
.\tools\install_mod.ps1
```

## Regras praticas do projeto

- Sempre testar cada zona em isolamento antes de expandir.
- Manter nomes de arquivos em minusculo e sem acentos.
- Usar arquivos infixados quando possivel, como `city.portoalegre.sii`, para reduzir conflito com outros mods.
- Preferir landmarks reconheciveis por silhueta: Gasometro, Mercado Publico, Beira-Rio, Ibere, Arena, shoppings e eixos com canteiro/corredor.
- Para a escala 1:4, nao tentar reproduzir todas as ruas; escolher uma malha essencial e reduzir quadras mantendo orientacao visual.

## Fontes tecnicas

- Map Editor: https://modding.scssoft.com/wiki/Documentation/Tools/Map_Editor
- Launching the Map Editor: https://modding.scssoft.com/wiki/Tutorials/Map_Editor/Introduction_to_the_Map_Editor/Launching_the_Map_Editor
- Saving, Loading, Sectors, and Files: https://modding.scssoft.com/wiki/Tutorials/Map_Editor/Introduction_to_the_Map_Editor/Saving%2C_Loading%2C_Sectors%2C_and_Files
- Mod Manager/manifest: https://modding.scssoft.com/index.php?mobileaction=toggle_view_desktop&title=Documentation%2FEngine%2FMod_manager
- Modding changes/sectors/infix files: https://eurotrucksimulator2.com/modding_changes.php
