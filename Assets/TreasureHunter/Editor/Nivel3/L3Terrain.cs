using System;
using UnityEditor;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Terreno da Caverna de Cristal (Unity Terrain): chão de entrada (y = 0), chão da saída (y = 13,5), paredes
    /// naturais subindo até o teto e um grande BURACO (Terrain Holes) no meio — o abismo que leva ao Void.
    /// Cinco camadas de textura, cada uma com textura difusa e normal map próprios.
    /// </summary>
    public static class L3Terrain
    {
        public const float OriginX = -45f, OriginZ = -30f, OriginY = -40f;
        public const float SizeX = 90f, SizeZ = 180f, MaxHeight = 90f;
        public const float StartFloor = 0f, EndFloor = 13.5f;
        /// <summary>O abismo começa depois desta linha (borda da área de entrada).</summary>
        public const float HoleStartZ = 6f;
        /// <summary>O abismo termina nesta linha (borda da área de saída).</summary>
        public const float HoleEndZ = 127.5f;
        /// <summary>Meia largura do abismo.</summary>
        public const float HoleHalfWidth = 18f;
        private const int LayerCount = 5;

        /// <summary>
        /// Cria o terreno, as camadas e o material.
        /// </summary>
        /// <param name="parent">Grupo da cena.</param>
        /// <returns>O Terrain.</returns>
        public static Terrain Build(Transform parent)
        {
            TerrainData td = new TerrainData();
            td.heightmapResolution = 257;
            td.size = new Vector3(SizeX, MaxHeight, SizeZ);
            td.alphamapResolution = 256;
            td.baseMapResolution = 256;
            BuilderCore.SaveAsset(td, L2Core.Root + "/Terreno_da_Caverna.asset");

            td.terrainLayers = new[]
            {
                Layer("Rocha_da_Caverna", 11, (x, y, r) => Mix(r, new Color(0.3f, 0.31f, 0.36f), new Color(0.16f, 0.16f, 0.2f), new Color(0.45f, 0.47f, 0.55f), 0.3f), 12f),
                Layer("Cascalho_Umido", 23, (x, y, r) => Mix(r, new Color(0.27f, 0.27f, 0.29f), new Color(0.1f, 0.1f, 0.12f), new Color(0.5f, 0.52f, 0.58f), 0.45f), 8f),
                Layer("Musgo_da_Caverna", 37, (x, y, r) => Mix(r, new Color(0.16f, 0.3f, 0.26f), new Color(0.08f, 0.16f, 0.14f), new Color(0.3f, 0.55f, 0.45f), 0.35f), 10f),
                Layer("Areia_de_Cristal", 41, (x, y, r) => r.NextDouble() < 0.05 ? new Color(0.7f, 0.9f, 1f) : Mix(r, new Color(0.38f, 0.42f, 0.52f), new Color(0.26f, 0.28f, 0.36f), new Color(0.55f, 0.6f, 0.72f), 0.3f), 8f),
                Layer("Pedra_Polida", 53, (x, y, r) => (x % 16 == 0 || y % 16 == 0) ? new Color(0.14f, 0.14f, 0.16f) : Mix(r, new Color(0.4f, 0.39f, 0.42f), new Color(0.3f, 0.29f, 0.32f), new Color(0.5f, 0.49f, 0.52f), 0.2f), 6f)
            };

            FillHeights(td);
            FillHoles(td);
            FillAlphamaps(td);
            EditorUtility.SetDirty(td);

            GameObject go = Terrain.CreateTerrainGameObject(td);
            go.name = "Terreno_da_Caverna";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(OriginX, OriginY, OriginZ);
            go.tag = GameTags.Ground;
            go.isStatic = true;
            Terrain terrain = go.GetComponent<Terrain>();
            Shader s = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (s != null)
            {
                Material m = new Material(s) { name = "mat3_terreno" };
                m.EnableKeyword("_NORMALMAP");
                BuilderCore.SaveAsset(m, L2Core.MaterialsPath + "/mat3_terreno.mat");
                terrain.materialTemplate = m;
            }
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 3f;
            terrain.basemapDistance = 150f;
            L2Core.Tag(go, AssetCategoryType.Terreno, "Terreno da caverna (Unity Terrain com abismo)");
            return terrain;
        }

        /// <summary>
        /// Altura do terreno (em coordenadas do mundo) num ponto.
        /// </summary>
        /// <param name="x">X do mundo.</param>
        /// <param name="z">Z do mundo.</param>
        /// <returns>Altura do mundo.</returns>
        public static float WorldHeight(float x, float z)
        {
            float ax = Mathf.Abs(x);
            float n = Mathf.PerlinNoise(x * 0.11f + 3.1f, z * 0.11f + 7.7f);
            float floor;
            float halfWidth;
            if (z < HoleStartZ + 2f) { floor = StartFloor; halfWidth = 14f; }
            else if (z > HoleEndZ - 2f) { floor = EndFloor; halfWidth = 13f; }
            else { floor = 22f; halfWidth = HoleHalfWidth; }

            float y = floor + (n - 0.5f) * 0.25f;
            // Paredes laterais naturais.
            if (ax > halfWidth)
            {
                float k = ax - halfWidth;
                y = floor + Mathf.Pow(k, 1.35f) * 1.6f + n * 3f;
            }
            // Parede do fundo (atrás da entrada) e parede depois da saída.
            if (z < -22f) y = Mathf.Max(y, StartFloor + Mathf.Pow(-22f - z, 1.4f) * 2.4f + n * 2f);
            if (z > 142f) y = Mathf.Max(y, EndFloor + Mathf.Pow(z - 142f, 1.4f) * 2.4f + n * 2f);
            return Mathf.Min(y, 44f);
        }

        /// <summary>
        /// Preenche o heightmap.
        /// </summary>
        /// <param name="td">Dados do terreno.</param>
        private static void FillHeights(TerrainData td)
        {
            int res = td.heightmapResolution;
            float[,] h = new float[res, res];
            for (int zi = 0; zi < res; zi++)
            {
                float z = OriginZ + zi / (float)(res - 1) * SizeZ;
                for (int xi = 0; xi < res; xi++)
                {
                    float x = OriginX + xi / (float)(res - 1) * SizeX;
                    h[zi, xi] = Mathf.Clamp01((WorldHeight(x, z) - OriginY) / MaxHeight);
                }
            }
            td.SetHeights(0, 0, h);
        }

        /// <summary>
        /// Abre o abismo no meio da caverna (Terrain Holes): quem cair ali vai para o Void.
        /// </summary>
        /// <param name="td">Dados do terreno.</param>
        private static void FillHoles(TerrainData td)
        {
            int res = td.holesResolution;
            bool[,] solid = new bool[res, res];
            for (int zi = 0; zi < res; zi++)
            {
                float z = OriginZ + (zi + 0.5f) / res * SizeZ;
                for (int xi = 0; xi < res; xi++)
                {
                    float x = OriginX + (xi + 0.5f) / res * SizeX;
                    solid[zi, xi] = !IsHole(x, z);
                }
            }
            td.SetHoles(0, 0, solid);
        }

        /// <summary>
        /// Indica se o ponto fica dentro do abismo.
        /// </summary>
        /// <param name="x">X do mundo.</param>
        /// <param name="z">Z do mundo.</param>
        /// <returns>true se é buraco.</returns>
        public static bool IsHole(float x, float z)
        {
            return z > HoleStartZ && z < HoleEndZ && Mathf.Abs(x) < HoleHalfWidth + 1.5f;
        }

        /// <summary>
        /// Pinta as camadas: rocha nas encostas, cascalho e areia de cristal no chão, musgo em manchas e
        /// pedra polida no caminho da entrada e da saída.
        /// </summary>
        /// <param name="td">Dados do terreno.</param>
        private static void FillAlphamaps(TerrainData td)
        {
            int res = td.alphamapResolution;
            float[,,] maps = new float[res, res, LayerCount];
            for (int zi = 0; zi < res; zi++)
            {
                float z = OriginZ + (zi + 0.5f) / res * SizeZ;
                for (int xi = 0; xi < res; xi++)
                {
                    float x = OriginX + (xi + 0.5f) / res * SizeX;
                    float slope = Mathf.Abs(WorldHeight(x + 0.5f, z) - WorldHeight(x - 0.5f, z)) + Mathf.Abs(WorldHeight(x, z + 0.5f) - WorldHeight(x, z - 0.5f));
                    float[] w = new float[LayerCount];
                    float rockW = Mathf.Clamp01((slope - 0.4f) * 1.5f);
                    float n = Mathf.PerlinNoise(x * 0.07f, z * 0.07f);
                    float flat = 1f - rockW;
                    w[0] = rockW + 0.05f;
                    w[1] = flat * (n > 0.5f ? 0.7f : 0.35f);
                    w[3] = flat * (n <= 0.5f ? 0.6f : 0.2f);
                    w[2] = Mathf.Clamp01(Mathf.PerlinNoise(x * 0.19f + 9f, z * 0.19f) - 0.55f) * 2f;
                    bool path = Mathf.Abs(x) < 2.2f && (z < HoleStartZ || z > HoleEndZ);
                    if (path) w[4] = 2f;
                    float sum = 0f;
                    foreach (float v in w) sum += v;
                    for (int l = 0; l < LayerCount; l++) maps[zi, xi, l] = w[l] / sum;
                }
            }
            td.SetAlphamaps(0, 0, maps);
        }

        /// <summary>
        /// Cria uma camada de terreno com textura difusa (64 px) e normal map gerado a partir dela.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="seed">Semente.</param>
        /// <param name="pixel">Função de cor.</param>
        /// <param name="tile">Tamanho do ladrilho (m).</param>
        /// <returns>A camada.</returns>
        private static TerrainLayer Layer(string name, int seed, Func<int, int, System.Random, Color> pixel, float tile)
        {
            const int size = 64;
            System.Random r = new System.Random(seed);
            Color[] c = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    c[y * size + x] = pixel(x, y, r);
            string lower = name.ToLowerInvariant();
            Texture2D diffuse = L2Core.SavePng("terreno3_" + lower, c, size, size, false, false, FilterMode.Bilinear);
            Texture2D normal = L2Core.SavePng("terreno3_" + lower + "_normal", Normals(c, size), size, size, true, false, FilterMode.Bilinear);
            TerrainLayer l = new TerrainLayer
            {
                name = "Camada3_" + name,
                diffuseTexture = diffuse,
                normalMapTexture = normal,
                tileSize = new Vector2(tile, tile),
                normalScale = 2.5f,
                smoothness = 0.12f,
                metallic = 0f
            };
            BuilderCore.SaveAsset(l, L2Core.Root + "/Camada3_" + name + ".terrainlayer");
            return l;
        }

        /// <summary>
        /// Normal map a partir da luminância (o relevo segue os tons da textura).
        /// </summary>
        /// <param name="c">Cores.</param>
        /// <param name="size">Lado.</param>
        /// <returns>Cores do normal map.</returns>
        private static Color[] Normals(Color[] c, int size)
        {
            float H(int x, int y)
            {
                Color p = c[((y + size) % size) * size + ((x + size) % size)];
                return p.r * 0.3f + p.g * 0.59f + p.b * 0.11f;
            }
            Color[] n = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (H(x + 1, y) - H(x - 1, y)) * 4f;
                    float dy = (H(x, y + 1) - H(x, y - 1)) * 4f;
                    Vector3 v = new Vector3(-dx, -dy, 1f).normalized;
                    n[y * size + x] = new Color(v.x * 0.5f + 0.5f, v.y * 0.5f + 0.5f, v.z * 0.5f + 0.5f, 1f);
                }
            }
            return n;
        }

        /// <summary>
        /// Mistura de três tons com ruído.
        /// </summary>
        /// <param name="r">Gerador.</param>
        /// <param name="a">Tom principal.</param>
        /// <param name="b">Tom escuro.</param>
        /// <param name="c">Tom claro.</param>
        /// <param name="mix">Chance dos tons secundários.</param>
        /// <returns>Cor.</returns>
        private static Color Mix(System.Random r, Color a, Color b, Color c, float mix)
        {
            double v = r.NextDouble();
            Color baseColor = v < mix * 0.5 ? b : (v < mix ? c : a);
            return L2Core.Jitter(baseColor, r, 0.07f);
        }
    }
}
