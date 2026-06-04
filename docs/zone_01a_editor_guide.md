# Zona 01-A: guia de construcao no Map Editor

Cidade piloto: Porto Alegre, RS

Mapa interno: `projeto_brasil`

Objetivo desta etapa: criar um loop urbano pequeno, reconhecivel e dirigivel antes de detalhar a cidade.

## Recorte

Foco inicial:

- Centro Historico
- Usina do Gasometro
- Orla Moacyr Scliar
- Praia de Belas
- começo da Av. Ipiranga

Este recorte deve parecer Porto Alegre, mas nao precisa reproduzir todas as ruas. A escala 1:4 pede selecao: e melhor ter poucas vias boas do que muitas vias vazias.

## Ordem recomendada no editor

### 1. Base de agua e horizonte

Crie primeiro a referencia visual do Guaiba a oeste/sudoeste. Isso ajuda a orientar todo o restante da malha.

Elementos:

- plano de agua/lago;
- margem curva simples;
- area aberta da Orla;
- espaco para o Gasometro como landmark.

### 2. Loop rodoviario minimo

Monte um circuito inicial com quatro segmentos:

1. Av. Edvaldo Pereira Paiva, acompanhando a Orla.
2. Av. Praia de Belas, paralela mais urbana.
3. Av. Borges de Medeiros, entrando em direcao ao Centro.
4. Av. Loureiro da Silva ou uma ligacao simplificada para fechar o loop.

Meta: o jogador deve conseguir dar uma volta completa sem ruas sem saida.

### 3. Pontos de referencia

Adicione primeiro silhuetas e massas, nao detalhes finos.

Landmarks prioritarios:

- Usina do Gasometro: predio baixo com chamine alta.
- Mercado Publico/Centro: bloco historico e area densa.
- Praia de Belas Shopping: volume comercial grande perto da Av. Praia de Belas.
- Parque Marinha do Brasil: area verde entre Praia de Belas e Orla.

### 4. Empresas temporarias

Antes das empresas definitivas, use prefabs simples para testar carga e manobra:

- `POA Mercado Logistica`: Centro Historico.
- `Orla Eventos`: perto do Gasometro/Orla.
- `Praia Retail`: Praia de Belas Shopping.
- `Guaiba Obras`: area de servico/manutencao proxima a Orla.

### 5. Teste de direcao

Depois de salvar, testar:

- se o caminhao consegue completar o loop;
- se as curvas aceitam carreta simples;
- se nao ha buracos, degraus ou cruzamentos quebrados;
- se existem pontos claros para spawn/garagem/servico;
- se o horizonte do Guaiba aparece do lado correto.

## Checklist de aceite da Zona 01-A

- Loop completo dirigivel.
- Pelo menos 2 pontos de entrega funcionais.
- Pelo menos 3 landmarks reconheciveis.
- Iluminacao e vegetacao basicas.
- Setores sincronizados para o repositorio.
- `.scs` empacotado e instalado no ETS2.

## Comandos depois de salvar no editor

No PowerShell, dentro deste repositorio:

```powershell
.\tools\sync_from_editor.ps1
.\tools\install_mod.ps1
```

