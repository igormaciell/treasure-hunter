using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Utilitários do gerador do Nível 2: pastas próprias (Generated_Nivel2, separada do Nível 1),
    /// Tags e Layers do combate, texturas em pixel art salvas como PNG (com mapas normais),
    /// materiais do URP, criação e registro de prefabs e acesso aos prefabs do Nível 1 que são reaproveitados.
    /// Nada aqui escreve nas pastas do Nível 1.
    /// </summary>
    public static class L2Core
    {
        /// <summary>Pasta gerada do Nível 2 (padrão).</summary>
        public const string Level2Root = BuilderCore.Root + "/Generated_Nivel2";
        /// <summary>Pasta dos sons do Nível 2 (padrão).</summary>
        public const string Level2Audio = BuilderCore.AudioPath + "/Nivel2";

        /// <summary>
        /// Pasta onde as funções desta classe gravam os assets. É a do Nível 2, a menos que outro gerador
        /// (ex.: Nível 3) chame <see cref="UseFolders"/> para reaproveitar as mesmas ferramentas na sua própria pasta.
        /// </summary>
        public static string Root { get; private set; } = Level2Root;
        public static string TexturesPath => Root + "/Textures";
        public static string MaterialsPath => Root + "/Materials";
        public static string MeshesPath => Root + "/Meshes";
        public static string PrefabsPath => Root + "/Prefabs";
        public static string AnimationsPath => Root + "/Animations";
        public static string LightingPath => Root + "/Lighting";
        /// <summary>Pasta dos sons procurados primeiro por <see cref="Clip"/>.</summary>
        public static string AudioPath { get; private set; } = Level2Audio;

        /// <summary>
        /// Troca as pastas de trabalho (gerada e de sons). Sem argumentos, volta às do Nível 2.
        /// </summary>
        /// <param name="root">Pasta gerada (ex.: Assets/TreasureHunter/Generated_Nivel3).</param>
        /// <param name="audio">Pasta dos sons do nível.</param>
        public static void UseFolders(string root = Level2Root, string audio = Level2Audio)
        {
            Root = root;
            AudioPath = audio;
        }
        public const string Level2ScenePath = BuilderCore.ScenesPath + "/Level2.unity";
        public const string Level3ScenePath = BuilderCore.ScenesPath + "/Level3.unity";

        /// <summary>Prefabs do Nível 2 criados nesta geração, por identificador.</summary>
        public static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();

        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Color[]> pixels = new Dictionary<string, Color[]>();

        /// <summary>Gerador aleatório determinístico (o cenário sai igual a cada geração).</summary>
        public static System.Random Rng = new System.Random(2027);

        /// <summary>
        /// Sorteia um número real num intervalo.
        /// </summary>
        /// <param name="min">Mínimo.</param>
        /// <param name="max">Máximo.</param>
        /// <returns>Número sorteado.</returns>
        public static float Range(float min, float max)
        {
            return min + (float)Rng.NextDouble() * (max - min);
        }

        /// <summary>Atalho para Vector3.</summary>
        public static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        // ================================================================== Pastas, Tags e Layers

        /// <summary>
        /// Apaga a pasta gerada do Nível 2 e recria a estrutura vazia.
        /// </summary>
        public static void PrepareFolders()
        {
            Prefabs.Clear();
            textures.Clear();
            materials.Clear();
            pixels.Clear();
            Rng = new System.Random(2027);
            if (AssetDatabase.IsValidFolder(Root)) AssetDatabase.DeleteAsset(Root);
            BuilderCore.EnsureFolder(TexturesPath);
            BuilderCore.EnsureFolder(MaterialsPath);
            BuilderCore.EnsureFolder(MeshesPath + "/Level");
            BuilderCore.EnsureFolder(PrefabsPath);
            BuilderCore.EnsureFolder(AnimationsPath);
            BuilderCore.EnsureFolder(LightingPath);
            BuilderCore.EnsureFolder(BuilderCore.ScenesPath);
            BuilderCore.EnsureFolder(BuilderCore.DocsPath);
        }

        /// <summary>
        /// Registra as Tags (Nível 1 + combate) e as Layers do combate (Enemy, Projectile, Interactable).
        /// </summary>
        public static void EnsureTagsAndLayers()
        {
            EnsureTagsAndLayers(CombatTags.All, new[] { CombatLayers.EnemyLayerName, CombatLayers.ProjectileLayerName, CombatLayers.InteractableLayerName });
        }

        /// <summary>
        /// Registra Tags e Layers no TagManager (as do Nível 1 sempre; as informadas, se ainda não existirem).
        /// </summary>
        /// <param name="extraTags">Tags a garantir.</param>
        /// <param name="extraLayers">Layers a garantir (nos primeiros espaços livres a partir do 8).</param>
        public static void EnsureTagsAndLayers(IEnumerable<string> extraTags, IEnumerable<string> extraLayers)
        {
            BuilderCore.EnsureTags();
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            SerializedObject tagManager = new SerializedObject(assets[0]);

            SerializedProperty tags = tagManager.FindProperty("tags");
            foreach (string tag in extraTags)
            {
                bool exists = false;
                for (int i = 0; i < tags.arraySize; i++)
                    if (tags.GetArrayElementAtIndex(i).stringValue == tag) { exists = true; break; }
                if (exists) continue;
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            }

            SerializedProperty layers = tagManager.FindProperty("layers");
            foreach (string layer in extraLayers)
            {
                bool exists = false;
                for (int i = 0; i < layers.arraySize; i++)
                    if (layers.GetArrayElementAtIndex(i).stringValue == layer) { exists = true; break; }
                if (exists) continue;
                // Camadas de usuário começam no índice 8.
                bool placed = false;
                for (int i = 8; i < layers.arraySize; i++)
                {
                    SerializedProperty sp = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(sp.stringValue)) { sp.stringValue = layer; placed = true; break; }
                }
                if (!placed)
                    Debug.LogWarning("[Treasure Hunter] Não há Layer livre para \"" + layer + "\". Liberem uma em Project Settings > Tags and Layers e gerem de novo.");
            }
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            CombatLayers.ResetCache();
        }

        /// <summary>
        /// Índice de uma camada pelo nome (0 = Default se não existir).
        /// </summary>
        /// <param name="name">Nome da camada.</param>
        /// <returns>Índice.</returns>
        public static int Layer(string name)
        {
            int l = LayerMask.NameToLayer(name);
            return l >= 0 ? l : 0;
        }

        /// <summary>
        /// Coloca um objeto e todos os filhos numa camada.
        /// </summary>
        /// <param name="go">Objeto.</param>
        /// <param name="layer">Camada.</param>
        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        // ================================================================== Áudio

        /// <summary>
        /// Carrega um som do Nível 2 (pasta Audio/Nivel2) ou, se não existir, do Nível 1.
        /// </summary>
        /// <param name="name">Nome do arquivo .wav.</param>
        /// <returns>O clipe ou nulo.</returns>
        public static AudioClip Clip(string name)
        {
            AudioClip c = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + "/" + name + ".wav");
            return c != null ? c : BuilderCore.Clip(name);
        }

        /// <summary>
        /// Vetor de sons (nomes ignorados se o arquivo não existir).
        /// </summary>
        /// <param name="names">Nomes dos arquivos.</param>
        /// <returns>Vetor de clipes.</returns>
        public static AudioClip[] Clips(params string[] names)
        {
            List<AudioClip> list = new List<AudioClip>();
            foreach (string n in names)
            {
                AudioClip c = Clip(n);
                if (c != null) list.Add(c);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Configura a importação dos sons do Nível 2 (ambiente e música em streaming).
        /// </summary>
        public static void ConfigureAudioImport()
        {
            if (!AssetDatabase.IsValidFolder(AudioPath)) return;
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioPath }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;
                bool longClip = path.Contains("ambience") || path.Contains("music");
                AudioImporterSampleSettings s = importer.defaultSampleSettings;
                s.loadType = longClip ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.7f;
                importer.defaultSampleSettings = s;
                importer.SaveAndReimport();
            }
        }

        // ================================================================== Prefabs do Nível 1

        /// <summary>
        /// Carrega um prefab gerado pelo Nível 1 (reaproveitado no Nível 2).
        /// </summary>
        /// <param name="id">Nome do prefab.</param>
        /// <returns>O prefab ou nulo.</returns>
        public static GameObject L1(string id)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(BuilderCore.PrefabsPath + "/" + id + ".prefab");
        }

        /// <summary>
        /// Carrega um prefab gerado pelo Nível 2 (reaproveitado nos níveis seguintes: John armado, bala, pedras...).
        /// </summary>
        /// <param name="id">Nome do prefab.</param>
        /// <returns>O prefab ou nulo.</returns>
        public static GameObject L2Prefab(string id)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(Level2Root + "/Prefabs/" + id + ".prefab");
        }

        /// <summary>
        /// Busca um prefab pelo identificador: primeiro os do Nível 2, depois os do Nível 1.
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <returns>O prefab ou nulo.</returns>
        public static GameObject Get(string id)
        {
            if (Prefabs.TryGetValue(id, out GameObject p) && p != null) return p;
            return L1(id);
        }

        // ================================================================== Texturas

        /// <summary>
        /// Cria uma textura em pixel art (filtro Point), salva como PNG e importa.
        /// </summary>
        /// <param name="name">Nome do arquivo.</param>
        /// <param name="size">Lado em pixels.</param>
        /// <param name="pixel">Função (x, y, aleatório) → cor (y = 0 é a linha de baixo).</param>
        /// <param name="clamp">true para não repetir (ícones, símbolos).</param>
        /// <returns>A textura importada.</returns>
        public static Texture2D Tex(string name, int size, Func<int, int, System.Random, Color> pixel, bool clamp = false)
        {
            if (textures.TryGetValue(name, out Texture2D cached)) return cached;
            System.Random rnd = new System.Random(StableHash(name));
            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    colors[y * size + x] = pixel(x, y, rnd);
            Texture2D t = SavePng(name, colors, size, size, false, clamp, FilterMode.Point);
            textures[name] = t;
            pixels[name] = colors;
            return t;
        }

        /// <summary>
        /// Gera o mapa normal de uma textura a partir do brilho de cada pixel (claro = alto).
        /// </summary>
        /// <param name="name">Nome do arquivo do mapa normal.</param>
        /// <param name="source">Textura difusa (gerada por Tex).</param>
        /// <param name="strength">Intensidade do relevo.</param>
        /// <returns>O mapa normal importado.</returns>
        public static Texture2D NormalMap(string name, Texture2D source, float strength = 3f)
        {
            if (textures.TryGetValue(name, out Texture2D cached)) return cached;
            if (source == null || !pixels.TryGetValue(source.name, out Color[] c)) return null;
            int w = Mathf.RoundToInt(Mathf.Sqrt(c.Length)), h = w;

            float H(int x, int y)
            {
                Color p = c[((y + h) % h) * w + ((x + w) % w)];
                return p.r * 0.3f + p.g * 0.59f + p.b * 0.11f;
            }
            Color[] n = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (H(x + 1, y) - H(x - 1, y)) * strength;
                    float dy = (H(x, y + 1) - H(x, y - 1)) * strength;
                    Vector3 v = new Vector3(-dx, -dy, 1f).normalized;
                    n[y * w + x] = new Color(v.x * 0.5f + 0.5f, v.y * 0.5f + 0.5f, v.z * 0.5f + 0.5f, 1f);
                }
            }
            Texture2D t = SavePng(name, n, w, h, true, false, FilterMode.Point);
            textures[name] = t;
            return t;
        }

        /// <summary>
        /// Textura circular suave das partículas do Nível 2.
        /// </summary>
        /// <returns>A textura.</returns>
        public static Texture2D SoftCircle()
        {
            if (textures.TryGetValue("tex2_particula", out Texture2D cached)) return cached;
            const int size = 32;
            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    colors[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
            }
            Texture2D t = SavePng("tex2_particula", colors, size, size, false, true, FilterMode.Bilinear, true);
            textures["tex2_particula"] = t;
            return t;
        }

        /// <summary>
        /// Salva cores como PNG dentro do projeto e configura a importação.
        /// </summary>
        /// <param name="name">Nome do arquivo (sem extensão).</param>
        /// <param name="colors">Cores (linha 0 = baixo).</param>
        /// <param name="w">Largura.</param>
        /// <param name="h">Altura.</param>
        /// <param name="normalMap">true para importar como Normal Map.</param>
        /// <param name="clamp">true para não repetir.</param>
        /// <param name="filter">Filtro.</param>
        /// <param name="alpha">true para transparência.</param>
        /// <returns>A textura importada.</returns>
        public static Texture2D SavePng(string name, Color[] colors, int w, int h, bool normalMap, bool clamp, FilterMode filter, bool alpha = false)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(colors);
            tex.Apply();
            string path = TexturesPath + "/" + name + ".png";
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.filterMode = filter;
            ti.wrapMode = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            ti.alphaIsTransparency = alpha;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// Cria um sprite em pixel art a partir de um desenho em texto (ícones do HUD).
        /// </summary>
        /// <param name="name">Nome do asset.</param>
        /// <param name="rows">Linhas do desenho (de cima para baixo).</param>
        /// <param name="palette">Caractere → cor.</param>
        /// <returns>O sprite.</returns>
        public static Sprite PixelSprite(string name, string[] rows, Dictionary<char, Color> palette)
        {
            int h = rows.Length, w = rows[0].Length;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, palette.TryGetValue(rows[h - 1 - y][x], out Color col) ? col : Color.clear);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            string path = TexturesPath + "/" + name + ".asset";
            BuilderCore.SaveAsset(tex, path);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
            sprite.name = name + "_sprite";
            AssetDatabase.AddObjectToAsset(sprite, path);
            return sprite;
        }

        /// <summary>
        /// Hash estável de um texto (o GetHashCode do .NET muda entre execuções).
        /// </summary>
        /// <param name="s">Texto.</param>
        /// <returns>Número inteiro.</returns>
        public static int StableHash(string s)
        {
            unchecked
            {
                int h = 23;
                foreach (char ch in s) h = h * 31 + ch;
                return h;
            }
        }

        /// <summary>
        /// Varia levemente o brilho de uma cor.
        /// </summary>
        /// <param name="c">Cor.</param>
        /// <param name="r">Aleatório.</param>
        /// <param name="amount">Intensidade.</param>
        /// <returns>Cor variada.</returns>
        public static Color Jitter(Color c, System.Random r, float amount)
        {
            float k = 1f + ((float)r.NextDouble() * 2f - 1f) * amount;
            return new Color(c.r * k, c.g * k, c.b * k, c.a);
        }

        // ================================================================== Materiais

        /// <summary>
        /// Shader Lit do URP (ou Standard, fora do URP).
        /// </summary>
        /// <returns>O shader.</returns>
        private static Shader LitShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        }

        /// <summary>
        /// Cria e salva um material opaco do URP, com mapa normal opcional.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="tex">Textura base (nula = branca).</param>
        /// <param name="color">Cor.</param>
        /// <param name="smoothness">Suavidade.</param>
        /// <param name="metallic">Metalicidade.</param>
        /// <param name="normal">Mapa normal (opcional).</param>
        /// <param name="normalScale">Intensidade do mapa normal.</param>
        /// <returns>O material.</returns>
        public static Material Lit(string name, Texture2D tex, Color color, float smoothness = 0.08f, float metallic = 0f, Texture2D normal = null, float normalScale = 1f)
        {
            if (materials.TryGetValue(name, out Material cached)) return cached;
            Material m = new Material(LitShader()) { name = name };
            if (tex != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (normal != null && m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", normal);
                if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", normalScale);
                m.EnableKeyword("_NORMALMAP");
            }
            m.enableInstancing = true;
            BuilderCore.SaveAsset(m, MaterialsPath + "/" + name + ".mat");
            materials[name] = m;
            return m;
        }

        /// <summary>
        /// Material que brilha (emissão): chamas, olhos dos esqueletos, fungos, runas.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="tex">Textura (opcional).</param>
        /// <param name="color">Cor base.</param>
        /// <param name="emission">Cor e intensidade da emissão.</param>
        /// <param name="emissionMap">Máscara de emissão (opcional).</param>
        /// <returns>O material.</returns>
        public static Material Emissive(string name, Texture2D tex, Color color, Color emission, Texture2D emissionMap = null)
        {
            if (materials.TryGetValue(name, out Material cached)) return cached;
            Material m = Lit(name, tex, color);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
            if (emissionMap != null) m.SetTexture("_EmissionMap", emissionMap);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// Material transparente simples (teias): superfície transparente do URP Lit.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="color">Cor com transparência.</param>
        /// <returns>O material.</returns>
        public static Material Transparent(string name, Color color)
        {
            if (materials.TryGetValue(name, out Material cached)) return cached;
            Material m = Lit(name, null, color, 0.3f);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// Material de partículas do URP (mistura alfa ou aditiva).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="additive">true para brilho aditivo.</param>
        /// <returns>O material.</returns>
        public static Material Particle(string name, bool additive)
        {
            if (materials.TryGetValue(name, out Material cached)) return cached;
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            Material m = new Material(shader) { name = name };
            Texture2D tex = SoftCircle();
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            BuilderCore.SaveAsset(m, MaterialsPath + "/" + name + ".mat");
            materials[name] = m;
            return m;
        }

        // ================================================================== Prefabs

        /// <summary>
        /// Começa um prefab: objeto raiz com a malha voxel principal (se houver).
        /// </summary>
        /// <param name="id">Nome do prefab.</param>
        /// <param name="model">Modelo voxel (pode ser nulo).</param>
        /// <returns>O objeto raiz.</returns>
        public static GameObject Begin(string id, VoxelModel model)
        {
            GameObject go = new GameObject(id);
            if (model != null && !model.IsEmpty) model.ApplyTo(go, MeshesPath + "/" + id + ".asset");
            return go;
        }

        /// <summary>
        /// Cria um filho com malha própria (partes móveis ou separadas).
        /// </summary>
        /// <param name="parent">Pai.</param>
        /// <param name="name">Nome.</param>
        /// <param name="localPos">Posição local.</param>
        /// <param name="model">Modelo voxel (pode ser nulo).</param>
        /// <returns>O filho.</returns>
        public static GameObject Child(GameObject parent, string name, Vector3 localPos, VoxelModel model)
        {
            GameObject c = new GameObject(name);
            c.transform.SetParent(parent.transform, false);
            c.transform.localPosition = localPos;
            if (model != null && !model.IsEmpty) model.ApplyTo(c, MeshesPath + "/" + parent.name + "_" + name + ".asset");
            return c;
        }

        /// <summary>
        /// Adiciona um BoxCollider.
        /// </summary>
        /// <param name="go">Objeto.</param>
        /// <param name="center">Centro.</param>
        /// <param name="size">Tamanho.</param>
        /// <param name="trigger">true para gatilho.</param>
        /// <returns>O colisor.</returns>
        public static BoxCollider Box(GameObject go, Vector3 center, Vector3 size, bool trigger = false)
        {
            BoxCollider c = go.AddComponent<BoxCollider>();
            c.center = center;
            c.size = size;
            c.isTrigger = trigger;
            return c;
        }

        /// <summary>
        /// Adiciona um BoxCollider ajustado aos limites do modelo.
        /// </summary>
        /// <param name="go">Objeto.</param>
        /// <param name="model">Modelo.</param>
        /// <param name="trigger">true para gatilho.</param>
        /// <returns>O colisor.</returns>
        public static BoxCollider Fit(GameObject go, VoxelModel model, bool trigger = false)
        {
            Bounds b = model.GetBounds();
            return Box(go, b.center, b.size, trigger);
        }

        /// <summary>
        /// Adiciona um SphereCollider.
        /// </summary>
        /// <param name="go">Objeto.</param>
        /// <param name="center">Centro.</param>
        /// <param name="radius">Raio.</param>
        /// <param name="trigger">true para gatilho.</param>
        /// <returns>O colisor.</returns>
        public static SphereCollider Sphere(GameObject go, Vector3 center, float radius, bool trigger = false)
        {
            SphereCollider c = go.AddComponent<SphereCollider>();
            c.center = center;
            c.radius = radius;
            c.isTrigger = trigger;
            return c;
        }

        /// <summary>
        /// Adiciona uma luz pontual (ou de outro tipo) como filha, com o modo de iluminação desejado.
        /// </summary>
        /// <param name="parent">Pai.</param>
        /// <param name="name">Nome.</param>
        /// <param name="pos">Posição local.</param>
        /// <param name="color">Cor.</param>
        /// <param name="range">Alcance.</param>
        /// <param name="intensity">Intensidade.</param>
        /// <param name="flicker">true para tremeluzir como fogo.</param>
        /// <param name="mode">Realtime, Mixed ou Baked.</param>
        /// <returns>A luz.</returns>
        public static Light PointLight(GameObject parent, string name, Vector3 pos, Color color, float range, float intensity,
                                       bool flicker = false, LightmapBakeType mode = LightmapBakeType.Realtime)
        {
            GameObject l = new GameObject(name);
            l.transform.SetParent(parent.transform, false);
            l.transform.localPosition = pos;
            Light light = l.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = mode;
            if (flicker) l.AddComponent<LightFlicker>();
            return light;
        }

        /// <summary>
        /// Coloca uma instância de um efeito (Nível 2 ou Nível 1) como filha.
        /// </summary>
        /// <param name="parent">Pai.</param>
        /// <param name="fx">Nome do prefab de partículas.</param>
        /// <param name="pos">Posição local.</param>
        /// <returns>O ParticleSystem (ou nulo).</returns>
        public static ParticleSystem AddFx(GameObject parent, string fx, Vector3 pos)
        {
            GameObject prefab = Get(fx);
            if (prefab == null) return null;
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.SetParent(parent.transform, false);
            inst.transform.localPosition = pos;
            return inst.GetComponent<ParticleSystem>();
        }

        /// <summary>
        /// Adiciona um obstáculo de navegação (caixa) que recorta o NavMesh (objetos que podem sumir ou se mover).
        /// </summary>
        /// <param name="go">Objeto.</param>
        /// <param name="center">Centro.</param>
        /// <param name="size">Tamanho.</param>
        /// <returns>O obstáculo.</returns>
        public static NavMeshObstacle Obstacle(GameObject go, Vector3 center, Vector3 size)
        {
            NavMeshObstacle o = go.AddComponent<NavMeshObstacle>();
            o.shape = NavMeshObstacleShape.Box;
            o.center = center;
            o.size = size;
            o.carving = true;
            o.carveOnlyStationary = true;
            return o;
        }

        /// <summary>
        /// Finaliza o prefab: etiqueta de categoria, salva em Generated_Nivel2/Prefabs e registra.
        /// </summary>
        /// <param name="go">Objeto raiz.</param>
        /// <param name="cat">Categoria.</param>
        /// <param name="display">Nome legível.</param>
        /// <returns>O prefab salvo.</returns>
        public static GameObject Save(GameObject go, AssetCategoryType cat, string display)
        {
            Tag(go, cat, display);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabsPath + "/" + go.name + ".prefab");
            UnityEngine.Object.DestroyImmediate(go);
            Prefabs[prefab.name] = prefab;
            return prefab;
        }

        /// <summary>
        /// Coloca (ou atualiza) a etiqueta de categoria de um objeto.
        /// </summary>
        /// <param name="go">Objeto.</param>
        /// <param name="cat">Categoria.</param>
        /// <param name="display">Nome legível.</param>
        public static void Tag(GameObject go, AssetCategoryType cat, string display)
        {
            AssetCategory tag = go.GetComponent<AssetCategory>();
            if (tag == null) tag = go.AddComponent<AssetCategory>();
            tag.category = cat;
            tag.displayName = display;
        }

        /// <summary>
        /// Atalho: prefab estático com malha e colisor opcional.
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="model">Modelo.</param>
        /// <param name="cat">Categoria.</param>
        /// <param name="display">Nome legível.</param>
        /// <param name="collider">true para colisor ajustado.</param>
        /// <returns>O prefab.</returns>
        public static GameObject Simple(string id, VoxelModel model, AssetCategoryType cat, string display, bool collider)
        {
            GameObject go = Begin(id, model);
            if (collider) Fit(go, model);
            return Save(go, cat, display);
        }
    }
}
