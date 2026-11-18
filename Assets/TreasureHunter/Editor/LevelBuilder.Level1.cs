using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Conteúdo do Nível 1 — A Entrada do Templo:
    /// introdução na floresta e no templo da superfície (armadilha do ídolo) e as quatro salas do subterrâneo
    /// (Câmara das Raízes, Câmara do Peso + Corredor das Pedras, Câmara dos Símbolos e Portão do Templo).
    /// </summary>
    public static partial class LevelBuilder
    {
        /// <summary>Altura do piso do complexo subterrâneo.</summary>
        private const float Y0 = -40f;

        private static GameManager gm;
        private static UIManager ui;
        private static GameObject player;

        /// <summary>
        /// Constrói e salva a cena completa do Nível 1.
        /// </summary>
        private static void BuildLevel1Scene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Light sun = SetupLighting();
            BuildManagersAndPlayer(sun);

            Progress("Nível 1: floresta e templo da superfície", 0.7f);
            BuildSurface();
            Progress("Nível 1: Sala 1 e Sala 2", 0.78f);
            BuildRoom1();
            BuildRoom2AndRockCorridor();
            Progress("Nível 1: Sala 3 e Sala 4", 0.86f);
            BuildRoom3();
            BuildRoom4AndExit();
            PlaceExtras();

            Group("Seguranca");
            GameObject kill = TriggerBox("ZonaDeQueda", new Vector3(0, Y0 - 25f, 20f), new Vector3(400f, 4f, 400f));
            kill.AddComponent<KillZone>();

            Progress("Nível 1: inventário de assets", 0.95f);
            WriteInventory();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Level1ScenePath);
        }

        /// <summary>
        /// Cria o sol, o céu procedural e a luz ambiente inicial (floresta).
        /// </summary>
        /// <returns>A luz direcional do sol.</returns>
        private static Light SetupLighting()
        {
            GameObject sunGo = new GameObject("Sol");
            sunGo.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.82f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            RenderSettings.skybox = MaterialFactory.Skybox();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.5f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.62f, 0.7f, 0.64f);
            RenderSettings.fogDensity = 0.012f;
            return sun;
        }

        /// <summary>
        /// Cria os gerenciadores (jogo, habilidades, áudio, atmosfera), o jogador, a câmera e a interface,
        /// ligando as referências entre eles.
        /// </summary>
        /// <param name="sun">Luz do sol (desligada no subterrâneo).</param>
        private static void BuildManagersAndPlayer(Light sun)
        {
            GameObject managers = new GameObject("Gerenciadores");
            gm = managers.AddComponent<GameManager>();
            SkillManager sm = managers.AddComponent<SkillManager>();
            sm.skills = SkillManager.CreateLevel1Skills();

            AudioManager am = managers.AddComponent<AudioManager>();
            am.footsteps = new[] { BuilderCore.Clip("footstep_1"), BuilderCore.Clip("footstep_2"), BuilderCore.Clip("footstep_3"), BuilderCore.Clip("footstep_4") };
            am.jump = BuilderCore.Clip("jump");
            am.land = BuilderCore.Clip("land");
            am.hurt = BuilderCore.Clip("hurt");
            am.torchWhoosh = BuilderCore.Clip("torch_whoosh");
            am.lever = BuilderCore.Clip("lever");
            am.doorGrind = BuilderCore.Clip("door_grind");
            am.plateClick = BuilderCore.Clip("plate_click");
            am.buttonPress = BuilderCore.Clip("button_press");
            am.puzzleWrong = BuilderCore.Clip("puzzle_wrong");
            am.puzzleSolved = BuilderCore.Clip("puzzle_solved");
            am.crateScrape = BuilderCore.Clip("crate_scrape");
            am.fireIgnite = BuilderCore.Clip("fire_ignite");
            am.rockExplosion = BuilderCore.Clip("rock_explosion");
            am.rumble = BuilderCore.Clip("rumble");
            am.coin = BuilderCore.Clip("coin");
            am.gem = BuilderCore.Clip("gem");
            am.artifact = BuilderCore.Clip("artifact");
            am.skillUnlock = BuilderCore.Clip("skill_unlock");
            am.uiClick = BuilderCore.Clip("ui_click");
            am.levelComplete = BuilderCore.Clip("level_complete");
            am.forestAmbience = BuilderCore.Clip("forest_ambience");
            am.caveAmbience = BuilderCore.Clip("cave_ambience");
            am.music = BuilderCore.Clip("music_level1");

            AtmosphereController atmo = managers.AddComponent<AtmosphereController>();
            atmo.sun = sun;
            atmo.profiles = new[]
            {
                new AtmosphereProfile { name = "Floresta", ambientColor = new Color(0.5f, 0.55f, 0.5f), fogColor = new Color(0.62f, 0.7f, 0.64f), fogDensity = 0.012f, sunEnabled = true, ambience = am.forestAmbience },
                new AtmosphereProfile { name = "Subterrâneo", ambientColor = new Color(0.11f, 0.1f, 0.09f), fogColor = new Color(0.03f, 0.025f, 0.02f), fogDensity = 0.03f, sunEnabled = false, ambience = am.caveAmbience }
            };

            // Jogador no acampamento, olhando para o templo (norte).
            player = (GameObject)PrefabUtility.InstantiatePrefab(CharacterFactory.PlayerPrefab);
            player.transform.position = new Vector3(0f, 0.05f, -60f);

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 2f, -64.5f);
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 250f;
            camGo.AddComponent<AudioListener>();
            ThirdPersonCamera tpc = camGo.AddComponent<ThirdPersonCamera>();
            tpc.target = player.transform;

            PlayerController pc = player.GetComponent<PlayerController>();
            pc.cameraTransform = camGo.transform;

            ui = UIFactory.BuildLevelUI();
            gm.player = pc;
            gm.playerHealth = player.GetComponent<PlayerHealth>();
            gm.ui = ui;
            gm.totalArtifacts = 3;
            gm.startObjective = "Siga a trilha até as ruínas do templo de Añang.";
        }

        // ================================================================== Superfície

        /// <summary>
        /// Superfície: terreno (Unity Terrain) com floresta, lago, ravina e montanhas, acampamento do explorador,
        /// escadaria, templo, armadilha do ídolo e poço da queda.
        /// </summary>
        private static void BuildSurface()
        {
            Transform surface = Group("Superficie");

            // Terreno feito com o Unity Terrain (passos da aula de terreno). Ele tem um buraco sobre o poço do templo.
            Progress("Nível 1: terreno (alturas, texturas, grama e árvores)", 0.68f);
            TerrainBuilder.Build(surface);

            // Limites da área jogável (as montanhas nas bordas fazem a moldura visual).
            Group("Limites", surface);
            InvisibleWall(new Vector3(-40, -10, -85), new Vector3(-38, 20, 2));
            InvisibleWall(new Vector3(38, -10, -85), new Vector3(40, 20, 2));
            InvisibleWall(new Vector3(-40, -10, -86), new Vector3(40, 20, -84));
            InvisibleWall(new Vector3(-40, -10, 0), new Vector3(40, 20, 2));

            BuildForest(surface);
            BuildCamp(surface);
            BuildTemple(surface);
        }

        /// <summary>
        /// Altura do terreno num ponto (atalho para posicionar objetos em cima dele).
        /// </summary>
        /// <param name="x">X do mundo.</param>
        /// <param name="z">Z do mundo.</param>
        /// <returns>Posição com Y na altura do terreno.</returns>
        private static Vector3 OnGround(float x, float z)
        {
            return new Vector3(x, TerrainShape.Height(x, z), z);
        }

        /// <summary>
        /// Enfeites da floresta sobre o terreno: algumas árvores voxel como objetos (as demais são árvores do Terrain),
        /// arbustos, flores, cogumelos, pedras, troncos, ponte da ravina, margens do lago e tesouros escondidos.
        /// </summary>
        /// <param name="surface">Grupo pai.</param>
        private static void BuildForest(Transform surface)
        {
            Group("Floresta", surface);
            string[] trees = { "Arvore_Carvalho", "Arvore_Pinheiro", "Arvore_Tropical" };
            ScatterSurface(trees, 14, 2.2f);
            ScatterSurface(new[] { "Arbusto" }, 30, 1.2f);
            ScatterSurface(new[] { "Capim" }, 20, 0.3f);
            ScatterSurface(new[] { "Samambaia" }, 18, 0.5f);
            ScatterSurface(new[] { "Cogumelo_Vermelho", "Cogumelos_Marrons" }, 14, 0.3f);
            ScatterSurface(new[] { "Flor_Amarela", "Flor_Roxa" }, 18, 0.3f);
            ScatterSurface(new[] { "Musgo" }, 8, 0.8f);
            ScatterSurface(new[] { "Bloco_de_Pedra", "Entulho" }, 8, 1f);
            Place("Tronco_Caido", OnGround(-6f, -40f), 70f);
            Place("Tronco_Caido", OnGround(10f, -33f), -20f);
            Place("Tronco_Caido", OnGround(8f, -55f), 15f);
            Place("FX_Leaves", new Vector3(0f, 9f, -38f));

            // Margens do lago: pedras e musgo na beira da água.
            Vector2 lake = TerrainShape.LakeCenter;
            for (int i = 0; i < 7; i++)
            {
                float ang = i / 7f * Mathf.PI * 2f + 0.4f;
                float x = lake.x + Mathf.Cos(ang) * TerrainShape.LakeRadiusX * 1.12f;
                float z = lake.y + Mathf.Sin(ang) * TerrainShape.LakeRadiusZ * 1.12f;
                Place(i % 2 == 0 ? "Bloco_de_Pedra" : "Musgo", OnGround(x, z), ang * Mathf.Rad2Deg, 0.8f);
            }

            // Ponte sobre a ravina e um pequeno mirante com tesouro do outro lado.
            float bx = TerrainShape.RavineX(TerrainShape.BridgeZ);
            Place("Ponte_de_Madeira", new Vector3(bx, 0f, TerrainShape.BridgeZ), 0f);
            Place("Bau_de_Tesouro", OnGround(35.5f, TerrainShape.BridgeZ - 1.5f), -90f);
            Place("Pilha_de_Ouro", OnGround(35.6f, TerrainShape.BridgeZ + 1.2f), 20f);
            Place("Totem", OnGround(36.5f, TerrainShape.BridgeZ + 3.5f), -90f);

            Treasures("Tesouros_Floresta", 4,
                OnGround(-12.5f, -40f), OnGround(-30f, -52f), OnGround(34.5f, TerrainShape.BridgeZ), OnGround(-24f, -14f),
                OnGround(15f, -70f), OnGround(-16f, -75f));
        }

        /// <summary>
        /// Diz se um ponto está sobre a trilha principal, as trilhas secundárias ou a ponte.
        /// </summary>
        /// <param name="x">X do mundo.</param>
        /// <param name="z">Z do mundo.</param>
        /// <param name="margin">Folga em metros.</param>
        /// <returns>true se o ponto atrapalharia a passagem.</returns>
        private static bool OnPath(float x, float z, float margin)
        {
            Vector2 p = new Vector2(x, z);
            float bx = TerrainShape.RavineX(TerrainShape.BridgeZ);
            if (TerrainShape.SegmentDistance(p, new Vector2(0f, -50f), new Vector2(-11f, -47f)) < 1.6f + margin) return true;
            if (TerrainShape.SegmentDistance(p, new Vector2(0f, -38f), new Vector2(bx + 8f, TerrainShape.BridgeZ)) < 1.6f + margin) return true;
            return TerrainShape.RectDistance(x, z, bx - 8f, 38f, TerrainShape.BridgeZ - 2f, TerrainShape.BridgeZ + 2f) < margin;
        }

        /// <summary>
        /// Posiciona vários prefabs aleatoriamente na floresta, em cima do terreno, respeitando áreas livres
        /// (trilhas, acampamento, templo, lago, ravina e encostas íngremes).
        /// </summary>
        /// <param name="ids">Prefabs possíveis (sorteados).</param>
        /// <param name="count">Quantidade desejada.</param>
        /// <param name="radius">Raio aproximado do objeto (afasta das áreas livres).</param>
        private static void ScatterSurface(string[] ids, int count, float radius)
        {
            int placed = 0, tries = 0;
            while (placed < count && tries < count * 40)
            {
                tries++;
                float x = BuilderCore.Range(-36f, 36f);
                float z = BuilderCore.Range(-82f, -2f);
                // Áreas livres: trilha, acampamento, templo + escadaria, trilhas secundárias, lago e ravina.
                if (Mathf.Abs(x - TerrainShape.PathX(z)) < 3.2f + radius && z < -24f) continue;
                if (Vector2.Distance(new Vector2(x, z), new Vector2(0f, -60f)) < 8f + radius) continue;
                if (Mathf.Abs(x) < 11f + radius && z > -32f) continue;
                if (OnPath(x, z, radius)) continue;
                if (!TerrainShape.IsGoodGround(x, z, radius)) continue;
                string id = ids[BuilderCore.Rng.Next(ids.Length)];
                float scale = id.StartsWith("Arvore") ? BuilderCore.Range(0.85f, 1.35f) : BuilderCore.Range(0.8f, 1.2f);
                Place(id, OnGround(x, z), BuilderCore.Range(0f, 360f), scale);
                placed++;
            }
        }

        /// <summary>
        /// Acampamento de John no começo da fase (tutorial de movimento).
        /// </summary>
        /// <param name="surface">Grupo pai.</param>
        private static void BuildCamp(Transform surface)
        {
            Group("Acampamento", surface);
            Place("Barraca", new Vector3(-4.5f, 0, -62f), 30f);
            Place("Fogueira", new Vector3(3f, 0, -61f));
            Place("Mesa_de_Campo", new Vector3(-3.5f, 0, -57.5f), 90f);
            Place("Diario_do_Explorador", new Vector3(-3.5f, 0.8f, -57.2f), 20f);
            Place("Caixote", new Vector3(4.5f, 0, -63.5f), 10f);
            Place("Mochila_do_Explorador", new Vector3(2f, 0, -63f), -30f);
            Place("Pa", new Vector3(5.2f, 0, -62.6f), 40f);
            Place("Saco", new Vector3(-6.2f, 0, -60f), 0f);
            Place("Rolo_de_Corda", new Vector3(1.2f, 0, -64f));

            GameObject start = TriggerBox("Inicio_Tutorial", new Vector3(0, 1.5f, -60f), new Vector3(6f, 3f, 6f));
            RoomTrigger rt = start.AddComponent<RoomTrigger>();
            rt.roomIndex = 0;
            rt.title = "Treasure Hunter";
            rt.subtitle = "Uruguai — em busca do templo de Añang";
            rt.tutorial = "WASD para andar e o mouse para olhar. Shift corre e Espaço pula (segure para pular mais alto).";
            rt.line = "Segundo o mapa, o templo fica no fim desta trilha.";
            rt.respawnPoint = Marker("Spawn_Acampamento", new Vector3(0, 0.05f, -60f));
        }

        /// <summary>
        /// Templo da superfície: base, escadaria com rampa, câmara com porta, piso que desaba,
        /// pedestal do ídolo, poço e a transição de queda para o subterrâneo.
        /// </summary>
        /// <param name="surface">Grupo pai.</param>
        private static void BuildTemple(Transform surface)
        {
            Transform temple = Group("Templo", surface);

            // Base do templo (y 0..3) ao redor do buraco da câmara.
            Block("Base", new Vector3(-8, 0, -20), new Vector3(8, 3, -16), Palette.TempleBrick);
            Block("Base", new Vector3(-8, 0, -6), new Vector3(8, 3, -4), Palette.TempleBrick);
            Block("Base", new Vector3(-8, 0, -16), new Vector3(-5, 3, -6), Palette.TempleBrick);
            Block("Base", new Vector3(5, 0, -16), new Vector3(8, 3, -6), Palette.TempleBrick);

            // Escadaria visual (sem colisão) + rampa invisível para subir.
            for (int i = 0; i < 6; i++)
                Block("Degrau", new Vector3(-3, 0, -26 + i), new Vector3(3, 0.5f * (i + 1), -20), Palette.TempleBrick, false);
            GameObject ramp = new GameObject("RampaDaEscada");
            ramp.transform.SetParent(currentParent, false);
            float angle = Mathf.Atan2(3f, 6f) * Mathf.Rad2Deg;
            ramp.transform.rotation = Quaternion.Euler(-angle, 0, 0);
            Vector3 normal = ramp.transform.up;
            ramp.transform.position = new Vector3(0, 1.5f, -23f) - normal * 0.1f;
            ramp.AddComponent<BoxCollider>().size = new Vector3(6f, 0.2f, Mathf.Sqrt(45f));
            ramp.tag = GameTags.Ground;

            // Câmara do templo (sem piso: o piso é feito de blocos que desabam).
            Room("Camara", -5, 5, -16, -6, 3, 5, Palette.TempleBrick, null, Palette.TempleBrick, null,
                new Opening(Side.South, 0, 3, 3.5f));
            Block("Telhado", new Vector3(-5, 9, -16), new Vector3(5, 10, -6), Palette.TempleBrick);
            Block("Telhado", new Vector3(-3.5f, 10, -14.5f), new Vector3(3.5f, 11, -7.5f), Palette.TempleBrick);
            Block("Telhado", new Vector3(-1, 11, -12), new Vector3(1, 11.6f, -10), Palette.Gold);

            // Decoração externa.
            Place("Estatua_Guardiao", new Vector3(-4.5f, 0, -25f), 180f);
            Place("Estatua_Guardiao", new Vector3(4.5f, 0, -25f), 180f);
            Place("Obelisco", new Vector3(-7f, 3, -19f));
            Place("Obelisco", new Vector3(7f, 3, -19f));
            Place("Pilar_Templo", new Vector3(-3f, 3, -18.5f));
            Place("Pilar_Templo", new Vector3(3f, 3, -18.5f));
            Place("Cortina_de_Trepadeiras", new Vector3(-4.2f, 4.5f, -17.05f), 180f);
            Place("Cortina_de_Trepadeiras", new Vector3(5.5f, 4f, -17.05f), 180f);
            Place("Arco_de_Pedra", new Vector3(0, 0, -30f), 0f);
            Place("Cabeca_de_Pedra", new Vector3(-10f, 0, -24f), 60f);
            Place("Tocha_de_Parede", new Vector3(-2.2f, 4.5f, -17.05f), 180f);
            Place("Tocha_de_Parede", new Vector3(2.2f, 4.5f, -17.05f), 180f);

            // Interior: tochas e relevos nas paredes (nada no piso, que vai desabar).
            Place("Tocha_de_Parede", new Vector3(-4.95f, 5.2f, -12f), 90f);
            Place("Tocha_de_Parede", new Vector3(4.95f, 5.2f, -12f), -90f);
            Place("Relevo_na_Parede", new Vector3(0, 4.6f, -6.05f), 180f);
            Place("Cortina_de_Trepadeiras", new Vector3(3.2f, 4.4f, -6.1f), 180f);

            // Piso de blocos que desabam e coluna do pedestal.
            Transform floor = Group("PisoQueDesaba", temple);
            List<Rigidbody> tiles = new List<Rigidbody>();
            for (int ix = 0; ix < 5; ix++)
            {
                for (int iz = 0; iz < 5; iz++)
                {
                    if (ix == 2 && iz == 2) continue;
                    GameObject t = Place("Bloco_do_Piso", new Vector3(-4 + ix * 2, 3, -15 + iz * 2));
                    tiles.Add(t.GetComponent<Rigidbody>());
                }
            }
            // Anel de blocos estreitos em volta da coluna do pedestal (assim não sobra chão firme ao lado dele).
            Vector3[] ringPos = { new Vector3(0, 3, -10.25f), new Vector3(0, 3, -11.75f), new Vector3(-0.75f, 3, -11f), new Vector3(0.75f, 3, -11f) };
            Vector3[] ringScale = { new Vector3(1, 1, 0.25f), new Vector3(1, 1, 0.25f), new Vector3(0.25f, 1, 0.5f), new Vector3(0.25f, 1, 0.5f) };
            for (int i = 0; i < 4; i++)
            {
                GameObject t = Place("Bloco_do_Piso", ringPos[i]);
                t.transform.localScale = ringScale[i];
                tiles.Add(t.GetComponent<Rigidbody>());
            }
            currentParent = temple;
            Block("ColunaDoPedestal", new Vector3(-0.5f, -14, -11.5f), new Vector3(0.5f, 3, -10.5f), Palette.StoneDark);
            GameObject pedestal = Place("Pedestal_do_Idolo", new Vector3(0, 3, -11), 180f);

            // Porta da câmara: começa aberta (escondida dentro da base) e sobe para trancar John.
            GameObject doorGo = Place("Porta_de_Pedra", new Vector3(0, 3, -16.5f));
            StoneDoor door = doorGo.GetComponent<StoneDoor>();
            door.startOpen = true;
            door.openHeight = -3.6f;
            door.speed = 3f;

            // Armadilha.
            GameObject trapGo = new GameObject("ArmadilhaDoIdolo");
            trapGo.transform.SetParent(temple, false);
            trapGo.transform.position = new Vector3(0, 3, -11);
            TempleCollapseTrap trap = trapGo.AddComponent<TempleCollapseTrap>();
            trap.floorTiles = tiles.ToArray();
            trap.entranceDoor = door;
            currentParent = trapGo.transform;
            trap.respawnAbovePit = Marker("Renascer_Sobre_o_Poco", new Vector3(0, 3.6f, -8f), 0f);
            currentParent = temple;
            trap.dustEffects = new[]
            {
                Place("FX_DoorDust", new Vector3(0, 3.2f, -11)).GetComponent<ParticleSystem>(),
                Place("FX_Smoke", new Vector3(-3, 7.5f, -9)).GetComponent<ParticleSystem>(),
                Place("FX_Smoke", new Vector3(3, 7.5f, -13)).GetComponent<ParticleSystem>()
            };
            foreach (ParticleSystem p in trap.dustEffects)
            {
                var main = p.main; main.playOnAwake = false;
            }
            ArtifactPedestal ap = pedestal.GetComponent<ArtifactPedestal>();
            ap.trap = trap;
            ap.firstTimeTip = "Objetos interativos brilham levemente. Pressione [E] para pegar o ídolo.";

            GameObject entry = TriggerBox("Entrada_do_Templo", new Vector3(0, 1.5f, -27.5f), new Vector3(8f, 3f, 2f));
            RoomTrigger rt = entry.AddComponent<RoomTrigger>();
            rt.roomIndex = 0;
            rt.objective = "Entre no templo e encontre o artefato da lenda.";
            rt.line = "Aí está... o templo de Añang! Deve haver algo valioso lá dentro.";
            rt.respawnPoint = Marker("Spawn_Escadaria", new Vector3(0, 0.05f, -28f));

            // Poço da queda (paredes escuras) e transição para o subterrâneo.
            Group("Poco", temple);
            Block("ParedeDoPoco", new Vector3(-6, -14, -17), new Vector3(-5, 0, -5), Palette.StoneDark);
            Block("ParedeDoPoco", new Vector3(5, -14, -17), new Vector3(6, 0, -5), Palette.StoneDark);
            Block("ParedeDoPoco", new Vector3(-5, -14, -17), new Vector3(5, 0, -16), Palette.StoneDark);
            Block("ParedeDoPoco", new Vector3(-5, -14, -6), new Vector3(5, 0, -5), Palette.StoneDark);
            Block("FundoDoPoco", new Vector3(-6, -16, -17), new Vector3(6, -15, -5), Palette.Void);
            GameObject fall = TriggerBox("TransicaoDeQueda", new Vector3(0, -10f, -11f), new Vector3(10f, 2f, 10f));
            FallTransition ft = fall.AddComponent<FallTransition>();
            currentParent = fall.transform;
            ft.destination = Marker("Despertar_Sala1", new Vector3(0, Y0 + 0.05f, 2.5f), 0f);
        }

        // ================================================================== Sala 1

        /// <summary>
        /// Sala 1 — Câmara das Raízes: encontrar a alavanca escondida entre as raízes (Observação).
        /// Inclui o nicho secreto atrás das trepadeiras com a Máscara Charrúa (artefato opcional).
        /// </summary>
        private static void BuildRoom1()
        {
            Transform under = Group("Subterraneo");
            Transform r1 = Group("Sala1_CamaraDasRaizes", under);
            Room("Estrutura", -8, 8, 0, 16, Y0, 6, Palette.StoneBrick, Palette.StoneFloor, Palette.StoneDark, null,
                new Opening(Side.North, 0, 3, 3.5f), new Opening(Side.West, 6, 2f, 2.6f));
            Room("NichoSecreto", -12.5f, -9f, 4.5f, 7.5f, Y0, 3.2f, Palette.StoneDark, Palette.StoneFloor, Palette.StoneDark, new[] { Side.East });
            Room("CorredorA", -1.5f, 1.5f, 17, 21, Y0, 4, Palette.StoneBrick, Palette.StoneFloor, Palette.StoneDark, new[] { Side.South, Side.North });
            currentParent = r1;

            // Porta 1 e a alavanca escondida no canto nordeste, entre raízes e arbustos.
            StoneDoor d1 = Door("Porta1", new Vector3(0, Y0, 16.5f));
            GameObject lever = Place("Alavanca", new Vector3(6.7f, Y0, 14.5f), -90f);
            Lever l = lever.GetComponent<Lever>();
            l.targets = new SignalReceiver[] { d1 };
            l.skillOnUse = "observacao";
            l.lineOnUse = "Uma alavanca escondida! Ouvi a porta se mexer.";
            Place("Raizes_Penduradas", new Vector3(6.4f, Y0 + 6f, 13.6f));
            Place("Raizes_Penduradas", new Vector3(5.6f, Y0 + 6f, 15.1f), 90f);
            Place("Raizes_Penduradas", new Vector3(7.2f, Y0 + 6f, 12.2f), 45f);
            Place("Arbusto", new Vector3(5.4f, Y0, 13.3f), 20f);
            Place("Arbusto", new Vector3(7.1f, Y0, 12.4f), 80f);
            Place("Samambaia", new Vector3(5.8f, Y0, 15.2f));
            Place("Feixe_de_Luz", new Vector3(5.6f, Y0 + 5.8f, 13.4f));

            // Cortina de trepadeiras escondendo o nicho secreto (sem colisão: dá para atravessar).
            Place("Cortina_de_Trepadeiras", new Vector3(-8.45f, Y0, 6f), 90f);
            Place("Mascara_Charrua", new Vector3(-11.6f, Y0, 6f));
            Place("Velas", new Vector3(-12.1f, Y0, 4.9f));
            Place("Cranio", new Vector3(-12f, Y0, 7.1f), 160f);
            Place("Ossos", new Vector3(-10.3f, Y0, 5f), 30f);

            // Iluminação fraca, decoração e tábua de habilidade.
            Place("Tocha_de_Parede", new Vector3(-4f, Y0 + 2.3f, 0.05f), 0f);
            Place("Tocha_de_Parede", new Vector3(4f, Y0 + 2.3f, 0.05f), 0f);
            Place("Tocha_de_Parede", new Vector3(-2.6f, Y0 + 2.3f, 15.95f), 180f);
            GameObject tablet = Place("Tabua_de_Habilidade", new Vector3(-2.5f, Y0, 5.2f), 180f);
            SkillTablet st = tablet.GetComponent<SkillTablet>();
            st.title = "Inscrição: Observação";
            st.text = "\"Nem todo caminho está à vista. Quem observa com calma encontra o que os apressados ignoram.\"";
            st.firstTimeTip = "Tábuas antigas guardam dicas. Pressione [E] para ler.";

            Place("Pilar_Quebrado", new Vector3(-5f, Y0, 11f), 30f);
            Place("Pilar", new Vector3(4.5f, Y0, 4.5f));
            Place("Estatua_Guardiao", new Vector3(-6.5f, Y0, 14.8f), 135f);
            Place("Entulho", new Vector3(-6f, Y0, 2f));
            Place("Entulho", new Vector3(2.5f, Y0, 10f), 90f);
            Place("Cranio", new Vector3(1.5f, Y0, 7f), 40f);
            Place("Ossos", new Vector3(2f, Y0, 7.4f));
            Place("Vaso_Pequeno", new Vector3(-7.2f, Y0, 1f));
            Place("Vaso_Grande", new Vector3(-7.1f, Y0, 2.1f));
            Place("Barril", new Vector3(7.1f, Y0, 1.2f));
            Place("Caixote", new Vector3(6.9f, Y0, 3f), 15f);
            Place("Musgo", new Vector3(-3f, Y0, 9f));
            Place("Musgo", new Vector3(3.5f, Y0, 13f), 50f);
            Place("Cogumelos_Marrons", new Vector3(-4.6f, Y0, 10.2f));
            Place("Cogumelo_Vermelho", new Vector3(5.2f, Y0, 6f));
            Place("Raizes_Penduradas", new Vector3(-2f, Y0 + 6f, 8f), 20f);
            Place("Raizes_Penduradas", new Vector3(2f, Y0 + 6f, 3f), 70f);
            Place("Raizes_Penduradas", new Vector3(-5.5f, Y0 + 6f, 6f));
            Place("Cortina_de_Trepadeiras", new Vector3(3f, Y0 + 1f, 0.15f), 0f);
            Place("Relevo_na_Parede", new Vector3(7.95f, Y0 + 1.6f, 8f), -90f);
            Place("Tocha_de_Parede", new Vector3(0f, Y0 + 2.6f, 19f), 90f).transform.position = new Vector3(-1.45f, Y0 + 2.6f, 19f);

            Treasures("Tesouros_Sala1", 3,
                new Vector3(-6.5f, Y0, 8f), new Vector3(6.5f, Y0, 9f), new Vector3(0f, Y0, 12f),
                new Vector3(-3f, Y0, 1.5f), new Vector3(3f, Y0, 14.5f), new Vector3(-10.5f, Y0, 6.8f));
        }

        /// <summary>
        /// Cria uma porta de pedra do subterrâneo.
        /// </summary>
        /// <param name="name">Nome do objeto.</param>
        /// <param name="pos">Posição (centro da base).</param>
        /// <returns>O componente StoneDoor.</returns>
        private static StoneDoor Door(string name, Vector3 pos)
        {
            GameObject go = Place("Porta_de_Pedra", pos);
            go.name = name;
            StoneDoor d = go.GetComponent<StoneDoor>();
            d.openHeight = 3.4f;
            d.requiredSignals = 1;
            d.latch = true;
            return d;
        }

        /// <summary>
        /// Cria o gatilho de entrada de uma sala (checkpoint, título, missão e dicas).
        /// </summary>
        /// <param name="index">Índice da sala.</param>
        /// <param name="center">Centro do gatilho.</param>
        /// <param name="spawn">Ponto de renascimento.</param>
        /// <param name="title">Título.</param>
        /// <param name="objective">Missão.</param>
        /// <param name="tutorial">Dica.</param>
        /// <param name="line">Fala de John.</param>
        /// <param name="skill">Habilidade desbloqueada ao entrar.</param>
        private static void RoomEntry(int index, Vector3 center, Vector3 spawn, string title, string objective, string tutorial, string line, string skill = "")
        {
            GameObject go = TriggerBox("Entrada_Sala" + index, center, new Vector3(3f, 3f, 1f));
            RoomTrigger rt = go.AddComponent<RoomTrigger>();
            rt.roomIndex = index;
            rt.title = "Nível 1 — A Entrada do Templo";
            rt.subtitle = title;
            rt.objective = objective;
            rt.tutorial = tutorial;
            rt.line = line;
            rt.skillOnEnter = skill;
            Transform prev = currentParent;
            currentParent = go.transform;
            rt.respawnPoint = Marker("Spawn_Sala" + index, spawn);
            currentParent = prev;
        }

        // ================================================================== Sala 2 + Corredor das Pedras

        /// <summary>
        /// Sala 2 — Câmara do Peso: empurrar o caixote até a placa (Força); colar de jade no alto (usar o caixote como degrau).
        /// Corredor das Pedras: pedras caem do teto em posições sorteadas (Reflexos).
        /// </summary>
        private static void BuildRoom2AndRockCorridor()
        {
            Transform under = GameObject.Find("Subterraneo").transform;
            Transform r2 = Group("Sala2_CamaraDoPeso", under);
            Room("Estrutura", -9, 9, 22, 40, Y0, 7, Palette.StoneBrick, Palette.StoneFloor, Palette.StoneDark, null,
                new Opening(Side.South, 0, 3, 3.5f), new Opening(Side.North, 0, 3, 3.5f));
            currentParent = r2;

            RoomEntry(2, new Vector3(0, Y0 + 1.5f, 21.5f), new Vector3(0, Y0 + 0.05f, 23.5f), "Sala 2: Câmara do Peso",
                "Leve o caixote até a placa de pressão para abrir a porta.",
                "Empurre o caixote andando contra ele. A porta só fica aberta enquanto ele estiver bem em cima da placa.",
                "Uma placa no chão... meu peso não basta. Preciso de algo mais pesado.");

            StoneDoor d2 = Door("Porta2", new Vector3(0, Y0, 40.5f));
            d2.latch = false; // fecha de novo se o caixote sair da placa
            GameObject crate = Place("Caixote_Empurravel", new Vector3(-5f, Y0 + 0.02f, 27f));
            crate.GetComponent<PushableCrate>().roomIndex = 2;
            GameObject plate = Place("Placa_de_Pressao", new Vector3(3f, Y0, 35f));
            PressurePlate pp = plate.GetComponent<PressurePlate>();
            pp.targets = new SignalReceiver[] { d2 };
            pp.skillOnPress = "forca";
            pp.lineOnPress = "Isso! O peso do caixote acionou o mecanismo.";

            // Plataforma alta com o Colar de Jade (só alcançável subindo no caixote).
            Block("Plataforma", new Vector3(6, Y0, 29), new Vector3(9, Y0 + 2.3f, 35), Palette.StoneBrick);
            Place("Colar_de_Jade", new Vector3(7.6f, Y0 + 2.3f, 32f));
            Place("Pilha_de_Ouro", new Vector3(8.2f, Y0 + 2.3f, 30.2f), 20f);
            Place("Vaso_Pequeno", new Vector3(8.3f, Y0 + 2.3f, 34.2f));

            GameObject tablet = Place("Tabua_de_Habilidade", new Vector3(-3.5f, Y0, 24.5f), 180f);
            SkillTablet st = tablet.GetComponent<SkillTablet>();
            st.title = "Inscrição: Força";
            st.text = "\"A pedra cede ao peso, não à pressa. Leve o que for pesado até onde ele é esperado.\"";

            Place("Pilar", new Vector3(-7f, Y0, 24f));
            Place("Pilar", new Vector3(7f, Y0, 24f));
            Place("Pilar", new Vector3(-7f, Y0, 38f));
            Place("Pilar", new Vector3(7f, Y0, 38f));
            Place("Tocha_de_Parede", new Vector3(-8.95f, Y0 + 2.4f, 28f), 90f);
            Place("Tocha_de_Parede", new Vector3(-8.95f, Y0 + 2.4f, 36f), 90f);
            Place("Tocha_de_Parede", new Vector3(8.95f, Y0 + 2.4f, 26f), -90f);
            Place("Tocha_de_Parede", new Vector3(8.95f, Y0 + 3.6f, 37f), -90f);
            Place("Tocha_de_Parede", new Vector3(-2.6f, Y0 + 2.4f, 39.95f), 180f);
            Place("Lanterna_Pendurada", new Vector3(0f, Y0 + 7f, 31f));
            Place("Viga_de_Madeira", new Vector3(0f, Y0 + 6.6f, 31f));
            Place("Barril", new Vector3(-8.2f, Y0, 32f));
            Place("Barril", new Vector3(-8.3f, Y0, 33.2f), 30f);
            Place("Caixote", new Vector3(-8f, Y0, 39f), 10f);
            Place("Vaso_Grande", new Vector3(5.2f, Y0, 39.2f));
            Place("Rolo_de_Corda", new Vector3(-6f, Y0, 30f));
            Place("Entulho", new Vector3(-2f, Y0, 38.5f));
            Place("Cranio", new Vector3(8f, Y0, 27f), 250f);
            Place("Raizes_Penduradas", new Vector3(-4f, Y0 + 7f, 35f));
            Place("Raizes_Penduradas", new Vector3(3f, Y0 + 7f, 26f), 60f);
            Place("Relevo_na_Parede", new Vector3(-8.95f, Y0 + 3f, 24.5f), 90f);
            Treasures("Tesouros_Sala2", 4,
                new Vector3(-8f, Y0, 36f), new Vector3(-1f, Y0, 30f), new Vector3(4.5f, Y0, 24f),
                new Vector3(0f, Y0, 38f), new Vector3(-4f, Y0, 34f), new Vector3(8.2f, Y0, 39f), new Vector3(-8.3f, Y0, 23f));

            // ---------------------------------------------------------- Corredor das Pedras
            Transform cb = Group("CorredorDasPedras", under);
            Room("Estrutura", -2.5f, 2.5f, 41, 59, Y0, 5, Palette.StoneBrick, Palette.StoneFloor, Palette.StoneDark, new[] { Side.South, Side.North });
            currentParent = cb;

            GameObject spawnerGo = TriggerBox("ArmadilhaDePedras", new Vector3(0, Y0 + 2f, 50f), new Vector3(5f, 4f, 17f));
            RockSpawner spawner = spawnerGo.AddComponent<RockSpawner>();
            spawner.rockPrefabs = new[] { PropFactory.Prefabs["Pedra_Pequena"], PropFactory.Prefabs["Pedra_Media"], PropFactory.Prefabs["Pedra_Grande"] };
            spawner.warningDustPrefab = ParticleFactory.Prefabs["FX_WarningDust"];
            List<Transform> points = new List<Transform>();
            currentParent = spawnerGo.transform;
            float[] xs = { -1.1f, 0f, 1.1f };
            int k = 0;
            for (float z = 43f; z <= 57f; z += 1.6f)
            {
                points.Add(Marker("PontoDeQueda_" + (points.Count + 1), new Vector3(xs[k % 3], Y0 + 3.9f, z)));
                k++;
            }
            spawner.spawnPoints = points.ToArray();
            currentParent = cb;

            Place("Tocha_de_Parede", new Vector3(-2.45f, Y0 + 2.4f, 45f), 90f);
            Place("Tocha_de_Parede", new Vector3(2.45f, Y0 + 2.4f, 53f), -90f);
            Place("Entulho", new Vector3(-1.9f, Y0, 48f), 30f);
            Place("Entulho", new Vector3(1.9f, Y0, 56f), 200f);
            Place("Mochila_do_Explorador", new Vector3(1.9f, Y0, 44f), -70f);
            Place("Diario_do_Explorador", new Vector3(1.6f, Y0, 44.8f), 30f);
            Place("Cranio", new Vector3(2f, Y0, 45.3f), 210f);
            Place("Ossos", new Vector3(-1.8f, Y0, 51f));
            Place("Raizes_Penduradas", new Vector3(-1.5f, Y0 + 5f, 50f));
        }

        // ================================================================== Sala 3

        /// <summary>
        /// Sala 3 — Câmara dos Símbolos: sala escura; acender braseiros/erguer a tocha revela a sequência (Domínio da Tocha)
        /// e pressionar os símbolos na ordem abre a porta (Memória).
        /// </summary>
        private static void BuildRoom3()
        {
            Transform under = GameObject.Find("Subterraneo").transform;
            Transform r3 = Group("Sala3_CamaraDosSimbolos", under);
            Room("Estrutura", -8, 8, 60, 76, Y0, 6, Palette.StoneBrick, Palette.StoneFloor, Palette.StoneDark, null,
                new Opening(Side.South, 0, 3, 3.5f), new Opening(Side.North, 0, 3, 3.5f));
            Room("CorredorC", -1.5f, 1.5f, 77, 81, Y0, 4, Palette.StoneBrick, Palette.StoneFloor, Palette.StoneDark, new[] { Side.South, Side.North });
            currentParent = r3;

            RoomEntry(3, new Vector3(0, Y0 + 1.5f, 60.5f), new Vector3(0, Y0 + 0.05f, 61.5f), "Sala 3: Câmara dos Símbolos",
                "Revele os símbolos escondidos e repita a sequência no mecanismo.",
                "Erga a tocha com [F] perto das paredes. Braseiros podem ser acesos com [E].",
                "Escapei das pedras! Mas aqui está escuro demais...", "reflexos");

            StoneDoor d3 = Door("Porta3", new Vector3(0, Y0, 76.5f));
            SymbolSequencePuzzle puzzle = SymbolPuzzle("MecanismoDosSimbolos", 76f, 70f, 4.6f, d3,
                "A porta se abriu! Os antigos adoravam enigmas...", 0);

            GameObject wall = puzzle.transform.Find("MuralDeSimbolos").gameObject;
            TorchRevealable reveal = wall.GetComponent<TorchRevealable>();
            Brazier b1 = Place("Braseiro", new Vector3(-5f, Y0, 73.5f)).GetComponent<Brazier>();
            Brazier b2 = Place("Braseiro", new Vector3(5f, Y0, 73.5f)).GetComponent<Brazier>();
            b1.targets = new SignalReceiver[] { reveal };
            b2.targets = new SignalReceiver[] { reveal };
            b1.skillOnLight = "tocha";
            b2.skillOnLight = "tocha";

            GameObject tablet = Place("Tabua_de_Habilidade", new Vector3(-4.5f, Y0, 62.5f), 180f);
            SkillTablet st = tablet.GetComponent<SkillTablet>();
            st.title = "Inscrição: Luz e Memória";
            st.text = "\"Os antigos guardavam seus segredos na escuridão. Leve a luz até eles e grave a ordem — da esquerda para a direita.\"";
            Place("Velas", new Vector3(-5.6f, Y0, 62f));

            Place("Cabeca_de_Pedra", new Vector3(-6.3f, Y0, 67f), 90f);
            Place("Cabeca_de_Pedra", new Vector3(6.3f, Y0, 67f), -90f);
            Place("Relevo_na_Parede", new Vector3(-7.95f, Y0 + 2f, 72f), 90f);
            Place("Relevo_na_Parede", new Vector3(7.95f, Y0 + 2f, 72f), -90f);
            Place("Vaso_Grande", new Vector3(7f, Y0, 75f));
            Place("Vaso_Pequeno", new Vector3(-7.2f, Y0, 75.2f));
            Place("Cranio", new Vector3(-6.8f, Y0, 63.5f), 30f);
            Place("Ossos", new Vector3(-6.5f, Y0, 64f));
            Place("Raizes_Penduradas", new Vector3(3f, Y0 + 6f, 64f));
            Place("Coracao", new Vector3(6.6f, Y0, 61.5f));
            Place("Tocha_de_Parede", new Vector3(-1.45f, Y0 + 2.6f, 79f), 90f);
            Treasures("Tesouros_Sala3", 3,
                new Vector3(-7f, Y0, 69f), new Vector3(7f, Y0, 70f), new Vector3(0f, Y0, 66f),
                new Vector3(-3f, Y0, 74.5f), new Vector3(3f, Y0, 74.5f), new Vector3(5f, Y0, 63f));
        }

        /// <summary>
        /// Monta um enigma de símbolos: botões em fila, mural com 4 molduras acima da porta (revelado pela tocha/braseiros).
        /// </summary>
        /// <param name="name">Nome do objeto do enigma.</param>
        /// <param name="wallZ">Z da face interna da parede norte.</param>
        /// <param name="buttonsZ">Z da fila de botões.</param>
        /// <param name="muralHeight">Altura do mural acima do piso.</param>
        /// <param name="door">Porta aberta pela solução.</param>
        /// <param name="line">Fala ao resolver.</param>
        /// <param name="requiredPower">Sinais de energia necessários (0 = sempre ativo).</param>
        /// <returns>O enigma criado.</returns>
        private static SymbolSequencePuzzle SymbolPuzzle(string name, float wallZ, float buttonsZ, float muralHeight, StoneDoor door, string line, int requiredPower)
        {
            Transform prev = currentParent;
            GameObject go = new GameObject(name);
            go.transform.SetParent(currentParent, false);
            go.transform.position = new Vector3(0, Y0, buttonsZ);
            currentParent = go.transform;

            SymbolSequencePuzzle puzzle = go.AddComponent<SymbolSequencePuzzle>();
            puzzle.glyphMaterials = Palette.Glyph;
            puzzle.glyphLitMaterials = Palette.GlyphLit;
            puzzle.sequenceLength = 4;
            puzzle.randomize = true;
            puzzle.requiredSignals = requiredPower;
            puzzle.latch = true;
            puzzle.targets = new SignalReceiver[] { door };
            puzzle.skillOnSolve = "memoria";
            puzzle.lineOnSolve = line;

            float[] xs = { -3f, -1f, 1f, 3f };
            SymbolButton[] buttons = new SymbolButton[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject b = Place("Botao_de_Simbolo", new Vector3(xs[i], Y0, buttonsZ), 180f);
                b.name = "Botao_" + (i + 1);
                buttons[i] = b.GetComponent<SymbolButton>();
            }
            puzzle.buttons = buttons;

            GameObject mural = new GameObject("MuralDeSimbolos");
            mural.transform.SetParent(go.transform, false);
            mural.transform.position = new Vector3(0, Y0 + muralHeight, wallZ - 0.5f);
            currentParent = mural.transform;
            Renderer[] faces = new Renderer[4];
            float[] mx = { -2.175f, -0.725f, 0.725f, 2.175f };
            for (int i = 0; i < 4; i++)
            {
                GameObject f = Place("Moldura_de_Simbolo", new Vector3(mx[i], Y0 + muralHeight, wallZ - 0.02f), 180f);
                f.name = "Moldura_" + (i + 1);
                faces[i] = f.transform.Find("Face").GetComponent<Renderer>();
            }
            puzzle.displaySlots = faces;
            TorchRevealable reveal = mural.AddComponent<TorchRevealable>();
            reveal.targets = faces;
            reveal.requiredSignals = 2;
            reveal.latch = true;
            reveal.skillOnReveal = "tocha";

            currentParent = prev;
            return puzzle;
        }

        // ================================================================== Sala 4 + saída

        /// <summary>
        /// Sala 4 — Portão do Templo (Combinação). A sala é dividida por uma parede com uma passagem guardada por um
        /// tronco com espinhos que gira sem parar (torque), a poucos metros do portão. Do outro lado do tronco fica a
        /// alavanca: ao puxá-la, o tronco freia e afunda no chão e o alçapão solta o caixote. O caixote precisa ficar
        /// sobre a placa para energizar o mecanismo de símbolos e manter o portão aberto; os braseiros (ou a tocha)
        /// revelam a sequência no mural acima do portão.
        /// </summary>
        private static void BuildRoom4AndExit()
        {
            Transform under = GameObject.Find("Subterraneo").transform;
            Transform r4 = Group("Sala4_PortaoDoTemplo", under);
            Room("Estrutura", -10, 10, 82, 104, Y0, 8, Palette.StoneBrick, Palette.StoneFloor, Palette.StoneDark, null,
                new Opening(Side.South, 0, 3, 3.5f), new Opening(Side.North, 0, 3, 3.5f));
            currentParent = r4;

            // Parede divisória perto do portão, com uma passagem de 7,2 m no meio (onde gira o tronco).
            Block("Divisoria", new Vector3(-10, Y0, 100), new Vector3(-3.6f, Y0 + 8, 101), Palette.StoneBrick);
            Block("Divisoria", new Vector3(3.6f, Y0, 100), new Vector3(10, Y0 + 8, 101), Palette.StoneBrick);

            RoomEntry(4, new Vector3(0, Y0 + 1.5f, 82.5f), new Vector3(0, Y0 + 0.05f, 83.5f), "Sala 4: Portão do Templo",
                "Encontre um jeito de parar o tronco e abrir o grande portão.",
                "Pule o tronco no momento certo. Do outro lado dele há uma alavanca.",
                "Um tronco com espinhos guardando o portão... deve haver um jeito de pará-lo.");

            // Portão: só abre com a sequência correta E com o caixote sobre a placa (2 sinais, sem trava).
            StoneDoor d4 = Door("Porta4", new Vector3(0, Y0, 104.5f));
            d4.requiredSignals = 2;
            d4.latch = false;

            // Tronco giratório na passagem da divisória, a 3,5 m do portão.
            GameObject logGo = Place("Tronco_Giratorio", new Vector3(0f, Y0, 100.5f));
            SpinningLogTrap log = logGo.GetComponent<SpinningLogTrap>();

            // Alçapão com o caixote (lado sul) e a alavanca do outro lado do tronco (lado norte).
            GameObject hatch = Place("Plataforma_com_Alcapao", new Vector3(7f, Y0, 87f));
            HatchRelease hr = hatch.GetComponent<HatchRelease>();
            hr.lineOnOpen = "";
            GameObject crate = Place("Caixote_Empurravel", new Vector3(7f, Y0 + 3.5f, 87f));
            crate.GetComponent<PushableCrate>().roomIndex = 4;
            GameObject lever = Place("Alavanca", new Vector3(-7f, Y0, 103.2f), 180f);
            Lever l = lever.GetComponent<Lever>();
            l.targets = new SignalReceiver[] { hr, log };
            l.lineOnUse = "O tronco está parando! E ouvi algo se soltar do outro lado da sala.";

            // Mecanismo de símbolos (botões antes da passagem; mural acima do portão).
            SymbolSequencePuzzle puzzle = SymbolPuzzle("MecanismoDoPortao", 104f, 95f, 4.8f, d4,
                "Consegui! O portão está se abrindo... sinto ar fresco!", 1);
            puzzle.skillOnSolve = "";
            puzzle.latch = false;

            // Placa: energiza o mecanismo e segura o portão aberto enquanto o caixote estiver sobre ela.
            GameObject plate = Place("Placa_de_Pressao", new Vector3(0f, Y0, 90f));
            PressurePlate pp = plate.GetComponent<PressurePlate>();
            pp.targets = new SignalReceiver[] { puzzle, d4 };

            TorchRevealable reveal = puzzle.transform.Find("MuralDeSimbolos").GetComponent<TorchRevealable>();
            Brazier b1 = Place("Braseiro", new Vector3(-5.3f, Y0, 102.4f)).GetComponent<Brazier>();
            Brazier b2 = Place("Braseiro", new Vector3(5.3f, Y0, 102.4f)).GetComponent<Brazier>();
            b1.targets = new SignalReceiver[] { reveal };
            b2.targets = new SignalReceiver[] { reveal };

            GameObject tablet = Place("Tabua_de_Habilidade", new Vector3(-4f, Y0, 84.5f), 180f);
            SkillTablet st = tablet.GetComponent<SkillTablet>();
            st.title = "Inscrição: Combinação";
            st.text = "\"Além do guardião que gira está a alavanca que o faz descansar. Depois, una o que aprendeu: peso, luz e memória.\"";

            Place("Tocha_de_Parede", new Vector3(-9.95f, Y0 + 2.6f, 90f), 90f);
            Place("Tocha_de_Parede", new Vector3(9.95f, Y0 + 2.6f, 92f), -90f);
            Place("Tocha_de_Parede", new Vector3(-6.5f, Y0 + 2.6f, 99.95f), 180f);
            Place("Tocha_de_Parede", new Vector3(6.5f, Y0 + 2.6f, 99.95f), 180f);
            Place("Tocha_de_Parede", new Vector3(-9.95f, Y0 + 2.6f, 102.5f), 90f);
            Place("Tocha_de_Parede", new Vector3(9.95f, Y0 + 2.6f, 102.5f), -90f);
            Place("Estatua_Guardiao", new Vector3(-8.6f, Y0, 102.9f), 150f);
            Place("Estatua_Guardiao", new Vector3(8.6f, Y0, 102.9f), -150f);
            Place("Totem", new Vector3(-8.6f, Y0, 93.5f), 90f);
            Place("Totem", new Vector3(3.5f, Y0, 83f), 180f);
            Place("Bau_de_Tesouro", new Vector3(9f, Y0, 83.2f), -90f);
            Place("Pilha_de_Ouro", new Vector3(8.6f, Y0, 84.6f));
            Place("Vaso_Grande", new Vector3(-9f, Y0, 83f));
            Place("Barril", new Vector3(-9.1f, Y0, 95f));
            Place("Entulho", new Vector3(-6f, Y0, 91f), 120f);
            Place("Raizes_Penduradas", new Vector3(-5f, Y0 + 8f, 88f));
            Place("Raizes_Penduradas", new Vector3(6.5f, Y0 + 8f, 102.5f), 30f);
            Place("Lanterna_Pendurada", new Vector3(0f, Y0 + 8f, 88f));
            Treasures("Tesouros_Sala4", 4,
                new Vector3(-8f, Y0, 88f), new Vector3(-2f, Y0, 85f), new Vector3(4f, Y0, 93f), new Vector3(-6f, Y0, 98.5f),
                new Vector3(8f, Y0, 98f), new Vector3(-2.8f, Y0, 103.3f), new Vector3(2.8f, Y0, 103.3f), new Vector3(9f, Y0, 89.5f));

            // ---------------------------------------------------------- Saída
            Transform exit = Group("Saida", under);
            Room("Estrutura", -1.5f, 1.5f, 105, 118, Y0, 4, Palette.StoneBrick, Palette.StoneFloor, Palette.StoneDark, new[] { Side.South });
            currentParent = exit;
            GameObject exitGo = TriggerBox("SaidaDoNivel1", new Vector3(0, Y0 + 1.5f, 114f), new Vector3(3f, 3f, 3f));
            exitGo.AddComponent<LevelExit>();
            Place("Feixe_de_Luz", new Vector3(0, Y0 + 3.9f, 115.5f));
            PropFactory.AddLight(exitGo, "LuzDoFim", new Vector3(0, 2f, 2.5f), new Color(1f, 0.95f, 0.8f), 12f, 4f);
            Place("Capim", new Vector3(-1f, Y0, 116f));
            Place("Capim", new Vector3(1f, Y0, 115f));
            Place("Samambaia", new Vector3(0.8f, Y0, 117.2f));
            Place("Cortina_de_Trepadeiras", new Vector3(0f, Y0 + 1f, 117.85f), 180f);
        }

        // ================================================================== Inventário

        /// <summary>
        /// Conta os assets do Nível 1 por categoria (etiquetas AssetCategory) e grava o relatório em Docs.
        /// </summary>
        private static void WriteInventory()
        {
            AssetCategory[] all = Object.FindObjectsByType<AssetCategory>(FindObjectsSortMode.None);
            var byCat = all.GroupBy(a => a.category).OrderBy(g => g.Key.ToString());
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Inventário de assets — Nível 1 (A Entrada do Templo)");
            sb.AppendLine();
            sb.AppendLine("Gerado automaticamente pelo menu *Treasure Hunter > Construir Nível 1*.");
            sb.AppendLine();
            sb.AppendLine("- Instâncias de assets na cena: **" + all.Length + "**");
            sb.AppendLine("- Assets distintos (prefabs) usados: **" + all.Select(a => a.displayName).Distinct().Count() + "**");
            sb.AppendLine("- Categorias: **" + byCat.Count() + "**");
            sb.AppendLine("- Prefabs gerados: " + AssetDatabase.FindAssets("t:Prefab", new[] { BuilderCore.PrefabsPath }).Length);
            sb.AppendLine("- Arquivos de áudio: " + Directory.GetFiles(BuilderCore.AudioPath, "*.wav").Length);
            Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            if (terrains.Length > 0)
            {
                if (string.IsNullOrEmpty(TerrainBuilder.Report)) TerrainBuilder.RefreshReport();
                TerrainData td = terrains[0].terrainData;
                sb.AppendLine();
                sb.AppendLine("## Terreno — assets usados");
                sb.AppendLine();
                sb.AppendLine("- Camadas de textura: " + td.terrainLayers.Length + " · tipos de grama/flores: " + td.detailPrototypes.Length +
                              " · tipos de árvore: " + td.treePrototypes.Length + " · árvores plantadas no terreno: " + td.treeInstanceCount);
                sb.Append(TerrainBuilder.Report);
            }
            sb.AppendLine();
            sb.AppendLine("| Categoria | Instâncias | Assets distintos |");
            sb.AppendLine("|---|---|---|");
            foreach (var g in byCat)
            {
                string names = string.Join(", ", g.Select(a => a.displayName).Distinct().OrderBy(n => n));
                sb.AppendLine("| " + g.Key + " | " + g.Count() + " | " + names + " |");
            }
            File.WriteAllText(BuilderCore.DocsPath + "/Inventario_Nivel1.md", sb.ToString(), Encoding.UTF8);
            Debug.Log("[Treasure Hunter] Nível 1: " + all.Length + " instâncias de assets em " + byCat.Count() + " categorias.");
        }
    }
}
