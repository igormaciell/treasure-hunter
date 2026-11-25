using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Prefabs do percurso de parkour (origem no CENTRO DO TOPO, onde John pisa): plataformas naturais
    /// (rocha, pilares de cristal, estalagmite, estalactite caída, santuário), estruturas da masmorra
    /// (torres e muro), plataformas especiais (móvel, elevador, disco giratório, frágil, cristal instável)
    /// e os elementos do totem e do checkpoint, estalactites cadentes, lascas, portal de saída e marca de pouso.
    /// </summary>
    public static class L3Parkour
    {
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        /// <summary>Largura × profundidade do topo de cada plataforma (usado no gerador e no verificador de saltos).</summary>
        public static Vector2 Footprint(string id)
        {
            switch (id)
            {
                case "Plataforma_Rochosa_P": return new Vector2(2.2f, 2.2f);
                case "Plataforma_Rochosa_M": return new Vector2(3f, 3f);
                case "Plataforma_Rochosa_G": return new Vector2(5f, 4f);
                case "Plataforma_Rochosa_Longa": return new Vector2(10f, 3f);
                case "Pilar_de_Cristal": return new Vector2(2f, 2f);
                case "Pilar_de_Cristal_Fino": return new Vector2(1.5f, 1.5f);
                case "Cristal_Instavel": return new Vector2(1.5f, 1.5f);
                case "Estalagmite_Plataforma": return new Vector2(2.2f, 2.2f);
                case "Estalactite_Caida": return new Vector2(1.4f, 8f);
                case "Plataforma_do_Santuario": return new Vector2(12f, 10f);
                case "Torre_da_Masmorra": return new Vector2(3f, 3f);
                case "Torre_Alta": return new Vector2(3f, 3f);
                case "Muro_da_Masmorra": return new Vector2(1.2f, 6f);
                case "Plataforma_Movel": return new Vector2(2.5f, 2.5f);
                case "Elevador_de_Cristal": return new Vector2(2.5f, 2.5f);
                case "Plataforma_Fragil": return new Vector2(2.5f, 2.5f);
                case "Disco_Giratorio": return new Vector2(5f, 5f);
                default: return new Vector2(2f, 2f);
            }
        }

        /// <summary>
        /// Cria todos os prefabs do percurso.
        /// </summary>
        public static void BuildAll()
        {
            BuildNaturalPlatforms();
            BuildStructures();
            BuildSpecialPlatforms();
            BuildHazardsAndMisc();
            BuildTotem();
        }

        // ================================================================== Plataformas naturais

        /// <summary>
        /// Rochas (P, M, G e longa), pilares de cristal (normal e fino), estalagmite, estalactite caída e santuário.
        /// </summary>
        private static void BuildNaturalPlatforms()
        {
            const AssetCategoryType C = AssetCategoryType.Arquitetura;
            RockPlatform("Plataforma_Rochosa_P", "Plataforma de rocha pequena", 2.2f, 2.2f, 5f, C);
            RockPlatform("Plataforma_Rochosa_M", "Plataforma de rocha média", 3f, 3f, 7f, C);
            RockPlatform("Plataforma_Rochosa_G", "Plataforma de rocha grande", 5f, 4f, 9f, C);
            RockPlatform("Plataforma_Rochosa_Longa", "Borda de rocha (saída)", 10f, 3f, 12f, C);

            CrystalPillar("Pilar_de_Cristal", "Pilar de cristal (plataforma)", 2f, 7f, L3Palette.CrystalBlue, new Color(0.25f, 0.6f, 1.6f), new Color(0.45f, 0.75f, 1f), false);
            CrystalPillar("Pilar_de_Cristal_Fino", "Pilar de cristal fino (plataforma)", 1.5f, 6f, L3Palette.CrystalCyan, new Color(0.2f, 1.3f, 1.4f), new Color(0.4f, 1f, 1f), false);

            // Estalagmite quebrada: topo plano, corpo descendo para o abismo.
            VoxelModel mite = new VoxelModel()
                .Add(V(0, -0.25f, 0), V(2.2f, 0.5f, 2.2f), L3Palette.WetStone)
                .Add(V(0, -2.3f, 0), V(2f, 3.6f, 2f), L3Palette.Rock, V(0, 15, 0))
                .Add(V(0.2f, -6f, 0.1f), V(2.6f, 4.5f, 2.5f), L3Palette.Rock, V(0, 40, 6))
                .Add(V(-0.2f, -10.5f, 0), V(3.4f, 5f, 3.2f), L3Palette.RockDark, V(5, 70, 0))
                .Add(V(0.6f, -0.35f, 0.6f), V(0.35f, 0.3f, 0.35f), L3Palette.WetStone, V(20, 30, 0));
            GameObject go = L2Core.Begin("Estalagmite_Plataforma", mite);
            go.tag = GameTags.Ground;
            L2Core.Box(go, V(0, -0.25f, 0), V(2.2f, 0.5f, 2.2f));
            L2Core.Box(go, V(0, -2.3f, 0), V(1.9f, 3.6f, 1.9f));
            EdgeCrystals(go, 2.2f, 2.2f, L3Palette.CrystalCyan);
            L2Core.Save(go, C, "Estalagmite quebrada (plataforma)");

            // Estalactite caída: ponte estreita de 8 m (equilíbrio).
            VoxelModel tite = new VoxelModel()
                .Add(V(0, -0.2f, 0), V(1.4f, 0.4f, 8f), L3Palette.WetStone)
                .Add(V(0, -0.9f, 0), V(1.3f, 1f, 8.2f), L3Palette.Rock)
                .Add(V(0, -0.9f, 0), V(0.9f, 1.5f, 8f), L3Palette.RockDark, V(0, 0, 45))
                .Add(V(0, -0.7f, 4.6f), V(0.8f, 0.8f, 1.4f), L3Palette.Rock, V(0, 0, 45))
                .Add(V(0, -0.65f, 5.4f), V(0.4f, 0.4f, 0.8f), L3Palette.WetStone, V(0, 0, 45))
                .Add(V(0, -0.8f, -4.3f), V(1.6f, 1.4f, 0.8f), L3Palette.RockDark, V(10, 20, 0));
            go = L2Core.Begin("Estalactite_Caida", tite);
            go.tag = GameTags.Ground;
            L2Core.Box(go, V(0, -0.2f, 0), V(1.4f, 0.4f, 8f));
            L2Core.Box(go, V(0, -0.9f, 0), V(1.2f, 1f, 8f));
            L2Core.Save(go, C, "Estalactite caída (ponte)");

            // Santuário: platô grande e seguro, com círculo de runas no chão.
            VoxelModel shrine = new VoxelModel()
                .Add(V(0, -0.4f, 0), V(12f, 0.8f, 10f), L3Palette.WetStone)
                .Add(V(0, -3f, 0), V(11f, 4.4f, 9f), L3Palette.Rock, V(0, 3, 0))
                .Add(V(0.5f, -7.5f, 0.3f), V(8f, 5f, 6.5f), L3Palette.RockDark, V(0, 20, 0))
                .Add(V(-0.5f, -11.5f, 0), V(4.5f, 4f, 3.5f), L3Palette.RockDark, V(0, 45, 5));
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                shrine.Add(V(Mathf.Cos(a) * 2.2f, 0.005f, 2.5f + Mathf.Sin(a) * 2.2f), V(0.55f, 0.01f, 0.2f), L3Palette.Rune, V(0, -a * Mathf.Rad2Deg + 90f, 0));
            }
            go = L2Core.Begin("Plataforma_do_Santuario", shrine);
            go.tag = GameTags.Ground;
            L2Core.Box(go, V(0, -0.4f, 0), V(12f, 0.8f, 10f));
            L2Core.Box(go, V(0, -3f, 0), V(11f, 4.4f, 9f));
            EdgeCrystals(go, 12f, 10f, L3Palette.CrystalBlue);
            L2Core.Save(go, C, "Platô do santuário");
        }

        /// <summary>
        /// Plataforma de rocha: laje de cascalho no topo e corpo afunilando para baixo (sem degraus que segurem quem cai).
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="display">Nome legível.</param>
        /// <param name="w">Largura do topo.</param>
        /// <param name="d">Profundidade do topo.</param>
        /// <param name="depth">Altura do corpo.</param>
        /// <param name="cat">Categoria.</param>
        private static void RockPlatform(string id, string display, float w, float d, float depth, AssetCategoryType cat)
        {
            VoxelModel m = new VoxelModel()
                .Add(V(0, -0.3f, 0), V(w, 0.6f, d), L3Palette.Gravel)
                .Add(V(0.04f, -0.6f - depth * 0.2f, 0), V(w * 0.94f, depth * 0.4f, d * 0.94f), L3Palette.Rock, V(0, 4, 0))
                .Add(V(-0.1f, -0.6f - depth * 0.55f, 0.05f), V(w * 0.7f, depth * 0.35f, d * 0.68f), L3Palette.RockDark, V(0, 22, 3))
                .Add(V(0.05f, -0.6f - depth * 0.85f, 0), V(w * 0.38f, depth * 0.3f, d * 0.38f), L3Palette.RockDark, V(4, 50, 0));
            GameObject go = L2Core.Begin(id, m);
            go.tag = GameTags.Ground;
            L2Core.Box(go, V(0, -0.3f, 0), V(w, 0.6f, d));
            L2Core.Box(go, V(0, -0.6f - depth * 0.2f, 0), V(w * 0.92f, depth * 0.4f, d * 0.92f));
            EdgeCrystals(go, w, d, L3Palette.CrystalCyan);
            L2Core.Save(go, cat, display);
        }

        /// <summary>
        /// Pequenos cristais brilhantes nas laterais, logo abaixo do topo: deixam as bordas visíveis no escuro.
        /// </summary>
        /// <param name="go">Plataforma.</param>
        /// <param name="w">Largura.</param>
        /// <param name="d">Profundidade.</param>
        /// <param name="mat">Material.</param>
        private static void EdgeCrystals(GameObject go, float w, float d, Material mat)
        {
            VoxelModel m = new VoxelModel();
            float hx = w * 0.5f, hz = d * 0.5f;
            L3Props.HangingSpike(m, V(hx - 0.2f, -0.35f, hz), 0.5f, 0.12f, V(-35, 0, 0), mat);
            L3Props.HangingSpike(m, V(-hx + 0.3f, -0.35f, hz), 0.4f, 0.1f, V(-40, 0, 10), mat);
            L3Props.HangingSpike(m, V(-hx, -0.35f, -hz + 0.3f), 0.45f, 0.11f, V(0, 0, 38), mat);
            L3Props.HangingSpike(m, V(hx, -0.35f, -hz + 0.5f), 0.4f, 0.1f, V(10, 0, -38), mat);
            L2Core.Child(go, "Cristais_da_Borda", Vector3.zero, m);
        }

        /// <summary>
        /// Pilar de cristal com topo octogonal: luz interna, brilho de partículas e pulso de energia (CrystalGlow).
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="display">Nome legível.</param>
        /// <param name="size">Lado do topo.</param>
        /// <param name="depth">Altura do corpo.</param>
        /// <param name="mat">Material do cristal.</param>
        /// <param name="glow">Emissão.</param>
        /// <param name="lightColor">Cor da luz.</param>
        /// <param name="unstable">true para o cristal que desmorona.</param>
        /// <returns>O objeto (ainda não salvo).</returns>
        private static GameObject CrystalPillar(string id, string display, float size, float depth, Material mat, Color glow, Color lightColor, bool unstable)
        {
            VoxelModel m = new VoxelModel()
                .Add(V(0, -0.25f, 0), V(size, 0.5f, size), mat)
                .Add(V(0, -0.25f, 0), V(size * 0.8f, 0.48f, size * 0.8f), mat, V(0, 45, 0))
                .Add(V(0, -0.5f - depth * 0.25f, 0), V(size * 0.8f, depth * 0.5f, size * 0.8f), mat, V(0, 20, 0))
                .Add(V(0, -0.5f - depth * 0.25f, 0), V(size * 0.62f, depth * 0.5f, size * 0.62f), mat, V(0, 65, 0))
                .Add(V(0.05f, -0.5f - depth * 0.68f, 0), V(size * 0.45f, depth * 0.38f, size * 0.45f), mat, V(0, 40, 3))
                .Add(V(0.05f, -0.5f - depth * 0.95f, 0), V(size * 0.18f, depth * 0.18f, size * 0.18f), mat, V(0, 10, 3));
            L3Props.HangingSpike(m, V(size * 0.4f, -0.4f, 0), depth * 0.35f, size * 0.2f, V(0, 0, -25), mat);
            L3Props.HangingSpike(m, V(-size * 0.35f, -0.45f, size * 0.2f), depth * 0.28f, size * 0.17f, V(15, 0, 25), mat);
            GameObject go = new GameObject(id);
            go.tag = unstable ? ParkourTags.Unstable : ParkourTags.Crystal;
            GameObject visual = L2Core.Child(go, "Visual", Vector3.zero, m);
            L2Core.Box(go, V(0, -0.25f, 0), V(size, 0.5f, size));
            L2Core.Box(go, V(0, -0.5f - depth * 0.25f, 0), V(size * 0.75f, depth * 0.5f, size * 0.75f));
            L2Core.PointLight(visual, "Luz", V(0, -1.2f, 0), lightColor, 7f, 1.6f, false, LightmapBakeType.Mixed);
            L2Core.AddFx(go, "FX3_Brilho_de_Cristal", V(0, -0.2f, 0));
            CrystalGlow g = go.AddComponent<CrystalGlow>();
            g.emission = glow;
            g.pulseSpeed = 0.4f;
            if (unstable) return go;
            L2Core.Save(go, AssetCategoryType.Arquitetura, display);
            return null;
        }

        // ================================================================== Estruturas da masmorra

        /// <summary>
        /// Torre (baixa e alta) e muro da masmorra: estruturas artificiais usadas como plataformas e bordas para escalar.
        /// </summary>
        private static void BuildStructures()
        {
            const AssetCategoryType C = AssetCategoryType.Arquitetura;
            Tower("Torre_da_Masmorra", "Torre da masmorra", 12f, false);
            Tower("Torre_Alta", "Torre alta da masmorra", 16f, true);

            VoxelModel wall = new VoxelModel()
                .Add(V(0, -5f, 0), V(1.2f, 10f, 6f), L3Palette.Brick)
                .Add(V(0, -0.15f, 0), V(1.25f, 0.3f, 6.05f), L3Palette.BrickDark)
                .Add(V(0.3f, -10.5f, 3.2f), V(1f, 2f, 1.2f), L3Palette.Brick, V(10, 0, 20))
                .Add(V(0, -4f, 3.05f), V(0.9f, 1.4f, 0.1f), L3Palette.Rune)
                .Add(V(0.62f, -3f, -1f), V(0.05f, 2f, 1.2f), L3Palette.Moss);
            GameObject go = L2Core.Begin("Muro_da_Masmorra", wall);
            go.tag = GameTags.Ground;
            L2Core.Box(go, V(0, -5f, 0), V(1.2f, 10f, 6f));
            L2Core.Save(go, C, "Muro da masmorra (bordas para escalar)");
        }

        /// <summary>
        /// Torre quadrada de tijolos (3 × 3 m no topo) descendo para o abismo.
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="display">Nome legível.</param>
        /// <param name="height">Altura do corpo.</param>
        /// <param name="broken">Com pedaços quebrados nas laterais.</param>
        private static void Tower(string id, string display, float height, bool broken)
        {
            VoxelModel m = new VoxelModel()
                .Add(V(0, -height * 0.5f, 0), V(3f, height, 3f), L3Palette.Brick)
                .Add(V(0, -0.45f, 0), V(3.2f, 0.3f, 3.2f), L3Palette.BrickDark)
                .Add(V(0, -height * 0.35f, 0), V(3.15f, 0.4f, 3.15f), L3Palette.BrickDark)
                .Add(V(0, -2.2f, 1.51f), V(0.6f, 1f, 0.05f), L3Palette.Void)
                .Add(V(1.51f, -2.6f, 0), V(0.05f, 1f, 0.6f), L3Palette.Void);
            if (broken)
                m.Add(V(1.6f, -height * 0.6f, 0.8f), V(1.2f, 2f, 1f), L3Palette.Brick, V(0, 20, 15))
                 .Add(V(-1.4f, -height * 0.8f, -0.6f), V(1f, 1.5f, 1.4f), L3Palette.BrickDark, V(10, -15, -10));
            GameObject go = L2Core.Begin(id, m);
            go.tag = GameTags.Ground;
            L2Core.Box(go, V(0, -height * 0.5f, 0), V(3f, height, 3f));
            L2Core.PointLight(go, "Luz_da_Janela", V(0, -2.2f, 1.9f), new Color(1f, 0.7f, 0.4f), 4f, 0.8f, true, LightmapBakeType.Mixed);
            L2Core.Save(go, AssetCategoryType.Arquitetura, display);
        }

        // ================================================================== Plataformas especiais

        /// <summary>
        /// Plataforma móvel, elevador de cristal, disco giratório (torque), plataforma frágil e cristal instável.
        /// </summary>
        private static void BuildSpecialPlatforms()
        {
            const AssetCategoryType C = AssetCategoryType.Mecanismos;

            // Plataforma móvel (vai e volta; deslocamento definido na cena).
            VoxelModel slab = new VoxelModel()
                .Add(V(0, -0.25f, 0), V(2.5f, 0.5f, 2.5f), L3Palette.BrickDark)
                .Add(V(0, -0.02f, 0), V(2.3f, 0.04f, 2.3f), L3Palette.WetStone)
                .Add(V(0, -0.52f, 0), V(1.6f, 0.1f, 1.6f), L3Palette.Rune);
            L3Props.HangingSpike(slab, V(0, -0.55f, 0), 1.2f, 0.4f, Vector3.zero, L3Palette.CrystalCyan);
            GameObject go = L2Core.Begin("Plataforma_Movel", slab);
            go.tag = ParkourTags.Unstable;
            L2Core.Box(go, V(0, -0.25f, 0), V(2.5f, 0.5f, 2.5f));
            go.AddComponent<Rigidbody>().isKinematic = true;
            go.AddComponent<PlatformMotion>();
            go.AddComponent<MovingPlatform>();
            L2Core.PointLight(go, "Luz", V(0, -1f, 0), new Color(0.4f, 1f, 1f), 5f, 1.2f);
            L2Core.Save(go, C, "Plataforma móvel");

            // Elevador de cristal (sobe e desce).
            VoxelModel lift = new VoxelModel()
                .Add(V(0, -0.25f, 0), V(2.5f, 0.5f, 2.5f), L3Palette.CrystalViolet)
                .Add(V(0, -0.25f, 0), V(2f, 0.48f, 2f), L3Palette.CrystalViolet, V(0, 45, 0))
                .Add(V(0, -0.02f, 0), V(2.2f, 0.05f, 2.2f), L3Palette.WetStone);
            L3Props.HangingSpike(lift, V(0, -0.5f, 0), 1.8f, 0.7f, Vector3.zero, L3Palette.CrystalViolet);
            go = L2Core.Begin("Elevador_de_Cristal", lift);
            go.tag = ParkourTags.Unstable;
            L2Core.Box(go, V(0, -0.25f, 0), V(2.5f, 0.5f, 2.5f));
            go.AddComponent<Rigidbody>().isKinematic = true;
            go.AddComponent<PlatformMotion>();
            MovingPlatform mp = go.AddComponent<MovingPlatform>();
            mp.offset = V(0, 3f, 0);
            mp.speed = 1.1f;
            mp.waitTime = 1.2f;
            L2Core.PointLight(go, "Luz", V(0, -1f, 0), new Color(0.75f, 0.5f, 1f), 6f, 1.5f);
            go.AddComponent<CrystalGlow>().emission = new Color(0.8f, 0.3f, 1.6f);
            L2Core.Save(go, C, "Elevador de cristal");

            // Disco giratório: dodecágono de pedra (3 caixas giradas), Rigidbody com torque.
            GameObject disc = new GameObject("Disco_Giratorio");
            disc.tag = ParkourTags.Unstable;
            VoxelModel dm = new VoxelModel();
            for (int i = 0; i < 3; i++) dm.Add(V(0, -0.25f, 0), V(5f, 0.5f, 5f), i == 1 ? L3Palette.WetStone : L3Palette.Gravel, V(0, i * 30, 0));
            dm.Add(V(0, -0.005f, 0), V(1f, 0.02f, 1f), L3Palette.Rune, V(0, 45, 0))
              .Add(V(0, -1.5f, 0), V(1.2f, 2f, 1.2f), L3Palette.RockDark, V(0, 45, 0));
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                L3Props.HangingSpike(dm, V(Mathf.Cos(a) * 2.2f, -0.45f, Mathf.Sin(a) * 2.2f), 0.7f, 0.18f, Vector3.zero, L3Palette.CrystalAmber);
            }
            L2Core.Child(disc, "Visual", Vector3.zero, dm);
            for (int i = 0; i < 3; i++)
            {
                GameObject part = new GameObject("Colisor_" + (i + 1));
                part.transform.SetParent(disc.transform, false);
                part.transform.localRotation = Quaternion.Euler(0, i * 30, 0);
                L2Core.Box(part, V(0, -0.25f, 0), V(5f, 0.5f, 5f));
            }
            Rigidbody drb = disc.AddComponent<Rigidbody>();
            drb.mass = 500f;
            disc.AddComponent<PlatformMotion>();
            disc.AddComponent<SpinningPlatform>();
            L2Core.PointLight(disc, "Luz", V(0, -1.2f, 0), new Color(1f, 0.75f, 0.35f), 6f, 1.4f);
            L2Core.Save(disc, C, "Disco giratório (torque)");

            // Plataforma frágil: rachaduras âmbar avisam que ela desmorona.
            GameObject frag = new GameObject("Plataforma_Fragil");
            frag.tag = ParkourTags.Unstable;
            VoxelModel fm = new VoxelModel()
                .Add(V(0, -0.3f, 0), V(2.5f, 0.6f, 2.5f), L3Palette.Gravel)
                .Add(V(0, -1.2f, 0), V(2f, 1.4f, 2f), L3Palette.Rock, V(0, 20, 0))
                .Add(V(0, -2.3f, 0), V(1f, 1f, 1f), L3Palette.RockDark, V(0, 50, 0));
            for (int i = 0; i < 5; i++)
                fm.Add(V(-0.8f + i * 0.4f, 0.005f, (i % 2 == 0 ? -0.3f : 0.4f)), V(0.9f, 0.01f, 0.06f), L3Palette.CrystalAmber, V(0, i * 37 - 60, 0));
            GameObject fv = L2Core.Child(frag, "Visual", Vector3.zero, fm);
            L2Core.Box(frag, V(0, -0.3f, 0), V(2.5f, 0.6f, 2.5f));
            frag.AddComponent<Rigidbody>().isKinematic = true;
            CrumblingPlatform cp = frag.AddComponent<CrumblingPlatform>();
            cp.visual = fv.transform;
            cp.crackEffects = new[] { L2Core.Get("FX3_Rachadura") };
            cp.breakEffects = new[] { L2Core.Get("FX3_Desmoronamento"), L2Core.Get("FX3_Pedrinhas") };
            L2Core.Save(frag, C, "Plataforma frágil (desmorona)");

            // Cristal instável: pilar fino que se estilhaça pouco depois de pisar.
            GameObject unstable = CrystalPillar("Cristal_Instavel", "", 1.5f, 6f, L3Palette.CrystalPink, new Color(1.4f, 0.35f, 0.9f), new Color(1f, 0.5f, 0.8f), true);
            unstable.AddComponent<Rigidbody>().isKinematic = true;
            CrumblingPlatform uc = unstable.AddComponent<CrumblingPlatform>();
            uc.visual = unstable.transform.Find("Visual");
            uc.shatter = true;
            uc.crumbleDelay = 0.75f;
            uc.respawnDelay = 3.5f;
            uc.crackEffects = new[] { L2Core.Get("FX3_Rachadura") };
            uc.breakEffects = new[] { L2Core.Get("FX3_Estilhacos_de_Cristal") };
            L2Core.Save(unstable, C, "Cristal instável (estilhaça)");
        }

        // ================================================================== Totem e checkpoint

        /// <summary>
        /// Modelo do totem: base de pedra, haste com runas, coroa dourada e cristal no topo (separado, gira).
        /// </summary>
        /// <param name="mat">Material da pedra.</param>
        /// <param name="gold">Material da coroa.</param>
        /// <returns>Modelo (sem o cristal).</returns>
        private static VoxelModel TotemBody(Material mat, Material gold)
        {
            return new VoxelModel()
                .Add(V(0, 0.1f, 0), V(0.6f, 0.2f, 0.6f), mat)
                .Add(V(0, 0.7f, 0), V(0.22f, 1.1f, 0.22f), mat)
                .Add(V(0, 0.7f, 0), V(0.18f, 1.12f, 0.18f), mat, V(0, 45, 0))
                .Add(V(0, 1.3f, 0), V(0.42f, 0.12f, 0.42f), gold)
                .Add(V(0.17f, 1.42f, 0), V(0.06f, 0.18f, 0.06f), gold)
                .Add(V(-0.17f, 1.42f, 0), V(0.06f, 0.18f, 0.06f), gold)
                .Add(V(0, 1.42f, 0.17f), V(0.06f, 0.18f, 0.06f), gold)
                .Add(V(0, 1.42f, -0.17f), V(0.06f, 0.18f, 0.06f), gold);
        }

        /// <summary>Cristal do topo do totem.</summary>
        /// <param name="mat">Material.</param>
        /// <returns>Modelo.</returns>
        private static VoxelModel TotemCrystalTop(Material mat)
        {
            VoxelModel m = new VoxelModel();
            L3Props.Spike(m, V(0, -0.15f, 0), 0.32f, 0.18f, Vector3.zero, mat);
            L3Props.HangingSpike(m, V(0, -0.15f, 0), 0.16f, 0.18f, Vector3.zero, mat);
            return m;
        }

        /// <summary>
        /// Totem colocado, totem fantasma, cristal especial com o totem e a pedra de checkpoint.
        /// </summary>
        private static void BuildTotem()
        {
            int totemLayer = L2Core.Layer(ParkourLayers.TotemLayerName);
            int interactable = L2Core.Layer(ParkourLayers.InteractableLayerName);

            // Totem de Checkpoint (colocado pelo jogador).
            GameObject totem = new GameObject("Totem_de_Checkpoint");
            totem.tag = ParkourTags.Totem;
            GameObject visual = L2Core.Child(totem, "Visual", Vector3.zero, TotemBody(L3Palette.TotemStone, L3Palette.Gold));
            GameObject crystal = L2Core.Child(visual, "Cristal_Totem", V(0, 1.7f, 0), TotemCrystalTop(L3Palette.TotemGlow));
            Light glow = L2Core.PointLight(visual, "Luz", V(0, 1.8f, 0.2f), new Color(0.45f, 1f, 0.95f), 7f, 2.4f);
            L2Core.AddFx(visual, "FX3_Brilho_de_Cristal", V(0, 1.6f, 0));
            L2Core.Sphere(totem, V(0, 0.8f, 0), 0.5f, true);
            CheckpointTotem ct = totem.AddComponent<CheckpointTotem>();
            ct.visual = visual.transform;
            ct.crystal = crystal.transform;
            ct.glowLight = glow;
            ct.activateEffects = new[] { L2Core.Get("FX3_Ativacao_do_Totem") };
            ct.removeEffects = new[] { L2Core.Get("FX3_Poeira_de_Pouso"), L2Core.Get("FX3_Rachadura") };
            ct.respawnEffects = new[] { L2Core.Get("FX3_Reaparecimento") };
            L2Core.SetLayerRecursive(totem, totemLayer);
            L2Core.Save(totem, AssetCategoryType.Mecanismos, "Totem de Checkpoint");

            // Totem fantasma (prévia de posicionamento, verde/vermelho).
            GameObject ghost = new GameObject("Totem_Fantasma");
            ghost.tag = ParkourTags.Totem;
            L2Core.Child(ghost, "Visual", Vector3.zero, TotemBody(L3Palette.Ghost, L3Palette.Ghost));
            L2Core.Child(ghost, "Cristal", V(0, 1.7f, 0), TotemCrystalTop(L3Palette.Ghost));
            L2Core.Child(ghost, "Anel", Vector3.zero, new VoxelModel().AddFitted(V(0, 0.02f, 0), V(1.2f, 0.01f, 1.2f), L3Palette.Marker));
            L2Core.PointLight(ghost, "Luz", V(0, 1f, 0), new Color(0.3f, 1f, 0.45f), 3f, 1.2f);
            L2Core.SetLayerRecursive(ghost, totemLayer);
            L2Core.Save(ghost, AssetCategoryType.Interface, "Totem fantasma (prévia de posição)");

            // Item do totem (aparece quando o cristal quebra).
            GameObject item = new GameObject("Totem_Item");
            item.tag = ParkourTags.Totem;
            GameObject itemVisual = L2Core.Child(item, "Visual_Item", V(0, 0.4f, 0), TotemBody(L3Palette.TotemStone, L3Palette.Gold));
            L2Core.Child(itemVisual, "Cristal_Item", V(0, 1.7f, 0), TotemCrystalTop(L3Palette.TotemGlow));
            Spinner spin = itemVisual.AddComponent<Spinner>();
            spin.degreesPerSecond = V(0, 60, 0);
            spin.bobAmplitude = 0.1f;
            L2Core.PointLight(itemVisual, "Luz", V(0, 1.4f, 0.4f), new Color(1f, 0.9f, 0.55f), 6f, 2.2f);
            L2Core.AddFx(item, "FX_Glint", V(0, 1.4f, 0));
            L2Core.Sphere(item, V(0, 1f, 0), 0.9f, true);
            TotemPickup pickup = item.AddComponent<TotemPickup>();
            pickup.visual = itemVisual.transform;
            pickup.prompt = "Pegar o Totem de Checkpoint";
            pickup.pickupEffects = new[] { L2Core.Get("FX3_Coleta_do_Totem"), L2Core.L1("FX_Sparkle") };
            L2Core.SetLayerRecursive(item, interactable);

            // Cristal especial (dourado) que guarda o totem.
            GameObject special = new GameObject("Cristal_do_Totem");
            special.tag = ParkourTags.Crystal;
            VoxelModel sm = new VoxelModel()
                .Add(V(0, 0.15f, 0), V(2.4f, 0.3f, 2.4f), L3Palette.BrickDark)
                .Add(V(0, 0.32f, 0), V(2f, 0.05f, 2f), L3Palette.Rune);
            GameObject pedestal = L2Core.Child(special, "Pedestal", Vector3.zero, sm);
            L2Core.Box(pedestal, V(0, 0.15f, 0), V(2.4f, 0.3f, 2.4f));
            VoxelModel shell = new VoxelModel();
            L3Props.Spike(shell, V(0, 0.3f, 0), 2.6f, 0.95f, V(0, 0, 3), L3Palette.CrystalAmber);
            L3Props.Spike(shell, V(0.45f, 0.3f, 0.15f), 1.6f, 0.55f, V(0, 0, -22), L3Palette.CrystalAmber);
            L3Props.Spike(shell, V(-0.4f, 0.3f, -0.2f), 1.8f, 0.6f, V(-10, 0, 24), L3Palette.CrystalCore);
            L3Props.Spike(shell, V(0.1f, 0.3f, 0.5f), 1.2f, 0.45f, V(28, 0, 6), L3Palette.CrystalAmber);
            GameObject intact = L2Core.Child(special, "Inteiro", Vector3.zero, shell);
            L2Core.Box(intact, V(0, 1.6f, 0), V(1.4f, 2.6f, 1.4f));
            Light core = L2Core.PointLight(special, "Luz_do_Nucleo", V(0, 1.6f, 1.2f), new Color(1f, 0.8f, 0.45f), 9f, 2.4f, false, LightmapBakeType.Mixed);
            L2Core.AddFx(special, "FX3_Brilho_de_Cristal", V(0, 1.6f, 0));
            item.transform.SetParent(special.transform, false);
            item.transform.localPosition = V(0, 0.3f, 0);
            L2Core.Sphere(special, V(0, 1.2f, 0), 2f, true);
            TotemCrystal tc = special.AddComponent<TotemCrystal>();
            tc.intactVisual = intact;
            tc.totemItem = item;
            tc.coreLight = core;
            tc.prompt = "Tocar o cristal especial";
            tc.firstTimeTip = "Este cristal pulsa de um jeito diferente... algo brilha lá dentro. Aperte [E].";
            tc.shardPrefabs = new[] { L2Core.Get("Lasca_de_Cristal_A"), L2Core.Get("Lasca_de_Cristal_B"), L2Core.Get("Lasca_de_Cristal_C") };
            tc.crackEffects = new[] { L2Core.Get("FX3_Rachadura") };
            tc.breakEffects = new[] { L2Core.Get("FX3_Estilhacos_de_Cristal"), L2Core.Get("FX3_Fumaca_de_Queda") };
            tc.highlightLight = L2Core.PointLight(special, "Destaque", V(0, 3.2f, 0), new Color(1f, 0.85f, 0.5f), 4f, 1.5f);
            L2Core.Save(special, AssetCategoryType.Mecanismos, "Cristal do totem (quebra e libera o totem)");

            // Pedra de checkpoint (runas acendem).
            GameObject stone = L2Core.Begin("Pedra_de_Checkpoint", new VoxelModel()
                .Add(V(0, 0.15f, 0), V(1.3f, 0.3f, 0.9f), L3Palette.BrickDark)
                .Add(V(0, 1.05f, 0), V(0.8f, 1.6f, 0.45f), L3Palette.TotemStone, V(0, 0, 2))
                .Add(V(0, 1.95f, 0), V(0.6f, 0.25f, 0.4f), L3Palette.TotemStone)
                .Add(V(0, 1.1f, 0.24f), V(0.5f, 1.1f, 0.02f), L3Palette.Rune));
            L2Core.Box(stone, V(0, 1f, 0), V(0.9f, 2f, 0.55f));
            GameObject trigger = new GameObject("Gatilho");
            trigger.transform.SetParent(stone.transform, false);
            L2Core.Box(trigger, V(0, 1f, 0.8f), V(5f, 2.5f, 3f), true);
            GameObject respawn = new GameObject("Renascimento");
            respawn.transform.SetParent(stone.transform, false);
            respawn.transform.localPosition = V(0, 0.05f, 1.4f);
            CheckpointStone cs = trigger.AddComponent<CheckpointStone>();
            cs.respawnPoint = respawn.transform;
            cs.label = "Santuário";
            cs.runeLight = L2Core.PointLight(stone, "Runas", V(0, 1.2f, 0.6f), new Color(0.4f, 1f, 1f), 5f, 0.4f);
            cs.activateEffects = new[] { L2Core.Get("FX3_Ativacao_do_Totem") };
            L2Core.Save(stone, AssetCategoryType.Mecanismos, "Pedra de checkpoint");
        }

        // ================================================================== Perigos e outros

        /// <summary>
        /// Lascas de cristal (vetor de 3), estalactites cadentes (vetor de 3, FallingRock do Nível 1),
        /// portal de saída e marca de pouso.
        /// </summary>
        private static void BuildHazardsAndMisc()
        {
            int interactable = L2Core.Layer(ParkourLayers.InteractableLayerName);
            Material[] shardMats = { L3Palette.CrystalCyan, L3Palette.CrystalBlue, L3Palette.CrystalAmber };
            string[] suffix = { "A", "B", "C" };
            for (int i = 0; i < 3; i++)
            {
                VoxelModel m = new VoxelModel();
                L3Props.Spike(m, V(0, -0.15f, 0), 0.3f + i * 0.08f, 0.12f + i * 0.03f, V(i * 20, 0, 0), shardMats[i]);
                GameObject go = L2Core.Begin("Lasca_de_Cristal_" + suffix[i], m);
                L2Core.Box(go, V(0, 0f, 0), V(0.15f + i * 0.03f, 0.35f + i * 0.08f, 0.15f + i * 0.03f));
                Rigidbody rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.3f;
                go.layer = interactable;
                L2Core.Save(go, AssetCategoryType.Efeitos, "Lasca de cristal " + suffix[i]);
            }

            float[] sizes = { 1.4f, 2.2f, 3f };
            string[] names = { "P", "M", "G" };
            for (int i = 0; i < 3; i++)
            {
                float h = sizes[i];
                VoxelModel m = new VoxelModel()
                    .Add(V(0, -h * 0.2f, 0), V(h * 0.34f, h * 0.4f, h * 0.32f), L3Palette.RockDark)
                    .Add(V(0, -h * 0.55f, 0), V(h * 0.22f, h * 0.35f, h * 0.22f), L3Palette.Rock, V(0, 20, 0))
                    .Add(V(0, -h * 0.85f, 0), V(h * 0.1f, h * 0.3f, h * 0.1f), L3Palette.WetStone, V(0, 45, 0));
                GameObject go = L2Core.Begin("Estalactite_Cadente_" + names[i], m);
                go.tag = GameTags.Rock;
                L2Core.Box(go, V(0, -h * 0.4f, 0), V(h * 0.3f, h * 0.8f, h * 0.3f));
                Rigidbody rb = go.AddComponent<Rigidbody>();
                rb.mass = 20f * h;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                FallingRock fr = go.AddComponent<FallingRock>();
                fr.damage = 1;
                fr.initialDownSpeed = 4f;
                fr.spin = 2f;
                fr.explosionPrefab = L2Core.L1("FX_Explosion");
                fr.pebblesPrefab = L2Core.Get("FX3_Pedrinhas");
                L2Core.Save(go, AssetCategoryType.Armadilhas, "Estalactite cadente " + names[i]);
            }

            // Portal de saída (arco da masmorra com luz das profundezas).
            GameObject portal = L2Core.Begin("Portal_de_Saida", new VoxelModel()
                .Add(V(-2.2f, 2.2f, 0), V(1f, 4.4f, 1.4f), L3Palette.Brick)
                .Add(V(2.2f, 2.2f, 0), V(1f, 4.4f, 1.4f), L3Palette.Brick)
                .Add(V(0, 4.8f, 0), V(5.4f, 0.9f, 1.6f), L3Palette.BrickDark)
                .Add(V(0, 5.4f, 0.75f), V(1f, 0.5f, 0.05f), L3Palette.Rune)
                .Add(V(0, 2.2f, 0), V(3.4f, 4.4f, 0.1f), L3Palette.CrystalCore));
            L2Core.Box(portal, V(-2.2f, 2.2f, 0), V(1f, 4.4f, 1.4f));
            L2Core.Box(portal, V(2.2f, 2.2f, 0), V(1f, 4.4f, 1.4f));
            GameObject exit = new GameObject("Saida");
            exit.transform.SetParent(portal.transform, false);
            L2Core.Box(exit, V(0, 1.5f, -0.6f), V(3.4f, 3f, 1.5f), true);
            exit.AddComponent<ParkourLevelExit>();
            L2Core.PointLight(portal, "Luz_do_Portal", V(0, 2.4f, -1.5f), new Color(0.6f, 0.9f, 1f), 12f, 3f, false, LightmapBakeType.Mixed);
            L2Core.AddFx(portal, "FX3_Brilho_de_Cristal", V(0, 2.2f, -0.4f));
            L2Core.AddFx(portal, "FX3_Esporos", V(0, 2f, -2f));
            L2Core.Save(portal, AssetCategoryType.Mecanismos, "Portal de saída (Nível 4)");

            // Marca de pouso (círculo projetado no chão durante o salto).
            GameObject marker = new GameObject("Marca_de_Pouso");
            GameObject ring = L2Core.Child(marker, "Anel", Vector3.zero, new VoxelModel().AddFitted(Vector3.zero, V(1f, 0.005f, 1f), L3Palette.Marker));
            MeshRenderer mr = ring.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.enabled = false;
            LandingIndicator li = marker.AddComponent<LandingIndicator>();
            li.marker = mr;
            L2Core.Save(marker, AssetCategoryType.Efeitos, "Marca de pouso");
        }
    }
}
