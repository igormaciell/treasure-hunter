using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Prefabs de cenário do Nível 2 em estilo voxel: arquitetura deteriorada (pilares rachados, arcos desmoronados,
    /// meias-paredes e barricadas para cobertura, escombros, sarcófagos, nichos de ossos, altar, estátua),
    /// props de abandono (caixotes e barris quebrados, porta destruída, correntes, gaiola, ossadas, teias,
    /// estandartes rasgados), iluminação (candelabro, lustre, lanterna do cinto, fenda de luz, braseiro caído),
    /// pouca vegetação (fungos luminosos, raízes secas), tesouros (artefatos) e a inscrição de aviso.
    /// </summary>
    public static class L2Props
    {
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        /// <summary>
        /// Cria todos os prefabs de cenário.
        /// </summary>
        public static void BuildAll()
        {
            BuildArchitecture();
            BuildProps();
            BuildLighting();
            BuildVegetation();
            BuildTreasures();
        }

        // ================================================================== Arquitetura

        /// <summary>
        /// Pilares, arcos, coberturas, escombros, sarcófagos, nicho de ossos, altar, estátua e escada.
        /// </summary>
        private static void BuildArchitecture()
        {
            const AssetCategoryType C = AssetCategoryType.Arquitetura;
            Material s = L2Palette.Stone, sd = L2Palette.StoneDark;

            // Pilar rachado (cobertura alta): base, fuste com lascas faltando, capitel torto.
            L2Core.Simple("Pilar_Rachado", new VoxelModel()
                .Add(V(0, 0.2f, 0), V(1.2f, 0.4f, 1.2f), sd)
                .Add(V(0, 1.6f, 0), V(0.9f, 2.4f, 0.9f), s)
                .Add(V(0.05f, 3.3f, 0), V(0.85f, 1f, 0.85f), s, V(0, 0, 3))
                .Add(V(0, 4.0f, 0), V(1.2f, 0.4f, 1.2f), sd, V(0, 8, 0))
                .Add(V(0.42f, 1.2f, 0.3f), V(0.12f, 0.6f, 0.4f), L2Palette.Void)
                .Add(V(0.7f, 0.12f, 0.5f), V(0.35f, 0.24f, 0.3f), s, V(0, 30, 0)), C, "Pilar rachado", true);

            // Pilar desabado (cobertura baixa, deitado).
            L2Core.Simple("Pilar_Desabado", new VoxelModel()
                .Add(V(0, 0.45f, 0), V(0.9f, 0.9f, 2.6f), s)
                .Add(V(0.15f, 0.42f, 1.9f), V(0.85f, 0.85f, 1.2f), s, V(0, 14, 4))
                .Add(V(-0.1f, 0.6f, -1.45f), V(1.2f, 1.2f, 0.4f), sd, V(0, 0, 12))
                .Add(V(0.8f, 0.15f, -0.4f), V(0.4f, 0.3f, 0.35f), s, V(0, 40, 0)), C, "Pilar desabado", true);

            // Arco desmoronado (decoração das passagens).
            L2Core.Simple("Arco_Desmoronado", new VoxelModel()
                .Add(V(-2.1f, 1.9f, 0), V(0.7f, 3.8f, 0.9f), s)
                .Add(V(2.1f, 1.2f, 0), V(0.7f, 2.4f, 0.9f), s)
                .Add(V(-1.2f, 4f, 0), V(2.4f, 0.6f, 1f), sd, V(0, 0, -6))
                .Add(V(1.9f, 0.3f, 0.6f), V(0.8f, 0.6f, 0.7f), s, V(0, 25, 15))
                .Add(V(2.4f, 0.2f, -0.5f), V(0.5f, 0.4f, 0.5f), sd, V(0, 60, 0)), C, "Arco desmoronado", false);

            // Meia-parede (cobertura de 1,4 m com topo quebrado).
            L2Core.Simple("Meia_Parede", new VoxelModel()
                .Add(V(0, 0.6f, 0), V(3f, 1.2f, 0.6f), s)
                .Add(V(-0.9f, 1.3f, 0), V(1.2f, 0.2f, 0.6f), s)
                .Add(V(0.6f, 1.28f, 0), V(0.7f, 0.16f, 0.58f), sd)
                .Add(V(1.3f, 0.15f, 0.55f), V(0.4f, 0.3f, 0.35f), s, V(0, 35, 0)), C, "Meia-parede (cobertura)", true);

            // Barricada de madeira (cobertura).
            VoxelModel barricade = new VoxelModel()
                .Add(V(-1.2f, 0.75f, 0), V(0.18f, 1.5f, 0.18f), L2Palette.WoodDark)
                .Add(V(1.2f, 0.75f, 0), V(0.18f, 1.5f, 0.18f), L2Palette.WoodDark);
            for (int i = 0; i < 4; i++)
                barricade.Add(V(0, 0.3f + i * 0.33f, 0.1f), V(2.7f, 0.28f, 0.08f), L2Palette.Wood, V(0, 0, i % 2 == 0 ? 3f : -4f));
            barricade.Add(V(0, 0.8f, 0.16f), V(2.9f, 0.18f, 0.06f), L2Palette.WoodDark, V(0, 0, 28));
            GameObject go = L2Core.Begin("Barricada", barricade);
            L2Core.Box(go, V(0, 0.75f, 0.05f), V(2.8f, 1.5f, 0.4f));
            L2Core.Save(go, C, "Barricada de madeira (cobertura)");

            // Escombros grandes (bloqueiam a escada de entrada) e pequenos.
            VoxelModel bigRubble = new VoxelModel();
            for (int i = 0; i < 12; i++)
            {
                float sz = 0.4f + (i * 37 % 10) / 10f * 0.6f;
                float a = i * 2.4f;
                float rr = 0.4f + (i % 4) * 0.35f;
                bigRubble.Add(V(Mathf.Cos(a) * rr, sz * 0.5f + (i % 3) * 0.25f, Mathf.Sin(a) * rr * 0.8f), V(sz, sz * 0.8f, sz), i % 3 == 0 ? sd : (i % 3 == 1 ? s : L2Palette.Rock), V(i * 11, i * 29, i * 7));
            }
            bigRubble.Add(V(0.2f, 1.2f, 0), V(1.6f, 0.35f, 0.4f), L2Palette.WoodDark, V(0, 20, 25));
            go = L2Core.Begin("Escombros_Grandes", bigRubble);
            L2Core.Box(go, V(0, 0.8f, 0), V(2.6f, 1.6f, 2.2f));
            L2Core.Save(go, C, "Escombros grandes");

            VoxelModel smallRubble = new VoxelModel();
            for (int i = 0; i < 6; i++)
            {
                float sz = 0.12f + (i * 29 % 10) / 10f * 0.22f;
                smallRubble.Add(V(Mathf.Cos(i * 2.1f) * 0.45f, sz * 0.5f, Mathf.Sin(i * 2.1f) * 0.4f), V(sz, sz, sz * 1.2f), i % 2 == 0 ? s : L2Palette.Rock, V(0, i * 33f, 0));
            }
            L2Core.Simple("Escombros_Pequenos", smallRubble, C, "Escombros pequenos", false);

            // Sarcófagos (cobertura baixa).
            VoxelModel sarc = new VoxelModel()
                .Add(V(0, 0.45f, 0), V(1.1f, 0.9f, 2.3f), L2Palette.Sarcophagus)
                .Add(V(0, 0.98f, 0), V(1.2f, 0.16f, 2.4f), L2Palette.Sarcophagus)
                .Add(V(0, 1.12f, -0.75f), V(0.45f, 0.14f, 0.4f), L2Palette.Sarcophagus)
                .Add(V(0, 1.1f, 0.1f), V(0.6f, 0.1f, 1.1f), L2Palette.StoneDark);
            L2Core.Simple("Sarcofago", sarc, C, "Sarcófago", true);

            VoxelModel open = new VoxelModel()
                .Add(V(0, 0.08f, 0), V(1.1f, 0.16f, 2.3f), L2Palette.Sarcophagus)
                .Add(V(-0.5f, 0.45f, 0), V(0.1f, 0.9f, 2.3f), L2Palette.Sarcophagus)
                .Add(V(0.5f, 0.45f, 0), V(0.1f, 0.9f, 2.3f), L2Palette.Sarcophagus)
                .Add(V(0, 0.45f, -1.1f), V(1.1f, 0.9f, 0.1f), L2Palette.Sarcophagus)
                .Add(V(0, 0.45f, 1.1f), V(1.1f, 0.9f, 0.1f), L2Palette.Sarcophagus)
                .Add(V(0, 0.2f, -0.55f), V(0.24f, 0.2f, 0.26f), L2Palette.BoneDark)
                .Add(V(0, 0.2f, 0.2f), V(0.08f, 0.08f, 0.9f), L2Palette.BoneDark)
                .Add(V(1.05f, 0.5f, 0), V(0.16f, 1f, 2.4f), L2Palette.Sarcophagus, V(0, 0, -25));
            go = L2Core.Begin("Sarcofago_Aberto", open);
            L2Core.Box(go, V(0, 0.5f, 0), V(1.2f, 1f, 2.4f));
            L2Core.Save(go, C, "Sarcófago aberto");

            // Nicho de ossos na parede (frente = +Z).
            VoxelModel niche = new VoxelModel()
                .Add(V(0, 1.5f, -0.1f), V(2.4f, 3f, 0.2f), sd)
                .Add(V(0, 0.9f, 0.1f), V(2.2f, 0.12f, 0.4f), s)
                .Add(V(0, 1.9f, 0.1f), V(2.2f, 0.12f, 0.4f), s)
                .Add(V(0, 2.9f, 0.1f), V(2.2f, 0.12f, 0.4f), s);
            for (int row = 0; row < 3; row++)
            {
                for (int i = 0; i < 4; i++)
                {
                    float x = -0.8f + i * 0.53f;
                    float y = 1.07f + row;
                    if ((row + i) % 3 == 1) niche.Add(V(x, y, 0.12f), V(0.08f, 0.08f, 0.35f), L2Palette.BoneDark, V(0, 70, 0));
                    else
                    {
                        niche.Add(V(x, y + 0.04f, 0.12f), V(0.22f, 0.2f, 0.24f), L2Palette.Bone);
                        niche.Add(V(x - 0.05f, y + 0.06f, 0.245f), V(0.06f, 0.06f, 0.01f), L2Palette.Void);
                        niche.Add(V(x + 0.05f, y + 0.06f, 0.245f), V(0.06f, 0.06f, 0.01f), L2Palette.Void);
                    }
                }
            }
            L2Core.Simple("Nicho_de_Ossos", niche, C, "Nicho de ossos (catacumba)", false);

            // Altar profanado.
            L2Core.Simple("Altar_Profanado", new VoxelModel()
                .Add(V(0, 0.15f, 0), V(2.4f, 0.3f, 1.3f), sd)
                .Add(V(0, 0.65f, 0), V(2f, 0.7f, 1f), L2Palette.Sarcophagus)
                .Add(V(0, 1.05f, 0), V(2.2f, 0.12f, 1.15f), s)
                .Add(V(0.1f, 1.3f, 0), V(0.35f, 0.4f, 0.3f), L2Palette.StoneDark, V(0, 0, 25))
                .Add(V(-0.7f, 1.14f, 0.2f), V(0.5f, 0.04f, 0.3f), L2Palette.ClothRed)
                .Add(V(0.75f, 1.2f, -0.2f), V(0.08f, 0.2f, 0.08f), L2Palette.Wax)
                .Add(V(0.6f, 1.17f, -0.25f), V(0.07f, 0.14f, 0.07f), L2Palette.Wax), C, "Altar profanado", true);

            // Estátua decapitada no pedestal (cobertura central).
            L2Core.Simple("Estatua_Decapitada", new VoxelModel()
                .Add(V(0, 0.4f, 0), V(2.2f, 0.8f, 2.2f), sd)
                .Add(V(0, 0.95f, 0), V(1.7f, 0.3f, 1.7f), s)
                .Add(V(-0.3f, 1.75f, 0), V(0.45f, 1.3f, 0.5f), s)
                .Add(V(0.3f, 1.75f, 0), V(0.45f, 1.3f, 0.5f), s)
                .Add(V(0, 3f, 0), V(1.1f, 1.3f, 0.65f), s)
                .Add(V(-0.75f, 2.9f, 0.1f), V(0.3f, 1.1f, 0.35f), s, V(0, 0, -8))
                .Add(V(0, 3.68f, 0), V(0.5f, 0.12f, 0.45f), L2Palette.StoneDark)
                .Add(V(0.95f, 0.98f, 0.6f), V(0.45f, 0.42f, 0.45f), s, V(20, 30, 10)), C, "Estátua decapitada", true);

            // Escada de pedra (subindo para a saída desabada).
            VoxelModel stairs = new VoxelModel();
            for (int i = 0; i < 8; i++) stairs.Add(V(0, 0.2f + i * 0.4f, -i * 0.5f - 0.25f), V(3.4f, 0.4f, 0.5f), i % 2 == 0 ? s : sd);
            L2Core.Simple("Escada_de_Pedra", stairs, C, "Escada de pedra", true);
        }

        // ================================================================== Props

        /// <summary>
        /// Caixote e barril quebrados, porta destruída, correntes, gaiola, pilha de crânios, ossada,
        /// teia grande, estandarte rasgado, mesa quebrada, armeiro, escudo caído, pergaminhos e cacos.
        /// </summary>
        private static void BuildProps()
        {
            const AssetCategoryType C = AssetCategoryType.Props;
            Material w = L2Palette.Wood, wd = L2Palette.WoodDark;

            L2Core.Simple("Caixote_Quebrado", new VoxelModel()
                .Add(V(0, 0.05f, 0), V(1f, 0.1f, 1f), wd)
                .Add(V(-0.45f, 0.35f, 0), V(0.1f, 0.6f, 1f), w, V(0, 0, 8))
                .Add(V(0.05f, 0.3f, -0.45f), V(0.9f, 0.5f, 0.1f), w)
                .Add(V(0.6f, 0.08f, 0.4f), V(0.9f, 0.1f, 0.2f), w, V(0, 35, 0))
                .Add(V(-0.1f, 0.12f, 0.7f), V(0.7f, 0.08f, 0.15f), wd, V(0, -20, 0)), C, "Caixote quebrado", false);

            L2Core.Simple("Barril_Quebrado", new VoxelModel()
                .Add(V(0, 0.3f, 0), V(0.7f, 0.6f, 0.9f), wd, V(0, 0, 90))
                .Add(V(0.5f, 0.3f, 0), V(0.05f, 0.62f, 0.92f), L2Palette.Iron, V(0, 0, 90))
                .Add(V(-0.7f, 0.05f, 0.3f), V(0.1f, 0.06f, 0.8f), wd, V(0, 30, 0))
                .Add(V(-0.6f, 0.05f, -0.4f), V(0.1f, 0.06f, 0.7f), wd, V(0, -50, 0)), C, "Barril quebrado", false);

            L2Core.Simple("Porta_Destruida", new VoxelModel()
                .Add(V(0, 0.08f, 0), V(1.3f, 0.12f, 2.4f), wd, V(0, 12, 0))
                .Add(V(0.2f, 0.16f, 0.4f), V(1.25f, 0.06f, 0.12f), L2Palette.Iron, V(0, 12, 0))
                .Add(V(-0.2f, 0.16f, -0.6f), V(1.25f, 0.06f, 0.12f), L2Palette.Iron, V(0, 12, 0))
                .Add(V(0.8f, 0.6f, -1f), V(0.1f, 1.2f, 0.6f), wd, V(0, 20, -15)), C, "Porta destruída", false);

            VoxelModel chains = new VoxelModel();
            for (int i = 0; i < 10; i++)
                chains.Add(V(0, -0.18f - i * 0.2f, 0), V(0.08f, 0.2f, 0.04f), L2Palette.IronDark, V(0, i % 2 == 0 ? 0 : 90, 0));
            for (int i = 0; i < 7; i++)
                chains.Add(V(0.35f, -0.18f - i * 0.2f, 0.1f), V(0.08f, 0.2f, 0.04f), L2Palette.IronDark, V(0, i % 2 == 0 ? 0 : 90, 0));
            chains.Add(V(0.35f, -1.6f, 0.1f), V(0.22f, 0.12f, 0.22f), L2Palette.IronDark);
            L2Core.Simple("Correntes_Penduradas", chains, C, "Correntes penduradas", false);

            VoxelModel cage = new VoxelModel()
                .Add(V(0, -0.5f, 0), V(0.05f, 1f, 0.05f), L2Palette.IronDark)
                .Add(V(0, -1.05f, 0), V(1f, 0.08f, 1f), L2Palette.IronDark)
                .Add(V(0, -2.65f, 0), V(1f, 0.08f, 1f), L2Palette.IronDark);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                cage.Add(V(Mathf.Cos(a) * 0.46f, -1.85f, Mathf.Sin(a) * 0.46f), V(0.05f, 1.6f, 0.05f), L2Palette.IronDark);
            }
            cage.Add(V(0, -2.45f, 0), V(0.22f, 0.22f, 0.24f), L2Palette.Bone)
                .Add(V(0.05f, -2.55f, 0.1f), V(0.08f, 0.08f, 0.5f), L2Palette.BoneDark, V(0, 50, 0))
                .Add(V(-0.1f, -2.58f, -0.1f), V(0.3f, 0.06f, 0.06f), L2Palette.BoneDark);
            L2Core.Simple("Gaiola_Pendurada", cage, C, "Gaiola pendurada", false);

            VoxelModel skulls = new VoxelModel();
            for (int i = 0; i < 9; i++)
            {
                float a = i * 2.3f;
                float r = i < 6 ? 0.35f : 0.15f;
                float y = i < 6 ? 0.12f : 0.32f;
                Vector3 p = V(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                skulls.Add(p, V(0.24f, 0.22f, 0.26f), i % 3 == 0 ? L2Palette.BoneDark : L2Palette.Bone, V(0, i * 40f, 0));
                skulls.Add(p + Quaternion.Euler(0, i * 40f, 0) * V(0, 0.02f, 0.131f), V(0.12f, 0.06f, 0.01f), L2Palette.Void, V(0, i * 40f, 0));
            }
            L2Core.Simple("Pilha_de_Cranios", skulls, C, "Pilha de crânios", false);

            L2Core.Simple("Ossada_Sentada", new VoxelModel()
                .Add(V(0, 0.08f, 0), V(0.34f, 0.12f, 0.2f), L2Palette.BoneDark)
                .Add(V(0, 0.4f, -0.05f), V(0.08f, 0.55f, 0.08f), L2Palette.BoneDark, V(-12, 0, 0))
                .Add(V(0, 0.45f, 0.02f), V(0.36f, 0.05f, 0.2f), L2Palette.Bone)
                .Add(V(0, 0.58f, 0.02f), V(0.34f, 0.05f, 0.2f), L2Palette.Bone)
                .Add(V(0.03f, 0.82f, 0.02f), V(0.24f, 0.22f, 0.24f), L2Palette.Bone, V(15, 0, 20))
                .Add(V(-0.12f, 0.05f, 0.35f), V(0.07f, 0.07f, 0.55f), L2Palette.Bone)
                .Add(V(0.12f, 0.05f, 0.35f), V(0.07f, 0.07f, 0.55f), L2Palette.Bone, V(0, 10, 0))
                .Add(V(0.25f, 0.35f, 0.15f), V(0.06f, 0.4f, 0.06f), L2Palette.Bone, V(20, 0, 30))
                .Add(V(0, 0.4f, -0.22f), V(0.5f, 0.6f, 0.06f), L2Palette.Cloth), C, "Ossada sentada", false);

            // Teia grande de canto (frente = +Z, presa no canto superior).
            VoxelModel web = new VoxelModel();
            for (int i = 0; i < 6; i++)
            {
                float a = i * 18f;
                web.Add(Quaternion.Euler(0, 0, -a) * V(0.85f, 0, 0), V(1.7f, 0.03f, 0.015f), L2Palette.Cobweb, V(0, 0, -a));
            }
            float[] rings = { 0.4f, 0.8f, 1.25f };
            foreach (float r in rings)
            {
                for (int i = 0; i < 5; i++)
                {
                    float a = (i + 0.5f) * 18f * Mathf.Deg2Rad;
                    float seg = 2f * r * Mathf.Sin(9f * Mathf.Deg2Rad);
                    web.Add(V(Mathf.Cos(a) * r, -Mathf.Sin(a) * r, 0), V(seg, 0.025f, 0.015f), L2Palette.Cobweb, V(0, 0, -(i + 0.5f) * 18f - 90f));
                }
            }
            L2Core.Simple("Teia_Grande", web, C, "Teia grande de canto", false);

            // Estandarte rasgado (preso na parede, frente = +Z).
            L2Core.Simple("Estandarte_Rasgado", new VoxelModel()
                .Add(V(0, 0, 0.05f), V(1.4f, 0.1f, 0.1f), L2Palette.WoodDark)
                .Add(V(-0.15f, -0.9f, 0.08f), V(0.9f, 1.7f, 0.03f), L2Palette.ClothRed)
                .Add(V(0.45f, -0.6f, 0.08f), V(0.3f, 1.1f, 0.03f), L2Palette.ClothRed)
                .Add(V(-0.15f, -0.7f, 0.1f), V(0.35f, 0.35f, 0.02f), L2Palette.Gold), C, "Estandarte rasgado", false);

            L2Core.Simple("Mesa_Quebrada", new VoxelModel()
                .Add(V(0, 0.55f, 0), V(1.6f, 0.08f, 0.9f), w, V(0, 0, 14))
                .Add(V(-0.7f, 0.33f, -0.35f), V(0.1f, 0.66f, 0.1f), wd)
                .Add(V(-0.7f, 0.33f, 0.35f), V(0.1f, 0.66f, 0.1f), wd)
                .Add(V(0.6f, 0.05f, 0.4f), V(0.1f, 0.1f, 0.66f), wd, V(0, 40, 90)), C, "Mesa quebrada", true);

            VoxelModel rack = new VoxelModel()
                .Add(V(-0.8f, 0.9f, 0), V(0.12f, 1.8f, 0.12f), wd)
                .Add(V(0.8f, 0.9f, 0), V(0.12f, 1.8f, 0.12f), wd)
                .Add(V(0, 1.5f, 0), V(1.7f, 0.1f, 0.12f), wd)
                .Add(V(0, 0.4f, 0), V(1.7f, 0.1f, 0.12f), wd);
            for (int i = 0; i < 4; i++)
                rack.Add(V(-0.55f + i * 0.37f, 1.0f, 0.1f), V(0.05f, 1.6f, 0.05f), i % 2 == 0 ? L2Palette.IronDark : L2Palette.Iron, V(0, 0, i == 2 ? 10 : 0))
                    .Add(V(-0.55f + i * 0.37f, 1.85f, 0.1f), V(0.12f, 0.2f, 0.03f), L2Palette.Iron);
            L2Core.Simple("Armeiro_Enferrujado", rack, C, "Armeiro enferrujado", true);

            L2Core.Simple("Escudo_Caido", new VoxelModel()
                .Add(V(0, 0.04f, 0), V(0.7f, 0.06f, 0.8f), L2Palette.Iron, V(0, 20, 0))
                .Add(V(0, 0.08f, 0), V(0.25f, 0.04f, 0.3f), L2Palette.ClothRed, V(0, 20, 0))
                .Add(V(0.45f, 0.03f, 0.5f), V(0.06f, 0.04f, 1f), L2Palette.IronDark, V(0, -30, 0))
                .Add(V(0.2f, 0.04f, 0.9f), V(0.25f, 0.05f, 0.06f), L2Palette.Leather, V(0, -30, 0)), C, "Escudo e espada caídos", false);

            L2Core.Simple("Pergaminhos", new VoxelModel()
                .Add(V(0, 0.03f, 0), V(0.4f, 0.04f, 0.3f), L2Palette.Paper, V(0, 15, 0))
                .Add(V(0.35f, 0.04f, 0.2f), V(0.08f, 0.08f, 0.45f), L2Palette.Paper, V(0, 70, 0))
                .Add(V(-0.3f, 0.05f, 0.25f), V(0.3f, 0.1f, 0.22f), L2Palette.Leather, V(0, -25, 0)), C, "Pergaminhos espalhados", false);

            VoxelModel shards = new VoxelModel();
            for (int i = 0; i < 7; i++)
                shards.Add(V(Mathf.Cos(i * 1.9f) * 0.35f, 0.03f, Mathf.Sin(i * 1.9f) * 0.3f), V(0.14f, 0.05f, 0.1f), L2Palette.BoneDark, V(i * 20, i * 50, 0));
            shards.Add(V(0, 0.12f, 0), V(0.3f, 0.24f, 0.3f), L2Palette.BoneDark, V(0, 0, 70));
            L2Core.Simple("Cacos_de_Urna", shards, C, "Cacos de urna", false);
        }

        // ================================================================== Iluminação

        /// <summary>
        /// Candelabro de ferro, lustre de velas, lanterna do cinto, fenda de luz no teto e braseiro caído.
        /// </summary>
        private static void BuildLighting()
        {
            const AssetCategoryType C = AssetCategoryType.Iluminacao;
            Color candle = new Color(1f, 0.75f, 0.45f);

            VoxelModel candelabra = new VoxelModel()
                .Add(V(0, 0.05f, 0), V(0.5f, 0.1f, 0.5f), L2Palette.IronDark)
                .Add(V(0, 0.8f, 0), V(0.07f, 1.5f, 0.07f), L2Palette.IronDark)
                .Add(V(0, 1.5f, 0), V(0.8f, 0.06f, 0.06f), L2Palette.IronDark);
            foreach (float x in new[] { -0.38f, 0f, 0.38f })
                candelabra.Add(V(x, 1.62f, 0), V(0.07f, 0.18f, 0.07f), L2Palette.Wax).Add(V(x, 1.75f, 0), V(0.04f, 0.07f, 0.04f), L2Palette.Flame);
            GameObject go = L2Core.Begin("Candelabro_de_Ferro", candelabra);
            L2Core.Box(go, V(0, 0.8f, 0), V(0.5f, 1.6f, 0.5f));
            L2Core.PointLight(go, "Luz", V(0, 1.9f, 0), candle, 5.5f, 1.3f, true, LightmapBakeType.Mixed);
            L2Core.Save(go, C, "Candelabro de ferro");

            VoxelModel chandelier = new VoxelModel()
                .Add(V(0, -0.8f, 0), V(0.05f, 1.6f, 0.05f), L2Palette.IronDark)
                .Add(V(0, -1.65f, 0), V(1.3f, 0.08f, 0.08f), L2Palette.IronDark)
                .Add(V(0, -1.65f, 0), V(0.08f, 0.08f, 1.3f), L2Palette.IronDark);
            foreach (Vector3 p in new[] { V(0.6f, 0, 0), V(-0.6f, 0, 0), V(0, 0, 0.6f), V(0, 0, -0.6f) })
                chandelier.Add(p + V(0, -1.53f, 0), V(0.08f, 0.16f, 0.08f), L2Palette.Wax).Add(p + V(0, -1.42f, 0), V(0.04f, 0.07f, 0.04f), L2Palette.Flame);
            go = L2Core.Begin("Lustre_de_Velas", chandelier);
            L2Core.PointLight(go, "Luz", V(0, -1.3f, 0), candle, 8f, 1.6f, true, LightmapBakeType.Mixed);
            L2Core.Save(go, C, "Lustre de velas");

            // Lanterna presa no cinto de John (luz pessoal de alcance curto: visibilidade reduzida).
            go = L2Core.Begin("Lanterna_do_Cinto", new VoxelModel()
                .Add(V(0, 0, 0), V(0.14f, 0.18f, 0.14f), L2Palette.Iron)
                .Add(V(0, 0, 0), V(0.1f, 0.13f, 0.15f), L2Palette.Flame)
                .Add(V(0, 0.12f, 0), V(0.04f, 0.06f, 0.04f), L2Palette.IronDark));
            Light lantern = L2Core.PointLight(go, "Luz", V(0, 0.25f, 0.2f), new Color(1f, 0.72f, 0.42f), 7f, 1.7f, false);
            lantern.shadows = LightShadows.Soft;
            lantern.shadowStrength = 0.75f;
            L2Core.Save(go, C, "Lanterna do cinto");

            // Fenda de luz no teto: foco de luz destacando pontos importantes (Spot Mixed + Area Baked + poeira).
            go = L2Core.Begin("Fenda_de_Luz", new VoxelModel()
                .Add(V(0, 0, 0), V(1.4f, 0.06f, 0.35f), L2Palette.CrackLight, V(0, 20, 0))
                .Add(V(0.5f, 0, 0.3f), V(0.6f, 0.06f, 0.2f), L2Palette.CrackLight, V(0, -30, 0)));
            GameObject spotGo = new GameObject("Foco");
            spotGo.transform.SetParent(go.transform, false);
            spotGo.transform.localPosition = V(0, -0.1f, 0);
            spotGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Light spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(0.72f, 0.8f, 1f);
            spot.range = 9f;
            spot.spotAngle = 38f;
            spot.intensity = 3.2f;
            spot.shadows = LightShadows.None;
            spot.lightmapBakeType = LightmapBakeType.Mixed;
            GameObject areaGo = new GameObject("LuzIndireta_Area");
            areaGo.transform.SetParent(go.transform, false);
            areaGo.transform.localPosition = V(0, -0.15f, 0);
            areaGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Light area = areaGo.AddComponent<Light>();
            area.type = LightType.Rectangle;
            area.areaSize = new Vector2(1.6f, 0.6f);
            area.color = new Color(0.7f, 0.78f, 1f);
            area.intensity = 2.5f;
            area.range = 8f;
            area.lightmapBakeType = LightmapBakeType.Baked;
            L2Core.AddFx(go, "FX_DustMotes", V(0, -2.5f, 0));
            L2Core.Save(go, C, "Fenda de luz no teto");

            // Braseiro tombado com brasas espalhadas.
            go = L2Core.Begin("Braseiro_Caido", new VoxelModel()
                .Add(V(0, 0.35f, 0), V(0.9f, 0.3f, 0.9f), L2Palette.IronDark, V(0, 0, 70))
                .Add(V(0.1f, 0.05f, 0.5f), V(0.06f, 0.6f, 0.06f), L2Palette.IronDark, V(80, 0, 0))
                .Add(V(0.6f, 0.03f, 0), V(0.8f, 0.06f, 0.7f), L2Palette.Embers)
                .Add(V(1f, 0.03f, 0.4f), V(0.3f, 0.06f, 0.3f), L2Palette.Embers));
            L2Core.AddFx(go, "FX2_Brasas", V(0.7f, 0.1f, 0.1f));
            L2Core.AddFx(go, "FX_Smoke", V(0.7f, 0.3f, 0.1f));
            L2Core.PointLight(go, "Luz", V(0.7f, 0.4f, 0.1f), new Color(1f, 0.45f, 0.15f), 4f, 1.2f, true, LightmapBakeType.Mixed);
            L2Core.Save(go, C, "Braseiro tombado");
        }

        // ================================================================== Vegetação (pouca)

        /// <summary>
        /// Fungos luminosos e raízes secas: a pouca vida que resta nas catacumbas.
        /// </summary>
        private static void BuildVegetation()
        {
            const AssetCategoryType C = AssetCategoryType.Vegetacao;
            VoxelModel fungus = new VoxelModel();
            for (int i = 0; i < 6; i++)
            {
                float a = i * 2.2f, r = 0.12f + (i % 3) * 0.1f, h = 0.08f + (i % 3) * 0.06f;
                Vector3 p = V(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                fungus.Add(p + V(0, h * 0.5f, 0), V(0.03f, h, 0.03f), L2Palette.Bone)
                      .Add(p + V(0, h, 0), V(0.12f, 0.04f, 0.12f), L2Palette.Fungus);
            }
            L2Core.Simple("Fungo_Luminoso", fungus, C, "Fungos luminosos", false);

            VoxelModel roots = new VoxelModel();
            for (int i = 0; i < 5; i++)
            {
                float x = -0.5f + i * 0.25f;
                float len = 0.8f + (i * 37 % 10) / 10f * 1.2f;
                roots.Add(V(x, -len * 0.5f, (i % 2) * 0.1f), V(0.06f, len, 0.06f), L2Palette.WoodDark, V(0, 0, (i - 2) * 6f));
            }
            L2Core.Simple("Raizes_Secas", roots, C, "Raízes secas", false);
        }

        // ================================================================== Tesouros e inscrição

        /// <summary>
        /// Artefatos do Nível 2 (Adaga Cerimonial e Amuleto de Osso) e a inscrição de aviso nas paredes.
        /// </summary>
        private static void BuildTreasures()
        {
            Artifact("Adaga_Cerimonial", "Adaga Cerimonial", new VoxelModel()
                .Add(V(0, 0.18f, 0), V(0.05f, 0.36f, 0.02f), L2Palette.Blade)
                .Add(V(0, 0, 0), V(0.18f, 0.04f, 0.04f), L2Palette.Gold)
                .Add(V(0, -0.1f, 0), V(0.04f, 0.18f, 0.04f), L2Palette.Leather)
                .Add(V(0, -0.21f, 0), V(0.07f, 0.07f, 0.07f), L2Palette.Ruby));
            Artifact("Amuleto_de_Osso", "Amuleto de Osso", new VoxelModel()
                .Add(V(0, 0, 0), V(0.22f, 0.26f, 0.05f), L2Palette.Bone)
                .Add(V(0, 0, 0.03f), V(0.08f, 0.08f, 0.02f), L2Palette.Ruby)
                .Add(V(0, 0.17f, 0), V(0.3f, 0.04f, 0.04f), L2Palette.Rope)
                .Add(V(-0.1f, -0.16f, 0), V(0.05f, 0.1f, 0.05f), L2Palette.Gold)
                .Add(V(0.1f, -0.16f, 0), V(0.05f, 0.1f, 0.05f), L2Palette.Gold));

            VoxelModel inscription = new VoxelModel()
                .Add(V(0, 0, 0.03f), V(1.8f, 1f, 0.06f), L2Palette.StoneDark);
            inscription.AddFitted(V(0, 0, 0.07f), V(1.6f, 0.8f, 0.02f), L2Palette.Runes);
            GameObject go = L2Core.Begin("Inscricao_de_Aviso", inscription);
            L2Core.PointLight(go, "Brilho", V(0, 0, 0.4f), new Color(1f, 0.35f, 0.2f), 2.5f, 0.6f, true);
            L2Core.Save(go, AssetCategoryType.Inscricoes, "Inscrição de aviso (runas)");
        }

        /// <summary>
        /// Cria um artefato coletável (gatilho, giro, brilho e luz), registrado no resumo do nível.
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="artifactName">Nome do artefato.</param>
        /// <param name="model">Modelo voxel.</param>
        private static void Artifact(string id, string artifactName, VoxelModel model)
        {
            GameObject go = new GameObject(id);
            go.tag = GameTags.Artifact;
            go.layer = L2Core.Layer(CombatLayers.InteractableLayerName);
            GameObject visual = L2Core.Child(go, "Visual", V(0, 0.6f, 0), model);
            Spinner spin = visual.AddComponent<Spinner>();
            spin.degreesPerSecond = V(0, 45f, 0);
            spin.bobAmplitude = 0.08f;
            L2Core.Sphere(go, V(0, 0.6f, 0), 0.55f, true);
            Collectible c = go.AddComponent<Collectible>();
            c.type = CollectibleType.Artifact;
            c.value = 1;
            c.artifactName = artifactName;
            c.pickupEffect = L2Core.L1("FX_Sparkle");
            L2Core.AddFx(go, "FX_Glint", V(0, 0.6f, 0));
            L2Core.PointLight(go, "Brilho", V(0, 0.8f, 0), new Color(1f, 0.85f, 0.4f), 3f, 1.2f);
            L2Core.Save(go, AssetCategoryType.Tesouros, artifactName + " (artefato)");
        }
    }
}
