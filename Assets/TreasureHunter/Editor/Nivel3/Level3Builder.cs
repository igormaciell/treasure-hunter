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
    /// Gerador do Nível 3 — A Caverna de Cristal (menu "Treasure Hunter > Construir Nível 3").
    /// Uma única sala enorme: caverna com terreno (Unity Terrain), abismo central (Void), cristais com luz e
    /// partículas, e um percurso de parkour em 4 trechos de dificuldade crescente. No Trecho 2 o jogador quebra
    /// o cristal especial e ganha o Totem de Checkpoint. Os assets ficam em Generated_Nivel3; os níveis
    /// anteriores não são regerados (só o menu principal ganha o botão "Nível 3").
    /// </summary>
    public static class Level3Builder
    {
        public const string Root = BuilderCore.Root + "/Generated_Nivel3";
        public const string AudioFolder = BuilderCore.AudioPath + "/Nivel3";
        public const string Level3ScenePath = BuilderCore.ScenesPath + "/Level3.unity";
        public const string Level4ScenePath = BuilderCore.ScenesPath + "/Level4.unity";
        private const string LevelTitle = "Nível 3 — A Caverna de Cristal";

        /// <summary>O Nível 3 de verdade já foi gerado (não só a tela provisória criada pelo Nível 2).</summary>
        public static bool IsGenerated => AssetDatabase.IsValidFolder(Root) && File.Exists(Level3ScenePath);

        private static readonly Dictionary<string, Mesh> blockMeshes = new Dictionary<string, Mesh>();
        private static Transform currentParent;
        private static int blockCounter;
        private static GameManager gm;
        private static GameObject player;

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        // ================================================================== Menus

        /// <summary>
        /// Gera o Nível 3 (assets, cena, Nível 4 provisório e botão no menu).
        /// </summary>
        [MenuItem("Treasure Hunter/Construir Nível 3 (parkour)", priority = 3)]
        public static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!Build()) return;
            EditorUtility.DisplayDialog("Treasure Hunter",
                "Nível 3 gerado com sucesso!\n\n" +
                "Cenas: Level3 (Caverna de Cristal) e Level4 (provisória).\n" +
                "O menu principal agora tem o botão \"Nível 3\"; o fim do Nível 2 já leva ao Nível 3.\n" +
                "Inventário: Assets/TreasureHunter/Docs/Inventario_Nivel3.md\n\n" +
                "Opcional: Treasure Hunter > Assar iluminação do Nível 3.", "OK");
        }

        /// <summary>
        /// Abre a cena do Nível 3.
        /// </summary>
        [MenuItem("Treasure Hunter/Abrir cena do Nível 3", priority = 23)]
        public static void OpenLevel3()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Level3ScenePath);
        }

        /// <summary>
        /// Assa a iluminação do Nível 3 (opcional).
        /// </summary>
        [MenuItem("Treasure Hunter/Assar iluminação do Nível 3 (opcional)", priority = 41)]
        public static void BakeLighting()
        {
            if (!File.Exists(Level3ScenePath))
            {
                EditorUtility.DisplayDialog("Treasure Hunter", "Gere o Nível 3 primeiro.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Level3ScenePath);
            if (Lightmapping.BakeAsync())
                EditorUtility.DisplayDialog("Treasure Hunter", "O cozimento da luz começou (barra de progresso no canto inferior direito).\nQuando terminar, salve a cena (Ctrl+S).", "OK");
        }

        /// <summary>
        /// Apaga o progresso salvo (totem coletado) para testar o Nível 3 do zero.
        /// </summary>
        [MenuItem("Treasure Hunter/Apagar progresso salvo (totem)", priority = 60)]
        public static void ResetProgress()
        {
            PlayerProgress.ResetAll();
            EditorUtility.DisplayDialog("Treasure Hunter", "Progresso apagado: o Totem de Checkpoint terá de ser coletado de novo.", "OK");
        }

        /// <summary>
        /// Gera tudo. Também é chamado pelo gerador do Nível 2 quando o Nível 3 já existia.
        /// </summary>
        /// <returns>true se terminou.</returns>
        public static bool Build()
        {
            if (L2Core.L1("John_Craft") == null || !File.Exists(LevelBuilder.Level1ScenePath))
            {
                EditorUtility.DisplayDialog("Treasure Hunter",
                    "O Nível 3 reaproveita o personagem e alguns itens do Nível 1.\n\nGere primeiro: Treasure Hunter > Construir Nível 1 (gerar tudo).", "OK");
                return false;
            }
            try
            {
                Progress("Pastas, tags e camadas", 0.02f);
                blockMeshes.Clear();
                blockCounter = 0;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                L2Core.UseFolders(Root, AudioFolder);
                L2Core.PrepareFolders();
                L2Core.EnsureTagsAndLayers(ParkourTags.All, new[] { ParkourLayers.InteractableLayerName, ParkourLayers.TotemLayerName });
                ParkourLayers.ResetCache();
                L2Core.ConfigureAudioImport();
                UIFactory.PrepareShared();

                Progress("Texturas, normal maps e materiais", 0.08f);
                L3Palette.Build();
                Progress("Partículas", 0.14f);
                L3Particles.BuildAll();
                Progress("Cristais, rochas e estruturas", 0.22f);
                L3Props.BuildAll();
                Progress("Plataformas, totem e checkpoint", 0.32f);
                L3Parkour.BuildAll();
                Progress("John (parkour) e animações", 0.4f);
                L3Characters.BuildAll();
                L3UI.BuildSprites();
                AssetDatabase.SaveAssets();

                Progress("Cena do Nível 3", 0.5f);
                BuildScene();

                Progress("Cena provisória do Nível 4", 0.88f);
                L3UI.BuildLevel4Placeholder();
                EditorBuildSettings.scenes = LevelBuilder.BuildScenes();
                Progress("Botão \"Nível 3\" no menu", 0.94f);
                L3UI.PatchMainMenu();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorSceneManager.OpenScene(Level3ScenePath);
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
            EditorUtility.DisplayProgressBar("Treasure Hunter — gerando o Nível 3", step, t);
        }

        // ================================================================== Cena

        /// <summary>
        /// Monta a cena completa.
        /// </summary>
        private static void BuildScene()
        {
            SetupLighting();
            BuildManagersAndPlayer();
            Progress("Nível 3: caverna, terreno e abismo", 0.56f);
            BuildCave();
            Progress("Nível 3: Trecho 1 (introdução)", 0.62f);
            BuildStartArea();
            BuildSection1();
            Progress("Nível 3: Trecho 2 (santuário do totem)", 0.68f);
            BuildSection2();
            Progress("Nível 3: Trecho 3 (travessia)", 0.74f);
            BuildSection3();
            Progress("Nível 3: Trecho 4 (escalada final) e saída", 0.8f);
            BuildSection4();
            BuildLightProbes();
            Progress("Nível 3: inventário", 0.85f);
            WriteInventory();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Level3ScenePath);
        }

        /// <summary>
        /// Ambiente azulado, neblina leve (dá para ver os saltos à frente) e configuração de luz assada.
        /// </summary>
        private static void SetupLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.sun = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.08f, 0.12f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.055f);
            RenderSettings.fogDensity = 0.017f;

            LightingSettings ls = new LightingSettings { name = "Iluminacao_Nivel3" };
            ls.bakedGI = true;
            ls.realtimeGI = false;
            ls.mixedBakeMode = MixedLightingMode.IndirectOnly;
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            ls.lightmapResolution = 4f;
            ls.lightmapPadding = 2;
            ls.lightmapMaxSize = 1024;
            ls.directionalityMode = LightmapsMode.NonDirectional;
            ls.autoGenerate = false;
            BuilderCore.SaveAsset(ls, L2Core.LightingPath + "/Iluminacao_Nivel3.lighting");
            Lightmapping.lightingSettings = ls;
        }

        /// <summary>
        /// Gerenciadores, jogador do parkour, câmera, marca de pouso e interface.
        /// </summary>
        private static void BuildManagersAndPlayer()
        {
            GameObject managers = new GameObject("Gerenciadores");
            gm = managers.AddComponent<GameManager>();
            gm.levelName = LevelTitle;
            gm.totalArtifacts = 2;
            gm.nextLevelScene = "Level4";
            gm.startObjective = "Atravesse a caverna saltando de plataforma em plataforma.";

            SkillManager sm = managers.AddComponent<SkillManager>();
            sm.skills = new List<SkillDefinition>
            {
                new SkillDefinition("salto_preciso", "Saltos Precisos", "Calcular a distância e pousar em plataformas de alturas diferentes."),
                new SkillDefinition("escalada", "Escalada de Bordas", "Agarrar bordas de cristais, rochas, caixas, muros e torres e subir."),
                new SkillDefinition("totem", "Totem de Checkpoint", "Carregar o totem que marca um ponto de retorno (fica para os próximos níveis)."),
                new SkillDefinition("checkpoint_proprio", "Checkpoint Próprio", "Escolher um chão firme e plano para colocar o totem antes dos saltos difíceis."),
                new SkillDefinition("equilibrio", "Equilíbrio", "Atravessar a ponte estreita desviando das estalactites."),
                new SkillDefinition("parkour", "Mestre do Parkour", "Atravessar a Caverna de Cristal do começo ao fim.")
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
            am.caveAmbience = L2Core.Clip("cave_ambience_crystal");
            am.forestAmbience = am.caveAmbience;
            am.music = L2Core.Clip("music_level3");
            am.musicVolume = 0.3f;

            ParkourAudio pa = managers.AddComponent<ParkourAudio>();
            pa.landSoft = L2Core.Clip("land_soft");
            pa.landHard = L2Core.Clip("land_hard");
            pa.ledgeGrab = L2Core.Clip("ledge_grab");
            pa.ledgeClimb = L2Core.Clip("ledge_climb");
            pa.fallWind = L2Core.Clip("fall_wind");
            pa.voidWhoosh = L2Core.Clip("void_whoosh");
            pa.crystalHit = L2Core.Clip("crystal_hit");
            pa.crystalBreak = L2Core.Clip("crystal_break");
            pa.crystalChime = L2Core.Clip("crystal_chime");
            pa.totemPickup = L2Core.Clip("totem_pickup");
            pa.totemPlace = L2Core.Clip("totem_place");
            pa.totemInvalid = L2Core.Clip("totem_invalid");
            pa.totemRespawn = L2Core.Clip("totem_respawn");
            pa.checkpoint = L2Core.Clip("checkpoint");
            pa.platformCrack = L2Core.Clip("platform_crack");
            pa.platformCrumble = L2Core.Clip("platform_crumble");
            pa.platformRestore = L2Core.Clip("platform_restore");

            AtmosphereController atmo = managers.AddComponent<AtmosphereController>();
            atmo.profiles = new[]
            {
                new AtmosphereProfile
                {
                    name = "Caverna de Cristal", ambientColor = RenderSettings.ambientLight, fogColor = RenderSettings.fogColor,
                    fogDensity = RenderSettings.fogDensity, sunEnabled = false, ambience = am.caveAmbience
                }
            };

            player = (GameObject)PrefabUtility.InstantiatePrefab(L3Characters.PlayerPrefab);
            player.transform.position = V(0f, 0.05f, -16f);

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = V(0f, 2.2f, -20.5f);
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 220f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            camGo.AddComponent<AudioListener>();
            ThirdPersonCamera tpc = camGo.AddComponent<ThirdPersonCamera>();
            tpc.target = player.transform;
            tpc.collisionMask = ParkourLayers.CameraMask;

            PlayerController pc = player.GetComponent<PlayerController>();
            pc.cameraTransform = camGo.transform;
            ParkourController parkour = player.GetComponent<ParkourController>();
            TotemAbility totem = player.GetComponent<TotemAbility>();

            GameObject marker = (GameObject)PrefabUtility.InstantiatePrefab(L2Core.Get("Marca_de_Pouso"));
            marker.GetComponent<LandingIndicator>().player = parkour;

            UIManager ui = L3UI.BuildHUD(totem, parkour);
            gm.player = pc;
            gm.playerHealth = player.GetComponent<PlayerHealth>();
            gm.ui = ui;
        }

        // ================================================================== Caverna

        /// <summary>
        /// Terreno, paredões do abismo, teto, estalactites, colunas, Void e névoa.
        /// </summary>
        private static void BuildCave()
        {
            Transform cave = Area("Caverna");
            L3Terrain.Build(cave);

            // Paredões: bordas do abismo (abaixo da entrada e da saída) e laterais.
            for (int i = 0; i < 4; i++)
            {
                float x = -15f + i * 10f;
                Place("Paredao_Rochoso", V(x, -0.05f, 4.7f), 0f);
                Place("Paredao_Rochoso", V(x, 13.45f, 128.8f), 180f);
            }
            for (int i = 0; i < 12; i++)
            {
                float z = 11f + i * 10f;
                float wobble = (i * 37 % 7) - 3f;
                Place("Paredao_Rochoso", V(-19.8f, RimTop(true, z), z), 90f + wobble);
                Place("Paredao_Rochoso", V(19.8f, RimTop(false, z), z), -90f - wobble);
            }

            // Limites invisíveis ao longo do abismo (ninguém contorna o percurso pelas paredes).
            InvisibleWall(V(-18f, -40f, 6f), V(-17.2f, 44f, 127.5f));
            InvisibleWall(V(17.2f, -40f, 6f), V(18f, 44f, 127.5f));

            // Teto.
            Transform prev = currentParent;
            currentParent = Group("Teto", cave);
            Block("Teto", V(-46f, 44f, -32f), V(46f, 48f, 152f), L3Palette.Ceiling, false);
            currentParent = prev;

            // Estalactites e cristais pendurados (sorteio determinístico).
            System.Random r = new System.Random(303);
            string[] tites = { "Estalactite_P", "Estalactite_M", "Estalactite_G" };
            for (int i = 0; i < 46; i++)
            {
                float x = (float)(r.NextDouble() * 64 - 32);
                float z = (float)(r.NextDouble() * 175 - 25);
                Place(tites[r.Next(tites.Length)], V(x, 44f, z), (float)(r.NextDouble() * 360), 1f + (float)r.NextDouble() * 0.8f);
            }
            for (int i = 0; i < 10; i++)
            {
                float x = (float)(r.NextDouble() * 50 - 25);
                float z = (float)(r.NextDouble() * 160 - 15);
                Place("Cristal_Suspenso", V(x, 44f, z), (float)(r.NextDouble() * 360), 1.6f);
            }

            // Colunas naturais do chão do abismo ao teto.
            Place("Coluna_Natural", V(-14f, -40f, 30f), 10f);
            Place("Coluna_Natural", V(14.5f, -40f, 68f), 40f);
            Place("Coluna_Natural", V(-13.5f, -40f, 104f), 70f);
            RimPlace("Formacao_Colunar", true, 61f, 0f, 1.4f);
            RimPlace("Formacao_Colunar", false, 91f, 30f, 1.3f);

            // Cristais nas paredes do abismo.
            for (int i = 0; i < 10; i++)
            {
                float z = 14f + i * 11f;
                bool left = i % 2 == 0;
                Place("Cristal_de_Parede", V(left ? -18.25f : 18.25f, 4f + (i * 13 % 9), z), left ? 90f : -90f, 2f);
            }
            RimPlace("Cristal_Grande", true, 41f, 0f, 1.5f);
            RimPlace("Cristal_Grande", false, 81f, 180f, 1.5f);
            RimPlace("Cristal_Rosa", true, 111f, 0f, 2f);
            RimPlace("Cristal_Aglomerado", false, 31f, 0f, 1.6f);

            // Void.
            Place("Fundo_do_Abismo", V(0, -46f, 60f), 0f);
            GameObject voidGo = new GameObject("Void");
            voidGo.transform.SetParent(currentParent, false);
            voidGo.transform.position = V(0, -32f, 60f);
            BoxCollider vc = voidGo.AddComponent<BoxCollider>();
            vc.isTrigger = true;
            vc.size = V(140f, 10f, 240f);
            VoidZone vz = voidGo.AddComponent<VoidZone>();
            vz.respawnEffects = new[] { L2Core.Get("FX3_Reaparecimento"), L2Core.Get("FX3_Poeira_de_Pouso") };
            foreach (float z in new[] { 18f, 48f, 78f, 108f })
                Place("FX3_Nevoa_do_Abismo", V(0, -20f, z), 0f);
            foreach (Vector3 p in new[] { V(0, 8f, 28f), V(0, 12f, 66f), V(0, 16f, 104f), V(0, 4f, -8f) })
                Place("FX3_Esporos", p, 0f);
            foreach (Vector3 p in new[] { V(6f, 30f, 20f), V(-5f, 34f, 58f), V(8f, 33f, 88f), V(-3f, 32f, 118f) })
                Place("FX3_Gotas", p, 0f);

            AreaLight("LuzDeArea_Paredes_Oeste", V(-17f, 8f, 60f), 90f, new Vector2(40f, 10f), new Color(0.35f, 0.55f, 1f), 0.8f);
            AreaLight("LuzDeArea_Paredes_Leste", V(17f, 8f, 60f), -90f, new Vector2(40f, 10f), new Color(0.6f, 0.4f, 1f), 0.8f);
        }

        /// <summary>
        /// Altura do topo do paredão lateral mais próximo (as bordas laterais do abismo, em x = ±19,8).
        /// </summary>
        /// <param name="left">Lado esquerdo (x negativo).</param>
        /// <param name="z">Posição ao longo da caverna.</param>
        /// <returns>Altura do topo.</returns>
        private static float RimTop(bool left, float z)
        {
            int i = Mathf.Clamp(Mathf.RoundToInt((z - 11f) / 10f), 0, 11);
            return 24f + ((left ? i : i + 1) % 3) * 0.8f;
        }

        /// <summary>
        /// Coloca um enfeite em cima da borda lateral do abismo (topo do paredão).
        /// </summary>
        /// <param name="id">Prefab.</param>
        /// <param name="left">Lado esquerdo.</param>
        /// <param name="z">Posição ao longo da caverna (use o centro de um paredão: 11, 21, 31...).</param>
        /// <param name="yaw">Rotação.</param>
        /// <param name="scale">Escala.</param>
        private static void RimPlace(string id, bool left, float z, float yaw, float scale)
        {
            Place(id, V(left ? -18.9f : 18.9f, RimTop(left, z) - 0.05f, z), yaw, scale);
        }

        // ================================================================== Entrada

        /// <summary>
        /// Área de entrada: arco da masmorra (vindo do Nível 2), ruínas, cristais, inscrição e o início do abismo.
        /// </summary>
        private static void BuildStartArea()
        {
            Area("Entrada");
            Place("Arco_da_Masmorra", V(0, 0, -21f), 0f);
            Place("Escadaria_Quebrada", V(-6.5f, 0, -15f), 90f);
            Place("Torre_Arruinada", V(-9.5f, 0, -8f), 15f);
            Place("Muro_Arruinado", V(8.5f, 0, -12f), 20f);
            Place("Coluna_da_Masmorra", V(-4.5f, 0, -1f), 0f);
            Place("Coluna_da_Masmorra", V(4.5f, 0, -1f), 0f);
            Place("Estandarte_Azul", V(-4.5f, 4.3f, -1.45f), 180f);
            Place("Estandarte_Azul", V(4.5f, 4.3f, -1.45f), 180f);
            Place("Braseiro_de_Cristal", V(-3.2f, 0, 4.5f), 0f);
            Place("Braseiro_de_Cristal", V(3.2f, 0, 4.5f), 0f);
            Place("Cristal_Grande", V(-11f, 0, -2f), 30f);
            Place("Cristal_Grande", V(11f, 0, -5f), -20f);
            Place("Cristal_Aglomerado", V(-7f, 0, 2.5f), 10f);
            Place("Cristal_Aglomerado", V(9f, 0, 1.5f), -40f);
            Place("Cristal_Agulha", V(6f, 0, -16f), 0f);
            Place("Cristal_Ambar", V(-8.5f, 0, -17f), 0f);
            Place("Cristal_Apagado", V(10.5f, 0, -16.5f), 0f);
            Place("Cristal_Flutuante", V(0, 2.5f, 2f), 0f);
            Place("Rocha_Grande", V(11.5f, 0, 3f), 0f);
            Place("Rocha_Media", V(-12f, 0, -12f), 40f);
            Place("Pedregulhos", V(2.5f, 0, -6f), 0f);
            Place("Pedregulhos", V(-2.8f, 0, 1.5f), 60f);
            Place("Estalagmite_M", V(-12.5f, 0, 2.5f), 0f);
            Place("Estalagmite_P", V(12.5f, 0, -10f), 0f);
            Place("Estalagmite_G", V(-12f, 0, -19f), 0f);
            Place("Caixa_da_Masmorra", V(7.5f, 0, -9.5f), 15f);
            Place("Caixa_Grande", V(8.8f, 0, -8f), -10f);
            Place("Barril_da_Masmorra", V(6.6f, 0, -7.8f), 0f);
            Place("Pilha_de_Caixas", V(-8.5f, 0, -12.5f), 70f);
            Place("Musgo_Luminoso", V(-6f, 0, -6f), 0f);
            Place("Musgo_Luminoso", V(5.5f, 0, 2f), 50f);
            Place("Cogumelos_Brilhantes", V(-10.2f, 0, -5.5f), 0f);
            Place("Cogumelos_Brilhantes", V(10f, 0, -1f), 0f);
            Place("Raizes_da_Caverna", V(-0.95f, 4.7f, -20.15f), 0f);
            Place("Placa_de_Runas", V(8.36f, 1.4f, -12.38f), 200f);
            // Treino de escalada (sem risco): um muro de 2,2 m para pular contra a borda e subir.
            GameObject practice = Place("Muro_da_Masmorra", V(8.5f, 2.2f, -3f), 90f);
            practice.name = "Treino_de_Escalada";
            Place("Tocha_de_Parede", V(-2.4f, 2.4f, -20.15f), 0f);
            Place("Tocha_de_Parede", V(2.4f, 2.4f, -20.15f), 0f);
            Place("Lampiao_da_Masmorra", V(0, 4.65f, -20.6f), 0f);
            Place("Corrente_Pendurada", V(-2f, 4.65f, -20.4f), 0f);
            Place("Corrente_Pendurada", V(2f, 4.65f, -20.4f), 0f);

            GameObject tablet = Place("Tabua_de_Habilidade", V(-3f, 0, -12f), 90f);
            SkillTablet st = tablet.GetComponent<SkillTablet>();
            if (st != null)
            {
                st.title = "Inscrição: A Caverna de Cristal";
                st.text = "\"Só atravessa o abismo quem salta com firmeza. Mãos firmes vencem bordas altas; quem cai, volta ao último lugar seguro.\"";
            }

            Section("Trecho1", V(0, 1.5f, -13.5f), V(12f, 3f, 2f), LevelTitle, "Trecho 1 — Primeiros Saltos",
                "Atravesse a caverna saltando de plataforma em plataforma.",
                "Espaço: pular (segure para ir mais alto) · Shift: correr nos saltos longos.\nPule contra uma borda alta para se agarrar e subir.\nO círculo no chão mostra onde você vai pousar (verde = seguro).",
                "Um abismo sem fundo... preciso pisar com cuidado.", "");

            Treasures("Tesouros_Entrada", 3, V(-6f, 0.5f, -4f), V(8.5f, 2.7f, -3f), V(-10f, 0.5f, -14f), V(10.5f, 0.5f, -12.5f), V(0f, 0.5f, -9f));
            AreaLight("LuzDeArea_Entrada", V(0, 6f, -6f), 0f, new Vector2(10f, 6f), new Color(0.4f, 0.7f, 1f), 1.2f, true);
            BakedFill("Preenchimento_Entrada", V(0, 5f, -6f), 18f, 0.7f);
        }

        // ================================================================== Trecho 1

        /// <summary>
        /// Trecho 1 — Introdução: saltos curtos, cristais como apoio, uma caixa como obstáculo, primeira torre
        /// (degrau de 1 m) e uma plataforma móvel para treinar o movimento aéreo.
        /// </summary>
        private static void BuildSection1()
        {
            Area("Trecho1_Introducao");
            Plat("S1a", "Plataforma_Rochosa_M", 0f, 0f, 8.5f);
            Plat("S1b", "Pilar_de_Cristal", 1.8f, 0.5f, 12f);
            Plat("S1c", "Plataforma_Rochosa_P", -0.8f, 1f, 15.2f);
            Plat("S1d", "Pilar_de_Cristal", -3.4f, 1.5f, 18.4f);
            Plat("S1e", "Plataforma_Rochosa_G", -1f, 1.5f, 23f);
            Place("Caixa_da_Masmorra", V(-1f, 1.5f, 23.2f), 10f);
            Plat("S1f", "Torre_da_Masmorra", 2.6f, 2.5f, 26.6f);
            Moving("S1g", "Plataforma_Movel", -2f, 2.5f, 31f, V(4f, 0, 0), 1.5f, 0.9f, 0f);
            Plat("S1h", "Plataforma_Rochosa_M", 0f, 3f, 35.5f);
            Plat("S1i", "Pilar_de_Cristal", 2.4f, 3f, 39f);

            Place("Tocha_de_Parede", V(4.15f, 1.4f, 26.6f), 90f);
            Place("Estandarte_Azul", V(2.6f, 1.9f, 28.1f), 0f);
            Place("Cristal_Aglomerado", V(-1.9f, 1.5f, 21.6f), 0f, 0.7f);
            Place("Musgo_Luminoso", V(0.5f, 1.5f, 24.3f), 0f, 0.8f);
            Place("Cristal_Flutuante", V(-5.5f, 5f, 27f), 0f);
            Place("Cristal_Flutuante", V(5.5f, 6f, 36f), 0f);
            Place("Corrente_Pendurada", V(4.15f, 2.45f, 25.6f), 0f);
            Place("Corrente_Pendurada", V(4.15f, 2.45f, 27.6f), 0f);
            Treasures("Tesouros_Trecho1", 3, V(0.6f, 0.5f, 8.8f), V(-0.8f, 1.5f, 15.3f), V(0.8f, 2f, 22.3f), V(2.6f, 3f, 26.6f), V(0f, 3.5f, 35.6f));
            AreaLight("LuzDeArea_Trecho1", V(0, 9f, 22f), 0f, new Vector2(8f, 16f), new Color(0.45f, 0.8f, 1f), 1.2f, true);
            BakedFill("Preenchimento_Trecho1", V(0, 7f, 24f), 16f, 0.6f);
        }

        // ================================================================== Trecho 2

        /// <summary>
        /// Trecho 2 — Santuário do Totem: platô seguro com pedra de checkpoint, o cristal especial (quebra e libera o
        /// totem) e três pilares sobre o abismo para testar o posicionamento do totem.
        /// </summary>
        private static void BuildSection2()
        {
            Area("Trecho2_Santuario");
            Plat("S2", "Plataforma_do_Santuario", 0f, 3f, 47f);
            Place("Pedra_de_Checkpoint", V(0, 3f, 43.2f), 0f);
            Place("Cristal_do_Totem", V(0, 3f, 49.3f), 0f);
            Plat("P1", "Pilar_de_Cristal_Fino", 8.4f, 3f, 45f);
            Plat("P2", "Plataforma_Rochosa_P", 11.4f, 3.5f, 47.6f);
            Plat("P3", "Pilar_de_Cristal", 9.6f, 4f, 51f);

            Place("Coluna_da_Masmorra", V(-5f, 3f, 51.2f), 0f);
            Place("Coluna_da_Masmorra", V(5f, 3f, 51.2f), 0f);
            Place("Estandarte_Azul", V(-5f, 7.3f, 50.7f), 180f);
            Place("Estandarte_Azul", V(5f, 7.3f, 50.7f), 180f);
            Place("Braseiro_de_Cristal", V(-4.5f, 3f, 44f), 0f);
            Place("Braseiro_de_Cristal", V(-3f, 3f, 50.6f), 0f);
            Place("Braseiro_de_Cristal", V(3f, 3f, 50.6f), 0f);
            Place("Cristal_Rosa", V(-5.2f, 3f, 46.5f), 20f);
            Place("Cristal_Ambar", V(-5.3f, 3f, 48.5f), 0f);
            Place("Musgo_Luminoso", V(-3f, 3f, 46f), 0f);
            Place("Cogumelos_Brilhantes", V(-4.8f, 3f, 42.7f), 0f);
            Place("Coracao", V(-4f, 3.05f, 47.6f), 0f);

            GameObject tablet = Place("Tabua_de_Habilidade", V(4.2f, 3f, 44.5f), -90f);
            SkillTablet st = tablet.GetComponent<SkillTablet>();
            if (st != null)
            {
                st.title = "Inscrição: O Totem";
                st.text = "\"Ponha o totem em chão firme e plano. Se o abismo te engolir, é a ele que você volta. Treine nos pilares ao leste antes de seguir.\"";
            }

            Section("Trecho2", V(0, 4.5f, 43f), V(11f, 3f, 2f), LevelTitle, "Trecho 2 — O Santuário do Totem",
                "Toque o cristal especial [E] e pegue o que ele guarda.",
                "Este platô é seguro: a pedra de checkpoint marcou seu ponto de retorno.\nO cristal dourado pulsa de um jeito diferente...",
                "Que energia é essa vindo do cristal?", "salto_preciso");

            Treasures("Tesouros_Santuario", 3, V(11.4f, 4f, 47.6f), V(9.6f, 4.5f, 51f), V(8.4f, 3.5f, 45f), V(-3f, 3.5f, 43f));
            AreaLight("LuzDeArea_Santuario", V(0, 9f, 47f), 0f, new Vector2(10f, 8f), new Color(1f, 0.8f, 0.5f), 1.5f, true);
            BakedFill("Preenchimento_Santuario", V(0, 7f, 47f), 16f, 0.7f);
        }

        // ================================================================== Trecho 3

        /// <summary>
        /// Trecho 3 — Travessia: sem checkpoints fixos (o totem é necessário). Saltos mais longos, torre para escalar,
        /// plataformas frágeis, estalagmite, disco giratório, caixa como degrau, ponte de estalactite com estalactites
        /// caindo (vetor de prefabs sorteados) e a torre alta. Segredo: Olho de Cristal num pilar afastado.
        /// </summary>
        private static void BuildSection3()
        {
            Area("Trecho3_Travessia");
            Plat("S3a", "Plataforma_Rochosa_M", 0f, 3f, 56f);
            Plat("S3b", "Torre_da_Masmorra", 3.4f, 5f, 61f);
            Plat("S3c", "Plataforma_Fragil", 0.4f, 4.5f, 67.4f);
            Plat("S3d", "Plataforma_Fragil", -3.4f, 4.5f, 71.4f);
            Plat("S3e", "Estalagmite_Plataforma", -4.4f, 5.5f, 75.6f);
            Plat("S3f", "Disco_Giratorio", 0.4f, 5.5f, 80.4f);
            Plat("S3g", "Plataforma_Rochosa_M", 4.8f, 5.5f, 86.2f);
            Place("Caixa_da_Masmorra", V(4.8f, 5.5f, 87f), 0f);
            Plat("S3h", "Estalactite_Caida", 4.8f, 6.5f, 93.2f);
            Plat("S3i", "Torre_Alta", 1f, 8.5f, 99.5f);
            Plat("A1", "Pilar_de_Cristal_Fino", -7f, 6.5f, 80.6f);
            Place("Olho_de_Cristal", V(-7f, 6.5f, 80.6f), 0f);

            // Estalactites que despencam sobre a ponte (RockSpawner + FallingRock do Nível 1, prefabs sorteados).
            GameObject dropper = new GameObject("Estalactites_Cadentes");
            dropper.transform.SetParent(currentParent, false);
            dropper.transform.position = V(4.8f, 7.8f, 93.2f);
            BoxCollider dc = dropper.AddComponent<BoxCollider>();
            dc.isTrigger = true;
            dc.size = V(3.5f, 3f, 8.5f);
            RockSpawner rs = dropper.AddComponent<RockSpawner>();
            rs.rockPrefabs = new[] { L2Core.Get("Estalactite_Cadente_P"), L2Core.Get("Estalactite_Cadente_M"), L2Core.Get("Estalactite_Cadente_G") };
            rs.warningDustPrefab = L2Core.L1("FX_WarningDust");
            rs.minInterval = 1.3f;
            rs.maxInterval = 2.3f;
            rs.warningTime = 0.8f;
            Transform[] points = new Transform[4];
            float[] zs = { 90.7f, 92.6f, 94.5f, 96.3f };
            for (int i = 0; i < 4; i++)
            {
                GameObject p = new GameObject("PontoDeQueda_" + (i + 1));
                p.transform.SetParent(dropper.transform, false);
                p.transform.position = V(4.8f, 18f, zs[i]);
                points[i] = p.transform;
                Place("Estalactite_G", V(4.8f + (i % 2 == 0 ? 0.6f : -0.6f), 44f, zs[i]), i * 40f, 1.3f);
            }
            rs.spawnPoints = points;

            Place("Tocha_de_Parede", V(4.95f, 3.8f, 61f), 90f);
            Place("Tocha_de_Parede", V(2.55f, 6.8f, 99.5f), 90f);
            Place("Corrente_Pendurada", V(2.57f, 8.45f, 98.8f), 0f);
            Place("Corrente_Pendurada", V(2.57f, 8.45f, 100.2f), 0f);
            Place("Estandarte_Azul", V(3.4f, 4.4f, 62.55f), 0f);
            Place("Cristal_Flutuante", V(-2.5f, 8f, 84.5f), 0f);
            Place("Cristal_Flutuante", V(7f, 9f, 77f), 0f);
            Place("Cristal_Flutuante", V(-6f, 9.5f, 66f), 0f);
            Place("Cristal_Agulha", V(-5f, 5.5f, 76.2f), 0f, 0.6f);
            Place("Pedregulhos", V(5.6f, 5.5f, 85.2f), 0f, 0.6f);
            Place("Coracao", V(4f, 5.55f, 85.3f), 0f);

            Section("Trecho3", V(0, 4.5f, 56f), V(3f, 3f, 3f), LevelTitle, "Trecho 3 — A Travessia",
                "Atravesse o abismo até a torre alta. Use o totem!",
                "Daqui em diante NÃO há checkpoints fixos.\nSegure T para colocar o totem em chão firme antes dos saltos difíceis.\nPlataformas com rachaduras âmbar desmoronam: não pare em cima delas.",
                "Se eu cair daqui... melhor marcar um ponto de volta.", "");
            Section("Equilibrio", V(4.8f, 7.6f, 96.8f), V(1.6f, 2f, 0.8f), "", "", "", "", "Ufa! Que equilíbrio!", "equilibrio");

            Treasures("Tesouros_Trecho3", 4, V(0f, 3.5f, 56f), V(3.4f, 5.5f, 61f), V(-4.4f, 6f, 75.6f), V(4.8f, 7f, 91.5f), V(4.8f, 7f, 95f), V(1f, 9f, 99f));
            AreaLight("LuzDeArea_Disco", V(0.4f, 12f, 80.4f), 0f, new Vector2(8f, 8f), new Color(1f, 0.75f, 0.4f), 1.2f, true);
            AreaLight("LuzDeArea_Ponte", V(4.8f, 13f, 93.2f), 0f, new Vector2(3f, 9f), new Color(0.5f, 0.85f, 1f), 1f, true);
            BakedFill("Preenchimento_Trecho3a", V(0, 10f, 66f), 16f, 0.6f);
            BakedFill("Preenchimento_Trecho3b", V(2, 12f, 90f), 16f, 0.6f);
        }

        // ================================================================== Trecho 4 e saída

        /// <summary>
        /// Trecho 4 — Escalada final: pilares finos subindo, cristal instável, muro da masmorra (escalada obrigatória),
        /// elevador de cristal e o salto para a borda da saída. Segredo: Lágrima de Cristal ao lado do elevador.
        /// </summary>
        private static void BuildSection4()
        {
            Area("Trecho4_Escalada_Final");
            Plat("S4a", "Pilar_de_Cristal_Fino", -2.6f, 9f, 103.4f);
            Plat("S4b", "Pilar_de_Cristal_Fino", -0.4f, 9.5f, 107f);
            Plat("S4c", "Cristal_Instavel", 2.6f, 10f, 110.2f);
            Plat("S4d", "Muro_da_Masmorra", 2.6f, 11.5f, 116f);
            Moving("S4e", "Elevador_de_Cristal", 2.6f, 11.5f, 121.4f, V(0, 3f, 0), 1.1f, 1.2f, 0f);
            Plat("A2", "Plataforma_Rochosa_P", -3.2f, 15.5f, 121.8f);
            Place("Lagrima_de_Cristal", V(-3.2f, 15.5f, 121.8f), 0f);
            Plat("Saida", "Plataforma_Rochosa_Longa", 0f, 13.5f, 126.5f);

            Place("Coracao", V(1.8f, 8.55f, 100.3f), 0f);
            Place("Cristal_Flutuante", V(-5f, 13f, 110f), 0f);
            Place("Cristal_Flutuante", V(6f, 15f, 118f), 0f);
            Place("Corrente_Pendurada", V(3.25f, 11.45f, 114.4f), 0f);
            Place("Corrente_Pendurada", V(3.25f, 11.45f, 117.6f), 0f);

            Section("Trecho4", V(1f, 9.6f, 99.5f), V(2.6f, 2f, 2.6f), LevelTitle, "Trecho 4 — A Escalada Final",
                "Suba até a borda da saída.",
                "Pilares finos, um cristal instável e o muro da masmorra: escale a borda do muro!\nO elevador de cristal leva você até a saída.",
                "Lá em cima... a saída!", "");

            // Área da saída (chão do terreno em y = 13,5).
            Area("Saida_para_o_Nivel4");
            Place("Portal_de_Saida", V(0, 13.5f, 140f), 0f);
            Place("Feixe_de_Luz", V(0, 18.4f, 138.5f), 0f);
            Place("Braseiro_de_Cristal", V(-3.5f, 13.5f, 137.5f), 0f);
            Place("Braseiro_de_Cristal", V(3.5f, 13.5f, 137.5f), 0f);
            Place("Cristal_Grande", V(-9f, 13.5f, 134f), 30f);
            Place("Cristal_Grande", V(9f, 13.5f, 136f), -30f);
            Place("Cristal_Aglomerado", V(-6f, 13.5f, 130f), 0f);
            Place("Cristal_Rosa", V(6.5f, 13.5f, 131f), 0f);
            Place("Coluna_da_Masmorra", V(-6f, 13.5f, 140f), 0f);
            Place("Coluna_da_Masmorra", V(6f, 13.5f, 140f), 0f);
            Place("Musgo_Luminoso", V(-2.5f, 13.5f, 132f), 0f);
            Place("Cogumelos_Brilhantes", V(4f, 13.5f, 133.5f), 0f);
            Place("Estalagmite_M", V(-10.5f, 13.5f, 129.5f), 0f);
            Place("Estalagmite_G", V(11f, 13.5f, 141f), 0f);
            Place("Arco_Natural", V(0, 13.5f, 133f), 0f, 1.1f);
            Place("Lampiao_da_Masmorra", V(0, 18.75f, 133f), 0f);
            Section("Chegada", V(0, 15f, 128f), V(10f, 3f, 2f), "", "", "Entre no portal para seguir para o Nível 4.", "", "Consegui atravessar a caverna!", "");
            Treasures("Tesouros_Saida", 3, V(-4f, 14f, 131f), V(4f, 14f, 129.5f), V(0f, 14f, 135f), V(-8f, 14f, 138f));
            AreaLight("LuzDeArea_Saida", V(0, 20f, 134f), 0f, new Vector2(10f, 10f), new Color(0.6f, 0.9f, 1f), 1.6f, true);
            BakedFill("Preenchimento_Saida", V(0, 18f, 132f), 16f, 0.7f);
        }

        // ================================================================== Iluminação auxiliar

        /// <summary>
        /// Sondas de luz ao longo do percurso (a luz assada chega a John, que se move).
        /// </summary>
        private static void BuildLightProbes()
        {
            GameObject go = new GameObject("SondasDeLuz");
            LightProbeGroup group = go.AddComponent<LightProbeGroup>();
            List<Vector3> probes = new List<Vector3>();
            for (float z = -18f; z <= 140f; z += 6f)
            {
                float floor = z < 8f ? 0f : z < 42f ? Mathf.Lerp(0f, 3f, (z - 8f) / 34f) : z < 55f ? 3f : z < 100f ? Mathf.Lerp(3f, 8.5f, (z - 55f) / 45f) : z < 126f ? Mathf.Lerp(8.5f, 13.5f, (z - 100f) / 26f) : 13.5f;
                foreach (float x in new[] { -8f, 0f, 8f })
                {
                    probes.Add(V(x, floor + 0.6f, z));
                    probes.Add(V(x, floor + 4.5f, z));
                }
            }
            group.probePositions = probes.ToArray();
        }

        /// <summary>
        /// Luz de área retangular (Baked).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="pos">Posição.</param>
        /// <param name="yaw">Rotação Y.</param>
        /// <param name="size">Tamanho.</param>
        /// <param name="color">Cor.</param>
        /// <param name="intensity">Intensidade.</param>
        /// <param name="down">Apontar para baixo.</param>
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
            l.range = 20f;
            l.lightmapBakeType = LightmapBakeType.Baked;
        }

        /// <summary>
        /// Luz pontual Baked de preenchimento.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="pos">Posição.</param>
        /// <param name="range">Alcance.</param>
        /// <param name="intensity">Intensidade.</param>
        private static void BakedFill(string name, Vector3 pos, float range, float intensity)
        {
            Light l = L2Core.PointLight(currentParent.gameObject, name, Vector3.zero, new Color(0.6f, 0.7f, 0.95f), range, intensity, false, LightmapBakeType.Baked);
            l.transform.position = pos;
        }

        // ================================================================== Percurso

        /// <summary>
        /// Coloca uma plataforma do percurso (origem no centro do topo).
        /// </summary>
        /// <param name="label">Rótulo do salto (mapa e verificador).</param>
        /// <param name="id">Prefab.</param>
        /// <param name="x">X do centro.</param>
        /// <param name="top">Altura do topo.</param>
        /// <param name="z">Z do centro.</param>
        /// <param name="yaw">Rotação.</param>
        /// <returns>A instância.</returns>
        private static GameObject Plat(string label, string id, float x, float top, float z, float yaw = 0f)
        {
            GameObject go = Place(id, V(x, top, z), yaw);
            go.name = label + "_" + id;
            return go;
        }

        /// <summary>
        /// Coloca uma plataforma móvel do percurso.
        /// </summary>
        /// <param name="label">Rótulo.</param>
        /// <param name="id">Prefab.</param>
        /// <param name="x">X inicial.</param>
        /// <param name="top">Altura inicial do topo.</param>
        /// <param name="z">Z.</param>
        /// <param name="offset">Deslocamento até a outra ponta.</param>
        /// <param name="speed">Velocidade.</param>
        /// <param name="wait">Pausa nas pontas.</param>
        /// <param name="phase">Fase inicial.</param>
        private static void Moving(string label, string id, float x, float top, float z, Vector3 offset, float speed, float wait, float phase)
        {
            GameObject go = Plat(label, id, x, top, z);
            MovingPlatform mp = go.GetComponent<MovingPlatform>();
            if (mp == null) return;
            mp.offset = offset;
            mp.speed = speed;
            mp.waitTime = wait;
            mp.startPhase = phase;
        }

        /// <summary>
        /// Gatilho de início de trecho (títulos, missão, dica, habilidade).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="center">Centro.</param>
        /// <param name="size">Tamanho.</param>
        /// <param name="title">Título.</param>
        /// <param name="subtitle">Subtítulo.</param>
        /// <param name="objective">Missão.</param>
        /// <param name="tutorial">Dica.</param>
        /// <param name="line">Fala.</param>
        /// <param name="skill">Habilidade.</param>
        private static void Section(string name, Vector3 center, Vector3 size, string title, string subtitle, string objective, string tutorial, string line, string skill)
        {
            GameObject go = new GameObject("Gatilho_" + name);
            go.transform.SetParent(currentParent, false);
            go.transform.position = center;
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

        // ================================================================== Auxiliares de cena

        /// <summary>Cria um grupo raiz e o define como pai atual.</summary>
        /// <param name="name">Nome.</param>
        /// <returns>O grupo.</returns>
        private static Transform Area(string name)
        {
            currentParent = Group(name, null);
            return currentParent;
        }

        /// <summary>Cria um objeto de agrupamento.</summary>
        /// <param name="name">Nome.</param>
        /// <param name="parent">Pai.</param>
        /// <returns>O grupo.</returns>
        private static Transform Group(string name, Transform parent)
        {
            GameObject g = new GameObject(name);
            if (parent != null) g.transform.SetParent(parent, false);
            return g.transform;
        }

        /// <summary>
        /// Instancia um prefab (Nível 3 ou Nível 1).
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="pos">Posição.</param>
        /// <param name="yaw">Rotação.</param>
        /// <param name="scale">Escala.</param>
        /// <returns>A instância.</returns>
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
        /// Bloco estático (malha com UV2 para lightmap).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="min">Canto mínimo.</param>
        /// <param name="max">Canto máximo.</param>
        /// <param name="mat">Material.</param>
        /// <param name="collider">Com colisor.</param>
        /// <returns>O bloco.</returns>
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

        /// <summary>
        /// Parede invisível (limite).
        /// </summary>
        /// <param name="min">Canto mínimo.</param>
        /// <param name="max">Canto máximo.</param>
        private static void InvisibleWall(Vector3 min, Vector3 max)
        {
            GameObject go = new GameObject("LimiteInvisivel");
            go.transform.SetParent(currentParent, false);
            go.transform.position = (min + max) * 0.5f;
            go.AddComponent<BoxCollider>().size = max - min;
        }

        /// <summary>
        /// Moedas e joias (prefabs do Nível 1) em pontos sorteados.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="count">Quantidade.</param>
        /// <param name="points">Pontos.</param>
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

        // ================================================================== Inventário

        /// <summary>
        /// Conta os assets do Nível 3: instâncias na cena, prefabs que surgem no jogo, e os arquivos de textura,
        /// normal map, material, camada de terreno, animação e som.
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
            int layers = AssetDatabase.FindAssets("t:TerrainLayer", new[] { Root }).Length;
            int wavs = Directory.Exists(AudioFolder) ? Directory.GetFiles(AudioFolder, "*.wav").Length : 0;

            var byCat = distinct.GroupBy(kv => kv.Value).OrderBy(g => g.Key.ToString());
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Inventário de assets — " + LevelTitle);
            sb.AppendLine();
            sb.AppendLine("Gerado automaticamente pelo menu *Treasure Hunter > Construir Nível 3*.");
            sb.AppendLine();
            sb.AppendLine("- **Prefabs/objetos distintos no nível: " + distinct.Count + "** (meta: 70; cópias do mesmo prefab não contam)");
            sb.AppendLine("- Instâncias colocadas na cena: " + scene.Length);
            sb.AppendLine("- Prefabs que surgem durante o jogo (totem, lascas, estalactites, efeitos): " + runtime.Count);
            sb.AppendLine("- Texturas: " + textures + " (das quais " + normals + " normal maps) · camadas de terreno: " + layers);
            sb.AppendLine("- Materiais: " + materials + " · clipes de animação: " + clips + " · sons do Nível 3: " + wavs);
            sb.AppendLine("- **Total de assets distintos (prefabs + texturas + materiais + camadas + animações + sons): " +
                          (distinct.Count + textures + materials + layers + clips + wavs) + "**");
            sb.AppendLine();
            sb.AppendLine("| Categoria | Prefabs distintos | Lista (× instâncias na cena · *surge no jogo*) |");
            sb.AppendLine("|---|---|---|");
            foreach (var g in byCat)
            {
                IEnumerable<string> names = g.Select(kv => kv.Key).OrderBy(n => n)
                    .Select(n => runtime.Contains(n) ? "*" + n + "*" : n + " ×" + instances[n]);
                sb.AppendLine("| " + g.Key + " | " + g.Count() + " | " + string.Join(", ", names) + " |");
            }
            File.WriteAllText(BuilderCore.DocsPath + "/Inventario_Nivel3.md", sb.ToString(), Encoding.UTF8);
            Debug.Log("[Treasure Hunter] Nível 3: " + distinct.Count + " prefabs distintos (" + scene.Length + " instâncias).");
        }
    }
}
