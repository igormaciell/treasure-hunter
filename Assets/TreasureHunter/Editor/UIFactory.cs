using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Monta as interfaces (Canvas) do jogo: HUD e menus do Nível 1 e o menu principal.
    /// Visual inspirado em pedra e inscrições antigas, como descrito no GDD.
    /// </summary>
    public static class UIFactory
    {
        private static Font font;
        private static Sprite heartFull, heartEmpty, torchIcon;
        private static readonly Color PanelColor = new Color(0.16f, 0.13f, 0.1f, 0.88f);
        private static readonly Color BorderColor = new Color(0.78f, 0.62f, 0.3f, 1f);
        private static readonly Color TextColor = new Color(0.96f, 0.91f, 0.78f);
        private static readonly Color Accent = new Color(1f, 0.82f, 0.35f);

        /// <summary>
        /// Carrega a fonte padrão e cria os sprites em pixel art da interface.
        /// </summary>
        public static void Prepare()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var pal = new Dictionary<char, Color>
            {
                { 'R', new Color(0.9f, 0.12f, 0.15f) }, { 'r', new Color(0.6f, 0.05f, 0.08f) }, { 'W', Color.white },
                { 'K', new Color(0.12f, 0.08f, 0.06f) }, { 'G', new Color(0.35f, 0.3f, 0.28f) },
                { 'B', new Color(0.45f, 0.3f, 0.15f) }, { 'O', new Color(1f, 0.6f, 0.1f) }, { 'Y', new Color(1f, 0.9f, 0.3f) }
            };
            string[] heart =
            {
                "...........",
                ".KKK...KKK.",
                "KRRRK.KRWRK",
                "KRRRRKRRRWK",
                "KRRRRRRRRRK",
                "KrRRRRRRRRK",
                ".KrRRRRRRK.",
                "..KrRRRRK..",
                "...KrRRK...",
                "....KrK....",
                ".....K....."
            };
            heartFull = TextureFactory.PixelSprite("ui_heart_full", heart, pal);
            string[] empty = new string[heart.Length];
            for (int i = 0; i < heart.Length; i++) empty[i] = heart[i].Replace('R', 'G').Replace('r', 'G').Replace('W', 'G');
            heartEmpty = TextureFactory.PixelSprite("ui_heart_empty", empty, pal);
            torchIcon = TextureFactory.PixelSprite("ui_torch", new[]
            {
                "....Y....",
                "...YOY...",
                "..YOOOY..",
                "..OOOOO..",
                "...OOO...",
                "...KBK...",
                "....B....",
                "....B....",
                "....B....",
                "....B....",
                "....B...."
            }, pal);
        }

        // ================================================================== Blocos básicos

        /// <summary>
        /// Cria um Canvas em tela cheia com escalonamento para 1920×1080.
        /// </summary>
        /// <param name="name">Nome do objeto.</param>
        /// <returns>O GameObject do Canvas.</returns>
        public static GameObject Canvas(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            return go;
        }

        /// <summary>
        /// Cria o EventSystem compatível com o sistema de entrada ativo.
        /// </summary>
        public static void EventSystem()
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        /// <summary>
        /// Cria um objeto de interface com RectTransform ancorado.
        /// </summary>
        /// <param name="parent">Pai.</param>
        /// <param name="name">Nome.</param>
        /// <param name="anchorMin">Âncora mínima (0..1).</param>
        /// <param name="anchorMax">Âncora máxima (0..1).</param>
        /// <param name="pos">Posição relativa à âncora.</param>
        /// <param name="size">Tamanho (quando as âncoras coincidem).</param>
        /// <param name="pivot">Pivô.</param>
        /// <returns>O RectTransform.</returns>
        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>
        /// Estica um RectTransform para ocupar todo o pai.
        /// </summary>
        /// <param name="rt">RectTransform.</param>
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Cria um painel de "pedra" com borda dourada.
        /// </summary>
        /// <param name="parent">Pai.</param>
        /// <param name="name">Nome.</param>
        /// <param name="anchor">Âncora (mesma para min e max).</param>
        /// <param name="pos">Posição.</param>
        /// <param name="size">Tamanho.</param>
        /// <returns>O RectTransform do painel.</returns>
        public static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            RectTransform rt = Rect(parent, name, anchor, anchor, pos, size, anchor);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = PanelColor;
            Outline o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = BorderColor;
            o.effectDistance = new Vector2(3, -3);
            return rt;
        }

        /// <summary>
        /// Cria um texto.
        /// </summary>
        /// <param name="parent">Pai.</param>
        /// <param name="name">Nome.</param>
        /// <param name="content">Conteúdo inicial.</param>
        /// <param name="size">Tamanho da fonte.</param>
        /// <param name="align">Alinhamento.</param>
        /// <param name="color">Cor.</param>
        /// <returns>O componente Text (esticado no pai, com margem).</returns>
        public static Text Label(Transform parent, string name, string content, int size, TextAnchor align, Color color)
        {
            RectTransform rt = Rect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            rt.offsetMin = new Vector2(16, 10);
            rt.offsetMax = new Vector2(-16, -10);
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = font;
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

        /// <summary>
        /// Cria um botão com texto.
        /// </summary>
        /// <param name="parent">Pai.</param>
        /// <param name="label">Texto do botão.</param>
        /// <param name="pos">Posição (relativa ao centro do pai).</param>
        /// <param name="width">Largura.</param>
        /// <returns>O botão.</returns>
        public static Button Button(Transform parent, string label, Vector2 pos, float width = 420f)
        {
            RectTransform rt = Rect(parent, "Botao_" + label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(width, 70), new Vector2(0.5f, 0.5f));
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.34f, 0.27f, 0.18f, 1f);
            Outline o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = BorderColor;
            o.effectDistance = new Vector2(2, -2);
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = ColorBlock.defaultColorBlock;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.3f, 1.2f, 0.9f);
            cb.pressedColor = new Color(0.7f, 0.6f, 0.5f);
            cb.selectedColor = new Color(1.15f, 1.1f, 0.9f);
            b.colors = cb;
            Label(rt, "Texto", label, 30, TextAnchor.MiddleCenter, TextColor);
            return b;
        }

        /// <summary>
        /// Cria uma imagem em tela cheia (flash de dano e escurecimento de transição).
        /// </summary>
        /// <param name="parent">Pai.</param>
        /// <param name="name">Nome.</param>
        /// <param name="color">Cor inicial.</param>
        /// <returns>A imagem.</returns>
        private static Image FullScreen(Transform parent, string name, Color color)
        {
            RectTransform rt = Rect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            Stretch(rt);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// Cria um painel em tela cheia escurecida com uma caixa central (menus).
        /// </summary>
        /// <param name="parent">Pai.</param>
        /// <param name="name">Nome.</param>
        /// <param name="boxSize">Tamanho da caixa central.</param>
        /// <param name="box">Saída: RectTransform da caixa.</param>
        /// <returns>O GameObject do painel (para ligar/desligar).</returns>
        private static GameObject Modal(Transform parent, string name, Vector2 boxSize, out RectTransform box)
        {
            RectTransform bg = Rect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            Stretch(bg);
            Image dim = bg.gameObject.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.6f);
            box = Panel(bg, "Caixa", new Vector2(0.5f, 0.5f), Vector2.zero, boxSize);
            return bg.gameObject;
        }

        // ================================================================== HUD do Nível 1

        /// <summary>
        /// Monta o Canvas completo do Nível 1 e preenche as referências do UIManager.
        /// </summary>
        /// <returns>O UIManager configurado.</returns>
        public static UIManager BuildLevelUI()
        {
            GameObject canvas = Canvas("Interface");
            Transform c = canvas.transform;
            UIManager ui = canvas.AddComponent<UIManager>();
            ui.heartFull = heartFull;
            ui.heartEmpty = heartEmpty;

            // Corações (canto superior esquerdo).
            RectTransform heartsRow = Rect(c, "Coracoes", new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -26), new Vector2(400, 64), new Vector2(0, 1));
            ui.hearts = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                RectTransform h = Rect(heartsRow, "Coracao_" + (i + 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(i * 70, 0), new Vector2(60, 60), new Vector2(0, 1));
                Image img = h.gameObject.AddComponent<Image>();
                img.sprite = heartFull;
                img.preserveAspect = true;
                img.raycastTarget = false;
                ui.hearts[i] = img;
            }

            RectTransform treasure = Rect(c, "Tesouros", new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -92), new Vector2(700, 50), new Vector2(0, 1));
            ui.treasureText = Label(treasure, "Texto", "Moedas 0   Joias 0   Artefatos 0/3", 26, TextAnchor.MiddleLeft, Accent);
            RectTransform objective = Rect(c, "Missao", new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -138), new Vector2(760, 90), new Vector2(0, 1));
            ui.objectiveText = Label(objective, "Texto", "Missão:", 26, TextAnchor.UpperLeft, TextColor);

            // Ferramenta (canto superior direito).
            RectTransform tool = Panel(c, "Ferramenta", new Vector2(1, 1), new Vector2(-24, -24), new Vector2(330, 100));
            RectTransform icon = Rect(tool, "Icone", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(76, 76), new Vector2(0, 0.5f));
            ui.toolIcon = icon.gameObject.AddComponent<Image>();
            ui.toolIcon.sprite = torchIcon;
            ui.toolIcon.preserveAspect = true;
            RectTransform toolTextRt = Rect(tool, "Texto", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            toolTextRt.offsetMin = new Vector2(90, 0);
            toolTextRt.offsetMax = Vector2.zero;
            ui.toolText = Label(toolTextRt, "Label", "Tocha\n[F] erguer", 26, TextAnchor.MiddleLeft, TextColor);

            // Ajuda de controles (canto inferior direito).
            RectTransform help = Rect(c, "Controles", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 16), new Vector2(900, 40), new Vector2(1, 0));
            Label(help, "Texto", "WASD mover · Shift correr · Espaço pular · E interagir · F tocha · Tab habilidades · Esc pausa", 20, TextAnchor.LowerRight, new Color(1, 1, 1, 0.6f));

            // Prompt de interação.
            RectTransform prompt = Panel(c, "Prompt", new Vector2(0.5f, 0), new Vector2(0, 230), new Vector2(560, 64));
            ui.promptPanel = prompt.gameObject;
            ui.promptText = Label(prompt, "Texto", "[E] Interagir", 30, TextAnchor.MiddleCenter, Accent);

            // Fala de John (legenda).
            RectTransform line = Rect(c, "Fala", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(1300, 100), new Vector2(0.5f, 0));
            ui.lineGroup = line.gameObject.AddComponent<CanvasGroup>();
            ui.lineGroup.blocksRaycasts = false;
            ui.lineText = Label(line, "Texto", "", 30, TextAnchor.MiddleCenter, Color.white);

            // Tutorial (canto inferior esquerdo).
            RectTransform tut = Panel(c, "Tutorial", new Vector2(0, 0), new Vector2(24, 70), new Vector2(560, 150));
            ui.tutorialGroup = tut.gameObject.AddComponent<CanvasGroup>();
            ui.tutorialGroup.blocksRaycasts = false;
            ui.tutorialText = Label(tut, "Texto", "", 24, TextAnchor.MiddleLeft, TextColor);

            // Aviso (habilidade, artefato, inscrição).
            RectTransform toast = Panel(c, "Aviso", new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(820, 150));
            ui.toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            ui.toastGroup.blocksRaycasts = false;
            RectTransform toastTitle = Rect(toast, "Titulo", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -8), new Vector2(0, 50), new Vector2(0.5f, 1));
            ui.toastTitle = Label(toastTitle, "Texto", "", 32, TextAnchor.MiddleCenter, Accent);
            ui.toastTitle.fontStyle = FontStyle.Bold;
            RectTransform toastBody = Rect(toast, "Corpo", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            toastBody.offsetMin = new Vector2(0, 0);
            toastBody.offsetMax = new Vector2(0, -52);
            ui.toastBody = Label(toastBody, "Texto", "", 24, TextAnchor.UpperCenter, TextColor);

            // Título da sala.
            RectTransform title = Rect(c, "TituloDaSala", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1400, 160), new Vector2(0.5f, 0.5f));
            ui.roomTitleGroup = title.gameObject.AddComponent<CanvasGroup>();
            ui.roomTitleGroup.blocksRaycasts = false;
            RectTransform t1 = Rect(title, "Titulo", new Vector2(0, 0.45f), new Vector2(1, 1), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            t1.offsetMin = Vector2.zero; t1.offsetMax = Vector2.zero;
            ui.roomTitleText = Label(t1, "Texto", "", 54, TextAnchor.MiddleCenter, Accent);
            ui.roomTitleText.fontStyle = FontStyle.Bold;
            RectTransform t2 = Rect(title, "Subtitulo", new Vector2(0, 0), new Vector2(1, 0.45f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            t2.offsetMin = Vector2.zero; t2.offsetMax = Vector2.zero;
            ui.roomSubtitleText = Label(t2, "Texto", "", 34, TextAnchor.MiddleCenter, TextColor);

            ui.damageFlash = FullScreen(c, "FlashDeDano", new Color(0.8f, 0, 0, 0));

            // Diário de Habilidades (Tab).
            ui.journalPanel = Modal(c, "DiarioDeHabilidades", new Vector2(900, 760), out RectTransform jbox);
            RectTransform jt = Rect(jbox, "Titulo", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -10), new Vector2(0, 70), new Vector2(0.5f, 1));
            Label(jt, "Texto", "Diário de Habilidades", 44, TextAnchor.MiddleCenter, Accent).fontStyle = FontStyle.Bold;
            RectTransform jb = Rect(jbox, "Lista", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            jb.offsetMin = new Vector2(30, 60); jb.offsetMax = new Vector2(-30, -90);
            ui.journalText = Label(jb, "Texto", "", 26, TextAnchor.UpperLeft, TextColor);
            RectTransform jh = Rect(jbox, "Dica", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 10), new Vector2(0, 50), new Vector2(0.5f, 0));
            Label(jh, "Texto", "[Tab] fechar", 22, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.6f));

            // Pausa.
            ui.pausePanel = Modal(c, "MenuDePausa", new Vector2(620, 560), out RectTransform pbox);
            RectTransform pt = Rect(pbox, "Titulo", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -20), new Vector2(0, 80), new Vector2(0.5f, 1));
            Label(pt, "Texto", "Pausa", 52, TextAnchor.MiddleCenter, Accent).fontStyle = FontStyle.Bold;
            ui.resumeButton = Button(pbox, "Continuar", new Vector2(0, 110));
            ui.pauseRestartRoomButton = Button(pbox, "Reiniciar sala", new Vector2(0, 20));
            ui.pauseRestartLevelButton = Button(pbox, "Reiniciar jogo", new Vector2(0, -70));
            ui.pauseMenuButton = Button(pbox, "Menu principal", new Vector2(0, -160));

            // Derrota.
            ui.gameOverPanel = Modal(c, "TelaDeDerrota", new Vector2(700, 560), out RectTransform gbox);
            RectTransform gt = Rect(gbox, "Titulo", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -20), new Vector2(0, 150), new Vector2(0.5f, 1));
            Label(gt, "Texto", "Você foi derrotado\n<size=28>O templo cobrou seu preço...</size>", 52, TextAnchor.MiddleCenter, new Color(1f, 0.45f, 0.4f)).supportRichText = true;
            ui.gameOverContinueButton = Button(gbox, "Voltar ao checkpoint", new Vector2(0, 30), 460);
            ui.gameOverRestartButton = Button(gbox, "Reiniciar jogo", new Vector2(0, -60), 460);
            ui.gameOverMenuButton = Button(gbox, "Menu principal", new Vector2(0, -150), 460);

            // Nível concluído.
            ui.completePanel = Modal(c, "TelaNivelConcluido", new Vector2(860, 780), out RectTransform cbox);
            RectTransform ct = Rect(cbox, "Titulo", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -20), new Vector2(0, 140), new Vector2(0.5f, 1));
            ui.completeTitle = Label(ct, "Texto", "", 46, TextAnchor.MiddleCenter, Accent);
            ui.completeTitle.fontStyle = FontStyle.Bold;
            RectTransform cs = Rect(cbox, "Resumo", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            cs.offsetMin = new Vector2(50, 230); cs.offsetMax = new Vector2(-50, -170);
            ui.completeSummary = Label(cs, "Texto", "", 28, TextAnchor.UpperLeft, TextColor);
            ui.completeRestartButton = Button(cbox, "Reiniciar jogo", new Vector2(0, -230));
            ui.completeMenuButton = Button(cbox, "Menu principal", new Vector2(0, -320));

            ui.fadeImage = FullScreen(c, "Escurecimento", new Color(0, 0, 0, 0));
            EventSystem();
            return ui;
        }

        // ================================================================== Menu principal

        /// <summary>
        /// Monta o Canvas do menu principal.
        /// </summary>
        /// <param name="menu">Controlador que receberá as referências.</param>
        public static void BuildMainMenu(MainMenuController menu)
        {
            GameObject canvas = Canvas("MenuPrincipal");
            Transform c = canvas.transform;

            RectTransform title = Rect(c, "Titulo", new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -110), new Vector2(1000, 160), new Vector2(0, 1));
            Text tt = Label(title, "Texto", "TREASURE HUNTER", 96, TextAnchor.MiddleLeft, Accent);
            tt.fontStyle = FontStyle.Bold;
            RectTransform sub = Rect(c, "Subtitulo", new Vector2(0, 1), new Vector2(0, 1), new Vector2(115, -260), new Vector2(1000, 60), new Vector2(0, 1));
            Label(sub, "Texto", "O Templo de Añang  —  Nível 1: A Entrada do Templo", 32, TextAnchor.MiddleLeft, TextColor);

            RectTransform col = Rect(c, "Botoes", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(330, -80), new Vector2(460, 400), new Vector2(0.5f, 0.5f));
            menu.playButton = Button(col, "Jogar", new Vector2(0, 110));
            menu.controlsButton = Button(col, "Controles", new Vector2(0, 20));
            menu.quitButton = Button(col, "Sair", new Vector2(0, -70));

            RectTransform credits = Rect(c, "Creditos", new Vector2(0, 0), new Vector2(0, 0), new Vector2(110, 30), new Vector2(1400, 60), new Vector2(0, 0));
            Label(credits, "Texto", "UVV · Ciência da Computação · Davi D'Ávila, Hugo Almeida, Igor Maciel, Pedro Souza · 2026", 22, TextAnchor.LowerLeft, new Color(1, 1, 1, 0.65f));

            menu.controlsPanel = Modal(c, "PainelControles", new Vector2(900, 720), out RectTransform box);
            RectTransform ht = Rect(box, "Titulo", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -10), new Vector2(0, 80), new Vector2(0.5f, 1));
            Label(ht, "Texto", "Controles", 48, TextAnchor.MiddleCenter, Accent).fontStyle = FontStyle.Bold;
            RectTransform hb = Rect(box, "Lista", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            hb.offsetMin = new Vector2(60, 130); hb.offsetMax = new Vector2(-60, -100);
            Label(hb, "Texto",
                "W A S D / setas — mover\n" +
                "Mouse — girar a câmera\n" +
                "Shift — correr\n" +
                "Espaço — pular (segure para pular mais alto)\n" +
                "E — interagir (alavancas, braseiros, símbolos)\n" +
                "Andar contra um caixote — empurrar\n" +
                "F / clique esquerdo — erguer a tocha\n" +
                "Tab — Diário de Habilidades\n" +
                "Esc — pausa (reiniciar sala ou jogo)", 30, TextAnchor.UpperLeft, TextColor);
            menu.closeControlsButton = Button(box, "Voltar", new Vector2(0, -290));

            EventSystem();
        }
    }
}
