# Treasure Hunter — Nível 1: A Entrada do Templo

Projeto Unity da entrega do 1º bimestre (Game Development — UVV 2026/2).
**Unity 6000.0.8f1 · template Universal 3D (URP) · 3ª pessoa · tema principal: Skills.**

> Os scripts **não usam assets de terceiros**: todo o visual (estilo voxel/retrô do GDD), as animações e os sons
> são gerados pelo próprio projeto. Assim, o nível funciona sem baixar nenhum pacote.
> Se quiserem usar os pacotes da Asset Store listados no GDD, vejam a seção "Trocar pelos assets da Asset Store".

---

## 1. Como montar o projeto (≈ 5 minutos)

1. No **Unity Hub**, crie um projeto novo com o **Unity 6000.0.8f1** e o template **Universal 3D**.
2. Feche o Unity. Copie a pasta `Assets/TreasureHunter` deste pacote para dentro da pasta `Assets` do projeto novo.
3. Abra o projeto e espere o Unity importar e compilar (sem erros no Console).
4. No menu de cima, clique em **Treasure Hunter → Construir Nível 1 (gerar tudo)**.
   O gerador pode levar alguns minutos. Ele cria texturas, materiais, partículas, os prefabs do cenário,
   o personagem com as animações, as cenas `MainMenu` e `Level1` e já configura o *Build Settings*.
5. Abra `Assets/TreasureHunter/Scenes/MainMenu.unity` e aperte **Play**.
   Para testar direto o nível, abra a cena `Level1` e aperte Play.

> Pode rodar o gerador de novo sempre que quiser: ele apaga só a pasta `Generated` e refaz tudo.
> **Atenção:** ele também recria as cenas `MainMenu` e `Level1`. Se editarem essas cenas à mão, salvem com outro nome antes.

### Atualizações por pacote (etapa 2, terreno)

Cada pacote de atualização muda prefabs e a cena. Aplicar uma atualização segue os passos abaixo. No pacote do terreno, importem antes os pacotes da aula (seção 1.1).

### Atualização da etapa 2 (ajustes da Sala 4, placas e símbolos)

Os ajustes desta atualização mudam prefabs e a cena, então o nível precisa ser **gerado de novo uma vez**:

1. Uma pessoa só: dê `git pull`, extraia o pacote de atualização por cima da pasta do projeto e abra o Unity.
2. Rode **Treasure Hunter → Construir Nível 1 (gerar tudo)**. Os assets extras (cristais e teias) já vêm incluídos.
3. Teste o nível, faça o commit e o push. Os outros só precisam dar `git pull`.

Se alguém tiver mexido à mão na cena `Level1` ou nos prefabs da pasta `Generated`, essas mudanças se perdem. Anotem antes para refazer depois.

**Para a entrega:** zipem a pasta do projeto inteira (`Assets`, `Packages`, `ProjectSettings`, `UserSettings`), sem `Library`, `Temp` e `Logs`.

---

## 1.1 Terreno da superfície (aula 2B)

A floresta em volta do templo agora é um **Unity Terrain** de verdade, criado pelo gerador seguindo os passos da aula de terreno.

**Antes de gerar (passos 2 a 4 da aula):** importem pela Asset Store os pacotes usados em aula:

