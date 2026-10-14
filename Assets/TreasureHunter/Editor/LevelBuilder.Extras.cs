using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Assets extras do Nível 1 (Cristais Luminosos e Teia de Aranha).
    /// Podem ser acrescentados a um projeto já gerado, sem rodar o gerador completo de novo
    /// (o que recriaria todos os assets e atrapalharia o versionamento no GitHub).
    /// </summary>
    public static partial class LevelBuilder
    {
        private const string ExtrasGroupName = "AssetsExtras";

        /// <summary>
        /// Acrescenta os dois assets extras à cena Level1 já existente: cria os prefabs,
        /// coloca as instâncias, salva a cena e atualiza o inventário. Pode ser repetido sem duplicar nada.
        /// </summary>
        [MenuItem("Treasure Hunter/Adicionar assets extras ao Nível 1 (sem regerar)", priority = 2)]
        public static void AddExtrasToExistingLevel()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // Usa os materiais que já existem no projeto (não recria nada do que foi gerado antes).
            Material crystal = AssetDatabase.LoadAssetAtPath<Material>(BuilderCore.MaterialsPath + "/mat_crystal_blue.mat");
            Material rock = AssetDatabase.LoadAssetAtPath<Material>(BuilderCore.MaterialsPath + "/mat_rock.mat");
            if (crystal == null || rock == null || AssetDatabase.LoadAssetAtPath<SceneAsset>(Level1ScenePath) == null)
            {
                EditorUtility.DisplayDialog("Treasure Hunter",
                    "Não encontrei o Nível 1 gerado. Rode primeiro \"Construir Nível 1 (gerar tudo)\".", "OK");
                return;
            }
            Material cobweb = LoadOrCreateCobwebMaterial();

            EditorSceneManager.OpenScene(Level1ScenePath);
            PropFactory.BuildExtras(crystal, cobweb, rock);

            // Remove uma colocação anterior (permite rodar o menu mais de uma vez).
            GameObject old = GameObject.Find(ExtrasGroupName);
            if (old != null) Object.DestroyImmediate(old);
            PlaceExtras();

            WriteInventory();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Level1ScenePath);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Treasure Hunter",
                "Cristais Luminosos e Teia de Aranha adicionados à cena Level1.\n" +
                "Inventário atualizado em Assets/TreasureHunter/Docs/Inventario_Nivel1.md", "OK");
        }

        /// <summary>
        /// Carrega o material da teia ou, se ainda não existir, cria e salva um material URP Lit claro.
        /// </summary>
        /// <returns>O material da teia.</returns>
        private static Material LoadOrCreateCobwebMaterial()
        {
            string path = BuilderCore.MaterialsPath + "/mat_cobweb.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = "mat_cobweb" };
            Color c = new Color(0.86f, 0.86f, 0.82f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.3f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>
        /// Coloca as instâncias dos assets extras no subterrâneo: cristais no nicho secreto e nos cantos
        /// das salas escuras; teias nas paredes, perto do teto.
        /// </summary>
        private static void PlaceExtras()
        {
            Group(ExtrasGroupName);

            // Cristais Luminosos (pequena luz azulada; antecipam as cavernas de cristal do Nível 4 do GDD).
            Place(PropFactory.CrystalId, new Vector3(-10.2f, Y0, 7.15f), 30f);   // Sala 1 — nicho secreto
            Place(PropFactory.CrystalId, new Vector3(7.2f, Y0, 64.5f), -40f);   // Sala 3 — canto leste
            Place(PropFactory.CrystalId, new Vector3(-7.2f, Y0, 70.5f), 110f);  // Sala 3 — canto oeste
            Place(PropFactory.CrystalId, new Vector3(-9.2f, Y0, 98.5f), 200f);  // Sala 4 — atrás da divisória

            // Teias de Aranha (presas nas paredes, viradas para dentro das salas).
            Place(PropFactory.CobwebId, new Vector3(-5f, Y0 + 4.8f, 15.95f), 180f);  // Sala 1 — parede norte
            Place(PropFactory.CobwebId, new Vector3(-8.95f, Y0 + 5.5f, 30f), 90f);   // Sala 2 — parede oeste
            Place(PropFactory.CobwebId, new Vector3(2.45f, Y0 + 3.8f, 48f), -90f);   // Corredor das Pedras
            Place(PropFactory.CobwebId, new Vector3(5f, Y0 + 4.5f, 60.05f), 0f);     // Sala 3 — parede sul
            Place(PropFactory.CobwebId, new Vector3(9.95f, Y0 + 6f, 86f), -90f);     // Sala 4 — parede leste
        }
    }
}
