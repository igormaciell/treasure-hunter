using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Cenário do Nível 4 (cavernas naturais): rochas e formações, escombros, caixas e restos de mineração,
    /// acampamento dos ogros, vegetação subterrânea (inclusive esconderijos), fontes de luz pequenas,
    /// tochas/braseiros que podem ser apagados e as peças da entrada e da saída.
    /// </summary>
    public static class L4Props
    {
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        /// <summary>
        /// Cria todos os prefabs do cenário.
        /// </summary>
        public static void BuildAll()
        {
            BuildRocks();
            BuildRubble();
            BuildCrates();
            BuildCamp();
            BuildVegetation();
            BuildLights();
            BuildLevelPieces();
        }

        // ================================================================== Auxiliares

        /// <summary>
        /// Rocha irregular (três caixas giradas, duas tonalidades).
        /// </summary>
        /// <param name="m">Modelo.</param>
        /// <param name="c">Centro.</param>
        /// <param name="s">Tamanho.</param>
        /// <param name="seed">Variação.</param>
        /// <param name="a">Material principal.</param>
        /// <param name="b">Material secundário.</param>
        /// <returns>O modelo.</returns>
        public static VoxelModel Rock(VoxelModel m, Vector3 c, float s, int seed, Material a, Material b)
        {
            m.Add(c, V(s, s * 0.78f, s * 0.92f), a, V(seed * 7 % 18, seed * 37 % 90, seed * 13 % 14));
            m.Add(c + V(s * 0.2f, s * 0.14f, -s * 0.12f), V(s * 0.7f, s * 0.66f, s * 0.72f), a, V(22 + seed * 3, 35 + seed * 11, 12));
            m.Add(c + V(-s * 0.24f, -s * 0.08f, s * 0.16f), V(s * 0.6f, s * 0.52f, s * 0.6f), b, V(8, 60 + seed * 7, 26));
            return m;
        }

        /// <summary>
        /// Colisor de caixa um pouco menor que o modelo (rochas giradas extrapolam os limites).
        /// </summary>
        /// <param name="go">Objeto.</param>
        /// <param name="model">Modelo.</param>
        /// <param name="shrink">Fator.</param>
        private static void Shrunk(GameObject go, VoxelModel model, float shrink = 0.85f)
        {
            Bounds b = model.GetBounds();
            Vector3 size = new Vector3(b.size.x * shrink, b.size.y * 0.95f, b.size.z * shrink);
            L2Core.Box(go, new Vector3(b.center.x, size.y * 0.5f + Mathf.Max(0f, b.min.y), b.center.z), size);
        }

        /// <summary>
        /// Prefab de rocha com colisor ajustado.
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="display">Nome legível.</param>
        /// <param name="m">Modelo.</param>
        /// <param name="cat">Categoria.</param>
        private static void Solid(string id, string display, VoxelModel m, AssetCategoryType cat)
        {
            GameObject go = L2Core.Begin(id, m);
            Shrunk(go, m);
            go.tag = StealthTags.Cover;
            L2Core.Save(go, cat, display);
        }

        /// <summary>
        /// Camada de interativos.
        /// </summary>
        private static int InteractableLayer => L2Core.Layer(CombatLayers.InteractableLayerName);

        // ================================================================== Rochas e formações

        /// <summary>
        /// Rochas P/M/G (coberturas baixa, média e alta), rochedo, pilares, estalagmites, estalactites,
        /// laje caída (vão seguro por baixo), parede irregular, arco, veio de cristal e pedregulhos.
        /// </summary>
        private static void BuildRocks()
        {
            const AssetCategoryType C = AssetCategoryType.Arquitetura;
            Solid("Rocha_Escura_P", "Rocha baixa (cobertura agachado)", Rock(new VoxelModel(), V(0, 0.42f, 0), 1.05f, 2, L4Palette.Rock, L4Palette.RockDark), C);
            Solid("Rocha_Escura_M", "Rocha média (cobertura)", Rock(new VoxelModel(), V(0, 0.75f, 0), 1.8f, 5, L4Palette.Rock, L4Palette.RockMoss), C);
            Solid("Rocha_Escura_G", "Rocha grande (cobertura alta)", Rock(new VoxelModel(), V(0, 1.15f, 0), 2.8f, 9, L4Palette.RockDark, L4Palette.Rock), C);

            VoxelModel big = new VoxelModel();
            Rock(big, V(0, 1.4f, 0), 3.6f, 3, L4Palette.Rock, L4Palette.RockDark);
            Rock(big, V(1.2f, 2.6f, 0.3f), 2.4f, 6, L4Palette.RockDark, L4Palette.RockMoss);
            Rock(big, V(-1.3f, 1f, -0.6f), 2.2f, 8, L4Palette.RockMoss, L4Palette.Rock);
            Rock(big, V(0.4f, 0.6f, 1.4f), 1.8f, 11, L4Palette.Rock, L4Palette.RockDark);
            GameObject rochedo = L2Core.Begin("Rochedo", big);
            L2Core.Box(rochedo, V(0, 1.8f, 0), V(4f, 3.6f, 3.8f));
            rochedo.tag = StealthTags.Cover;
            L2Core.Save(rochedo, C, "Rochedo (grande formação)");

            Pillar("Pilar_de_Rocha", "Pilar de rocha (chão ao teto)", 8.5f, 1.9f);
            Pillar("Coluna_Grossa", "Coluna grossa de rocha", 10.5f, 2.8f);

            float[] mite = { 1.2f, 2.4f, 4f };
            string[] n = { "P", "M", "G" };
            for (int i = 0; i < 3; i++)
            {
                float h = mite[i];
                VoxelModel s = new VoxelModel();
                L3Props.Spike(s, Vector3.zero, h, h * 0.42f, V(0, i * 20, 3), L4Palette.Rock);
                L3Props.Spike(s, V(h * 0.22f, 0, 0.1f), h * 0.45f, h * 0.22f, V(0, 10, -18), L4Palette.RockDark);
                GameObject g = L2Core.Begin("Estalagmite_Escura_" + n[i], s);
                if (i > 0) L2Core.Box(g, V(0, h * 0.4f, 0), V(h * 0.38f, h * 0.8f, h * 0.38f));
                L2Core.Save(g, C, "Estalagmite escura " + (i == 0 ? "pequena" : i == 1 ? "média" : "grande"));

                VoxelModel t = new VoxelModel();
                L3Props.HangingSpike(t, Vector3.zero, h * 1.2f, h * 0.36f, V(0, i * 15, 4), L4Palette.RockDark);
                L3Props.HangingSpike(t, V(h * 0.2f, 0, -0.1f), h * 0.5f, h * 0.2f, V(0, 30, -14), L4Palette.Rock);
                L2Core.Simple("Estalactite_Escura_" + n[i], t, C, "Estalactite escura " + (i == 0 ? "pequena" : i == 1 ? "média" : "grande"), false);
            }

            // Laje caída: vão de 1,2 m por baixo (só agachado; os ogros não cabem) — área segura.
            VoxelModel slab = new VoxelModel()
                .Add(V(0, 1.55f, 0), V(4.2f, 0.5f, 3f), L4Palette.FlatStone, V(0, 0, 4))
                .Add(V(0.4f, 1.85f, -0.2f), V(3f, 0.3f, 2.2f), L4Palette.RockDark, V(0, 12, 3));
            Rock(slab, V(-1.9f, 0.6f, 0), 1.3f, 4, L4Palette.Rock, L4Palette.RockDark);
            Rock(slab, V(2f, 0.6f, 0.2f), 1.25f, 7, L4Palette.RockDark, L4Palette.Rock);
            GameObject laje = L2Core.Begin("Laje_Caida", slab);
            L2Core.Box(laje, V(0, 1.6f, 0), V(4.2f, 0.6f, 3f));
            L2Core.Box(laje, V(-2f, 0.6f, 0), V(1f, 1.2f, 2.6f));
            L2Core.Box(laje, V(2.05f, 0.6f, 0.2f), V(1f, 1.2f, 2.6f));
            GameObject under = new GameObject("VaoSeguro");
            under.transform.SetParent(laje.transform, false);
            under.layer = 2;
            L2Core.Box(under, V(0, 0.6f, 0), V(2.8f, 1.2f, 2.8f), true);
            SafeZone sz = under.AddComponent<SafeZone>();
            sz.label = "Vão sob a laje";
            laje.tag = StealthTags.Cover;
            L2Core.Save(laje, C, "Laje caída (vão seguro)");

            VoxelModel wall = new VoxelModel();
            for (int i = 0; i < 5; i++)
            {
                float y = 0.8f + i * 1.2f;
                wall.Add(V(((i * 37) % 5 - 2) * 0.12f, y, ((i * 13) % 3 - 1) * 0.15f), V(4.2f - (i % 2) * 0.6f, 1.5f, 1.2f), i % 2 == 0 ? L4Palette.Wall : L4Palette.RockDark, V((i * 7) % 9 - 4, (i * 23) % 14 - 7, (i * 11) % 8 - 4));
            }
            wall.Add(V(1.2f, 1.4f, 0.45f), V(1.2f, 1.6f, 0.6f), L4Palette.RockMoss, V(8, 25, 10));
            wall.Add(V(-1.3f, 3.6f, 0.4f), V(1f, 1.2f, 0.5f), L4Palette.Rock, V(-6, -20, 15));
            GameObject pw = L2Core.Begin("Parede_Irregular", wall);
            L2Core.Box(pw, V(0, 3.2f, 0), V(4f, 6.4f, 1f));
            L2Core.Save(pw, C, "Parede rochosa irregular");

            VoxelModel arch = new VoxelModel();
            Rock(arch, V(-2.1f, 1.2f, 0), 1.8f, 1, L4Palette.Rock, L4Palette.RockDark);
            Rock(arch, V(-2f, 2.9f, 0), 1.5f, 2, L4Palette.RockDark, L4Palette.Rock);
            Rock(arch, V(2.1f, 1.2f, 0), 1.8f, 3, L4Palette.Rock, L4Palette.RockMoss);
            Rock(arch, V(2f, 2.9f, 0), 1.5f, 4, L4Palette.Rock, L4Palette.RockDark);
            arch.Add(V(0, 4.1f, 0), V(5.6f, 1.2f, 1.6f), L4Palette.RockDark, V(0, 0, 3));
            GameObject ar = L2Core.Begin("Arco_de_Rocha", arch);
            L2Core.Box(ar, V(-2.1f, 1.9f, 0), V(1.4f, 3.8f, 1.4f));
            L2Core.Box(ar, V(2.1f, 1.9f, 0), V(1.4f, 3.8f, 1.4f));
            L2Core.Box(ar, V(0, 4.1f, 0), V(5.6f, 1.2f, 1.5f));
            L2Core.Save(ar, C, "Arco natural de rocha");

            VoxelModel vein = new VoxelModel().Add(V(0, 0.5f, -0.05f), V(1.4f, 1f, 0.1f), L4Palette.RockDark);
            L3Props.Spike(vein, V(-0.3f, 0.4f, 0f), 0.5f, 0.12f, V(70, 0, 15), L4Palette.CrystalViolet);
            L3Props.Spike(vein, V(0.2f, 0.6f, 0f), 0.4f, 0.1f, V(65, 0, -20), L4Palette.CrystalViolet);
            L3Props.Spike(vein, V(0.4f, 0.3f, 0f), 0.3f, 0.08f, V(80, 0, 5), L4Palette.CrystalViolet);
            GameObject vg = L2Core.Begin("Veio_de_Cristal", vein);
            L2Core.PointLight(vg, "Luz", V(0, 0.5f, 0.5f), new Color(0.6f, 0.35f, 1f), 3f, 0.6f, false, LightmapBakeType.Mixed);
            L2Core.Save(vg, AssetCategoryType.Iluminacao, "Veio de cristal (luz fraca)");

            VoxelModel pebbles = new VoxelModel();
            for (int i = 0; i < 9; i++)
            {
                float s = 0.1f + (i * 37 % 10) / 10f * 0.18f;
                pebbles.Add(V(Mathf.Cos(i * 2.1f) * 0.9f, s * 0.35f, Mathf.Sin(i * 2.1f) * 0.7f), V(s, s * 0.7f, s * 1.1f), i % 3 == 0 ? L4Palette.RockDark : L4Palette.Rock, V(i * 17, i * 41, 0));
            }
            L2Core.Simple("Pedregulhos_Soltos", pebbles, C, "Pedregulhos soltos", false);
        }

        /// <summary>
        /// Coluna de rocha do chão ao teto (blocos irregulares empilhados).
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="display">Nome.</param>
        /// <param name="height">Altura.</param>
        /// <param name="width">Largura média.</param>
        private static void Pillar(string id, string display, float height, float width)
        {
            VoxelModel m = new VoxelModel();
            int parts = Mathf.CeilToInt(height / 1.8f);
            for (int i = 0; i < parts; i++)
            {
                float w = width * (i == 0 || i == parts - 1 ? 1.35f : 0.85f + 0.15f * ((i * 7) % 3));
                m.Add(V(((i * 13) % 3 - 1) * 0.1f, i * 1.8f + 0.9f, 0), V(w, 1.9f, w * 0.9f), i % 2 == 0 ? L4Palette.Rock : L4Palette.RockDark, V(0, i * 31, ((i * 5) % 5 - 2) * 2));
            }
            GameObject go = L2Core.Begin(id, m);
            L2Core.Box(go, V(0, height * 0.5f, 0), V(width * 0.95f, height, width * 0.85f));
            go.tag = StealthTags.Cover;
            L2Core.Save(go, AssetCategoryType.Arquitetura, display);
        }

        // ================================================================== Escombros

        /// <summary>
        /// Pilhas de escombros (alturas diferentes para cobertura), cascalho solto e ossos espalhados
        /// (chão barulhento).
        /// </summary>
        private static void BuildRubble()
        {
            const AssetCategoryType P = AssetCategoryType.Props;
            float[] sizes = { 1.5f, 2.2f, 3f };
            float[] heights = { 0.95f, 1.4f, 1.9f };
            string[] n = { "P", "M", "G" };
            for (int i = 0; i < 3; i++)
            {
                float s = sizes[i], h = heights[i];
                VoxelModel m = new VoxelModel();
                for (int k = 0; k < 6 + i * 2; k++)
                {
                    float a = k * 2.4f;
                    float r = (k % 3) * 0.18f * s;
                    float bs = s * (0.28f + (k * 7 % 5) * 0.05f);
                    float y = Mathf.Max(bs * 0.35f, h * (1f - r / s) * 0.6f);
                    m.Add(V(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r * 0.8f), V(bs, bs * 0.7f, bs * 0.85f), k % 2 == 0 ? L4Palette.Rock : L4Palette.RockDark, V(k * 19, k * 47, k * 11 % 25));
                }
                m.Add(V(0, h * 0.7f, 0), V(s * 0.45f, h * 0.5f, s * 0.4f), L4Palette.RockDark, V(10, 30, 5));
                if (i == 2) m.Add(V(0.3f, h * 0.65f, 0.1f), V(2.6f, 0.22f, 0.24f), L4Palette.WoodDark, V(0, 35, 18));
                GameObject go = L2Core.Begin("Escombros_" + n[i], m);
                L2Core.Box(go, V(0, h * 0.5f, 0), V(s * 0.8f, h, s * 0.65f));
                go.tag = StealthTags.Cover;
                L2Core.Save(go, P, "Escombros " + (i == 0 ? "baixos" : i == 1 ? "médios" : "altos") + " (cobertura)");
            }

            NoisyPatch("Cascalho_Solto", "Cascalho solto (chão barulhento)", L4Palette.Gravel, L4Palette.Rock, "Cascalho solto",
                L2Core.Clips("gravel_step_1", "gravel_step_2"));
            NoisyPatch("Ossos_Espalhados", "Ossos espalhados (chão barulhento)", L4Palette.Bone, L4Palette.BoneDark, "Ossos espalhados",
                L2Core.Clips("bone_crunch_1", "bone_crunch_2"));
        }

        /// <summary>
        /// Mancha de chão barulhento (visual + gatilho NoisyFloor).
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="display">Nome.</param>
        /// <param name="a">Material A.</param>
        /// <param name="b">Material B.</param>
        /// <param name="label">Texto no HUD.</param>
        /// <param name="clips">Sons dos passos.</param>
        private static void NoisyPatch(string id, string display, Material a, Material b, string label, AudioClip[] clips)
        {
            VoxelModel m = new VoxelModel().Add(V(0, 0.02f, 0), V(3f, 0.04f, 3f), a, V(0, 12, 0));
            for (int k = 0; k < 14; k++)
            {
                float ang = k * 2.3f, r = 0.3f + (k % 4) * 0.3f;
                float s = 0.08f + (k * 7 % 5) * 0.03f;
                m.Add(V(Mathf.Cos(ang) * r, s * 0.4f, Mathf.Sin(ang) * r), V(s * (a == L4Palette.Bone ? 2.5f : 1f), s * 0.6f, s), k % 2 == 0 ? a : b, V(k * 13, k * 59, k * 7));
            }
            GameObject go = L2Core.Begin(id, m);
            L2Core.Box(go, V(0, 0.6f, 0), V(3f, 1.2f, 3f), true);
            NoisyFloor nf = go.AddComponent<NoisyFloor>();
            nf.label = label;
            nf.stepClips = clips;
            nf.stepEffects = new[] { L2Core.Get("FX4_Cascalho") };
            go.layer = 2;
            L2Core.Save(go, AssetCategoryType.Props, display);
        }

        // ================================================================== Caixas e mineração

        /// <summary>
        /// Caixa de madeira velha com cintas.
        /// </summary>
        /// <param name="s">Lado.</param>
        /// <returns>Modelo.</returns>
        private static VoxelModel Crate(float s)
        {
            return new VoxelModel()
                .Add(V(0, s * 0.5f, 0), V(s, s, s), L4Palette.Wood)
                .Add(V(0, s * 0.5f, 0), V(s * 1.02f, s * 0.12f, s * 1.02f), L4Palette.Iron)
                .Add(V(0, s * 0.92f, 0), V(s * 1.02f, s * 0.08f, s * 1.02f), L4Palette.WoodDark)
                .Add(V(0, s * 0.5f, s * 0.511f), V(s * 0.1f, s * 0.95f, 0.02f), L4Palette.WoodDark, V(0, 0, 45));
        }

        /// <summary>
        /// Caixas, pilha, caixa quebrada, barril, carrinho de mina, trilhos, tábuas, andaime e ferramentas.
        /// </summary>
        private static void BuildCrates()
        {
            const AssetCategoryType P = AssetCategoryType.Props;
            CrateProp("Caixa_Velha", "Caixa velha (cobertura baixa)", Crate(1.1f), 1.1f);
            CrateProp("Caixa_Velha_Grande", "Caixa velha grande (cobertura)", Crate(1.6f), 1.6f);

            VoxelModel stack = Crate(1.2f);
            stack.Add(V(1.15f, 0.55f, 0.1f), V(1.1f, 1.1f, 1.1f), L4Palette.WoodDark, V(0, 10, 0))
                 .Add(V(0.3f, 1.75f, 0.05f), V(1.2f, 1.1f, 1.1f), L4Palette.Wood, V(0, -8, 0))
                 .Add(V(0.3f, 2.2f, 0.05f), V(1.22f, 0.1f, 1.12f), L4Palette.Iron, V(0, -8, 0));
            GameObject st = L2Core.Begin("Pilha_de_Caixas_Velhas", stack);
            L2Core.Box(st, V(0.55f, 1.15f, 0.05f), V(2.3f, 2.3f, 1.2f));
            st.tag = StealthTags.Cover;
            L2Core.Save(st, P, "Pilha de caixas velhas (cobertura alta)");

            VoxelModel broken = new VoxelModel()
                .Add(V(0, 0.08f, 0), V(1.2f, 0.16f, 1.2f), L4Palette.WoodDark)
                .Add(V(-0.55f, 0.35f, 0), V(0.1f, 0.7f, 1.1f), L4Palette.Wood, V(0, 0, 8))
                .Add(V(0, 0.25f, 0.55f), V(1.1f, 0.5f, 0.1f), L4Palette.Wood, V(-12, 0, 0))
                .Add(V(0.6f, 0.1f, -0.3f), V(0.9f, 0.08f, 0.15f), L4Palette.Plank, V(0, 40, 5))
                .Add(V(0.2f, 0.12f, 0.2f), V(0.25f, 0.2f, 0.25f), L4Palette.Bone);
            CrateProp("Caixa_Quebrada", "Caixa quebrada", broken, 0.6f);

            GameObject barrel = L2Core.Begin("Barril_Velho", new VoxelModel()
                .Add(V(0, 0.55f, 0), V(0.8f, 1.1f, 0.65f), L4Palette.Wood)
                .Add(V(0, 0.55f, 0), V(0.65f, 1.1f, 0.8f), L4Palette.Wood)
                .Add(V(0, 0.22f, 0), V(0.85f, 0.07f, 0.85f), L4Palette.Iron)
                .Add(V(0, 0.88f, 0), V(0.85f, 0.07f, 0.85f), L4Palette.Iron));
            L2Core.Box(barrel, V(0, 0.55f, 0), V(0.8f, 1.1f, 0.8f));
            barrel.tag = StealthTags.Cover;
            L2Core.Save(barrel, P, "Barril velho");

            VoxelModel cart = new VoxelModel()
                .Add(V(0, 0.85f, 0), V(1.2f, 0.7f, 1.8f), L4Palette.Iron)
                .Add(V(0, 1.22f, 0), V(1.26f, 0.06f, 1.86f), L4Palette.WoodDark)
                .Add(V(0, 1.1f, 0), V(1f, 0.3f, 1.6f), L4Palette.Rock, V(0, 0, 3))
                .Add(V(0, 0.45f, 0), V(0.9f, 0.12f, 1.6f), L4Palette.WoodDark);
            foreach (float x in new[] { -0.55f, 0.55f })
                foreach (float z in new[] { -0.55f, 0.55f })
                    cart.Add(V(x, 0.28f, z), V(0.1f, 0.5f, 0.5f), L4Palette.Iron);
            GameObject ct = L2Core.Begin("Carrinho_de_Mina", cart);
            L2Core.Box(ct, V(0, 0.7f, 0), V(1.25f, 1.4f, 1.85f));
            ct.tag = StealthTags.Cover;
            L2Core.Save(ct, P, "Carrinho de mina (cobertura)");

            VoxelModel rails = new VoxelModel();
            rails.Add(V(-0.45f, 0.06f, 0), V(0.08f, 0.1f, 4f), L4Palette.Iron).Add(V(0.45f, 0.06f, 0), V(0.08f, 0.1f, 4f), L4Palette.Iron);
            for (int i = 0; i < 6; i++) rails.Add(V(0, 0.03f, -1.75f + i * 0.7f), V(1.3f, 0.06f, 0.22f), L4Palette.WoodDark, V(0, (i % 2) * 4 - 2, 0));
            L2Core.Simple("Trilhos", rails, P, "Trilhos de mina", false);

            VoxelModel planks = new VoxelModel();
            for (int i = 0; i < 5; i++) planks.Add(V(((i * 7) % 3 - 1) * 0.1f, 0.08f + i * 0.14f, 0), V(2.4f, 0.12f, 0.35f), i % 2 == 0 ? L4Palette.Plank : L4Palette.Wood, V(0, (i * 13) % 20 - 10, 0));
            CrateProp("Tabuas_Empilhadas", "Tábuas empilhadas", planks, 0.8f);

            VoxelModel scaffold = new VoxelModel();
            foreach (float x in new[] { -1.2f, 1.2f })
                scaffold.Add(V(x, 1.5f, 0), V(0.22f, 3f, 0.22f), L4Palette.WoodDark);
            scaffold.Add(V(0, 2.9f, 0), V(2.8f, 0.2f, 0.25f), L4Palette.Wood)
                    .Add(V(0, 1.6f, 0), V(2.6f, 0.18f, 0.2f), L4Palette.Wood, V(0, 0, 32))
                    .Add(V(-0.3f, 2.2f, 0.4f), V(1.8f, 0.12f, 0.9f), L4Palette.Plank, V(0, 0, -12))
                    .Add(V(1.5f, 0.15f, 0.6f), V(1.4f, 0.15f, 0.3f), L4Palette.Plank, V(0, 30, 5));
            GameObject sc = L2Core.Begin("Andaime_Quebrado", scaffold);
            L2Core.Box(sc, V(-1.2f, 1.5f, 0), V(0.3f, 3f, 0.3f));
            L2Core.Box(sc, V(1.2f, 1.5f, 0), V(0.3f, 3f, 0.3f));
            L2Core.Save(sc, P, "Andaime quebrado dos mineiros");

            VoxelModel tools = new VoxelModel()
                .Add(V(0, 0.7f, 0), V(0.07f, 1.4f, 0.07f), L4Palette.WoodDark, V(0, 0, 12))
                .Add(V(0.14f, 1.38f, 0), V(0.6f, 0.08f, 0.08f), L4Palette.Iron, V(0, 0, 12))
                .Add(V(0.4f, 0.6f, 0.1f), V(0.07f, 1.2f, 0.07f), L4Palette.WoodDark, V(0, 0, -15))
                .Add(V(0.24f, 0.08f, 0.1f), V(0.3f, 0.35f, 0.04f), L4Palette.Iron, V(0, 0, -15));
            L2Core.Simple("Picareta_e_Pa", tools, P, "Picareta e pá", false);
        }

        /// <summary>
        /// Caixa/obstáculo com colisor e tag de cobertura.
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="display">Nome.</param>
        /// <param name="m">Modelo.</param>
        /// <param name="h">Altura do colisor.</param>
        private static void CrateProp(string id, string display, VoxelModel m, float h)
        {
            GameObject go = L2Core.Begin(id, m);
            Bounds b = m.GetBounds();
            L2Core.Box(go, V(b.center.x, h * 0.5f, b.center.z), V(b.size.x, h, b.size.z));
            go.tag = StealthTags.Cover;
            L2Core.Save(go, AssetCategoryType.Props, display);
        }

        // ================================================================== Acampamento dos ogros

        /// <summary>
        /// Fogueira, ossos, crânio gigante, espeto de carne, caldeirão, peles, trono de pedra, totem de ossos,
        /// correntes, tocha de parede e braseiro (estes dois podem ser apagados).
        /// </summary>
        private static void BuildCamp()
        {
            const AssetCategoryType P = AssetCategoryType.Props;

            // Fogueira (luz Mixed tremulante: a área mais iluminada do covil).
            VoxelModel fire = new VoxelModel();
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f;
                fire.Add(V(Mathf.Cos(a) * 1.05f, 0.18f, Mathf.Sin(a) * 1.05f), V(0.42f, 0.36f, 0.38f), i % 2 == 0 ? L4Palette.Rock : L4Palette.RockDark, V(0, i * 40, 8));
            }
            fire.Add(V(0, 0.08f, 0), V(1.5f, 0.12f, 1.5f), L4Palette.Coal)
                .Add(V(0, 0.2f, 0), V(1.5f, 0.18f, 0.22f), L4Palette.WoodDark, V(0, 30, 10))
                .Add(V(0, 0.22f, 0), V(1.4f, 0.18f, 0.22f), L4Palette.WoodDark, V(0, -40, -8))
                .Add(V(0, 0.18f, 0), V(0.9f, 0.1f, 0.9f), L4Palette.Embers);
            GameObject fg = L2Core.Begin("Fogueira_dos_Ogros", fire);
            L2Core.Box(fg, V(0, 0.3f, 0), V(2.4f, 0.6f, 2.4f));
            L2Core.AddFx(fg, "FX4_Fogueira", V(0, 0.25f, 0));
            L2Core.AddFx(fg, "FX4_Brasas", V(0, 0.5f, 0));
            L2Core.AddFx(fg, "FX4_Fumaca", V(0, 1.4f, 0));
            L2Core.PointLight(fg, "LuzDaFogueira", V(0, 1.2f, 0), new Color(1f, 0.55f, 0.25f), 12f, 2.6f, true, LightmapBakeType.Mixed);
            L2Core.Save(fg, AssetCategoryType.Iluminacao, "Fogueira dos ogros");

            VoxelModel bones = new VoxelModel();
            for (int i = 0; i < 12; i++)
            {
                float a = i * 1.7f, r = (i % 4) * 0.22f;
                bones.Add(V(Mathf.Cos(a) * r, 0.1f + (3 - i % 4) * 0.13f, Mathf.Sin(a) * r), V(0.7f, 0.1f, 0.1f), i % 3 == 0 ? L4Palette.BoneDark : L4Palette.Bone, V(i * 23, i * 67, i * 31));
            }
            bones.Add(V(0.1f, 0.65f, 0), V(0.3f, 0.26f, 0.32f), L4Palette.Bone).Add(V(0.1f, 0.62f, 0.165f), V(0.2f, 0.06f, 0.01f), L4Palette.Void);
            GameObject bp = L2Core.Begin("Pilha_de_Ossos", bones);
            L2Core.Box(bp, V(0, 0.45f, 0), V(1.4f, 0.9f, 1.4f));
            L2Core.Save(bp, P, "Pilha de ossos");

            VoxelModel skull = new VoxelModel()
                .Add(V(0, 0.6f, 0), V(1.1f, 0.9f, 1.2f), L4Palette.Bone)
                .Add(V(0, 0.25f, 0.4f), V(0.9f, 0.4f, 0.6f), L4Palette.Bone)
                .Add(V(-0.25f, 0.68f, 0.601f), V(0.28f, 0.26f, 0.02f), L4Palette.Void)
                .Add(V(0.25f, 0.68f, 0.601f), V(0.28f, 0.26f, 0.02f), L4Palette.Void)
                .Add(V(0, 0.15f, 0.71f), V(0.6f, 0.1f, 0.02f), L4Palette.BoneDark)
                .Add(V(-0.62f, 0.95f, 0), V(0.3f, 0.12f, 0.12f), L4Palette.Tusk, V(0, 0, 30))
                .Add(V(0.62f, 0.95f, 0), V(0.3f, 0.12f, 0.12f), L4Palette.Tusk, V(0, 0, -30));
            CrateProp("Cranio_Gigante", "Crânio gigante", skull, 1.1f);

            VoxelModel rack = new VoxelModel()
                .Add(V(-1f, 0.9f, 0), V(0.12f, 1.8f, 0.12f), L4Palette.WoodDark, V(0, 0, -8))
                .Add(V(1f, 0.9f, 0), V(0.12f, 1.8f, 0.12f), L4Palette.WoodDark, V(0, 0, 8))
                .Add(V(0, 1.7f, 0), V(2.3f, 0.1f, 0.1f), L4Palette.Wood)
                .Add(V(-0.45f, 1.35f, 0), V(0.35f, 0.6f, 0.3f), L4Palette.Meat, V(0, 0, 8))
                .Add(V(0.35f, 1.3f, 0), V(0.4f, 0.7f, 0.32f), L4Palette.Meat, V(0, 20, -6))
                .Add(V(0.8f, 1.45f, 0), V(0.18f, 0.4f, 0.16f), L4Palette.Bone);
            GameObject rk = L2Core.Begin("Espeto_de_Carne", rack);
            L2Core.Box(rk, V(0, 0.9f, 0), V(2.2f, 1.8f, 0.4f));
            L2Core.Save(rk, P, "Espeto de carne dos ogros");

            VoxelModel pot = new VoxelModel()
                .Add(V(0, 0.65f, 0), V(1.3f, 0.9f, 1.3f), L4Palette.Iron)
                .Add(V(0, 0.65f, 0), V(1.45f, 0.7f, 1.1f), L4Palette.Iron)
                .Add(V(0, 1.08f, 0), V(1.2f, 0.06f, 1.2f), L4Palette.Mud)
                .Add(V(0, 0.15f, 0), V(1f, 0.3f, 1f), L4Palette.Coal)
                .Add(V(0.5f, 1.3f, 0.2f), V(0.08f, 0.9f, 0.08f), L4Palette.WoodDark, V(20, 0, -25));
            CrateProp("Caldeirao", "Caldeirão dos ogros", pot, 1.15f);

            VoxelModel furs = new VoxelModel()
                .Add(V(0, 0.08f, 0), V(2.4f, 0.16f, 1.6f), L4Palette.Fur, V(0, 8, 0))
                .Add(V(0.3f, 0.18f, 0.1f), V(1.6f, 0.12f, 1.1f), L4Palette.FurDark, V(0, -14, 0))
                .Add(V(-0.9f, 0.25f, 0), V(0.5f, 0.3f, 0.9f), L4Palette.Fur);
            L2Core.Simple("Peles_de_Dormir", furs, P, "Peles de dormir", false);

            VoxelModel throne = new VoxelModel();
            Rock(throne, V(0, 0.5f, 0), 1.6f, 12, L4Palette.Rock, L4Palette.RockDark);
            throne.Add(V(0, 1.3f, -0.6f), V(1.5f, 1.6f, 0.5f), L4Palette.RockDark, V(-8, 0, 0))
                  .Add(V(0, 1.02f, 0.05f), V(1.3f, 0.1f, 1f), L4Palette.Fur)
                  .Add(V(0, 2.25f, -0.65f), V(0.5f, 0.4f, 0.45f), L4Palette.Bone);
            CrateProp("Trono_de_Pedra", "Trono de pedra do chefe", throne, 1.6f);

            VoxelModel totem = new VoxelModel()
                .Add(V(0, 1.5f, 0), V(0.25f, 3f, 0.25f), L4Palette.WoodDark)
                .Add(V(0, 2.9f, 0), V(0.5f, 0.42f, 0.5f), L4Palette.Bone)
                .Add(V(0, 2.85f, 0.255f), V(0.3f, 0.1f, 0.01f), L4Palette.Void)
                .Add(V(0, 2.3f, 0), V(1.4f, 0.1f, 0.1f), L4Palette.Bone)
                .Add(V(-0.55f, 1.95f, 0.06f), V(0.4f, 0.7f, 0.04f), L4Palette.Leather, V(0, 0, 6))
                .Add(V(0.55f, 1.95f, 0.06f), V(0.4f, 0.7f, 0.04f), L4Palette.Leather, V(0, 0, -6))
                .Add(V(0, 3.3f, 0), V(0.08f, 0.5f, 0.08f), L4Palette.Tusk, V(0, 0, 20));
            GameObject tt = L2Core.Begin("Totem_de_Ossos", totem);
            L2Core.Box(tt, V(0, 1.5f, 0), V(0.4f, 3f, 0.4f));
            L2Core.Save(tt, P, "Totem de ossos dos ogros");

            VoxelModel chains = new VoxelModel();
            for (int i = 0; i < 15; i++) chains.Add(V(0, -0.15f - i * 0.2f, 0), V(0.1f, 0.22f, 0.05f), L4Palette.Iron, V(0, i % 2 == 0 ? 0 : 90, 0));
            chains.Add(V(0, -3.15f, 0), V(0.3f, 0.12f, 0.3f), L4Palette.Iron);
            L2Core.Simple("Correntes_Penduradas", chains, P, "Correntes penduradas", false);

            // Tocha de parede dos ogros (apagável).
            VoxelModel torch = new VoxelModel()
                .Add(V(0, 0, 0.06f), V(0.3f, 0.5f, 0.12f), L4Palette.Iron)
                .Add(V(0, 0.3f, 0.25f), V(0.1f, 0.6f, 0.1f), L4Palette.WoodDark, V(-20, 0, 0))
                .Add(V(0, 0.62f, 0.36f), V(0.18f, 0.12f, 0.18f), L4Palette.Rope, V(-20, 0, 0));
            GameObject tg = L2Core.Begin("Tocha_dos_Ogros", torch);
            GameObject flameVis = L2Core.Child(tg, "Chama", V(0, 0.72f, 0.4f), new VoxelModel().Add(Vector3.zero, V(0.14f, 0.14f, 0.14f), L4Palette.FireGlow));
            ParticleSystem tf = L2Core.AddFx(tg, "FX_Fire", V(0, 0.75f, 0.4f));
            if (tf != null) tf.transform.localScale = Vector3.one * 0.6f;
            Light tl = L2Core.PointLight(tg, "Luz", V(0, 0.95f, 0.6f), new Color(1f, 0.6f, 0.28f), 6.5f, 1.8f, true);
            L2Core.Sphere(tg, V(0, 0.5f, 0.4f), 1f, true);
            ExtinguishableLight el = tg.AddComponent<ExtinguishableLight>();
            el.lights = new[] { tl };
            el.flames = tf != null ? new[] { tf } : new ParticleSystem[0];
            el.litVisuals = new[] { flameVis };
            el.extinguishEffects = new[] { L2Core.Get("FX4_Fumaca_Apagada") };
            el.extinguishClip = L2Core.Clip("torch_extinguish");
            el.igniteClip = L2Core.Clip("torch_ignite");
            el.firstTimeTip = "Apagar tochas [E] cria sombras para passar escondido (o chiado faz um pouco de barulho).";
            tg.layer = InteractableLayer;
            L2Core.Save(tg, AssetCategoryType.Iluminacao, "Tocha de parede dos ogros (apagável)");

            // Braseiro (apagável, luz Mixed).
            VoxelModel brazier = new VoxelModel()
                .Add(V(0, 0.95f, 0), V(0.9f, 0.3f, 0.9f), L4Palette.Iron)
                .Add(V(0, 1.06f, 0), V(0.75f, 0.1f, 0.75f), L4Palette.Coal);
            foreach (float a in new[] { 0f, 120f, 240f })
            {
                float r = a * Mathf.Deg2Rad;
                brazier.Add(V(Mathf.Cos(r) * 0.32f, 0.42f, Mathf.Sin(r) * 0.32f), V(0.08f, 0.9f, 0.08f), L4Palette.Iron, V(Mathf.Sin(r) * 12f, 0, -Mathf.Cos(r) * 12f));
            }
            GameObject bz = L2Core.Begin("Braseiro_dos_Ogros", brazier);
            GameObject coals = L2Core.Child(bz, "Brasas", V(0, 1.12f, 0), new VoxelModel().Add(Vector3.zero, V(0.6f, 0.06f, 0.6f), L4Palette.Embers));
            ParticleSystem bf = L2Core.AddFx(bz, "FX4_Fogueira", V(0, 1.12f, 0));
            if (bf != null) bf.transform.localScale = Vector3.one * 0.55f;
            ParticleSystem be = L2Core.AddFx(bz, "FX4_Brasas", V(0, 1.2f, 0));
            Light bl = L2Core.PointLight(bz, "Luz", V(0, 1.7f, 0), new Color(1f, 0.55f, 0.25f), 8f, 2f, true, LightmapBakeType.Mixed);
            L2Core.Box(bz, V(0, 0.6f, 0), V(0.9f, 1.2f, 0.9f));
            GameObject bzTrigger = new GameObject("Interacao");
            bzTrigger.transform.SetParent(bz.transform, false);
            bzTrigger.layer = InteractableLayer;
            L2Core.Sphere(bzTrigger, V(0, 0.9f, 0), 1.2f, true);
            ExtinguishableLight bel = bz.AddComponent<ExtinguishableLight>();
            bel.lights = new[] { bl };
            bel.flames = bf != null && be != null ? new[] { bf, be } : new ParticleSystem[0];
            bel.litVisuals = new[] { coals };
            bel.extinguishEffects = new[] { L2Core.Get("FX4_Fumaca_Apagada") };
            bel.extinguishClip = L2Core.Clip("torch_extinguish");
            bel.igniteClip = L2Core.Clip("torch_ignite");
            bel.noiseRadius = 4f;
            L2Core.Save(bz, AssetCategoryType.Iluminacao, "Braseiro dos ogros (apagável)");
        }

        // ================================================================== Vegetação subterrânea

        /// <summary>
        /// Cogumelos altos e samambaias (esconderijos), cogumelos pequenos e fungo de prateleira (luzes fracas),
        /// musgo brilhante, raízes penduradas e líquen.
        /// </summary>
        private static void BuildVegetation()
        {
            const AssetCategoryType G = AssetCategoryType.Vegetacao;

            VoxelModel tall = new VoxelModel();
            float[] hs = { 2.1f, 1.6f, 1.3f, 1.9f, 1.1f, 1.45f, 1.75f };
            for (int i = 0; i < hs.Length; i++)
            {
                float a = i * 2.2f, r = 0.35f + (i % 3) * 0.32f;
                Vector3 p = V(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                float h = hs[i];
                tall.Add(p + V(0, h * 0.5f, 0), V(0.16f, h, 0.16f), L4Palette.FungusStem, V(i * 3 - 9, 0, (i % 3 - 1) * 5))
                    .Add(p + V(0, h, 0), V(0.75f + h * 0.15f, 0.16f, 0.75f + h * 0.15f), i % 3 == 0 ? L4Palette.FungusAmber : L4Palette.FungusTeal, V(0, i * 25, 0))
                    .Add(p + V(0, h + 0.1f, 0), V(0.45f, 0.1f, 0.45f), L4Palette.FungusTeal, V(0, i * 25 + 45, 0));
            }
            GameObject tm = L2Core.Begin("Cogumelos_Altos", tall);
            HideSpot(tm, V(2.4f, 1.8f, 2.4f), "Cogumelos altos");
            L2Core.PointLight(tm, "Brilho", V(0, 1.9f, 0), new Color(0.3f, 0.9f, 0.8f), 3f, 0.3f, false, LightmapBakeType.Mixed);
            L2Core.AddFx(tm, "FX4_Esporos", V(0, 1.4f, 0));
            L2Core.Save(tm, G, "Cogumelos altos (esconderijo)");

            VoxelModel fern = new VoxelModel();
            for (int i = 0; i < 11; i++)
            {
                float a = i * 0.57f * Mathf.PI;
                float len = 0.9f + (i % 3) * 0.25f;
                fern.Add(V(Mathf.Cos(a) * len * 0.45f, 0.55f, Mathf.Sin(a) * len * 0.45f), V(0.32f, 0.06f, len), L4Palette.Fern, V(-35, a * Mathf.Rad2Deg + 90f, 0));
            }
            fern.Add(V(0, 0.15f, 0), V(0.4f, 0.3f, 0.4f), L4Palette.Fern);
            GameObject fg = L2Core.Begin("Samambaia_da_Caverna", fern);
            HideSpot(fg, V(1.9f, 1.3f, 1.9f), "Samambaias");
            L2Core.Save(fg, G, "Samambaias da caverna (esconderijo)");

            VoxelModel small = new VoxelModel();
            for (int i = 0; i < 6; i++)
            {
                float a = i * 1.9f, r = 0.15f + (i % 3) * 0.12f;
                float h = 0.18f + (i * 7 % 4) * 0.07f;
                Vector3 p = V(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                small.Add(p + V(0, h * 0.5f, 0), V(0.05f, h, 0.05f), L4Palette.FungusStem)
                     .Add(p + V(0, h, 0), V(0.18f, 0.06f, 0.18f), i % 2 == 0 ? L4Palette.FungusTeal : L4Palette.FungusAmber);
            }
            GameObject sm = L2Core.Begin("Cogumelos_Pequenos", small);
            L2Core.PointLight(sm, "Luz", V(0, 0.5f, 0), new Color(0.35f, 0.95f, 0.85f), 4f, 0.9f, false, LightmapBakeType.Mixed);
            L2Core.Save(sm, AssetCategoryType.Iluminacao, "Cogumelos brilhantes (luz fraca)");

            VoxelModel shelf = new VoxelModel()
                .Add(V(0, 0, 0.2f), V(0.7f, 0.08f, 0.4f), L4Palette.FungusAmber)
                .Add(V(0.15f, -0.25f, 0.15f), V(0.5f, 0.07f, 0.3f), L4Palette.FungusAmber)
                .Add(V(-0.12f, 0.22f, 0.12f), V(0.4f, 0.06f, 0.25f), L4Palette.FungusAmber);
            GameObject sf = L2Core.Begin("Fungo_de_Prateleira", shelf);
            L2Core.PointLight(sf, "Luz", V(0, 0, 0.6f), new Color(1f, 0.6f, 0.25f), 3.5f, 0.8f, false, LightmapBakeType.Mixed);
            L2Core.Save(sf, AssetCategoryType.Iluminacao, "Fungo de prateleira (luz fraca)");

            VoxelModel moss = new VoxelModel();
            for (int i = 0; i < 8; i++)
                moss.Add(V(Mathf.Cos(i * 1.7f) * 0.7f, 0.02f, Mathf.Sin(i * 1.7f) * 0.6f), V(0.7f + (i % 3) * 0.2f, 0.04f, 0.55f), L4Palette.Moss, V(0, i * 33, 0));
            L2Core.Simple("Musgo_Brilhante", moss, G, "Musgo brilhante", false);

            VoxelModel roots = new VoxelModel();
            for (int i = 0; i < 8; i++)
            {
                float len = 0.8f + (i * 7 % 5) * 0.35f;
                roots.Add(V(-0.7f + i * 0.2f, -len * 0.5f, (i % 3) * 0.12f), V(0.07f, len, 0.07f), L4Palette.Roots, V(0, 0, (i - 4) * 4f));
            }
            L2Core.Simple("Raizes_Pendentes", roots, G, "Raízes penduradas", false);

            VoxelModel lichen = new VoxelModel();
            for (int i = 0; i < 7; i++)
                lichen.Add(V(Mathf.Cos(i * 2f) * 0.5f, 0.6f + Mathf.Sin(i * 2f) * 0.4f, 0.02f), V(0.35f + (i % 2) * 0.15f, 0.3f, 0.03f), L4Palette.Lichen, V(0, 0, i * 20));
            L2Core.Simple("Liquen_de_Parede", lichen, G, "Líquen de parede", false);
        }

        /// <summary>
        /// Gatilho de esconderijo com efeito de folhas.
        /// </summary>
        /// <param name="go">Objeto.</param>
        /// <param name="size">Tamanho do gatilho.</param>
        /// <param name="label">Nome no HUD.</param>
        private static void HideSpot(GameObject go, Vector3 size, string label)
        {
            GameObject t = new GameObject("Esconderijo");
            t.transform.SetParent(go.transform, false);
            L2Core.Box(t, V(0, size.y * 0.5f, 0), size, true);
            t.layer = 2;
            HidingSpot hs = t.AddComponent<HidingSpot>();
            hs.label = label;
            ParticleSystem leaves = L2Core.AddFx(t, "FX4_Folhas", V(0, size.y * 0.5f, 0));
            hs.rustleEffect = leaves;
            hs.rustleClip = L2Core.Clip("foliage_rustle");
            go.tag = StealthTags.HidingSpot;
        }

        // ================================================================== Luzes

        /// <summary>
        /// Fenda de luz no teto (área + feixe), poça brilhante e cristais fracos.
        /// </summary>
        private static void BuildLights()
        {
            const AssetCategoryType L = AssetCategoryType.Iluminacao;

            // Fenda de luz: feixe visível + luz de área (Baked) + zona de luz para a furtividade.
            GameObject shaft = new GameObject("Fenda_de_Luz");
            GameObject beam = L2Core.Child(shaft, "Feixe", V(0, 3.5f, 0), new VoxelModel().Add(Vector3.zero, V(1.6f, 7f, 1.6f), L4Palette.Beam, V(0, 30, 0)));
            beam.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObject areaGo = new GameObject("LuzDeArea");
            areaGo.transform.SetParent(shaft.transform, false);
            areaGo.transform.localPosition = V(0, 6.8f, 0);
            areaGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Light area = areaGo.AddComponent<Light>();
            area.type = LightType.Rectangle;
            area.areaSize = new Vector2(1.6f, 1.6f);
            area.color = new Color(0.65f, 0.8f, 1f);
            area.intensity = 3f;
            area.range = 10f;
            area.lightmapBakeType = LightmapBakeType.Baked;
            L2Core.PointLight(shaft, "Preenchimento", V(0, 1.5f, 0), new Color(0.6f, 0.75f, 1f), 4.5f, 0.7f, false, LightmapBakeType.Mixed);
            GameObject zone = new GameObject("ZonaDeLuz");
            zone.transform.SetParent(shaft.transform, false);
            zone.layer = 2;
            L2Core.Box(zone, V(0, 1.5f, 0), V(2.6f, 3f, 2.6f), true);
            LightZone lz = zone.AddComponent<LightZone>();
            lz.bonus = 0.45f;
            lz.label = "Feixe de luz";
            L2Core.AddFx(shaft, "FX4_Poeira_no_Ar", V(0, 3f, 0)).transform.localScale = V(0.25f, 1f, 0.25f);
            L2Core.Save(shaft, L, "Fenda de luz no teto (luz de área)");

            VoxelModel pool = new VoxelModel()
                .Add(V(0, 0.02f, 0), V(2.6f, 0.04f, 1.8f), L4Palette.Water, V(0, 15, 0))
                .Add(V(0.4f, 0.03f, 0.3f), V(1.8f, 0.04f, 1.4f), L4Palette.Water, V(0, -25, 0));
            for (int i = 0; i < 7; i++)
                pool.Add(V(Mathf.Cos(i * 0.9f) * 1.45f, 0.08f, Mathf.Sin(i * 0.9f) * 1.05f), V(0.35f, 0.16f, 0.3f), L4Palette.RockDark, V(0, i * 40, 0));
            GameObject pg = L2Core.Begin("Poca_Brilhante", pool);
            L2Core.PointLight(pg, "Luz", V(0, 0.6f, 0), new Color(0.35f, 0.85f, 0.95f), 5f, 1f, false, LightmapBakeType.Mixed);
            L2Core.AddFx(pg, "FX4_Esporos", V(0, 0.8f, 0));
            L2Core.Save(pg, L, "Poça brilhante");

            VoxelModel cr = new VoxelModel();
            L3Props.Spike(cr, Vector3.zero, 1f, 0.25f, V(0, 0, 6), L4Palette.CrystalViolet);
            L3Props.Spike(cr, V(0.22f, 0, 0.1f), 0.7f, 0.18f, V(0, 0, -25), L4Palette.CrystalViolet);
            L3Props.Spike(cr, V(-0.2f, 0, -0.08f), 0.6f, 0.16f, V(15, 0, 25), L4Palette.CrystalViolet);
            GameObject cg = L2Core.Begin("Cristais_Fracos", cr);
            L2Core.Fit(cg, cr);
            L2Core.PointLight(cg, "Luz", V(0, 0.9f, 0), new Color(0.65f, 0.4f, 1f), 4f, 0.9f, false, LightmapBakeType.Mixed);
            L2Core.Save(cg, L, "Cristais fracos (luz violeta)");
        }

        // ================================================================== Peças do nível

        /// <summary>
        /// Corda de descida (entrada), saída das galerias, moldura das fendas, placa de aviso e restos de minerador.
        /// </summary>
        private static void BuildLevelPieces()
        {
            VoxelModel rope = new VoxelModel();
            for (int i = 0; i < 12; i++) rope.Add(V(((i % 2) - 0.5f) * 0.04f, 0.3f + i * 0.35f, 0), V(0.07f, 0.38f, 0.07f), L4Palette.Rope, V(0, 0, (i % 2 - 0.5f) * 6));
            rope.Add(V(0.3f, 0.1f, 0.2f), V(0.6f, 0.2f, 0.5f), L4Palette.Rope, V(0, 30, 0));
            Rock(rope, V(-0.8f, 0.25f, 0.3f), 0.5f, 3, L4Palette.Rock, L4Palette.RockDark);
            Rock(rope, V(0.9f, 0.2f, -0.4f), 0.4f, 5, L4Palette.RockDark, L4Palette.Rock);
            L2Core.Simple("Corda_de_Descida", rope, AssetCategoryType.Props, "Corda de descida (entrada)", false);

            // Saída: moldura de mina escorada, luz fria vinda de cima e o gatilho de fim de nível.
            VoxelModel exit = new VoxelModel();
            foreach (float x in new[] { -1.8f, 1.8f })
                exit.Add(V(x, 1.75f, 0), V(0.35f, 3.5f, 0.35f), L4Palette.WoodDark);
            exit.Add(V(0, 3.6f, 0), V(4.2f, 0.4f, 0.45f), L4Palette.WoodDark)
                .Add(V(0, 3.95f, 0), V(4.5f, 0.3f, 0.6f), L4Palette.Iron)
                .Add(V(-1.4f, 3.2f, 0.2f), V(0.8f, 0.6f, 0.1f), L4Palette.Plank, V(0, 0, 35));
            GameObject eg = L2Core.Begin("Saida_das_Galerias", exit);
            L2Core.Box(eg, V(-1.8f, 1.75f, 0), V(0.4f, 3.5f, 0.4f));
            L2Core.Box(eg, V(1.8f, 1.75f, 0), V(0.4f, 3.5f, 0.4f));
            L2Core.PointLight(eg, "LuzDeCima", V(0, 3.2f, 1.5f), new Color(0.7f, 0.85f, 1f), 7f, 1.6f, false, LightmapBakeType.Mixed);
            GameObject trig = new GameObject("Saida");
            trig.transform.SetParent(eg.transform, false);
            trig.layer = 2;
            L2Core.Box(trig, V(0, 1.5f, 1.5f), V(3.2f, 3f, 1.5f), true);
            trig.AddComponent<StealthLevelExit>();
            L2Core.AddFx(eg, "FX4_Poeira_no_Ar", V(0, 2f, 2f)).transform.localScale = V(0.35f, 0.6f, 0.35f);
            L2Core.Save(eg, AssetCategoryType.Mecanismos, "Saída das galerias (para o Nível 5)");

            VoxelModel frame = new VoxelModel();
            Rock(frame, V(-1.55f, 0.7f, 0), 1.1f, 2, L4Palette.Rock, L4Palette.RockDark);
            Rock(frame, V(1.55f, 0.7f, 0), 1.1f, 6, L4Palette.RockDark, L4Palette.Rock);
            frame.Add(V(0, 1.65f, 0), V(4f, 0.6f, 1.2f), L4Palette.RockDark, V(0, 0, 3))
                 .Add(V(0.5f, 1.4f, 0.55f), V(1.2f, 0.25f, 0.2f), L4Palette.RockMoss, V(0, 0, -8));
            GameObject fr = L2Core.Begin("Moldura_da_Fenda", frame);
            L2Core.Box(fr, V(-1.6f, 0.75f, 0), V(0.8f, 1.5f, 1.1f));
            L2Core.Box(fr, V(1.6f, 0.75f, 0), V(0.8f, 1.5f, 1.1f));
            L2Core.Box(fr, V(0, 1.7f, 0), V(4f, 0.6f, 1.1f));
            L2Core.Save(fr, AssetCategoryType.Arquitetura, "Boca de fenda estreita");

            VoxelModel sign = new VoxelModel()
                .Add(V(0, 0.7f, 0), V(0.12f, 1.4f, 0.12f), L4Palette.WoodDark)
                .Add(V(0, 1.3f, 0.07f), V(1f, 0.5f, 0.06f), L4Palette.Plank, V(0, 0, -6))
                .Add(V(-0.25f, 1.33f, 0.105f), V(0.35f, 0.08f, 0.01f), L4Palette.Meat, V(0, 0, -6))
                .Add(V(0.2f, 1.25f, 0.105f), V(0.3f, 0.08f, 0.01f), L4Palette.Meat, V(0, 0, 30))
                .Add(V(0.2f, 1.25f, 0.105f), V(0.3f, 0.08f, 0.01f), L4Palette.Meat, V(0, 0, -30));
            L2Core.Simple("Placa_de_Aviso", sign, AssetCategoryType.Inscricoes, "Placa de aviso dos mineiros", true);

            VoxelModel miner = new VoxelModel()
                .Add(V(0, 0.12f, 0), V(0.5f, 0.2f, 0.9f), L4Palette.Canvas, V(0, 20, 0))
                .Add(V(0.05f, 0.3f, -0.35f), V(0.26f, 0.24f, 0.28f), L4Palette.Bone)
                .Add(V(0.05f, 0.45f, -0.35f), V(0.34f, 0.12f, 0.34f), L4Palette.Iron)
                .Add(V(-0.3f, 0.06f, 0.3f), V(0.6f, 0.08f, 0.08f), L4Palette.Bone, V(0, 50, 0))
                .Add(V(0.35f, 0.06f, 0.25f), V(0.55f, 0.08f, 0.08f), L4Palette.Bone, V(0, -30, 0))
                .Add(V(0.45f, 0.15f, -0.45f), V(0.18f, 0.28f, 0.18f), L4Palette.Iron)
                .Add(V(0.45f, 0.18f, -0.45f), V(0.12f, 0.16f, 0.12f), L4Palette.CrystalViolet);
            L2Core.Simple("Restos_de_Minerador", miner, AssetCategoryType.Props, "Restos de um minerador", false);

            Artifact("Amuleto_do_Minerador", "Amuleto do Minerador", new VoxelModel()
                .Add(V(0, 0, 0), V(0.3f, 0.36f, 0.06f), L4Palette.Iron)
                .Add(V(0, 0, 0.035f), V(0.18f, 0.22f, 0.02f), L4Palette.CrystalViolet)
                .Add(V(0, 0.24f, 0), V(0.05f, 0.14f, 0.05f), L4Palette.Rope), new Color(0.7f, 0.5f, 1f));
            Artifact("Dente_de_Ogro_Dourado", "Dente de Ogro Dourado", new VoxelModel()
                .Add(V(0, 0, 0), V(0.16f, 0.4f, 0.16f), L4Palette.Tusk, V(0, 0, 12))
                .Add(V(0.02f, 0.22f, 0), V(0.12f, 0.12f, 0.12f), L4Palette.FireGlow)
                .Add(V(0, -0.2f, 0), V(0.22f, 0.08f, 0.22f), L4Palette.Iron), new Color(1f, 0.75f, 0.35f));
        }

        /// <summary>
        /// Artefato coletável (gira, brilha fraco).
        /// </summary>
        /// <param name="id">Prefab.</param>
        /// <param name="artifactName">Nome do artefato.</param>
        /// <param name="model">Modelo.</param>
        /// <param name="glow">Cor do brilho.</param>
        private static void Artifact(string id, string artifactName, VoxelModel model, Color glow)
        {
            GameObject go = new GameObject(id);
            go.tag = GameTags.Artifact;
            go.layer = InteractableLayer;
            GameObject visual = L2Core.Child(go, "Visual", V(0, 0.6f, 0), model);
            Spinner spin = visual.AddComponent<Spinner>();
            spin.degreesPerSecond = V(0, 70f, 0);
            spin.bobAmplitude = 0.08f;
            L2Core.Sphere(go, V(0, 0.6f, 0), 0.55f, true);
            Collectible c = go.AddComponent<Collectible>();
            c.type = CollectibleType.Artifact;
            c.artifactName = artifactName;
            c.pickupEffect = L2Core.L1("FX_Sparkle");
            L2Core.AddFx(go, "FX_Glint", V(0, 0.6f, 0));
            L2Core.PointLight(go, "Brilho", V(0, 0.9f, 0), glow, 2.5f, 0.9f);
            L2Core.Save(go, AssetCategoryType.Tesouros, artifactName + " (artefato)");
        }
    }
}