- [Nature Starter Kit 2](https://assetstore.unity.com/packages/3d/environments/nature-starter-kit-2-52977) (Shapes): textura "ground" e árvores.
- [Grass Flowers Pack Free](https://assetstore.unity.com/packages/2d/textures-materials/nature/grass-flowers-pack-free-138810) (OE Objective Environment): texturas de grama e flores.

No Unity: **Window → Package Manager → My Assets**, baixe e importe os dois. Depois rode **Treasure Hunter → Construir Nível 1 (gerar tudo)**.

- O gerador procura esses pacotes sozinho e usa os assets deles no terreno.
- Se não encontrar algum, usa texturas e árvores próprias no estilo voxel. O jogo funciona igual.
- O que foi usado de cada pacote aparece no fim do `Docs/Inventario_Nivel1.md` (seção "Terreno — assets usados").
- Quem gerar o nível deve fazer commit também das pastas que os pacotes criam dentro de `Assets`. O terreno aponta para os arquivos delas; sem elas, os colegas veriam o terreno sem essas texturas e árvores.

| Passo da aula | Como ficou no terreno do jogo |
| --- | --- |
| 5) Create 3D Object → Terrain | Objeto **Terreno** dentro de `Superficie` na cena `Level1` (com Terrain Collider) |
| 6) Terrain Settings | Width/Length **160** (para cobrir floresta, lago, ravina e templo; 50 cobriria só o templo) · **Height 600** · **Detail Resolution 256** · **Detail Resolution Per Patch 32** |
| 7) Set Height 60 + Flatten All | Base plana na altura **60**, que no jogo fica em y = 0 (onde estão templo, acampamento e trilha) |
| 8) Paint Texture + Terrain Layers | 6 camadas, cada uma com **Normal Map**, **Tiling 20** e **Normal Scale 5**. A primeira (grama, ou o "ground" do pacote) cobre o terreno todo |
| 8.1) Outras camadas e caminhos | Terra nos caminhos (acampamento → templo, trilhas para o lago e a ponte), areia, cascalho, rocha e grama seca |
| 9) Raise or Lower / Stamp Terrain | Morros suaves, um vale, montanhas em volta da área jogável e um morro grande "carimbado" atrás do templo |
| 10) Paint Details | Grama e flores com **billboard desligado** (render mode *Grass*), **Noise Spread 0,5**, cores saudável (verde) e seca (amarela) |
| 11) Paint Trees / Mass Place Trees | ~280 árvores espalhadas com **tamanhos e rotações aleatórios**, fora das trilhas, do lago e da ravina |

Elementos de terreno já prontos para o 2º bimestre (mínimo de 10): textura, **morros**, **vales**, **água** (lago), **ravina**, **areia**, **cascalho**, **grama**, **gramíneas** (detalhes), **árvores** e **caminhos**.

Outras novidades da superfície:
- uma **ponte de madeira** atravessa a ravina, com um pequeno mirante e um baú do outro lado;
- há moedas e joias sorteadas pela floresta.

**Esculpir à mão depois:** selecionem o objeto `Terreno` e usem os pincéis da aula (Raise or Lower, Paint Texture, Paint Details, Paint Trees).

- Mantenham planas as áreas do templo, do acampamento e da trilha, onde ficam os objetos do jogo.
- Rodar o gerador de novo refaz o terreno do zero e apaga o que foi esculpido à mão. Esculpam por último ou anotem o que fizeram.

## 2. Controles

| Tecla | Ação |
|---|---|
| W A S D / setas | mover (em relação à câmera) |
| Mouse | girar a câmera |
| Shift | correr |
| Espaço | pular (segurar = pula mais alto) |
| E | interagir (alavanca, braseiro, símbolos, inscrições, pegar o ídolo) |
| Andar contra o caixote | empurrar |
| F ou clique esquerdo | erguer/abaixar a tocha |
| Tab | Diário de Habilidades |
| Esc | pausa (Continuar · Reiniciar sala · **Reiniciar jogo** · Menu principal) |

---

## 3. Roteiro do Nível 1 (para a apresentação)

O mapa visto de cima está em `Docs/Mapa_Nivel1.png`.

