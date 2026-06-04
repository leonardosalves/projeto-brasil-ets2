# Geracao programatica de mapa

Este projeto usa uma trilha experimental com TruckLib para gerar setores do ETS2 por codigo.

## O que ja da para gerar

- mapa interno `projeto_brasil`;
- arquivos `.mbd`, `.set`, `.expa`;
- setores `sec-*`;
- estradas simples a partir de coordenadas aproximadas reais;
- escala 1:4 aplicada nos pontos convertidos de latitude/longitude para metros.

## Limites importantes

- O gerador nao substitui totalmente o Map Editor.
- Depois de gerar, abra o mapa no editor e rode `Map > Recompute map`.
- Prefabs, empresas, cruzamentos complexos e modelos podem quebrar se os nomes de assets nao existirem na versao/DLC instalada.
- Nao devemos copiar setores, modelos ou assets de mapas de terceiros. Podemos estudar estrutura, documentacao e mods permitidos, mas o Projeto Brasil precisa ter conteudo proprio.

## Como gerar

No PowerShell, dentro do repositorio:

```powershell
.\tools\generate_map.ps1
.\tools\install_mod.ps1
```

Depois abra o ETS2 com:

```text
-edit projeto_brasil -noworkshop
```

No editor:

1. abrir `Map > Recompute map`;
2. salvar;
3. fazer inspecao visual;
4. ajustar as vias no editor quando necessario.

## Fonte tecnica

- TruckLib: https://github.com/sk-zk/TruckLib
- Documentacao TruckLib: https://sk-zk.github.io/trucklib/master/
- SCS Map Editor: https://modding.scssoft.com/wiki/Documentation/Tools/Map_Editor
