# Treasure Hunter — Nível 1: A Entrada do Templo · Nível 2: As Catacumbas de Añang

Projeto Unity das entregas do 1º e do 2º bimestre (Game Development — UVV 2026/2).
**Novo: Nível 2 (combate)** — veja a seção [9. Nível 2](#9-nível-2--as-catacumbas-de-añang-combate).
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

No Nível 2 a tocha dá lugar às armas (tabela completa na seção 9.2).

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
│                        Combat e Enemies — estes dois são do Nível 2)
├── Editor/             (gerador: menu "Treasure Hunter"; Editor/Nivel2 = gerador do Nível 2)
├── Audio/              (28 sons do Nível 1) e Audio/Nivel2 (41 sons do Nível 2)
├── Docs/               (mapas e inventários de assets dos dois níveis)
├── Generated/          (criado pelo gerador do Nível 1)
├── Generated_Nivel2/   (criado pelo gerador do Nível 2: texturas, mapas normais, materiais, malhas,
│                        prefabs, animações, máscara de animação e configuração de iluminação)
└── Scenes/             (MainMenu, Level1, Level2 e Level3 — esta é provisória)
```

---

## 8. Se algo der errado

- **Erros vermelhos no Console logo ao abrir:** confirmem a versão (6000.0.8f1) e o template Universal 3D. O projeto usa o pacote *Input System*, que já vem nesse template.
- **Materiais rosa:** o projeto não está usando o URP. Criem o projeto pelo template Universal 3D.
- **O menu "Treasure Hunter" não aparece:** existe algum erro de compilação no Console. Ele precisa ser resolvido primeiro.
- **Nível 2: esqueletos parados ou aviso "Failed to create agent":** o NavMesh é gerado ao dar Play (objeto `Gerenciadores` → `NavMeshRuntimeBaker`). Confiram se o pacote *AI Navigation* está instalado (Window → Package Manager; vem no template).
- **Nível 2: tiros atravessando inimigos, câmera batendo nos esqueletos:** faltam as Layers `Enemy`, `Projectile` e `Interactable`. Rodem o gerador do Nível 2 de novo (ele cria as Layers) e façam commit da pasta `ProjectSettings`.
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
- Rodar **Construir Nível 1** de novo agora também regera o Nível 2 no final, porque os prefabs do Nível 2 reaproveitam efeitos, itens e o modelo de John do Nível 1.
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
| **Saída** | Vencer a Sala 3 abre a última grade. A tela final mostra inimigos derrotados, **disparos usados (x/25)**, precisão e se a faca foi encontrada, com os botões **Próximo nível**, **Reiniciar jogo** e **Menu principal**. O Nível 3 ainda é uma tela provisória. | — |

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
