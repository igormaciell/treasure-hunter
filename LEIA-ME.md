# Treasure Hunter — Nível 1: A Entrada do Templo · Nível 2: As Catacumbas de Añang · Nível 3: A Caverna de Cristal · Nível 4: As Galerias dos Ogros

Projeto Unity das entregas do 1º e do 2º bimestre (Game Development — UVV 2026/2).
**Novo: Nível 4 (furtividade + ogros)** — veja a seção [11. Nível 4](#11-nível-4--as-galerias-dos-ogros-furtividade).
Nível 3 (parkour): seção [10](#10-nível-3--a-caverna-de-cristal-parkour) · Nível 2 (combate): seção [9](#9-nível-2--as-catacumbas-de-añang-combate).
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
- O Nature Starter Kit 2 é de 2016 (pipeline Built-in). Depois de importar:
  1. apaguem a pasta `Assets/NatureStarterKit2/Editor` (scripts antigos de efeitos de câmera que não compilam no Unity 6 e impedem o Play);
  2. rodem **Treasure Hunter → Converter materiais dos pacotes da aula para URP**, para as árvores e arbustos não ficarem cor-de-rosa. O Render Pipeline Converter do Unity não converte esses materiais.

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

No Nível 2 a tocha dá lugar às armas (tabela completa na seção 9.2). No Nível 3 entram a escalada de bordas e o totem (**T**, seção 10.2). No Nível 4, agachar (**C** ou **Ctrl**), a tocha na mão esquerda (**F**) e as pedras de distração (**G**) (seção 11.2).

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
├── Scripts/            (jogo: Core, Player, Interaction, Hazards, Collectibles, Effects, UI,
│                        Combat e Enemies — Nível 2 —, Parkour — Nível 3 —, Stealth — Nível 4)
├── Editor/             (gerador: menu "Treasure Hunter"; Editor/Nivel2, Nivel3 e Nivel4 = geradores dos Níveis 2, 3 e 4)
├── Audio/              (28 sons do Nível 1), Audio/Nivel2 (41 sons), Audio/Nivel3 (19 sons) e Audio/Nivel4 (31 sons)
├── Docs/               (mapas e inventários de assets dos níveis)
├── Generated/          (criado pelo gerador do Nível 1)
├── Generated_Nivel2/   (criado pelo gerador do Nível 2: texturas, mapas normais, materiais, malhas,
│                        prefabs, animações, máscara de animação e configuração de iluminação)
├── Generated_Nivel3/   (criado pelo gerador do Nível 3: o mesmo, mais o terreno da caverna e as camadas de terreno)
├── Generated_Nivel4/   (criado pelo gerador do Nível 4: texturas, normal maps, materiais, prefabs, ogros, animações, ícones)
└── Scenes/             (MainMenu, Level1, Level2, Level3, Level4 e Level5 — esta é provisória)
```

---

## 8. Se algo der errado

- **Erros vermelhos no Console logo ao abrir:** confirmem a versão (6000.0.8f1) e o template Universal 3D. O projeto usa o pacote *Input System*, que já vem nesse template.
- **Materiais rosa:** o projeto não está usando o URP. Criem o projeto pelo template Universal 3D.
- **O menu "Treasure Hunter" não aparece:** existe algum erro de compilação no Console. Ele precisa ser resolvido primeiro.
- **Nível 2: esqueletos parados ou aviso "Failed to create agent":** o NavMesh é gerado ao dar Play (objeto `Gerenciadores` → `NavMeshRuntimeBaker`). Confiram se o pacote *AI Navigation* está instalado (Window → Package Manager; vem no template).
- **Nível 2: tiros atravessando inimigos, câmera batendo nos esqueletos:** faltam as Layers `Enemy`, `Projectile` e `Interactable`. Rodem o gerador do Nível 2 de novo (ele cria as Layers) e façam commit da pasta `ProjectSettings`.
- **Nível 3: o totem fica sempre vermelho ou a câmera "pula" ao passar perto do totem:** falta a Layer `Totem` (ou a Tag `Unstable`). Rodem o gerador do Nível 3 de novo e façam commit da pasta `ProjectSettings`.
- **Nível 3: terreno rosa ou sem textura:** o shader `Universal Render Pipeline/Terrain/Lit` não foi encontrado (projeto fora do URP). Com o template Universal 3D isso não acontece.
- **Nível 4:** veja a seção [11.7](#117-se-algo-der-errado-nível-4).
- **Todas as camadas de Layers ocupadas:** o gerador avisa no Console se não houver espaço para `Totem`; liberem uma Layer sem uso em *Project Settings → Tags and Layers*.
- Mandem o texto do erro do Console para corrigirmos.

---

## 9. Nível 2 — As Catacumbas de Añang (combate)

John desce às catacumbas sob o templo. Os escombros fecham a escada por onde ele veio, e o único caminho é atravessar três salas guardadas por esqueletos.
O nível apresenta o **combate**: um revólver com **apenas 25 balas para o nível inteiro**, coronhada, faca, pedras e armadilhas do cenário.

### 9.1 Como gerar (uma pessoa só)

O Nível 2 tem um gerador próprio. Ele **não regera nem altera os assets do Nível 1**: só cria a pasta `Generated_Nivel2` e as cenas novas.

1. `git pull`. Extraiam o pacote `Atualizacao_Nivel2.zip` **por cima da pasta do projeto** (substituir arquivos) e abram o Unity.
2. Esperem compilar (Console sem erros vermelhos).
3. Rodem **Treasure Hunter → Construir Nível 2 (combate)**. O Nível 1 precisa já ter sido gerado (no repositório ele já está). O gerador:
   - cria as **Tags** `Enemy`, `Projectile`, `Weapon`, `Destructible` e as **Layers** `Enemy`, `Projectile`, `Interactable` (em `ProjectSettings`);
   - cria texturas com **mapas normais**, materiais, partículas, 78 prefabs, animações, a cena `Level2` e a cena provisória `Level3`;
   - acrescenta o botão **Próximo nível** na tela final do Nível 1 e o botão **Nível 2** no menu principal (só esses ajustes; o resto das cenas fica igual);
   - coloca `Level2` e `Level3` no *Build Settings*;
   - grava `Docs/Inventario_Nivel2.md` (contagem real de assets).
4. Testem: abram `MainMenu`, cliquem em **Nível 2** e joguem (ou abram a cena `Level2` e deem Play).
5. Commit e push de tudo: `Assets` (inclusive `Generated_Nivel2`, `Audio/Nivel2`, `Scenes`, `Docs`) **e `ProjectSettings`** (Tags, Layers e Build Settings). Os outros só dão `git pull`.

Observações:

- Rodar o gerador do Nível 2 de novo refaz só o que é do Nível 2. Edições feitas à mão na cena `Level2` ou em `Generated_Nivel2` se perdem.
- Rodar **Construir Nível 1** de novo agora também regera os Níveis 2, 3 e 4 no final, porque os prefabs deles reaproveitam efeitos, itens e o modelo de John do Nível 1.
- Rodar **Construir Nível 2** regera também o Nível 4 (se já existir), porque o John do Nível 4 é feito a partir do John armado do Nível 2.
- Rodar **Construir Nível 2** não mexe mais no Nível 3: a tela provisória `Level3` só é criada enquanto o Nível 3 de verdade não existe.
- **Iluminação assada (opcional):** **Treasure Hunter → Assar iluminação do Nível 2**. Leva alguns minutos; depois salvem a cena (Ctrl+S) e façam commit da pasta `Scenes/Level2` criada pelo Unity. Sem assar, o nível funciona normalmente: as luzes *Realtime* e *Mixed* já iluminam tudo. As luzes *Baked* e de *Área* só aparecem depois de assar.

### 9.2 Controles de combate

| Tecla | Ação |
|---|---|
| Clique esquerdo | atirar (revólver) · golpear (faca) |
| Botão direito (segurar) | mirar: câmera de ombro, zoom, menos dispersão, anda mais devagar |
| R | recarregar o tambor (só com balas da reserva) |
| V | coronhada (revólver, não gasta bala) · golpe de faca |
| G | arremessar pedra (atordoa e faz barulho) |
| 1 / 2, Q ou roda do mouse | trocar entre revólver e faca |
| E | pegar o revólver, pegar pedras, puxar alavanca, ler inscrições |

Tiro na **cabeça** causa o dobro de dano. Cada tiro faz barulho e acorda inimigos próximos.

### 9.3 Roteiro (mapa em `Docs/Mapa_Nivel2.png`)

| Trecho | O que acontece | Habilidade |
|---|---|---|
| **Corredor de entrada** | Escombros bloqueiam a escada atrás de John. Sob um foco de luz está a mesa com o **revólver** (E): 6 balas no tambor + 19 na reserva = **25 no nível inteiro** (não há munição para coletar). Pegar a arma abre a grade da cripta. Uma inscrição avisa sobre os guardiões. | — |
| **Sala 1 — Cripta dos Esquecidos** | Área segura com **boneco de treino** (atirar e dar coronhada) e **pilha de pedras**. Ao avançar, **2 esqueletos com faca** surgem do chão. Sarcófagos servem de cobertura, há **espinhos** no piso e um **barril explosivo**. Cada esqueleto tem **35% de chance de deixar a faca**, que vira um ataque corpo a corpo alternativo. Vencer abre a grade norte. | Corpo a Corpo, Pontaria, Recarga Tática |
| **Galeria dos Arqueiros (Sala 2)** | **3 arqueiros** mantêm distância (7 a 15 m), escolhem pontos de tiro com visão do jogador, tentam **flanquear** e trocam de posição quando John se esconde. As flechas fazem **curva** (gravidade) e se cravam nas paredes. Meias-paredes, pilares, altar e barricadas no meio da sala protegem **os dois lados**. A **Adaga Cerimonial** está no altar. | Cobertura |
| **Corredor da lâmina** | Uma **lâmina pendular** (Rigidbody + HingeJoint + torque) varre o corredor. É preciso passar no tempo certo. | — |
| **Salão do Guardião (Sala 3)** | **7 inimigos**: 3 esqueletos com faca que avançam, 2 arqueiros em **plataformas elevadas** e 2 **brutamontes** (machado, muita vida, olhos laranja) **dormindo junto à saída**: tiros os acordam. O jogador decide a ordem: eliminar as facas em silêncio (coronhada, faca, pedras, espinhos), derrubar os arqueiros e só então enfrentar os brutamontes. A **alavanca** perto da entrada derruba o **teto rachado** sobre o centro da sala. O **Amuleto de Osso** fica numa plataforma. | Improviso, Estratégia |
| **Saída** | Vencer a Sala 3 abre a última grade. A tela final mostra inimigos derrotados, **disparos usados (x/25)**, precisão e se a faca foi encontrada, com os botões **Próximo nível** (vai para o Nível 3), **Reiniciar jogo** e **Menu principal**. | — |

Ao morrer, **Voltar ao checkpoint** reinicia só a sala atual: inimigos, barris e armadilhas voltam, e o arsenal volta ao que John tinha ao entrar na sala. Assim, a conta das 25 balas nunca aumenta.

### 9.4 Requisitos do Nível 2 → onde estão

| Requisito | Implementação (arquivo → função) |
|---|---|
| 3 salas ligadas por passagens | `Level2Builder.BuildRoom1/2/3`, `BuildBladeCorridor`, `BuildExit`; grades `IronGate` entre as salas |
| Arma de longo alcance com **25 disparos** | `PlayerCombat` (`startingShots = 25`, tambor de 6). `GiveRevolver` limita a 25 e `FinishReload` só move balas da reserva para o tambor. Não existe munição extra no nível. `CombatRoom` guarda e restaura o arsenal ao reiniciar a sala |
| Controle de munição + UI | `CombatHUD`: tambor com 6 balas desenhadas, "tambor \| reserva", **"Disparos restantes: X / 25"**, barra de recarga, mira dinâmica (vermelha sobre inimigo), marcador de acerto |
| Colisão do projétil e dano | `Projectile` (Rigidbody, `SphereCastNonAlloc` + `OnTriggerEnter`), `IDamageable.TakeHit`, `Hitbox` (cabeça ×2), `DamageInfo` |
| Animações de disparo e recarga | Camada "Bracos" do Animator de John (`L2Characters.PlayerController`): `John2_Mirando`, `John2_Disparo`, `John2_Recarga`, `John2_Coronhada`, `John2_Faca_Golpe`, `John2_Arremesso` |
| Partículas de impacto | `FX2_Faiscas`, `FX2_Poeira_de_Impacto`, `FX2_Lascas_de_Osso`, `FX_Pebbles`, clarão e fumaça do disparo |
| Sons de disparo, impacto e morte | `CombatAudio` + `revolver_shot`, `bullet_ricochet_1/2`, `bullet_bone_hit`, `skeleton_death_1/2` e mais 35 sons em `Audio/Nivel2` |
| Sala 1: 2 inimigos com faca, NavMesh, animações | `MeleeEnemy` + `NavMeshAgent`; clipes `Esqueleto_Levantando/Parado/Andando/Correndo/Ataque_Faca/Dano/Morte` |
| Faca com **35%** de chance | `EnemyLoot.dropChance = 0.35` (`TryDrop`: `Random.value < 0.35`), prefab `Faca_Coletavel`; `PlayerCombat.TryMelee(true)` |
| Sala 2: 3 arqueiros, flechas com trajetória, cobertura | `RangedEnemy` (`BallisticVelocity`, pontos de tiro com pontuação por distância, visão e flanco, `Retreat`, troca de posição quando perde a visão); `Flecha` com gravidade e `stickOnImpact` |
| Sala 3: 7 inimigos mistos | `CombatRoom.groups`: 3 facas (alertas), 2 arqueiros (plataformas), 2 brutamontes (dormentes; acordam por visão ou barulho: `EnemyBase.MakeNoise`) |
| Passagem para o próximo nível | `CombatRoom.gatesOnClear` → `IronGate` → `CombatLevelExit` → `GameManager.LoadNextLevel` (cena `Level3`) |
| Collider / Rigidbody | Projéteis, pedras, destroços, pedregulhos, lâmina, caixotes, barris, esqueletos (cinemáticos) |
| Translate | `IronGate` (grades sobem e descem), `SpikeTrap` (espinhos) |
| AddForce | `Projectile.Launch`, `ThrownStone.Launch`, destroços de `Destructible`, `AddExplosionForce` do barril, `CrushingRock` |
| Rotação / Torque | `PendulumTrap` (`AddTorque` + HingeJoint), `ThrownStone` e destroços (`AddTorque`), `TrapLever`, `TrainingDummy`, `Spinner` da faca |
| NavMesh | `NavMeshRuntimeBaker` (gera o NavMesh ao iniciar), `NavMeshAgent` nos inimigos, `NavMeshObstacle` com *carving* nas grades, caixotes e barris |
| Scripts reutilizáveis | `EnemyBase` (abstrata) → `MeleeEnemy` / `RangedEnemy`; `Projectile` serve para balas e flechas; `Destructible` para caixote, barril e urna; interfaces `IDamageable` e `IResettable` |
| Vetores de prefabs + escolha aleatória | `CombatRoom.SpawnGroup.variants` (2 variações por tipo de esqueleto) com pontos embaralhados; `RangedEnemy.arrowPrefabs`; `Destructible.debrisPrefabs/lootPrefabs`; `CeilingCollapseTrap.rockPrefabs` + `dropPoints`; `EnemyLoot.dropPrefabs`; efeitos de `Projectile` |
| Spawn por funções | `CombatRoom.SpawnEnemy()`, `CeilingCollapseTrap.SpawnRock()`, `EnemyLoot.TryDrop()`, `Destructible.Break()`, `Projectile.SpawnEffects()` |
| Tags e Layers | Tags `Enemy`, `Projectile`, `Weapon`, `Destructible` (+ as do Nível 1); Layers `Enemy`, `Projectile`, `Interactable` (+ `Ignore Raycast` no jogador). `CombatLayers.SetupCollisions` usa `Physics.IgnoreLayerCollision`; máscaras separadas para tiro, visão dos inimigos e câmera |
| Partículas: impacto, explosão, poeira, pedrinhas, fumaça | `FX2_Poeira_de_Impacto`, `FX2_Explosao_do_Barril` + `FX_Explosion`, `FX2_Poeira_de_Osso`, `FX2_Poeira_dos_Espinhos`, `FX_Pebbles`, `FX2_Fumaca_do_Disparo`, `FX_Smoke`; ambiente: `FX2_Nevoa_Rasteira`, `FX2_Gotas`, `FX2_Almas`, `FX2_Brasas` |
| Sons ambientes e efeitos | `dungeon_ambience` (loop), `music_level2` (loop) e 39 efeitos, todos sintetizados para o projeto |
| Animações | Esqueletos: surgir, parado, andar, correr, atacar (faca, arco, machado), dano, morte. John: parado, andar, correr, pular, cair, empurrar, dano + camada dos braços |
| Luzes Point, Area, Baked e Mixed | Point: tochas, lanterna do cinto de John (com sombra), olhos dos esqueletos. Mixed: candelabros, lustres, braseiro, focos das fendas no teto. Area (Rectangle, Baked): `Fenda_de_Luz` e `LuzDeArea_*`. Baked: luzes de preenchimento das salas. `LightingSettings` próprio, sondas de luz (`LightProbeGroup`) e geometria com UV2 para lightmap |
| UI com textos e botões; reiniciar | HUD de combate + missão, avisos, título da sala; botões **Reiniciar sala** e **Reiniciar jogo** (pausa), **Voltar ao checkpoint** (derrota), **Próximo nível / Reiniciar jogo / Menu principal** (fim do nível) |
| **70+ assets distintos** | `Docs/Inventario_Nivel2.md`, gerado com a contagem real (passa de 100: arquitetura, props, iluminação, armadilhas, armas, inimigos, efeitos, interface…) |

---

## 10. Nível 3 — A Caverna de Cristal (parkour)

Ao sair das catacumbas, John chega a uma caverna enorme, iluminada por cristais, com um abismo sem fundo no meio. Para atravessá-la ele precisa subir de plataforma em plataforma: rochas, pilares de cristal, estalagmites, uma estalactite caída, torres e muros de uma masmorra em ruínas.
O nível apresenta o **parkour** (saltos precisos, controle no ar e escalada de bordas) e o **Totem de Checkpoint**: um totem que o jogador coloca onde quiser e que vira o ponto de retorno quando ele cai no abismo.

### 10.1 Como gerar (uma pessoa só)

O Nível 3 tem um gerador próprio. Ele **não regera nem altera os assets dos Níveis 1 e 2**: só cria a pasta `Generated_Nivel3` e as cenas novas.

1. `git pull`. Extraiam o pacote `Atualizacao_Nivel3.zip` **por cima da pasta do projeto** (substituir arquivos) e abram o Unity.
2. Esperem compilar (Console sem erros vermelhos).
3. Rodem **Treasure Hunter → Construir Nível 3 (parkour)**. O Nível 1 precisa já ter sido gerado (no repositório ele já está). O gerador:
   - cria as **Tags** `Unstable`, `Crystal`, `Checkpoint`, `Totem` e a **Layer** `Totem` (e `Interactable`, se ainda não existir);
   - cria o **terreno** da caverna (Unity Terrain com o abismo aberto por *Terrain Holes*, 5 camadas de textura com **difusa + normal map**), texturas e materiais com normal maps, partículas, os prefabs, as animações de John, a cena `Level3` e a cena provisória `Level4` (só enquanto o Nível 4 de verdade não existe);
   - acrescenta o botão **Nível 3** no menu principal (o fim do Nível 2 já leva ao Nível 3);
   - coloca `Level3` e `Level4` no *Build Settings*;
   - grava `Docs/Inventario_Nivel3.md` (contagem real de assets).
4. Testem: abram `MainMenu`, cliquem em **Nível 3** e joguem (ou abram a cena `Level3` e deem Play).
5. Commit e push de tudo: `Assets` (inclusive `Generated_Nivel3`, `Audio/Nivel3`, `Scenes`, `Docs`) **e `ProjectSettings`** (Tags, Layers e Build Settings). Os outros só dão `git pull`.

Observações:

- Rodar o gerador do Nível 3 de novo refaz só o que é do Nível 3. Edições feitas à mão na cena `Level3` ou em `Generated_Nivel3` se perdem.
- **Iluminação assada (opcional):** **Treasure Hunter → Assar iluminação do Nível 3**. Depois salvem a cena (Ctrl+S) e façam commit da pasta `Scenes/Level3` criada pelo Unity. Sem assar, os cristais (luzes *Mixed* e *Realtime*) já iluminam a caverna; as luzes *Baked* e de *Área* só aparecem depois de assar.
- O totem coletado fica salvo no computador (PlayerPrefs). Para testar do zero: **Treasure Hunter → Apagar progresso salvo (totem)**. No Nível 3 ele sempre precisa ser coletado de novo (é parte do percurso).

### 10.2 Controles do parkour

| Tecla | Ação |
|---|---|
| W A S D | mover; no ar também dá para corrigir a direção (controle aéreo) |
| Shift | correr: necessário nos saltos longos (até ~6 m correndo, ~3 m andando) |
| Espaço | pular (segurar = mais alto). Dá para pular um instante depois de sair da borda |
| Pular contra uma borda alta | **agarrar e subir** sozinho (bordas de 0,6 m a 2,3 m acima dos pés) |
| E | tocar o cristal especial, pegar o totem, ler inscrições |
| **T (segurar)** | mostra onde o totem vai ficar: **verde = pode**, **vermelho = não pode** (o motivo aparece na tela) |
| Soltar T | coloca o totem; se ele já estiver no chão, **muda de lugar** |
| Botão direito (com T segurado) | cancela |

Durante o salto, um **círculo no chão** mostra onde John vai pousar: verde em superfície firme, vermelho quando embaixo só há abismo ou parede.

### 10.3 Roteiro (mapa em `Docs/Mapa_Nivel3.png`)

| Trecho | O que acontece | Habilidade |
|---|---|---|
| **Entrada** | Arco da masmorra (de onde John vem), inscrição e um **muro de treino** de 2,2 m para aprender a agarrar a borda sem risco. À frente, a borda do abismo. | — |
| **Trecho 1 — Primeiros Saltos** | 9 saltos curtos (1 a 1,75 m) subindo de 0 a 3 m: rochas, **pilares de cristal**, uma **caixa** em cima da plataforma, uma **torre** da masmorra (degrau de 1 m) e uma **plataforma móvel** para treinar o controle no ar. | — |
| **Trecho 2 — Santuário do Totem** | Platô seguro com uma **pedra de checkpoint**. O **cristal dourado** (E) racha três vezes e se parte: os estilhaços voam e o **Totem de Checkpoint** fica livre para ser pego. Três pilares ao leste servem para treinar: tentem colocar o totem no ar, numa parede ou numa ponta de cristal para ver o aviso vermelho. | Saltos Precisos, Totem de Checkpoint |
| **Trecho 3 — A Travessia** | **Sem checkpoints fixos**: o jogo pede para colocar o totem em S3a. Depois: escalar uma torre (2 m), **salto longo correndo** (3,7 m) até uma **plataforma frágil**, outra frágil, estalagmite, **disco giratório** (movido por torque), caixa como degrau, **ponte de estalactite** com **estalactites caindo** do teto e a **torre alta** (escalada). Segredo: **Olho de Cristal** num pilar afastado do disco (salto correndo de ~4 m). | Escalada de Bordas, Checkpoint Próprio, Equilíbrio |
| **Trecho 4 — A Escalada Final** | Pilares finos (1,5 m) subindo, **cristal instável** (estilhaça logo depois do pouso), **muro da masmorra** (escalada obrigatória), **elevador de cristal** e o salto até a borda da saída. Segredo: **Lágrima de Cristal**, alcançada do alto do elevador. | — |
| **Saída** | Portal de cristal → tela final com quedas, totens colocados, bordas escaladas e pousos, e o botão **Próximo nível** (Nível 4). | Mestre do Parkour |

Cair no abismo não tira vida: a tela escurece e John volta ao **último checkpoint**, que pode ser o início, a pedra do santuário ou o totem (vale o mais recente). Plataformas frágeis voltam alguns segundos depois de cair.

### 10.4 Totem de Checkpoint: regras de colocação

A validação é feita com Raycast e CheckCapsule (`TotemAbility.Validate`). O totem **não** pode ser colocado:

| Situação | Aviso na tela |
|---|---|
| Mirando o vazio (nenhuma superfície) | "Não dá para colocar o totem no ar." |
| Superfície com mais de 30° de inclinação (parede) | "Isso é uma parede: o totem precisa de chão plano." |
| Plataforma móvel, giratória, frágil ou instável (tag `Unstable`, `PlatformMotion`, `CrumblingPlatform`, Rigidbody dinâmico) | "Superfície instável: ela se move ou desmorona." |
| A mais de 5 m (horizontal) ou 3 m (altura) de John | "Longe demais: aproxime-se do lugar." |
| Base não apoiada por inteiro: 6 raios ao redor do ponto (borda, ponta de cristal, degrau) | "Chão irregular ou estreito demais." |
| Sem espaço para uma pessoa em pé | "Sem espaço livre acima." |

Também não dá para mirar o totem enquanto John está escalando uma borda. Colocar de novo **move** o mesmo totem (não cria outro). Ao cair, John reaparece ao lado dele.

### 10.5 Como usar o totem no Nível 5

O totem coletado fica salvo (`PlayerProgress.HasTotem`). No Nível 5, basta:

1. colocar no John do nível o componente `TotemAbility` com **`loadSavedProgress = true`**, `totemPrefab = Totem_de_Checkpoint` e `ghostPrefab = Totem_Fantasma` (prefabs em `Generated_Nivel3/Prefabs`). O `ParkourController` é opcional (escalada de bordas);
2. pôr na cena um `VoidZone` (ou chamar `VoidZone.Instance.Respawn()` onde o jogador puder cair) e o `ParkourHUD` (o `L3UI.BuildHUD` monta o painel do totem);
3. pronto: o `GameManager` já usa o totem como checkpoint (`SetCheckpoint`).

### 10.6 Requisitos do Nível 3 → onde estão

| Requisito | Implementação (arquivo → função) |
|---|---|
| Sala única enorme com paredes naturais | `Level3Builder.BuildCave` + `L3Terrain` (Unity Terrain 90 × 180 m, paredes subindo até o teto), paredões, teto, colunas naturais |
| Cristais com luz própria e partículas | `L3Props.CrystalPrefab`: luz Point (Mixed) + `FX3_Brilho_de_Cristal` + `CrystalGlow` (emissão pulsando via `MaterialPropertyBlock`; pisca e toca ao pousar) |
| Estalactites, estalagmites, plataformas e formações naturais | `Estalactite_P/M/G`, `Estalagmite_P/M/G`, `Estalactite_Caida` (ponte), `Estalagmite_Plataforma`, `Plataforma_Rochosa_P/M/G/Longa`, `Formacao_Colunar`, `Arco_Natural`, `Coluna_Natural` |
| Estruturas de masmorra (torres, caixas, paredes) | `Torre_da_Masmorra`, `Torre_Alta`, `Muro_da_Masmorra`, `Caixa_da_Masmorra`, `Caixa_Grande`, `Torre_Arruinada`, `Muro_Arruinado`, `Arco_da_Masmorra`… |
| Abismos profundos e Void | Buraco no terreno (`TerrainData.SetHoles`), `VoidZone` (gatilho 140 × 240 m em y ≈ −32) + altura de segurança em `ParkourController.killHeight`; `FX3_Nevoa_do_Abismo` |
| Terreno e materiais com difusa + normal map | `L3Terrain.Layer` (5 `TerrainLayer` com `diffuseTexture` + `normalMapTexture`), `L3Palette` (materiais com `_BumpMap`) |
| Saltos, pouso, queda e movimento aéreo | `PlayerController` (inalterado: pulo, *coyote time*, *jump buffer*, `airControl = 0.6`) + `ParkourController` (altura da queda, pouso normal/forte, queda no abismo) |
| Superfícies válidas de pouso | `LandingIndicator` (Raycast para baixo, verde/vermelho), `ParkourController.ProbeGround` (SphereCast) |
| Escalada de bordas | `ParkourController.TryLedgeGrab`: Raycast na parede em 3 alturas + Raycast do topo + `CheckCapsule` → `StartMantle` |
| Cristais, estalactites, paredes, caixas e torres como apoio | Todos os prefabs do percurso têm colisor de topo e bordas escaláveis |
| Totem: quebrar o cristal, coletar, colocar, validar, feedback, reposicionar, reaparecer | `TotemCrystal` (Translate tremendo, estilhaços com AddForce/AddTorque) → `TotemPickup` → `TotemAbility` (fantasma verde/vermelho, texto `[OK]`/`[X]`, sons) → `CheckpointTotem` → `VoidZone.Respawn` → `GameManager.RespawnPlayer` |
| Percurso em 4 trechos | `Level3Builder.BuildSection1/2/3/4` + `SectionTrigger` (títulos, missão, dicas e habilidades) |
| Collider / Rigidbody | Plataformas (cinemáticas), disco (Rigidbody com torque), plataformas frágeis (caem com AddForce/AddTorque), estilhaços, estalactites (`FallingRock`) |
| Translate / AddForce | Translate: tremor do cristal e das plataformas frágeis, totem subindo do chão. AddForce: estilhaços, queda das plataformas, impulso herdado da plataforma móvel ao pular |
| Rotação / Torque | `SpinningPlatform` (`AddTorque`), `CheckpointTotem` (cristal girando), `Spinner` dos cristais flutuantes, estilhaços (`AddTorque`) |
| Raycast em superfícies | Validação do totem, escalada, indicador de pouso, detecção do abismo |
| Vetores de prefabs + escolha aleatória | `TotemCrystal.shardPrefabs` (`Lasca_de_Cristal_A/B/C`), `RockSpawner.rockPrefabs` (`Estalactite_Cadente_P/M/G`), `CollectibleSpawner.prefabs` |
| Spawn por funções | `TotemAbility.SpawnTotem()`, `TotemCrystal.SpawnShards()`, `RockSpawner`, `ParkourUtil.SpawnAll()`, `CollectibleSpawner` |
| Tags e Layers | Tags `Unstable`, `Crystal`, `Checkpoint`, `Totem`; Layer `Totem`; máscaras `ParkourLayers.SurfaceMask` (validação) e `CameraMask` (câmera) |
| Partículas: brilho, quebra, poeira/pedrinhas, queda/fumaça | `FX3_Brilho_de_Cristal`, `FX3_Estilhacos_de_Cristal`, `FX3_Poeira_de_Pouso`, `FX3_Pedrinhas`, `FX3_Fumaca_de_Queda` e mais 8 (`FX3_Nevoa_do_Abismo`, `FX3_Esporos`, `FX3_Gotas`, `FX3_Rachadura`, `FX3_Desmoronamento`, `FX3_Ativacao_do_Totem`, `FX3_Reaparecimento`, `FX3_Coleta_do_Totem`) |
| Sons de pulo, pouso, quebra e totem | `jump` (Nível 1), `land_soft`, `land_hard`, `crystal_break`, `totem_pickup`, `totem_place`, `totem_invalid`, `totem_respawn` e mais 12 em `Audio/Nivel3` (inclusive ambiente e música em loop), todos sintetizados para o projeto |
| Animações | `John_Craft_Parkour.controller`: `John3_Parado`, `Andando`, `Correndo`, `Pulando`, `Caindo`, `Aterrissando`, `Aterrissagem_Forte`, `Escalando`, `Empurrando`, `Dano`, `Colocando_Totem` |
| Luzes Point, Area, Baked e Mixed | Point: cristais, tochas, lanterna de cristal no cinto de John (com sombra). Mixed: `Cristal_Grande`, `Lampiao_da_Masmorra`, `Braseiro_de_Cristal`, cristais suspensos. Area (Rectangle, Baked): `LuzDeArea_*`. Baked: `Preenchimento_*`. `LightingSettings` próprio e sondas de luz |
| UI com textos e botões; reiniciar | `ParkourHUD` (totem, aviso de colocação, checkpoint atual, quedas, alerta de queda, dicas) + pausa com **Reiniciar sala** / **Reiniciar jogo**, tela final com **Próximo nível / Reiniciar / Menu** |
| Progressão para o Nível 4 e totem no Nível 5 | `ParkourLevelExit` → `GameManager.LoadNextLevel` (cena `Level4`); `PlayerProgress` + `TotemAbility.loadSavedProgress` (seção 10.5) |
| **70+ assets distintos** | `Docs/Inventario_Nivel3.md`, gerado com a contagem real (≈ 90 prefabs feitos para o Nível 3, fora texturas, normal maps, materiais, camadas de terreno, animações e sons) |

---

## 11. Nível 4 — As Galerias dos Ogros (furtividade)

Saindo da caverna de cristal, John desce por uma corda até galerias naturais escuras: paredes de rocha, túneis estreitos, cogumelos, raízes, escombros e restos de uma antiga mina. Lá embaixo vivem **três ogros enormes**. Um golpe de ogro mata, e as poucas balas do revólver só fazem um ogro **cambalear**.
O nível apresenta a **furtividade**: agachar, ficar nas sombras, controlar o barulho, observar as patrulhas e escolher o momento de cruzar. Lutar não é uma opção.

### 11.1 Como gerar (uma pessoa só)

O Nível 4 tem um gerador próprio. Ele **não regera nem altera os assets dos Níveis 1, 2 e 3**: só cria a pasta `Generated_Nivel4` e as cenas novas.

1. `git pull`. Extraiam o pacote `Atualizacao_Nivel4.zip` **por cima da pasta do projeto** (substituir arquivos) e abram o Unity.
2. Esperem compilar (Console sem erros vermelhos).
3. Rodem **Treasure Hunter → Construir Nível 4 (furtividade)**. Os **Níveis 1 e 2 precisam já ter sido gerados** (no repositório eles já estão): John, o revólver e as pedras do Nível 4 vêm do John armado do Nível 2. O gerador:
   - cria as **Tags** `Ogre`, `Cover`, `HidingSpot`, `SafeZone` (e as de combate e as Layers `Enemy`, `Projectile`, `Interactable`, se ainda não existirem);
   - cria texturas com **mapas normais**, materiais, 18 partículas, os prefabs (rochas, escombros, caixas, mina, acampamento, vegetação, luzes), os **3 ogros** com animações e o *Animator Controller*, o John furtivo (agachado e golpeado), a cena `Level4` e a cena provisória `Level5`;
   - acrescenta o botão **Nível 4** no menu principal (o fim do Nível 3 já leva ao Nível 4);
   - coloca `Level4` e `Level5` no *Build Settings*;
   - grava `Docs/Inventario_Nivel4.md` (contagem real de assets).
4. Testem: abram `MainMenu`, cliquem em **Nível 4** e joguem (ou abram a cena `Level4` e deem Play).
5. Commit e push de tudo: `Assets` (inclusive `Generated_Nivel4`, `Audio/Nivel4`, `Scenes`, `Docs`) **e `ProjectSettings`** (Tags, Layers e Build Settings). Os outros só dão `git pull`.

Observações:

- Rodar o gerador do Nível 4 de novo refaz só o que é do Nível 4. Edições feitas à mão na cena `Level4` ou em `Generated_Nivel4` se perdem.
- Rodar **Construir Nível 2** (ou o Nível 1, que regera o 2) agora regera também o Nível 4 no final, se ele já existir: o John do Nível 4 é feito a partir do John do Nível 2.
- Rodar **Construir Nível 3** não mexe mais no Nível 4: a tela provisória `Level4` só é criada enquanto o Nível 4 de verdade não existe. O mesmo vale para a `Level5` provisória.
- **Iluminação assada (opcional):** **Treasure Hunter → Assar iluminação do Nível 4 (opcional)**. Depois salvem a cena (Ctrl+S) e façam commit da pasta `Scenes/Level4` criada pelo Unity. Sem assar, o nível funciona: tochas, braseiros, fogueira e fungos (*Realtime* e *Mixed*) já iluminam as galerias. As luzes *Baked* e de *Área* (fendas de luz no teto) só aparecem depois de assar.
- O NavMesh é gerado ao dar Play (`Gerenciadores` → `NavMeshRuntimeBaker`) com o tamanho dos ogros (raio 0,65 m, altura 3 m). Por isso as fendas baixas, os nichos e a fenda estreita ficam fora do NavMesh e **os ogros não entram neles**.

### 11.2 Controles da furtividade

| Tecla | Ação |
|---|---|
| **C** | agachar / levantar (alterna) |
| **Ctrl esquerdo (segurar)** | agachado enquanto estiver segurando |
| Shift ou Espaço (agachado) | levanta (se houver espaço acima). Agachado não dá para correr nem pular |
| **F** | acender / apagar a tocha da mão esquerda (começa apagada) |
| E | pegar pedras, **apagar tochas e braseiros dos ogros**, ler inscrições |
| **G** | arremessar pedra (distração: o ogro vai investigar o ponto do impacto) |
| Clique esquerdo / botão direito / R | atirar / mirar / recarregar (revólver com **6 balas no nível inteiro**) |
| V | coronhada (num ogro, só o irrita) |
| Tab · Esc | Diário de Habilidades · pausa |

O HUD da furtividade mostra:

- o **olho** de visibilidade: SEGURO, ESCONDIDO, NAS SOMBRAS, VISÍVEL ou ILUMINADO;
- a postura, o estado da tocha e o **medidor de ruído** do último som de John;
- a lista dos ogros com o estado de cada um;
- **setas** em volta do centro da tela apontando para os ogros alertas (até 30 m);
- uma vinheta e o **batimento cardíaco**, que aceleram quando um ogro está desconfiado ou perseguindo.

Sobre cada ogro aparece uma **barra de percepção**, um **"?" amarelo** (desconfiado) ou um **"!" vermelho** (viu John).

### 11.3 Regras da furtividade

**Visão dos ogros** (`VisionSensor`):

- Cone com ângulo e distância próprios de cada ogro. O centro do cone (30°) vê melhor que a visão periférica.
- Raycasts (cabeça, peito e quadril de John) contra o cenário: rochas, caixas e paredes **bloqueiam a visão**. Atrás de uma rocha baixa, só quem está agachado some.
- A percepção não é instantânea: uma barra enche conforme a **visibilidade** de John (luz × postura × movimento × esconderijo), a distância e o quanto do corpo está à mostra.
- 30% da barra = **desconfiado**; 100% = **detectado**. A barra esvazia aos poucos quando John sai de vista.
- A menos de 2,2 m o ogro percebe John mesmo no escuro.
- **Tocha acesa**: John fica totalmente iluminado e o alcance da visão do ogro aumenta 70%.

**Luz e esconderijos** (`PlayerStealth.UpdateExposure`):

- A visibilidade soma as luzes próximas (tochas, braseiros, fogueira, fungos, fenda de luz, o lampião de Brann). Uma luz atrás de uma rocha conta pouco.
- **Sombras** densas reduzem a visibilidade.
- **Cogumelos altos e samambaias** escondem quase tudo de quem está agachado (12% da visibilidade).
- **Nichos de teto baixo, fendas e o vão sob a laje caída** são **áreas seguras**: o ogro não entra; se chegar na borda, bufa e desiste.

**Ruído** (`NoiseSystem` + `HearingSensor`), com alcance aproximado:

| Som | Alcance |
|---|---|
| passo agachado | 0,8 m |
| passo andando | 4 m |
| passo correndo | 10 m |
| pulo / pouso | 4,5 m / 5 m ou mais (cresce com a queda) |
| **cascalho e ossos no chão** | ×2,2 andando · ×3 agachado |
| acender ou apagar a tocha · apagar uma chama | 2,5 a 4 m |
| pedra arremessada (no ponto do impacto) | 9 m |
| **disparo** | 26 m: revela a origem na hora e todos os ogros que ouvem **começam a perseguir** |

Paredes no caminho abafam o som (alcance × 0,55; o disparo quase não é abafado). Passos e pousos deixam o ogro **desconfiado** e ele vai investigar um ponto **aproximado**, com erro maior quanto mais longe estiver. Uma pedra o leva ao **ponto exato** do impacto.

**Estados dos ogros** (`OgreAI`). As transições são graduais: o ogro para, vira a cabeça, fareja e rosna antes de mudar de comportamento.

| Estado | Comportamento |
|---|---|
| **Patrulhando** | Segue o trajeto (circuito ou ida e volta), parando em cada ponto e olhando para uma direção definida. |
| **Suspeitando** | Para e encara a direção do som ou do vulto ("?" amarelo). Se a barra encher, persegue; se nada acontecer, investiga. |
| **Investigando** | Anda até o ponto suspeito, olha em volta e fareja. |
| **Perseguindo** | Ruge ("!" vermelho) e corre atrás de John, indo até a última posição vista. Perde John depois de alguns segundos sem vê-lo. |
| **Retornando** | Resmunga e volta ao ponto mais próximo do trajeto. |
| Atacando / Cambaleando | Golpe de clava (um golpe mata). Uma bala faz o ogro cambalear 1,2 s, e depois ele persegue o atirador. |

**Morte e checkpoint:** o golpe do ogro derruba John (animação "Golpeado") e aparece a tela **"O ogro te esmagou!"**. **Voltar ao checkpoint** leva ao **início do percurso atual**, com os ogros de volta às rotas, as tochas reacesas, as pedras repostas e o revólver como estava ao entrar no percurso.

### 11.4 Os três ogros

| Ogro | Visual | Visão | Audição | Patrulha / perseguição |
|---|---|---|---|---|
| **Grukk** | pele verde, clava, olhos amarelos | 13 m · 100° | ×1,15 | 1,35 / 4,0 m/s |
| **Morg** | pele cinza, armadura de ossos | 11,5 m · 90° | **×1,4** (ouve melhor) | 1,2 / 3,7 m/s |
| **Brann** | pele vermelha, martelo, olhos vermelhos e **lampião** | **14 m · 110°** | ×1,0 | 1,4 / 4,2 m/s |

Os ogros têm **vida enorme** (nunca chega a zero) e não são inimigos para enfrentar. John correndo (6,2 m/s) é mais rápido que eles, mas correr faz barulho. O lampião de Brann ilumina quem chega perto dele.
São **exatamente três ogros no nível**: ao entrar em cada percurso, os mesmos três recebem novos trajetos (`StealthSector`). Quem não está no percurso fica desativado.

### 11.5 Roteiro (mapa em `Docs/Mapa_Nivel4.png`)

| Trecho | O que acontece | Habilidade |
|---|---|---|
| **Entrada** | John desce pela corda. O **diário de um minerador** explica como os ogros caçam. Ali dá para treinar tudo sem risco: agachar atrás de uma rocha baixa, acender e apagar a tocha, apagar a **tocha dos ogros** na parede (E) e pegar pedras. A **fenda baixa** (teto 1,35 m) só passa agachado. | Agachar, Senhor das Sombras |
| **Percurso 1 — Primeiro Contato** (1 ogro) | **Grukk** faz um circuito em volta de uma formação rochosa central, com pausas em cada canto. O lado oeste é uma faixa de **sombra** com caixas, escombros e a **laje caída** (vão seguro embaixo), onde está o **Amuleto do Minerador**. O lado leste tem **cogumelos altos**, carrinho de mina e um **nicho baixo** seguro. Perto da saída: **cascalho e ossos** (barulhentos) e um **braseiro** que pode ser apagado. A saída é a fenda baixa do lado leste. | Distração (na primeira pedra que atrai o ogro) |
| **Percurso 2 — Observação** (2 ogros) | Uma parede de rocha divide a galeria. No lado **escuro** (oeste), **Morg** vai e volta. No lado **iluminado** (leste: fenda de luz, poça brilhante, fungos), **Grukk** dá voltas em torno de pilares e rochedos. Os lados se ligam por uma **passagem larga** (arco) e por uma **fenda estreita** onde só John cabe. Há dois **nichos seguros**; no do leste está o **Dente de Ogro Dourado**. A saída é a fenda baixa no noroeste, com ossos, cascalho e um braseiro no caminho. | Observação |
| **Percurso 3 — O Covil** (3 ogros) | No centro, a **fogueira dos ogros** com peles, trono, caldeirão, totens e ossos. **Brann** dá voltas em torno dela com o lampião. **Grukk** vai e volta de sul a norte, cortando o círculo de Brann. **Morg** vai e volta de leste a oeste diante da saída. Os trajetos **se cruzam**, então é preciso esperar o momento certo. Há três **nichos seguros** (sudoeste, noroeste e leste), uma faixa de sombra a oeste, cobertura alta (coluna, rochedos, pilhas de caixas) e **tochas perto da saída** que podem ser apagadas. | Travessia Calculada |
| **Saída das Galerias** | O túnel no nordeste leva ao Nível 5 (cena provisória). A tela final mostra artefatos, habilidades e tempo, e ainda: **vezes visto**, **golpes de ogro**, **disparos** e quantas vezes a tocha foi acesa. | Furtividade; **Fantasma** se nunca foi visto |

Dica para a apresentação: no Percurso 1, fiquem agachados na sombra do oeste e esperem Grukk passar. No Percurso 2, joguem uma pedra no lado leste para atrair Grukk e cruzem pela fenda estreita. No Percurso 3, apaguem a tocha do oeste e usem os nichos.

### 11.6 Requisitos do Nível 4 → onde estão

| Requisito | Implementação (arquivo → função) |
|---|---|
| Caverna natural escura, túneis estreitos, grandes formações | `Level4Builder.BuildStart/BuildCourse1/2/3` (paredes, tetos de 4,5 a 10 m, fendas de 1,35 m, nichos de 1,6 m); `DecorateRoom` (paredes irregulares, estalactites, estalagmites, raízes, líquen, musgo, pedregulhos); `Pilar_de_Rocha`, `Coluna_Grossa`, `Rochedo`, `Arco_de_Rocha`, `Laje_Caida` |
| Vegetação subterrânea, escombros, caixas e estruturas para se esconder | `Cogumelos_Altos`, `Samambaia_da_Caverna` (`HidingSpot`), `Escombros_P/M/G`, `Caixa_Velha(_Grande)`, `Pilha_de_Caixas_Velhas`, `Carrinho_de_Mina`, `Andaime_Quebrado` (tag `Cover`), fendas e nichos (`SafeZone`) |
| Pouca luz, sombras, pequenas fontes de luz | Luz ambiente quase zero e neblina preta (`SetupLighting`); `LightZone` nas sombras; tochas, braseiros, fogueira, fungos, cristais, poça e fendas de luz |
| Agachar: menos ruído, menos visível, mais lento | `PlayerStealth` (cápsula 1,8 → 1,15 m, `speedMultiplier 0,5`, passos 0,8 m de ruído, visibilidade × 0,5, câmera mais baixa, `CheckCapsule` para levantar); `John4_Agachado`, `John4_Agachado_Andando` |
| Visão: cone por ângulo e distância, Raycast, luz, tocha, obstáculos | `VisionSensor.Tick` (ângulo + distância + 3 `Linecast` + `PlayerStealth.VisibilityFactor` + `torchRangeMultiplier`); `PlayerStealth.UpdateExposure` (luzes da cena com `Linecast` de oclusão) |
| Estados de alerta com transições coerentes | `OgreAI`: Patrulhando, Suspeitando, Investigando, Perseguindo, Retornando (+ Atacando, Cambaleando), com tempo de reação, olhar em volta, memória da perseguição e rugido antes de correr; evento `StateChanged` para o HUD |
| Ruído: andar baixo, correr alto, pulo/pouso, disparo extremo, investigação aproximada | `NoiseSystem.Emit` (passos, pulo, pouso, tocha, pedras, disparos, explosões); `HearingSensor` (alcance × audição, oclusão por Raycast); `OgreAI.OnHeard` (ponto aproximado para passos, exato para pedras, perseguição imediata para disparos) |
| Exatamente 3 ogros, vida enorme, um golpe mata, lentos, visão reduzida, audição | `L4Ogres.BuildOgre` (Grukk, Morg, Brann); `OgreAI.maxHealth 600` (nunca zera), `Smash` → `PlayerStealth.Die`; velocidades e sentidos na seção 11.4; `StealthSector` reaproveita os 3 ogros em cada percurso |
| Animações dos ogros | `Ogro.controller`: `Ogro_Parado`, `Ogro_Andando`, `Ogro_Correndo` (blend de locomoção), `Ogro_Atacando`, `Ogro_Investigando`, `Ogro_Desconfiado`, `Ogro_Rugindo`, `Ogro_Cambaleando` |
| Tocha facilita a detecção; disparo revela a posição | `StealthTorch` (exposição máxima + alcance da visão × 1,7); `PlayerCombat` → `NoiseSystem.Emit(Gunshot, 26 m)` → `OgreAI.StartChase` |
| 3 percursos com dificuldade crescente | Entrada + Percurso 1 (1 ogro) + Percurso 2 (2 ogros, lados escuro e iluminado) + Percurso 3 (3 ogros, rotas cruzadas); `SectionTrigger` com dicas; `StealthSector` com checkpoint por percurso |
| NavMesh / NavMeshAgent | `NavMeshRuntimeBaker` (com `includeLayers` sem gatilhos nem inimigos) e `NavMeshAgent` dos ogros (raio 0,65, altura 3) |
| Máquina de estados | `OgreAI.Update` → `switch (CurrentState)` → `UpdatePatrol`, `UpdateSuspicious`, `UpdateInvestigate`, `UpdateChase`, `UpdateReturn`, `UpdateAttack`, `UpdateStagger`; entradas `EnterSuspicious`, `StartChase`… |
| Collider / Rigidbody | Ogros (cápsula + Rigidbody cinemático), John (CharacterController), pedras (`ThrownStone` com Rigidbody), gatilhos de esconderijo, sombra, área segura e chão barulhento |
| Translate / AddForce | Translate: ícones "?"/"!" flutuando sobre os ogros (`AlertIndicator.LateUpdate`). AddForce: pedras de distração (`ThrownStone.Launch`) e balas (`Projectile.Launch`) |
| Rotação | Ogros giram para os pontos de patrulha e para olhar em volta (`Quaternion.RotateTowards`); ícones de alerta viram para a câmera; braço da tocha (`StealthTorch.LateUpdate`) |
| Scripts reutilizáveis de percepção e patrulha | `VisionSensor`, `HearingSensor`, `PatrolRoute`, `AlertIndicator`, `NoiseSystem` (interface `INoiseListener`), `StealthVolume` → `HidingSpot` / `SafeZone` / `NoisyFloor` / `LightZone` |
| Vetores de prefabs + escolha aleatória | `StealthUtil.SpawnRandom` / `SpawnAll`: `OgreAI.stepEffects/smashEffects/alertEffects/suspiciousEffects/breathEffects`, `NoisyFloor.stepEffects`, `ExtinguishableLight.extinguishEffects`; sons sorteados (`StealthUtil.Pick`); `DecorateRoom` sorteia estalactites e estalagmites |
| Spawn por funções | `StealthUtil.SpawnAll()` / `SpawnRandom()` chamadas por `OgreAI.UpdateSteps`, `OgreAI.Smash`, `OgreAI.EnterSuspicious`, `OgreAI.StartChase`, `PlayerStealth.Die()`, `ExtinguishableLight.Apply()`; `StonePile` repõe as pedras |
| Tags e Layers | Tags `Ogre`, `Cover`, `HidingSpot`, `SafeZone` (+ as dos níveis anteriores); Layers `Enemy` (ogros), `Interactable`, `Ignore Raycast` (gatilhos e jogador); máscara de visão `StealthUtil.SightMask` |
| Partículas: poeira, fumaça, pedrinhas, investigação, alerta, movimento dos ogros, ambiente | `FX4_Poeira_do_Passo` (passos dos ogros), `FX4_Impacto_da_Clava`, `FX4_Pedrinhas`, `FX4_Bafo`, `FX4_Suspeita`, `FX4_Alerta`, `FX4_Fumaca`, `FX4_Fumaca_Apagada`, `FX4_Poeira_no_Ar`, `FX4_Nevoa_Rasteira`, `FX4_Gotas`, `FX4_Esporos`, `FX4_Pedrinhas_do_Teto`, `FX4_Fogueira`, `FX4_Brasas`, `FX4_Faiscas`, `FX4_Cascalho`, `FX4_Folhas` |
| Sons de passos, respiração, rosnados, alertas e ambiente | 31 sons em `Audio/Nivel4`, sintetizados para o projeto: `ogre_step_1/2`, `ogre_breath` (loop), `ogre_growl_1/2`, `ogre_suspicious_1/2`, `ogre_sniff`, `ogre_roar_1/2`, `ogre_attack`, `ogre_smash`, `ogre_stagger`, `ogre_frustrated`, `stinger_suspicious/spotted/lost`, `heartbeat` (loop), `gravel_step_1/2`, `bone_crunch_1/2`, `foliage_rustle`, `torch_ignite/extinguish`, `crouch_down`, `stand_up`, `player_crushed`, `sector_reached`, `cave_ambience_ogres` e `music_level4` (loops) |
| Luzes Point, Area, Baked e Mixed | Point: tochas e braseiros dos ogros, fungos, cristais, tocha de John, lampião de Brann. Mixed: fogueira dos ogros, preenchimento das fendas de luz. Area (Rectangle, Baked): `Fenda_de_Luz`. Baked: luzes de preenchimento. `LightingSettings` próprio e sondas de luz |
| UI com textos e botões; reiniciar | `StealthHUD` (olho, postura, tocha, ruído, ogros, setas, alerta, dicas) + HUD de combate; pausa com **Reiniciar sala** / **Reiniciar jogo**; derrota com **Voltar ao checkpoint** / **Reiniciar jogo**; tela final com **Próximo nível** / **Reiniciar** / **Menu** |
| Saída para o Nível 5 | `StealthLevelExit` → `GameManager.LoadNextLevel` (cena `Level5`, provisória) |
| **70+ assets distintos** | `Docs/Inventario_Nivel4.md`, gerado com a contagem real: ≈ 65 prefabs feitos para o Nível 4 (cenário, 3 ogros, John furtivo), 18 partículas e 31 sons, fora texturas, normal maps, materiais, ícones da interface e animações |

### 11.7 Se algo der errado (Nível 4)

- **Aviso "Gere primeiro: Construir Nível 1 e Construir Nível 2":** confiram se `Generated` e `Generated_Nivel2` estão no repositório (`git pull`) ou rodem os geradores dos Níveis 1 e 2.
- **Ogros parados ou "Failed to create agent":** confiram o pacote *AI Navigation* (vem no template). O NavMesh é gerado ao dar Play.
- **Ogros que enxergam através das rochas ou tiros que atravessam os ogros:** faltam as Layers `Enemy`/`Interactable`. Rodem o gerador do Nível 4 de novo e façam commit da pasta `ProjectSettings`.
- **Segurar Ctrl dispara atalhos do editor durante o Play (Ctrl+S, Ctrl+D):** usem **C** para agachar no editor. No jogo compilado isso não acontece.
