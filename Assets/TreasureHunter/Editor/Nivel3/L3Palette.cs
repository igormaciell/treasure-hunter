using UnityEditor;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Texturas (pixel art em PNG), mapas normais e materiais do Nível 3 — a Caverna de Cristal.
    /// Rochas, basalto, pedra úmida, tijolos e madeira da masmorra usam textura difusa + normal map;
    /// os cristais são emissivos (a "energia interna" pulsa com o script CrystalGlow).
    /// Usa as ferramentas do L2Core apontadas para a pasta Generated_Nivel3.
    /// </summary>
    public static class L3Palette
    {
        public static Material Rock, RockDark, Ceiling, Basalt, WetStone, Gravel, Brick, BrickDark, Wood, WoodDark, Iron, Rope, Cloth, Gold, Ruby;
        public static Material CrystalBlue, CrystalCyan, CrystalViolet, CrystalPink, CrystalAmber, CrystalCore, CrystalDark;
        public static Material Moss, Mushroom, MushroomStem, TotemStone, TotemGlow, Ghost, Marker, Void, Bone, Rune;

        /// <summary>
        /// Cria todos os materiais.
        /// </summary>
        public static void Build()
        {
            Texture2D rock = L2Core.Tex("tex3_rocha_caverna", 16, CaveRock);
            Texture2D rockN = L2Core.NormalMap("tex3_rocha_caverna_normal", rock, 4f);
            Rock = L2Core.Lit("mat3_rocha_caverna", rock, Color.white, 0.12f, 0f, rockN, 1.3f);
            RockDark = L2Core.Lit("mat3_rocha_escura", rock, new Color(0.55f, 0.57f, 0.65f), 0.1f, 0f, rockN, 1.3f);
            Ceiling = L2Core.Lit("mat3_teto_da_caverna", rock, new Color(0.38f, 0.4f, 0.48f), 0.08f, 0f, rockN, 1f);

            Texture2D basalt = L2Core.Tex("tex3_basalto", 16, (x, y, r) =>
            {
                bool seam = x % 8 == 0 || ((y + (x / 8) * 4) % 8 == 0);
                if (seam) return L2Core.Jitter(new Color(0.1f, 0.11f, 0.14f), r, 0.1f);
                return L2Core.Jitter(new Color(0.26f, 0.27f, 0.32f) * (0.85f + 0.15f * (y % 8) / 7f), r, 0.08f);
            });
            Basalt = L2Core.Lit("mat3_basalto", basalt, Color.white, 0.2f, 0f, L2Core.NormalMap("tex3_basalto_normal", basalt, 4f), 1.2f);

            Texture2D wet = L2Core.Tex("tex3_pedra_umida", 16, (x, y, r) =>
            {
                double v = r.NextDouble();
                if (v < 0.06) return new Color(0.55f, 0.65f, 0.75f);
                if (v < 0.2) return L2Core.Jitter(new Color(0.16f, 0.18f, 0.22f), r, 0.08f);
                return L2Core.Jitter(new Color(0.27f, 0.3f, 0.35f), r, 0.1f);
            });
            WetStone = L2Core.Lit("mat3_pedra_umida", wet, Color.white, 0.55f, 0.05f, L2Core.NormalMap("tex3_pedra_umida_normal", wet, 3f), 1f);

            Texture2D gravel = L2Core.Tex("tex3_cascalho", 16, (x, y, r) =>
            {
                double v = r.NextDouble();
                if (v < 0.25) return L2Core.Jitter(new Color(0.45f, 0.46f, 0.5f), r, 0.12f);
                if (v < 0.4) return new Color(0.12f, 0.12f, 0.14f);
                return L2Core.Jitter(new Color(0.3f, 0.29f, 0.3f), r, 0.12f);
            });
            Gravel = L2Core.Lit("mat3_cascalho", gravel, Color.white, 0.06f, 0f, L2Core.NormalMap("tex3_cascalho_normal", gravel, 3.5f), 1f);

            Texture2D brick = L2Core.Tex("tex3_tijolo_masmorra", 16, (x, y, r) =>
            {
                int row = y / 4;
                int offset = (row % 2) * 4;
                if (y % 4 == 0 || (x + offset) % 8 == 0) return L2Core.Jitter(new Color(0.13f, 0.12f, 0.13f), r, 0.1f);
                if (r.NextDouble() < 0.06) return new Color(0.2f, 0.32f, 0.3f);
                return L2Core.Jitter(new Color(0.36f, 0.33f, 0.36f) * (0.8f + 0.2f * (y % 4) / 3f), r, 0.1f);
            });
            Texture2D brickN = L2Core.NormalMap("tex3_tijolo_masmorra_normal", brick, 3.5f);
            Brick = L2Core.Lit("mat3_tijolo_masmorra", brick, Color.white, 0.07f, 0f, brickN, 1.2f);
            BrickDark = L2Core.Lit("mat3_tijolo_escuro", brick, new Color(0.6f, 0.6f, 0.68f), 0.06f, 0f, brickN, 1.2f);

            Texture2D wood = L2Core.Tex("tex3_madeira_caixa", 16, (x, y, r) =>
            {
                if (y % 8 == 0) return new Color(0.16f, 0.11f, 0.07f);
                if (x == 0 || x == 15) return new Color(0.22f, 0.16f, 0.1f);
                if ((x * 7 + y) % 13 == 0) return new Color(0.28f, 0.2f, 0.12f);
                return L2Core.Jitter(new Color(0.48f, 0.36f, 0.22f), r, 0.08f);
            });
            Texture2D woodN = L2Core.NormalMap("tex3_madeira_caixa_normal", wood, 3f);
            Wood = L2Core.Lit("mat3_madeira_caixa", wood, Color.white, 0.05f, 0f, woodN, 1f);
            WoodDark = L2Core.Lit("mat3_madeira_escura", wood, new Color(0.55f, 0.5f, 0.48f), 0.05f, 0f, woodN, 1f);

            Texture2D iron = L2Core.Tex("tex3_ferro", 16, (x, y, r) =>
            {
                double v = r.NextDouble();
                if (v < 0.15) return L2Core.Jitter(new Color(0.42f, 0.27f, 0.16f), r, 0.12f);
                return L2Core.Jitter(new Color(0.26f, 0.27f, 0.3f), r, 0.08f);
            });
            Iron = L2Core.Lit("mat3_ferro", iron, Color.white, 0.35f, 0.6f, L2Core.NormalMap("tex3_ferro_normal", iron, 2f), 0.8f);

            Rope = L2Core.Lit("mat3_corda", null, new Color(0.55f, 0.45f, 0.3f), 0.05f);
            Cloth = L2Core.Lit("mat3_pano_azul", null, new Color(0.18f, 0.25f, 0.45f), 0.05f);
            Gold = L2Core.Lit("mat3_ouro", null, new Color(1f, 0.78f, 0.3f), 0.7f, 0.85f);
            Ruby = L2Core.Emissive("mat3_rubi", null, new Color(0.9f, 0.1f, 0.2f), new Color(0.6f, 0.05f, 0.1f));
            Bone = L2Core.Lit("mat3_osso", null, new Color(0.82f, 0.78f, 0.66f), 0.2f);

            // Cristais: textura de facetas + emissão forte (o CrystalGlow faz pulsar).
            Texture2D facets = L2Core.Tex("tex3_facetas_cristal", 16, (x, y, r) =>
            {
                float band = ((x + y * 2) % 7) / 6f;
                float v = 0.65f + 0.35f * band;
                if ((x * 3 + y) % 11 == 0) v = 1f;
                return new Color(v, v, v);
            });
            CrystalBlue = Crystal("mat3_cristal_azul", facets, new Color(0.35f, 0.6f, 1f), new Color(0.25f, 0.6f, 1.6f));
            CrystalCyan = Crystal("mat3_cristal_ciano", facets, new Color(0.35f, 0.95f, 1f), new Color(0.2f, 1.3f, 1.4f));
            CrystalViolet = Crystal("mat3_cristal_violeta", facets, new Color(0.65f, 0.4f, 1f), new Color(0.8f, 0.3f, 1.6f));
            CrystalPink = Crystal("mat3_cristal_rosa", facets, new Color(1f, 0.5f, 0.8f), new Color(1.4f, 0.35f, 0.9f));
            CrystalAmber = Crystal("mat3_cristal_ambar", facets, new Color(1f, 0.7f, 0.25f), new Color(1.5f, 0.8f, 0.2f));
            CrystalCore = Crystal("mat3_cristal_nucleo", null, new Color(0.9f, 0.97f, 1f), new Color(2.2f, 2.6f, 3f));
            CrystalDark = Crystal("mat3_cristal_apagado", facets, new Color(0.25f, 0.3f, 0.45f), new Color(0.05f, 0.1f, 0.25f));

            Moss = L2Core.Emissive("mat3_musgo_luminoso", null, new Color(0.25f, 0.55f, 0.35f), new Color(0.1f, 0.6f, 0.35f));
            Mushroom = L2Core.Emissive("mat3_cogumelo_brilhante", null, new Color(0.5f, 0.35f, 0.9f), new Color(0.6f, 0.3f, 1.3f));
            MushroomStem = L2Core.Lit("mat3_talo_de_cogumelo", null, new Color(0.75f, 0.72f, 0.65f), 0.1f);

            Texture2D totem = L2Core.Tex("tex3_pedra_do_totem", 16, (x, y, r) =>
            {
                bool rune = (x % 5 == 2 && y % 6 < 4) || (y % 6 == 1 && x % 5 < 4);
                if (rune) return new Color(0.2f, 0.85f, 0.9f);
                return L2Core.Jitter(new Color(0.42f, 0.4f, 0.38f), r, 0.08f);
            });
            Texture2D totemMask = L2Core.Tex("tex3_runas_do_totem", 16, (x, y, r) =>
            {
                bool rune = (x % 5 == 2 && y % 6 < 4) || (y % 6 == 1 && x % 5 < 4);
                return rune ? Color.white : Color.black;
            });
            TotemStone = L2Core.Emissive("mat3_pedra_do_totem", totem, Color.white, new Color(0.3f, 1.4f, 1.5f), totemMask);
            TotemGlow = Crystal("mat3_brilho_do_totem", null, new Color(0.5f, 1f, 0.95f), new Color(0.6f, 2.2f, 2f));
            Rune = L2Core.Emissive("mat3_runa", totemMask, new Color(0.3f, 0.35f, 0.4f), new Color(0.3f, 1.5f, 1.6f), totemMask);

            Ghost = L2Core.Transparent("mat3_totem_fantasma", new Color(0.3f, 1f, 0.45f, 0.45f));
            Ghost.EnableKeyword("_EMISSION");
            Ghost.SetColor("_EmissionColor", new Color(0.3f, 1.2f, 0.5f));
            Marker = L2Core.Particle("mat3_marca_de_pouso", true);
            Marker.SetTexture("_BaseMap", RingTexture());
            Void = L2Core.Lit("mat3_vazio", null, new Color(0.01f, 0.01f, 0.02f), 0f);
            EditorUtility.SetDirty(Ghost);
            EditorUtility.SetDirty(Marker);
        }

        /// <summary>
        /// Material de cristal: emissivo, liso e um pouco metálico (reflexos).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="tex">Textura (facetas).</param>
        /// <param name="color">Cor base.</param>
        /// <param name="emission">Emissão (HDR).</param>
        /// <returns>O material.</returns>
        private static Material Crystal(string name, Texture2D tex, Color color, Color emission)
        {
            Material m = L2Core.Emissive(name, tex, color, emission, tex);
            m.SetFloat("_Smoothness", 0.85f);
            m.SetFloat("_Metallic", 0.2f);
            return m;
        }

        /// <summary>
        /// Rocha de caverna: manchas, veios claros e rachaduras escuras.
        /// </summary>
        /// <param name="x">X do pixel.</param>
        /// <param name="y">Y do pixel.</param>
        /// <param name="r">Gerador aleatório.</param>
        /// <returns>Cor.</returns>
        private static Color CaveRock(int x, int y, System.Random r)
        {
            if ((x * 5 + y * 3) % 17 == 0) return new Color(0.12f, 0.12f, 0.15f);
            if ((x + y * 7) % 23 == 0) return new Color(0.55f, 0.58f, 0.66f);
            double v = r.NextDouble();
            Color baseColor = new Color(0.34f, 0.35f, 0.4f) * (0.85f + 0.15f * ((x / 4 + y / 4) % 3) / 2f);
            if (v < 0.12) return baseColor * 0.7f;
            return L2Core.Jitter(baseColor, r, 0.1f);
        }

        /// <summary>
        /// Anel suave (marca de pouso no chão).
        /// </summary>
        /// <returns>A textura.</returns>
        private static Texture2D RingTexture()
        {
            const int size = 64;
            Color[] c = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.8f) / 0.12f);
                    float center = Mathf.Clamp01(1f - d / 0.2f);
                    float a = Mathf.Max(ring, center * 0.8f) * (d < 1f ? 1f : 0f);
                    c[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            return L2Core.SavePng("tex3_anel_de_pouso", c, size, size, false, true, FilterMode.Bilinear, true);
        }
    }
}
