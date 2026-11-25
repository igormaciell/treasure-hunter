using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Paleta do Nível 2 — as catacumbas: pedra envelhecida e rachada, piso partido, madeira podre,
    /// ferro enferrujado, ossos, panos sujos, metal do revólver e brilhos (chamas, olhos dos esqueletos,
    /// fungos e runas). As superfícies principais têm mapa normal gerado a partir da textura.
    /// </summary>
    public static class L2Palette
    {
        public static Material Stone, StoneDark, Ceiling, Floor, Dirt, Wood, WoodDark, Iron, IronDark;
        public static Material Bone, BoneDark, Cloth, ClothRed, ClothGreen, Leather, Rope;
        public static Material Gunmetal, Grip, Brass, Blade, Straw, Sarcophagus, Runes, Gold, Ruby, Rock, BarrelRed;
        public static Material Cobweb, Wax, Flame, Embers, Fungus, EyeRed, EyeGreen, EyeOrange, Void, CrackLight, Feather, FeatherRed, Paper;

        private static readonly Color StoneBase = new Color(0.36f, 0.34f, 0.31f);

        /// <summary>
        /// Cria todas as texturas (PNG em pixel art), mapas normais e materiais do Nível 2.
        /// </summary>
        public static void Build()
        {
            Texture2D stone = Bricks("tex2_pedra_velha", StoneBase, new Color(0.17f, 0.16f, 0.15f), 0.07f, 0.35f);
            Stone = L2Core.Lit("mat2_pedra_velha", stone, Color.white, 0.06f, 0f, L2Core.NormalMap("tex2_pedra_velha_normal", stone, 3.5f), 1.2f);
            Texture2D dark = Bricks("tex2_pedra_escura", new Color(0.24f, 0.23f, 0.22f), new Color(0.1f, 0.1f, 0.1f), 0.05f, 0.25f);
            Texture2D darkN = L2Core.NormalMap("tex2_pedra_escura_normal", dark, 3.5f);
            StoneDark = L2Core.Lit("mat2_pedra_escura", dark, Color.white, 0.05f, 0f, darkN, 1.2f);
            Ceiling = L2Core.Lit("mat2_teto", dark, new Color(0.7f, 0.7f, 0.74f), 0.04f, 0f, darkN, 1f);

            Texture2D floor = CrackedSlabs("tex2_piso_rachado", new Color(0.33f, 0.31f, 0.28f));
            Floor = L2Core.Lit("mat2_piso_rachado", floor, Color.white, 0.08f, 0f, L2Core.NormalMap("tex2_piso_rachado_normal", floor, 3f), 1.1f);
            Texture2D dirt = L2Core.Tex("tex2_piso_terra", 16, (x, y, r) =>
            {
                double v = r.NextDouble();
                if (v < 0.12) return L2Core.Jitter(new Color(0.42f, 0.4f, 0.37f), r, 0.12f);
                if (v < 0.2) return new Color(0.14f, 0.11f, 0.09f);
                return L2Core.Jitter(new Color(0.27f, 0.21f, 0.16f), r, 0.12f);
            });
            Dirt = L2Core.Lit("mat2_piso_terra", dirt, Color.white, 0.03f, 0f, L2Core.NormalMap("tex2_piso_terra_normal", dirt, 2.5f), 1f);

            Texture2D wood = RottenPlanks("tex2_madeira_podre", new Color(0.36f, 0.27f, 0.18f));
            Texture2D woodN = L2Core.NormalMap("tex2_madeira_podre_normal", wood, 3f);
            Wood = L2Core.Lit("mat2_madeira_podre", wood, Color.white, 0.04f, 0f, woodN, 1f);
            WoodDark = L2Core.Lit("mat2_madeira_escura", wood, new Color(0.6f, 0.55f, 0.5f), 0.04f, 0f, woodN, 1f);

            Texture2D iron = L2Core.Tex("tex2_ferro_enferrujado", 16, (x, y, r) =>
            {
                double v = r.NextDouble();
                if (v < 0.22) return L2Core.Jitter(new Color(0.48f, 0.24f, 0.1f), r, 0.15f);
                if (v < 0.3) return L2Core.Jitter(new Color(0.62f, 0.33f, 0.14f), r, 0.1f);
                return L2Core.Jitter(new Color(0.24f, 0.24f, 0.26f), r, 0.1f);
            });
            Texture2D ironN = L2Core.NormalMap("tex2_ferro_enferrujado_normal", iron, 2f);
            Iron = L2Core.Lit("mat2_ferro_enferrujado", iron, Color.white, 0.3f, 0.5f, ironN, 0.8f);
            IronDark = L2Core.Lit("mat2_ferro_escuro", iron, new Color(0.55f, 0.55f, 0.6f), 0.35f, 0.6f, ironN, 0.8f);

            Bone = L2Core.Lit("mat2_osso", Noise("tex2_osso", new Color(0.86f, 0.82f, 0.7f), new Color(0.74f, 0.69f, 0.56f), 0.3f), Color.white, 0.2f);
            BoneDark = L2Core.Lit("mat2_osso_velho", Noise("tex2_osso_velho", new Color(0.66f, 0.6f, 0.46f), new Color(0.5f, 0.45f, 0.33f), 0.35f), Color.white, 0.15f);
            Cloth = L2Core.Lit("mat2_pano_sujo", Noise("tex2_pano_sujo", new Color(0.33f, 0.3f, 0.26f), new Color(0.25f, 0.22f, 0.19f), 0.4f), Color.white);
            ClothRed = L2Core.Lit("mat2_pano_vermelho", L2Core.Tex("tex2_pano_vermelho", 16, (x, y, r) =>
            {
                if (r.NextDouble() < 0.06) return new Color(0.08f, 0.04f, 0.04f);
                return L2Core.Jitter(y % 5 == 0 ? new Color(0.62f, 0.5f, 0.22f) : new Color(0.46f, 0.08f, 0.07f), r, 0.12f);
            }), Color.white);
            ClothGreen = L2Core.Lit("mat2_pano_verde", Noise("tex2_pano_verde", new Color(0.17f, 0.24f, 0.15f), new Color(0.12f, 0.17f, 0.1f), 0.4f), Color.white);
            Leather = L2Core.Lit("mat2_couro", Noise("tex2_couro", new Color(0.36f, 0.21f, 0.12f), new Color(0.29f, 0.16f, 0.09f), 0.35f), Color.white, 0.2f);
            Rope = L2Core.Lit("mat2_corda", Noise("tex2_corda", new Color(0.62f, 0.52f, 0.34f), new Color(0.5f, 0.41f, 0.26f), 0.5f), Color.white);

            Gunmetal = L2Core.Lit("mat2_metal_revolver", Noise("tex2_metal_revolver", new Color(0.2f, 0.21f, 0.24f), new Color(0.32f, 0.33f, 0.37f), 0.2f), Color.white, 0.6f, 0.85f);
            Grip = L2Core.Lit("mat2_cabo_madeira", Planks("tex2_cabo_madeira", new Color(0.4f, 0.22f, 0.12f)), Color.white, 0.35f);
            Brass = L2Core.Lit("mat2_latao", Noise("tex2_latao", new Color(0.82f, 0.64f, 0.28f), new Color(0.95f, 0.82f, 0.45f), 0.25f), Color.white, 0.65f, 0.9f);
            Blade = L2Core.Lit("mat2_lamina", Noise("tex2_lamina", new Color(0.7f, 0.72f, 0.74f), new Color(0.85f, 0.87f, 0.9f), 0.3f), Color.white, 0.75f, 0.9f);
            Straw = L2Core.Lit("mat2_palha", L2Core.Tex("tex2_palha", 16, (x, y, r) =>
                L2Core.Jitter((x + y / 3) % 3 == 0 ? new Color(0.62f, 0.5f, 0.22f) : new Color(0.82f, 0.7f, 0.36f), r, 0.12f)), Color.white);

            Texture2D sarc = L2Core.Tex("tex2_sarcofago", 16, (x, y, r) =>
            {
                Color b = new Color(0.47f, 0.44f, 0.39f);
                bool border = x == 1 || x == 14 || y == 1 || y == 14;
                bool motif = (x - 8) * (x - 8) + (y - 8) * (y - 8) < 10 && (x + y) % 2 == 0;
                if (border || motif) return b * 0.6f;
                return L2Core.Jitter(b, r, 0.08f);
            });
            Sarcophagus = L2Core.Lit("mat2_sarcofago", sarc, Color.white, 0.1f, 0f, L2Core.NormalMap("tex2_sarcofago_normal", sarc, 4f), 1.3f);

            Texture2D runes = L2Core.Tex("tex2_runas", 16, (x, y, r) => RunePixel(x, y) ? new Color(0.12f, 0.1f, 0.09f) : L2Core.Jitter(new Color(0.3f, 0.29f, 0.27f), r, 0.08f), true);
            Texture2D runesMask = L2Core.Tex("tex2_runas_mascara", 16, (x, y, r) => RunePixel(x, y) ? Color.white : Color.black, true);
            Runes = L2Core.Emissive("mat2_runas", runes, Color.white, new Color(1.6f, 0.35f, 0.2f), runesMask);

            Gold = L2Core.Emissive("mat2_ouro", Noise("tex2_ouro", new Color(0.92f, 0.72f, 0.22f), new Color(1f, 0.9f, 0.5f), 0.2f), Color.white, new Color(0.25f, 0.17f, 0.02f));
            Ruby = L2Core.Emissive("mat2_rubi", null, new Color(0.8f, 0.1f, 0.14f), new Color(0.6f, 0.03f, 0.05f));
            Texture2D rock = Noise("tex2_rocha", new Color(0.42f, 0.4f, 0.37f), new Color(0.3f, 0.29f, 0.27f), 0.45f);
            Rock = L2Core.Lit("mat2_rocha", rock, Color.white, 0.05f, 0f, L2Core.NormalMap("tex2_rocha_normal", rock, 3f), 1f);
            BarrelRed = L2Core.Lit("mat2_barril_explosivo", Planks("tex2_barril_explosivo", new Color(0.55f, 0.14f, 0.1f)), Color.white);

            Cobweb = L2Core.Transparent("mat2_teia", new Color(0.88f, 0.88f, 0.84f, 0.55f));
            Wax = L2Core.Lit("mat2_cera", null, new Color(0.88f, 0.83f, 0.7f), 0.3f);
            Paper = L2Core.Lit("mat2_pergaminho", Noise("tex2_pergaminho", new Color(0.78f, 0.7f, 0.52f), new Color(0.66f, 0.58f, 0.42f), 0.3f), Color.white);
            Flame = L2Core.Emissive("mat2_chama", null, new Color(1f, 0.6f, 0.2f), new Color(3f, 1.4f, 0.3f));
            Embers = L2Core.Emissive("mat2_brasas", Noise("tex2_brasas", new Color(0.18f, 0.07f, 0.04f), new Color(0.85f, 0.32f, 0.06f), 0.35f), Color.white, new Color(1.1f, 0.32f, 0.05f));
            Fungus = L2Core.Emissive("mat2_fungo_luminoso", null, new Color(0.35f, 0.85f, 0.8f), new Color(0.25f, 1.4f, 1.2f));
            EyeRed = L2Core.Emissive("mat2_olho_vermelho", null, new Color(1f, 0.2f, 0.1f), new Color(4f, 0.6f, 0.2f));
            EyeGreen = L2Core.Emissive("mat2_olho_verde", null, new Color(0.3f, 1f, 0.3f), new Color(0.6f, 4f, 0.6f));
            EyeOrange = L2Core.Emissive("mat2_olho_laranja", null, new Color(1f, 0.6f, 0.1f), new Color(4f, 2f, 0.2f));
            Void = L2Core.Lit("mat2_vazio", null, new Color(0.02f, 0.02f, 0.02f));
            CrackLight = L2Core.Emissive("mat2_luz_da_fenda", null, new Color(0.7f, 0.8f, 1f), new Color(1.4f, 1.7f, 2.2f));
            Feather = L2Core.Lit("mat2_pena", null, new Color(0.85f, 0.83f, 0.78f));
            FeatherRed = L2Core.Lit("mat2_pena_vermelha", null, new Color(0.6f, 0.12f, 0.1f));
        }

        // ================================================================== Desenhos das texturas

        /// <summary>
        /// Tijolos de pedra envelhecida: argamassa, rachaduras, lascas e sujeira mais escura perto do chão.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="baseColor">Cor da pedra.</param>
        /// <param name="mortar">Cor da argamassa.</param>
        /// <param name="crack">Chance de pixel de rachadura.</param>
        /// <param name="grime">Quanto escurece embaixo.</param>
        /// <returns>A textura.</returns>
        private static Texture2D Bricks(string name, Color baseColor, Color mortar, float crack, float grime)
        {
            return L2Core.Tex(name, 16, (x, y, r) =>
            {
                int row = y / 4;
                int offset = (row % 2) * 4;
                if (y % 4 == 0 || (x + offset) % 8 == 0) return L2Core.Jitter(mortar, r, 0.12f);
                // Rachaduras diagonais em alguns blocos.
                if (((x * 3 + y * 5 + row * 7) % 23 == 0) || r.NextDouble() < crack) return baseColor * 0.45f;
                if (r.NextDouble() < 0.05) return baseColor * 1.25f;
                float shade = 1f - grime * (1f - y / 15f);
                return L2Core.Jitter(baseColor * shade, r, 0.1f);
            });
        }

        /// <summary>
        /// Lajes do piso (8×8) com rachaduras e terra nas juntas.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="stone">Cor da laje.</param>
        /// <returns>A textura.</returns>
        private static Texture2D CrackedSlabs(string name, Color stone)
        {
            return L2Core.Tex(name, 16, (x, y, r) =>
            {
                if (x % 8 == 0 || y % 8 == 0) return L2Core.Jitter(new Color(0.2f, 0.16f, 0.12f), r, 0.15f);
                int sx = x / 8, sy = y / 8;
                // Uma rachadura diagonal em duas das quatro lajes.
                if ((sx + sy) % 2 == 0 && (x % 8) == (y % 8)) return stone * 0.5f;
                if (r.NextDouble() < 0.05) return stone * 0.75f;
                return L2Core.Jitter(stone, r, 0.08f);
            });
        }

        /// <summary>
        /// Tábuas de madeira podre com frestas e buracos.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="wood">Cor da madeira.</param>
        /// <returns>A textura.</returns>
        private static Texture2D RottenPlanks(string name, Color wood)
        {
            return L2Core.Tex(name, 16, (x, y, r) =>
            {
                if (y % 4 == 0) return wood * 0.45f;
                if (r.NextDouble() < 0.04) return new Color(0.05f, 0.04f, 0.03f);
                if ((x + (y / 4) * 5) % 16 == 0) return wood * 0.6f;
                Color c = L2Core.Jitter(wood, r, 0.1f);
                return r.NextDouble() < 0.12 ? Color.Lerp(c, new Color(0.22f, 0.26f, 0.16f), 0.4f) : c;
            });
        }

        /// <summary>
        /// Tábuas simples (cabo do revólver, barril explosivo).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="wood">Cor.</param>
        /// <returns>A textura.</returns>
        private static Texture2D Planks(string name, Color wood)
        {
            return L2Core.Tex(name, 16, (x, y, r) =>
            {
                if (y % 4 == 0) return wood * 0.6f;
                return L2Core.Jitter(wood, r, 0.08f);
            });
        }

        /// <summary>
        /// Ruído simples de duas cores.
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="a">Cor principal.</param>
        /// <param name="b">Cor secundária.</param>
        /// <param name="mix">Chance da cor secundária.</param>
        /// <returns>A textura.</returns>
        private static Texture2D Noise(string name, Color a, Color b, float mix)
        {
            return L2Core.Tex(name, 16, (x, y, r) => L2Core.Jitter(r.NextDouble() < mix ? b : a, r, 0.1f));
        }

        /// <summary>
        /// Desenho das runas de aviso (traços angulosos).
        /// </summary>
        /// <param name="x">Coluna.</param>
        /// <param name="y">Linha.</param>
        /// <returns>true se o pixel é parte de uma runa.</returns>
        private static bool RunePixel(int x, int y)
        {
            if (y < 3 || y > 12) return false;
            int col = x / 4, lx = x % 4;
            if (lx == 3) return false;
            switch (col)
            {
                case 0: return lx == 1 || (y == 8 && lx != 1);
                case 1: return lx == 0 || (lx == y % 3 && y > 6);
                case 2: return (lx == 1 && y > 5) || (y == 12) || (y == 5 && lx != 1);
                default: return lx == 2 || (lx == 1 && (y == 3 || y == 9));
            }
        }
    }
}
