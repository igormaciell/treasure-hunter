using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Interface do Nível 2: HUD do Nível 1 (corações, tesouros, missão, menus) + elementos de combate
    /// (arma, munição 25 balas, tambor, mira, marcador de acerto, inimigos da sala, dicas),
    /// a cena provisória do Nível 3 e os ajustes nas cenas já existentes (botão "Próximo nível" no fim
    /// do Nível 1 e botão "Nível 2" no menu principal), feitos sem regenerar essas cenas.
    /// </summary>
    public static class L2UI
    {
        private static Sprite revolverIcon, knifeIcon, bulletFull, bulletEmpty, stoneIcon, skullIcon;
        private static readonly Color Dim = new Color(1f, 1f, 1f, 0.6f);

        /// <summary>
        /// Cria os sprites em pixel art da interface de combate.
        /// </summary>
        public static void BuildSprites()
        {
            var pal = new Dictionary<char, Color>
            {
                { 'K', new Color(0.1f, 0.08f, 0.07f) }, { 'G', new Color(0.38f, 0.4f, 0.44f) }, { 'g', new Color(0.24f, 0.25f, 0.28f) },
                { 'W', new Color(0.85f, 0.88f, 0.92f) }, { 'B', new Color(0.45f, 0.28f, 0.14f) }, { 'b', new Color(0.3f, 0.18f, 0.09f) },
                { 'Y', new Color(1f, 0.82f, 0.35f) }, { 'y', new Color(0.75f, 0.55f, 0.2f) }, { 'S', new Color(0.55f, 0.52f, 0.48f) },
                { 's', new Color(0.36f, 0.34f, 0.31f) }, { 'C', new Color(0.92f, 0.88f, 0.78f) }, { 'c', new Color(0.65f, 0.6f, 0.52f) },
                { 'R', new Color(1f, 0.25f, 0.15f) }
            };
            revolverIcon = L2Core.PixelSprite("ui2_revolver", new[]
            {
                "................",
                "..K.............",
                ".KGKKKKKKKKKKKK.",
                ".KGGGGGGGGGGGGGK",
                ".KgGgggggggggggK",
                ".KGGGGKKKKKKKKK.",
                ".KgYgK..........",
                "..KggKK.........",
                "..KBBK.K........",
                "..KBbBKK........",
                "..KBBbK.........",
                "...KBBK.........",
                "...KBbK.........",
                "...KBBK.........",
                "....KK..........",
                "................"
            }, pal);
            knifeIcon = L2Core.PixelSprite("ui2_faca", new[]
            {
                "...............K",
                "..............KW",
                ".............KWK",
                "............KWK.",
                "...........KWK..",
                "..........KWK...",
                ".........KWK....",
                "........KWK.....",
                "....K..KWK......",
                "....KKKWK.......",
                ".....KYK........",
                "....KBKYK.......",
                "...KBbK.K.......",
                "..KBbK..........",
                ".KBbK...........",
                ".KKK............"
            }, pal);
            string[] bullet =
            {
                "..KK..",
                ".KYYK.",
                ".KYyK.",
                "KYYYyK",
                "KYYYyK",
                "KyyyyK",
                "KYYYyK",
                "KYYYyK",
                "KKKKKK"
            };
            bulletFull = L2Core.PixelSprite("ui2_bala_cheia", bullet, pal);
            string[] empty = new string[bullet.Length];
            for (int i = 0; i < bullet.Length; i++) empty[i] = bullet[i].Replace('Y', 'g').Replace('y', 'g');
            bulletEmpty = L2Core.PixelSprite("ui2_bala_vazia", empty, pal);
            stoneIcon = L2Core.PixelSprite("ui2_pedra", new[]
            {
                "...KKKK...",
                "..KSSSsK..",
                ".KSSSSssK.",
                "KSSSSSSssK",
                "KSSSsSSssK",
                "KsSSSSSssK",
                ".KssSSssK.",
                "..KKKKKK.."
            }, pal);
            skullIcon = L2Core.PixelSprite("ui2_cranio", new[]
            {
                "..KKKKKK..",
                ".KCCCCCCK.",
                "KCCCCCCCcK",
                "KCKKCCKKcK",
                "KCKRCCKRcK",
                "KCCCKKCCcK",
                ".KCCCCCcK.",
                "..KCKCKK..",
                "..KcKcK...",
                "...KKKK..."
            }, pal);
        }

        // ================================================================== HUD do Nível 2

        /// <summary>
        /// Monta o Canvas do Nível 2: reaproveita o HUD do Nível 1 (sem recriar seus sprites) e acrescenta o combate.
        /// </summary>
        /// <param name="combat">Combate do jogador.</param>
        /// <returns>O UIManager configurado.</returns>
        public static UIManager BuildHUD(PlayerCombat combat)
        {
            UIManager ui = UIFactory.BuildLevelUI();
            Transform c = ui.transform;

            // O Nível 2 não tem a tocha: remove o painel da ferramenta.
            Transform tool = c.Find("Ferramenta");
            if (tool != null) Object.DestroyImmediate(tool.gameObject);
            ui.toolIcon = null;
            ui.toolText = null;

            Transform help = c.Find("Controles/Texto");
            if (help != null)
                help.GetComponent<Text>().text = "WASD mover · Shift correr · Espaço pular · E interagir · Clique atirar · Botão dir. mirar · R recarregar · V golpe · G pedra · 1/2 arma · Esc pausa";
            RectTransform helpRt = c.Find("Controles") as RectTransform;
            if (helpRt != null) helpRt.sizeDelta = new Vector2(1500, 40);

            CombatHUD hud = ui.gameObject.AddComponent<CombatHUD>();
            hud.combat = combat;
            hud.revolverSprite = revolverIcon;
            hud.knifeSprite = knifeIcon;
            hud.bulletFull = bulletFull;
            hud.bulletEmpty = bulletEmpty;

            // Painel da arma (canto superior direito).
            RectTransform panel = UIFactory.Panel(c, "PainelDaArma", new Vector2(1, 1), new Vector2(-24, -24), new Vector2(470, 200));
            hud.weaponPanel = panel.gameObject;
            RectTransform icon = UIFactory.Rect(panel, "Icone", new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(96, 96), new Vector2(0, 1));
            hud.weaponIcon = icon.gameObject.AddComponent<Image>();
            hud.weaponIcon.sprite = revolverIcon;
            hud.weaponIcon.preserveAspect = true;
            hud.weaponIcon.raycastTarget = false;
            hud.weaponName = Text(panel, "NomeDaArma", "Revólver", 28, TextAnchor.UpperLeft, UIFactory.Accent, new Vector2(120, -6), new Vector2(330, 44));
            hud.weaponName.fontStyle = FontStyle.Bold;
            hud.ammoText = Text(panel, "Municao", "6 | 19", 46, TextAnchor.UpperLeft, UIFactory.Accent, new Vector2(120, -42), new Vector2(330, 64));
            hud.ammoText.fontStyle = FontStyle.Bold;

            hud.cylinderSlots = new Image[6];
            for (int i = 0; i < 6; i++)
            {
                RectTransform slot = UIFactory.Rect(panel, "Camara_" + (i + 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(132 + i * 34, -112), new Vector2(24, 34), new Vector2(0, 1));
                Image img = slot.gameObject.AddComponent<Image>();
                img.sprite = bulletFull;
                img.preserveAspect = true;
                img.raycastTarget = false;
                hud.cylinderSlots[i] = img;
            }
            RectTransform stone = UIFactory.Rect(panel, "IconePedra", new Vector2(0, 0), new Vector2(0, 0), new Vector2(16, 52), new Vector2(26, 22), new Vector2(0, 0));
            Image stoneImg = stone.gameObject.AddComponent<Image>();
            stoneImg.sprite = stoneIcon;
            stoneImg.preserveAspect = true;
            stoneImg.raycastTarget = false;
            hud.stonesText = Text(panel, "Pedras", "Pedras: 0 / 3", 20, TextAnchor.MiddleLeft, UIFactory.TextColor, new Vector2(40, 0), new Vector2(420, 34), true, 46f);
            hud.totalText = Text(panel, "Total", "Disparos restantes: 25 / 25", 22, TextAnchor.MiddleLeft, UIFactory.TextColor, new Vector2(16, 0), new Vector2(460, 36), true, 8f);
            hud.keysText = Text(c, "TeclasDaArma", "", 19, TextAnchor.UpperRight, Dim, Vector2.zero, new Vector2(760, 60), false, 0f, new Vector2(1, 1), new Vector2(-24, -232));

            // Mira (centro da tela): quatro hastes que abrem com o movimento e ficam vermelhas sobre um inimigo.
            RectTransform cross = UIFactory.Rect(c, "Mira", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 80), new Vector2(0.5f, 0.5f));
            hud.crosshair = cross;
            hud.crosshairArms = new RectTransform[4];
            List<Image> crossImages = new List<Image>();
            Vector2[] sizes = { new Vector2(4, 14), new Vector2(4, 14), new Vector2(14, 4), new Vector2(14, 4) };
            string[] names = { "Cima", "Baixo", "Esquerda", "Direita" };
            for (int i = 0; i < 4; i++)
            {
                RectTransform arm = UIFactory.Rect(cross, "Haste_" + names[i], new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, sizes[i], new Vector2(0.5f, 0.5f));
                Image img = arm.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                arm.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.8f);
                hud.crosshairArms[i] = arm;
                crossImages.Add(img);
            }
            RectTransform dot = UIFactory.Rect(cross, "Ponto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4, 4), new Vector2(0.5f, 0.5f));
            Image dotImg = dot.gameObject.AddComponent<Image>();
            dotImg.raycastTarget = false;
            crossImages.Add(dotImg);
            hud.crosshairImages = crossImages.ToArray();

            // Marcador de acerto (X em volta da mira) e texto ("NA CABEÇA!", "DERROTADO").
            RectTransform hit = UIFactory.Rect(c, "MarcadorDeAcerto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 80), new Vector2(0.5f, 0.5f));
            hud.hitMarker = hit.gameObject.AddComponent<CanvasGroup>();
            hud.hitMarker.blocksRaycasts = false;
            hud.hitMarker.interactable = false;
            List<Image> hitImages = new List<Image>();
            for (int i = 0; i < 4; i++)
            {
                float a = 45f + 90f * i;
                Vector2 dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                RectTransform line = UIFactory.Rect(hit, "Traco_" + (i + 1), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dir * 20f, new Vector2(4, 14), new Vector2(0.5f, 0.5f));
                line.localRotation = Quaternion.Euler(0, 0, a - 90f);
                Image img = line.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                hitImages.Add(img);
            }
            hud.hitMarkerImages = hitImages.ToArray();
            hud.hitText = Text(hit, "Texto", "", 26, TextAnchor.MiddleCenter, new Color(1f, 0.45f, 0.35f), new Vector2(0, -64), new Vector2(400, 40), false, 0f, new Vector2(0.5f, 0.5f), new Vector2(0, -64));
            hud.hitText.fontStyle = FontStyle.Bold;

            // Barra de recarga (abaixo da mira).
            RectTransform reload = UIFactory.Rect(c, "Recarga", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(220, 16), new Vector2(0.5f, 0.5f));
            hud.reloadGroup = reload.gameObject;
            Image back = reload.gameObject.AddComponent<Image>();
            back.color = new Color(0, 0, 0, 0.6f);
            back.raycastTarget = false;
            RectTransform fill = UIFactory.Rect(reload, "Preenchimento", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, new Vector2(0, 0.5f));
            fill.offsetMin = new Vector2(2, 2);
            fill.offsetMax = new Vector2(-2, -2);
            Image fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.color = UIFactory.Accent;
            fillImg.raycastTarget = false;
            hud.reloadFill = fill;
            Text(reload, "Texto", "Recarregando...", 20, TextAnchor.MiddleCenter, UIFactory.TextColor, new Vector2(0, 24), new Vector2(300, 30), false, 0f, new Vector2(0.5f, 0.5f), new Vector2(0, 24));

            // Inimigos restantes na sala (topo, abaixo do aviso).
            RectTransform enemies = UIFactory.Panel(c, "InimigosDaSala", new Vector2(0.5f, 1), new Vector2(0, -192), new Vector2(380, 56));
            hud.enemyPanel = enemies.gameObject;
            RectTransform skull = UIFactory.Rect(enemies, "Cranio", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(40, 40), new Vector2(0, 0.5f));
            Image skullImg = skull.gameObject.AddComponent<Image>();
            skullImg.sprite = skullIcon;
            skullImg.preserveAspect = true;
            skullImg.raycastTarget = false;
            hud.enemyText = Text(enemies, "Texto", "Inimigos: 0 / 0", 28, TextAnchor.MiddleCenter, UIFactory.Accent, new Vector2(24, 0), new Vector2(330, 56), false, 0f, new Vector2(0.5f, 0.5f), new Vector2(24, 0));

            // Dicas rápidas do combate ("Tambor vazio!", "Essa foi a última bala!").
            RectTransform hint = UIFactory.Rect(c, "DicaDeCombate", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 300), new Vector2(900, 56), new Vector2(0.5f, 0));
            hud.hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            hud.hintGroup.blocksRaycasts = false;
            hud.hintText = UIFactory.Label(hint, "Texto", "", 30, TextAnchor.MiddleCenter, new Color(1f, 0.6f, 0.4f));
            hud.hintText.fontStyle = FontStyle.Bold;

            // Os painéis novos ficam atrás dos menus (pausa, derrota, conclusão e escurecimento).
            foreach (string menu in new[] { "DiarioDeHabilidades", "MenuDePausa", "TelaDeDerrota", "TelaNivelConcluido", "FlashDeDano", "Escurecimento" })
            {
                Transform m = c.Find(menu);
                if (m != null) m.SetAsLastSibling();
            }

            // Etiquetas de inventário (a interface também conta como asset do nível).
            L2Core.Tag(ui.gameObject, AssetCategoryType.Interface, "HUD de combate (munição, mira, inimigos)");
            if (ui.pausePanel != null) L2Core.Tag(ui.pausePanel, AssetCategoryType.Interface, "Menu de pausa (reiniciar sala/jogo)");
            if (ui.gameOverPanel != null) L2Core.Tag(ui.gameOverPanel, AssetCategoryType.Interface, "Tela de derrota (reiniciar)");
            if (ui.completePanel != null) L2Core.Tag(ui.completePanel, AssetCategoryType.Interface, "Tela de nível concluído (próximo nível)");
            if (ui.journalPanel != null) L2Core.Tag(ui.journalPanel, AssetCategoryType.Interface, "Diário de habilidades");
            return ui;
        }

        /// <summary>
        /// Cria um texto com posição e tamanho próprios.
        /// </summary>
        /// <param name="parent">Pai.</param>
        /// <param name="name">Nome.</param>
        /// <param name="content">Conteúdo.</param>
        /// <param name="size">Tamanho da fonte.</param>
        /// <param name="align">Alinhamento.</param>
        /// <param name="color">Cor.</param>
        /// <param name="pos">Posição (ancorada no canto superior esquerdo do pai, se não houver âncora).</param>
        /// <param name="rect">Tamanho.</param>
        /// <param name="bottom">true para ancorar no canto inferior esquerdo do pai.</param>
        /// <param name="bottomOffset">Altura a partir da base (com bottom = true).</param>
        /// <param name="anchor">Âncora explícita (opcional).</param>
        /// <param name="anchoredPos">Posição com a âncora explícita.</param>
        /// <returns>O texto.</returns>
        private static Text Text(Transform parent, string name, string content, int size, TextAnchor align, Color color, Vector2 pos, Vector2 rect,
            bool bottom = false, float bottomOffset = 0f, Vector2? anchor = null, Vector2? anchoredPos = null)
        {
            RectTransform rt;
            if (anchor.HasValue)
                rt = UIFactory.Rect(parent, name, anchor.Value, anchor.Value, anchoredPos ?? pos, rect, anchor.Value);
            else if (bottom)
                rt = UIFactory.Rect(parent, name, new Vector2(0, 0), new Vector2(0, 0), new Vector2(pos.x, bottomOffset), rect, new Vector2(0, 0));
            else
                rt = UIFactory.Rect(parent, name, new Vector2(0, 1), new Vector2(0, 1), pos, rect, new Vector2(0, 1));
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = UIFactory.UIFont;
            t.text = content;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            Shadow sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.8f);
            sh.effectDistance = new Vector2(2, -2);
            return t;
        }

        // ================================================================== Cena provisória do Nível 3

        /// <summary>
        /// Cria a cena "Level3": um salão escuro com o portal para as profundezas e a mensagem de que o
        /// Nível 3 ainda está em construção, com botões para jogar o Nível 2 de novo ou voltar ao menu.
        /// </summary>
        public static void BuildLevel3Placeholder()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.08f, 0.07f, 0.07f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.03f);
            RenderSettings.fogDensity = 0.06f;
            RenderSettings.skybox = null;

            GameObject set = new GameObject("Cenario");
            Place(set, "Escada_de_Pedra", new Vector3(0, 0, 3f), 180f);
            Place(set, "Arco_Desmoronado", new Vector3(0, 0, 5f), 0f);
            Place(set, "Pilar_Rachado", new Vector3(-3.5f, 0, 3.5f), 20f);
            Place(set, "Pilar_Desabado", new Vector3(3.8f, 0, 3f), -30f);
            Place(set, "Candelabro_de_Ferro", new Vector3(-2.4f, 0, 1.5f), 0f);
            Place(set, "Candelabro_de_Ferro", new Vector3(2.4f, 0, 1.5f), 0f);
            Place(set, "Pilha_de_Cranios", new Vector3(-1.6f, 0, 0.5f), 40f);
            Place(set, "Teia_Grande", new Vector3(3f, 3f, 4.5f), 0f);
            Place(set, "FX2_Nevoa_Rasteira", new Vector3(0, 0.05f, 2f), 0f);
            Place(set, "FX2_Almas", new Vector3(0, 1.5f, 5f), 0f);
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Piso";
            floor.transform.SetParent(set.transform, false);
            floor.transform.position = new Vector3(0, -0.5f, 2f);
            floor.transform.localScale = new Vector3(20f, 1f, 14f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = L2Palette.Floor;
            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "Fundo";
            back.transform.SetParent(set.transform, false);
            back.transform.position = new Vector3(0, 4f, 7f);
            back.transform.localScale = new Vector3(20f, 8f, 1f);
            back.GetComponent<MeshRenderer>().sharedMaterial = L2Palette.Stone;

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 1.8f, -5.5f);
            camGo.transform.LookAt(new Vector3(0, 1.6f, 4f));
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
            cam.fieldOfView = 55f;
            camGo.AddComponent<AudioListener>();

            GameObject managers = new GameObject("Menu");
            AudioSource music = managers.AddComponent<AudioSource>();
            music.clip = L2Core.Clip("dungeon_ambience");
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
            RectTransform box = UIFactory.Panel(c, "Caixa", new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(980, 560));
            RectTransform title = UIFactory.Rect(box, "Titulo", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -20), new Vector2(0, 120), new Vector2(0.5f, 1));
            Text tt = UIFactory.Label(title, "Texto", "Nível 3 — As Profundezas de Añang", 50, TextAnchor.MiddleCenter, UIFactory.Accent);
            tt.fontStyle = FontStyle.Bold;
            RectTransform body = UIFactory.Rect(box, "Corpo", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            body.offsetMin = new Vector2(50, 210);
            body.offsetMax = new Vector2(-50, -150);
            UIFactory.Label(body, "Texto",
                "John venceu os guardiões das catacumbas e desceu a escadaria rumo ao coração do templo.\n\n" +
                "<i>Esta fase ainda está em construção — volte em breve!</i>", 28, TextAnchor.UpperCenter, UIFactory.TextColor).supportRichText = true;
            Button again = UIFactory.Button(box, "Jogar o Nível 2 de novo", new Vector2(0, -150), 520f);
            Button toMenu = UIFactory.Button(box, "Menu principal", new Vector2(0, -235), 520f);
            menu.buttons = new[]
            {
                new SimpleSceneMenu.SceneButton { button = again, sceneName = "Level2" },
                new SimpleSceneMenu.SceneButton { button = toMenu, sceneName = "MainMenu" }
            };
            L2Core.Tag(canvas, AssetCategoryType.Interface, "Tela provisória do Nível 3");
            UIFactory.EventSystem();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), L2Core.Level3ScenePath);
        }

        /// <summary>
        /// Instancia um prefab (Nível 2 ou Nível 1) como filho de um grupo.
        /// </summary>
        /// <param name="parent">Grupo.</param>
        /// <param name="id">Prefab.</param>
        /// <param name="pos">Posição.</param>
        /// <param name="yaw">Rotação em Y.</param>
        private static void Place(GameObject parent, string id, Vector3 pos, float yaw)
        {
            GameObject prefab = L2Core.Get(id);
            if (prefab == null) return;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        // ================================================================== Ajustes nas cenas existentes

        /// <summary>
        /// Abre a cena do Nível 1 e liga o fim dela ao Nível 2: define a próxima cena no GameManager e acrescenta
        /// o botão "Próximo nível" na tela de conclusão (se ainda não existir). Nada mais é alterado.
        /// </summary>
        /// <returns>true se a cena foi ajustada.</returns>
        public static bool PatchLevel1()
        {
            if (!System.IO.File.Exists(LevelBuilder.Level1ScenePath)) return false;
            var scene = EditorSceneManager.OpenScene(LevelBuilder.Level1ScenePath, OpenSceneMode.Single);
            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            UIManager ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            if (gm == null || ui == null) return false;
            gm.nextLevelScene = "Level2";
            EditorUtility.SetDirty(gm);

            if (ui.completeNextButton == null && ui.completePanel != null)
            {
                Transform box = ui.completePanel.transform.Find("Caixa");
                if (box != null)
                {
                    ui.completeNextButton = UIFactory.Button(box, "Próximo nível", new Vector2(0, -230));
                    Reposition(ui.completeRestartButton, new Vector2(-215, -320), 400f);
                    Reposition(ui.completeMenuButton, new Vector2(215, -320), 400f);
                }
            }
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(scene);
            return EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Abre o menu principal e acrescenta o botão "Nível 2" (se ainda não existir), reorganizando a coluna de botões
        /// e incluindo os controles de combate na ajuda.
        /// </summary>
        /// <returns>true se a cena foi ajustada.</returns>
        public static bool PatchMainMenu()
        {
            if (!System.IO.File.Exists(LevelBuilder.MainMenuScenePath)) return false;
            var scene = EditorSceneManager.OpenScene(LevelBuilder.MainMenuScenePath, OpenSceneMode.Single);
            MainMenuController menu = Object.FindFirstObjectByType<MainMenuController>();
            if (menu == null) return false;
            menu.level2Scene = "Level2";
            if (menu.level2Button == null && menu.playButton != null)
            {
                Transform col = menu.playButton.transform.parent;
                menu.level2Button = UIFactory.Button(col, "Nível 2", new Vector2(0, 60));
                Reposition(menu.playButton, new Vector2(0, 150), 420f);
                Reposition(menu.controlsButton, new Vector2(0, -30), 420f);
                Reposition(menu.quitButton, new Vector2(0, -120), 420f);
            }
            if (menu.controlsPanel != null)
            {
                Transform list = menu.controlsPanel.transform.Find("Caixa/Lista/Texto");
                Text help = list != null ? list.GetComponent<Text>() : null;
                if (help != null && !help.text.Contains("Nível 2"))
                {
                    help.text += "\nNível 2: clique esq. atirar · direito mirar · R recarregar\nV coronhada/faca · G pedra · 1/2 ou Q trocar arma";
                    help.fontSize = 26;
                    EditorUtility.SetDirty(help);
                }
            }
            EditorUtility.SetDirty(menu);
            EditorSceneManager.MarkSceneDirty(scene);
            return EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Muda a posição e a largura de um botão existente.
        /// </summary>
        /// <param name="b">Botão.</param>
        /// <param name="pos">Nova posição.</param>
        /// <param name="width">Nova largura.</param>
        private static void Reposition(Button b, Vector2 pos, float width)
        {
            if (b == null) return;
            RectTransform rt = (RectTransform)b.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
            EditorUtility.SetDirty(rt);
        }
    }
}
