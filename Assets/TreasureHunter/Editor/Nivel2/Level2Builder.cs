using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Gerador do Nível 2 — As Catacumbas de Añang (menu "Treasure Hunter > Construir Nível 2").
    /// Cria todos os assets do nível numa pasta própria (Generated_Nivel2), monta a cena Level2 com três salas de
    /// combate ligadas por passagens, a cena provisória do Nível 3, e liga o fim do Nível 1 e o menu principal ao
    /// Nível 2 — sem regenerar nem alterar os assets do Nível 1.
    /// </summary>
    public static class Level2Builder
    {
        private const string LevelTitle = "Nível 2 — As Catacumbas de Añang";

        private enum Side { South, North, West, East }

        /// <summary>Abertura (passagem) numa parede.</summary>
        private struct Opening
        {
            public Side side; public float center; public float width; public float height;
            public Opening(Side side, float center, float width, float height)
            {
                this.side = side; this.center = center; this.width = width; this.height = height;
            }
        }

        private static readonly Dictionary<string, Mesh> blockMeshes = new Dictionary<string, Mesh>();
        private static Transform currentParent;
        private static Transform geometryRoot;
        private static int blockCounter;
        private static GameManager gm;
        private static UIManager ui;
        private static GameObject player;
        private static PlayerCombat combat;

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        // ================================================================== Menus

        /// <summary>
        /// Gera o Nível 2 (assets, cena, Nível 3 provisório e ligações com o Nível 1 e o menu).
        /// </summary>
        [MenuItem("Treasure Hunter/Construir Nível 2 (combate)", priority = 2)]
        public static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!Build()) return;
            EditorUtility.DisplayDialog("Treasure Hunter",
                "Nível 2 gerado com sucesso!\n\n" +
                (Level3Builder.IsGenerated ? "Cena: Level2 (catacumbas); o Nível 3 já existente foi mantido.\n" : "Cenas: Level2 (catacumbas) e Level3 (provisória).\n") +
                "O fim do Nível 1 agora tem o botão \"Próximo nível\" e o menu principal tem o botão \"Nível 2\".\n" +
                "Inventário: Assets/TreasureHunter/Docs/Inventario_Nivel2.md\n\n" +
                "Opcional: Treasure Hunter > Assar iluminação do Nível 2.", "OK");
        }

        /// <summary>
        /// Abre a cena do Nível 2 (atalho).
        /// </summary>
        [MenuItem("Treasure Hunter/Abrir cena do Nível 2", priority = 22)]
        public static void OpenLevel2()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(L2Core.Level2ScenePath);
        }

        /// <summary>
        /// Assa a iluminação do Nível 2 (luzes Baked e a parte indireta das Mixed). Opcional: sem assar,
        /// as luzes Realtime e Mixed já iluminam a cena; as Baked e de Área só aparecem depois de assar.
        /// </summary>
        [MenuItem("Treasure Hunter/Assar iluminação do Nível 2 (opcional)", priority = 40)]
        public static void BakeLighting()
        {
            if (!File.Exists(L2Core.Level2ScenePath))
            {
                EditorUtility.DisplayDialog("Treasure Hunter", "Gere o Nível 2 primeiro.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(L2Core.Level2ScenePath);
            if (Lightmapping.BakeAsync())
                EditorUtility.DisplayDialog("Treasure Hunter", "O cozimento da luz começou (barra de progresso no canto inferior direito).\nQuando terminar, salve a cena (Ctrl+S).", "OK");
        }

        /// <summary>
        /// Gera tudo. Também é chamado pelo gerador do Nível 1 quando o Nível 2 já existia
        /// (os prefabs do Nível 2 reaproveitam efeitos e itens do Nível 1).
        /// </summary>
        /// <returns>true se terminou.</returns>
        public static bool Build()
        {
            if (L2Core.L1("John_Craft") == null || !File.Exists(LevelBuilder.Level1ScenePath))
            {
                EditorUtility.DisplayDialog("Treasure Hunter",
                    "O Nível 2 reaproveita o personagem, os efeitos e os itens do Nível 1.\n\n" +
                    "Gere primeiro: Treasure Hunter > Construir Nível 1 (gerar tudo).", "OK");
                return false;
            }
            try
            {
                Progress("Pastas, tags e camadas", 0.02f);
                blockMeshes.Clear();
                blockCounter = 0;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                L2Core.UseFolders();
                L2Core.PrepareFolders();
                L2Core.EnsureTagsAndLayers();
                L2Core.ConfigureAudioImport();
                UIFactory.PrepareShared();

                Progress("Texturas e materiais (com mapas normais)", 0.08f);
                L2Palette.Build();
                Progress("Partículas", 0.15f);
                L2Particles.BuildAll();
                Progress("Cenário", 0.22f);
                L2Props.BuildAll();
                Progress("Armas, projéteis, armadilhas e quebráveis", 0.32f);
                L2Combat.BuildAll();
                Progress("Esqueletos, John e animações", 0.42f);
                L2Characters.BuildAll();
                L2UI.BuildSprites();
                AssetDatabase.SaveAssets();

                Progress("Cena do Nível 2", 0.55f);
                BuildScene();

                // A tela provisória só é criada enquanto o Nível 3 de verdade não existe.
                Progress("Cena provisória do Nível 3", 0.85f);
                if (!Level3Builder.IsGenerated) L2UI.BuildLevel3Placeholder();
                EditorBuildSettings.scenes = LevelBuilder.BuildScenes();

                Progress("Ligando o Nível 1 e o menu ao Nível 2", 0.92f);
                L2UI.PatchLevel1();
                L2UI.PatchMainMenu();
                EditorBuildSettings.scenes = LevelBuilder.BuildScenes();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorSceneManager.OpenScene(L2Core.Level2ScenePath);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            // O Nível 4 reaproveita o John armado, a bala e as pedras do Nível 2: se ele já existia, é refeito
            // para continuar apontando para os prefabs recém-criados.
            if (Level4Builder.IsGenerated)
            {
                Level4Builder.Build();
                EditorSceneManager.OpenScene(L2Core.Level2ScenePath);
            }
            return true;
        }

        /// <summary>
        /// Barra de progresso do editor.
        /// </summary>
        /// <param name="step">Etapa.</param>
        /// <param name="t">Progresso (0 a 1).</param>
        private static void Progress(string step, float t)
        {
            EditorUtility.DisplayProgressBar("Treasure Hunter — gerando o Nível 2", step, t);
        }

        // ================================================================== Cena

        /// <summary>
        /// Monta a cena: iluminação, gerenciadores, jogador, salas, passagens, inventário.
        /// </summary>
        private static void BuildScene()
        {
            SetupLighting();
            geometryRoot = new GameObject("Geometria").transform;
            BuildManagersAndPlayer();

            Progress("Nível 2: corredor de entrada e Sala 1", 0.6f);
            BuildEntrance();
            BuildRoom1();
            Progress("Nível 2: Sala 2 (arqueiros)", 0.68f);
            BuildRoom2();
            BuildBladeCorridor();
            Progress("Nível 2: Sala 3 (guardião)", 0.76f);
            BuildRoom3();
            BuildExit();
            BuildLightProbes();

            currentParent = null;
            GameObject kill = new GameObject("ZonaDeQueda");
            kill.transform.position = V(0, -15f, 48f);
            BoxCollider kc = kill.AddComponent<BoxCollider>();
            kc.isTrigger = true;
            kc.size = V(200f, 4f, 300f);
            kill.AddComponent<KillZone>();

            Progress("Nível 2: inventário de assets", 0.82f);
            WriteInventory();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), L2Core.Level2ScenePath);
        }

        /// <summary>
        /// Ambiente escuro (sem sol), neblina densa e as configurações de luz assada (Baked/Mixed).
        /// </summary>
        private static void SetupLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.sun = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.065f, 0.06f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.015f, 0.013f, 0.012f);
            RenderSettings.fogDensity = 0.045f;

            LightingSettings ls = new LightingSettings { name = "Iluminacao_Nivel2" };
            ls.bakedGI = true;
            ls.realtimeGI = false;
            ls.mixedBakeMode = MixedLightingMode.IndirectOnly;
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            ls.lightmapResolution = 6f;
            ls.lightmapPadding = 2;
            ls.lightmapMaxSize = 1024;
            ls.directionalityMode = LightmapsMode.NonDirectional;
            ls.autoGenerate = false;
            BuilderCore.SaveAsset(ls, L2Core.LightingPath + "/Iluminacao_Nivel2.lighting");
            Lightmapping.lightingSettings = ls;
        }

        /// <summary>
        /// Gerenciadores (jogo, habilidades, áudio, sons de combate, atmosfera, física do combate e NavMesh),
        /// jogador de combate, câmera de ombro e interface.
        /// </summary>
        private static void BuildManagersAndPlayer()
        {
            GameObject managers = new GameObject("Gerenciadores");
            gm = managers.AddComponent<GameManager>();
            gm.levelName = LevelTitle;
            gm.totalArtifacts = 2;
            gm.nextLevelScene = "Level3";
            gm.startObjective = "Pegue o revólver na mesa do corredor [E].";

            SkillManager sm = managers.AddComponent<SkillManager>();
            sm.skills = new List<SkillDefinition>
            {
                new SkillDefinition("corpo_a_corpo", "Corpo a Corpo", "Golpear com a coronhada ou com a faca para economizar balas."),
                new SkillDefinition("pontaria", "Pontaria", "Mirar com calma (botão direito) e acertar a cabeça: dano dobrado."),
                new SkillDefinition("recarga", "Recarga Tática", "Recarregar o tambor no momento certo, longe do perigo."),
                new SkillDefinition("cobertura", "Cobertura", "Usar paredes e pilares para se proteger das flechas e avançar."),
                new SkillDefinition("improviso", "Improviso", "Usar o cenário — pedras, barris e armadilhas — contra os inimigos."),
                new SkillDefinition("estrategia", "Estratégia", "Escolher a ordem dos alvos e vencer um grupo misto com poucas balas.")
            };

            AudioManager am = managers.AddComponent<AudioManager>();
            am.footsteps = new[] { BuilderCore.Clip("footstep_1"), BuilderCore.Clip("footstep_2"), BuilderCore.Clip("footstep_3"), BuilderCore.Clip("footstep_4") };
            am.jump = BuilderCore.Clip("jump");
            am.land = BuilderCore.Clip("land");
            am.hurt = BuilderCore.Clip("hurt");
            am.lever = BuilderCore.Clip("lever");
            am.doorGrind = BuilderCore.Clip("door_grind");
            am.plateClick = BuilderCore.Clip("plate_click");
            am.buttonPress = BuilderCore.Clip("button_press");
            am.rockExplosion = BuilderCore.Clip("rock_explosion");
            am.rumble = BuilderCore.Clip("rumble");
            am.coin = BuilderCore.Clip("coin");
            am.gem = BuilderCore.Clip("gem");
            am.artifact = BuilderCore.Clip("artifact");
            am.skillUnlock = BuilderCore.Clip("skill_unlock");
            am.uiClick = BuilderCore.Clip("ui_click");
            am.levelComplete = BuilderCore.Clip("level_complete");
            am.caveAmbience = L2Core.Clip("dungeon_ambience");
            am.forestAmbience = am.caveAmbience;
            am.music = L2Core.Clip("music_level2");
            am.musicVolume = 0.3f;

            CombatAudio ca = managers.AddComponent<CombatAudio>();
            ca.revolverShot = L2Core.Clip("revolver_shot");
            ca.dryFire = L2Core.Clip("revolver_dry_fire");
            ca.reload = L2Core.Clip("revolver_reload");
            ca.ricochet = L2Core.Clip("bullet_ricochet_1");
            ca.boneHit = L2Core.Clip("bullet_bone_hit");
            ca.pistolWhip = L2Core.Clip("pistol_whip");
            ca.knifeSlash = L2Core.Clip("knife_slash");
            ca.knifeHit = L2Core.Clip("knife_hit");
            ca.swingWhoosh = L2Core.Clip("swing_whoosh");
            ca.stoneThrow = L2Core.Clip("stone_throw");
            ca.stonePickup = L2Core.Clip("stone_pickup");
            ca.stoneImpact = L2Core.Clip("stone_impact");
            ca.bowDraw = L2Core.Clip("bow_draw");
            ca.bowRelease = L2Core.Clip("bow_release");
            ca.arrowImpact = L2Core.Clip("arrow_impact");
            ca.skeletonAlert = L2Core.Clips("skeleton_alert_1", "skeleton_alert_2");
            ca.skeletonAttack = L2Core.Clips("skeleton_attack_1", "skeleton_attack_2");
            ca.skeletonHurt = L2Core.Clips("skeleton_hurt_1", "skeleton_hurt_2");
            ca.skeletonDeath = L2Core.Clips("skeleton_death_1", "skeleton_death_2");
            ca.boneSteps = L2Core.Clips("bone_step_1", "bone_step_2");
            ca.skeletonRise = L2Core.Clip("skeleton_rise");
            ca.bruteRoar = L2Core.Clip("brute_roar");
            ca.axeSwing = L2Core.Clip("axe_swing");
            ca.barrelExplosion = L2Core.Clip("barrel_explosion");
            ca.crateBreak = L2Core.Clip("crate_break");
            ca.spikeTrap = L2Core.Clip("spike_trap");
            ca.pendulum = L2Core.Clip("pendulum_swing");
            ca.gateMove = L2Core.Clip("gate_move");
            ca.knifePickup = L2Core.Clip("knife_pickup");
            ca.weaponPickup = L2Core.Clip("weapon_pickup");
            ca.roomClear = L2Core.Clip("room_clear");
            ca.dummyHit = L2Core.Clip("dummy_hit");

            AtmosphereController atmo = managers.AddComponent<AtmosphereController>();
            atmo.profiles = new[]
            {
                new AtmosphereProfile
                {
                    name = "Catacumbas", ambientColor = RenderSettings.ambientLight, fogColor = RenderSettings.fogColor,
                    fogDensity = RenderSettings.fogDensity, sunEnabled = false, ambience = am.caveAmbience
                }
            };

            managers.AddComponent<CombatSetup>();
            NavMeshRuntimeBaker baker = managers.AddComponent<NavMeshRuntimeBaker>();
            baker.geometryRoot = geometryRoot;
            baker.boundsCenter = V(0f, 4f, 48f);
            baker.boundsSize = V(40f, 16f, 136f);

            // Jogador no corredor de entrada, olhando para a cripta (norte).
            player = (GameObject)PrefabUtility.InstantiatePrefab(L2Characters.PlayerPrefab);
            player.transform.position = V(0f, 0.05f, -10f);
            combat = player.GetComponent<PlayerCombat>();

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = V(0.5f, 2f, -14f);
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            camGo.AddComponent<AudioListener>();
            ThirdPersonCamera tpc = camGo.AddComponent<ThirdPersonCamera>();
            tpc.target = player.transform;
            tpc.shoulderOffset = V(0.45f, 0f, 0f);
            tpc.collisionMask = CombatLayers.CameraMask;

            PlayerController pc = player.GetComponent<PlayerController>();
            pc.cameraTransform = camGo.transform;

            ui = L2UI.BuildHUD(combat);
            gm.player = pc;
            gm.playerHealth = player.GetComponent<PlayerHealth>();
            gm.ui = ui;
        }

        // ================================================================== Corredor de entrada

        /// <summary>
        /// Corredor de entrada: a escada por onde John desceu desabou atrás dele; o revólver está numa mesa
        /// sob uma fenda de luz e a grade da cripta só abre quando ele pega a arma.
        /// </summary>
        private static void BuildEntrance()
        {
            Transform area = Area("Corredor_de_Entrada");
            Corridor("Corredor", -1.75f, 1.75f, -15f, -1f, 5f, true, false);

            Static(Place("Escada_de_Pedra", V(0, 0, -10.75f), 0f));
            Static(Place("Escombros_Grandes", V(0, 2.4f, -13.2f), 15f));
            Place("Escombros_Pequenos", V(0.6f, 0, -9.6f), 40f);
            Place("Escombros_Pequenos", V(-0.9f, 1.6f, -12.3f), 0f);
            Place("Fenda_de_Luz", V(0, 4.95f, -12.5f), 30f);
            Place("FX_WarningDust", V(0, 4.6f, -12.5f));

            // Revólver na mesa, com foco de luz (ponto importante).
            GameObject table = Place("Mesa_do_Revolver", V(1.05f, 0, -5f), -90f);
            Place("Fenda_de_Luz", V(0.9f, 4.95f, -5f), 0f);
            Place("Inscricao_de_Aviso", V(-1.75f, 2.4f, -5f), 90f);
            Place("Tocha_de_Parede", V(-1.75f, 2.3f, -8f), 90f);
            Place("Tocha_de_Parede", V(1.75f, 2.3f, -2.5f), -90f);
            Place("Ossada_Sentada", V(-1.2f, 0, -2.8f), 90f);
            Place("Teia_Grande", V(-1.74f, 4.98f, -1.4f), 0f, 0.9f);
            Place("Teia_de_Aranha", V(1.4f, 4.4f, -14.4f), 0f);
            Place("Fungo_Luminoso", V(1.3f, 0, -9.2f));
            Place("FX2_Gotas", V(0.6f, 4.95f, -7f));
            Place("Pergaminhos", V(-0.9f, 0, -6.4f), 30f);
            Place("Diario_do_Explorador", V(-1.1f, 0.02f, -7.2f), 70f);

            GameObject tablet = Place("Tabua_de_Habilidade", V(-1.2f, 0, -9.5f), 90f);
            SkillTablet st = tablet.GetComponent<SkillTablet>();
            if (st != null)
            {
                st.title = "Inscrição: Os Guardiões";
                st.text = "\"Os mortos de Añang guardam o caminho. Ossos se partem com golpes, mas só a cabeça derruba um guardião depressa. Quem gasta tudo no primeiro inimigo não chega ao último.\"";
            }

            // Grade da cripta: abre quando o revólver é coletado.
            IronGate gateA = Gate("Grade_A_Cripta", V(0, 0, -0.5f), false);
            Transform pickup = table.transform.Find("Revolver_Coleta");
            if (pickup != null) pickup.GetComponent<WeaponPickup>().targets = new SignalReceiver[] { gateA };
            area.name = "Corredor_de_Entrada";
        }

        // ================================================================== Sala 1

        /// <summary>
        /// Sala 1 — Cripta dos Esquecidos: boneco de treino e pedras (área segura ao sul), dois esqueletos com faca
        /// que surgem ao entrar (35% de chance de cada um deixar a faca), sarcófagos como cobertura e espinhos no piso.
        /// </summary>
        private static void BuildRoom1()
        {
            Transform area = Area("Sala1_Cripta_dos_Esquecidos");
            RoomShell("Sala1", -10f, 10f, 0f, 20f, 6f, new Opening(Side.South, 0f, 3.5f, 4f), new Opening(Side.North, 0f, 3.5f, 4f));

            // Área de treino (fora do gatilho da sala).
            Place("Boneco_de_Treino", V(-4.5f, 0, 3.5f), 180f);
            Place("Pilha_de_Pedras", V(-7.5f, 0, 2.5f), 0f).GetComponent<StonePile>().roomIndex = 1;
            Place("Fenda_de_Luz", V(-4.5f, 5.95f, 3.5f), 60f);

            // Cobertura e armadilha.
            Static(Place("Sarcofago", V(-5f, 0, 12f), 0f));
            Static(Place("Sarcofago_Aberto", V(5f, 0, 12f), 0f));
            Place("Armadilha_de_Espinhos", V(0, 0, 9f), 0f).GetComponent<SpikeTrap>().roomIndex = 1;
            Destructible(Place("Barril_Explosivo", V(-8.3f, 0, 13.5f), 10f), 1);
            Destructible(Place("Caixote_Fragil", V(8.2f, 0, 7.6f), 10f), 1);
            Destructible(Place("Caixote_Fragil", V(8.6f, 0, 8.7f), -15f), 1);
            Destructible(Place("Urna_Funeraria", V(-8.8f, 0, 18.6f), 0f), 1);
            Destructible(Place("Urna_Funeraria", V(8.9f, 0, 18.4f), 0f), 1);

            // Paredes e decoração.
            Static(Place("Nicho_de_Ossos", V(-9.8f, 0, 6.5f), 90f));
            Static(Place("Nicho_de_Ossos", V(9.8f, 0, 14f), -90f));
            Place("Estandarte_Rasgado", V(-4.5f, 4.6f, 19.95f), 180f);
            Place("Estandarte_Rasgado", V(4.5f, 4.6f, 19.95f), 180f);
            Place("Inscricao_de_Aviso", V(0, 4.9f, 19.95f), 180f);
            Place("Tocha_de_Parede", V(-3.2f, 2.4f, 0.05f), 0f);
            Place("Tocha_de_Parede", V(3.2f, 2.4f, 0.05f), 0f);
            Place("Tocha_de_Parede", V(-6.5f, 2.4f, 19.95f), 180f);
            Place("Tocha_de_Parede", V(6.5f, 2.4f, 19.95f), 180f);
            Place("Lustre_de_Velas", V(0, 6f, 11f), 0f);
            Static(Place("Candelabro_de_Ferro", V(-8.6f, 0, 4.2f), 0f));
            Static(Place("Candelabro_de_Ferro", V(8.6f, 0, 15.8f), 0f));
            Place("Teia_Grande", V(-9.95f, 5.95f, 19.9f), 0f);
            Place("Teia_Grande", V(9.95f, 5.95f, 19.9f), 180f);
            Place("Teia_de_Aranha", V(9.4f, 5f, 0.6f), 180f);
            Place("Correntes_Penduradas", V(-6f, 6f, 8.5f), 0f);
            Place("Correntes_Penduradas", V(6.5f, 6f, 16.5f), 40f);
            Place("Gaiola_Pendurada", V(7.5f, 6f, 3.5f), 20f);
            Place("Raizes_Secas", V(2.5f, 6f, 17.5f), 0f);
            Place("Raizes_Secas", V(-7f, 6f, 1.5f), 90f);
            Place("Pilha_de_Cranios", V(-8.6f, 0, 16.6f), 30f);
            Place("Ossos", V(2.2f, 0, 6.2f), 20f);
            Place("Cranio", V(-2.3f, 0, 15.4f), 160f);
            Place("Escombros_Pequenos", V(6.6f, 0, 5.8f), 0f);
            Place("Escombros_Pequenos", V(-3f, 0, 18.6f), 70f);
            Place("Porta_Destruida", V(6.8f, 0, 1.6f), 30f);
            Place("Caixote_Quebrado", V(9f, 0, 3f), 20f);
            Place("Cacos_de_Urna", V(-6.6f, 0, 17.5f), 0f);
            Place("Fungo_Luminoso", V(9.2f, 0, 19.2f));
            Place("Fungo_Luminoso", V(-9.2f, 0, 0.8f));
            Place("FX2_Nevoa_Rasteira", V(0, 0.05f, 11f));
            Place("FX2_Gotas", V(6f, 5.9f, 9f));
            Place("FX_DustMotes", V(0, 3f, 10f));
            AreaLight("LuzDeArea_Nicho", V(-9.4f, 3.2f, 6.5f), 90f, new Vector2(2.4f, 2.4f), new Color(0.55f, 0.6f, 0.8f), 1.2f);
            BakedFill("Preenchimento_Sala1", V(0, 3.5f, 10f), 14f, 0.6f);

            Treasures("Tesouros_Sala1", 4, V(-9f, 0.5f, 10f), V(9f, 0.5f, 11f), V(0f, 0.5f, 18.5f), V(-2f, 0.5f, 1.2f), V(4f, 0.5f, 18.6f), V(-9.2f, 0.5f, 2f));

            // Combate: dois esqueletos com faca (variações sorteadas), gatilho a partir da metade da sala.
            CombatRoom room = CombatRoomAt("Combate_Sala1", 1, V(0, 2f, 12.5f), V(19f, 4f, 13f), V(0, 0.05f, 2.5f), 0f);
            room.title = LevelTitle;
            room.subtitle = "Sala 1 — Cripta dos Esquecidos";
            room.objective = "Derrote os esqueletos da cripta.";
            room.tutorial = "Botão direito: mirar · Clique: atirar (mire na cabeça!)\n[V] coronhada: não gasta balas · [R] recarregar\nAtraia-os para os espinhos ou atire no barril vermelho.";
            room.line = "Os mortos estão se levantando!";
            room.clearedObjective = "A grade se abriu. Siga para o norte — e cuide da munição.";
            room.clearedLine = "Nada mal. Espero que as balas durem.";
            room.gatesOnClear = new SignalReceiver[] { Gate("Grade_B_Sala1", V(0, 0, 20.5f), false) };
            room.closeEntrance = false;
            room.groups = new[]
            {
                Group("Esqueletos com faca", new[] { "Esqueleto_Faca", "Esqueleto_Faca_Elmo" }, 2, true, room.transform,
                    P(-5f, 15.5f, 180f), P(5f, 15.5f, 180f), P(0f, 16.8f, 180f), P(-7.5f, 9.5f, 150f), P(7.5f, 9.5f, 210f))
            };
            area.name = "Sala1_Cripta_dos_Esquecidos";
        }

        // ================================================================== Sala 2

        /// <summary>
        /// Sala 2 — Galeria dos Arqueiros: três arqueiros que mantêm distância, procuram pontos de tiro e tentam
        /// flanquear; meias-paredes, pilares, altar e barricadas servem de cobertura para os dois lados.
        /// </summary>
        private static void BuildRoom2()
        {
            Transform area = Area("Sala2_Galeria_dos_Arqueiros");
            Corridor("Passagem1", -1.75f, 1.75f, 21f, 28f, 4.5f, false, false);
            Place("Tocha_de_Parede", V(-1.75f, 2.3f, 24.5f), 90f);
            Place("Teia_de_Aranha", V(1.3f, 3.9f, 26.5f), 0f);
            Place("Raizes_Secas", V(0.5f, 4.5f, 23f), 0f);

            RoomShell("Sala2", -14f, 14f, 29f, 55f, 7f, new Opening(Side.South, 0f, 3.5f, 4f), new Opening(Side.North, 0f, 3.5f, 4f));

            // Coberturas (centro da sala) — servem ao jogador e aos arqueiros.
            Static(Place("Meia_Parede", V(-6f, 0, 34f), 0f));
            Static(Place("Meia_Parede", V(6f, 0, 34f), 0f));
            Static(Place("Pilar_Rachado", V(-11f, 0, 39f), 0f));
            Static(Place("Meia_Parede", V(0f, 0, 39f), 0f));
            Static(Place("Pilar_Rachado", V(11f, 0, 39f), 45f));
            Static(Place("Altar_Profanado", V(-6f, 0, 43.5f), 0f));
            Static(Place("Barricada", V(6f, 0, 43.5f), 180f));
            Static(Place("Meia_Parede", V(-10f, 0, 48f), 90f));
            Static(Place("Pilar_Desabado", V(0f, 0, 48f), 90f));
            Static(Place("Meia_Parede", V(10f, 0, 48f), 90f));
            Static(Place("Barricada", V(-4.5f, 0, 52.2f), 180f));
            Static(Place("Meia_Parede", V(4.5f, 0, 52.2f), 0f));
            Static(Place("Escombros_Grandes", V(12.2f, 0, 53.2f), 30f));
            Static(Place("Armeiro_Enferrujado", V(-13.6f, 0, 41f), 90f));
            Static(Place("Mesa_Quebrada", V(12.8f, 0, 34.5f), -90f));

            Destructible(Place("Barril_Explosivo", V(-8.5f, 0, 51f), 0f), 2);
            Destructible(Place("Barril_Explosivo", V(9.2f, 0, 37.4f), 30f), 2);
            Destructible(Place("Caixote_Fragil", V(12.6f, 0, 30.8f), 0f), 2);
            Destructible(Place("Caixote_Fragil", V(-13.1f, 0, 37.8f), 25f), 2);
            Destructible(Place("Urna_Funeraria", V(-13.2f, 0, 53.6f), 0f), 2);
            Destructible(Place("Urna_Funeraria", V(13.2f, 0, 46.2f), 0f), 2);

            // Iluminação: focos nas coberturas centrais, candelabros nos pontos dos arqueiros, tochas nas paredes.
            Place("Fenda_de_Luz", V(0f, 6.95f, 39f), 0f);
            Place("Fenda_de_Luz", V(-6f, 6.95f, 43.5f), 45f);
            Place("Lustre_de_Velas", V(0f, 7f, 47f), 0f);
            Static(Place("Candelabro_de_Ferro", V(-12.6f, 0, 50.4f), 0f));
            Static(Place("Candelabro_de_Ferro", V(12.6f, 0, 50.4f), 0f));
            Place("Braseiro_Caido", V(-12f, 0, 31.5f), 30f);
            Place("Tocha_de_Parede", V(-4.2f, 2.4f, 29.05f), 0f);
            Place("Tocha_de_Parede", V(4.2f, 2.4f, 29.05f), 0f);
            Place("Tocha_de_Parede", V(-7f, 2.6f, 54.95f), 180f);
            Place("Tocha_de_Parede", V(7f, 2.6f, 54.95f), 180f);
            Place("Tocha_de_Parede", V(-13.95f, 2.4f, 36f), 90f);
            Place("Tocha_de_Parede", V(13.95f, 2.4f, 42f), -90f);
            AreaLight("LuzDeArea_Altar", V(-6f, 3.5f, 43.5f), 0f, new Vector2(2.4f, 1.2f), new Color(1f, 0.55f, 0.35f), 1.5f, true);
            BakedFill("Preenchimento_Sala2", V(0, 4f, 42f), 18f, 0.6f);

            // Decoração.
            Static(Place("Nicho_de_Ossos", V(13.8f, 0, 40f), -90f));
            Static(Place("Nicho_de_Ossos", V(-13.8f, 0, 49f), 90f));
            Place("Estandarte_Rasgado", V(-3.6f, 5.5f, 54.95f), 180f);
            Place("Estandarte_Rasgado", V(3.6f, 5.5f, 54.95f), 180f);
            Place("Inscricao_de_Aviso", V(0, 5f, 54.95f), 180f);
            Place("Escudo_Caido", V(5f, 0, 31.4f), 30f);
            Place("Pergaminhos", V(-5f, 0, 45.6f), 0f);
            Place("Correntes_Penduradas", V(-5f, 7f, 37f), 0f);
            Place("Correntes_Penduradas", V(6f, 7f, 50f), 70f);
            Place("Gaiola_Pendurada", V(-9f, 7f, 46.5f), 0f);
            Place("Teia_Grande", V(-13.95f, 6.95f, 54.9f), 0f);
            Place("Teia_Grande", V(13.95f, 6.95f, 54.9f), 180f);
            Place("Teia_Grande", V(-13.95f, 6.95f, 29.1f), 0f, 0.8f);
            Place("Pilha_de_Cranios", V(9.5f, 0, 53.8f), 0f);
            Place("Ossada_Sentada", V(-13.4f, 0, 35f), 90f);
            Place("Cacos_de_Urna", V(2.5f, 0, 44.5f), 0f);
            Place("Escombros_Pequenos", V(-2.4f, 0, 31.5f), 0f);
            Place("Escombros_Pequenos", V(8.4f, 0, 47.4f), 120f);
            Place("Ossos", V(-8.6f, 0, 40.6f), 0f);
            Place("Cranio", V(3.2f, 0, 41f), 200f);
            Place("Vaso_Grande", V(13.2f, 0, 29.8f), 0f);
            Place("Vaso_Pequeno", V(-13.3f, 0, 29.8f), 0f);
            Place("Raizes_Secas", V(-3f, 7f, 33f), 0f);
            Place("Raizes_Secas", V(9f, 7f, 44f), 60f);
            Place("Fungo_Luminoso", V(-13.2f, 0, 39.2f));
            Place("Fungo_Luminoso", V(13.3f, 0, 52f));
            Place("FX2_Nevoa_Rasteira", V(0, 0.05f, 36f));
            Place("FX2_Nevoa_Rasteira", V(0, 0.05f, 49f));
            Place("FX2_Gotas", V(-9f, 6.9f, 33f));
            Place("FX_DustMotes", V(0, 3.5f, 43f));

            // Artefato no altar (exige se expor às flechas).
            Place("Adaga_Cerimonial", V(-6f, 1.1f, 43.5f), 0f);
            Treasures("Tesouros_Sala2", 5, V(-12.5f, 0.5f, 33f), V(12.5f, 0.5f, 41f), V(-1.5f, 0.5f, 50.5f), V(8f, 0.5f, 53.5f), V(-9.5f, 0.5f, 53.5f), V(1.5f, 0.5f, 36.5f), V(13f, 0.5f, 49f));

            // Combate: três arqueiros (variações sorteadas), pontos de tiro espalhados para flanquear.
            CombatRoom room = CombatRoomAt("Combate_Sala2", 2, V(0, 2f, 43f), V(27f, 4f, 23f), V(0, 0.05f, 30.2f), 0f);
            room.title = LevelTitle;
            room.subtitle = "Sala 2 — Galeria dos Arqueiros";
            room.objective = "Derrote os três arqueiros. Use as coberturas!";
            room.tutorial = "Flechas fazem curva e podem ser desviadas: fique atrás das meias-paredes e pilares.\nOs arqueiros trocam de posição e tentam flanquear — avance de cobertura em cobertura.";
            room.line = "Arqueiros! Preciso de cobertura.";
            room.clearedObjective = "Atravesse o corredor da lâmina e entre no salão.";
            room.clearedLine = "Essas paredes salvaram minha pele.";
            room.skillOnClear = "cobertura";
            room.entranceGate = Gate("Grade_Sala2_Entrada", V(0, 0, 28.5f), true);
            room.closeEntrance = true;
            room.gatesOnClear = new SignalReceiver[] { Gate("Grade_C_Sala2", V(0, 0, 55.5f), false) };
            room.attackPoints = Points("PontosDeTiro", room.transform,
                P(-11f, 52f), P(-7f, 50f), P(-2f, 54f), P(2f, 54f), P(7f, 50f), P(11f, 51.5f),
                P(-12.5f, 44.5f), P(12.5f, 44.5f), P(-8.8f, 41.4f), P(8.8f, 41.4f), P(-3f, 45.6f), P(3f, 45.6f),
                P(-12.5f, 36.5f), P(12.5f, 36.8f));
            room.groups = new[]
            {
                Group("Arqueiros", new[] { "Esqueleto_Arqueiro", "Esqueleto_Arqueiro_Capuz" }, 3, true, room.transform,
                    P(-10f, 53.2f, 180f), P(0f, 53.6f, 180f), P(10f, 53f, 180f), P(-12f, 46.5f, 150f), P(12f, 46.5f, 210f))
            };
            area.name = "Sala2_Galeria_dos_Arqueiros";
        }

        // ================================================================== Corredor da lâmina

        /// <summary>
        /// Passagem entre a Sala 2 e a Sala 3 com a lâmina pendular (física: Rigidbody, HingeJoint e torque).
        /// </summary>
        private static void BuildBladeCorridor()
        {
            Area("Corredor_da_Lamina");
            Corridor("Passagem2", -1.75f, 1.75f, 56f, 64f, 4.5f, false, false);
            Place("Lamina_Pendular", V(0, 4.3f, 60f), 0f);
            Place("Tocha_de_Parede", V(-1.75f, 2.3f, 57.3f), 90f);
            Place("Tocha_de_Parede", V(1.75f, 2.3f, 62.7f), -90f);
            Place("Ossos", V(0.9f, 0, 59.2f), 50f);
            Place("Cranio", V(-1f, 0, 61.4f), 120f);
            Place("FX2_Gotas", V(-0.8f, 4.45f, 58f));
            Place("Teia_de_Aranha", V(-1.3f, 3.8f, 63.5f), 0f);
        }

        // ================================================================== Sala 3

        /// <summary>
        /// Sala 3 — Salão do Guardião: sete inimigos mistos (3 facas, 2 arqueiros nas plataformas e 2 brutamontes
        /// adormecidos que despertam com barulho), estátua central, teto instável com alavanca, espinhos e barris.
        /// Vencer abre a passagem para o Nível 3.
        /// </summary>
        private static void BuildRoom3()
        {
            Transform area = Area("Sala3_Salao_do_Guardiao");
            RoomShell("Sala3", -15f, 15f, 65f, 95f, 8f, new Opening(Side.South, 0f, 3.5f, 4f), new Opening(Side.North, 0f, 3.5f, 4f));

            // Plataformas elevadas dos arqueiros (cantos noroeste e nordeste) com rampas de 15°.
            Transform prev = currentParent;
            currentParent = Group("Plataformas", geometryRoot);
            Block("Plataforma", V(-15f, 0, 87f), V(-9f, 1.6f, 95f), L2Palette.StoneDark);
            Block("Plataforma", V(9f, 0, 87f), V(15f, 1.6f, 95f), L2Palette.StoneDark);
            Ramp("Rampa", V(-6f, 0.8f, 90.5f), 6f, 3f, 1.6f, true);
            Ramp("Rampa", V(6f, 0.8f, 90.5f), 6f, 3f, 1.6f, false);
            currentParent = prev;
            Static(Place("Meia_Parede", V(-12f, 1.6f, 87.3f), 0f));
            Static(Place("Meia_Parede", V(12f, 1.6f, 87.3f), 0f));

            // Cobertura central e laterais.
            Static(Place("Estatua_Decapitada", V(0, 0, 80f), 0f));
            Static(Place("Meia_Parede", V(-6.5f, 0, 71.5f), 0f));
            Static(Place("Meia_Parede", V(6.5f, 0, 71.5f), 0f));
            Static(Place("Pilar_Rachado", V(-9f, 0, 85.6f), 0f));
            Static(Place("Pilar_Rachado", V(9f, 0, 85.6f), 30f));
            Static(Place("Sarcofago", V(-11.5f, 0, 80.5f), 0f));
            Static(Place("Sarcofago_Aberto", V(11.5f, 0, 80.5f), 180f));
            Static(Place("Pilar_Desabado", V(0f, 0, 90.8f), 90f));

            // Armadilhas: espinhos, teto instável (alavanca na parede oeste) e barris explosivos.
            Place("Armadilha_de_Espinhos", V(-4.2f, 0, 76.5f), 0f).GetComponent<SpikeTrap>().roomIndex = 3;
            Place("Armadilha_de_Espinhos", V(4.2f, 0, 76.5f), 0f).GetComponent<SpikeTrap>().roomIndex = 3;
            GameObject ceiling = Place("Teto_Instavel", V(0, 7.95f, 85.2f), 0f);
            CeilingCollapseTrap collapse = ceiling.GetComponent<CeilingCollapseTrap>();
            collapse.roomIndex = 3;
            GameObject leverGo = Place("Alavanca_Enferrujada", V(-11f, 0, 65.45f), 0f);
            TrapLever lever = leverGo.GetComponent<TrapLever>();
            lever.roomIndex = 3;
            lever.targets = new SignalReceiver[] { collapse };
            Place("Fenda_de_Luz", V(-11f, 7.95f, 66.2f), 0f);
            Destructible(Place("Barril_Explosivo", V(-7.6f, 0, 83.4f), 0f), 3);
            Destructible(Place("Barril_Explosivo", V(7.8f, 0, 77.2f), 0f), 3);
            Destructible(Place("Barril_Explosivo", V(1.8f, 0, 87.4f), 0f), 3);
            Destructible(Place("Caixote_Fragil", V(-13.6f, 0, 85.8f), 0f), 3);
            Destructible(Place("Caixote_Fragil", V(13.6f, 0, 66.4f), 0f), 3);
            Destructible(Place("Urna_Funeraria", V(-13.8f, 0, 66.2f), 0f), 3);
            Destructible(Place("Urna_Funeraria", V(13.8f, 0, 84.6f), 0f), 3);
            Place("Pilha_de_Pedras", V(-8f, 0, 66.8f), 0f).GetComponent<StonePile>().roomIndex = 3;
            Place("Pilha_de_Pedras", V(8f, 0, 66.8f), 0f).GetComponent<StonePile>().roomIndex = 3;

            // Iluminação.
            Place("Lustre_de_Velas", V(0, 8f, 80f), 0f);
            Place("Fenda_de_Luz", V(0, 7.95f, 92.5f), 0f);
            Static(Place("Candelabro_de_Ferro", V(-14f, 1.6f, 94f), 0f));
            Static(Place("Candelabro_de_Ferro", V(14f, 1.6f, 94f), 0f));
            Place("Braseiro_Caido", V(3.5f, 0, 69.8f), 200f);
            Place("Tocha_de_Parede", V(-5f, 2.5f, 65.05f), 0f);
            Place("Tocha_de_Parede", V(5f, 2.5f, 65.05f), 0f);
            Place("Tocha_de_Parede", V(-14.95f, 2.6f, 79.5f), 90f);
            Place("Tocha_de_Parede", V(14.95f, 2.6f, 79.5f), -90f);
            Place("Tocha_de_Parede", V(-3.2f, 3f, 94.95f), 180f);
            Place("Tocha_de_Parede", V(3.2f, 3f, 94.95f), 180f);
            AreaLight("LuzDeArea_Estatua", V(0, 5.5f, 80f), 0f, new Vector2(3f, 3f), new Color(0.6f, 0.7f, 1f), 1.4f, true);
            BakedFill("Preenchimento_Sala3", V(0, 5f, 80f), 20f, 0.6f);

            // Decoração.
            Static(Place("Nicho_de_Ossos", V(-14.8f, 0, 69.5f), 90f));
            Static(Place("Nicho_de_Ossos", V(14.8f, 0, 74f), -90f));
            Place("Estandarte_Rasgado", V(-7f, 6f, 94.95f), 180f);
            Place("Estandarte_Rasgado", V(7f, 6f, 94.95f), 180f);
            Place("Inscricao_de_Aviso", V(0, 5.2f, 94.95f), 180f);
            Place("Inscricao_de_Aviso", V(-11f, 3.1f, 65.05f), 0f);
            Place("Gaiola_Pendurada", V(6f, 8f, 70f), 0f);
            Place("Correntes_Penduradas", V(-6f, 8f, 76f), 0f);
            Place("Correntes_Penduradas", V(9.5f, 8f, 88f), 30f);
            Place("Teia_Grande", V(-14.95f, 7.95f, 94.9f), 0f);
            Place("Teia_Grande", V(14.95f, 7.95f, 94.9f), 180f);
            Place("Teia_Grande", V(14.95f, 7.95f, 65.1f), 180f, 0.8f);
            Place("Pilha_de_Cranios", V(-12.5f, 1.6f, 94.2f), 0f);
            Place("Pilha_de_Cranios", V(1.6f, 0, 82.6f), 0f);
            Place("Ossada_Sentada", V(14.2f, 0, 77.6f), -90f);
            Place("Escudo_Caido", V(-2.6f, 0, 73.4f), 0f);
            Place("Pergaminhos", V(-12.6f, 1.6f, 90.5f), 0f);
            Place("Escombros_Pequenos", V(-1.8f, 0, 86.2f), 0f);
            Place("Escombros_Pequenos", V(10.4f, 0, 67.5f), 30f);
            Place("Cacos_de_Urna", V(-10f, 0, 67.4f), 0f);
            Place("Raizes_Secas", V(-9f, 8f, 70f), 0f);
            Place("Raizes_Secas", V(3f, 8f, 75f), 30f);
            Place("Fungo_Luminoso", V(-14.2f, 0, 77f));
            Place("Fungo_Luminoso", V(14.3f, 1.6f, 88.4f));
            Place("Cristais_Luminosos", V(-14.2f, 1.6f, 92f), 0f);
            Place("FX2_Almas", V(0, 1.2f, 80f));
            Place("FX2_Nevoa_Rasteira", V(0, 0.05f, 72f));
            Place("FX2_Nevoa_Rasteira", V(0, 0.05f, 86f));
            Place("FX2_Gotas", V(9f, 7.9f, 75f));
            Place("FX_DustMotes", V(0, 4f, 80f));

            // Artefato numa plataforma dos arqueiros.
            Place("Amuleto_de_Osso", V(13.2f, 1.6f, 93.6f), 0f);
            Treasures("Tesouros_Sala3", 6, V(-14f, 0.5f, 68f), V(14f, 0.5f, 72f), V(-1f, 0.5f, 93.5f), V(-10f, 2.1f, 92.6f), V(10f, 2.1f, 92.6f), V(0f, 0.5f, 70f), V(-13.6f, 0.5f, 88.6f), V(7f, 0.5f, 86f));

            // Combate misto: prioridades diferentes por arma (facas avançam, arqueiros no alto, brutamontes dormentes).
            CombatRoom room = CombatRoomAt("Combate_Sala3", 3, V(0, 2.5f, 81f), V(29f, 5f, 26f), V(0, 0.05f, 66.5f), 0f);
            room.title = LevelTitle;
            room.subtitle = "Sala 3 — Salão do Guardião";
            room.objective = "Derrote os 7 guardiões para abrir a passagem.";
            room.tutorial = "Facas avançam rápido, arqueiros atiram do alto e os brutamontes (olhos laranja) dormem junto à saída.\nTiros fazem barulho e acordam os brutamontes! A alavanca perto da entrada derruba o teto rachado.";
            room.line = "É uma emboscada... preciso escolher bem cada tiro.";
            room.clearedObjective = "A passagem para as profundezas está aberta. Siga para o norte!";
            room.clearedLine = "Consegui! O caminho para o coração do templo está livre.";
            room.skillOnClear = "estrategia";
            room.entranceGate = Gate("Grade_D_Sala3_Entrada", V(0, 0, 64.5f), true);
            room.closeEntrance = true;
            room.gatesOnClear = new SignalReceiver[] { Gate("Grade_E_Saida", V(0, 0, 95.5f), false) };
            room.attackPoints = Points("PontosDeTiro", room.transform,
                P(-11f, 89f, 180f, 1.6f), P(-13.6f, 89.4f, 180f, 1.6f), P(-12f, 93.4f, 180f, 1.6f), P(-10f, 91.6f, 180f, 1.6f),
                P(11f, 89f, 180f, 1.6f), P(13.6f, 89.4f, 180f, 1.6f), P(12f, 93.4f, 180f, 1.6f), P(10f, 91.6f, 180f, 1.6f),
                P(-1.5f, 93.4f), P(1.5f, 93.4f));
            room.groups = new[]
            {
                Group("Esqueletos com faca", new[] { "Esqueleto_Faca", "Esqueleto_Faca_Elmo" }, 3, true, room.transform,
                    P(-6f, 85f, 180f), P(6f, 85f, 180f), P(0f, 88f, 180f), P(-3f, 84f, 180f), P(3f, 84f, 180f)),
                Group("Arqueiros", new[] { "Esqueleto_Arqueiro", "Esqueleto_Arqueiro_Capuz" }, 2, true, room.transform,
                    P(-12f, 91f, 180f, 1.6f), P(12f, 91f, 180f, 1.6f), P(-13f, 93.5f, 180f, 1.6f), P(13f, 93.5f, 180f, 1.6f)),
                Group("Brutamontes", new[] { "Esqueleto_Brutamontes", "Esqueleto_Brutamontes_Elmo" }, 2, false, room.transform,
                    P(-6f, 93.4f, 180f), P(6f, 93.4f, 180f), P(-2.4f, 93.6f, 180f), P(2.4f, 93.6f, 180f))
            };
            area.name = "Sala3_Salao_do_Guardiao";
        }

        // ================================================================== Saída

        /// <summary>
        /// Corredor de saída com a passagem para o Nível 3 (CombatLevelExit mostra o resumo do combate).
        /// </summary>
        private static void BuildExit()
        {
            Area("Saida_para_o_Nivel3");
            Corridor("CorredorDeSaida", -1.75f, 1.75f, 96f, 112f, 5f, false, true);
            Place("Feixe_de_Luz", V(0, 4.9f, 108.5f));
            Place("FX2_Almas", V(0, 1f, 109.5f));
            Place("FX_DustMotes", V(0, 2.5f, 107f));
            Place("Tocha_de_Parede", V(-1.75f, 2.3f, 99f), 90f);
            Place("Tocha_de_Parede", V(1.75f, 2.3f, 103f), -90f);
            Place("Escombros_Pequenos", V(1f, 0, 110.6f), 0f);
            Place("Cristais_Luminosos", V(-1.2f, 0, 110.8f), 0f);
            Light glow = L2Core.PointLight(currentParent.gameObject, "LuzDasProfundezas", V(0, 2.5f, 110.5f), new Color(0.55f, 0.75f, 1f), 9f, 2.4f, false, LightmapBakeType.Mixed);
            glow.shadows = LightShadows.None;

            GameObject exit = new GameObject("SaidaDoNivel2");
            exit.transform.SetParent(currentParent, false);
            exit.transform.position = V(0, 1.5f, 106f);
            BoxCollider bc = exit.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = V(3.4f, 3f, 2f);
            exit.AddComponent<CombatLevelExit>();
        }

        // ================================================================== Iluminação extra

        /// <summary>
        /// Luz de área retangular (Baked) apontando para baixo ou para a frente — luz indireta suave em pontos importantes.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="pos">Posição.</param>
        /// <param name="yaw">Rotação em Y.</param>
        /// <param name="size">Tamanho do retângulo.</param>
        /// <param name="color">Cor.</param>
        /// <param name="intensity">Intensidade.</param>
        /// <param name="down">true para apontar para baixo.</param>
        private static void AreaLight(string name, Vector3 pos, float yaw, Vector2 size, Color color, float intensity, bool down = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(currentParent, false);
            go.transform.position = pos;
            go.transform.rotation = down ? Quaternion.Euler(90f, yaw, 0f) : Quaternion.Euler(0f, yaw, 0f);
            Light l = go.AddComponent<Light>();
            l.type = LightType.Rectangle;
            l.areaSize = size;
            l.color = color;
            l.intensity = intensity;
            l.range = 10f;
            l.lightmapBakeType = LightmapBakeType.Baked;
        }

        /// <summary>
        /// Luz pontual Baked fraca de preenchimento (luz rebatida da sala, só aparece depois de assar).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="pos">Posição.</param>
        /// <param name="range">Alcance.</param>
        /// <param name="intensity">Intensidade.</param>
        private static void BakedFill(string name, Vector3 pos, float range, float intensity)
        {
            Light l = L2Core.PointLight(currentParent.gameObject, name, pos, new Color(0.75f, 0.68f, 0.6f), range, intensity, false, LightmapBakeType.Baked);
            l.transform.position = pos;
        }

        /// <summary>
        /// Sondas de luz em grade (iluminação assada também chega a John e aos esqueletos, que se movem).
        /// </summary>
        private static void BuildLightProbes()
        {
            GameObject go = new GameObject("SondasDeLuz");
            LightProbeGroup group = go.AddComponent<LightProbeGroup>();
            List<Vector3> probes = new List<Vector3>();
            void Grid(float x0, float x1, float z0, float z1, float step, float top)
            {
                for (float x = x0; x <= x1 + 0.01f; x += step)
                    for (float z = z0; z <= z1 + 0.01f; z += step)
                    {
                        probes.Add(V(x, 0.6f, z));
                        probes.Add(V(x, top, z));
                    }
            }
            Grid(-1f, 1f, -14f, -2f, 4f, 4f);
            Grid(-9f, 9f, 1f, 19f, 6f, 4.5f);
            Grid(-1f, 1f, 22f, 27f, 5f, 3.5f);
            Grid(-13f, 13f, 30f, 54f, 6.5f, 5f);
            Grid(-1f, 1f, 57f, 63f, 6f, 3.5f);
            Grid(-14f, 14f, 66f, 94f, 7f, 5.5f);
            Grid(-1f, 1f, 97f, 111f, 7f, 4f);
            group.probePositions = probes.ToArray();
        }

        // ================================================================== Geometria (blocos, salas, rampas)

        /// <summary>
        /// Cria um grupo para os objetos de uma área e o define como pai atual.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <returns>O grupo.</returns>
        private static Transform Area(string name)
        {
            currentParent = Group(name, null);
            return currentParent;
        }

        /// <summary>
        /// Cria um objeto de agrupamento.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="parent">Pai (nulo = raiz).</param>
        /// <returns>O Transform do grupo.</returns>
        private static Transform Group(string name, Transform parent)
        {
            GameObject g = new GameObject(name);
            if (parent != null) g.transform.SetParent(parent, false);
            return g.transform;
        }

        /// <summary>
        /// Bloco estático de cenário (malha com UV2 para lightmap, colisor e flags estáticas).
        /// Malhas de mesmo tamanho e material são reaproveitadas.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="min">Canto mínimo.</param>
        /// <param name="max">Canto máximo.</param>
        /// <param name="mat">Material.</param>
        /// <returns>O bloco.</returns>
        private static GameObject Block(string name, Vector3 min, Vector3 max, Material mat)
        {
            Vector3 size = max - min;
            GameObject go = BlockMesh(name, size, mat);
            go.transform.position = (min + max) * 0.5f;
            return go;
        }

        /// <summary>
        /// Cria o objeto do bloco com a malha (sem posicionar).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="size">Tamanho.</param>
        /// <param name="mat">Material.</param>
        /// <returns>O objeto.</returns>
        private static GameObject BlockMesh(string name, Vector3 size, Material mat)
        {
            string key = mat.name + "_" + F(size.x) + "x" + F(size.y) + "x" + F(size.z);
            if (!blockMeshes.TryGetValue(key, out Mesh mesh))
            {
                mesh = new VoxelModel().Add(Vector3.zero, size, mat).Build(key, out _);
                Unwrapping.GenerateSecondaryUVSet(mesh);
                BuilderCore.SaveAsset(mesh, L2Core.MeshesPath + "/Level/" + key + ".asset");
                blockMeshes[key] = mesh;
            }
            GameObject go = new GameObject(name + "_" + (++blockCounter));
            go.transform.SetParent(currentParent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<BoxCollider>().size = size;
            go.tag = GameTags.Ground;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
            return go;
        }

        /// <summary>
        /// Rampa inclinada (bloco girado) que sobe da altura 0 até a altura informada.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="topCenter">Centro da superfície superior.</param>
        /// <param name="run">Comprimento horizontal (ao longo de X).</param>
        /// <param name="width">Largura (ao longo de Z).</param>
        /// <param name="rise">Altura vencida.</param>
        /// <param name="upTowardsMinusX">true se a rampa sobe na direção −X.</param>
        private static void Ramp(string name, Vector3 topCenter, float run, float width, float rise, bool upTowardsMinusX)
        {
            const float thickness = 0.5f;
            float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            float length = Mathf.Sqrt(run * run + rise * rise);
            float beta = upTowardsMinusX ? -angle : angle;
            Quaternion rot = Quaternion.Euler(0f, 0f, beta);
            GameObject go = BlockMesh(name, V(length, thickness, width), L2Palette.Floor);
            go.transform.rotation = rot;
            go.transform.position = topCenter - rot * Vector3.up * (thickness * 0.5f);
        }

        /// <summary>
        /// Sala retangular: piso, teto e quatro paredes (1 m de espessura) com aberturas.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="xMin">Limite interno oeste.</param>
        /// <param name="xMax">Limite interno leste.</param>
        /// <param name="zMin">Limite interno sul.</param>
        /// <param name="zMax">Limite interno norte.</param>
        /// <param name="h">Altura interna.</param>
        /// <param name="openings">Aberturas.</param>
        private static void RoomShell(string name, float xMin, float xMax, float zMin, float zMax, float h, params Opening[] openings)
        {
            Shell(name, xMin, xMax, zMin, zMax, h, new HashSet<Side>(), openings);
        }

        /// <summary>
        /// Corredor (paredes laterais, piso e teto; extremidades abertas ou fechadas).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="xMin">Limite oeste.</param>
        /// <param name="xMax">Limite leste.</param>
        /// <param name="zMin">Limite sul.</param>
        /// <param name="zMax">Limite norte.</param>
        /// <param name="h">Altura.</param>
        /// <param name="closedSouth">true para fechar a extremidade sul.</param>
        /// <param name="closedNorth">true para fechar a extremidade norte.</param>
        private static void Corridor(string name, float xMin, float xMax, float zMin, float zMax, float h, bool closedSouth, bool closedNorth)
        {
            HashSet<Side> skip = new HashSet<Side>();
            if (!closedSouth) skip.Add(Side.South);
            if (!closedNorth) skip.Add(Side.North);
            Shell(name, xMin, xMax, zMin, zMax, h, skip);
        }

        /// <summary>
        /// Implementação comum de salas e corredores (blocos filhos da raiz de geometria, usada pelo NavMesh).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="xMin">Limite oeste.</param>
        /// <param name="xMax">Limite leste.</param>
        /// <param name="zMin">Limite sul.</param>
        /// <param name="zMax">Limite norte.</param>
        /// <param name="h">Altura.</param>
        /// <param name="skipped">Lados sem parede.</param>
        /// <param name="openings">Aberturas.</param>
        private static void Shell(string name, float xMin, float xMax, float zMin, float zMax, float h, HashSet<Side> skipped, params Opening[] openings)
        {
            Transform previous = currentParent;
            currentParent = Group(name, geometryRoot);
            Material wall = L2Palette.Stone, floor = L2Palette.Floor, ceiling = L2Palette.Ceiling;

            float fx0 = xMin - (skipped.Contains(Side.West) ? 0 : 1), fx1 = xMax + (skipped.Contains(Side.East) ? 0 : 1);
            float fz0 = zMin - (skipped.Contains(Side.South) ? 0 : 1), fz1 = zMax + (skipped.Contains(Side.North) ? 0 : 1);
            Block("Piso", V(fx0, -1f, fz0), V(fx1, 0f, fz1), floor);
            Block("Teto", V(fx0, h, fz0), V(fx1, h + 1f, fz1), ceiling);

            foreach (Side side in new[] { Side.South, Side.North, Side.West, Side.East })
            {
                if (skipped.Contains(side)) continue;
                List<Opening> ops = new List<Opening>();
                foreach (Opening o in openings) if (o.side == side) ops.Add(o);
                ops.Sort((a, b) => a.center.CompareTo(b.center));

                bool alongX = side == Side.South || side == Side.North;
                float a0 = alongX ? xMin - (skipped.Contains(Side.West) ? 0f : 1f) : zMin;
                float a1 = alongX ? xMax + (skipped.Contains(Side.East) ? 0f : 1f) : zMax;
                float t0, t1;
                switch (side)
                {
                    case Side.South: t0 = zMin - 1f; t1 = zMin; break;
                    case Side.North: t0 = zMax; t1 = zMax + 1f; break;
                    case Side.West: t0 = xMin - 1f; t1 = xMin; break;
                    default: t0 = xMax; t1 = xMax + 1f; break;
                }

                float cursor = a0;
                foreach (Opening o in ops)
                {
                    float s = o.center - o.width * 0.5f, e = o.center + o.width * 0.5f;
                    if (s > cursor + 0.01f) WallPiece(alongX, cursor, s, t0, t1, 0f, h, wall);
                    if (o.height < h) WallPiece(alongX, s, e, t0, t1, o.height, h, wall);
                    cursor = e;
                }
                if (a1 > cursor + 0.01f) WallPiece(alongX, cursor, a1, t0, t1, 0f, h, wall);
            }
            currentParent = previous;
        }

        /// <summary>
        /// Pedaço de parede.
        /// </summary>
        /// <param name="alongX">true se corre ao longo de X.</param>
        /// <param name="a">Início.</param>
        /// <param name="b">Fim.</param>
        /// <param name="t0">Início da espessura.</param>
        /// <param name="t1">Fim da espessura.</param>
        /// <param name="yb">Base.</param>
        /// <param name="yt">Topo.</param>
        /// <param name="mat">Material.</param>
        private static void WallPiece(bool alongX, float a, float b, float t0, float t1, float yb, float yt, Material mat)
        {
            if (yt - yb < 0.01f || b - a < 0.01f) return;
            if (alongX) Block("Parede", V(a, yb, t0), V(b, yt, t1), mat);
            else Block("Parede", V(t0, yb, a), V(t1, yt, b), mat);
        }

        /// <summary>
        /// Formata um número com duas casas e ponto decimal.
        /// </summary>
        /// <param name="v">Valor.</param>
        /// <returns>Texto.</returns>
        private static string F(float v)
        {
            return v.ToString("F2", CultureInfo.InvariantCulture);
        }

        // ================================================================== Colocação de prefabs

        /// <summary>
        /// Instancia um prefab do Nível 2 (ou do Nível 1) no grupo atual.
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="pos">Posição.</param>
        /// <param name="yaw">Rotação em Y.</param>
        /// <param name="scale">Escala uniforme.</param>
        /// <returns>A instância.</returns>
        private static GameObject Place(string id, Vector3 pos, float yaw = 0f, float scale = 1f)
        {
            GameObject prefab = L2Core.Get(id);
            if (prefab == null)
            {
                Debug.LogError("[Treasure Hunter] Prefab não encontrado: " + id);
                GameObject missing = new GameObject("FALTANDO_" + id);
                missing.transform.SetParent(currentParent, false);
                return missing;
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(currentParent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (!Mathf.Approximately(scale, 1f)) go.transform.localScale = Vector3.one * scale;
            return go;
        }

        /// <summary>
        /// Move um prop sólido para a raiz de geometria (entra no NavMesh como obstáculo) e o marca como estático.
        /// </summary>
        /// <param name="go">Instância.</param>
        /// <returns>A mesma instância.</returns>
        private static GameObject Static(GameObject go)
        {
            Transform holder = geometryRoot.Find("Props_Solidos");
            if (holder == null) holder = Group("Props_Solidos", geometryRoot);
            go.transform.SetParent(holder, true);
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>(true))
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        /// <summary>
        /// Define a sala de um objeto quebrável (para ser restaurado ao reiniciar a sala).
        /// </summary>
        /// <param name="go">Instância.</param>
        /// <param name="room">Índice da sala.</param>
        private static void Destructible(GameObject go, int room)
        {
            Destructible d = go.GetComponent<Destructible>();
            if (d != null) d.roomIndex = room;
        }

        /// <summary>
        /// Coloca uma grade de ferro numa abertura.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="pos">Posição (centro da parede).</param>
        /// <param name="startOpen">true se começa aberta.</param>
        /// <returns>A grade.</returns>
        private static IronGate Gate(string name, Vector3 pos, bool startOpen)
        {
            GameObject go = Place("Grade_de_Ferro", pos, 0f);
            go.name = name;
            IronGate g = go.GetComponent<IronGate>();
            g.startOpen = startOpen;
            return g;
        }

        /// <summary>
        /// Gerador de moedas e joias (prefabs do Nível 1) com um vetor de pontos sorteados.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="count">Quantidade.</param>
        /// <param name="points">Pontos possíveis.</param>
        private static void Treasures(string name, int count, params Vector3[] points)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(currentParent, false);
            CollectibleSpawner sp = go.AddComponent<CollectibleSpawner>();
            sp.count = count;
            sp.prefabs = new[] { L2Core.L1("Moeda_de_Ouro"), L2Core.L1("Moeda_de_Ouro"), L2Core.L1("Joia_Vermelha"), L2Core.L1("Joia_Azul"), L2Core.L1("Joia_Verde") };
            Transform[] pts = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                GameObject p = new GameObject("Ponto_" + (i + 1));
                p.transform.SetParent(go.transform, false);
                p.transform.position = points[i];
                pts[i] = p.transform;
            }
            sp.points = pts;
        }

        // ================================================================== Salas de combate

        /// <summary>Ponto (x, z, rotação, altura) de surgimento ou de tiro.</summary>
        private struct Pt
        {
            public Vector3 pos; public float yaw;
        }

        /// <summary>
        /// Atalho para um ponto no chão (ou numa plataforma).
        /// </summary>
        /// <param name="x">X.</param>
        /// <param name="z">Z.</param>
        /// <param name="yaw">Rotação em Y.</param>
        /// <param name="y">Altura do piso.</param>
        /// <returns>O ponto.</returns>
        private static Pt P(float x, float z, float yaw = 180f, float y = 0f)
        {
            return new Pt { pos = V(x, y, z), yaw = yaw };
        }

        /// <summary>
        /// Cria a sala de combate (gatilho + CombatRoom) com o ponto de renascimento fora do gatilho.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="index">Índice da sala.</param>
        /// <param name="center">Centro do gatilho.</param>
        /// <param name="size">Tamanho do gatilho.</param>
        /// <param name="respawn">Ponto de renascimento.</param>
        /// <param name="respawnYaw">Rotação do renascimento.</param>
        /// <returns>A sala.</returns>
        private static CombatRoom CombatRoomAt(string name, int index, Vector3 center, Vector3 size, Vector3 respawn, float respawnYaw)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(currentParent, false);
            go.transform.position = center;
            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = size;
            CombatRoom room = go.AddComponent<CombatRoom>();
            room.roomIndex = index;
            GameObject sp = new GameObject("Renascimento_Sala" + index);
            sp.transform.SetParent(go.transform, false);
            sp.transform.position = respawn;
            sp.transform.rotation = Quaternion.Euler(0f, respawnYaw, 0f);
            room.respawnPoint = sp.transform;
            return room;
        }

        /// <summary>
        /// Monta um grupo de inimigos com o vetor de variações (escolha aleatória) e os pontos de surgimento.
        /// </summary>
        /// <param name="label">Nome do grupo.</param>
        /// <param name="variants">Prefabs possíveis.</param>
        /// <param name="count">Quantidade.</param>
        /// <param name="alerted">true se já surgem atacando.</param>
        /// <param name="parent">Sala.</param>
        /// <param name="points">Pontos de surgimento.</param>
        /// <returns>O grupo.</returns>
        private static CombatRoom.SpawnGroup Group(string label, string[] variants, int count, bool alerted, Transform parent, params Pt[] points)
        {
            return new CombatRoom.SpawnGroup
            {
                label = label,
                variants = variants.Select(L2Core.Get).Where(p => p != null).ToArray(),
                points = Points("Surgimento_" + label.Replace(' ', '_'), parent, points),
                count = count,
                startAlerted = alerted
            };
        }

        /// <summary>
        /// Cria marcadores (filhos de um grupo) a partir de pontos.
        /// </summary>
        /// <param name="name">Nome do grupo.</param>
        /// <param name="parent">Pai.</param>
        /// <param name="points">Pontos.</param>
        /// <returns>Os marcadores.</returns>
        private static Transform[] Points(string name, Transform parent, params Pt[] points)
        {
            Transform holder = Group(name, parent);
            Transform[] result = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                GameObject p = new GameObject("Ponto_" + (i + 1));
                p.transform.SetParent(holder, false);
                p.transform.position = points[i].pos;
                p.transform.rotation = Quaternion.Euler(0f, points[i].yaw, 0f);
                result[i] = p.transform;
            }
            return result;
        }

        // ================================================================== Inventário

        /// <summary>
        /// Conta os assets do Nível 2: instâncias na cena (etiquetas AssetCategory, inclusive dentro de outros prefabs)
        /// e prefabs que surgem durante o jogo (inimigos, projéteis, faca, destroços e efeitos).
        /// </summary>
        private static void WriteInventory()
        {
            AssetCategory[] scene = Object.FindObjectsByType<AssetCategory>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Dictionary<string, AssetCategoryType> distinct = new Dictionary<string, AssetCategoryType>();
            Dictionary<string, int> instances = new Dictionary<string, int>();
            foreach (AssetCategory a in scene)
            {
                distinct[a.displayName] = a.category;
                instances[a.displayName] = instances.TryGetValue(a.displayName, out int n) ? n + 1 : 1;
            }

            // Prefabs referenciados pela cena (inimigos das salas, projéteis, faca, destroços, itens e efeitos
            // que só aparecem durante o jogo). Prefabs gerados mas não usados no Nível 2 não entram na conta.
            HashSet<string> runtime = new HashSet<string>();
            GameObject[] roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            foreach (Object o in EditorUtility.CollectDependencies(roots))
            {
                GameObject prefab = o as GameObject;
                if (prefab == null || !AssetDatabase.Contains(prefab)) continue;
                AssetCategory tag = prefab.GetComponent<AssetCategory>();
                if (tag == null || distinct.ContainsKey(tag.displayName)) continue;
                distinct[tag.displayName] = tag.category;
                runtime.Add(tag.displayName);
            }

            int wavs = Directory.Exists(L2Core.AudioPath) ? Directory.GetFiles(L2Core.AudioPath, "*.wav").Length : 0;
            var byCat = distinct.GroupBy(kv => kv.Value).OrderBy(g => g.Key.ToString());
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Inventário de assets — " + LevelTitle);
            sb.AppendLine();
            sb.AppendLine("Gerado automaticamente pelo menu *Treasure Hunter > Construir Nível 2*.");
            sb.AppendLine();
            sb.AppendLine("- **Assets distintos no nível: " + distinct.Count + "** (meta: 70; repetições não contam)");
            sb.AppendLine("- Instâncias colocadas na cena: " + scene.Length);
            sb.AppendLine("- Prefabs que surgem durante o jogo (inimigos, projéteis, itens, destroços, efeitos): " + runtime.Count);
            sb.AppendLine("- Categorias: " + byCat.Count());
            sb.AppendLine("- Prefabs gerados para o Nível 2: " + AssetDatabase.FindAssets("t:Prefab", new[] { L2Core.PrefabsPath }).Length);
            sb.AppendLine("- Sons do Nível 2: " + wavs + " arquivos .wav");
            sb.AppendLine("- Inimigos: 2 (Sala 1) + 3 (Sala 2) + 7 (Sala 3) = **" + CombatRoomTotal() + "**");
            sb.AppendLine();
            sb.AppendLine("| Categoria | Assets distintos | Lista (× instâncias na cena · *surge no jogo*) |");
            sb.AppendLine("|---|---|---|");
            foreach (var g in byCat)
            {
                IEnumerable<string> names = g.Select(kv => kv.Key).OrderBy(n => n)
                    .Select(n => runtime.Contains(n) ? "*" + n + "*" : n + " ×" + instances[n]);
                sb.AppendLine("| " + g.Key + " | " + g.Count() + " | " + string.Join(", ", names) + " |");
            }
            File.WriteAllText(BuilderCore.DocsPath + "/Inventario_Nivel2.md", sb.ToString(), Encoding.UTF8);
            Debug.Log("[Treasure Hunter] Nível 2: " + distinct.Count + " assets distintos (" + scene.Length + " instâncias na cena).");
        }

        /// <summary>
        /// Soma os inimigos configurados nas salas de combate da cena.
        /// </summary>
        /// <returns>Total de inimigos.</returns>
        private static int CombatRoomTotal()
        {
            int total = 0;
            foreach (CombatRoom r in Object.FindObjectsByType<CombatRoom>(FindObjectsSortMode.None))
                if (r.groups != null) foreach (CombatRoom.SpawnGroup g in r.groups) total += g.count;
            return total;
        }
    }
}
