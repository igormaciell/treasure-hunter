using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Forma do terreno da superfície (floresta em volta do templo), descrita por funções matemáticas
    /// em coordenadas do mundo. A mesma função gera o heightmap do Unity Terrain e dá a altura certa
    /// para posicionar os objetos do cenário em cima do terreno.
    /// Elementos: base plana (Set Height 60 / Flatten), morros suaves (Raise/Lower), um morro grande
    /// (Stamp), vale, lago, ravina e montanhas nas bordas; o templo, o acampamento e a trilha ficam planos.
    /// </summary>
    public static class TerrainShape
    {
        /// <summary>Canto mínimo do terreno no mundo (X).</summary>
        public const float OriginX = -80f;
        /// <summary>Canto mínimo do terreno no mundo (Z).</summary>
        public const float OriginZ = -115f;
        /// <summary>Largura e comprimento do terreno (Terrain Width/Length), em metros.</summary>
        public const float Size = 160f;
        /// <summary>Altura máxima do terreno (Terrain Height), como na aula.</summary>
        public const float MaxHeight = 600f;
        /// <summary>Altura da base plana (Set Height + Flatten All), como na aula.</summary>
        public const float BaseHeight = 60f;

        /// <summary>Centro do lago.</summary>
        public static readonly Vector2 LakeCenter = new Vector2(-22f, -46f);
        /// <summary>Raios do lago (X e Z).</summary>
        public const float LakeRadiusX = 10f, LakeRadiusZ = 8f;
        /// <summary>Profundidade máxima do lago.</summary>
        public const float LakeDepth = 2.4f;
        /// <summary>Altura da superfície da água.</summary>
        public const float WaterLevel = -0.6f;

        /// <summary>Trecho em Z ocupado pela ravina.</summary>
        public const float RavineStartZ = -92f, RavineEndZ = -10f;
        /// <summary>Profundidade máxima da ravina.</summary>
        public const float RavineDepth = 8f;
        /// <summary>Linha (em Z) onde a ponte atravessa a ravina.</summary>
        public const float BridgeZ = -45f;

        /// <summary>
        /// Centro da trilha principal (acampamento → templo) para um dado Z.
        /// </summary>
        /// <param name="z">Coordenada Z.</param>
        /// <returns>Coordenada X do centro da trilha.</returns>
        public static float PathX(float z)
        {
            return Mathf.Sin(z * 0.25f) * 0.8f;
        }

        /// <summary>
        /// Centro da ravina (em X) para um dado Z — uma linha levemente sinuosa.
        /// </summary>
        /// <param name="z">Coordenada Z.</param>
        /// <returns>Coordenada X do centro da ravina.</returns>
        public static float RavineX(float z)
        {
            return 24f + 2f * Mathf.Sin(z * 0.12f);
        }

        /// <summary>
        /// Ruído fractal (várias camadas de Perlin) entre 0 e 1.
        /// </summary>
        /// <param name="x">Coordenada X já escalada.</param>
        /// <param name="z">Coordenada Z já escalada.</param>
        /// <returns>Valor do ruído.</returns>
        public static float Fbm(float x, float z)
        {
            float sum = 0f, amp = 0.5f, freq = 1f, norm = 0f;
            for (int i = 0; i < 4; i++)
            {
                sum += amp * Mathf.PerlinNoise(x * freq + 37.1f * i, z * freq + 11.7f * i);
                norm += amp;
                amp *= 0.5f;
                freq *= 2f;
            }
            return sum / norm;
        }

        /// <summary>
        /// Interpolação suave entre 0 e 1 quando v vai de a até b.
        /// </summary>
        /// <param name="a">Início.</param>
        /// <param name="b">Fim.</param>
        /// <param name="v">Valor.</param>
        /// <returns>Valor suavizado entre 0 e 1.</returns>
        public static float Smooth(float a, float b, float v)
        {
            float t = Mathf.Clamp01((v - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Distância de um ponto a um retângulo (0 dentro dele).
        /// </summary>
        /// <param name="x">X do ponto.</param>
        /// <param name="z">Z do ponto.</param>
        /// <param name="x0">X mínimo.</param>
        /// <param name="x1">X máximo.</param>
        /// <param name="z0">Z mínimo.</param>
        /// <param name="z1">Z máximo.</param>
        /// <returns>Distância em metros.</returns>
        public static float RectDistance(float x, float z, float x0, float x1, float z0, float z1)
        {
            float dx = Mathf.Max(x0 - x, 0f, x - x1);
            float dz = Mathf.Max(z0 - z, 0f, z - z1);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// Distância de um ponto a um segmento de reta.
        /// </summary>
        /// <param name="p">Ponto.</param>
        /// <param name="a">Início do segmento.</param>
        /// <param name="b">Fim do segmento.</param>
        /// <returns>Distância em metros.</returns>
        public static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>
        /// Quanto um ponto pertence às áreas que precisam ficar planas na altura 0
        /// (templo e escadaria, acampamento, trilha principal e cabeceiras da ponte).
        /// </summary>
        /// <param name="x">X do mundo.</param>
        /// <param name="z">Z do mundo.</param>
        /// <returns>1 dentro das áreas planas, 0 longe delas.</returns>
        public static float FlatMask(float x, float z)
        {
            float m = 1f - Smooth(0f, 8f, RectDistance(x, z, -13f, 13f, -34f, 2f));
            m = Mathf.Max(m, 1f - Smooth(10f, 16f, Vector2.Distance(new Vector2(x, z), new Vector2(0f, -60f))));
            if (z > -70f && z < -22f) m = Mathf.Max(m, 1f - Smooth(3f, 8f, Mathf.Abs(x - PathX(z))));
            m = Mathf.Max(m, 1f - Smooth(0f, 5f, RectDistance(x, z, 14f, 34f, BridgeZ - 3.5f, BridgeZ + 3.5f)));
            return m;
        }

        /// <summary>
        /// Distância normalizada ao centro do lago (menor que 1 = dentro do lago).
        /// </summary>
        /// <param name="x">X do mundo.</param>
        /// <param name="z">Z do mundo.</param>
        /// <returns>Valor da elipse (0 no centro, 1 na borda).</returns>
        public static float LakeEllipse(float x, float z)
        {
            float dx = (x - LakeCenter.x) / LakeRadiusX;
            float dz = (z - LakeCenter.y) / LakeRadiusZ;
            return dx * dx + dz * dz;
        }

        /// <summary>
        /// Profundidade da ravina num ponto (0 fora dela). O fundo é plano e as pontas sobem até o nível do chão,
        /// para o jogador conseguir sair caminhando se cair.
        /// </summary>
        /// <param name="x">X do mundo.</param>
        /// <param name="z">Z do mundo.</param>
        /// <returns>Profundidade em metros.</returns>
        public static float RavineCut(float x, float z)
        {
            if (z < RavineStartZ || z > RavineEndZ) return 0f;
            float ends = Mathf.Clamp01(Mathf.Min((z - RavineStartZ) / 16f, (RavineEndZ - z) / 16f));
            float d = Mathf.Abs(x - RavineX(z));
            return RavineDepth * ends * (1f - Smooth(2.5f, 6f, d));
        }

        /// <summary>
        /// Altura do terreno (em metros, relativa ao chão do jogo y = 0) num ponto do mundo.
        /// </summary>
        /// <param name="x">X do mundo.</param>
        /// <param name="z">Z do mundo.</param>
        /// <returns>Altura do terreno.</returns>
        public static float Height(float x, float z)
        {
            float flat = FlatMask(x, z);

            // Morros suaves (como o pincel Raise or Lower), apagados nas áreas planas.
            float hills = (Fbm(x * 0.045f, z * 0.045f) - 0.42f) * 6f * (1f - flat);

            // Vale a noroeste.
            float valley = -3f * Mathf.Exp(-((x + 24f) * (x + 24f) + (z + 14f) * (z + 14f)) / 98f) * (1f - flat);

            // Montanhas nas bordas da área jogável (moldura natural da floresta).
            // Distância até a borda da área jogável, com contorno irregular (ruído) e cantos arredondados.
            float edge = RectDistance(x, z, -40f, 40f, -86f, 6f) + (Fbm(x * 0.05f + 7f, z * 0.05f) - 0.5f) * 12f;
            float mountains = 26f * Smooth(0f, 30f, edge) * (0.7f + 0.6f * Fbm(x * 0.03f + 50f, z * 0.03f)) * (1f - flat);

            // Morro grande "carimbado" atrás do templo (como o Stamp Terrain da aula).
            float sx = x - 30f, sz = z - 28f;
            float stamp = 40f * Mathf.Exp(-(sx * sx + sz * sz) / (2f * 13f * 13f)) * (1f - flat);

            float h = hills + valley + mountains + stamp;

            // Lago: bacia elíptica que se mistura com o chão nas margens.
            float e = LakeEllipse(x, z);
            if (e < 1.6f)
            {
                float lake = e < 1f ? -LakeDepth * Mathf.Pow(1f - e, 0.7f) : 0f;
                h = Mathf.Lerp(lake, h, Smooth(0.85f, 1.6f, e));
            }

            // Ravina: corte profundo e estreito a leste.
            h -= RavineCut(x, z);
            return h;
        }

        /// <summary>
        /// Inclinação do terreno num ponto (tangente do ângulo; 1 = 45°).
        /// </summary>
        /// <param name="x">X do mundo.</param>
        /// <param name="z">Z do mundo.</param>
        /// <returns>Inclinação.</returns>
        public static float Slope(float x, float z)
        {
            const float d = 0.5f;
            float hx = (Height(x + d, z) - Height(x - d, z)) / (2f * d);
            float hz = (Height(x, z + d) - Height(x, z - d)) / (2f * d);
            return Mathf.Sqrt(hx * hx + hz * hz);
        }

        /// <summary>
        /// Diz se um ponto está no buraco do poço sob a câmara do templo (o terreno é furado ali).
        /// </summary>
        /// <param name="x">X do mundo.</param>
        /// <param name="z">Z do mundo.</param>
        /// <returns>true se o ponto está dentro do poço.</returns>
        public static bool IsShaftHole(float x, float z)
        {
            return x > -5.3f && x < 5.3f && z > -16.3f && z < -5.7f;
        }

        /// <summary>
        /// Diz se um ponto é bom para enfeites (fora do lago, da ravina, de encostas íngremes e da área plana).
        /// </summary>
        /// <param name="x">X do mundo.</param>
        /// <param name="z">Z do mundo.</param>
        /// <param name="clearance">Folga extra em volta de lago e ravina.</param>
        /// <returns>true se dá para colocar um objeto ali.</returns>
        public static bool IsGoodGround(float x, float z, float clearance)
        {
            if (LakeEllipse(x, z) < 1.3f + clearance * 0.1f) return false;
            if (z > RavineStartZ - 2f && z < RavineEndZ + 2f && Mathf.Abs(x - RavineX(z)) < 7f + clearance) return false;
            if (Slope(x, z) > 0.45f) return false;
            return Height(x, z) < 6f;
        }
    }
}