| Trecho | O que acontece | Habilidade (tema Skills) |
|---|---|---|
| **Introdução — floresta** | John começa no acampamento e segue a trilha até as ruínas. Popups ensinam a andar, correr e pular. O terreno tem morros, um lago, uma ravina com ponte (e um baú do outro lado) e montanhas em volta. | — |
| **Templo da superfície** | Sobe a escadaria, entra na câmara e pega o **Ídolo de Añang** (E). O chão treme, a porta se fecha e o piso desaba (blocos com AddForce + AddTorque). John cai no poço e acorda no subterrâneo. | Exploração |
| **Sala 1 — Câmara das Raízes** | A saída está fechada. A **alavanca escondida** fica no canto nordeste, entre raízes e arbustos (um feixe de luz dá a pista). **Segredo:** a cortina de trepadeiras na parede oeste esconde um nicho com a **Máscara Charrúa**. | Observação |
| **Sala 2 — Câmara do Peso** | Empurrar o **caixote** até ficar **bem em cima** da **placa de pressão** (o centro do caixote sobre a placa). A porta só fica aberta enquanto o caixote estiver lá; tirando o caixote, ela fecha. **Segredo:** encostando o caixote na plataforma alta e subindo nele, dá para pegar o **Colar de Jade**. | Força |
| **Corredor das Pedras** | Pedras caem do teto em pontos sorteados (a poeira avisa onde vão cair). Ao bater, elas explodem em poeira e pedrinhas. | Reflexos |
| **Sala 3 — Câmara dos Símbolos** | Sala escura. Acender os **2 braseiros** (E) ou erguer a tocha (F) perto da porta revela 4 símbolos. Pressionar os botões **na mesma ordem, da esquerda para a direita**. Errar só reinicia o mecanismo. A ordem é sorteada a cada partida. | Domínio da Tocha, Memória |
| **Sala 4 — Portão do Templo** | Uma parede divide a sala. Na passagem, a 3,5 m do portão, gira sem parar um **tronco com espinhos** (torque). Combina tudo: pular o tronco no momento certo → puxar a **alavanca** do outro lado (o tronco freia, recolhe os espinhos e afunda no chão, e o alçapão solta o caixote) → empurrar o caixote até ficar sobre a **placa** (energiza o mecanismo) → acender os **braseiros** para revelar os símbolos acima do portão → digitar a sequência nos botões → o portão abre. Ele só fica aberto com o caixote sobre a placa. | Combinação |
| **Saída** | Luz no fim do túnel: aparece a tela de **Nível concluído** com artefatos (x/3), moedas, joias, vida, habilidades (x/7) e tempo, e o botão **Reiniciar jogo**. | — |

Moedas e joias aparecem em posições sorteadas a cada partida. Há um coração de cura na Sala 3.
Se a vida (5 corações) acabar, a tela de derrota oferece voltar ao checkpoint, reiniciar o jogo ou ir ao menu.

---

## 4. Requisitos do 1º bimestre → onde estão

| Critério do PDF | Implementação (arquivo → função) |
|---|---|
| Tema (Skills) | `SkillManager` (7 habilidades), aviso "Nova habilidade" no HUD, **Diário de Habilidades** (Tab), 4 **Tábuas de Habilidade** (`SkillTablet`), habilidades desbloqueadas por `Lever`, `PressurePlate`, `RoomTrigger`, `Brazier`/`TorchRevealable`, `SymbolSequencePuzzle` e `LevelExit` |
| Colisão | `OnCollisionEnter/Stay` em `FallingRock`, `PlayerController` (empurrar), `Hazard`; gatilhos `OnTriggerEnter/Exit` em `PressurePlate`, `Collectible`, `RoomTrigger`, `RockSpawner`, `KillZone`, `FallTransition` |
| Movimentação — Translate | `StoneDoor.Update` (`transform.Translate`) |
| Movimentação — AddForce | `PlayerController.ApplyMovement / HandleJump / ApplyExtraGravity / Knockback`, `PushableCrate.Push`, `FallingRock.Start`, `TempleCollapseTrap` |
| Rotação | `Lever` (cabo), `HatchRelease` (dobradiça), `Spinner` (tesouros e câmera do menu), `ThirdPersonCamera`, `PlayerController.RotateModel` |
| Torque | `SpinningLogTrap.FixedUpdate` (`AddTorque` para girar e para frear), `FallingRock.Start`, blocos do piso em `TempleCollapseTrap` |
| Vetor de prefabs | `RockSpawner.rockPrefabs` (pedra pequena/média/grande), `CollectibleSpawner.prefabs` (moeda/joias) |
| Randomizar a posição no vetor | `RockSpawner.SpawnRock` (`spawnPoints[Random.Range]`), `CollectibleSpawner.SpawnCollectibles`, `SymbolSequencePuzzle.Shuffle` |
| Spawn de objetos por função | `RockSpawner.SpawnRock()`, `CollectibleSpawner.SpawnCollectibles()` |
| Scripts | 40 scripts de jogo + 9 do gerador, todos com documentação XML em cada função (o que faz, parâmetros, retorno) e comentários no corpo |
| Partículas: explosão e pedrinhas | `FX_Explosion` (clarão + poeira) e `FX_Pebbles` (cubos com gravidade e colisão), criadas pelo impacto das pedras |
| Fumaça | `FX_Smoke` (braseiros, fogueira, desabamento), `FX_DoorDust` (portas) |
| Som | `AudioManager` + 28 sons originais (`Assets/TreasureHunter/Audio`): passos, pulo, alavanca, porta, explosão, coleta, música e ambientes |
| Animação: parado, andando, correndo, pulando, caindo | `CharacterFactory` gera os clipes `John_Parado/Andando/Correndo/Pulando/Caindo` (+ `Empurrando`) e o Animator Controller (blend tree + transições); `PlayerController.UpdateAnimator` controla os parâmetros |
| UI: texto e botão (reiniciar) | `UIManager`: corações, missão, tesouros, ferramenta, avisos, legendas; botões **Reiniciar sala/jogo** na pausa, na derrota e no fim do nível |
| Tag | `GameTags` (Player, Pushable, Hazard, Collectible, Artifact, Rock, Ground), usadas com `CompareTag` |
| 70 assets de categorias diferentes | 73 assets distintos na cena do Nível 1 (70 + terreno, água do lago e ponte), em 11 categorias, e centenas de instâncias. O gerador grava a contagem real em `Docs/Inventario_Nivel1.md` |

