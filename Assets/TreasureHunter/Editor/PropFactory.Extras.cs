using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Terceira parte da fábrica de prefabs: assets extras do Nível 1
    /// (Cristais Luminosos e Teia de Aranha), puramente decorativos.
    /// </summary>
    public static partial class PropFactory
    {
        /// <summary>Identificador do prefab dos cristais.</summary>
        public const string CrystalId = "Cristais_Luminosos";
        /// <summary>Identificador do prefab da teia.</summary>
        public const string CobwebId = "Teia_de_Aranha";

        /// <summary>
        /// Cria os dois prefabs extras e os registra no dicionário de prefabs.
        /// Recebe os materiais por parâmetro para funcionar tanto no gerador completo
        /// quanto no menu que apenas acrescenta esses assets a um projeto já gerado.
        /// </summary>
        /// <param name="crystal">Material emissivo azul dos cristais.</param>
        /// <param name="cobweb">Material claro dos fios da teia.</param>
        /// <param name="rock">Material de rocha da base dos cristais.</param>
        public static void BuildExtras(Material crystal, Material cobweb, Material rock)
        {
            // Cristais Luminosos: aglomerado de prismas girados sobre uma base de rocha, com luz azulada.
            VoxelModel crystals = new VoxelModel()
                .Add(V(0, 0.08f, 0), V(0.7f, 0.16f, 0.6f), rock)
                .Add(V(0, 0.45f, 0), V(0.16f, 0.8f, 0.16f), crystal, V(0, 45, 6))
                .Add(V(0.18f, 0.3f, 0.08f), V(0.12f, 0.5f, 0.12f), crystal, V(0, 20, -25))
                .Add(V(-0.17f, 0.28f, -0.05f), V(0.12f, 0.46f, 0.12f), crystal, V(15, 60, 22))
                .Add(V(0.05f, 0.22f, -0.2f), V(0.1f, 0.34f, 0.1f), crystal, V(-28, 10, 0))
                .Add(V(-0.08f, 0.18f, 0.2f), V(0.08f, 0.26f, 0.08f), crystal, V(30, 35, 10));
            GameObject go = Begin(CrystalId, crystals);
            Box(go, V(0, 0.35f, 0), V(0.6f, 0.7f, 0.5f));
            AddLight(go, "Brilho", V(0, 0.6f, 0), new Color(0.45f, 0.85f, 1f), 3.5f, 1.1f);
            Save(go, AssetCategoryType.Iluminacao, "Cristais luminosos");

            // Teia de Aranha: fios radiais e anéis num painel vertical (frente = +Z), presa perto do teto.
            VoxelModel web = new VoxelModel();
            for (int i = 0; i < 8; i++)
            {
                float a = i * 22.5f;
                web.Add(V(0, 0, 0), V(1.3f, 0.025f, 0.015f), cobweb, V(0, 0, a));
            }
            float[] rings = { 0.2f, 0.38f, 0.56f };
            foreach (float r in rings)
            {
                for (int i = 0; i < 16; i++)
                {
                    float a = (i + 0.5f) * 22.5f * Mathf.Deg2Rad;
                    float seg = 2f * r * Mathf.Sin(11.25f * Mathf.Deg2Rad);
                    web.Add(V(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0), V(seg, 0.02f, 0.015f), cobweb, V(0, 0, (i + 0.5f) * 22.5f + 90f));
                }
            }
            go = Begin(CobwebId, web);
            Save(go, AssetCategoryType.Props, "Teia de aranha");
        }
    }
}
