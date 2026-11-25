using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Prefabs de cenário do Nível 3 (voxel): cristais de vários formatos (com luz e partículas próprias),
    /// rochas e formações, estalactites e estalagmites, estruturas da masmorra, caixas, vegetação da caverna,
    /// iluminação, artefatos, placa de runas e o fundo do abismo.
    /// </summary>
    public static class L3Props
    {
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        /// <summary>
        /// Cria todos os prefabs de cenário.
        /// </summary>
        public static void BuildAll()
        {
            BuildCrystals();
            BuildRocks();
            BuildStalactites();
            BuildStructures();
            BuildVegetation();
            BuildLighting();
            BuildTreasures();
        }

        // ================================================================== Auxiliares de modelagem

        /// <summary>
        /// Acrescenta um cristal prismático (duas caixas giradas 45° + ponta) a partir de uma base, inclinado.
        /// </summary>
        /// <param name="m">Modelo.</param>
        /// <param name="basePos">Base do cristal.</param>
        /// <param name="h">Altura.</param>
        /// <param name="w">Largura.</param>
        /// <param name="tilt">Inclinação (Euler).</param>
        /// <param name="mat">Material.</param>
        /// <returns>O modelo.</returns>
        public static VoxelModel Spike(VoxelModel m, Vector3 basePos, float h, float w, Vector3 tilt, Material mat)
        {
            Quaternion q = Quaternion.Euler(tilt);
            Vector3 e2 = (q * Quaternion.Euler(0f, 45f, 0f)).eulerAngles;
            m.Add(basePos + q * V(0, h * 0.5f, 0), V(w, h, w), mat, tilt);
            m.Add(basePos + q * V(0, h * 0.48f, 0), V(w * 0.8f, h * 0.96f, w * 0.8f), mat, e2);
            m.Add(basePos + q * V(0, h * 1.02f, 0), V(w * 0.5f, h * 0.2f, w * 0.5f), mat, e2);
            m.Add(basePos + q * V(0, h * 1.13f, 0), V(w * 0.22f, h * 0.1f, w * 0.22f), mat, tilt);
            return m;
        }

        /// <summary>
        /// Cristal pendurado (de cima para baixo).
        /// </summary>
        /// <param name="m">Modelo.</param>
        /// <param name="topPos">Ponto de fixação.</param>
        /// <param name="h">Comprimento.</param>
        /// <param name="w">Largura.</param>
        /// <param name="tilt">Inclinação.</param>
        /// <param name="mat">Material.</param>
        /// <returns>O modelo.</returns>
        public static VoxelModel HangingSpike(VoxelModel m, Vector3 topPos, float h, float w, Vector3 tilt, Material mat)
        {
            return Spike(m, topPos, h, w, tilt + V(180f, 0f, 0f), mat);
        }

        /// <summary>
        /// Rocha irregular (três caixas giradas).
        /// </summary>
        /// <param name="m">Modelo.</param>
        /// <param name="c">Centro.</param>
        /// <param name="s">Tamanho.</param>
        /// <param name="seed">Variação.</param>
        /// <param name="mat">Material.</param>
        /// <returns>O modelo.</returns>
        public static VoxelModel Boulder(VoxelModel m, Vector3 c, float s, int seed, Material mat)
        {
            m.Add(c, V(s, s * 0.8f, s * 0.9f), mat, V(seed * 7 % 20, seed * 37 % 90, seed * 13 % 15));
            m.Add(c + V(s * 0.2f, s * 0.15f, -s * 0.1f), V(s * 0.7f, s * 0.7f, s * 0.75f), mat, V(25 + seed * 3, 35 + seed * 11, 15));
            m.Add(c + V(-s * 0.25f, -s * 0.1f, s * 0.15f), V(s * 0.6f, s * 0.55f, s * 0.6f), L3Palette.RockDark, V(10, 60 + seed * 7, 30));
            return m;
        }

        /// <summary>
        /// Cria o prefab com cristal: Tag Crystal, CrystalGlow e (opcional) luz e brilho de partículas.
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="display">Nome legível.</param>
        /// <param name="model">Modelo.</param>
        /// <param name="glow">Cor da emissão (HDR).</param>
        /// <param name="lightColor">Cor da luz (transparente = sem luz).</param>
        /// <param name="lightPos">Posição da luz e das partículas.</param>
        /// <param name="range">Alcance da luz.</param>
        /// <param name="sparkles">Com partículas brilhantes.</param>
        /// <param name="collider">Com colisor ajustado.</param>
        /// <returns>O prefab.</returns>
        private static GameObject CrystalPrefab(string id, string display, VoxelModel model, Color glow, Color lightColor, Vector3 lightPos, float range, bool sparkles, bool collider)
        {
            GameObject go = L2Core.Begin(id, model);
            go.tag = ParkourTags.Crystal;
            if (collider) L2Core.Fit(go, model);
            if (lightColor.a > 0f) L2Core.PointLight(go, "Luz", lightPos, lightColor, range, 1.8f, false, LightmapBakeType.Mixed);
            if (sparkles) L2Core.AddFx(go, "FX3_Brilho_de_Cristal", lightPos);
            CrystalGlow g = go.AddComponent<CrystalGlow>();
            g.emission = glow;
            g.pulseSpeed = 0.35f + (L2Core.StableHash(id) % 40) / 100f;
            return L2Core.Save(go, AssetCategoryType.Iluminacao, display);
        }

        // ================================================================== Cristais

        /// <summary>
        /// Nove formatos de cristal: agulha, aglomerado, grande formação, de parede, suspenso, rosa, âmbar,
        /// flutuante e apagado.
        /// </summary>
        private static void BuildCrystals()
        {
            Color cyan = new Color(0.2f, 1.3f, 1.4f), blue = new Color(0.25f, 0.6f, 1.6f), violet = new Color(0.8f, 0.3f, 1.6f);

            CrystalPrefab("Cristal_Agulha", "Cristal em agulha", Spike(new VoxelModel(), Vector3.zero, 1.8f, 0.32f, V(6, 0, 4), L3Palette.CrystalCyan),
                cyan, Color.clear, V(0, 1f, 0), 0f, false, true);

            VoxelModel cluster = new VoxelModel();
            Spike(cluster, Vector3.zero, 1.5f, 0.38f, V(0, 0, 0), L3Palette.CrystalCyan);
            Spike(cluster, V(0.3f, 0, 0.1f), 1f, 0.28f, V(0, 0, -28), L3Palette.CrystalCyan);
            Spike(cluster, V(-0.28f, 0, 0.05f), 1.1f, 0.3f, V(10, 0, 30), L3Palette.CrystalBlue);
            Spike(cluster, V(0.05f, 0, -0.3f), 0.8f, 0.24f, V(-30, 0, 5), L3Palette.CrystalCyan);
            Spike(cluster, V(0.05f, 0, 0.32f), 0.7f, 0.22f, V(30, 0, -10), L3Palette.CrystalBlue);
            CrystalPrefab("Cristal_Aglomerado", "Aglomerado de cristais", cluster, cyan, Color.clear, V(0, 0.8f, 0), 0f, true, true);

            VoxelModel big = new VoxelModel().Add(V(0, 0.2f, 0), V(2.2f, 0.4f, 2f), L3Palette.RockDark);
            Spike(big, V(0, 0.3f, 0), 3.2f, 0.75f, V(0, 0, 4), L3Palette.CrystalBlue);
            Spike(big, V(0.6f, 0.3f, 0.2f), 2.2f, 0.55f, V(5, 0, -24), L3Palette.CrystalBlue);
            Spike(big, V(-0.55f, 0.3f, -0.1f), 2.4f, 0.6f, V(-8, 0, 26), L3Palette.CrystalCyan);
            Spike(big, V(0.1f, 0.3f, -0.6f), 1.6f, 0.45f, V(-30, 0, 0), L3Palette.CrystalBlue);
            Spike(big, V(-0.2f, 0.3f, 0.6f), 1.4f, 0.4f, V(32, 0, 8), L3Palette.CrystalCyan);
            CrystalPrefab("Cristal_Grande", "Grande formação de cristal", big, blue, new Color(0.45f, 0.75f, 1f), V(0, 2f, 0.8f), 10f, true, true);

            VoxelModel wall = new VoxelModel().Add(V(0, 0.8f, -0.1f), V(1.8f, 1.6f, 0.2f), L3Palette.RockDark);
            Spike(wall, V(0, 0.6f, 0), 1.2f, 0.3f, V(70, 0, 0), L3Palette.CrystalViolet);
            Spike(wall, V(0.5f, 0.9f, 0), 0.9f, 0.25f, V(60, 0, -25), L3Palette.CrystalViolet);
            Spike(wall, V(-0.5f, 1f, 0), 1f, 0.26f, V(65, 0, 25), L3Palette.CrystalBlue);
            Spike(wall, V(0.2f, 1.3f, 0), 0.7f, 0.2f, V(45, 0, -10), L3Palette.CrystalViolet);
            CrystalPrefab("Cristal_de_Parede", "Cristais de parede", wall, violet, Color.clear, V(0, 1f, 0.6f), 0f, true, false);

            VoxelModel hanging = new VoxelModel().Add(V(0, -0.1f, 0), V(1f, 0.2f, 1f), L3Palette.RockDark);
            HangingSpike(hanging, V(0, -0.15f, 0), 2.2f, 0.45f, V(0, 0, 4), L3Palette.CrystalViolet);
            HangingSpike(hanging, V(0.3f, -0.15f, 0.1f), 1.4f, 0.3f, V(0, 0, -20), L3Palette.CrystalViolet);
            HangingSpike(hanging, V(-0.3f, -0.15f, -0.1f), 1.1f, 0.28f, V(15, 0, 20), L3Palette.CrystalBlue);
            CrystalPrefab("Cristal_Suspenso", "Cristal suspenso", hanging, violet, new Color(0.7f, 0.45f, 1f), V(0, -1.6f, 0), 7f, true, false);

            VoxelModel pink = new VoxelModel();
            Spike(pink, Vector3.zero, 1.2f, 0.34f, V(0, 0, 6), L3Palette.CrystalPink);
            Spike(pink, V(0.25f, 0, 0.1f), 0.8f, 0.24f, V(0, 0, -32), L3Palette.CrystalPink);
            Spike(pink, V(-0.2f, 0, -0.15f), 0.9f, 0.26f, V(-20, 0, 28), L3Palette.CrystalPink);
            CrystalPrefab("Cristal_Rosa", "Cristais rosados", pink, new Color(1.4f, 0.35f, 0.9f), Color.clear, V(0, 0.7f, 0), 0f, true, true);

            VoxelModel amber = new VoxelModel();
            Spike(amber, Vector3.zero, 1.4f, 0.4f, V(0, 0, -8), L3Palette.CrystalAmber);
            Spike(amber, V(0.3f, 0, -0.1f), 0.9f, 0.28f, V(10, 0, -35), L3Palette.CrystalAmber);
            Spike(amber, V(-0.25f, 0, 0.2f), 0.7f, 0.22f, V(25, 0, 30), L3Palette.CrystalAmber);
            CrystalPrefab("Cristal_Ambar", "Cristais âmbar", amber, new Color(1.5f, 0.8f, 0.2f), new Color(1f, 0.7f, 0.35f), V(0, 0.9f, 0), 5f, false, true);

            // Cristal flutuante: gira e flutua (Spinner do Nível 1).
            VoxelModel floating = new VoxelModel();
            Spike(floating, V(0, -0.5f, 0), 0.9f, 0.35f, Vector3.zero, L3Palette.CrystalCore);
            HangingSpike(floating, V(0, -0.5f, 0), 0.5f, 0.35f, Vector3.zero, L3Palette.CrystalCore);
            GameObject fl = new GameObject("Cristal_Flutuante");
            fl.tag = ParkourTags.Crystal;
            GameObject flVisual = L2Core.Child(fl, "Visual", V(0, 1.5f, 0), floating);
            Spinner sp = flVisual.AddComponent<Spinner>();
            sp.degreesPerSecond = V(0, 40, 0);
            sp.bobAmplitude = 0.15f;
            L2Core.PointLight(flVisual, "Luz", Vector3.zero, new Color(0.7f, 0.9f, 1f), 6f, 1.6f, false, LightmapBakeType.Mixed);
            L2Core.AddFx(fl, "FX3_Brilho_de_Cristal", V(0, 1.5f, 0));
            fl.AddComponent<CrystalGlow>().emission = new Color(2.2f, 2.6f, 3f);
            L2Core.Save(fl, AssetCategoryType.Iluminacao, "Cristal flutuante");

            VoxelModel dead = new VoxelModel();
            Spike(dead, Vector3.zero, 1.3f, 0.4f, V(0, 0, 12), L3Palette.CrystalDark);
            Spike(dead, V(0.3f, 0, 0), 0.7f, 0.28f, V(0, 0, -30), L3Palette.CrystalDark);
            L2Core.Simple("Cristal_Apagado", dead, AssetCategoryType.Props, "Cristal apagado", true);
        }

        // ================================================================== Rochas

        /// <summary>
        /// Rochas grande e média, pedregulhos, colunas de basalto, coluna natural, arco natural e paredão.
        /// </summary>
        private static void BuildRocks()
        {
            const AssetCategoryType C = AssetCategoryType.Arquitetura;
            L2Core.Simple("Rocha_Grande", Boulder(new VoxelModel(), V(0, 1f, 0), 2.4f, 3, L3Palette.Rock), C, "Rocha grande", true);
            L2Core.Simple("Rocha_Media", Boulder(new VoxelModel(), V(0, 0.5f, 0), 1.2f, 5, L3Palette.Rock), C, "Rocha média", true);

            VoxelModel pebbles = new VoxelModel();
            for (int i = 0; i < 7; i++)
            {
                float s = 0.12f + (i * 37 % 10) / 10f * 0.22f;
                pebbles.Add(V(Mathf.Cos(i * 2.3f) * 0.6f, s * 0.4f, Mathf.Sin(i * 2.3f) * 0.5f), V(s, s * 0.8f, s * 1.1f), i % 2 == 0 ? L3Palette.Rock : L3Palette.Gravel, V(i * 17, i * 41, 0));
            }
            L2Core.Simple("Pedregulhos", pebbles, C, "Pedregulhos", false);

            VoxelModel basalt = new VoxelModel();
            float[] heights = { 4.5f, 3.6f, 5.2f, 2.8f, 3.9f, 2.2f, 4.8f };
            for (int i = 0; i < heights.Length; i++)
            {
                Vector3 p = V((i % 3 - 1) * 0.8f + (i / 3) * 0.4f, 0, (i / 3 - 1) * 0.7f);
                basalt.Add(p + V(0, heights[i] * 0.5f, 0), V(0.75f, heights[i], 0.7f), L3Palette.Basalt, V(0, i * 17, 0))
                      .Add(p + V(0, heights[i] * 0.5f, 0), V(0.55f, heights[i] * 0.99f, 0.8f), L3Palette.Basalt, V(0, i * 17 + 45, 0));
            }
            L2Core.Simple("Formacao_Colunar", basalt, C, "Formação colunar de basalto", true);

            VoxelModel column = new VoxelModel();
            float[] widths = { 3.4f, 2.6f, 2.1f, 1.9f, 2.2f, 2.9f, 3.6f };
            for (int i = 0; i < widths.Length; i++)
                column.Add(V(0, i * 12f + 6f, 0), V(widths[i], 12.2f, widths[i] * 0.9f), i % 2 == 0 ? L3Palette.Rock : L3Palette.RockDark, V(0, i * 23, 0));
            GameObject col = L2Core.Begin("Coluna_Natural", column);
            L2Core.Box(col, V(0, 42f, 0), V(2.4f, 84f, 2.2f));
            L2Core.Save(col, C, "Coluna natural (chão ao teto)");

            VoxelModel arch = new VoxelModel();
            Boulder(arch, V(-3f, 1.5f, 0), 3f, 1, L3Palette.Rock);
            Boulder(arch, V(-2.6f, 4f, 0), 2.4f, 2, L3Palette.Rock);
            Boulder(arch, V(3f, 1.4f, 0), 2.8f, 4, L3Palette.Rock);
            Boulder(arch, V(2.5f, 3.8f, 0), 2.2f, 6, L3Palette.Rock);
            arch.Add(V(0, 5.6f, 0), V(6.5f, 1.6f, 2f), L3Palette.RockDark, V(0, 0, 4));
            L2Core.Simple("Arco_Natural", arch, C, "Arco natural de rocha", true);

            // Paredão: bloco alto de rocha que forma as bordas e paredes do abismo (origem no topo).
            VoxelModel cliff = new VoxelModel()
                .Add(V(0, -15f, 0), V(10.5f, 30f, 3f), L3Palette.Rock)
                .Add(V(1.5f, -40f, 0.3f), V(9f, 22f, 2.6f), L3Palette.RockDark, V(0, 0, 3))
                .Add(V(-3f, -6f, 1.2f), V(3f, 7f, 1.5f), L3Palette.RockDark, V(0, 15, 8))
                .Add(V(2.8f, -20f, 1.3f), V(3.5f, 9f, 1.6f), L3Palette.Rock, V(5, -10, -6));
            GameObject cl = L2Core.Begin("Paredao_Rochoso", cliff);
            L2Core.Box(cl, V(0, -26f, 0), V(10.5f, 52f, 3f));
            L2Core.Save(cl, C, "Paredão rochoso do abismo");
        }

        // ================================================================== Estalactites e estalagmites

        /// <summary>
        /// Três tamanhos de estalactite (penduradas, origem no topo) e de estalagmite (origem na base).
        /// </summary>
        private static void BuildStalactites()
        {
            const AssetCategoryType C = AssetCategoryType.Arquitetura;
            float[] sizes = { 1.6f, 3.2f, 6f };
            string[] names = { "P", "M", "G" };
            string[] labels = { "pequena", "média", "grande" };
            for (int i = 0; i < 3; i++)
            {
                float h = sizes[i];
                VoxelModel tite = new VoxelModel()
                    .Add(V(0, -h * 0.2f, 0), V(h * 0.32f, h * 0.4f, h * 0.3f), L3Palette.RockDark)
                    .Add(V(0, -h * 0.55f, 0), V(h * 0.2f, h * 0.35f, h * 0.2f), L3Palette.Rock, V(0, 20, 2))
                    .Add(V(0.02f, -h * 0.82f, 0), V(h * 0.1f, h * 0.25f, h * 0.1f), L3Palette.WetStone, V(0, 45, 3))
                    .Add(V(0.03f, -h * 0.98f, 0), V(h * 0.04f, h * 0.1f, h * 0.04f), L3Palette.WetStone);
                L2Core.Simple("Estalactite_" + names[i], tite, C, "Estalactite " + labels[i], false);

                float g = sizes[i] * 0.8f;
                VoxelModel mite = new VoxelModel()
                    .Add(V(0, g * 0.2f, 0), V(g * 0.4f, g * 0.4f, g * 0.38f), L3Palette.Rock)
                    .Add(V(0, g * 0.55f, 0), V(g * 0.25f, g * 0.35f, g * 0.24f), L3Palette.Rock, V(0, 25, -2))
                    .Add(V(0, g * 0.82f, 0), V(g * 0.12f, g * 0.25f, g * 0.12f), L3Palette.WetStone, V(0, 50, 2))
                    .Add(V(0, g * 0.98f, 0), V(g * 0.05f, g * 0.1f, g * 0.05f), L3Palette.WetStone);
                GameObject go = L2Core.Begin("Estalagmite_" + names[i], mite);
                L2Core.Box(go, V(0, g * 0.35f, 0), V(g * 0.36f, g * 0.7f, g * 0.34f));
                L2Core.Save(go, C, "Estalagmite " + labels[i]);
            }
        }

        // ================================================================== Estruturas da masmorra e caixas

        /// <summary>
        /// Torre arruinada, muro arruinado, coluna e arco da masmorra, escadaria quebrada, estandarte,
        /// caixas, barril, pilha de caixas, correntes e placa de runas.
        /// </summary>
        private static void BuildStructures()
        {
            const AssetCategoryType C = AssetCategoryType.Arquitetura;
            const AssetCategoryType P = AssetCategoryType.Props;

            L2Core.Simple("Torre_Arruinada", new VoxelModel()
                .Add(V(0, 2.5f, 0), V(3.2f, 5f, 3.2f), L3Palette.Brick)
                .Add(V(-0.9f, 5.8f, 0.6f), V(1.4f, 1.6f, 2f), L3Palette.Brick, V(0, 0, 8))
                .Add(V(1.1f, 5.4f, -0.8f), V(1f, 0.9f, 1.6f), L3Palette.BrickDark)
                .Add(V(0, 0.4f, 0), V(3.6f, 0.8f, 3.6f), L3Palette.BrickDark)
                .Add(V(2.2f, 0.3f, 1.4f), V(1f, 0.6f, 0.8f), L3Palette.Brick, V(10, 30, 0)), C, "Torre arruinada", true);

            L2Core.Simple("Muro_Arruinado", new VoxelModel()
                .Add(V(0, 1.2f, 0), V(5f, 2.4f, 0.8f), L3Palette.Brick)
                .Add(V(-1.4f, 2.8f, 0), V(2.2f, 0.8f, 0.8f), L3Palette.Brick)
                .Add(V(1.8f, 2.6f, 0), V(0.9f, 0.5f, 0.75f), L3Palette.BrickDark)
                .Add(V(2.9f, 0.25f, 0.8f), V(0.8f, 0.5f, 0.6f), L3Palette.Brick, V(0, 40, 12)), C, "Muro arruinado", true);

            L2Core.Simple("Coluna_da_Masmorra", new VoxelModel()
                .Add(V(0, 0.25f, 0), V(1.3f, 0.5f, 1.3f), L3Palette.BrickDark)
                .Add(V(0, 2.5f, 0), V(0.9f, 4f, 0.9f), L3Palette.Brick)
                .Add(V(0, 4.7f, 0), V(1.3f, 0.4f, 1.3f), L3Palette.BrickDark)
                .Add(V(0, 2.6f, 0.46f), V(0.3f, 1.4f, 0.02f), L3Palette.Rune), C, "Coluna da masmorra", true);

            // Arco da masmorra (entrada vinda do Nível 2).
            GameObject arch = L2Core.Begin("Arco_da_Masmorra", new VoxelModel()
                .Add(V(-2.4f, 2.4f, 0), V(1.2f, 4.8f, 1.6f), L3Palette.Brick)
                .Add(V(2.4f, 2.4f, 0), V(1.2f, 4.8f, 1.6f), L3Palette.Brick)
                .Add(V(0, 5.2f, 0), V(6f, 1f, 1.8f), L3Palette.BrickDark)
                .Add(V(0, 5.75f, 0.92f), V(1.2f, 0.6f, 0.05f), L3Palette.Rune)
                .Add(V(0, 2.3f, -0.5f), V(3.6f, 4.6f, 0.4f), L3Palette.Void));
            L2Core.Box(arch, V(-2.4f, 2.4f, 0), V(1.2f, 4.8f, 1.6f));
            L2Core.Box(arch, V(2.4f, 2.4f, 0), V(1.2f, 4.8f, 1.6f));
            L2Core.Box(arch, V(0, 2.3f, -0.5f), V(3.6f, 4.6f, 0.4f));
            L2Core.Save(arch, C, "Arco da masmorra");

            VoxelModel stairs = new VoxelModel();
            for (int i = 0; i < 5; i++)
                stairs.Add(V(i % 2 == 0 ? 0 : 0.15f, 0.15f + i * 0.3f, -i * 0.45f - 0.22f), V(3f - (i == 4 ? 1f : 0f), 0.3f, 0.45f), i % 2 == 0 ? L3Palette.Brick : L3Palette.BrickDark);
            stairs.Add(V(1.8f, 0.2f, 0.5f), V(0.7f, 0.4f, 0.5f), L3Palette.Brick, V(0, 30, 10));
            L2Core.Simple("Escadaria_Quebrada", stairs, C, "Escadaria quebrada", true);

            L2Core.Simple("Estandarte_Azul", new VoxelModel()
                .Add(V(0, 0, 0.05f), V(1.3f, 0.1f, 0.1f), L3Palette.WoodDark)
                .Add(V(-0.1f, -0.95f, 0.08f), V(1f, 1.8f, 0.03f), L3Palette.Cloth)
                .Add(V(0.4f, -0.6f, 0.08f), V(0.25f, 1.1f, 0.03f), L3Palette.Cloth)
                .Add(V(-0.1f, -0.7f, 0.1f), V(0.35f, 0.35f, 0.02f), L3Palette.Gold), P, "Estandarte azul", false);

            // Caixas: obstáculos e pontos de apoio (origem na base).
            VoxelModel crate = CrateModel(1f);
            GameObject c1 = L2Core.Begin("Caixa_da_Masmorra", crate);
            L2Core.Box(c1, V(0, 0.5f, 0), V(1f, 1f, 1f));
            L2Core.Save(c1, P, "Caixa da masmorra");
            GameObject c2 = L2Core.Begin("Caixa_Grande", CrateModel(1.5f));
            L2Core.Box(c2, V(0, 0.75f, 0), V(1.5f, 1.5f, 1.5f));
            L2Core.Save(c2, P, "Caixa grande");

            GameObject barrel = L2Core.Begin("Barril_da_Masmorra", new VoxelModel()
                .Add(V(0, 0.5f, 0), V(0.75f, 1f, 0.6f), L3Palette.Wood)
                .Add(V(0, 0.5f, 0), V(0.6f, 1f, 0.75f), L3Palette.Wood)
                .Add(V(0, 0.2f, 0), V(0.8f, 0.07f, 0.8f), L3Palette.Iron)
                .Add(V(0, 0.8f, 0), V(0.8f, 0.07f, 0.8f), L3Palette.Iron));
            L2Core.Box(barrel, V(0, 0.5f, 0), V(0.75f, 1f, 0.75f));
            L2Core.Save(barrel, P, "Barril da masmorra");

            VoxelModel pile = CrateModel(1f);
            pile.Add(V(1.05f, 0.5f, 0.1f), V(1f, 1f, 1f), L3Palette.WoodDark, V(0, 12, 0))
                .Add(V(0.5f, 1.5f, 0.05f), V(1f, 1f, 1f), L3Palette.Wood, V(0, -8, 0));
            L2Core.Simple("Pilha_de_Caixas", pile, P, "Pilha de caixas", true);

            VoxelModel chains = new VoxelModel();
            for (int i = 0; i < 12; i++)
                chains.Add(V(0, -0.18f - i * 0.2f, 0), V(0.08f, 0.2f, 0.04f), L3Palette.Iron, V(0, i % 2 == 0 ? 0 : 90, 0));
            L2Core.Simple("Corrente_Pendurada", chains, P, "Corrente pendurada", false);

            VoxelModel plaque = new VoxelModel().Add(V(0, 0, 0.04f), V(1.6f, 1f, 0.08f), L3Palette.BrickDark);
            plaque.AddFitted(V(0, 0, 0.085f), V(1.4f, 0.8f, 0.02f), L3Palette.Rune);
            GameObject pl = L2Core.Begin("Placa_de_Runas", plaque);
            L2Core.PointLight(pl, "Brilho", V(0, 0, 0.5f), new Color(0.3f, 1f, 1f), 3f, 0.8f);
            L2Core.Save(pl, AssetCategoryType.Inscricoes, "Placa de runas");
        }

        /// <summary>
        /// Caixa de madeira com reforços de ferro.
        /// </summary>
        /// <param name="s">Lado.</param>
        /// <returns>Modelo.</returns>
        public static VoxelModel CrateModel(float s)
        {
            VoxelModel m = new VoxelModel().Add(V(0, s * 0.5f, 0), V(s, s, s), L3Palette.Wood);
            foreach (float x in new[] { -0.45f, 0.45f })
                foreach (float z in new[] { -0.45f, 0.45f })
                    m.Add(V(x * s, s * 0.5f, z * s), V(s * 0.12f, s * 1.01f, s * 0.12f), L3Palette.Iron);
            m.Add(V(0, s * 0.5f, s * 0.505f), V(s * 1.1f, s * 0.1f, s * 0.02f), L3Palette.WoodDark, V(0, 0, 45));
            m.Add(V(0, s * 0.5f, -s * 0.505f), V(s * 1.1f, s * 0.1f, s * 0.02f), L3Palette.WoodDark, V(0, 0, -45));
            return m;
        }

        // ================================================================== Vegetação

        /// <summary>
        /// Musgo luminoso, cogumelos brilhantes e raízes da caverna.
        /// </summary>
        private static void BuildVegetation()
        {
            const AssetCategoryType C = AssetCategoryType.Vegetacao;
            VoxelModel moss = new VoxelModel();
            for (int i = 0; i < 6; i++)
                moss.Add(V(Mathf.Cos(i * 1.9f) * 0.5f, 0.02f, Mathf.Sin(i * 1.9f) * 0.45f), V(0.5f + (i % 3) * 0.15f, 0.04f, 0.4f), L3Palette.Moss, V(0, i * 31, 0));
            L2Core.Simple("Musgo_Luminoso", moss, C, "Musgo luminoso", false);

            VoxelModel shrooms = new VoxelModel();
            for (int i = 0; i < 5; i++)
            {
                float a = i * 2.4f, r = 0.15f + (i % 3) * 0.12f, h = 0.15f + (i % 3) * 0.1f;
                Vector3 p = V(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                shrooms.Add(p + V(0, h * 0.5f, 0), V(0.05f, h, 0.05f), L3Palette.MushroomStem)
                       .Add(p + V(0, h, 0), V(0.2f, 0.07f, 0.2f), L3Palette.Mushroom);
            }
            GameObject sh = L2Core.Begin("Cogumelos_Brilhantes", shrooms);
            L2Core.PointLight(sh, "Brilho", V(0, 0.4f, 0), new Color(0.65f, 0.4f, 1f), 2.5f, 0.7f);
            L2Core.Save(sh, C, "Cogumelos brilhantes");

            VoxelModel roots = new VoxelModel();
            for (int i = 0; i < 6; i++)
            {
                float len = 1f + (i * 37 % 10) / 10f * 2f;
                roots.Add(V(-0.6f + i * 0.24f, -len * 0.5f, (i % 2) * 0.12f), V(0.07f, len, 0.07f), L3Palette.WoodDark, V(0, 0, (i - 3) * 5f));
            }
            L2Core.Simple("Raizes_da_Caverna", roots, C, "Raízes da caverna", false);
        }

        // ================================================================== Iluminação

        /// <summary>
        /// Lanterna de cristal (cinto de John), lampião da masmorra (Mixed) e braseiro de cristal (Mixed).
        /// </summary>
        private static void BuildLighting()
        {
            const AssetCategoryType C = AssetCategoryType.Iluminacao;
            GameObject lantern = L2Core.Begin("Lanterna_de_Cristal", new VoxelModel()
                .Add(Vector3.zero, V(0.14f, 0.2f, 0.14f), L3Palette.Iron)
                .Add(Vector3.zero, V(0.1f, 0.15f, 0.15f), L3Palette.CrystalCore)
                .Add(V(0, 0.13f, 0), V(0.05f, 0.06f, 0.05f), L3Palette.Iron));
            Light l = L2Core.PointLight(lantern, "Luz", V(0, 0.3f, 0.2f), new Color(0.75f, 0.9f, 1f), 9f, 1.9f);
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.7f;
            L2Core.Save(lantern, C, "Lanterna de cristal");

            VoxelModel lamp = new VoxelModel();
            for (int i = 0; i < 6; i++)
                lamp.Add(V(0, -0.15f - i * 0.2f, 0), V(0.06f, 0.2f, 0.04f), L3Palette.Iron, V(0, i % 2 == 0 ? 0 : 90, 0));
            lamp.Add(V(0, -1.45f, 0), V(0.35f, 0.08f, 0.35f), L3Palette.Iron)
                .Add(V(0, -1.65f, 0), V(0.28f, 0.32f, 0.28f), L3Palette.CrystalAmber)
                .Add(V(0, -1.85f, 0), V(0.35f, 0.08f, 0.35f), L3Palette.Iron);
            GameObject lp = L2Core.Begin("Lampiao_da_Masmorra", lamp);
            L2Core.PointLight(lp, "Luz", V(0, -1.65f, 0), new Color(1f, 0.75f, 0.45f), 8f, 1.6f, true, LightmapBakeType.Mixed);
            L2Core.Save(lp, C, "Lampião da masmorra");

            VoxelModel brazier = new VoxelModel()
                .Add(V(0, 0.3f, 0), V(0.5f, 0.6f, 0.5f), L3Palette.BrickDark)
                .Add(V(0, 0.7f, 0), V(1f, 0.25f, 1f), L3Palette.Brick);
            Spike(brazier, V(0, 0.8f, 0), 0.7f, 0.25f, V(0, 0, 8), L3Palette.CrystalCyan);
            Spike(brazier, V(0.2f, 0.8f, 0.1f), 0.5f, 0.2f, V(0, 0, -25), L3Palette.CrystalCyan);
            Spike(brazier, V(-0.2f, 0.8f, -0.1f), 0.45f, 0.18f, V(15, 0, 25), L3Palette.CrystalBlue);
            GameObject bz = L2Core.Begin("Braseiro_de_Cristal", brazier);
            L2Core.Box(bz, V(0, 0.45f, 0), V(1f, 0.9f, 1f));
            L2Core.PointLight(bz, "Luz", V(0, 1.5f, 0), new Color(0.45f, 0.95f, 1f), 7f, 1.8f, false, LightmapBakeType.Mixed);
            L2Core.AddFx(bz, "FX3_Brilho_de_Cristal", V(0, 1.2f, 0));
            bz.AddComponent<CrystalGlow>().emission = new Color(0.2f, 1.3f, 1.4f);
            L2Core.Save(bz, C, "Braseiro de cristal");
        }

        // ================================================================== Tesouros e abismo

        /// <summary>
        /// Artefatos do Nível 3 (Olho de Cristal e Lágrima de Cristal) e o fundo escuro do abismo.
        /// </summary>
        private static void BuildTreasures()
        {
            VoxelModel eye = new VoxelModel()
                .Add(Vector3.zero, V(0.34f, 0.2f, 0.12f), L3Palette.Gold)
                .Add(V(0, 0, 0.05f), V(0.2f, 0.16f, 0.06f), L3Palette.CrystalCore)
                .Add(V(0, 0, 0.085f), V(0.08f, 0.08f, 0.02f), L3Palette.CrystalViolet);
            Artifact("Olho_de_Cristal", "Olho de Cristal", eye);
            VoxelModel tear = new VoxelModel();
            Spike(tear, V(0, -0.1f, 0), 0.28f, 0.16f, Vector3.zero, L3Palette.CrystalCyan);
            HangingSpike(tear, V(0, -0.1f, 0), 0.14f, 0.16f, Vector3.zero, L3Palette.CrystalCyan);
            tear.Add(V(0, 0.25f, 0), V(0.06f, 0.06f, 0.06f), L3Palette.Gold);
            Artifact("Lagrima_de_Cristal", "Lágrima de Cristal", tear);

            GameObject bottom = L2Core.Begin("Fundo_do_Abismo", new VoxelModel().Add(Vector3.zero, V(80f, 1f, 160f), L3Palette.Void));
            L2Core.Save(bottom, AssetCategoryType.Terreno, "Fundo do abismo (Void)");
        }

        /// <summary>
        /// Artefato coletável (gatilho, giro, brilho e luz).
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="artifactName">Nome do artefato.</param>
        /// <param name="model">Modelo.</param>
        private static void Artifact(string id, string artifactName, VoxelModel model)
        {
            GameObject go = new GameObject(id);
            go.tag = GameTags.Artifact;
            go.layer = L2Core.Layer(ParkourLayers.InteractableLayerName);
            GameObject visual = L2Core.Child(go, "Visual", V(0, 0.6f, 0), model);
            Spinner spin = visual.AddComponent<Spinner>();
            spin.degreesPerSecond = V(0, 70f, 0);
            spin.bobAmplitude = 0.1f;
            L2Core.Sphere(go, V(0, 0.6f, 0), 0.55f, true);
            Collectible c = go.AddComponent<Collectible>();
            c.type = CollectibleType.Artifact;
            c.artifactName = artifactName;
            c.pickupEffect = L2Core.L1("FX_Sparkle");
            L2Core.AddFx(go, "FX_Glint", V(0, 0.6f, 0));
            L2Core.PointLight(go, "Brilho", V(0, 0.9f, 0), new Color(0.6f, 0.9f, 1f), 3f, 1.4f);
            L2Core.Save(go, AssetCategoryType.Tesouros, artifactName + " (artefato)");
        }
    }
}
