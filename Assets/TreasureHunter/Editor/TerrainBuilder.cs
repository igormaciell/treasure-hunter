using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Cria o Unity Terrain da superfície (floresta em volta do templo) seguindo os passos da aula de terreno:
    /// Terrain Settings (altura 600, detail resolution 256 com 32 por patch), Set Height 60 + Flatten,
    /// camadas de textura com normal map (tiling 20, normal scale 5), caminhos pintados, morros (Raise/Lower),
    /// morro carimbado (Stamp), grama sem billboard com noise spread, e árvores plantadas em massa
    /// com tamanhos aleatórios. Também cria a água do lago.
    /// Usa os pacotes da aula (Nature Starter Kit 2 e Grass Flowers Pack Free) quando eles estão importados;
    /// sem eles, usa texturas e árvores próprias no estilo voxel.
    /// </summary>
    public static class TerrainBuilder
    {
        private const string Folder = BuilderCore.Generated + "/Terrain";
        private const string TexFolder = Folder + "/Textures";

        // Ordem das camadas de textura (Terrain Layers).
        private const int LGrass = 0, LDry = 1, LDirt = 2, LSand = 3, LGravel = 4, LRock = 5, LayerCount = 6;

        /// <summary>Resumo do que foi usado (pacotes da aula ou assets próprios), para o relatório.</summary>
        public static string Report = "";

        /// <summary>Assets dos pacotes da aula encontrados no projeto.</summary>
        private class PackAssets
        {
            public Texture2D ground, groundNormal;
            public readonly List<Texture2D> grass = new List<Texture2D>();
            public readonly List<GameObject> trees = new List<GameObject>();
        }

        // ================================================================== Entrada

        /// <summary>
        /// Constrói o terreno completo e a água do lago.
        /// </summary>
        /// <param name="parent">Grupo da cena onde o terreno é colocado.</param>
        /// <returns>O componente Terrain criado.</returns>
        public static Terrain Build(Transform parent)
        {
            BuilderCore.EnsureFolder(TexFolder);
            PackAssets pack = FindPackAssets();

            // 6) Terrain Settings: tamanho, altura 600 e detail resolution 256 / 32 por patch.
            TerrainData td = new TerrainData();
            td.heightmapResolution = 513;
            td.size = new Vector3(TerrainShape.Size, TerrainShape.MaxHeight, TerrainShape.Size);
            td.SetDetailResolution(256, 32);
            td.alphamapResolution = 512;
            td.baseMapResolution = 512;
            BuilderCore.SaveAsset(td, Folder + "/Terreno_do_Templo.asset");

            // 8) Camadas de textura com normal map.
            td.terrainLayers = CreateLayers(pack);

            // 7) e 9) Alturas: Set Height 60 + Flatten, depois morros, vale, lago, ravina, montanhas e stamp.
            FillHeights(td);
            FillHoles(td);
            // 8.1) Pintura das texturas: grama em tudo, depois caminhos, areia, cascalho e rocha.
            FillAlphamaps(td);
            // 10) Paint Details: grama sem billboard.
            FillDetails(td, pack);
            // 11) Paint Trees / Mass Place Trees.
            FillTrees(td, pack);
            EditorUtility.SetDirty(td);

            GameObject go = Terrain.CreateTerrainGameObject(td);
            go.name = "Terreno";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(TerrainShape.OriginX, -TerrainShape.BaseHeight, TerrainShape.OriginZ);
            go.tag = GameTags.Ground;
            go.isStatic = true;
            Terrain terrain = go.GetComponent<Terrain>();
            Material mat = TerrainMaterial();
            if (mat != null) terrain.materialTemplate = mat;
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 4f;
            terrain.basemapDistance = 250f;
            terrain.detailObjectDistance = 70f;
            terrain.treeDistance = 220f;
            AssetCategory cat = go.AddComponent<AssetCategory>();
            cat.category = AssetCategoryType.Terreno;
            cat.displayName = "Terreno do templo (Unity Terrain)";

            BuildWater(parent);
            return terrain;
        }

        // ================================================================== Pacotes da aula

        /// <summary>
        /// Refaz o resumo dos assets do terreno (usado pelo inventário quando o resumo ainda não existe nesta sessão do editor).
        /// </summary>
        public static void RefreshReport()
        {
            FindPackAssets();
        }

        /// <summary>
        /// Procura no projeto os assets dos pacotes usados na aula (Nature Starter Kit 2 e Grass Flowers Pack Free).
        /// </summary>
        /// <returns>O que foi encontrado (listas vazias quando os pacotes não estão importados).</returns>
        private static PackAssets FindPackAssets()
        {
            PackAssets p = new PackAssets();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith(BuilderCore.Root)) continue;
                string folder = path.ToLowerInvariant().Replace(" ", "").Replace("_", "");
                string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                bool isNormal = file.Contains("normal") || file.Contains("nrm") || file.EndsWith("_n") || file.EndsWith("_nm");

                // Textura "ground" dos pacotes da aula (difusa e normal map), usada como primeira camada.
                bool inPack = folder.Contains("naturestarterkit") || (folder.Contains("grass") && folder.Contains("flower"));
                if (inPack && file.Contains("ground"))
                {
                    if (isNormal) { if (p.groundNormal == null) p.groundNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(path); }
                    else if (p.ground == null || file.Contains("ground1") || file.Contains("ground01")) p.ground = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }

                // Texturas de grama e flores do Grass Flowers Pack Free (detalhes do terreno).
                if (folder.Contains("grass") && folder.Contains("flower") && !isNormal && (file.Contains("grass") || file.Contains("flower")) && p.grass.Count < 4)
                    p.grass.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            }

            // Árvores e arbustos do Nature Starter Kit 2 (precisam de renderizador ou LOD na raiz).
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string folder = path.ToLowerInvariant().Replace(" ", "").Replace("_", "");
                string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (!folder.Contains("naturestarterkit") || !(file.Contains("tree") || file.Contains("bush")) || p.trees.Count >= 6) continue;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && (prefab.GetComponent<MeshRenderer>() != null || prefab.GetComponent<LODGroup>() != null)) p.trees.Add(prefab);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("- Textura base (ground do Nature Starter Kit 2): " + (p.ground != null ? AssetDatabase.GetAssetPath(p.ground) : "não encontrada — usada textura própria"));
            sb.AppendLine("- Normal map da textura base: " + (p.groundNormal != null ? AssetDatabase.GetAssetPath(p.groundNormal) : "gerado pelo projeto"));
            sb.AppendLine("- Grama/flores (Grass Flowers Pack Free): " + (p.grass.Count > 0 ? p.grass.Count + " texturas do pacote" : "não encontradas — usadas texturas próprias"));
            sb.AppendLine("- Árvores (Nature Starter Kit 2): " + (p.trees.Count > 0 ? p.trees.Count + " prefabs do pacote" : "não encontradas — usadas árvores voxel próprias"));
            Report = sb.ToString();
            return p;
        }

        // ================================================================== Texturas e camadas

        /// <summary>
        /// Cria as 6 camadas de textura do terreno. Cada uma tem normal map, tiling 20 e normal scale 5, como na aula.
        /// </summary>
        /// <param name="pack">Assets dos pacotes (a textura ground substitui a grama própria quando existe).</param>
        /// <returns>As camadas, na ordem usada pelos alphamaps.</returns>
        private static TerrainLayer[] CreateLayers(PackAssets pack)
        {
            TerrainLayer[] layers = new TerrainLayer[LayerCount];

            Texture2D grassD, grassN;
            if (pack.ground != null)
            {
                grassD = pack.ground;
                grassN = pack.groundNormal != null ? pack.groundNormal : NormalFromColors("terreno_ground_normal", ReadableColors(pack.ground), 64);
            }
            else
            {
                Color[] c = Pixels(64, 1, (x, y, r) => Mix(r, new Color(0.33f, 0.55f, 0.22f), new Color(0.24f, 0.44f, 0.16f), new Color(0.42f, 0.62f, 0.26f), (x * 7 + y * 3) % 11 == 0 ? 0.6f : 0.3f));
                grassD = SavePng("terreno_grama", c, 64, false, false);
                grassN = NormalFromColors("terreno_grama_normal", c, 64);
            }
            layers[LGrass] = Layer("Camada_Grama", grassD, grassN);
            layers[LDry] = MakeLayer("Grama_Seca", 2, (x, y, r) => Mix(r, new Color(0.62f, 0.56f, 0.28f), new Color(0.5f, 0.45f, 0.2f), new Color(0.45f, 0.55f, 0.25f), 0.3f));
            layers[LDirt] = MakeLayer("Terra_do_Caminho", 3, (x, y, r) =>
                r.NextDouble() < 0.06 ? new Color(0.62f, 0.6f, 0.55f) : Mix(r, new Color(0.47f, 0.34f, 0.22f), new Color(0.4f, 0.28f, 0.17f), new Color(0.55f, 0.4f, 0.26f), 0.35f));
            layers[LSand] = MakeLayer("Areia", 4, (x, y, r) => Mix(r, new Color(0.84f, 0.76f, 0.55f), new Color(0.76f, 0.68f, 0.48f), new Color(0.9f, 0.83f, 0.62f), 0.25f));
            layers[LGravel] = MakeLayer("Cascalho", 5, Gravel);
            layers[LRock] = MakeLayer("Rocha", 6, (x, y, r) =>
                ((x + (y / 8) * 5) % 16 == 0 || y % 8 == 0) ? new Color(0.3f, 0.3f, 0.29f) : Mix(r, new Color(0.52f, 0.51f, 0.48f), new Color(0.44f, 0.43f, 0.4f), new Color(0.6f, 0.59f, 0.56f), 0.3f));
            return layers;
        }

        /// <summary>
        /// Gera a textura difusa e o normal map de uma camada e cria o TerrainLayer.
        /// </summary>
        /// <param name="name">Nome da camada.</param>
        /// <param name="seed">Semente do ruído.</param>
        /// <param name="pixel">Função (x, y, rnd) → cor.</param>
        /// <returns>A camada criada.</returns>
        private static TerrainLayer MakeLayer(string name, int seed, Func<int, int, System.Random, Color> pixel)
        {
            Color[] c = Pixels(64, seed, pixel);
            Texture2D d = SavePng("terreno_" + name.ToLowerInvariant(), c, 64, false, false);
            Texture2D n = NormalFromColors("terreno_" + name.ToLowerInvariant() + "_normal", c, 64);
            return Layer("Camada_" + name, d, n);
        }

        /// <summary>
        /// Cria e salva um TerrainLayer com os ajustes da aula: tiling 20 (menos repetição) e normal scale 5 (mais relevo).
        /// </summary>
        /// <param name="name">Nome do asset.</param>
        /// <param name="diffuse">Textura difusa.</param>
        /// <param name="normal">Normal map.</param>
        /// <returns>A camada.</returns>
        private static TerrainLayer Layer(string name, Texture2D diffuse, Texture2D normal)
        {
            TerrainLayer l = new TerrainLayer();
            l.name = name;
            l.diffuseTexture = diffuse;
            l.normalMapTexture = normal;
            l.tileSize = new Vector2(20f, 20f);
            l.normalScale = 5f;
            l.smoothness = 0.05f;
            l.metallic = 0f;
            BuilderCore.SaveAsset(l, Folder + "/" + name + ".terrainlayer");
            return l;
        }

        /// <summary>
        /// Mistura três tons com ruído (pixel art do chão).
        /// </summary>
        /// <param name="r">Gerador aleatório.</param>
        /// <param name="a">Tom principal.</param>
        /// <param name="b">Tom escuro.</param>
        /// <param name="c">Tom claro.</param>
        /// <param name="mix">Chance dos tons secundários.</param>
        /// <returns>Cor do pixel.</returns>
        private static Color Mix(System.Random r, Color a, Color b, Color c, float mix)
        {
            double v = r.NextDouble();
            Color col = v < mix * 0.5 ? b : (v < mix ? c : a);
            return TextureFactory.Jitter(col, r, 0.06f);
        }

        /// <summary>
        /// Pixel de cascalho: pedrinhas (células) com contorno escuro.
        /// </summary>
        /// <param name="x">Coluna.</param>
        /// <param name="y">Linha.</param>
        /// <param name="r">Gerador aleatório.</param>
        /// <returns>Cor do pixel.</returns>
        private static Color Gravel(int x, int y, System.Random r)
        {
            int cx = x / 6, cy = y / 6;
            int lx = x % 6, ly = y % 6;
            if (lx == 0 || ly == 0) return new Color(0.28f, 0.27f, 0.25f);
            float shade = 0.45f + ((cx * 73 + cy * 37) % 7) * 0.05f;
            return TextureFactory.Jitter(new Color(shade, shade * 0.97f, shade * 0.92f), r, 0.05f);
        }

        /// <summary>
        /// Calcula os pixels de uma textura quadrada.
        /// </summary>
        /// <param name="size">Lado em pixels.</param>
        /// <param name="seed">Semente.</param>
        /// <param name="pixel">Função (x, y, rnd) → cor.</param>
        /// <returns>Vetor de cores (linha a linha).</returns>
        private static Color[] Pixels(int size, int seed, Func<int, int, System.Random, Color> pixel)
        {
            System.Random r = new System.Random(seed * 7919);
            Color[] c = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    c[y * size + x] = pixel(x, y, r);
            return c;
        }

        /// <summary>
        /// Lê as cores de uma textura do pacote (cópia redimensionada para 64×64), para gerar um normal map quando o pacote não traz um.
        /// </summary>
        /// <param name="tex">Textura original.</param>
        /// <returns>Cores 64×64.</returns>
        private static Color[] ReadableColors(Texture2D tex)
        {
            RenderTexture rt = RenderTexture.GetTemporary(64, 64, 0);
            Graphics.Blit(tex, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D copy = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
            copy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            Color[] c = copy.GetPixels();
            UnityEngine.Object.DestroyImmediate(copy);
            return c;
        }

        /// <summary>
        /// Gera um normal map a partir do brilho das cores (pixels claros = mais altos) e o importa como Normal Map.
        /// </summary>
        /// <param name="name">Nome do arquivo.</param>
        /// <param name="colors">Cores da textura difusa.</param>
        /// <param name="size">Lado em pixels.</param>
        /// <returns>O normal map importado.</returns>
        private static Texture2D NormalFromColors(string name, Color[] colors, int size)
        {
            float H(int x, int y)
            {
                Color c = colors[((y + size) % size) * size + ((x + size) % size)];
                return c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
            }

            Color[] n = new Color[size * size];
            const float strength = 3f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (H(x + 1, y) - H(x - 1, y)) * strength;
                    float dy = (H(x, y + 1) - H(x, y - 1)) * strength;
                    Vector3 v = new Vector3(-dx, -dy, 1f).normalized;
                    n[y * size + x] = new Color(v.x * 0.5f + 0.5f, v.y * 0.5f + 0.5f, v.z * 0.5f + 0.5f, 1f);
                }
            }
            return SavePng(name, n, size, true, false);
        }

        /// <summary>
        /// Salva cores como PNG dentro do projeto e configura a importação (difusa, normal map ou com transparência).
        /// </summary>
        /// <param name="name">Nome do arquivo (sem extensão).</param>
        /// <param name="colors">Cores.</param>
        /// <param name="size">Lado em pixels.</param>
        /// <param name="normalMap">true para importar como Normal Map.</param>
        /// <param name="alpha">true para textura com transparência (grama).</param>
        /// <returns>A textura importada.</returns>
        private static Texture2D SavePng(string name, Color[] colors, int size, bool normalMap, bool alpha)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(colors);
            tex.Apply();
            string path = TexFolder + "/" + name + ".png";
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.filterMode = normalMap ? FilterMode.Bilinear : FilterMode.Point;
            ti.wrapMode = alpha ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            ti.alphaIsTransparency = alpha;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// Material do terreno no URP (Terrain/Lit com normal maps ligados).
        /// </summary>
        /// <returns>O material salvo, ou nulo se o shader não existir (projeto fora do URP).</returns>
        private static Material TerrainMaterial()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (s == null) return null;
            Material m = new Material(s) { name = "mat_terreno" };
            m.EnableKeyword("_NORMALMAP");
            BuilderCore.SaveAsset(m, Folder + "/mat_terreno.mat");
            return m;
        }

        // ================================================================== Alturas, buraco e pintura

        /// <summary>
        /// Converte a posição de uma amostra do terreno (índice) para coordenadas do mundo.
        /// </summary>
        /// <param name="i">Índice da amostra.</param>
        /// <param name="count">Número de intervalos (resolução − 1 para alturas; resolução para células).</param>
        /// <param name="center">true para usar o centro da célula.</param>
        /// <param name="origin">Origem do eixo no mundo.</param>
        /// <returns>Coordenada no mundo.</returns>
        private static float ToWorld(int i, int count, bool center, float origin)
        {
            return origin + (i + (center ? 0.5f : 0f)) / count * TerrainShape.Size;
        }

        /// <summary>
        /// Preenche o heightmap: base em 60 (Set Height + Flatten All) somada à forma do TerrainShape.
        /// </summary>
        /// <param name="td">Dados do terreno.</param>
        private static void FillHeights(TerrainData td)
        {
            int res = td.heightmapResolution;
            float[,] h = new float[res, res];
            for (int zi = 0; zi < res; zi++)
            {
                float z = ToWorld(zi, res - 1, false, TerrainShape.OriginZ);
                for (int xi = 0; xi < res; xi++)
                {
                    float x = ToWorld(xi, res - 1, false, TerrainShape.OriginX);
                    h[zi, xi] = Mathf.Clamp01((TerrainShape.BaseHeight + TerrainShape.Height(x, z)) / TerrainShape.MaxHeight);
                }
            }
            td.SetHeights(0, 0, h);
        }

        /// <summary>
        /// Abre um buraco no terreno sobre o poço do templo, para John cair no subterrâneo.
        /// </summary>
        /// <param name="td">Dados do terreno.</param>
        private static void FillHoles(TerrainData td)
        {
            int res = td.holesResolution;
            bool[,] solid = new bool[res, res];
            for (int zi = 0; zi < res; zi++)
            {
                float z = ToWorld(zi, res, true, TerrainShape.OriginZ);
                for (int xi = 0; xi < res; xi++)
                {
                    float x = ToWorld(xi, res, true, TerrainShape.OriginX);
                    solid[zi, xi] = !TerrainShape.IsShaftHole(x, z);
                }
            }
            td.SetHoles(0, 0, solid);
        }

        /// <summary>
        /// Calcula o peso de cada camada de textura num ponto: grama em tudo, grama seca por ruído,
        /// terra nos caminhos, areia no lago, cascalho na ravina e rocha nas encostas e montanhas.
        /// </summary>
        /// <param name="x">X do mundo.</param>
        /// <param name="z">Z do mundo.</param>
        /// <returns>Pesos das 6 camadas (somam 1).</returns>
        private static float[] Weights(float x, float z)
        {
            float[] w = new float[LayerCount];
            w[LGrass] = 1f;
            float h = TerrainShape.Height(x, z);
            float flat = TerrainShape.FlatMask(x, z);

            Overlay(w, LDry, Mathf.Clamp01((TerrainShape.Fbm(x * 0.08f + 100f, z * 0.08f) - 0.55f) * 3f) * (1f - flat * 0.6f));
            Overlay(w, LSand, 1f - TerrainShape.Smooth(1.05f, 1.5f, TerrainShape.LakeEllipse(x, z)));
            Overlay(w, LDirt, PathWeight(x, z));
            Overlay(w, LGravel, TerrainShape.Smooth(0.3f, 1.5f, TerrainShape.RavineCut(x, z)));
            float rock = Mathf.Max(TerrainShape.Smooth(0.75f, 1.1f, TerrainShape.Slope(x, z)), TerrainShape.Smooth(18f, 28f, h));
            Overlay(w, LRock, rock);
            return w;
        }

        /// <summary>
        /// Pinta uma camada por cima das outras com a força informada (como o pincel Paint Texture com opacidade).
        /// </summary>
        /// <param name="w">Pesos atuais.</param>
        /// <param name="layer">Camada pintada.</param>
        /// <param name="amount">Força (0 a 1).</param>
        private static void Overlay(float[] w, int layer, float amount)
        {
            amount = Mathf.Clamp01(amount);
            if (amount <= 0f) return;
            for (int i = 0; i < w.Length; i++) w[i] *= 1f - amount;
            w[layer] += amount;
        }

        /// <summary>
        /// Peso da textura de caminho: trilha principal, pátio do acampamento, frente da escadaria e trilhas para o lago e a ponte.
        /// </summary>
        /// <param name="x">X do mundo.</param>
        /// <param name="z">Z do mundo.</param>
        /// <returns>Peso entre 0 e 1.</returns>
        private static float PathWeight(float x, float z)
        {
            Vector2 p = new Vector2(x, z);
            float w = 0f;
            if (z > -67f && z < -25f) w = 1f - TerrainShape.Smooth(1.4f, 2.2f, Mathf.Abs(x - TerrainShape.PathX(z)));
            w = Mathf.Max(w, 1f - TerrainShape.Smooth(4.5f, 6f, Vector2.Distance(p, new Vector2(0f, -60f))));
            w = Mathf.Max(w, 1f - TerrainShape.Smooth(0f, 1.5f, TerrainShape.RectDistance(x, z, -6f, 6f, -31f, -26f)));
            w = Mathf.Max(w, 1f - TerrainShape.Smooth(0.9f, 1.6f, TerrainShape.SegmentDistance(p, new Vector2(0f, -50f), new Vector2(-11f, -47f))));
            float bx = TerrainShape.RavineX(TerrainShape.BridgeZ);
            w = Mathf.Max(w, 1f - TerrainShape.Smooth(0.9f, 1.6f, TerrainShape.SegmentDistance(p, new Vector2(0f, -38f), new Vector2(bx - 7f, TerrainShape.BridgeZ))));
            w = Mathf.Max(w, 1f - TerrainShape.Smooth(0.9f, 1.6f, TerrainShape.SegmentDistance(p, new Vector2(bx + 7f, TerrainShape.BridgeZ), new Vector2(36f, TerrainShape.BridgeZ))));
            return w;
        }

        /// <summary>
        /// Preenche os alphamaps (pintura das camadas) do terreno inteiro.
        /// </summary>
        /// <param name="td">Dados do terreno.</param>
        private static void FillAlphamaps(TerrainData td)
        {
            int res = td.alphamapResolution;
            float[,,] maps = new float[res, res, LayerCount];
            for (int zi = 0; zi < res; zi++)
            {
                float z = ToWorld(zi, res, true, TerrainShape.OriginZ);
                for (int xi = 0; xi < res; xi++)
                {
                    float x = ToWorld(xi, res, true, TerrainShape.OriginX);
                    float[] w = Weights(x, z);
                    for (int l = 0; l < LayerCount; l++) maps[zi, xi, l] = w[l];
                }
            }
            td.SetAlphamaps(0, 0, maps);
        }

        // ================================================================== Detalhes (grama)

        /// <summary>
        /// Cria os detalhes de grama e flores (render mode Grass, ou seja, sem billboard) e os espalha
        /// onde há grama, deixando livres caminhos, areia, cascalho, rocha, água e o pátio do templo.
        /// </summary>
        /// <param name="td">Dados do terreno.</param>
        /// <param name="pack">Assets dos pacotes (texturas de grama do Grass Flowers Pack Free).</param>
        private static void FillDetails(TerrainData td, PackAssets pack)
        {
            List<Texture2D> textures = new List<Texture2D>(pack.grass);
            if (textures.Count == 0)
            {
                textures.Add(GrassTexture("detalhe_grama_alta", 1, false));
                textures.Add(GrassTexture("detalhe_grama_baixa", 2, false));
                textures.Add(GrassTexture("detalhe_flores", 3, true));
            }

            DetailPrototype[] protos = new DetailPrototype[textures.Count];
            for (int i = 0; i < textures.Count; i++)
            {
                bool flowers = textures[i].name.ToLowerInvariant().Contains("flor") || textures[i].name.ToLowerInvariant().Contains("flower");
                protos[i] = new DetailPrototype
                {
                    prototypeTexture = textures[i],
                    usePrototypeMesh = false,
                    renderMode = DetailRenderMode.Grass, // "desmarcar billboard": a grama não fica virando para a câmera
                    minWidth = 0.6f, maxWidth = 1.1f,
                    minHeight = flowers ? 0.4f : 0.5f, maxHeight = flowers ? 0.7f : 1.0f,
                    noiseSpread = 0.5f, // menor = manchas maiores de grama seca; maior = mais verde
                    healthyColor = new Color(0.45f, 0.75f, 0.3f),
                    dryColor = new Color(0.8f, 0.72f, 0.35f)
                };
            }

            // Quantidade de instâncias por célula (modo Instance Count).
            td.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
            td.detailPrototypes = protos;
            td.RefreshPrototypes();

            int res = td.detailResolution;
            int[][,] layers = new int[protos.Length][,];
            for (int i = 0; i < protos.Length; i++) layers[i] = new int[res, res];
            System.Random rnd = new System.Random(4242);

            for (int zi = 0; zi < res; zi++)
            {
                float z = ToWorld(zi, res, true, TerrainShape.OriginZ);
                for (int xi = 0; xi < res; xi++)
                {
                    float x = ToWorld(xi, res, true, TerrainShape.OriginX);
                    if (TerrainShape.IsShaftHole(x, z) || TerrainShape.RectDistance(x, z, -9f, 9f, -27f, -3f) < 0.01f) continue;
                    if (TerrainShape.Height(x, z) < -0.3f) continue;
                    float[] w = Weights(x, z);
                    if (w[LDirt] > 0.3f || w[LSand] > 0.3f || w[LGravel] > 0.3f || w[LRock] > 0.3f) continue;

                    float n = TerrainShape.Fbm(x * 0.15f + 300f, z * 0.15f);
                    int count = Mathf.RoundToInt(Mathf.Lerp(0f, 3f, n * 1.4f - 0.2f));
                    if (count <= 0) continue;
                    for (int i = 0; i < protos.Length; i++)
                    {
                        bool flowers = protos[i].maxHeight < 0.8f;
                        if (flowers) layers[i][zi, xi] = rnd.NextDouble() < 0.06 ? 1 : 0;
                        else layers[i][zi, xi] = rnd.Next(0, count + 1);
                    }
                }
            }
            for (int i = 0; i < protos.Length; i++) td.SetDetailLayer(0, 0, i, layers[i]);
            td.wavingGrassTint = new Color(0.7f, 0.8f, 0.6f);
            td.wavingGrassAmount = 0.3f;
            td.wavingGrassSpeed = 0.4f;
            td.wavingGrassStrength = 0.4f;
        }

        /// <summary>
        /// Gera uma textura de tufo de grama (ou com flores) em pixel art, com fundo transparente.
        /// </summary>
        /// <param name="name">Nome do arquivo.</param>
        /// <param name="seed">Semente.</param>
        /// <param name="flowers">true para pôr flores no topo.</param>
        /// <returns>A textura importada com transparência.</returns>
        private static Texture2D GrassTexture(string name, int seed, bool flowers)
        {
            const int size = 32;
            System.Random r = new System.Random(seed * 131);
            Color[] c = new Color[size * size];
            for (int i = 0; i < c.Length; i++) c[i] = new Color(0, 0, 0, 0);
            int blades = flowers ? 9 : 14;
            for (int b = 0; b < blades; b++)
            {
                int x = 2 + r.Next(size - 4);
                int top = (flowers ? 14 : (seed == 1 ? 18 : 10)) + r.Next(size - (flowers ? 16 : (seed == 1 ? 18 : 12)));
                float lean = (float)(r.NextDouble() - 0.5) * 0.4f;
                Color g = Color.Lerp(new Color(0.3f, 0.55f, 0.18f), new Color(0.5f, 0.75f, 0.28f), (float)r.NextDouble());
                for (int y = 0; y < top && y < size; y++)
                {
                    int px = Mathf.Clamp(x + Mathf.RoundToInt(lean * y), 0, size - 1);
                    c[y * size + px] = Color.Lerp(g * 0.7f, g, y / (float)top);
                }
                if (flowers && top < size - 2)
                {
                    Color petal = r.NextDouble() < 0.5 ? new Color(0.98f, 0.82f, 0.2f) : new Color(0.7f, 0.35f, 0.85f);
                    int px = Mathf.Clamp(x + Mathf.RoundToInt(lean * top), 1, size - 2);
                    for (int dy = 0; dy < 2; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                            c[(top + dy) * size + px + dx] = petal;
                }
            }
            for (int i = 0; i < c.Length; i++) c[i] = new Color(c[i].r, c[i].g, c[i].b, c[i].a > 0f ? 1f : 0f);
            return SavePng(name, c, size, false, true);
        }

        // ================================================================== Árvores

        /// <summary>
        /// Define os tipos de árvore e planta árvores em massa (Mass Place Trees) com tamanhos e rotações aleatórios,
        /// longe da trilha, do templo, do acampamento, do lago e da ravina.
        /// </summary>
        /// <param name="td">Dados do terreno.</param>
        /// <param name="pack">Assets dos pacotes (árvores do Nature Starter Kit 2).</param>
        private static void FillTrees(TerrainData td, PackAssets pack)
        {
            List<GameObject> prefabs = new List<GameObject>(pack.trees);
            if (prefabs.Count == 0)
            {
                prefabs.Add(PropFactory.Prefabs["Arvore_Carvalho"]);
                prefabs.Add(PropFactory.Prefabs["Arvore_Pinheiro"]);
                prefabs.Add(PropFactory.Prefabs["Arvore_Tropical"]);
            }
            TreePrototype[] protos = new TreePrototype[prefabs.Count];
            for (int i = 0; i < prefabs.Count; i++) protos[i] = new TreePrototype { prefab = prefabs[i] };
            td.treePrototypes = protos;
            td.RefreshPrototypes();

            System.Random rnd = new System.Random(77);
            List<TreeInstance> trees = new List<TreeInstance>();
            for (int attempt = 0; attempt < 8000 && trees.Count < 280; attempt++)
            {
                float x = TerrainShape.OriginX + 2f + (float)rnd.NextDouble() * (TerrainShape.Size - 4f);
                float z = TerrainShape.OriginZ + 2f + (float)rnd.NextDouble() * (TerrainShape.Size - 4f);
                if (!TreeSpotOk(x, z)) continue;

                float scale = 0.8f + (float)rnd.NextDouble() * 0.6f; // tamanhos aleatórios, como no Paint Trees
                TreeInstance t = new TreeInstance
                {
                    position = new Vector3((x - TerrainShape.OriginX) / TerrainShape.Size, 0f, (z - TerrainShape.OriginZ) / TerrainShape.Size),
                    widthScale = scale,
                    heightScale = scale * (0.9f + (float)rnd.NextDouble() * 0.2f),
                    rotation = (float)rnd.NextDouble() * Mathf.PI * 2f,
                    color = new Color32(255, 255, 255, 255),
                    lightmapColor = new Color32(255, 255, 255, 255),
                    prototypeIndex = rnd.Next(protos.Length)
                };
                trees.Add(t);
            }
            td.SetTreeInstances(trees.ToArray(), true);
        }

        /// <summary>
        /// Diz se uma posição serve para plantar uma árvore do terreno.
        /// </summary>
        /// <param name="x">X do mundo.</param>
        /// <param name="z">Z do mundo.</param>
        /// <returns>true se a posição é livre e não muito íngreme.</returns>
        private static bool TreeSpotOk(float x, float z)
        {
            if (TerrainShape.FlatMask(x, z) > 0.05f) return false;
            if (PathWeight(x, z) > 0.01f) return false;
            if (TerrainShape.LakeEllipse(x, z) < 1.5f) return false;
            if (z > TerrainShape.RavineStartZ - 3f && z < TerrainShape.RavineEndZ + 3f && Mathf.Abs(x - TerrainShape.RavineX(z)) < 8f) return false;
            if (TerrainShape.Slope(x, z) > 0.7f) return false;
            return TerrainShape.Height(x, z) < 24f;
        }

        // ================================================================== Água

        /// <summary>
        /// Cria a superfície da água do lago (placa transparente; as margens do terreno escondem as bordas).
        /// </summary>
        /// <param name="parent">Grupo da cena.</param>
        private static void BuildWater(Transform parent)
        {
            GameObject go = new GameObject("Agua_do_Lago");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(TerrainShape.LakeCenter.x, TerrainShape.WaterLevel, TerrainShape.LakeCenter.y);
            VoxelModel m = new VoxelModel().Add(Vector3.zero, new Vector3(TerrainShape.LakeRadiusX * 2f + 6f, 0.04f, TerrainShape.LakeRadiusZ * 2f + 6f), WaterMaterial());
            m.ApplyTo(go, Folder + "/malha_agua.asset");
            AssetCategory cat = go.AddComponent<AssetCategory>();
            cat.category = AssetCategoryType.Terreno;
            cat.displayName = "Água do lago";
        }

        /// <summary>
        /// Material de água do URP: azul, transparente e bem liso (reflete o céu).
        /// </summary>
        /// <returns>O material salvo.</returns>
        private static Material WaterMaterial()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material m = new Material(s) { name = "mat_agua" };
            Color c = new Color(0.16f, 0.42f, 0.55f, 0.72f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.92f);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            BuilderCore.SaveAsset(m, Folder + "/mat_agua.mat");
            return m;
        }
    }
}
