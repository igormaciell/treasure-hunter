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
    /// Gerador do Nível 4 — As Galerias dos Ogros (menu "Treasure Hunter > Construir Nível 4").
    /// Cavernas naturais escuras ligadas por fendas estreitas (só agachado): a entrada, três percursos de
    /// furtividade com 1, 2 e 3 ogros (Grukk, Morg e Brann — exatamente três no nível, que avançam junto com
    /// John de um percurso para o outro), coberturas, esconderijos, nichos seguros, chão barulhento, chamas que
    /// podem ser apagadas e a saída para o Nível 5. Os assets ficam em Generated_Nivel4; reaproveita o John
    /// armado do Nível 2 (revólver e pedras) e itens do Nível 1.
    /// </summary>
    public static class Level4Builder
    {
        public const string Root = BuilderCore.Root + "/Generated_Nivel4";
        public const string AudioFolder = BuilderCore.AudioPath + "/Nivel4";
        public const string Level4ScenePath = BuilderCore.ScenesPath + "/Level4.unity";
        public const string Level5ScenePath = BuilderCore.ScenesPath + "/Level5.unity";
        private const string LevelTitle = "Nível 4 — As Galerias dos Ogros";

        /// <summary>O Nível 4 de verdade já foi gerado (não só a tela provisória do Nível 3).</summary>
        public static bool IsGenerated => AssetDatabase.IsValidFolder(Root) && File.Exists(Level4ScenePath);

        /// <summary>Um Nível 5 de verdade (futuro) já existe: a tela provisória não é recriada.</summary>
        private static bool Level5Generated => AssetDatabase.IsValidFolder(BuilderCore.Root + "/Generated_Nivel5");

        private static readonly Dictionary<string, Mesh> blockMeshes = new Dictionary<string, Mesh>();
        private static Transform currentParent;
        private static Transform geometryRoot;
        private static int blockCounter;
        private static GameManager gm;
        private static System.Random rng;
        private static OgreAI grukk, morg, brann;
        private static readonly Dictionary<string, PatrolRoute> routes = new Dictionary<string, PatrolRoute>();

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        // ================================================================== Menus

        /// <summary>
        /// Gera o Nível 4 (assets, cena, Nível 5 provisório e botão no menu).
        /// </summary>
        [MenuItem("Treasure Hunter/Construir Nível 4 (furtividade)", priority = 4)]
        public static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!Build()) return;
            EditorUtility.DisplayDialog("Treasure Hunter",
                "Nível 4 gerado com sucesso!\n\n" +
                "Cenas: Level4 (Galerias dos Ogros) e Level5 (provisória).\n" +
                "O menu principal agora tem o botão \"Nível 4\"; o fim do Nível 3 já leva ao Nível 4.\n" +
                "Inventário: Assets/TreasureHunter/Docs/Inventario_Nivel4.md\n\n" +
                "Opcional: Treasure Hunter > Assar iluminação do Nível 4.", "OK");
        }

        /// <summary>
        /// Abre a cena do Nível 4.
        /// </summary>
        [MenuItem("Treasure Hunter/Abrir cena do Nível 4", priority = 24)]
        public static void OpenLevel4()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Level4ScenePath);
        }

        /// <summary>
        /// Assa a iluminação do Nível 4 (opcional).
        /// </summary>
        [MenuItem("Treasure Hunter/Assar iluminação do Nível 4 (opcional)", priority = 42)]
        public static void BakeLighting()
        {
            if (!File.Exists(Level4ScenePath))
            {
                EditorUtility.DisplayDialog("Treasure Hunter", "Gere o Nível 4 primeiro.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Level4ScenePath);
            if (Lightmapping.BakeAsync())
                EditorUtility.DisplayDialog("Treasure Hunter", "O cozimento da luz começou (barra de progresso no canto inferior direito).\nQuando terminar, salve a cena (Ctrl+S).", "OK");
        }

        /// <summary>
        /// Gera tudo. Também é chamado pelo gerador do Nível 2 (o John armado vem de lá).
        /// </summary>
        /// <returns>true se terminou.</returns>
        public static bool Build()
        {
            if (L2Core.L1("John_Craft") == null || !File.Exists(LevelBuilder.Level1ScenePath) || L2Core.L2Prefab("John_Craft_Combate") == null)
            {
                EditorUtility.DisplayDialog("Treasure Hunter",
                    "O Nível 4 reaproveita o John armado do Nível 2 (revólver e pedras) e itens do Nível 1.\n\n" +
                    "Gere primeiro: Treasure Hunter > Construir Nível 1 e Construir Nível 2.", "OK");
                return false;
            }
            try
            {
                Progress("Pastas, tags e camadas", 0.02f);
                blockMeshes.Clear();
                routes.Clear();
                blockCounter = 0;
                rng = new System.Random(404);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                L2Core.UseFolders(Root, AudioFolder);
                L2Core.PrepareFolders();
                L2Core.EnsureTagsAndLayers(StealthTags.All.Concat(CombatTags.All),
                    new[] { CombatLayers.EnemyLayerName, CombatLayers.ProjectileLayerName, CombatLayers.InteractableLayerName });
                CombatLayers.ResetCache();
                L2Core.ConfigureAudioImport();
                UIFactory.PrepareShared();

                Progress("Texturas, normal maps e materiais", 0.08f);
                L4Palette.Build();
                Progress("Partículas", 0.14f);
                L4Particles.BuildAll();
                Progress("Rochas, escombros, caixas, acampamento e vegetação", 0.22f);
                L4Props.BuildAll();
                Progress("Ogros, animações e indicador de alerta", 0.32f);
                L4Ogres.BuildAll();
                Progress("John (furtivo)", 0.4f);
                if (!L4Characters.BuildAll()) throw new System.Exception("John_Craft_Combate (Nível 2) não encontrado.");
                L2UI.BuildSprites();
                L4UI.BuildSprites();
                AssetDatabase.SaveAssets();

                Progress("Cena do Nível 4", 0.5f);
                BuildScene();

                Progress("Cena provisória do Nível 5", 0.9f);
                if (!Level5Generated) L4UI.BuildLevel5Placeholder();
                EditorBuildSettings.scenes = LevelBuilder.BuildScenes();
                Progress("Botão \"Nível 4\" no menu", 0.95f);
                L4UI.PatchMainMenu();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorSceneManager.OpenScene(Level4ScenePath);
            }
            finally
            {
                L2Core.UseFolders();
                EditorUtility.ClearProgressBar();
            }
            return true;
        }

        /// <summary>
        /// Barra de progresso.
        /// </summary>
        /// <param name="step">Etapa.</param>
        /// <param name="t">Progresso.</param>
        private static void Progress(string step, float t)
        {
            EditorUtility.DisplayProgressBar("Treasure Hunter — gerando o Nível 4", step, t);
        }

        // ================================================================== Cena

        /// <summary>
        /// Monta a cena completa.
        /// </summary>
        private static void BuildScene()
        {
            SetupLighting();
            geometryRoot = new GameObject("Geometria").transform;
            BuildManagers(out PlayerCombat combat, out PlayerStealth stealth, out StealthTorch torch);
            Progress("Nível 4: entrada", 0.55f);
            BuildStart();
            Progress("Nível 4: Percurso 1 (introdução)", 0.6f);
            BuildCourse1();
            Progress("Nível 4: Percurso 2 (observação)", 0.66f);
            BuildCourse2();
            Progress("Nível 4: Percurso 3 (o covil)", 0.72f);
            BuildCourse3();
            Progress("Nível 4: ogros, trajetos e trechos", 0.78f);
            BuildOgresAndSectors();
            BuildLightProbes();
            gm.ui = L4UI.BuildHUD(combat, stealth, torch);
            Progress("Nível 4: inventário", 0.85f);
            WriteInventory();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Level4ScenePath);
        }

        /// <summary>
        /// Caverna muito escura (luz ambiente mínima, neblina preta) e configuração de luz assada.
        /// </summary>
        private static void SetupLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.sun = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.055f, 0.05f, 0.06f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.012f, 0.012f, 0.016f);
            RenderSettings.fogDensity = 0.03f;

            LightingSettings ls = new LightingSettings { name = "Iluminacao_Nivel4" };
            ls.bakedGI = true;
            ls.realtimeGI = false;
            ls.mixedBakeMode = MixedLightingMode.IndirectOnly;
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            ls.lightmapResolution = 4f;
            ls.lightmapPadding = 2;
            ls.lightmapMaxSize = 1024;
            ls.directionalityMode = LightmapsMode.NonDirectional;
            ls.autoGenerate = false;
            BuilderCore.SaveAsset(ls, L2Core.LightingPath + "/Iluminacao_Nivel4.lighting");
            Lightmapping.lightingSettings = ls;
        }

        /// <summary>
        /// Gerenciadores (jogo, habilidades, sons, combate, furtividade, NavMesh), jogador e câmera.
        /// </summary>
        /// <param name="combat">Saída: combate do jogador.</param>
        /// <param name="stealth">Saída: furtividade do jogador.</param>
        /// <param name="torch">Saída: tocha.</param>
        private static void BuildManagers(out PlayerCombat combat, out PlayerStealth stealth, out StealthTorch torch)
        {
            GameObject managers = new GameObject("Gerenciadores");
            gm = managers.AddComponent<GameManager>();
            gm.levelName = LevelTitle;
            gm.totalArtifacts = 2;
            gm.nextLevelScene = "Level5";
            gm.startObjective = "Atravesse as galerias sem ser visto pelos ogros.";

            SkillManager sm = managers.AddComponent<SkillManager>();
            sm.skills = new List<SkillDefinition>
            {
                new SkillDefinition("agachar", "Agachar", "Agachado, John faz menos barulho, é menos visível e passa por fendas baixas."),
                new SkillDefinition("sombras", "Senhor das Sombras", "Apagar tochas e braseiros para criar sombras no caminho."),
                new SkillDefinition("distracao", "Distração", "Arremessar pedras longe para atrair o ogro para outro lugar."),
                new SkillDefinition("observacao", "Observação", "Estudar as patrulhas antes de agir: trajetos, pausas e para onde cada ogro olha."),
                new SkillDefinition("travessia", "Travessia Calculada", "Esperar o momento certo para cruzar caminhos patrulhados."),
                new SkillDefinition("furtividade", "Furtividade", "Atravessar as Galerias dos Ogros evitando o confronto."),
                new SkillDefinition("fantasma", "Fantasma", "Chegar à saída sem nunca ser visto por um ogro.")
            };

            AudioManager am = managers.AddComponent<AudioManager>();
            am.footsteps = new[] { BuilderCore.Clip("footstep_1"), BuilderCore.Clip("footstep_2"), BuilderCore.Clip("footstep_3"), BuilderCore.Clip("footstep_4") };
            am.jump = BuilderCore.Clip("jump");
            am.land = BuilderCore.Clip("land");
            am.hurt = BuilderCore.Clip("hurt");
            am.lever = BuilderCore.Clip("lever");
            am.rockExplosion = BuilderCore.Clip("rock_explosion");
            am.rumble = BuilderCore.Clip("rumble");
            am.coin = BuilderCore.Clip("coin");
            am.gem = BuilderCore.Clip("gem");
            am.artifact = BuilderCore.Clip("artifact");
            am.skillUnlock = BuilderCore.Clip("skill_unlock");
            am.uiClick = BuilderCore.Clip("ui_click");
            am.levelComplete = BuilderCore.Clip("level_complete");
            am.torchWhoosh = BuilderCore.Clip("torch_whoosh");
            am.caveAmbience = L2Core.Clip("cave_ambience_ogres");
            am.forestAmbience = am.caveAmbience;
            am.music = L2Core.Clip("music_level4");
            am.musicVolume = 0.28f;

            CombatAudio ca = managers.AddComponent<CombatAudio>();
            ca.revolverShot = L2Clip("revolver_shot");
            ca.dryFire = L2Clip("revolver_dry_fire");
            ca.reload = L2Clip("revolver_reload");
            ca.ricochet = L2Clip("bullet_ricochet_1");
            ca.boneHit = L2Clip("bullet_bone_hit");
            ca.pistolWhip = L2Clip("pistol_whip");
            ca.knifeSlash = L2Clip("knife_slash");
            ca.knifeHit = L2Clip("knife_hit");
            ca.swingWhoosh = L2Clip("swing_whoosh");
            ca.stoneThrow = L2Clip("stone_throw");
            ca.stonePickup = L2Clip("stone_pickup");
            ca.stoneImpact = L2Clip("stone_impact");

            StealthAudio sa = managers.AddComponent<StealthAudio>();
            sa.heartbeat = L2Core.Clip("heartbeat");
            sa.suspiciousStinger = L2Core.Clip("stinger_suspicious");
            sa.spottedStinger = L2Core.Clip("stinger_spotted");
            sa.lostStinger = L2Core.Clip("stinger_lost");
            sa.playerDeath = L2Core.Clip("player_crushed");
            sa.crouchDown = L2Core.Clip("crouch_down");
            sa.standUp = L2Core.Clip("stand_up");
            sa.torchIgnite = L2Core.Clip("torch_ignite");
            sa.torchExtinguish = L2Core.Clip("torch_extinguish");
            sa.gravelSteps = L2Core.Clips("gravel_step_1", "gravel_step_2");
            sa.checkpoint = L2Core.Clip("sector_reached");

            AtmosphereController atmo = managers.AddComponent<AtmosphereController>();
            atmo.profiles = new[]
            {
                new AtmosphereProfile
                {
                    name = "Galerias dos Ogros", ambientColor = RenderSettings.ambientLight, fogColor = RenderSettings.fogColor,
                    fogDensity = RenderSettings.fogDensity, sunEnabled = false, ambience = am.caveAmbience
                }
            };

            NavMeshRuntimeBaker baker = managers.AddComponent<NavMeshRuntimeBaker>();
            baker.geometryRoot = geometryRoot;
            baker.boundsCenter = V(0f, 3.2f, 77f);
            baker.boundsSize = V(54f, 8.4f, 200f);
            baker.agentRadius = 0.65f;
            baker.agentHeight = 3f;
            baker.agentClimb = 0.4f;
            baker.agentSlope = 35f;
            int interactable = L2Core.Layer(CombatLayers.InteractableLayerName);
            int enemy = L2Core.Layer(CombatLayers.EnemyLayerName);
            int mask = ~(1 << 2);
            if (interactable >= 0) mask &= ~(1 << interactable);
            if (enemy >= 0) mask &= ~(1 << enemy);
            baker.includeLayers = mask;

            StealthLevelStart start = managers.AddComponent<StealthLevelStart>();
            start.startingShots = 6;

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(L4Characters.PlayerPrefab);
            player.transform.position = V(0f, 0.05f, -13f);

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = V(0f, 2.2f, -17.5f);
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            camGo.AddComponent<AudioListener>();
            ThirdPersonCamera tpc = camGo.AddComponent<ThirdPersonCamera>();
            tpc.target = player.transform;
            tpc.collisionMask = CombatLayers.CameraMask;

            PlayerController pc = player.GetComponent<PlayerController>();
            pc.cameraTransform = camGo.transform;
            combat = player.GetComponent<PlayerCombat>();
            stealth = player.GetComponent<PlayerStealth>();
            torch = player.GetComponent<StealthTorch>();
            gm.player = pc;
            gm.playerHealth = player.GetComponent<PlayerHealth>();
        }

        /// <summary>
        /// Som do Nível 2 (o revólver e as pedras).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <returns>O clipe.</returns>
        private static AudioClip L2Clip(string name)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(BuilderCore.AudioPath + "/Nivel2/" + name + ".wav");
        }

        // ================================================================== Entrada

        /// <summary>
        /// Entrada (x −5..5, z −16..0, teto 4,5 m): John desce pela corda, aprende a agachar, a tocha e as pedras;
        /// a fenda baixa (z 0..6) leva ao Percurso 1.
        /// </summary>
        private static void BuildStart()
        {
            Geo("Entrada");
            R("Chao", -7f, -18f, 7f, 6f, -1f, 0f, L4Palette.Dirt);
            R("Teto", -7f, -18f, 7f, 0f, 4.5f, 6f, L4Palette.Ceiling);
            R("Parede", -8f, -18f, -5f, 0f, 0f, 4.5f, L4Palette.Wall);
            R("Parede", 5f, -18f, 8f, 0f, 0f, 4.5f, L4Palette.Wall);
            R("Parede", -8f, -19f, 8f, -16f, 0f, 4.5f, L4Palette.Wall);
            // Rocha entre a entrada e o Percurso 1, com a fenda baixa no meio.
            R("Rocha", -8f, 0f, -1.2f, 6f, 0f, 7.5f, L4Palette.RockDark);
            R("Rocha", 1.2f, 0f, 8f, 6f, 0f, 7.5f, L4Palette.RockDark);
            R("TetoDaFenda", -1.2f, 0f, 1.2f, 6f, 1.35f, 7.5f, L4Palette.RockDark);
            Safe("Fenda_Entrada", V(-1.2f, 0f, 0f), V(1.2f, 1.35f, 6f), "Fenda baixa");
            Place("Moldura_da_Fenda", V(0, 0, -0.3f), 180f);

            Place("Corda_de_Descida", V(0.5f, 0, -14f), 0f);
            Place("Fenda_de_Luz", V(0.5f, 0, -14.2f), 0f).transform.localScale = V(1f, 0.62f, 1f);
            Place("Restos_de_Minerador", V(-3.5f, 0, -9.5f), 40f);
            Place("Picareta_e_Pa", V(4.3f, 0, -11f), -80f);
            Place("Caixa_Velha", V(3.8f, 0, -14f), 15f);
            Place("Barril_Velho", V(-4f, 0, -14.8f), 0f);
            Place("Cogumelos_Pequenos", V(-4f, 0, -3.5f), 0f);
            Place("Cogumelos_Pequenos", V(4f, 0, -2f), 60f);
            Place("Musgo_Brilhante", V(2.5f, 0, -6f), 0f);
            Place("Liquen_de_Parede", V(-4.95f, 0.5f, -12f), 90f);
            Place("Raizes_Pendentes", V(-2f, 4.5f, -8f), 20f);
            Place("Estalactite_Escura_P", V(2.5f, 4.5f, -5f), 0f);
            Place("Estalactite_Escura_P", V(-2.5f, 4.5f, -2.5f), 70f);
            Place("Estalagmite_Escura_P", V(4.2f, 0, -5.5f), 0f);
            Place("Rocha_Escura_P", V(-2.6f, 0, -4.6f), 25f);        // cobertura baixa: só esconde quem está agachado
            Place("Caixa_Quebrada", V(-3.5f, 0, -7f), 25f);
            Place("FX4_Poeira_no_Ar", V(0, 2f, -8f), 0f);
            Place("FX4_Gotas", V(-1f, 4.4f, -6f), 0f);
            Place("Tocha_dos_Ogros", V(-4.95f, 1.7f, -6f), 90f).GetComponent<ExtinguishableLight>().roomIndex = 0;
            Pile(V(3.2f, 0, -8f), 0);

            GameObject tablet = Place("Tabua_de_Habilidade", V(-3.6f, 0, -12.5f), 90f);
            SkillTablet st = tablet != null ? tablet.GetComponent<SkillTablet>() : null;
            if (st != null)
            {
                st.title = "Diário de um minerador";
                st.text = "\"Os ogros enxergam pouco e só para a frente, mas ouvem tudo. Andem agachados, longe da luz. " +
                          "Se um deles vir vocês, corram para uma fenda baixa: eles não cabem. E nunca, NUNCA atirem.\"";
            }
            Section("Fenda", V(0, 1f, -2f), V(4f, 2f, 2f), "", "", "Passe agachado pela fenda baixa [C].",
                "A fenda tem teto baixo: agache com C (ou segure Ctrl) para passar.", "", "");
        }

        // ================================================================== Percurso 1

        /// <summary>
        /// Percurso 1 — Introdução (x −16..16, z 6..44, teto 7,5): um ogro (Grukk) dá voltas em torno de uma grande
        /// formação rochosa. Coberturas dos dois lados, sombra densa a oeste, nicho seguro a leste, laje caída com
        /// vão seguro, braseiro iluminando a saída (pode ser apagado) e a fenda para o Percurso 2.
        /// </summary>
        private static void BuildCourse1()
        {
            Geo("Percurso1");
            R("Chao", -18f, 4f, 21f, 46f, -1f, 0f, L4Palette.Dirt);
            R("Teto", -18f, 6f, 21f, 44f, 7.5f, 9f, L4Palette.Ceiling);
            R("Parede", -19f, 4f, -16f, 46f, 0f, 7.5f, L4Palette.Wall);
            R("Parede", 16f, 4f, 19f, 24f, 0f, 7.5f, L4Palette.Wall);
            R("Parede", 16f, 28f, 19f, 46f, 0f, 7.5f, L4Palette.Wall);
            R("Parede", 19f, 22f, 22f, 30f, 0f, 7.5f, L4Palette.Wall);
            R("Parede", -19f, 4f, -8f, 6f, 0f, 7.5f, L4Palette.Wall);
            R("Parede", 8f, 4f, 19f, 6f, 0f, 7.5f, L4Palette.Wall);
            R("TetoDoNicho", 16f, 24f, 19f, 28f, 1.6f, 7.5f, L4Palette.RockDark);
            Safe("Nicho_Leste_P1", V(16.2f, 0f, 24f), V(19f, 1.6f, 28f), "Nicho baixo");
            // Rocha até o Percurso 2, com a fenda baixa (x 10,8..13,2).
            R("Rocha", -19f, 44f, 10.8f, 50f, 0f, 8f, L4Palette.RockDark);
            R("Rocha", 13.2f, 44f, 22f, 50f, 0f, 8f, L4Palette.RockDark);
            R("TetoDaFenda", 10.8f, 44f, 13.2f, 50f, 1.35f, 8f, L4Palette.RockDark);
            Safe("Fenda_P1_P2", V(10.8f, 0f, 44f), V(13.2f, 1.35f, 50f), "Fenda baixa");
            Place("Moldura_da_Fenda", V(12f, 0, 43.7f), 180f);
            Place("Moldura_da_Fenda", V(0f, 0, 6.3f), 0f);

            // Formação central (Grukk dá a volta nela).
            Place("Rochedo", V(-2f, 0, 21.8f), 10f);
            Place("Rochedo", V(2.2f, 0, 22f), 100f);
            Place("Rochedo", V(-2.1f, 0, 26.2f), 200f);
            Place("Rochedo", V(2f, 0, 26.4f), 300f);
            Place("Pilar_de_Rocha", V(0f, 0, 24f), 15f);
            Place("Tocha_dos_Ogros", V(4.3f, 1.8f, 24f), 90f).GetComponent<ExtinguishableLight>().roomIndex = 1;

            // Faixa sul (logo depois da fenda).
            Place("Escombros_P", V(-4f, 0, 9.5f), 20f);
            Place("Escombros_M", V(4.5f, 0, 9.8f), -30f);
            Place("Caixa_Velha", V(-10f, 0, 8.5f), 12f);
            Place("Rocha_Escura_M", V(9f, 0, 8.2f), 40f);
            Place("Placa_de_Aviso", V(-2.4f, 0, 7.4f), 180f);
            // Lado oeste (sombra densa).
            Place("Escombros_M", V(-13f, 0, 14f), 70f);
            Place("Pilha_de_Caixas_Velhas", V(-14f, 0, 19.5f), 90f);
            Place("Rocha_Escura_G", V(-12.6f, 0, 25f), 0f);
            Place("Laje_Caida", V(-13.6f, 0, 31f), 90f);
            Place("Amuleto_do_Minerador", V(-13.6f, 0.1f, 31f), 0f);
            Place("Barril_Velho", V(-11.2f, 0, 36.2f), 0f);
            Place("Escombros_G", V(-13f, 0, 40.5f), 20f);
            Place("Restos_de_Minerador", V(-15f, 0, 22.5f), -60f);
            // Dentro da volta (entre o caminho do ogro e a formação).
            Place("Caixa_Velha_Grande", V(-6.3f, 0, 16f), 10f);
            Place("Trilhos", V(6.3f, 0, 17.5f), 0f);
            Place("Carrinho_de_Mina", V(6.3f, 0, 17f), 0f);
            Place("Escombros_P", V(6f, 0, 30.5f), 60f);
            Place("Cogumelos_Altos", V(-6.3f, 0, 31f), 0f);
            // Lado leste.
            Place("Rocha_Escura_G", V(13f, 0, 12.3f), 30f);
            Place("Cogumelos_Altos", V(13.5f, 0, 20.5f), 40f);
            Place("Tabuas_Empilhadas", V(12.2f, 0, 31.5f), 80f);
            Place("Caixa_Velha", V(14.2f, 0, 35.5f), -10f);
            Place("Escombros_M", V(14.6f, 0, 40.5f), 10f);
            // Faixa norte (perto da saída).
            Place("Escombros_P", V(-5f, 0, 40.3f), 0f);
            Place("Cascalho_Solto", V(2.5f, 0, 41f), 0f);
            Place("Ossos_Espalhados", V(-9f, 0, 41.5f), 30f);
            Place("Braseiro_dos_Ogros", V(7.2f, 0, 41f), 0f).GetComponent<ExtinguishableLight>().roomIndex = 1;
            Pile(V(-14.6f, 0, 9f), 1);

            // Luzes pequenas, decoração e névoa.
            Place("Cogumelos_Pequenos", V(12.5f, 0, 15.5f), 0f);
            Place("Andaime_Quebrado", V(15.1f, 0, 17.5f), -90f);
            Place("Cogumelos_Pequenos", V(-7f, 0, 40.5f), 90f);
            Place("Fungo_de_Prateleira", V(-15.95f, 2.5f, 16f), 90f);
            Place("Fungo_de_Prateleira", V(15.95f, 2.2f, 37f), -90f);
            Place("Veio_de_Cristal", V(-15.95f, 1.2f, 34f), 90f);
            Place("Fenda_de_Luz", V(6f, 0, 34f), 0f);
            Shadow("Sombra_Oeste_P1", V(-16f, 0f, 8f), V(-10f, 3f, 42f), 0.5f);
            Place("FX4_Nevoa_Rasteira", V(-13f, 0.2f, 25f), 0f);
            Place("FX4_Poeira_no_Ar", V(0, 3f, 14f), 0f);
            Place("FX4_Poeira_no_Ar", V(5f, 3f, 36f), 0f);
            Place("FX4_Gotas", V(-8f, 7.4f, 30f), 0f);
            Place("FX4_Pedrinhas_do_Teto", V(9f, 7.4f, 24f), 0f);
            DecorateRoom(-16f, 6f, 16f, 44f, 7.5f, 16, new[] { V(0, 0, 6f), V(12f, 0, 44f), V(17.5f, 0, 26f), V(15.1f, 0, 17.5f) });
            Section("Primeiro_Ogro", V(0, 1f, 9f), V(10f, 2f, 3f), "", "",
                "", "Observe Grukk antes de sair da cobertura.\nA barra sobre a cabeça dele enche quando ele está te notando; \"?\" amarelo = desconfiado; \"!\" vermelho = te viu.\nPedras [E] arremessadas com [G] fazem barulho longe de você e atraem o ogro.",
                "Um ogro... enorme. Melhor não chamar a atenção.", "");
        }

        // ================================================================== Percurso 2

        /// <summary>
        /// Percurso 2 — Observação (x −12..18, z 50..98, teto 8): uma parede de rocha divide a galeria. Do lado
        /// escuro (oeste) Morg vai e volta; do lado iluminado (leste) Grukk dá voltas em torno de pilares. Uma
        /// passagem larga e uma fenda estreita (só John cabe) ligam os lados; nichos seguros, chão barulhento e a
        /// fenda para o Percurso 3.
        /// </summary>
        private static void BuildCourse2()
        {
            Geo("Percurso2");
            R("Chao", -17f, 50f, 24f, 98f, -1f, 0f, L4Palette.Dirt);
            R("Teto", -17f, 50f, 24f, 98f, 8f, 9.5f, L4Palette.Ceiling);
            R("Parede", -15f, 50f, -12f, 84f, 0f, 8f, L4Palette.Wall);
            R("Parede", -15f, 88f, -12f, 98f, 0f, 8f, L4Palette.Wall);
            R("Parede", -17f, 82f, -15f, 90f, 0f, 8f, L4Palette.Wall);
            R("TetoDoNicho", -15f, 84f, -12f, 88f, 1.6f, 8f, L4Palette.RockDark);
            Safe("Nicho_Oeste_P2", V(-15f, 0f, 84f), V(-12.2f, 1.6f, 88f), "Nicho baixo");
            R("Parede", 18f, 50f, 21f, 74f, 0f, 8f, L4Palette.Wall);
            R("Parede", 18f, 78f, 21f, 98f, 0f, 8f, L4Palette.Wall);
            R("Parede", 21f, 72f, 24f, 80f, 0f, 8f, L4Palette.Wall);
            R("TetoDoNicho", 18f, 74f, 21f, 78f, 1.6f, 8f, L4Palette.RockDark);
            Safe("Nicho_Leste_P2", V(18.2f, 0f, 74f), V(21f, 1.6f, 78f), "Nicho baixo");
            // Parede central com passagem larga (z 66..69) e fenda estreita (z 80..81,2).
            R("ParedeCentral", -2f, 56f, 2f, 66f, 0f, 8f, L4Palette.Wall);
            R("ParedeCentral", -2f, 69f, 2f, 80f, 0f, 8f, L4Palette.Wall);
            R("ParedeCentral", -2f, 81.2f, 2f, 92f, 0f, 8f, L4Palette.Wall);
            Safe("Fenda_Estreita", V(-2f, 0f, 80f), V(2f, 2.5f, 81.2f), "Fenda estreita");
            // Rocha até o Percurso 3, com a fenda baixa (x −9,2..−6,8).
            R("Rocha", -17f, 98f, -9.2f, 104f, 0f, 10f, L4Palette.RockDark);
            R("Rocha", -6.8f, 98f, 24f, 104f, 0f, 10f, L4Palette.RockDark);
            R("TetoDaFenda", -9.2f, 98f, -6.8f, 104f, 1.35f, 10f, L4Palette.RockDark);
            Safe("Fenda_P2_P3", V(-9.2f, 0f, 98f), V(-6.8f, 1.35f, 104f), "Fenda baixa");
            Place("Moldura_da_Fenda", V(12f, 0, 50.3f), 0f);
            Place("Moldura_da_Fenda", V(-8f, 0, 97.7f), 180f);
            Place("Arco_de_Rocha", V(0f, 0, 67.5f), 90f);

            // Lado leste: pilares e rochedos no meio do circuito de Grukk.
            Place("Rochedo", V(10f, 0, 66.5f), 30f);
            Place("Pilar_de_Rocha", V(10f, 0, 73.5f), 0f);
            Place("Rochedo", V(10f, 0, 80.5f), 120f);
            Place("Pilar_de_Rocha", V(10f, 0, 87f), 45f);
            Place("Poca_Brilhante", V(14.5f, 0, 61f), 20f);
            Place("Fenda_de_Luz", V(12.8f, 0, 77f), 0f);
            Place("Cogumelos_Pequenos", V(6.8f, 0, 70.5f), 0f);
            Place("Cogumelos_Pequenos", V(16.6f, 0, 84f), 0f);
            Place("Cogumelos_Altos", V(16.4f, 0, 66f), 0f);
            Place("Escombros_M", V(16.9f, 0, 71f), 80f);
            Place("Caixa_Velha_Grande", V(16.9f, 0, 92f), 5f);
            Place("Tabuas_Empilhadas", V(7.2f, 0, 77.5f), 90f);
            Place("Samambaia_da_Caverna", V(7.4f, 0, 84.5f), 0f);
            Place("Caixa_Velha", V(13.2f, 0, 84f), 20f);
            Place("Carrinho_de_Mina", V(13.1f, 0, 70.2f), 90f);
            Place("Dente_de_Ogro_Dourado", V(19.6f, 0.1f, 76f), 0f);

            // Lado oeste (escuro): Morg.
            Place("Escombros_G", V(-10.4f, 0, 61f), 0f);
            Place("Pilha_de_Caixas_Velhas", V(-10.4f, 0, 71.5f), 90f);
            Place("Rocha_Escura_G", V(-10f, 0, 79f), 40f);
            Place("Cogumelos_Altos", V(-10.3f, 0, 93f), 0f);
            Place("Caixa_Velha", V(-3.4f, 0, 60f), 0f);
            Place("Escombros_P", V(-3.3f, 0, 76.5f), 45f);
            Place("Barril_Velho", V(-3.2f, 0, 88.5f), 0f);
            Place("Tocha_dos_Ogros", V(-11.95f, 1.8f, 65.5f), 90f).GetComponent<ExtinguishableLight>().roomIndex = 2;
            Place("Veio_de_Cristal", V(-2.05f, 1.3f, 85f), -90f);
            Place("Ossos_Espalhados", V(-7f, 0, 82.5f), 0f);
            Shadow("Sombra_Lado_Oeste", V(-12f, 0f, 52f), V(-2f, 3f, 92f), 0.5f);

            // Faixas sul (entrada) e norte (saída).
            Place("Rocha_Escura_M", V(15.6f, 0, 53.2f), 10f);
            Place("Rocha_Escura_P", V(11f, 0, 55.2f), 60f);
            Place("Pilha_de_Caixas_Velhas", V(7.4f, 0, 52.4f), 0f);
            Place("Escombros_P", V(3.5f, 0, 54.5f), 0f);
            Place("Caixa_Velha", V(-4.5f, 0, 53.5f), 30f);
            Place("Escombros_M", V(-9.4f, 0, 54f), 0f);
            Place("Rocha_Escura_G", V(12.5f, 0, 95.6f), 0f);
            Place("Escombros_M", V(-3f, 0, 95.6f), 0f);
            Place("Ossos_Espalhados", V(6f, 0, 95f), 0f);
            Place("Cascalho_Solto", V(-11f, 0, 95.5f), 0f);
            Place("Braseiro_dos_Ogros", V(3.2f, 0, 95.8f), 0f).GetComponent<ExtinguishableLight>().roomIndex = 2;
            Pile(V(16.6f, 0, 51.8f), 2);
            Pile(V(4f, 0, 93f), 2);

            Place("FX4_Poeira_no_Ar", V(10f, 3f, 60f), 0f);
            Place("FX4_Poeira_no_Ar", V(-7f, 3f, 80f), 0f);
            Place("FX4_Nevoa_Rasteira", V(-7f, 0.2f, 72f), 0f);
            Place("FX4_Gotas", V(4f, 7.9f, 75f), 0f);
            Place("FX4_Gotas", V(-6f, 7.9f, 62f), 0f);
            Place("FX4_Pedrinhas_do_Teto", V(-5f, 7.9f, 90f), 0f);
            DecorateRoom(-12f, 50f, 18f, 98f, 8f, 18, new[] { V(12f, 0, 50f), V(-8f, 0, 98f), V(-13.5f, 0, 86f), V(19.5f, 0, 76f), V(0, 0, 67.5f), V(0, 0, 80.6f) });
        }

        // ================================================================== Percurso 3

        /// <summary>
        /// Percurso 3 — O Covil (x −20..20, z 104..158, teto 10): a fogueira dos ogros no centro; Brann (com
        /// lampião) dá voltas em torno dela, Grukk vai e volta de norte a sul e Morg de leste a oeste perto da
        /// saída — os trajetos se cruzam. Nichos seguros nas paredes, laje caída, esconderijos e a saída.
        /// </summary>
        private static void BuildCourse3()
        {
            Geo("Percurso3");
            R("Chao", -25f, 104f, 24f, 160f, -1f, 0f, L4Palette.Dirt);
            R("Teto", -25f, 104f, 24f, 158f, 10f, 11.5f, L4Palette.Ceiling);
            R("Parede", -25f, 100f, -17f, 104f, 0f, 10f, L4Palette.Wall);
            R("Parede", -23f, 104f, -20f, 112f, 0f, 10f, L4Palette.Wall);
            R("Parede", -23f, 116f, -20f, 140f, 0f, 10f, L4Palette.Wall);
            R("Parede", -23f, 144f, -20f, 158f, 0f, 10f, L4Palette.Wall);
            R("Parede", -25f, 110f, -23f, 118f, 0f, 10f, L4Palette.Wall);
            R("Parede", -25f, 138f, -23f, 146f, 0f, 10f, L4Palette.Wall);
            R("TetoDoNicho", -23f, 112f, -20f, 116f, 1.6f, 10f, L4Palette.RockDark);
            R("TetoDoNicho", -23f, 140f, -20f, 144f, 1.6f, 10f, L4Palette.RockDark);
            Safe("Nicho_Sudoeste_P3", V(-23f, 0f, 112f), V(-20.2f, 1.6f, 116f), "Nicho baixo");
            Safe("Nicho_Noroeste_P3", V(-23f, 0f, 140f), V(-20.2f, 1.6f, 144f), "Nicho baixo");
            R("Parede", 20f, 104f, 23f, 126f, 0f, 10f, L4Palette.Wall);
            R("Parede", 20f, 131f, 23f, 158f, 0f, 10f, L4Palette.Wall);
            R("Parede", 23f, 124f, 25f, 133f, 0f, 10f, L4Palette.Wall);
            R("TetoDoNicho", 20f, 126f, 23f, 131f, 1.6f, 10f, L4Palette.RockDark);
            Safe("Nicho_Leste_P3", V(20.2f, 0f, 126f), V(23f, 1.6f, 131f), "Nicho baixo");
            // Parede norte com o túnel da saída (x 6..10).
            R("Parede", -25f, 158f, 6f, 172f, 0f, 10f, L4Palette.Wall);
            R("Parede", 10f, 158f, 24f, 172f, 0f, 10f, L4Palette.Wall);
            R("Chao", 5f, 158f, 11f, 172f, -1f, 0f, L4Palette.FlatStone);
            R("TetoDaSaida", 6f, 158f, 10f, 172f, 4f, 10f, L4Palette.RockDark);
            R("FundoDaSaida", 6f, 170f, 10f, 172f, 0f, 4f, L4Palette.RockDark);
            Place("Moldura_da_Fenda", V(-8f, 0, 104.3f), 0f);
            Place("Saida_das_Galerias", V(8f, 0, 163f), 0f);
            Place("Arco_de_Rocha", V(8f, 0, 158.6f), 0f);

            // Acampamento no centro (círculo de Brann: x −9..9, z 121..139; Grukk passa em x = −4).
            Place("Fogueira_dos_Ogros", V(0f, 0, 130f), 0f);
            Place("Peles_de_Dormir", V(-1.5f, 0, 134.3f), 20f);
            Place("Peles_de_Dormir", V(3.4f, 0, 126.2f), -30f);
            Place("Trono_de_Pedra", V(1.2f, 0, 136.8f), 180f);
            Place("Caldeirao", V(3.2f, 0, 133.6f), 0f);
            Place("Espeto_de_Carne", V(-2.4f, 0, 126.3f), 90f);
            Place("Pilha_de_Ossos", V(5.6f, 0, 130.5f), 0f);
            Place("Cranio_Gigante", V(-6.6f, 0, 130.5f), 90f);
            Place("Totem_de_Ossos", V(6.6f, 0, 124.2f), 0f);
            Place("Totem_de_Ossos", V(-6.6f, 0, 137f), 0f);
            Place("Ossos_Espalhados", V(-6.4f, 0, 117.5f), 0f);
            Place("Ossos_Espalhados", V(12.2f, 0, 128.5f), 40f);
            Place("Correntes_Penduradas", V(4f, 10f, 128f), 0f);
            Place("Correntes_Penduradas", V(-3f, 10f, 133f), 30f);

            // Entrada (sudoeste).
            Place("Escombros_M", V(-12.5f, 0, 107.5f), 20f);
            Place("Rocha_Escura_M", V(-16.5f, 0, 108.5f), 0f);
            Place("Caixa_Velha", V(-1.6f, 0, 106.4f), 0f);
            // Coluna oeste.
            Place("Pilha_de_Caixas_Velhas", V(-17.2f, 0, 121f), 90f);
            Place("Rochedo", V(-13.2f, 0, 128f), 60f);
            Place("Cogumelos_Altos", V(-16.8f, 0, 134f), 0f);
            Place("Coluna_Grossa", V(-13.5f, 0, 140.5f), 20f);      // cobre a chegada ao nicho noroeste
            Place("Cogumelos_Altos", V(-7f, 0, 144.5f), 30f);
            Place("Rocha_Escura_G", V(-11.6f, 0, 152.6f), 15f);
            Place("Escombros_G", V(-17.4f, 0, 153.6f), 0f);
            // Faixa norte (perto da saída).
            Place("Escombros_G", V(-2.6f, 0, 154.2f), 90f);
            Place("Pilha_de_Caixas_Velhas", V(3.2f, 0, 154.5f), 0f);
            Place("Barril_Velho", V(5.2f, 0, 156.2f), 0f);
            Place("Cascalho_Solto", V(11.5f, 0, 152.5f), 0f);
            // Coluna leste.
            Place("Trilhos", V(15.5f, 0, 110f), 0f);
            Place("Carrinho_de_Mina", V(15.5f, 0, 110.5f), 0f);
            Place("Caixa_Velha_Grande", V(17.6f, 0, 116.6f), 20f);
            Place("Laje_Caida", V(15.6f, 0, 121.8f), 90f);
            Place("Escombros_M", V(13.6f, 0, 136f), 60f);
            Place("Samambaia_da_Caverna", V(17.6f, 0, 141f), 0f);
            Place("Rocha_Escura_M", V(12.6f, 0, 145f), 0f);
            Place("Pilha_de_Caixas_Velhas", V(15.2f, 0, 153.3f), 90f);
            Pile(V(-18.2f, 0, 106.2f), 3);
            Pile(V(18.2f, 0, 112.5f), 3);

            // Luzes: fogueira (no prefab), tochas, braseiros, cristais e fungos.
            Place("Tocha_dos_Ogros", V(-19.95f, 1.8f, 124f), 90f).GetComponent<ExtinguishableLight>().roomIndex = 3;
            Place("Tocha_dos_Ogros", V(19.95f, 1.8f, 148f), -90f).GetComponent<ExtinguishableLight>().roomIndex = 3;
            Place("Tocha_dos_Ogros", V(4.5f, 1.8f, 157.95f), 180f).GetComponent<ExtinguishableLight>().roomIndex = 3;
            Place("Braseiro_dos_Ogros", V(12.8f, 0, 155.8f), 0f).GetComponent<ExtinguishableLight>().roomIndex = 3;
            Place("Cristais_Fracos", V(-18.6f, 0, 150f), 0f);
            Place("Cogumelos_Pequenos", V(-18f, 0, 119f), 0f);
            Place("Cogumelos_Pequenos", V(17.8f, 0, 107f), 0f);
            Place("Fungo_de_Prateleira", V(19.95f, 2.6f, 138f), -90f);
            Place("Veio_de_Cristal", V(-19.95f, 1.6f, 147f), 90f);
            Place("Fenda_de_Luz", V(13.6f, 0, 129f), 0f).transform.localScale = V(1f, 1.4f, 1f);
            Shadow("Sombra_Oeste_P3", V(-20f, 0f, 106f), V(-13f, 3f, 156f), 0.55f);
            Shadow("Sombra_Sudeste_P3", V(13f, 0f, 106f), V(20f, 3f, 120f), 0.6f);

            Place("FX4_Poeira_no_Ar", V(0, 4f, 140f), 0f);
            Place("FX4_Poeira_no_Ar", V(-12f, 3f, 115f), 0f);
            Place("FX4_Nevoa_Rasteira", V(-16f, 0.2f, 135f), 0f);
            Place("FX4_Nevoa_Rasteira", V(16f, 0.2f, 112f), 0f);
            Place("FX4_Gotas", V(-10f, 9.9f, 140f), 0f);
            Place("FX4_Pedrinhas_do_Teto", V(0f, 9.9f, 120f), 0f);
            DecorateRoom(-20f, 104f, 20f, 158f, 10f, 22, new[] { V(-8f, 0, 104f), V(8f, 0, 158f), V(-21.5f, 0, 114f), V(-21.5f, 0, 142f), V(21.5f, 0, 128.5f) });
            Section("Covil", V(-8f, 1f, 106.5f), V(4f, 2f, 2f), "", "", "",
                "Brann carrega um lampião: a luz dele revela você mesmo nas sombras.\nOs três caminhos se cruzam perto da fogueira e da saída — espere nos nichos e observe.",
                "O covil... os três estão aqui.", "");
            Section("Quase_La", V(8f, 1.5f, 155f), V(8f, 3f, 2f), "", "", "Entre na saída das galerias.", "", "A saída! Só mais um pouco...", "");
        }

        // ================================================================== Ogros, trajetos e trechos

        /// <summary>
        /// Os trajetos de patrulha de cada percurso, os três ogros e os quatro trechos (entrada e percursos),
        /// que ligam cada ogro ao trajeto certo quando John chega.
        /// </summary>
        private static void BuildOgresAndSectors()
        {
            Transform group = new GameObject("Patrulhas").transform;

            // Percurso 1: Grukk dá voltas em torno da formação central.
            Route(group, "Rota_1_Grukk", PatrolRoute.Mode.Loop,
                new[] { V(-9f, 0, 12f), V(9f, 0, 12f), V(9f, 0, 36f), V(-9f, 0, 36f) },
                new[] { V(-1f, 0, -0.3f), V(1f, 0, 0.2f), V(0.4f, 0, 1f), V(-1f, 0, 0.5f) },
                new[] { 2.5f, 2f, 3f, 2f });
            // Percurso 2: Grukk no circuito iluminado; Morg vai e volta no lado escuro.
            Route(group, "Rota_2_Grukk", PatrolRoute.Mode.Loop,
                new[] { V(5f, 0, 58f), V(15f, 0, 58f), V(15f, 0, 90f), V(5f, 0, 90f) },
                new[] { V(-1f, 0, 0f), V(0.2f, 0, -1f), V(0f, 0, 1f), V(-1f, 0, 0.3f) },
                new[] { 2f, 1.5f, 2.5f, 2f });
            Route(group, "Rota_2_Morg", PatrolRoute.Mode.PingPong,
                new[] { V(-7f, 0, 57f), V(-7f, 0, 74f), V(-7f, 0, 91f) },
                new[] { V(0f, 0, -1f), V(1f, 0, 0f), V(0f, 0, 1f) },
                new[] { 3f, 1.5f, 3f });
            // Percurso 3: Grukk norte–sul (x = −4), Morg leste–oeste (z = 148) e Brann em volta da fogueira.
            Route(group, "Rota_3_Grukk", PatrolRoute.Mode.PingPong,
                new[] { V(-4f, 0, 110f), V(-4f, 0, 130f), V(-4f, 0, 150f) },
                new[] { V(0f, 0, -1f), V(-1f, 0, 0f), V(0f, 0, 1f) },
                new[] { 3f, 1f, 3f });
            Route(group, "Rota_3_Morg", PatrolRoute.Mode.PingPong,
                new[] { V(16f, 0, 148f), V(0f, 0, 148f), V(-14f, 0, 148f) },
                new[] { V(-0.5f, 0, 1f), V(0f, 0, -1f), V(0f, 0, 1f) },
                new[] { 2.5f, 1f, 2.5f });
            Route(group, "Rota_3_Brann", PatrolRoute.Mode.Loop,
                new[] { V(-9f, 0, 121f), V(9f, 0, 121f), V(9f, 0, 139f), V(-9f, 0, 139f) },
                new[] { V(-1f, 0, -1f), V(1f, 0, -1f), V(1f, 0, 1f), V(-1f, 0, 1f) },
                new[] { 1.5f, 1.5f, 1.5f, 1.5f });

            Transform ogres = new GameObject("Ogros").transform;
            grukk = SpawnOgre(L4Ogres.Grukk, ogres, routes["Rota_1_Grukk"], 2, true);
            morg = SpawnOgre(L4Ogres.Morg, ogres, routes["Rota_2_Morg"], 2, false);
            brann = SpawnOgre(L4Ogres.Brann, ogres, routes["Rota_3_Brann"], 2, false);

            Transform sectors = new GameObject("Trechos").transform;
            Sector(sectors, 0, "Entrada", V(-4.5f, 0f, -16f), V(4.5f, 3f, -9f), V(0f, 0.05f, -13f), 0f,
                "Entrada", "Atravesse as galerias sem ser visto pelos ogros.",
                "C (ou segure Ctrl) agacha: menos ruído, menos visível e mais devagar.\n" +
                "O olho no canto mostra o quanto você está visível: sombras escondem, luz denuncia.\n" +
                "F acende/apaga a tocha — acesa, você enxerga, mas os ogros te veem de longe. E apaga chamas.",
                "Que cheiro horrível... e esses roncos. Ogros!", "",
                new[] { (grukk, routes["Rota_1_Grukk"], 2) }, new[] { morg, brann });
            Sector(sectors, 1, "Percurso1", V(-1.2f, 0f, 4.2f), V(1.2f, 1.3f, 7f), V(0f, 0.05f, 7.8f), 0f,
                "Percurso 1 — Primeiro Contato", "Chegue à fenda do outro lado sem ser visto por Grukk.",
                "Um golpe de ogro mata. Fique nas sombras, atrás de rochas e caixas, e espere ele passar.\n" +
                "Se for visto, corra para uma fenda ou um nicho de teto baixo: os ogros não cabem.",
                "", "",
                new[] { (grukk, routes["Rota_1_Grukk"], 2) }, new[] { morg, brann });
            Sector(sectors, 2, "Percurso2", V(10.8f, 0f, 48.2f), V(13.2f, 1.3f, 51f), V(12f, 0.05f, 51.8f), 0f,
                "Percurso 2 — Observação", "Atravesse a galeria dividida até a fenda do noroeste.",
                "Morg patrulha o lado escuro; Grukk, o iluminado. Observe as rotas e as pausas antes de avançar.\n" +
                "A fenda estreita na parede central só deixa você passar. Cascalho e ossos fazem barulho: passe agachado.",
                "Dois agora... preciso observar antes de me mexer.", "observacao",
                new[] { (grukk, routes["Rota_2_Grukk"], 2), (morg, routes["Rota_2_Morg"], 2) }, new[] { brann });
            Sector(sectors, 3, "Percurso3", V(-9.2f, 0f, 102.2f), V(-6.8f, 1.3f, 105f), V(-8f, 0.05f, 105.8f), 0f,
                "Percurso 3 — O Covil", "Chegue à saída no fundo do covil, além da fogueira.",
                "Os três ogros patrulham caminhos que se cruzam. Espere nos nichos e escolha o momento de cruzar.\n" +
                "Um disparo atrai todos os ogros de uma vez.",
                "", "travessia",
                new[] { (grukk, routes["Rota_3_Grukk"], 2), (morg, routes["Rota_3_Morg"], 0), (brann, routes["Rota_3_Brann"], 2) }, new OgreAI[0]);
        }

        /// <summary>
        /// Cria um trajeto de patrulha (pontos filhos olhando para a direção de cada pausa).
        /// </summary>
        /// <param name="parent">Grupo.</param>
        /// <param name="name">Nome.</param>
        /// <param name="mode">Circuito ou vai-e-volta.</param>
        /// <param name="points">Pontos.</param>
        /// <param name="facings">Direção do olhar em cada ponto.</param>
        /// <param name="waits">Pausas.</param>
        private static void Route(Transform parent, string name, PatrolRoute.Mode mode, Vector3[] points, Vector3[] facings, float[] waits)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            PatrolRoute r = go.AddComponent<PatrolRoute>();
            r.mode = mode;
            r.waitTimes = waits;
            r.points = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                GameObject p = new GameObject("Ponto_" + (i + 1));
                p.transform.SetParent(go.transform, false);
                p.transform.position = points[i];
                p.transform.rotation = Quaternion.LookRotation(facings[i].normalized, Vector3.up);
                r.points[i] = p.transform;
            }
            routes[name] = r;
        }

        /// <summary>
        /// Instancia um ogro no ponto inicial do trajeto.
        /// </summary>
        /// <param name="prefab">Prefab.</param>
        /// <param name="parent">Grupo.</param>
        /// <param name="route">Trajeto.</param>
        /// <param name="start">Ponto inicial.</param>
        /// <param name="active">Já em cena (senão, desligado até o trecho dele).</param>
        /// <returns>O ogro.</returns>
        private static OgreAI SpawnOgre(GameObject prefab, Transform parent, PatrolRoute route, int start, bool active)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.position = route.Point(start);
            go.transform.rotation = route.Facing(start);
            OgreAI ai = go.GetComponent<OgreAI>();
            ai.route = route;
            ai.startPoint = start;
            go.SetActive(active);
            return ai;
        }

        /// <summary>
        /// Trecho: gatilho (na fenda de entrada), ponto de retorno, textos e ogros.
        /// </summary>
        private static void Sector(Transform parent, int index, string name, Vector3 min, Vector3 max, Vector3 respawn, float respawnYaw,
            string subtitle, string objective, string tutorial, string line, string skill,
            (OgreAI ogre, PatrolRoute route, int start)[] ogres, OgreAI[] dormant)
        {
            GameObject go = new GameObject("Trecho_" + index + "_" + name);
            go.transform.SetParent(parent, false);
            go.transform.position = (min + max) * 0.5f;
            go.layer = 2;
            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = max - min;
            GameObject rp = new GameObject("PontoDeRetorno");
            rp.transform.SetParent(go.transform, false);
            rp.transform.position = respawn;
            rp.transform.rotation = Quaternion.Euler(0f, respawnYaw, 0f);
            StealthSector s = go.AddComponent<StealthSector>();
            s.sectorIndex = index;
            s.title = LevelTitle;
            s.subtitle = subtitle;
            s.objective = objective;
            s.tutorial = tutorial;
            s.line = line;
            s.skillOnEnter = skill;
            s.respawnPoint = rp.transform;
            s.ogres = ogres.Select(o => new StealthSector.OgreAssignment { ogre = o.ogre, route = o.route, startPoint = o.start }).ToArray();
            s.dormant = dormant;
        }

        // ================================================================== Iluminação

        /// <summary>
        /// Sondas de luz sobre todas as áreas caminháveis.
        /// </summary>
        private static void BuildLightProbes()
        {
            GameObject go = new GameObject("SondasDeLuz");
            LightProbeGroup group = go.AddComponent<LightProbeGroup>();
            List<Vector3> probes = new List<Vector3>();
            void Grid(float x0, float z0, float x1, float z1, float step, float top)
            {
                for (float z = z0; z <= z1; z += step)
                    for (float x = x0; x <= x1; x += step)
                    {
                        probes.Add(V(x, 0.6f, z));
                        probes.Add(V(x, Mathf.Min(top - 0.5f, 3.2f), z));
                    }
            }
            Grid(-4f, -15f, 4f, -1f, 4f, 4.5f);
            Grid(-15f, 7f, 15f, 43f, 5f, 7.5f);
            Grid(-11f, 51f, 17f, 97f, 5f, 8f);
            Grid(-19f, 105f, 19f, 157f, 6f, 10f);
            group.probePositions = probes.ToArray();
        }

        // ================================================================== Auxiliares de cena

        /// <summary>
        /// Novo grupo de geometria (vai para o NavMesh) e define o pai atual.
        /// </summary>
        /// <param name="name">Nome.</param>
        private static void Geo(string name)
        {
            GameObject g = new GameObject(name);
            g.transform.SetParent(geometryRoot, false);
            currentParent = g.transform;
        }

        /// <summary>
        /// Bloco retangular (x0..x1, y0..y1, z0..z1).
        /// </summary>
        private static void R(string name, float x0, float z0, float x1, float z1, float y0, float y1, Material mat)
        {
            Block(name, V(x0, y0, z0), V(x1, y1, z1), mat);
        }

        /// <summary>
        /// Área segura (gatilho).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="min">Canto mínimo.</param>
        /// <param name="max">Canto máximo.</param>
        /// <param name="label">Texto no HUD.</param>
        private static void Safe(string name, Vector3 min, Vector3 max, string label)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(currentParent, false);
            go.transform.position = (min + max) * 0.5f;
            go.layer = 2;
            go.AddComponent<BoxCollider>().size = max - min;
            SafeZone sz = go.AddComponent<SafeZone>();
            sz.label = label;
            go.tag = StealthTags.SafeZone;
        }

        /// <summary>
        /// Zona de sombra (multiplica a exposição de John à luz).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="min">Canto mínimo.</param>
        /// <param name="max">Canto máximo.</param>
        /// <param name="multiplier">Multiplicador.</param>
        private static void Shadow(string name, Vector3 min, Vector3 max, float multiplier)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(currentParent, false);
            go.transform.position = (min + max) * 0.5f;
            go.layer = 2;
            go.AddComponent<BoxCollider>().size = max - min;
            LightZone z = go.AddComponent<LightZone>();
            z.multiplier = multiplier;
            z.label = "Sombra";
        }

        /// <summary>
        /// Pilha de pedras do Nível 2 (pegar com E, arremessar com G).
        /// </summary>
        /// <param name="pos">Posição.</param>
        /// <param name="room">Trecho (reinício).</param>
        private static void Pile(Vector3 pos, int room)
        {
            GameObject prefab = L2Core.L2Prefab("Pilha_de_Pedras");
            if (prefab == null) return;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(currentParent, false);
            go.transform.position = pos;
            StonePile p = go.GetComponent<StonePile>();
            if (p != null)
            {
                p.roomIndex = room;
                p.firstTimeTip = "Pedras [G] fazem barulho onde caem: o ogro vai investigar lá. Use para tirá-lo do caminho.";
            }
        }

        /// <summary>
        /// Decoração das paredes, do teto e do chão de uma sala (sorteio determinístico), longe das passagens.
        /// </summary>
        /// <param name="x0">X mínimo do interior.</param>
        /// <param name="z0">Z mínimo.</param>
        /// <param name="x1">X máximo.</param>
        /// <param name="z1">Z máximo.</param>
        /// <param name="ceiling">Altura do teto.</param>
        /// <param name="count">Quantidade de enfeites no teto.</param>
        /// <param name="keepClear">Passagens (sem paredes irregulares perto).</param>
        private static void DecorateRoom(float x0, float z0, float x1, float z1, float ceiling, int count, Vector3[] keepClear)
        {
            Transform prev = currentParent;
            GameObject deco = new GameObject("Decoracao");
            deco.transform.SetParent(prev, false);
            currentParent = deco.transform;

            bool Clear(Vector3 p, float r)
            {
                foreach (Vector3 k in keepClear) if (StealthUtil.FlatDistance(p, k) < r) return false;
                return true;
            }

            // Paredes irregulares encostadas (quebram as superfícies retas).
            for (float z = z0 + 2.5f; z < z1 - 2f; z += 4.3f)
            {
                Vector3 w = V(x0 + 0.35f, 0, z);
                if (Clear(w, 4.5f)) Place("Parede_Irregular", w, 90f + Rand(-6f, 6f), 1f);
                Vector3 e = V(x1 - 0.35f, 0, z + 1.5f);
                if (Clear(e, 4.5f)) Place("Parede_Irregular", e, -90f + Rand(-6f, 6f), 1f);
            }
            for (float x = x0 + 2.5f; x < x1 - 2f; x += 4.3f)
            {
                Vector3 s = V(x, 0, z0 + 0.35f);
                if (Clear(s, 4.5f)) Place("Parede_Irregular", s, Rand(-6f, 6f), 1f);
                Vector3 n = V(x + 1.2f, 0, z1 - 0.35f);
                if (Clear(n, 4.5f)) Place("Parede_Irregular", n, 180f + Rand(-6f, 6f), 1f);
            }

            // Teto: estalactites e raízes; chão perto das paredes: estalagmites, musgo, líquen.
            // Estalactites: as grandes só no teto alto (a ponta fica sempre acima da cabeça dos ogros).
            string[] tites = ceiling >= 9.5f
                ? new[] { "Estalactite_Escura_P", "Estalactite_Escura_M", "Estalactite_Escura_G" }
                : new[] { "Estalactite_Escura_P", "Estalactite_Escura_M" };
            for (int i = 0; i < count; i++)
            {
                Vector3 p = V(Rand(x0 + 1f, x1 - 1f), ceiling, Rand(z0 + 1f, z1 - 1f));
                Place(i % 5 == 4 ? "Raizes_Pendentes" : tites[rng.Next(tites.Length)], p, Rand(0f, 360f), Rand(0.8f, 1.1f));
            }
            // Estalagmites encostadas nas paredes (no máximo ~2 m para dentro, longe das patrulhas).
            string[] mites = { "Estalagmite_Escura_P", "Estalagmite_Escura_M", "Estalagmite_Escura_G" };
            float[] offsets = { 0.6f, 0.8f, 1.1f };
            for (int i = 0; i < count / 2; i++)
            {
                bool west = i % 2 == 0;
                int k = rng.Next(mites.Length);
                float off = offsets[k] + Rand(0f, 0.15f);
                Vector3 p = V(west ? x0 + off : x1 - off, 0, Rand(z0 + 2f, z1 - 2f));
                if (Clear(p, 4f)) Place(mites[k], p, Rand(0f, 360f), 1f);
            }
            for (int i = 0; i < count / 3; i++)
            {
                Place("Musgo_Brilhante", V(Rand(x0 + 1f, x1 - 1f), 0, Rand(z0 + 1f, z1 - 1f)), Rand(0f, 360f), 1f);
                Place("Pedregulhos_Soltos", V(Rand(x0 + 1f, x1 - 1f), 0, Rand(z0 + 1f, z1 - 1f)), Rand(0f, 360f), 1f);
                bool west = i % 2 == 0;
                Place("Liquen_de_Parede", V(west ? x0 + 0.05f : x1 - 0.05f, Rand(0.3f, 2f), Rand(z0 + 2f, z1 - 2f)), west ? 90f : -90f, 1f);
            }
            currentParent = prev;
        }

        /// <summary>Número sorteado (determinístico).</summary>
        private static float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        /// <summary>
        /// Gatilho de dica/fala (reaproveita o SectionTrigger do Nível 3).
        /// </summary>
        private static void Section(string name, Vector3 center, Vector3 size, string title, string subtitle, string objective, string tutorial, string line, string skill)
        {
            GameObject go = new GameObject("Dica_" + name);
            go.transform.SetParent(currentParent, false);
            go.transform.position = center;
            go.layer = 2;
            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = size;
            SectionTrigger st = go.AddComponent<SectionTrigger>();
            st.title = title;
            st.subtitle = subtitle;
            st.objective = objective;
            st.tutorial = tutorial;
            st.line = line;
            st.skillOnEnter = skill;
        }

        /// <summary>
        /// Instancia um prefab (Nível 4, depois Nível 1).
        /// </summary>
        private static GameObject Place(string id, Vector3 pos, float yaw = 0f, float scale = 1f)
        {
            GameObject prefab = L2Core.Get(id);
            if (prefab == null)
            {
                Debug.LogError("[Treasure Hunter] Prefab não encontrado: " + id);
                GameObject missing = new GameObject("FALTANDO_" + id);
                missing.transform.SetParent(currentParent, false);
                missing.transform.position = pos;
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
        /// Bloco estático (malha com UV2 para lightmap e colisor).
        /// </summary>
        private static GameObject Block(string name, Vector3 min, Vector3 max, Material mat, bool collider = true)
        {
            Vector3 size = max - min;
            string key = mat.name + "_" + size.x.ToString("F2", CultureInfo.InvariantCulture) + "x" + size.y.ToString("F2", CultureInfo.InvariantCulture) + "x" + size.z.ToString("F2", CultureInfo.InvariantCulture);
            if (!blockMeshes.TryGetValue(key, out Mesh mesh))
            {
                mesh = new VoxelModel().Add(Vector3.zero, size, mat).Build(key, out _);
                Unwrapping.GenerateSecondaryUVSet(mesh);
                BuilderCore.SaveAsset(mesh, L2Core.MeshesPath + "/Level/" + key + ".asset");
                blockMeshes[key] = mesh;
            }
            GameObject go = new GameObject(name + "_" + (++blockCounter));
            go.transform.SetParent(currentParent, false);
            go.transform.position = (min + max) * 0.5f;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<BoxCollider>().size = size;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        // ================================================================== Inventário

        /// <summary>
        /// Conta os assets do Nível 4: instâncias na cena, prefabs que surgem no jogo, e os arquivos de textura,
        /// normal map, material, animação e som.
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

            int textures = AssetDatabase.FindAssets("t:Texture2D", new[] { L2Core.TexturesPath }).Length;
            int normals = Directory.Exists(L2Core.TexturesPath) ? Directory.GetFiles(L2Core.TexturesPath, "*normal*.png").Length : 0;
            int materials = AssetDatabase.FindAssets("t:Material", new[] { L2Core.MaterialsPath }).Length;
            int clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { L2Core.AnimationsPath }).Length;
            int wavs = Directory.Exists(AudioFolder) ? Directory.GetFiles(AudioFolder, "*.wav").Length : 0;

            var byCat = distinct.GroupBy(kv => kv.Value).OrderBy(g => g.Key.ToString());
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Inventário de assets — " + LevelTitle);
            sb.AppendLine();
            sb.AppendLine("Gerado automaticamente pelo menu *Treasure Hunter > Construir Nível 4*.");
            sb.AppendLine();
            sb.AppendLine("- **Prefabs/objetos distintos no nível: " + distinct.Count + "** (meta: 70; cópias do mesmo prefab não contam)");
            sb.AppendLine("- Instâncias colocadas na cena: " + scene.Length);
            sb.AppendLine("- Prefabs que surgem durante o jogo (efeitos, projéteis): " + runtime.Count);
            sb.AppendLine("- Texturas: " + textures + " (das quais " + normals + " normal maps) · materiais: " + materials);
            sb.AppendLine("- Clipes de animação: " + clips + " · sons do Nível 4: " + wavs);
            sb.AppendLine("- **Total de assets distintos (prefabs + texturas + materiais + animações + sons): " +
                          (distinct.Count + textures + materials + clips + wavs) + "**");
            sb.AppendLine();
            sb.AppendLine("| Categoria | Prefabs distintos | Lista (× instâncias na cena · *surge no jogo*) |");
            sb.AppendLine("|---|---|---|");
            foreach (var g in byCat)
            {
                IEnumerable<string> names = g.Select(kv => kv.Key).OrderBy(n => n)
                    .Select(n => runtime.Contains(n) ? "*" + n + "*" : n + " ×" + instances[n]);
                sb.AppendLine("| " + g.Key + " | " + g.Count() + " | " + string.Join(", ", names) + " |");
            }
            File.WriteAllText(BuilderCore.DocsPath + "/Inventario_Nivel4.md", sb.ToString(), Encoding.UTF8);
            Debug.Log("[Treasure Hunter] Nível 4: " + distinct.Count + " prefabs distintos (" + scene.Length + " instâncias).");
        }
    }
}
