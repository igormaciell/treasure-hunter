using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Interface do Nível 4: reaproveita o HUD de combate do Nível 2 (munição, mira, pedras) e acrescenta o
    /// painel da furtividade (olho de visibilidade, postura, tocha, ruído, lugar), a lista de ogros, o aviso de
    /// perigo com vinheta e as setas. Também cria a cena provisória do Nível 5 e o botão "Nível 4" no menu.
    /// </summary>
    public static class L4UI
    {
        private static Sprite eyeClosed, eyeHalf, eyeOpen, torchOn, torchOff, arrow, ear;

        /// <summary>
        /// Ícones em pixel art.
        /// </summary>
        public static void BuildSprites()
        {
            var pal = new Dictionary<char, Color>
            {
                { 'K', new Color(0.06f, 0.06f, 0.07f) }, { 'W', Color.white }, { 'G', new Color(0.7f, 0.7f, 0.72f) },
                { 'B', new Color(0.45f, 0.3f, 0.16f) }, { 'b', new Color(0.3f, 0.2f, 0.1f) }, { 'Y', new Color(1f, 0.85f, 0.35f) },
                { 'O', new Color(1f, 0.55f, 0.15f) }, { 'R', new Color(1f, 0.3f, 0.15f) }
            };
            eyeOpen = L2Core.PixelSprite("ui4_olho_aberto", new[]
            {
                "................",
                ".....KKKKKK.....",
                "...KKWWWWWWKK...",
                "..KWWWWKKWWWWK..",
                ".KWWWWKKKKWWWWK.",
                "KWWWWKKWWKKWWWWK",
                ".KWWWWKKKKWWWWK.",
                "..KWWWWKKWWWWK..",
                "...KKWWWWWWKK...",
                ".....KKKKKK.....",
                "................"
            }, pal);
            eyeHalf = L2Core.PixelSprite("ui4_olho_meio", new[]
            {
                "................",
                "................",
                "................",
                ".KKKKKKKKKKKKKK.",
                "KWWWWWKKKKWWWWWK",
                "KWWWWKKWWKKWWWWK",
                ".KWWWWKKKKWWWWK.",
                "..KWWWWKKWWWWK..",
                "...KKWWWWWWKK...",
                ".....KKKKKK.....",
                "................"
            }, pal);
            eyeClosed = L2Core.PixelSprite("ui4_olho_fechado", new[]
            {
                "................",
                "................",
                "................",
                "................",
                "................",
                "KKKKKKKKKKKKKKKK",
                ".KWWWWWWWWWWWWK.",
                "..KKWWWWWWWWKK..",
                ".K..KKKKKKKK..K.",
                "K...K..K...K...K",
                "................"
            }, pal);
            torchOn = L2Core.PixelSprite("ui4_tocha_acesa", new[]
            {
                "....R.....",
                "...ROR....",
                "...OYOR...",
                "..ROYYO...",
                "..OYYYO...",
                "...KbbK...",
                "....BK....",
                "....BK....",
                "....bK....",
                "....BK....",
                "....bK....",
                "....KK...."
            }, pal);
            torchOff = L2Core.PixelSprite("ui4_tocha_apagada", new[]
            {
                "...G......",
                "....G.....",
                "...G......",
                "..........",
                "...KKKK...",
                "...KbbK...",
                "....BK....",
                "....BK....",
                "....bK....",
                "....BK....",
                "....bK....",
                "....KK...."
            }, pal);
            arrow = L2Core.PixelSprite("ui4_seta", new[]
            {
                "......KK......",
                ".....KWWK.....",
                "....KWWWWK....",
                "...KWWWWWWK...",
                "..KWWWWWWWWK..",
                ".KKKKWWWWKKKK.",
                "....KWWWWK....",
                "....KWWWWK....",
                "....KKKKKK...."
            }, pal);
            ear = L2Core.PixelSprite("ui4_ruido", new[]
            {
                "......K.....K.",
                "...KK..K..K..K",
                "..KWWK..K..K.K",
                ".KWWWK..K..K.K",
                "KWWWWK..K..K.K",
                ".KWWWK..K..K.K",
                "..KWWK..K..K.K",
                "...KK..K..K..K",
                "......K.....K."
            }, pal);
        }

        // ================================================================== HUD

        /// <summary>
        /// Monta o Canvas do Nível 4.
        /// </summary>
        /// <param name="combat">Combate (revólver e pedras).</param>
        /// <param name="stealth">Furtividade do jogador.</param>
        /// <param name="torch">Tocha.</param>
        /// <returns>O UIManager.</returns>
        public static UIManager BuildHUD(PlayerCombat combat, PlayerStealth stealth, StealthTorch torch)
        {
            UIManager ui = L2UI.BuildHUD(combat);
            Transform c = ui.transform;
            Transform enemies = c.Find("InimigosDaSala");
            if (enemies != null) Object.DestroyImmediate(enemies.gameObject);
            CombatHUD ch = ui.GetComponent<CombatHUD>();
            if (ch != null) { ch.enemyPanel = null; ch.enemyText = null; }

            Transform help = c.Find("Controles/Texto");
            if (help != null)
                help.GetComponent<Text>().text = "WASD mover · Shift correr · C/Ctrl agachar · F tocha · E interagir (apagar chamas, pegar pedras) · G pedra · Clique atirar · Botão dir. mirar · Esc pausa";
            RectTransform helpRt = c.Find("Controles") as RectTransform;
            if (helpRt != null) helpRt.sizeDelta = new Vector2(1600, 40);

            Transform defeat = c.Find("TelaDeDerrota/Caixa/Titulo/Texto");
            if (defeat != null)
            {
                Text dt = defeat.GetComponent<Text>();
                if (dt != null) dt.text = "O ogro te esmagou!\n<size=28>Um golpe basta. Volte ao começo do trecho e tente outro caminho.</size>";
            }

            StealthHUD hud = ui.gameObject.AddComponent<StealthHUD>();
            hud.stealth = stealth;
            hud.torch = torch;
            hud.eyeClosed = eyeClosed;
            hud.eyeHalf = eyeHalf;
            hud.eyeOpen = eyeOpen;
            hud.torchOn = torchOn;
            hud.torchOff = torchOff;

            // Painel da furtividade (canto inferior esquerdo, acima dos controles).
            RectTransform panel = UIFactory.Panel(c, "PainelDaFurtividade", new Vector2(0, 0), new Vector2(24, 70), new Vector2(520, 230));
            panel.pivot = new Vector2(0, 0);
            panel.anchoredPosition = new Vector2(24, 70);
            RectTransform eye = UIFactory.Rect(panel, "Olho", new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -12), new Vector2(84, 58), new Vector2(0, 1));
            hud.eyeIcon = eye.gameObject.AddComponent<Image>();
            hud.eyeIcon.sprite = eyeClosed;
            hud.eyeIcon.preserveAspect = true;
            hud.eyeIcon.raycastTarget = false;
            RectTransform vt = UIFactory.Rect(panel, "Visibilidade", new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -8), new Vector2(390, 40), new Vector2(0, 1));
            hud.visibilityText = UIFactory.Label(vt, "Texto", "ESCONDIDO", 32, TextAnchor.MiddleLeft, new Color(0.45f, 1f, 0.55f));
            hud.visibilityText.fontStyle = FontStyle.Bold;
            hud.visibilityFill = Bar(panel, "BarraDeVisibilidade", new Vector2(110, -52), 390f, out hud.visibilityFillImage);
            RectTransform stance = UIFactory.Rect(panel, "Postura", new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -82), new Vector2(490, 34), new Vector2(0, 1));
            hud.stanceText = UIFactory.Label(stance, "Texto", "De pé", 24, TextAnchor.MiddleLeft, UIFactory.TextColor);
            hud.stanceText.supportRichText = true;
            RectTransform ti = UIFactory.Rect(panel, "IconeTocha", new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -120), new Vector2(26, 32), new Vector2(0, 1));
            hud.torchIcon = ti.gameObject.AddComponent<Image>();
            hud.torchIcon.sprite = torchOff;
            hud.torchIcon.preserveAspect = true;
            hud.torchIcon.raycastTarget = false;
            RectTransform tt = UIFactory.Rect(panel, "Tocha", new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -118), new Vector2(460, 34), new Vector2(0, 1));
            hud.torchText = UIFactory.Label(tt, "Texto", "Tocha apagada [F]", 24, TextAnchor.MiddleLeft, UIFactory.TextColor);
            hud.torchText.supportRichText = true;
            RectTransform ni = UIFactory.Rect(panel, "IconeRuido", new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -160), new Vector2(34, 24), new Vector2(0, 1));
            Image niImg = ni.gameObject.AddComponent<Image>();
            niImg.sprite = ear;
            niImg.preserveAspect = true;
            niImg.raycastTarget = false;
            RectTransform nt = UIFactory.Rect(panel, "Ruido", new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -154), new Vector2(200, 34), new Vector2(0, 1));
            hud.noiseText = UIFactory.Label(nt, "Texto", "Ruído: silêncio", 22, TextAnchor.MiddleLeft, UIFactory.TextColor);
            hud.noiseFill = Bar(panel, "BarraDeRuido", new Vector2(250, -164), 250f, out hud.noiseFillImage);
            RectTransform spot = UIFactory.Rect(panel, "Lugar", new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -192), new Vector2(490, 32), new Vector2(0, 1));
            hud.spotText = UIFactory.Label(spot, "Texto", "", 22, TextAnchor.MiddleLeft, new Color(0.6f, 0.95f, 1f));

            // Lista dos ogros (canto superior direito, abaixo da arma).
            RectTransform list = UIFactory.Rect(c, "Ogros", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -300), new Vector2(460, 130), new Vector2(1, 1));
            hud.ogreList = UIFactory.Label(list, "Texto", "", 23, TextAnchor.UpperRight, UIFactory.TextColor);
            hud.ogreList.supportRichText = true;

            // Aviso de perigo (centro, acima da mira).
            RectTransform alert = UIFactory.Rect(c, "AvisoDePerigo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1100, 70), new Vector2(0.5f, 0.5f));
            hud.alertGroup = alert.gameObject.AddComponent<CanvasGroup>();
            hud.alertGroup.alpha = 0f;
            hud.alertGroup.blocksRaycasts = false;
            hud.alertText = UIFactory.Label(alert, "Texto", "", 36, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.3f));
            hud.alertText.fontStyle = FontStyle.Bold;

            // Setas para os ogros alertados (em volta da mira).
            RectTransform arrows = UIFactory.Rect(c, "SetasDosOgros", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10), new Vector2(0.5f, 0.5f));
            hud.arrows = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform a = UIFactory.Rect(arrows, "Seta_" + (i + 1), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(42, 28), new Vector2(0.5f, 0.5f));
                Image img = a.gameObject.AddComponent<Image>();
                img.sprite = arrow;
                img.preserveAspect = true;
                img.raycastTarget = false;
                a.gameObject.SetActive(false);
                hud.arrows[i] = a;
            }

            // Vinheta de perigo (bordas da tela).
            RectTransform vig = UIFactory.Rect(c, "VinhetaDePerigo", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            hud.vignette = vig.gameObject.AddComponent<Image>();
            hud.vignette.sprite = VignetteSprite();
            hud.vignette.color = new Color(0.7f, 0.05f, 0.02f, 0f);
            hud.vignette.raycastTarget = false;
            vig.SetAsFirstSibling();

            // Dicas rápidas.
            RectTransform hint = UIFactory.Rect(c, "DicaDaFurtividade", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 360), new Vector2(1200, 56), new Vector2(0.5f, 0));
            hud.hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            hud.hintGroup.alpha = 0f;
            hud.hintGroup.blocksRaycasts = false;
            hud.hintText = UIFactory.Label(hint, "Texto", "", 28, TextAnchor.MiddleCenter, new Color(0.75f, 0.95f, 1f));
            hud.hintText.fontStyle = FontStyle.Bold;

            // Batimento cardíaco (volume controlado pelo HUD).
            AudioSource hb = ui.gameObject.AddComponent<AudioSource>();
            hb.clip = L2Core.Clip("heartbeat");
            hb.loop = true;
            hb.playOnAwake = false;
            hb.volume = 0f;
            hb.spatialBlend = 0f;
            hud.heartbeat = hb;

            foreach (string menu in new[] { "DiarioDeHabilidades", "MenuDePausa", "TelaDeDerrota", "TelaNivelConcluido", "FlashDeDano", "Escurecimento" })
            {
                Transform m = c.Find(menu);
                if (m != null) m.SetAsLastSibling();
            }

            L2Core.Tag(ui.gameObject, AssetCategoryType.Interface, "HUD da furtividade (visibilidade, ruído, tocha, ogros)");
            L2Core.Tag(panel.gameObject, AssetCategoryType.Interface, "Painel de visibilidade e ruído");
            L2Core.Tag(alert.gameObject, AssetCategoryType.Interface, "Aviso de perigo com vinheta e batimento");
            L2Core.Tag(arrows.gameObject, AssetCategoryType.Interface, "Setas dos ogros alertados");
            if (ui.pausePanel != null) L2Core.Tag(ui.pausePanel, AssetCategoryType.Interface, "Menu de pausa (reiniciar sala/jogo)");
            if (ui.gameOverPanel != null) L2Core.Tag(ui.gameOverPanel, AssetCategoryType.Interface, "Tela de derrota (voltar ao checkpoint)");
            if (ui.completePanel != null) L2Core.Tag(ui.completePanel, AssetCategoryType.Interface, "Tela de nível concluído (próximo nível)");
            if (ui.journalPanel != null) L2Core.Tag(ui.journalPanel, AssetCategoryType.Interface, "Diário de habilidades");
            return ui;
        }

        /// <summary>
        /// Barra horizontal (fundo + preenchimento com pivô à esquerda).
        /// </summary>
        /// <param name="parent">Pai.</param>
        /// <param name="name">Nome.</param>
        /// <param name="pos">Posição (canto superior esquerdo).</param>
        /// <param name="width">Largura.</param>
        /// <param name="fillImage">Saída: imagem do preenchimento.</param>
        /// <returns>O preenchimento (escala X = valor).</returns>
        private static RectTransform Bar(RectTransform parent, string name, Vector2 pos, float width, out Image fillImage)
        {
            RectTransform back = UIFactory.Rect(parent, name, new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(width, 14), new Vector2(0, 1));
            Image b = back.gameObject.AddComponent<Image>();
            b.color = new Color(0f, 0f, 0f, 0.6f);
            b.raycastTarget = false;
            RectTransform fill = UIFactory.Rect(back, "Preenchimento", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, new Vector2(0, 0.5f));
            fill.offsetMin = new Vector2(2, 2);
            fill.offsetMax = new Vector2(-2, -2);
            fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.45f, 1f, 0.55f);
            fillImage.raycastTarget = false;
            return fill;
        }

        /// <summary>
        /// Textura de vinheta (transparente no centro, opaca nas bordas).
        /// </summary>
        /// <returns>O sprite.</returns>
        private static Sprite VignetteSprite()
        {
            const int size = 64;
            Color[] c = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
                    float a = Mathf.Clamp01((d - 0.55f) / 0.6f);
                    c[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
            Texture2D tex = L2Core.SavePng("ui4_vinheta", c, size, size, false, true, FilterMode.Bilinear, true);
            string path = AssetDatabase.GetAssetPath(tex);
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ================================================================== Nível 5 provisório

        /// <summary>
        /// Cena "Level5": saída das galerias com a mensagem de que o Nível 5 está em construção (o totem do
        /// Nível 3 espera por ele), com botões para jogar o Nível 4 de novo ou voltar ao menu.
        /// </summary>
        public static void BuildLevel5Placeholder()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.08f, 0.08f, 0.1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.03f, 0.03f, 0.04f);
            RenderSettings.fogDensity = 0.05f;
            RenderSettings.skybox = null;

            GameObject set = new GameObject("Cenario");
            Place(set, "Saida_das_Galerias", new Vector3(0, 0, 4f), 180f);
            Place(set, "Rocha_Escura_G", new Vector3(-3.5f, 0, 3f), 20f);
            Place(set, "Rocha_Escura_M", new Vector3(3.2f, 0, 2.5f), -30f);
            Place(set, "Cogumelos_Altos", new Vector3(2.4f, 0, 5.5f), 0f);
            Place(set, "Fenda_de_Luz", new Vector3(0, 0, 6f), 0f);
            Place(set, "Restos_de_Minerador", new Vector3(-1.5f, 0, 1.5f), 40f);
            Place(set, "Pedregulhos_Soltos", new Vector3(1f, 0, 0.5f), 0f);
            Place(set, "FX4_Poeira_no_Ar", new Vector3(0, 1.5f, 3f), 0f);
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Chao";
            floor.transform.SetParent(set.transform, false);
            floor.transform.position = new Vector3(0, -0.5f, 4f);
            floor.transform.localScale = new Vector3(14f, 1f, 14f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = L4Palette.Dirt;

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 2f, -5f);
            camGo.transform.LookAt(new Vector3(0, 1.4f, 4f));
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            cam.fieldOfView = 55f;
            camGo.AddComponent<AudioListener>();

            GameObject managers = new GameObject("Menu");
            AudioSource music = managers.AddComponent<AudioSource>();
            music.clip = L2Core.Clip("cave_ambience_ogres");
            music.loop = true;
            music.playOnAwake = true;
            music.volume = 0.5f;
            AudioSource click = managers.AddComponent<AudioSource>();
            click.clip = L2Core.Clip("ui_click");
            click.playOnAwake = false;
            SimpleSceneMenu menu = managers.AddComponent<SimpleSceneMenu>();
            menu.clickSource = click;

            GameObject canvas = UIFactory.Canvas("Interface");
            Transform c = canvas.transform;
            RectTransform box = UIFactory.Panel(c, "Caixa", new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(980, 560));
            RectTransform title = UIFactory.Rect(box, "Titulo", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -20), new Vector2(0, 120), new Vector2(0.5f, 1));
            UIFactory.Label(title, "Texto", "Nível 5 — Em breve", 52, TextAnchor.MiddleCenter, UIFactory.Accent).fontStyle = FontStyle.Bold;
            RectTransform body = UIFactory.Rect(box, "Corpo", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            body.offsetMin = new Vector2(50, 210);
            body.offsetMax = new Vector2(-50, -150);
            UIFactory.Label(body, "Texto",
                "John escapou das galerias sem acordar a fúria dos ogros.\n" +
                "O Totem de Checkpoint do Nível 3 continua guardado para a próxima fase.\n\n<i>Esta fase ainda está em construção.</i>", 28, TextAnchor.UpperCenter, UIFactory.TextColor).supportRichText = true;
            Button again = UIFactory.Button(box, "Jogar o Nível 4 de novo", new Vector2(0, -150), 520f);
            Button toMenu = UIFactory.Button(box, "Menu principal", new Vector2(0, -235), 520f);
            menu.buttons = new[]
            {
                new SimpleSceneMenu.SceneButton { button = again, sceneName = "Level4" },
                new SimpleSceneMenu.SceneButton { button = toMenu, sceneName = "MainMenu" }
            };
            L2Core.Tag(canvas, AssetCategoryType.Interface, "Tela provisória do Nível 5");
            UIFactory.EventSystem();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Level4Builder.Level5ScenePath);
        }

        /// <summary>
        /// Instancia um prefab como filho de um grupo.
        /// </summary>
        /// <param name="parent">Grupo.</param>
        /// <param name="id">Prefab.</param>
        /// <param name="pos">Posição.</param>
        /// <param name="yaw">Rotação.</param>
        private static void Place(GameObject parent, string id, Vector3 pos, float yaw)
        {
            GameObject prefab = L2Core.Get(id);
            if (prefab == null) return;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        // ================================================================== Menu principal

        /// <summary>
        /// Acrescenta o botão "Nível 4" ao menu principal (se ainda não existir), reorganiza a coluna de botões
        /// e inclui os controles da furtividade na ajuda.
        /// </summary>
        /// <returns>true se a cena foi salva.</returns>
        public static bool PatchMainMenu()
        {
            if (!System.IO.File.Exists(LevelBuilder.MainMenuScenePath)) return false;
            var scene = EditorSceneManager.OpenScene(LevelBuilder.MainMenuScenePath, OpenSceneMode.Single);
            MainMenuController menu = Object.FindFirstObjectByType<MainMenuController>();
            if (menu == null) return false;
            menu.level4Scene = "Level4";
            if (menu.level4Button == null && menu.playButton != null)
            {
                Transform col = menu.playButton.transform.parent;
                menu.level4Button = UIFactory.Button(col, "Nível 4", new Vector2(0, -39));
                Reposition(menu.playButton, new Vector2(0, 195));
                Reposition(menu.level2Button, new Vector2(0, 117));
                Reposition(menu.level3Button, new Vector2(0, 39));
                Reposition(menu.controlsButton, new Vector2(0, -117));
                Reposition(menu.quitButton, new Vector2(0, -195));
            }
            if (menu.controlsPanel != null)
            {
                Transform list = menu.controlsPanel.transform.Find("Caixa/Lista/Texto");
                Text help = list != null ? list.GetComponent<Text>() : null;
                if (help != null && !help.text.Contains("Nível 4"))
                {
                    help.text += "\nNível 4: C/Ctrl agachar · F acender/apagar a tocha · G pedra (distração)";
                    help.fontSize = 22;
                    EditorUtility.SetDirty(help);
                }
            }
            EditorUtility.SetDirty(menu);
            EditorSceneManager.MarkSceneDirty(scene);
            return EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Muda a posição de um botão existente.
        /// </summary>
        /// <param name="b">Botão.</param>
        /// <param name="pos">Posição.</param>
        private static void Reposition(Button b, Vector2 pos)
        {
            if (b == null) return;
            RectTransform rt = (RectTransform)b.transform;
            rt.anchoredPosition = pos;
            EditorUtility.SetDirty(rt);
        }
    }
}
