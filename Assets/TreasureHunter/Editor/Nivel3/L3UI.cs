using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Interface do Nível 3: HUD do Nível 1 (corações, tesouros, missão, menus com reiniciar) + painel do totem,
    /// mensagem de posição válida/inválida, checkpoint ativo, quedas e aviso de queda; cena provisória do Nível 4;
    /// botão "Nível 3" no menu principal.
    /// </summary>
    public static class L3UI
    {
        private static Sprite totemIcon;

        /// <summary>
        /// Ícone do totem (pixel art).
        /// </summary>
        public static void BuildSprites()
        {
            var pal = new Dictionary<char, Color>
            {
                { 'K', new Color(0.08f, 0.08f, 0.1f) }, { 'S', new Color(0.55f, 0.53f, 0.5f) }, { 's', new Color(0.38f, 0.36f, 0.34f) },
                { 'Y', new Color(1f, 0.8f, 0.3f) }, { 'C', new Color(0.5f, 1f, 0.95f) }, { 'c', new Color(0.25f, 0.7f, 0.75f) }
            };
            totemIcon = L2Core.PixelSprite("ui3_totem", new[]
            {
                ".....KK.....",
                "....KCCK....",
                "...KCCcK....",
                "....KcK.....",
                "..KYKYKYK...",
                "..KYYYYYK...",
                "....KSK.....",
                "....KcK.....",
                "....KSK.....",
                "....KsK.....",
                "....KcK.....",
                "....KSK.....",
                "...KSSsK....",
                "..KSSSSsK...",
                "..KKKKKKK..."
            }, pal);
        }

        /// <summary>
        /// Monta o Canvas do Nível 3.
        /// </summary>
        /// <param name="totem">Habilidade do totem (jogador).</param>
        /// <param name="parkour">Parkour do jogador.</param>
        /// <returns>O UIManager.</returns>
        public static UIManager BuildHUD(TotemAbility totem, ParkourController parkour)
        {
            UIManager ui = UIFactory.BuildLevelUI();
            Transform c = ui.transform;
            Transform tool = c.Find("Ferramenta");
            if (tool != null) Object.DestroyImmediate(tool.gameObject);
            ui.toolIcon = null;
            ui.toolText = null;
            Transform help = c.Find("Controles/Texto");
            if (help != null)
                help.GetComponent<Text>().text = "WASD mover · Shift correr · Espaço pular · Pule contra uma borda para escalar · T (segurar) totem · E interagir · Tab habilidades · Esc pausa";
            RectTransform helpRt = c.Find("Controles") as RectTransform;
            if (helpRt != null) helpRt.sizeDelta = new Vector2(1500, 40);

            ParkourHUD hud = ui.gameObject.AddComponent<ParkourHUD>();
            hud.totem = totem;
            hud.parkour = parkour;

            // Painel do totem (canto superior direito).
            RectTransform panel = UIFactory.Panel(c, "PainelDoTotem", new Vector2(1, 1), new Vector2(-24, -24), new Vector2(420, 120));
            hud.totemPanel = panel.gameObject;
            RectTransform icon = UIFactory.Rect(panel, "Icone", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(84, 100), new Vector2(0, 0.5f));
            hud.totemIcon = icon.gameObject.AddComponent<Image>();
            hud.totemIcon.sprite = totemIcon;
            hud.totemIcon.preserveAspect = true;
            hud.totemIcon.raycastTarget = false;
            RectTransform statusRt = UIFactory.Rect(panel, "Estado", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            statusRt.offsetMin = new Vector2(96, 0);
            statusRt.offsetMax = Vector2.zero;
            hud.totemStatus = UIFactory.Label(statusRt, "Texto", "Totem pronto\n[T] segure para colocar", 24, TextAnchor.MiddleLeft, UIFactory.TextColor);

            // Checkpoint e quedas (abaixo do painel do totem).
            RectTransform info = UIFactory.Rect(c, "Checkpoint", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -150), new Vector2(560, 80), new Vector2(1, 1));
            hud.checkpointText = UIFactory.Label(info, "Texto", "Checkpoint: Início", 24, TextAnchor.UpperRight, UIFactory.Accent);
            RectTransform falls = UIFactory.Rect(c, "Quedas", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -190), new Vector2(560, 50), new Vector2(1, 1));
            hud.fallsText = UIFactory.Label(falls, "Texto", "Quedas no abismo: 0", 22, TextAnchor.UpperRight, new Color(1f, 1f, 1f, 0.7f));

            // Mensagem do posicionamento (acima do prompt).
            RectTransform placement = UIFactory.Rect(c, "PosicaoDoTotem", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 360), new Vector2(1100, 90), new Vector2(0.5f, 0));
            hud.placementText = UIFactory.Label(placement, "Texto", "", 30, TextAnchor.MiddleCenter, Color.white);
            hud.placementText.fontStyle = FontStyle.Bold;
            hud.placementText.supportRichText = true;

            // Aviso de queda no abismo.
            RectTransform warn = UIFactory.Rect(c, "AvisoDeQueda", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(900, 70), new Vector2(0.5f, 0.5f));
            hud.voidWarning = warn.gameObject.AddComponent<CanvasGroup>();
            hud.voidWarning.blocksRaycasts = false;
            Text wt = UIFactory.Label(warn, "Texto", "Caindo no abismo... voltando ao checkpoint", 34, TextAnchor.MiddleCenter, new Color(0.7f, 0.85f, 1f));
            wt.fontStyle = FontStyle.Bold;

            // Dicas rápidas.
            RectTransform hint = UIFactory.Rect(c, "DicaDoParkour", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 300), new Vector2(1100, 56), new Vector2(0.5f, 0));
            hud.hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            hud.hintGroup.blocksRaycasts = false;
            hud.hintText = UIFactory.Label(hint, "Texto", "", 28, TextAnchor.MiddleCenter, new Color(0.7f, 1f, 0.95f));
            hud.hintText.fontStyle = FontStyle.Bold;

            foreach (string menu in new[] { "DiarioDeHabilidades", "MenuDePausa", "TelaDeDerrota", "TelaNivelConcluido", "FlashDeDano", "Escurecimento" })
            {
                Transform m = c.Find(menu);
                if (m != null) m.SetAsLastSibling();
            }

            L2Core.Tag(ui.gameObject, AssetCategoryType.Interface, "HUD do parkour (totem, checkpoint, quedas)");
            if (ui.pausePanel != null) L2Core.Tag(ui.pausePanel, AssetCategoryType.Interface, "Menu de pausa (reiniciar sala/jogo)");
            if (ui.gameOverPanel != null) L2Core.Tag(ui.gameOverPanel, AssetCategoryType.Interface, "Tela de derrota (reiniciar)");
            if (ui.completePanel != null) L2Core.Tag(ui.completePanel, AssetCategoryType.Interface, "Tela de nível concluído (próximo nível)");
            if (ui.journalPanel != null) L2Core.Tag(ui.journalPanel, AssetCategoryType.Interface, "Diário de habilidades");
            L2Core.Tag(placement.gameObject, AssetCategoryType.Interface, "Mensagem de posição do totem");
            return ui;
        }

        // ================================================================== Nível 4 provisório

        /// <summary>
        /// Cena "Level4": salão de cristais com a mensagem de que o Nível 4 está em construção,
        /// e botões para jogar o Nível 3 de novo ou voltar ao menu.
        /// </summary>
        public static void BuildLevel4Placeholder()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.08f, 0.12f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.05f);
            RenderSettings.fogDensity = 0.05f;
            RenderSettings.skybox = null;

            GameObject set = new GameObject("Cenario");
            Place(set, "Plataforma_Rochosa_G", new Vector3(0, 0, 2f), 0f);
            Place(set, "Cristal_Grande", new Vector3(-2.6f, 0, 3.4f), 20f);
            Place(set, "Cristal_Aglomerado", new Vector3(2.4f, 0, 3f), -30f);
            Place(set, "Cristal_Rosa", new Vector3(1.2f, 0, 0.8f), 0f);
            Place(set, "Cristal_Flutuante", new Vector3(0, 0.5f, 3.5f), 0f);
            Place(set, "Coluna_da_Masmorra", new Vector3(-4.5f, -1f, 6f), 0f);
            Place(set, "Torre_Arruinada", new Vector3(5f, -2f, 7f), 25f);
            Place(set, "Estalactite_G", new Vector3(-1.5f, 8f, 5f), 0f);
            Place(set, "Estalactite_M", new Vector3(2f, 8f, 6f), 0f);
            Place(set, "FX3_Esporos", new Vector3(0, 2f, 3f), 0f);
            Place(set, "FX3_Nevoa_do_Abismo", new Vector3(0, -6f, 6f), 0f);

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 2f, -5f);
            camGo.transform.LookAt(new Vector3(0, 1.2f, 3f));
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            cam.fieldOfView = 55f;
            camGo.AddComponent<AudioListener>();

            GameObject managers = new GameObject("Menu");
            AudioSource music = managers.AddComponent<AudioSource>();
            music.clip = L2Core.Clip("cave_ambience_crystal");
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
            UIFactory.Label(title, "Texto", "Nível 4 — Em breve", 52, TextAnchor.MiddleCenter, UIFactory.Accent).fontStyle = FontStyle.Bold;
            RectTransform body = UIFactory.Rect(box, "Corpo", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            body.offsetMin = new Vector2(50, 210);
            body.offsetMax = new Vector2(-50, -150);
            UIFactory.Label(body, "Texto",
                "John atravessou a Caverna de Cristal levando o Totem de Checkpoint.\n" +
                "O totem fica guardado para as próximas fases.\n\n<i>Esta fase ainda está em construção.</i>", 28, TextAnchor.UpperCenter, UIFactory.TextColor).supportRichText = true;
            Button again = UIFactory.Button(box, "Jogar o Nível 3 de novo", new Vector2(0, -150), 520f);
            Button toMenu = UIFactory.Button(box, "Menu principal", new Vector2(0, -235), 520f);
            menu.buttons = new[]
            {
                new SimpleSceneMenu.SceneButton { button = again, sceneName = "Level3" },
                new SimpleSceneMenu.SceneButton { button = toMenu, sceneName = "MainMenu" }
            };
            L2Core.Tag(canvas, AssetCategoryType.Interface, "Tela provisória do Nível 4");
            UIFactory.EventSystem();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Level3Builder.Level4ScenePath);
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
        /// Acrescenta o botão "Nível 3" ao menu principal (se ainda não existir) e os controles do parkour na ajuda.
        /// </summary>
        /// <returns>true se a cena foi salva.</returns>
        public static bool PatchMainMenu()
        {
            if (!System.IO.File.Exists(LevelBuilder.MainMenuScenePath)) return false;
            var scene = EditorSceneManager.OpenScene(LevelBuilder.MainMenuScenePath, OpenSceneMode.Single);
            MainMenuController menu = Object.FindFirstObjectByType<MainMenuController>();
            if (menu == null) return false;
            menu.level3Scene = "Level3";
            if (menu.level3Button == null && menu.playButton != null)
            {
                Transform col = menu.playButton.transform.parent;
                menu.level3Button = UIFactory.Button(col, "Nível 3", new Vector2(0, 10));
                Reposition(menu.playButton, new Vector2(0, 180));
                Reposition(menu.level2Button, new Vector2(0, 95));
                Reposition(menu.controlsButton, new Vector2(0, -75));
                Reposition(menu.quitButton, new Vector2(0, -160));
            }
            if (menu.controlsPanel != null)
            {
                Transform list = menu.controlsPanel.transform.Find("Caixa/Lista/Texto");
                Text help = list != null ? list.GetComponent<Text>() : null;
                if (help != null && !help.text.Contains("Nível 3"))
                {
                    help.text += "\nNível 3: pule contra bordas para escalar · T (segurar) totem";
                    help.fontSize = 24;
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