---

## 5. Assets do nível (70+)

Ao gerar o nível, o arquivo `Assets/TreasureHunter/Docs/Inventario_Nivel1.md` é criado com a contagem por categoria
(Arquitetura, Vegetação, Props, Tesouros, Mecanismos, Iluminação, Armadilhas, Inscrições, Personagem, Efeitos).
Dá para copiar essa tabela direto para o GDD.

### Assets extras (Cristais Luminosos e Teia de Aranha)

Dois assets decorativos completam os 70 distintos:

- **Cristais Luminosos** (Iluminação): 4 aglomerados, no nicho secreto da Sala 1, em dois cantos da Sala 3 e atrás da divisória da Sala 4.
- **Teia de Aranha** (Props): 5 teias nas paredes, perto do teto.

Em um projeto que já foi gerado, use **Treasure Hunter → Adicionar assets extras ao Nível 1 (sem regerar)**. Esse menu:

- cria só esses 2 prefabs;
- coloca as instâncias na cena `Level1`;
- atualiza o inventário.

Ele não recria nada do que já existe e pode ser rodado mais de uma vez sem duplicar. O gerador completo também já inclui esses assets.

### Trocar pelos assets da Asset Store (opcional)

Os prefabs ficam em `Assets/TreasureHunter/Generated/Prefabs`. Para usar um modelo da Asset Store:

1. Abra o prefab gerado e coloque o modelo novo como filho.
2. Desative o `MeshRenderer` voxel original.
3. Mantenha os componentes de script e os colisores.

**Importante:** se rodar o gerador de novo depois disso, essas edições se perdem. Façam as trocas só depois da versão final do nível.
Lembrem também da OBS 2 do PDF: validar com o professor quais assets de terceiros podem ser usados.

---

## 6. Ajustes sugeridos no GDD

- **Câmera:** o GDD (4.4.1) fala em 1ª pessoa. O nível usa **3ª pessoa**, para que as animações pedidas na avaliação apareçam.
- **Versão do Unity:** em 4.6.2 está "6000.80.f1". O correto é **6000.0.8f1**.
- **Nível 1:** a seção 4.3.1 já descreve as 4 salas, e a implementação segue esse texto. Vale acrescentar ao GDD:
  - o Corredor das Pedras;
  - o tronco giratório da Sala 4;
  - o mapa (`Docs/Mapa_Nivel1.png`);
  - o inventário de assets;
  - a tabela de requisitos acima.

---

## 7. Estrutura

```
Assets/TreasureHunter/
├── Scripts/        (jogo: Core, Player, Interaction, Hazards, Collectibles, Effects, UI)
├── Editor/         (gerador do nível: menu "Treasure Hunter")
├── Audio/          (28 sons .wav originais)
├── Docs/           (mapa e inventário de assets)
├── Generated/      (criado pelo gerador: texturas, materiais, malhas, prefabs, animações)
└── Scenes/         (criado pelo gerador: MainMenu e Level1)
```

---

## 8. Se algo der errado

- **Erros vermelhos no Console logo ao abrir:** confirmem a versão (6000.0.8f1) e o template Universal 3D. O projeto usa o pacote *Input System*, que já vem nesse template.
- **Materiais rosa:** o projeto não está usando o URP. Criem o projeto pelo template Universal 3D.
- **O menu "Treasure Hunter" não aparece:** existe algum erro de compilação no Console. Ele precisa ser resolvido primeiro.
- Mandem o texto do erro do Console para corrigirmos.
