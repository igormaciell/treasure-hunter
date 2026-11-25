using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Texturas (pixel art em PNG), normal maps e materiais do Nível 4 — as Galerias dos Ogros.
    /// Cavernas naturais escuras (rocha terrosa, terra batida, cascalho, lama), madeira velha de mineração,
    /// fungos que brilham fraco, as peles dos três ogros e os ícones do indicador de alerta.
    /// Usa as ferramentas do L2Core apontadas para a pasta Generated_Nivel4.
    /// </summary>
    public static class L4Palette
    {
        public static Material Rock, RockDark, RockMoss, Ceiling, Dirt, Gravel, Mud, FlatStone, Wall;
        public static Material Wood, WoodDark, Plank, Iron, Rope, Canvas, Bone, BoneDark;
        public static Material FungusTeal, FungusAmber, FungusStem, Moss, Fern, Roots, Lichen, CrystalViolet;
        public static Material SkinGreen, SkinGrey, SkinRed, Leather, Fur, FurDark, Tusk, EyeYellow, EyeRed, Teeth, Nail;
        public static Material ClubWood, StoneHead, LanternGlass, FireGlow, Embers, Coal, Meat, Water;
        public static Material AlertQuestion, AlertExclaim, AlertBar, AlertBarBack, Beam, Void;

        /// <summary>
        /// Cria todos os materiais.
        /// </summary>
        public static void Build()
        {
            // ---------------------------------------------------------------- Rochas e chão
            Texture2D rock = L2Core.Tex("tex4_rocha_terrosa", 16, (x, y, r) =>
            {
                if ((x * 5 + y * 3) % 19 == 0) return new Color(0.08f, 0.07f, 0.07f);
                if ((x + y * 7) % 29 == 0) return new Color(0.42f, 0.38f, 0.33f);
                Color b = new Color(0.27f, 0.25f, 0.23f) * (0.85f + 0.15f * ((x / 4 + y / 3) % 3) / 2f);
                return r.NextDouble() < 0.14 ? b * 0.7f : L2Core.Jitter(b, r, 0.1f);
            });
            Texture2D rockN = L2Core.NormalMap("tex4_rocha_terrosa_normal", rock, 4.5f);
            Rock = L2Core.Lit("mat4_rocha", rock, Color.white, 0.1f, 0f, rockN, 1.4f);
            RockDark = L2Core.Lit("mat4_rocha_escura", rock, new Color(0.6f, 0.58f, 0.6f), 0.08f, 0f, rockN, 1.4f);
            Ceiling = L2Core.Lit("mat4_teto", rock, new Color(0.4f, 0.38f, 0.4f), 0.06f, 0f, rockN, 1.1f);
            Wall = L2Core.Lit("mat4_parede_da_caverna", rock, new Color(0.75f, 0.72f, 0.72f), 0.12f, 0f, rockN, 1.5f);

            Texture2D mossRock = L2Core.Tex("tex4_rocha_musgo", 16, (x, y, r) =>
            {
                double v = r.NextDouble();
                if (y > 9 && v < 0.55) return L2Core.Jitter(new Color(0.18f, 0.3f, 0.16f), r, 0.15f);
                if (v < 0.1) return new Color(0.1f, 0.09f, 0.08f);
                return L2Core.Jitter(new Color(0.27f, 0.26f, 0.23f), r, 0.1f);
            });
            RockMoss = L2Core.Lit("mat4_rocha_com_musgo", mossRock, Color.white, 0.12f, 0f, L2Core.NormalMap("tex4_rocha_musgo_normal", mossRock, 4f), 1.2f);

            Texture2D dirt = L2Core.Tex("tex4_terra_batida", 16, (x, y, r) =>
            {
                double v = r.NextDouble();
                if (v < 0.08) return new Color(0.1f, 0.08f, 0.06f);
                if (v < 0.16) return new Color(0.33f, 0.28f, 0.22f);
                return L2Core.Jitter(new Color(0.22f, 0.18f, 0.14f), r, 0.08f);
            });
            Dirt = L2Core.Lit("mat4_terra_batida", dirt, Color.white, 0.05f, 0f, L2Core.NormalMap("tex4_terra_batida_normal", dirt, 2.5f), 1f);

            Texture2D gravel = L2Core.Tex("tex4_cascalho_solto", 16, (x, y, r) =>
            {
                double v = r.NextDouble();
                if (v < 0.3) return L2Core.Jitter(new Color(0.46f, 0.43f, 0.38f), r, 0.15f);
                if (v < 0.45) return new Color(0.1f, 0.09f, 0.08f);
                return L2Core.Jitter(new Color(0.29f, 0.27f, 0.24f), r, 0.12f);
            });
            Gravel = L2Core.Lit("mat4_cascalho_solto", gravel, Color.white, 0.06f, 0f, L2Core.NormalMap("tex4_cascalho_solto_normal", gravel, 4.5f), 1.2f);

            Texture2D mud = L2Core.Tex("tex4_lama", 16, (x, y, r) =>
            {
                double v = r.NextDouble();
                if (v < 0.06) return new Color(0.35f, 0.4f, 0.42f);
                return L2Core.Jitter(new Color(0.15f, 0.12f, 0.1f), r, 0.1f);
            });
            Mud = L2Core.Lit("mat4_lama", mud, Color.white, 0.55f, 0f, L2Core.NormalMap("tex4_lama_normal", mud, 2f), 0.8f);

            Texture2D slab = L2Core.Tex("tex4_laje", 16, (x, y, r) =>
            {
                if (x % 8 == 0 || (y + (x / 8) * 5) % 9 == 0) return new Color(0.12f, 0.11f, 0.1f);
                return L2Core.Jitter(new Color(0.32f, 0.3f, 0.28f), r, 0.08f);
            });
            FlatStone = L2Core.Lit("mat4_laje", slab, Color.white, 0.1f, 0f, L2Core.NormalMap("tex4_laje_normal", slab, 3.5f), 1.2f);

            // ---------------------------------------------------------------- Madeira, ferro, ossos
            Texture2D wood = L2Core.Tex("tex4_madeira_velha", 16, (x, y, r) =>
            {
                if (y % 5 == 0) return new Color(0.12f, 0.08f, 0.05f);
                if ((x * 3 + y) % 11 == 0) return new Color(0.2f, 0.14f, 0.09f);
                if (r.NextDouble() < 0.05) return new Color(0.18f, 0.24f, 0.14f);
                return L2Core.Jitter(new Color(0.38f, 0.28f, 0.18f), r, 0.1f);
            });
            Texture2D woodN = L2Core.NormalMap("tex4_madeira_velha_normal", wood, 3f);
            Wood = L2Core.Lit("mat4_madeira_velha", wood, Color.white, 0.05f, 0f, woodN, 1.1f);
            WoodDark = L2Core.Lit("mat4_madeira_escura", wood, new Color(0.55f, 0.48f, 0.45f), 0.05f, 0f, woodN, 1.1f);
            Plank = L2Core.Lit("mat4_tabua", wood, new Color(0.85f, 0.78f, 0.68f), 0.05f, 0f, woodN, 1f);

            Texture2D iron = L2Core.Tex("tex4_ferro_enferrujado", 16, (x, y, r) =>
            {
                double v = r.NextDouble();
                if (v < 0.3) return L2Core.Jitter(new Color(0.45f, 0.25f, 0.12f), r, 0.15f);
                return L2Core.Jitter(new Color(0.24f, 0.23f, 0.24f), r, 0.08f);
            });
            Iron = L2Core.Lit("mat4_ferro", iron, Color.white, 0.3f, 0.55f, L2Core.NormalMap("tex4_ferro_normal", iron, 2f), 0.8f);
            Rope = L2Core.Lit("mat4_corda", null, new Color(0.5f, 0.4f, 0.26f), 0.05f);
            Canvas = L2Core.Lit("mat4_lona", null, new Color(0.42f, 0.38f, 0.3f), 0.05f);
            Bone = L2Core.Lit("mat4_osso", null, new Color(0.78f, 0.74f, 0.62f), 0.2f);
            BoneDark = L2Core.Lit("mat4_osso_velho", null, new Color(0.55f, 0.5f, 0.4f), 0.15f);

            // ---------------------------------------------------------------- Vegetação subterrânea
            FungusTeal = L2Core.Emissive("mat4_fungo_turquesa", null, new Color(0.25f, 0.6f, 0.6f), new Color(0.1f, 0.75f, 0.7f));
            FungusAmber = L2Core.Emissive("mat4_fungo_ambar", null, new Color(0.7f, 0.45f, 0.2f), new Color(0.8f, 0.4f, 0.08f));
            FungusStem = L2Core.Lit("mat4_talo_de_fungo", null, new Color(0.62f, 0.58f, 0.5f), 0.1f);
            Moss = L2Core.Emissive("mat4_musgo_brilhante", null, new Color(0.18f, 0.35f, 0.2f), new Color(0.05f, 0.25f, 0.12f));
            Texture2D fern = L2Core.Tex("tex4_samambaia", 16, (x, y, r) =>
            {
                bool vein = x % 4 == 0 || (x + y) % 6 == 0;
                return vein ? new Color(0.1f, 0.2f, 0.1f) : L2Core.Jitter(new Color(0.16f, 0.3f, 0.18f), r, 0.15f);
            });
            Fern = L2Core.Lit("mat4_samambaia", fern, Color.white, 0.15f, 0f, L2Core.NormalMap("tex4_samambaia_normal", fern, 2f), 0.6f);
            Roots = L2Core.Lit("mat4_raizes", null, new Color(0.3f, 0.22f, 0.16f), 0.05f);
            Lichen = L2Core.Emissive("mat4_liquen", null, new Color(0.45f, 0.5f, 0.3f), new Color(0.1f, 0.15f, 0.04f));
            CrystalViolet = L2Core.Emissive("mat4_cristal_fraco", null, new Color(0.45f, 0.3f, 0.7f), new Color(0.5f, 0.2f, 0.9f));
            CrystalViolet.SetFloat("_Smoothness", 0.8f);

            // ---------------------------------------------------------------- Ogros
            SkinGreen = Skin("mat4_pele_verde", new Color(0.36f, 0.45f, 0.24f));
            SkinGrey = Skin("mat4_pele_cinza", new Color(0.42f, 0.42f, 0.4f));
            SkinRed = Skin("mat4_pele_avermelhada", new Color(0.55f, 0.32f, 0.24f));
            Texture2D leather = L2Core.Tex("tex4_couro", 16, (x, y, r) =>
            {
                if (y % 6 == 0 && x % 3 != 0) return new Color(0.12f, 0.08f, 0.05f);
                return L2Core.Jitter(new Color(0.34f, 0.23f, 0.14f), r, 0.12f);
            });
            Leather = L2Core.Lit("mat4_couro", leather, Color.white, 0.15f, 0f, L2Core.NormalMap("tex4_couro_normal", leather, 2.5f), 1f);
            Texture2D fur = L2Core.Tex("tex4_pelo", 16, (x, y, r) =>
            {
                bool strand = (x + y * 2) % 3 == 0;
                return L2Core.Jitter(strand ? new Color(0.3f, 0.24f, 0.18f) : new Color(0.42f, 0.34f, 0.25f), r, 0.15f);
            });
            Texture2D furN = L2Core.NormalMap("tex4_pelo_normal", fur, 3f);
            Fur = L2Core.Lit("mat4_pelo", fur, Color.white, 0.05f, 0f, furN, 1.2f);
            FurDark = L2Core.Lit("mat4_pelo_escuro", fur, new Color(0.45f, 0.42f, 0.45f), 0.05f, 0f, furN, 1.2f);
            Tusk = L2Core.Lit("mat4_presa", null, new Color(0.88f, 0.84f, 0.7f), 0.35f);
            Teeth = L2Core.Lit("mat4_dentes", null, new Color(0.8f, 0.76f, 0.58f), 0.3f);
            Nail = L2Core.Lit("mat4_unhas", null, new Color(0.25f, 0.22f, 0.18f), 0.3f);
            EyeYellow = L2Core.Emissive("mat4_olho_amarelo", null, new Color(1f, 0.85f, 0.3f), new Color(1.6f, 1.1f, 0.2f));
            EyeRed = L2Core.Emissive("mat4_olho_vermelho", null, new Color(1f, 0.3f, 0.2f), new Color(1.8f, 0.3f, 0.15f));
            ClubWood = L2Core.Lit("mat4_clava", wood, new Color(0.7f, 0.6f, 0.5f), 0.05f, 0f, woodN, 1.3f);
            StoneHead = L2Core.Lit("mat4_cabeca_de_pedra", rock, new Color(0.8f, 0.8f, 0.85f), 0.12f, 0f, rockN, 1.5f);
            LanternGlass = L2Core.Emissive("mat4_lampiao_aceso", null, new Color(1f, 0.75f, 0.4f), new Color(2.2f, 1.3f, 0.4f));

            // ---------------------------------------------------------------- Fogo e acampamento
            FireGlow = L2Core.Emissive("mat4_fogo", null, new Color(1f, 0.55f, 0.15f), new Color(3f, 1.2f, 0.2f));
            Embers = L2Core.Emissive("mat4_brasas", null, new Color(0.6f, 0.2f, 0.05f), new Color(1.6f, 0.35f, 0.05f));
            Coal = L2Core.Lit("mat4_carvao", null, new Color(0.08f, 0.07f, 0.07f), 0.05f);
            Meat = L2Core.Lit("mat4_carne", null, new Color(0.5f, 0.2f, 0.15f), 0.35f);
            Water = L2Core.Emissive("mat4_poca_brilhante", null, new Color(0.15f, 0.35f, 0.4f), new Color(0.05f, 0.35f, 0.4f));
            Water.SetFloat("_Smoothness", 0.95f);

            // ---------------------------------------------------------------- Indicador de alerta e efeitos
            AlertQuestion = Icon("mat4_alerta_interrogacao", "tex4_icone_interrogacao", new[]
            {
                "..XXXXXX..",
                ".XXXXXXXX.",
                "XXX....XXX",
                "XX.....XXX",
                "......XXX.",
                ".....XXX..",
                "....XXX...",
                "....XX....",
                "....XX....",
                "..........",
                "....XX....",
                "....XX...."
            });
            AlertExclaim = Icon("mat4_alerta_exclamacao", "tex4_icone_exclamacao", new[]
            {
                "...XXXX...",
                "...XXXX...",
                "...XXXX...",
                "...XXXX...",
                "....XX....",
                "....XX....",
                "....XX....",
                "....XX....",
                "..........",
                "...XXXX...",
                "...XXXX...",
                ".........."
            });
            Color[] white = new Color[16];
            for (int i = 0; i < white.Length; i++) white[i] = Color.white;
            Texture2D solid = L2Core.SavePng("tex4_branco", white, 4, 4, false, true, FilterMode.Point, true);
            AlertBar = L2Core.Particle("mat4_barra_de_alerta", false);
            AlertBar.SetTexture("_BaseMap", solid);
            AlertBarBack = L2Core.Particle("mat4_fundo_da_barra", false);
            AlertBarBack.SetTexture("_BaseMap", solid);
            Beam = L2Core.Particle("mat4_feixe_de_luz", true);
            Beam.SetColor("_BaseColor", new Color(0.55f, 0.7f, 0.85f, 0.18f));
            Void = L2Core.Lit("mat4_escuridao", null, new Color(0.01f, 0.01f, 0.01f), 0f);
            foreach (Material m in new[] { AlertBar, AlertBarBack, Beam }) EditorUtility.SetDirty(m);
        }

        /// <summary>
        /// Pele de ogro: base da cor dada, verrugas e manchas (com normal map para o relevo).
        /// </summary>
        /// <param name="name">Nome.</param>
        /// <param name="tone">Tom.</param>
        /// <returns>O material.</returns>
        private static Material Skin(string name, Color tone)
        {
            Texture2D t = L2Core.Tex("tex4_" + name.Substring(5), 16, (x, y, r) =>
            {
                double v = r.NextDouble();
                if ((x * 7 + y * 5) % 23 == 0) return tone * 1.25f;
                if (v < 0.12) return tone * 0.75f;
                return L2Core.Jitter(tone, r, 0.07f);
            });
            return L2Core.Lit(name, t, Color.white, 0.25f, 0f, L2Core.NormalMap("tex4_" + name.Substring(5) + "_normal", t, 3f), 0.9f);
        }

        /// <summary>
        /// Ícone sem fundo (indicador "?" / "!" sobre os ogros), material de partícula colorido pelo script.
        /// </summary>
        /// <param name="matName">Material.</param>
        /// <param name="texName">Textura.</param>
        /// <param name="rows">Desenho (X = preenchido).</param>
        /// <returns>O material.</returns>
        private static Material Icon(string matName, string texName, string[] rows)
        {
            int h = rows.Length, w = rows[0].Length;
            const int pad = 3;
            int W = w + pad * 2, H = h + pad * 2;
            int size = Mathf.Max(W, H);
            Color[] c = new Color[size * size];
            int ox = (size - w) / 2, oy = (size - h) / 2;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (rows[h - 1 - y][w - 1 - x] != 'X') continue;
                    // (O desenho é espelhado: as faces das caixas voxel mostram a textura invertida na horizontal.)
            // Contorno escuro (1 pixel) para ler bem sobre qualquer fundo.
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int px = ox + x + dx, py = oy + y + dy;
                            int i = py * size + px;
                            if (c[i].a < 0.5f) c[i] = new Color(0.05f, 0.03f, 0.02f, 1f);
                        }
                }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (rows[h - 1 - y][w - 1 - x] == 'X') c[(oy + y) * size + ox + x] = Color.white;
            Texture2D tex = L2Core.SavePng(texName, c, size, size, false, true, FilterMode.Point, true);
            Material m = L2Core.Particle(matName, false);
            m.SetTexture("_BaseMap", tex);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
