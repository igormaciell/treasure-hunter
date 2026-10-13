using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Paleta de materiais do Nível 1 (verdes e marrons, pedra desgastada, raízes e ouro),
    /// criada a partir das texturas em pixel art.
    /// </summary>
    public static class Palette
    {
        public static Material StoneBrick, TempleBrick, StoneFloor, StoneDark, Rock, Dirt, Grass, Leaves, LeavesLight, LeavesDark;
        public static Material Bark, Wood, WoodDark, Gold, Jade, Ruby, Sapphire, Emerald, Bone, Clay, ClothRed, ClothTan, Iron, Rope;
        public static Material Vine, Roots, MushroomRed, MushroomBrown, Stem, FlowerYellow, FlowerPurple, Paper, Leather;
        public static Material Skin, Hat, Shirt, Pants, Boots, Belt, Eyes, Hair, Backpack;
        public static Material FireGlow, Coals, IdolGlow, CandleFlame, HeartGlow, Void, CrystalBlue;
        public static Material[] Glyph, GlyphLit;

        /// <summary>
        /// Cria todas as texturas e materiais da paleta.
        /// </summary>
        public static void Build()
        {
            StoneBrick = MaterialFactory.Lit("mat_stone_brick", TextureFactory.Bricks("tex_stone_brick", new Color(0.46f, 0.47f, 0.42f), new Color(0.26f, 0.26f, 0.23f), 0.18f), Color.white);
            TempleBrick = MaterialFactory.Lit("mat_temple_brick", TextureFactory.Bricks("tex_temple_brick", new Color(0.68f, 0.62f, 0.5f), new Color(0.42f, 0.37f, 0.29f), 0.28f), Color.white);
            StoneDark = MaterialFactory.Lit("mat_stone_dark", TextureFactory.Bricks("tex_stone_dark", new Color(0.32f, 0.31f, 0.3f), new Color(0.16f, 0.16f, 0.15f), 0.06f), Color.white);
            StoneFloor = MaterialFactory.Lit("mat_stone_floor", TextureFactory.Slabs("tex_stone_floor", new Color(0.44f, 0.42f, 0.38f)), Color.white);
            Rock = MaterialFactory.Lit("mat_rock", TextureFactory.Noise("tex_rock", new Color(0.5f, 0.49f, 0.46f), new Color(0.37f, 0.36f, 0.34f), 0.4f), Color.white);
            Dirt = MaterialFactory.Lit("mat_dirt", TextureFactory.Noise("tex_dirt", new Color(0.44f, 0.31f, 0.2f), new Color(0.35f, 0.24f, 0.15f), 0.4f), Color.white);
            Grass = MaterialFactory.Lit("mat_grass", TextureFactory.Noise("tex_grass", new Color(0.34f, 0.55f, 0.22f), new Color(0.27f, 0.45f, 0.17f), 0.35f), Color.white);
            Leaves = MaterialFactory.Lit("mat_leaves", TextureFactory.Noise("tex_leaves", new Color(0.2f, 0.45f, 0.15f), new Color(0.13f, 0.33f, 0.1f), 0.4f), Color.white);
            LeavesLight = MaterialFactory.Lit("mat_leaves_light", TextureFactory.Noise("tex_leaves_light", new Color(0.38f, 0.6f, 0.2f), new Color(0.29f, 0.5f, 0.15f), 0.4f), Color.white);
            LeavesDark = MaterialFactory.Lit("mat_leaves_dark", TextureFactory.Noise("tex_leaves_dark", new Color(0.12f, 0.32f, 0.16f), new Color(0.08f, 0.24f, 0.12f), 0.4f), Color.white);
            Bark = MaterialFactory.Lit("mat_bark", TextureFactory.Bark("tex_bark", new Color(0.4f, 0.28f, 0.17f)), Color.white);
            Wood = MaterialFactory.Lit("mat_wood", TextureFactory.Planks("tex_wood", new Color(0.58f, 0.4f, 0.23f)), Color.white);
            WoodDark = MaterialFactory.Lit("mat_wood_dark", TextureFactory.Planks("tex_wood_dark", new Color(0.34f, 0.23f, 0.14f)), Color.white);
            Gold = MaterialFactory.Lit("mat_gold", TextureFactory.Shiny("tex_gold", new Color(0.95f, 0.74f, 0.2f)), Color.white, 0.65f, 0.85f);
            Jade = MaterialFactory.Lit("mat_jade", TextureFactory.Shiny("tex_jade", new Color(0.25f, 0.72f, 0.5f)), Color.white, 0.75f);
            Ruby = MaterialFactory.Emissive("mat_ruby", TextureFactory.Shiny("tex_ruby", new Color(0.85f, 0.1f, 0.15f)), Color.white, new Color(0.4f, 0.02f, 0.04f));
            Sapphire = MaterialFactory.Emissive("mat_sapphire", TextureFactory.Shiny("tex_sapphire", new Color(0.15f, 0.35f, 0.95f)), Color.white, new Color(0.03f, 0.08f, 0.45f));
            Emerald = MaterialFactory.Emissive("mat_emerald", TextureFactory.Shiny("tex_emerald", new Color(0.1f, 0.8f, 0.35f)), Color.white, new Color(0.02f, 0.35f, 0.1f));
            Bone = MaterialFactory.Lit("mat_bone", TextureFactory.Noise("tex_bone", new Color(0.9f, 0.87f, 0.78f), new Color(0.8f, 0.76f, 0.67f), 0.3f), Color.white);
            Clay = MaterialFactory.Lit("mat_clay", TextureFactory.Noise("tex_clay", new Color(0.66f, 0.38f, 0.22f), new Color(0.56f, 0.31f, 0.18f), 0.3f), Color.white);
            ClothRed = MaterialFactory.Lit("mat_cloth_red", TextureFactory.Noise("tex_cloth_red", new Color(0.62f, 0.15f, 0.12f), new Color(0.52f, 0.12f, 0.1f), 0.3f), Color.white);
            ClothTan = MaterialFactory.Lit("mat_cloth_tan", TextureFactory.Noise("tex_cloth_tan", new Color(0.76f, 0.66f, 0.46f), new Color(0.68f, 0.58f, 0.4f), 0.3f), Color.white);
            Iron = MaterialFactory.Lit("mat_iron", TextureFactory.Noise("tex_iron", new Color(0.27f, 0.27f, 0.29f), new Color(0.2f, 0.2f, 0.22f), 0.4f), Color.white, 0.4f, 0.6f);
            Rope = MaterialFactory.Lit("mat_rope", TextureFactory.Noise("tex_rope", new Color(0.72f, 0.6f, 0.39f), new Color(0.62f, 0.5f, 0.32f), 0.5f), Color.white);
            Vine = MaterialFactory.Lit("mat_vine", TextureFactory.Noise("tex_vine", new Color(0.18f, 0.42f, 0.12f), new Color(0.26f, 0.52f, 0.16f), 0.45f), Color.white);
            Roots = MaterialFactory.Lit("mat_roots", TextureFactory.Bark("tex_roots", new Color(0.3f, 0.2f, 0.12f)), Color.white);
            MushroomRed = MaterialFactory.Lit("mat_mushroom_red", TextureFactory.Noise("tex_mushroom_red", new Color(0.8f, 0.14f, 0.1f), new Color(0.95f, 0.93f, 0.88f), 0.14f), Color.white);
            MushroomBrown = MaterialFactory.Lit("mat_mushroom_brown", TextureFactory.Noise("tex_mushroom_brown", new Color(0.55f, 0.38f, 0.22f), new Color(0.65f, 0.48f, 0.3f), 0.3f), Color.white);
            Stem = MaterialFactory.Lit("mat_stem", null, new Color(0.9f, 0.86f, 0.72f));
            FlowerYellow = MaterialFactory.Lit("mat_flower_yellow", null, new Color(0.98f, 0.82f, 0.2f));
            FlowerPurple = MaterialFactory.Lit("mat_flower_purple", null, new Color(0.62f, 0.3f, 0.8f));
            Paper = MaterialFactory.Lit("mat_paper", null, new Color(0.9f, 0.86f, 0.72f));
            Leather = MaterialFactory.Lit("mat_leather", TextureFactory.Noise("tex_leather", new Color(0.42f, 0.24f, 0.13f), new Color(0.36f, 0.2f, 0.1f), 0.3f), Color.white);
            Skin = MaterialFactory.Lit("mat_skin", null, new Color(0.86f, 0.66f, 0.5f));
            Hat = MaterialFactory.Lit("mat_hat", TextureFactory.Noise("tex_hat", new Color(0.46f, 0.31f, 0.17f), new Color(0.4f, 0.27f, 0.14f), 0.3f), Color.white);
            Shirt = MaterialFactory.Lit("mat_shirt", TextureFactory.Noise("tex_shirt", new Color(0.74f, 0.66f, 0.44f), new Color(0.68f, 0.6f, 0.4f), 0.3f), Color.white);
            Pants = MaterialFactory.Lit("mat_pants", TextureFactory.Noise("tex_pants", new Color(0.36f, 0.28f, 0.19f), new Color(0.32f, 0.25f, 0.17f), 0.3f), Color.white);
            Boots = MaterialFactory.Lit("mat_boots", null, new Color(0.22f, 0.15f, 0.1f));
            Belt = MaterialFactory.Lit("mat_belt", null, new Color(0.28f, 0.18f, 0.1f));
            Eyes = MaterialFactory.Lit("mat_eyes", null, new Color(0.08f, 0.08f, 0.1f));
            Hair = MaterialFactory.Lit("mat_hair", null, new Color(0.26f, 0.16f, 0.08f));
            Backpack = MaterialFactory.Lit("mat_backpack", TextureFactory.Noise("tex_backpack", new Color(0.42f, 0.38f, 0.22f), new Color(0.36f, 0.32f, 0.18f), 0.3f), Color.white);
            FireGlow = MaterialFactory.Emissive("mat_fire_glow", null, new Color(1f, 0.55f, 0.15f), new Color(2.5f, 1.1f, 0.2f));
            Coals = MaterialFactory.Emissive("mat_coals", TextureFactory.Noise("tex_coals", new Color(0.2f, 0.08f, 0.05f), new Color(0.9f, 0.35f, 0.08f), 0.35f), Color.white, new Color(1.2f, 0.35f, 0.05f), TextureFactory.Noise("tex_coals_mask", Color.black, Color.white, 0.35f));
            IdolGlow = MaterialFactory.Emissive("mat_idol", TextureFactory.Shiny("tex_idol", new Color(1f, 0.78f, 0.22f)), Color.white, new Color(0.6f, 0.4f, 0.05f));
            CandleFlame = MaterialFactory.Emissive("mat_candle_flame", null, new Color(1f, 0.85f, 0.4f), new Color(3f, 2f, 0.6f));
            HeartGlow = MaterialFactory.Emissive("mat_heart", null, new Color(0.95f, 0.15f, 0.2f), new Color(0.9f, 0.05f, 0.1f));
            CrystalBlue = MaterialFactory.Emissive("mat_crystal_blue", null, new Color(0.5f, 0.85f, 1f), new Color(0.3f, 0.9f, 1.6f));
            Void = MaterialFactory.Lit("mat_void", null, new Color(0.02f, 0.02f, 0.02f));

            // Símbolos antigos: versão apagada (entalhe) e acesa (brilho dourado).
            int n = TextureFactory.GlyphNames.Length;
            Glyph = new Material[n];
            GlyphLit = new Material[n];
            for (int i = 0; i < n; i++)
            {
                Texture2D stone = TextureFactory.GlyphStone(i);
                Texture2D mask = TextureFactory.GlyphMask(i);
                Glyph[i] = MaterialFactory.Lit("mat_glyph_" + i, stone, Color.white);
                GlyphLit[i] = MaterialFactory.Emissive("mat_glyph_lit_" + i, stone, Color.white, new Color(2.2f, 1.5f, 0.4f), mask);
            }
        }
    }
}
