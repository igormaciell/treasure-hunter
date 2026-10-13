using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Utilitários do gerador: caminhos, criação de pastas, registro de Tags,
    /// salvamento de assets e números aleatórios determinísticos (o nível sai igual a cada geração).
    /// </summary>
    public static class BuilderCore
    {
        public const string Root = "Assets/TreasureHunter";
        public const string Generated = Root + "/Generated";
        public const string TexturesPath = Generated + "/Textures";
        public const string MaterialsPath = Generated + "/Materials";
        public const string MeshesPath = Generated + "/Meshes";
        public const string PrefabsPath = Generated + "/Prefabs";
        public const string AnimationsPath = Generated + "/Animations";
        public const string ScenesPath = Root + "/Scenes";
        public const string AudioPath = Root + "/Audio";
        public const string DocsPath = Root + "/Docs";

        /// <summary>Gerador aleatório com semente fixa, para o cenário ser reproduzível.</summary>
        public static System.Random Rng = new System.Random(2026);

        /// <summary>
        /// Reinicia o gerador aleatório com a semente padrão.
        /// </summary>
        public static void ResetRandom()
        {
            Rng = new System.Random(2026);
        }

        /// <summary>
        /// Sorteia um número real num intervalo usando o gerador determinístico.
        /// </summary>
        /// <param name="min">Valor mínimo.</param>
        /// <param name="max">Valor máximo.</param>
        /// <returns>Número entre min e max.</returns>
        public static float Range(float min, float max)
        {
            return min + (float)Rng.NextDouble() * (max - min);
        }

        /// <summary>
        /// Cria (se necessário) uma pasta do projeto, inclusive as pastas pai.
        /// </summary>
        /// <param name="path">Caminho começando com "Assets/".</param>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>
        /// Apaga a pasta de assets gerados e recria a estrutura de pastas vazia.
        /// </summary>
        public static void PrepareFolders()
        {
            if (AssetDatabase.IsValidFolder(Generated)) AssetDatabase.DeleteAsset(Generated);
            EnsureFolder(TexturesPath);
            EnsureFolder(MaterialsPath);
            EnsureFolder(MeshesPath + "/Level");
            EnsureFolder(PrefabsPath);
            EnsureFolder(AnimationsPath);
            EnsureFolder(ScenesPath);
            EnsureFolder(DocsPath);
        }

        /// <summary>
        /// Salva um objeto como asset, substituindo um existente no mesmo caminho.
        /// </summary>
        /// <param name="obj">Objeto em memória (textura, material, malha, clipe...).</param>
        /// <param name="path">Caminho do asset.</param>
        public static void SaveAsset(UnityEngine.Object obj, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(obj, path);
        }

        /// <summary>
        /// Registra no TagManager as Tags personalizadas usadas pelos scripts.
        /// </summary>
        public static void EnsureTags()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty tags = tagManager.FindProperty("tags");

            foreach (string tag in GameTags.CustomTags)
            {
                bool exists = false;
                for (int i = 0; i < tags.arraySize; i++)
                {
                    if (tags.GetArrayElementAtIndex(i).stringValue == tag) { exists = true; break; }
                }
                if (exists) continue;
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            }
            tagManager.ApplyModifiedProperties();
        }

        /// <summary>
        /// Carrega um clipe de áudio da pasta Audio pelo nome do arquivo (sem extensão).
        /// </summary>
        /// <param name="name">Nome do arquivo .wav.</param>
        /// <returns>O clipe, ou nulo se não existir.</returns>
        public static AudioClip Clip(string name)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + "/" + name + ".wav");
        }

        /// <summary>
        /// Configura a importação dos áudios: ambientes e música em streaming, efeitos carregados na memória.
        /// </summary>
        public static void ConfigureAudioImport()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioPath });
            foreach (string guid in guids)
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
    }

    /// <summary>
    /// Cria as texturas em pixel art (16×16, filtro Point) que dão o visual voxel/retrô do GDD,
    /// os símbolos antigos dos enigmas e os ícones da interface.
    /// </summary>
    public static class TextureFactory
    {
        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        /// <summary>
        /// Limpa o cache entre gerações.
        /// </summary>
        public static void Clear()
        {
            cache.Clear();
        }

        /// <summary>
        /// Cria uma textura a partir de uma função que calcula a cor de cada pixel e a salva como asset.
        /// </summary>
        /// <param name="name">Nome do asset.</param>
        /// <param name="size">Largura/altura em pixels.</param>
        /// <param name="pixel">Função (x, y, rnd) → cor.</param>
        /// <param name="clamp">true para não repetir a textura (ícones).</param>
        /// <returns>A textura criada.</returns>
        public static Texture2D Make(string name, int size, Func<int, int, System.Random, Color> pixel, bool clamp = false)
        {
            if (cache.TryGetValue(name, out Texture2D cached)) return cached;
            System.Random rnd = new System.Random(name.GetHashCode());
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = name;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, pixel(x, y, rnd));
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            tex.Apply();
            BuilderCore.SaveAsset(tex, BuilderCore.TexturesPath + "/" + name + ".asset");
            cache[name] = tex;
            return tex;
        }

        /// <summary>
        /// Varia levemente o brilho de uma cor (ruído dos pixels).
        /// </summary>
        /// <param name="c">Cor base.</param>
        /// <param name="rnd">Gerador aleatório.</param>
        /// <param name="amount">Intensidade da variação.</param>
        /// <returns>Cor variada.</returns>
        public static Color Jitter(Color c, System.Random rnd, float amount)
        {
            float k = 1f + ((float)rnd.NextDouble() * 2f - 1f) * amount;
            return new Color(c.r * k, c.g * k, c.b * k, c.a);
        }

        /// <summary>
        /// Textura de blocos de pedra (tijolos), com argamassa escura e musgo opcional.
        /// </summary>
        /// <param name="name">Nome do asset.</param>
        /// <param name="baseColor">Cor da pedra.</param>
        /// <param name="mortar">Cor da argamassa.</param>
        /// <param name="moss">Probabilidade de pixels de musgo (0 a 1).</param>
        /// <returns>A textura.</returns>
        public static Texture2D Bricks(string name, Color baseColor, Color mortar, float moss)
        {
            Color mossColor = new Color(0.29f, 0.42f, 0.18f);
            return Make(name, 16, (x, y, r) =>
            {
                int row = y / 4;
                int offset = (row % 2) * 4;
                bool line = y % 4 == 0 || (x + offset) % 8 == 0;
                if (line) return Jitter(mortar, r, 0.1f);
                if (r.NextDouble() < moss * (1f - y / 16f)) return Jitter(mossColor, r, 0.15f);
                return Jitter(baseColor, r, 0.1f);
            });
        }

        /// <summary>
        /// Textura de ruído simples (terra, rocha, grama, folhas).
        /// </summary>
        /// <param name="name">Nome do asset.</param>
        /// <param name="a">Cor principal.</param>
        /// <param name="b">Cor secundária.</param>
        /// <param name="mix">Probabilidade da cor secundária.</param>
        /// <returns>A textura.</returns>
        public static Texture2D Noise(string name, Color a, Color b, float mix)
        {
            return Make(name, 16, (x, y, r) => Jitter(r.NextDouble() < mix ? b : a, r, 0.12f));
        }

        /// <summary>
        /// Textura de tábuas de madeira horizontais.
        /// </summary>
        /// <param name="name">Nome do asset.</param>
        /// <param name="wood">Cor da madeira.</param>
        /// <returns>A textura.</returns>
        public static Texture2D Planks(string name, Color wood)
        {
            return Make(name, 16, (x, y, r) =>
            {
                if (y % 4 == 0) return wood * 0.6f;
                if ((x + (y / 4) * 5) % 16 == 0) return wood * 0.7f;
                return Jitter(wood, r, 0.08f);
            });
        }

        /// <summary>
        /// Textura de casca de árvore (listras verticais).
        /// </summary>
        /// <param name="name">Nome do asset.</param>
        /// <param name="bark">Cor da casca.</param>
        /// <returns>A textura.</returns>
        public static Texture2D Bark(string name, Color bark)
        {
            return Make(name, 16, (x, y, r) => Jitter(x % 4 == 0 ? bark * 0.7f : bark, r, 0.1f));
        }

        /// <summary>
        /// Textura de piso de lajes 8×8.
        /// </summary>
        /// <param name="name">Nome do asset.</param>
        /// <param name="stone">Cor da laje.</param>
        /// <returns>A textura.</returns>
        public static Texture2D Slabs(string name, Color stone)
        {
            return Make(name, 16, (x, y, r) =>
            {
                if (x % 8 == 0 || y % 8 == 0) return stone * 0.65f;
                if (r.NextDouble() < 0.05) return stone * 0.8f;
                return Jitter(stone, r, 0.07f);
            });
        }

        /// <summary>
        /// Textura de ouro com pontos de brilho.
        /// </summary>
        /// <param name="name">Nome do asset.</param>
        /// <param name="color">Cor do metal.</param>
        /// <returns>A textura.</returns>
        public static Texture2D Shiny(string name, Color color)
        {
            return Make(name, 16, (x, y, r) =>
            {
                if ((x + y) % 7 == 0 && r.NextDouble() < 0.5) return Color.Lerp(color, Color.white, 0.6f);
                return Jitter(color, r, 0.1f);
            });
        }

        /// <summary>
        /// Textura branca 4×4 (para materiais de cor sólida).
        /// </summary>
        /// <returns>A textura.</returns>
        public static Texture2D White()
        {
            return Make("tex_white", 4, (x, y, r) => Color.white);
        }

        /// <summary>
        /// Textura circular suave usada pelas partículas (fumaça, fogo, poeira).
        /// </summary>
        /// <returns>A textura.</returns>
        public static Texture2D SoftCircle()
        {
            if (cache.TryGetValue("tex_particle", out Texture2D cached)) return cached;
            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "tex_particle";
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            BuilderCore.SaveAsset(tex, BuilderCore.TexturesPath + "/tex_particle.asset");
            cache["tex_particle"] = tex;
            return tex;
        }

        /// <summary>Nomes dos seis símbolos antigos usados nos enigmas.</summary>
        public static readonly string[] GlyphNames = { "Sol", "Lua", "Serpente", "Olho", "Pássaro", "Mão" };

        /// <summary>
        /// Diz se um pixel pertence ao desenho de um símbolo (pixel art procedural).
        /// </summary>
        /// <param name="glyph">Índice do símbolo.</param>
        /// <param name="x">Coluna (0 a 15).</param>
        /// <param name="y">Linha (0 a 15).</param>
        /// <returns>true se o pixel faz parte do símbolo.</returns>
        public static bool GlyphPixel(int glyph, int x, int y)
        {
            float cx = x - 7.5f, cy = y - 7.5f;
            float d = Mathf.Sqrt(cx * cx + cy * cy);
            switch (glyph)
            {
                case 0: // Sol: círculo com raios
                    if (d < 3.2f) return true;
                    if (d > 4.5f && d < 7f && (Mathf.Abs(cx) < 0.8f || Mathf.Abs(cy) < 0.8f || Mathf.Abs(Mathf.Abs(cx) - Mathf.Abs(cy)) < 0.8f)) return true;
                    return false;
                case 1: // Lua crescente
                    float d2 = Mathf.Sqrt((cx - 2.5f) * (cx - 2.5f) + (cy - 1f) * (cy - 1f));
                    return d < 6f && d2 > 4.6f;
                case 2: // Serpente: senoide grossa com cabeça
                    float sy = Mathf.Sin(x / 15f * Mathf.PI * 2f) * 3.5f + 7.5f;
                    if (x >= 1 && x <= 14 && Mathf.Abs(y - sy) < 1.3f) return true;
                    return x >= 12 && x <= 14 && Mathf.Abs(y - (sy + 1f)) < 2f;
                case 3: // Olho
                    float ey = Mathf.Abs(cy) / 4.5f + (cx * cx) / 49f;
                    if (Mathf.Abs(ey - 1f) < 0.18f) return true;
                    return d < 2f;
                case 4: // Pássaro (asas em V)
                    if (Mathf.Abs(cy - Mathf.Abs(cx) * 0.7f + 1.5f) < 1.1f && Mathf.Abs(cx) < 7f) return true;
                    return d < 1.6f && cy < 0.5f;
                default: // Mão: palma + dedos
                    if (y >= 2 && y <= 8 && x >= 4 && x <= 11) return true;
                    if (y > 8 && y <= 13 && x >= 4 && x <= 11 && (x - 4) % 2 == 0) return true;
                    return y >= 5 && y <= 9 && x >= 1 && x <= 3;
            }
        }

        /// <summary>
        /// Textura do símbolo entalhado na pedra (versão apagada).
        /// </summary>
        /// <param name="glyph">Índice do símbolo.</param>
        /// <returns>A textura.</returns>
        public static Texture2D GlyphStone(int glyph)
        {
            Color stone = new Color(0.55f, 0.52f, 0.46f);
            return Make("tex_glyph_" + glyph, 16, (x, y, r) =>
                GlyphPixel(glyph, x, y) ? stone * 0.45f : Jitter(stone, r, 0.08f), true);
        }

        /// <summary>
        /// Máscara de emissão do símbolo (branco no desenho, preto no resto).
        /// </summary>
        /// <param name="glyph">Índice do símbolo.</param>
        /// <returns>A textura.</returns>
        public static Texture2D GlyphMask(int glyph)
        {
            return Make("tex_glyphmask_" + glyph, 16, (x, y, r) => GlyphPixel(glyph, x, y) ? Color.white : Color.black, true);
        }

        /// <summary>
        /// Cria um sprite de interface em pixel art a partir de um "desenho" em texto.
        /// Cada caractere do desenho é um pixel; a paleta converte caractere em cor.
        /// </summary>
        /// <param name="name">Nome do asset.</param>
        /// <param name="rows">Linhas do desenho (de cima para baixo).</param>
        /// <param name="palette">Caractere → cor.</param>
        /// <returns>O sprite (salvo como sub-asset da textura).</returns>
        public static Sprite PixelSprite(string name, string[] rows, Dictionary<char, Color> palette)
        {
            int h = rows.Length, w = rows[0].Length;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = name;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    char c = rows[h - 1 - y][x];
                    tex.SetPixel(x, y, palette.TryGetValue(c, out Color col) ? col : Color.clear);
                }
            }
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            string path = BuilderCore.TexturesPath + "/" + name + ".asset";
            BuilderCore.SaveAsset(tex, path);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
            sprite.name = name + "_sprite";
            AssetDatabase.AddObjectToAsset(sprite, path);
            return sprite;
        }
    }

    /// <summary>
    /// Cria os materiais do URP (Lit para o cenário, Particles para os efeitos) a partir das texturas.
    /// Se o projeto não estiver em URP, usa os shaders padrão como alternativa.
    /// </summary>
    public static class MaterialFactory
    {
        private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        /// <summary>
        /// Limpa o cache entre gerações.
        /// </summary>
        public static void Clear()
        {
            cache.Clear();
        }

        /// <summary>
        /// Busca um material já criado pelo nome.
        /// </summary>
        /// <param name="name">Nome do material.</param>
        /// <returns>O material (ou nulo).</returns>
        public static Material Get(string name)
        {
            cache.TryGetValue(name, out Material m);
            return m;
        }

        /// <summary>
        /// Encontra o shader Lit do URP (ou o Standard, se o URP não estiver ativo).
        /// </summary>
        /// <returns>O shader encontrado.</returns>
        private static Shader LitShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        }

        /// <summary>
        /// Cria e salva um material opaco iluminado.
        /// </summary>
        /// <param name="name">Nome do material.</param>
        /// <param name="tex">Textura base (pode ser nula).</param>
        /// <param name="color">Cor multiplicada pela textura.</param>
        /// <param name="smoothness">Suavidade/brilho (0 a 1).</param>
        /// <param name="metallic">Metalicidade (0 a 1).</param>
        /// <returns>O material.</returns>
        public static Material Lit(string name, Texture2D tex, Color color, float smoothness = 0.08f, float metallic = 0f)
        {
            if (cache.TryGetValue(name, out Material cached)) return cached;
            Material m = new Material(LitShader());
            m.name = name;
            Texture2D t = tex != null ? tex : TextureFactory.White();
            SetTex(m, t);
            SetColor(m, color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            m.enableInstancing = true;
            BuilderCore.SaveAsset(m, BuilderCore.MaterialsPath + "/" + name + ".mat");
            cache[name] = m;
            return m;
        }

        /// <summary>
        /// Cria um material iluminado que também emite luz própria (brilho).
        /// </summary>
        /// <param name="name">Nome do material.</param>
        /// <param name="tex">Textura base.</param>
        /// <param name="color">Cor base.</param>
        /// <param name="emission">Cor/intensidade da emissão.</param>
        /// <param name="emissionMap">Máscara de emissão (nula = emite na superfície inteira).</param>
        /// <returns>O material.</returns>
        public static Material Emissive(string name, Texture2D tex, Color color, Color emission, Texture2D emissionMap = null)
        {
            if (cache.TryGetValue(name, out Material cached)) return cached;
            Material m = Lit(name, tex, color);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
            if (emissionMap != null) m.SetTexture("_EmissionMap", emissionMap);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// Cria um material transparente para partículas (mistura alfa ou aditiva).
        /// </summary>
        /// <param name="name">Nome do material.</param>
        /// <param name="additive">true para brilho aditivo (fogo, faíscas).</param>
        /// <returns>O material.</returns>
        public static Material Particle(string name, bool additive)
        {
            if (cache.TryGetValue(name, out Material cached)) return cached;
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            Material m = new Material(shader);
            m.name = name;
            Texture2D tex = TextureFactory.SoftCircle();
            SetTex(m, tex);
            SetColor(m, Color.white);

            // Configuração manual de transparência no URP (equivalente a escolher no Inspector).
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            BuilderCore.SaveAsset(m, BuilderCore.MaterialsPath + "/" + name + ".mat");
            cache[name] = m;
            return m;
        }

        /// <summary>
        /// Cria e salva o material do céu procedural da superfície.
        /// </summary>
        /// <returns>O material do skybox (ou nulo se o shader não existir).</returns>
        public static Material Skybox()
        {
            Shader s = Shader.Find("Skybox/Procedural");
            if (s == null) return null;
            Material m = new Material(s) { name = "mat_sky" };
            if (m.HasProperty("_SunSize")) m.SetFloat("_SunSize", 0.035f);
            if (m.HasProperty("_AtmosphereThickness")) m.SetFloat("_AtmosphereThickness", 0.9f);
            if (m.HasProperty("_SkyTint")) m.SetColor("_SkyTint", new Color(0.55f, 0.65f, 0.6f));
            if (m.HasProperty("_GroundColor")) m.SetColor("_GroundColor", new Color(0.25f, 0.3f, 0.2f));
            if (m.HasProperty("_Exposure")) m.SetFloat("_Exposure", 1.1f);
            BuilderCore.SaveAsset(m, BuilderCore.MaterialsPath + "/mat_sky.mat");
            return m;
        }

        /// <summary>
        /// Define a textura base nas propriedades do URP e do shader padrão.
        /// </summary>
        /// <param name="m">Material.</param>
        /// <param name="t">Textura.</param>
        private static void SetTex(Material m, Texture2D t)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t);
        }

        /// <summary>
        /// Define a cor base nas propriedades do URP e do shader padrão.
        /// </summary>
        /// <param name="m">Material.</param>
        /// <param name="c">Cor.</param>
        private static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }

    /// <summary>
    /// Modelo voxel: uma lista de caixas (cada uma com material e rotação opcional)
    /// que é convertida numa única malha com uma submalha por material.
    /// As coordenadas de textura usam a escala do mundo (1 repetição por metro), mantendo
    /// os pixels do mesmo tamanho em qualquer bloco — o visual "voxel retrô" do GDD.
    /// </summary>
    public class VoxelModel
    {
        private struct Box { public Vector3 center; public Vector3 size; public Material mat; public Quaternion rot; }
        private readonly List<Box> boxes = new List<Box>();

        /// <summary>
        /// Adiciona uma caixa ao modelo.
        /// </summary>
        /// <param name="center">Centro da caixa (espaço local do objeto).</param>
        /// <param name="size">Dimensões da caixa.</param>
        /// <param name="mat">Material da caixa.</param>
        /// <returns>O próprio modelo (para encadear chamadas).</returns>
        public VoxelModel Add(Vector3 center, Vector3 size, Material mat)
        {
            boxes.Add(new Box { center = center, size = size, mat = mat, rot = Quaternion.identity });
            return this;
        }

        /// <summary>
        /// Adiciona uma caixa girada ao modelo.
        /// </summary>
        /// <param name="center">Centro da caixa.</param>
        /// <param name="size">Dimensões.</param>
        /// <param name="mat">Material.</param>
        /// <param name="euler">Rotação em graus.</param>
        /// <returns>O próprio modelo.</returns>
        public VoxelModel Add(Vector3 center, Vector3 size, Material mat, Vector3 euler)
        {
            boxes.Add(new Box { center = center, size = size, mat = mat, rot = Quaternion.Euler(euler) });
            return this;
        }

        /// <summary>Indica se o modelo não possui caixas.</summary>
        public bool IsEmpty => boxes.Count == 0;

        /// <summary>
        /// Calcula os limites (bounds) do modelo, ignorando rotações.
        /// </summary>
        /// <returns>Os limites.</returns>
        public Bounds GetBounds()
        {
            Bounds b = new Bounds(boxes[0].center, boxes[0].size);
            foreach (Box box in boxes) b.Encapsulate(new Bounds(box.center, box.size));
            return b;
        }

        /// <summary>
        /// Gera a malha (uma submalha por material) e devolve os materiais na mesma ordem.
        /// </summary>
        /// <param name="name">Nome da malha.</param>
        /// <param name="materials">Saída: materiais de cada submalha.</param>
        /// <returns>A malha gerada (ainda não salva).</returns>
        public Mesh Build(string name, out Material[] materials)
        {
            List<Material> mats = new List<Material>();
            foreach (Box b in boxes) if (!mats.Contains(b.mat)) mats.Add(b.mat);

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<List<int>> tris = new List<List<int>>();
            foreach (Material m in mats) tris.Add(new List<int>());

            foreach (Box b in boxes)
            {
                List<int> t = tris[mats.IndexOf(b.mat)];
                AddBox(b, verts, normals, uvs, t);
            }

            Mesh mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = mats.Count;
            for (int i = 0; i < mats.Count; i++) mesh.SetTriangles(tris[i], i);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            materials = mats.ToArray();
            return mesh;
        }

        /// <summary>
        /// Escreve as 6 faces de uma caixa nas listas de vértices.
        /// </summary>
        /// <param name="b">Caixa.</param>
        /// <param name="verts">Vértices.</param>
        /// <param name="normals">Normais.</param>
        /// <param name="uvs">Coordenadas de textura.</param>
        /// <param name="tris">Triângulos da submalha.</param>
        private static void AddBox(Box b, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris)
        {
            Vector3 h = b.size * 0.5f;
            // Cada face: normal, eixo U e eixo V (definem orientação e coordenadas de textura).
            Vector3[] n = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            Vector3[] u = { Vector3.back, Vector3.forward, Vector3.right, Vector3.right, Vector3.right, Vector3.left };
            Vector3[] v = { Vector3.up, Vector3.up, Vector3.back, Vector3.forward, Vector3.up, Vector3.up };

            for (int f = 0; f < 6; f++)
            {
                Vector3 normal = n[f], ua = u[f], va = v[f];
                Vector3 c = Vector3.Scale(normal, h);
                float hu = Mathf.Abs(Vector3.Dot(h, ua));
                float hv = Mathf.Abs(Vector3.Dot(h, va));
                int start = verts.Count;
                Vector3[] corners =
                {
                    c - ua * hu - va * hv,
                    c + ua * hu - va * hv,
                    c + ua * hu + va * hv,
                    c - ua * hu + va * hv
                };
                foreach (Vector3 corner in corners)
                {
                    verts.Add(b.center + b.rot * corner);
                    normals.Add(b.rot * normal);
                    // Coordenadas de textura em metros, relativas ao canto da caixa.
                    Vector3 local = corner + h;
                    uvs.Add(new Vector2(Vector3.Dot(local, ua), Vector3.Dot(local, va)));
                }
                // Ordem horária vista de fora (convenção do Unity para a face visível).
                tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
                tris.Add(start); tris.Add(start + 2); tris.Add(start + 3);
            }
        }

        /// <summary>
        /// Gera e salva a malha como asset e aplica MeshFilter/MeshRenderer ao objeto.
        /// </summary>
        /// <param name="go">Objeto que receberá a malha.</param>
        /// <param name="meshPath">Caminho do asset da malha.</param>
        /// <returns>O MeshRenderer configurado.</returns>
        public MeshRenderer ApplyTo(GameObject go, string meshPath)
        {
            Mesh mesh = Build(go.name, out Material[] mats);
            BuilderCore.SaveAsset(mesh, meshPath);
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null) mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr == null) mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            return mr;
        }
    }
}
