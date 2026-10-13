using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Ponto de entrada do gerador (menu "Treasure Hunter") e funções genéricas de construção de cena:
    /// blocos de cenário, salas com paredes/aberturas, colocação de prefabs e cenas de menu.
    /// O conteúdo específico do Nível 1 está em LevelBuilder.Level1.cs.
    /// </summary>
    public static partial class LevelBuilder
    {
        public const string MainMenuScenePath = BuilderCore.ScenesPath + "/MainMenu.unity";
        public const string Level1ScenePath = BuilderCore.ScenesPath + "/Level1.unity";

        private static readonly Dictionary<string, Mesh> blockMeshes = new Dictionary<string, Mesh>();
        private static Transform currentParent;
        private static int blockCounter;

        /// <summary>Lados de uma sala retangular.</summary>
        public enum Side { South, North, West, East }

        /// <summary>Abertura (passagem/porta) em uma parede.</summary>
        public struct Opening
        {
            public Side side; public float center; public float width; public float height; public float bottom;
            /// <summary>
            /// Cria uma abertura.
            /// </summary>
            /// <param name="side">Parede.</param>
            /// <param name="center">Centro ao longo da parede (x para Sul/Norte, z para Oeste/Leste).</param>
            /// <param name="width">Largura.</param>
            /// <param name="height">Altura.</param>
            /// <param name="bottom">Altura do peitoril (0 = rente ao chão).</param>
            public Opening(Side side, float center, float width, float height, float bottom = 0f)
            {
                this.side = side; this.center = center; this.width = width; this.height = height; this.bottom = bottom;
            }
        }

        // ================================================================== Menu do editor

        /// <summary>
        /// Gera TUDO: texturas, materiais, partículas, prefabs, personagem, animações,
        /// cena do menu principal, cena do Nível 1 e configurações de build.
        /// </summary>
        [MenuItem("Treasure Hunter/Construir Nível 1 (gerar tudo)", priority = 1)]
        public static void BuildEverything()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                Progress("Preparando pastas e tags", 0.02f);
                BuilderCore.ResetRandom();
                TextureFactory.Clear();
                MaterialFactory.Clear();
                blockMeshes.Clear();
                blockCounter = 0;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                BuilderCore.PrepareFolders();
                BuilderCore.EnsureTags();
                BuilderCore.ConfigureAudioImport();

                Progress("Texturas e materiais voxel", 0.1f);
                Palette.Build();
                Progress("Partículas", 0.2f);
                ParticleFactory.BuildAll();
                Progress("Prefabs do cenário", 0.3f);
                PropFactory.BuildAll();
                Progress("Personagem e animações", 0.45f);
                CharacterFactory.Build();
                UIFactory.Prepare();
                AssetDatabase.SaveAssets();

                Progress("Cena do menu principal", 0.55f);
                BuildMainMenuScene();
                Progress("Cena do Nível 1", 0.65f);
                BuildLevel1Scene();

                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(MainMenuScenePath, true),
                    new EditorBuildSettingsScene(Level1ScenePath, true)
                };
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorSceneManager.OpenScene(Level1ScenePath);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            EditorUtility.DisplayDialog("Treasure Hunter",
                "Nível 1 gerado com sucesso!\n\nCenas: Assets/TreasureHunter/Scenes (MainMenu e Level1).\n" +
                "Inventário de assets: Assets/TreasureHunter/Docs/Inventario_Nivel1.md\n\n" +
                "Abra a cena MainMenu e aperte Play.", "OK");
        }

        /// <summary>
        /// Abre a cena do menu principal (atalho).
        /// </summary>
        [MenuItem("Treasure Hunter/Abrir cena do Menu", priority = 20)]
        public static void OpenMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(MainMenuScenePath);
        }

        /// <summary>
        /// Abre a cena do Nível 1 (atalho).
        /// </summary>
        [MenuItem("Treasure Hunter/Abrir cena do Nível 1", priority = 21)]
        public static void OpenLevel1()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Level1ScenePath);
        }

        /// <summary>
        /// Mostra a barra de progresso do editor.
        /// </summary>
        /// <param name="step">Descrição da etapa.</param>
        /// <param name="t">Progresso (0 a 1).</param>
        private static void Progress(string step, float t)
        {
            EditorUtility.DisplayProgressBar("Treasure Hunter — gerando o Nível 1", step, t);
        }

        // ================================================================== Blocos e salas

        /// <summary>
        /// Define o objeto pai dos próximos blocos/prefabs criados.
        /// </summary>
        /// <param name="name">Nome do grupo.</param>
        /// <param name="parent">Pai do grupo (nulo = raiz da cena).</param>
        /// <returns>O Transform do grupo.</returns>
        private static Transform Group(string name, Transform parent = null)
        {
            GameObject g = new GameObject(name);
            if (parent != null) g.transform.SetParent(parent, false);
            currentParent = g.transform;
            return g.transform;
        }

        /// <summary>
        /// Cria um bloco de cenário (caixa com malha voxel e colisor). Malhas de mesmo tamanho/material são reutilizadas.
        /// </summary>
        /// <param name="name">Nome do objeto.</param>
        /// <param name="min">Canto mínimo (x, y, z).</param>
        /// <param name="max">Canto máximo (x, y, z).</param>
        /// <param name="mat">Material.</param>
        /// <param name="collider">true para adicionar BoxCollider.</param>
        /// <returns>O bloco criado.</returns>
        private static GameObject Block(string name, Vector3 min, Vector3 max, Material mat, bool collider = true)
        {
            Vector3 size = max - min;
            Vector3 center = (min + max) * 0.5f;
            string key = mat.name + "_" + F(size.x) + "x" + F(size.y) + "x" + F(size.z);
            if (!blockMeshes.TryGetValue(key, out Mesh mesh))
            {
                mesh = new VoxelModel().Add(Vector3.zero, size, mat).Build(key, out _);
                BuilderCore.SaveAsset(mesh, BuilderCore.MeshesPath + "/Level/" + key + ".asset");
                blockMeshes[key] = mesh;
            }
            GameObject go = new GameObject(name + "_" + (++blockCounter));
            go.transform.SetParent(currentParent, false);
            go.transform.position = center;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider)
            {
                BoxCollider bc = go.AddComponent<BoxCollider>();
                bc.size = size;
                go.tag = GameTags.Ground;
            }
            go.isStatic = true;
            return go;
        }

        /// <summary>
        /// Formata um número com duas casas e ponto decimal (nomes de arquivos independentes do idioma do sistema).
        /// </summary>
        /// <param name="v">Valor.</param>
        /// <returns>Texto formatado.</returns>
        private static string F(float v)
        {
            return v.ToString("F2", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Constrói uma sala retangular: piso, teto (opcional) e quatro paredes com aberturas.
        /// O interior da sala é o retângulo [xMin,xMax]×[zMin,zMax]; paredes têm 1 m de espessura do lado de fora.
        /// </summary>
        /// <param name="name">Nome do grupo da sala.</param>
        /// <param name="xMin">Limite interno oeste.</param>
        /// <param name="xMax">Limite interno leste.</param>
        /// <param name="zMin">Limite interno sul.</param>
        /// <param name="zMax">Limite interno norte.</param>
        /// <param name="y0">Altura do piso.</param>
        /// <param name="h">Altura interna.</param>
        /// <param name="wall">Material das paredes.</param>
        /// <param name="floor">Material do piso (nulo = sem piso).</param>
        /// <param name="ceiling">Material do teto (nulo = sem teto).</param>
        /// <param name="skip">Lados sem parede (corredores abertos).</param>
        /// <param name="openings">Aberturas nas paredes.</param>
        /// <returns>O Transform do grupo da sala.</returns>
        private static Transform Room(string name, float xMin, float xMax, float zMin, float zMax, float y0, float h,
            Material wall, Material floor, Material ceiling, Side[] skip, params Opening[] openings)
        {
            Transform previous = currentParent;
            Transform room = Group(name, previous);
            HashSet<Side> skipped = new HashSet<Side>(skip ?? new Side[0]);

            // Piso e teto se estendem por baixo/por cima das paredes existentes.
            float fx0 = xMin - (skipped.Contains(Side.West) ? 0 : 1), fx1 = xMax + (skipped.Contains(Side.East) ? 0 : 1);
            float fz0 = zMin - (skipped.Contains(Side.South) ? 0 : 1), fz1 = zMax + (skipped.Contains(Side.North) ? 0 : 1);
            if (floor != null) Block("Piso", new Vector3(fx0, y0 - 1f, fz0), new Vector3(fx1, y0, fz1), floor);
            if (ceiling != null) Block("Teto", new Vector3(fx0, y0 + h, fz0), new Vector3(fx1, y0 + h + 1f, fz1), ceiling);

            foreach (Side side in new[] { Side.South, Side.North, Side.West, Side.East })
            {
                if (skipped.Contains(side)) continue;
                List<Opening> ops = new List<Opening>();
                foreach (Opening o in openings) if (o.side == side) ops.Add(o);
                ops.Sort((a, b) => a.center.CompareTo(b.center));

                bool alongX = side == Side.South || side == Side.North;
                // Paredes Sul/Norte cobrem os cantos, exceto do lado que está aberto (evita sobreposição com a sala vizinha).
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
                    if (s > cursor + 0.01f) WallPiece(alongX, cursor, s, t0, t1, y0, y0 + h, wall);
                    if (o.bottom + o.height < h) WallPiece(alongX, s, e, t0, t1, y0 + o.bottom + o.height, y0 + h, wall);
                    if (o.bottom > 0f) WallPiece(alongX, s, e, t0, t1, y0, y0 + o.bottom, wall);
                    cursor = e;
                }
                if (a1 > cursor + 0.01f) WallPiece(alongX, cursor, a1, t0, t1, y0, y0 + h, wall);
            }

            currentParent = previous;
            return room;
        }

        /// <summary>
        /// Cria um pedaço de parede.
        /// </summary>
        /// <param name="alongX">true se a parede corre ao longo do eixo X.</param>
        /// <param name="a">Início ao longo da parede.</param>
        /// <param name="b">Fim ao longo da parede.</param>
        /// <param name="t0">Início da espessura.</param>
        /// <param name="t1">Fim da espessura.</param>
        /// <param name="yb">Base.</param>
        /// <param name="yt">Topo.</param>
        /// <param name="mat">Material.</param>
        private static void WallPiece(bool alongX, float a, float b, float t0, float t1, float yb, float yt, Material mat)
        {
            if (yt - yb < 0.01f || b - a < 0.01f) return;
            if (alongX) Block("Parede", new Vector3(a, yb, t0), new Vector3(b, yt, t1), mat);
            else Block("Parede", new Vector3(t0, yb, a), new Vector3(t1, yt, b), mat);
        }

        /// <summary>
        /// Instancia um prefab gerado na cena.
        /// </summary>
        /// <param name="id">Identificador do prefab (props, partículas ou personagem).</param>
        /// <param name="pos">Posição no mundo.</param>
        /// <param name="yaw">Rotação em Y (graus).</param>
        /// <param name="scale">Escala uniforme.</param>
        /// <returns>A instância.</returns>
        private static GameObject Place(string id, Vector3 pos, float yaw = 0f, float scale = 1f)
        {
            GameObject prefab;
            if (!PropFactory.Prefabs.TryGetValue(id, out prefab) && !ParticleFactory.Prefabs.TryGetValue(id, out prefab))
            {
                Debug.LogError("Prefab não encontrado: " + id);
                return new GameObject("FALTANDO_" + id);
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(currentParent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (!Mathf.Approximately(scale, 1f)) go.transform.localScale = Vector3.one * scale;
            return go;
        }

        /// <summary>
        /// Cria um objeto vazio com um BoxCollider de gatilho.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="center">Centro.</param>
        /// <param name="size">Tamanho.</param>
        /// <returns>O objeto criado.</returns>
        private static GameObject TriggerBox(string name, Vector3 center, Vector3 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(currentParent, false);
            go.transform.position = center;
            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = size;
            return go;
        }

        /// <summary>
        /// Cria um marcador (ponto de renascimento, ponto de queda, ponto de tesouro).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="pos">Posição.</param>
        /// <param name="yaw">Rotação em Y.</param>
        /// <returns>O Transform do marcador.</returns>
        private static Transform Marker(string name, Vector3 pos, float yaw = 0f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(currentParent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return go.transform;
        }

        /// <summary>
        /// Cria uma parede invisível (limite do mapa).
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
        /// Coloca um gerador de coletáveis com um vetor de pontos.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="count">Quantos itens criar.</param>
        /// <param name="points">Posições possíveis.</param>
        private static void Treasures(string name, int count, params Vector3[] points)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(currentParent, false);
            CollectibleSpawner sp = go.AddComponent<CollectibleSpawner>();
            sp.count = count;
            sp.prefabs = new[] { PropFactory.Prefabs["Moeda_de_Ouro"], PropFactory.Prefabs["Moeda_de_Ouro"], PropFactory.Prefabs["Joia_Vermelha"], PropFactory.Prefabs["Joia_Azul"], PropFactory.Prefabs["Joia_Verde"] };
            Transform[] pts = new Transform[points.Length];
            Transform prev = currentParent;
            currentParent = go.transform;
            for (int i = 0; i < points.Length; i++) pts[i] = Marker("Ponto_" + (i + 1), points[i]);
            currentParent = prev;
            sp.points = pts;
        }

        // ================================================================== Menu principal

        /// <summary>
        /// Constrói a cena do menu principal: diorama do pedestal com o ídolo, câmera girando, música e botões.
        /// </summary>
        private static void BuildMainMenuScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.1f, 0.09f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.05f, 0.04f, 0.04f);
            RenderSettings.fogDensity = 0.05f;
            RenderSettings.skybox = null;

            Group("Diorama");
            Block("Piso", new Vector3(-10, -1, -10), new Vector3(10, 0, 10), Palette.StoneFloor);
            Block("Fundo", new Vector3(-10, 0, 6), new Vector3(10, 8, 7), Palette.TempleBrick);
            Place("Pedestal", Vector3.zero, 0f);
            GameObject idol = Place("Idolo_de_Anang", new Vector3(0, 1.1f, 0), 180f);
            Spinner sp = idol.AddComponent<Spinner>();
            sp.degreesPerSecond = new Vector3(0, 30, 0);
            sp.bobAmplitude = 0.05f;
            Place("FX_Glint", new Vector3(0, 1.5f, 0));
            Place("Braseiro_Aceso", new Vector3(-2.6f, 0, 1.2f));
            Place("Braseiro_Aceso", new Vector3(2.6f, 0, 1.2f));
            Place("Pilar_Templo", new Vector3(-4.5f, 0, 4f));
            Place("Pilar_Quebrado", new Vector3(4.5f, 0, 4f), 40f);
            Place("Estatua_Guardiao", new Vector3(-6.5f, 0, 2f), 120f);
            Place("Totem", new Vector3(6.5f, 0, 2.5f), -120f);
            Place("Pilha_de_Ouro", new Vector3(1.2f, 0, -0.6f), 30f);
            Place("Bau_de_Tesouro", new Vector3(-1.4f, 0, 1.4f), 15f);
            Place("Vaso_Grande", new Vector3(3.6f, 0, -1.5f));
            Place("Cranio", new Vector3(-2f, 0, -1.8f), 200f);
            Place("Cortina_de_Trepadeiras", new Vector3(-2.5f, 3.5f, 5.9f), 0f);
            Place("Raizes_Penduradas", new Vector3(2.5f, 7f, 4f));
            Place("Relevo_na_Parede", new Vector3(0, 3.5f, 5.9f), 0f);
            Place("Tocha_de_Parede", new Vector3(-6f, 2.4f, 5.95f), 0f);
            Place("Tocha_de_Parede", new Vector3(6f, 2.4f, 5.95f), 0f);

            // Câmera presa a um pivô que gira lentamente (rotação contínua).
            GameObject pivot = new GameObject("PivoDaCamera");
            Spinner ps = pivot.AddComponent<Spinner>();
            ps.degreesPerSecond = new Vector3(0, 5f, 0);
            ps.bobAmplitude = 0f;
            ps.unscaledTime = true;
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(pivot.transform, false);
            camGo.transform.localPosition = new Vector3(2.5f, 2.2f, -6.5f);
            camGo.transform.LookAt(new Vector3(0, 1.3f, 0));
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.025f, 0.02f);
            cam.fieldOfView = 50f;
            camGo.AddComponent<AudioListener>();

            GameObject managers = new GameObject("Menu");
            AudioSource music = managers.AddComponent<AudioSource>();
            music.clip = BuilderCore.Clip("music_level1");
            music.loop = true; music.playOnAwake = true; music.volume = 0.4f;
            AudioSource click = managers.AddComponent<AudioSource>();
            click.clip = BuilderCore.Clip("ui_click");
            click.playOnAwake = false;
            MainMenuController menu = managers.AddComponent<MainMenuController>();
            menu.levelScene = "Level1";
            menu.clickSource = click;
            UIFactory.BuildMainMenu(menu);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), MainMenuScenePath);
        }
    }
}
