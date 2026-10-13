using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Cria os prefabs de todos os objetos do Nível 1 em estilo voxel, organizados por categoria:
    /// vegetação, arquitetura, props, tesouros, mecanismos, iluminação, armadilhas e inscrições.
    /// Cada prefab recebe a etiqueta AssetCategory para o inventário de assets.
    /// </summary>
    public static partial class PropFactory
    {
        /// <summary>Prefabs criados, por identificador.</summary>
        public static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();

        /// <summary>Material de física sem atrito (jogador) e com atrito (caixote).</summary>
        public static PhysicsMaterial PlayerPhysics, CratePhysics;

        /// <summary>
        /// Cria todos os prefabs do nível.
        /// </summary>
        public static void BuildAll()
        {
            Prefabs.Clear();
            CreatePhysicsMaterials();
            BuildVegetation();
            BuildArchitecture();
            BuildProps();
            BuildTreasures();
            BuildLighting();
            BuildTraps();
            BuildMechanisms();
        }

        /// <summary>
        /// Cria os materiais de física usados pelo jogador e pelos caixotes.
        /// </summary>
        private static void CreatePhysicsMaterials()
        {
            PlayerPhysics = new PhysicsMaterial("phys_player")
            {
                dynamicFriction = 0f, staticFriction = 0f, bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum
            };
            BuilderCore.SaveAsset(PlayerPhysics, BuilderCore.MaterialsPath + "/phys_player.physicMaterial");
            CratePhysics = new PhysicsMaterial("phys_crate")
            {
                dynamicFriction = 0.45f, staticFriction = 0.55f, bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Average, bounceCombine = PhysicsMaterialCombine.Minimum
            };
            BuilderCore.SaveAsset(CratePhysics, BuilderCore.MaterialsPath + "/phys_crate.physicMaterial");
        }

        // ================================================================== Auxiliares

        /// <summary>
        /// Começa um novo prefab: cria o GameObject raiz e aplica a malha voxel principal (se houver).
        /// </summary>
        /// <param name="id">Identificador/nome do prefab.</param>
        /// <param name="model">Modelo voxel principal (pode ser nulo).</param>
        /// <returns>O GameObject raiz.</returns>
        private static GameObject Begin(string id, VoxelModel model)
        {
            GameObject go = new GameObject(id);
            if (model != null && !model.IsEmpty) model.ApplyTo(go, BuilderCore.MeshesPath + "/" + id + ".asset");
            return go;
        }

        /// <summary>
        /// Cria um filho com malha voxel própria (partes móveis: cabo da alavanca, placa, botão...).
        /// </summary>
        /// <param name="parent">Objeto pai.</param>
        /// <param name="name">Nome do filho.</param>
        /// <param name="localPos">Posição local.</param>
        /// <param name="model">Modelo voxel do filho.</param>
        /// <returns>O filho criado.</returns>
        private static GameObject Child(GameObject parent, string name, Vector3 localPos, VoxelModel model)
        {
            GameObject c = new GameObject(name);
            c.transform.SetParent(parent.transform, false);
            c.transform.localPosition = localPos;
            if (model != null && !model.IsEmpty) model.ApplyTo(c, BuilderCore.MeshesPath + "/" + parent.name + "_" + name + ".asset");
            return c;
        }

        /// <summary>
        /// Adiciona um BoxCollider ajustado aos limites do modelo.
        /// </summary>
        /// <param name="go">Objeto.</param>
        /// <param name="model">Modelo de referência.</param>
        /// <param name="trigger">true para gatilho.</param>
        /// <returns>O colisor.</returns>
        private static BoxCollider Fit(GameObject go, VoxelModel model, bool trigger = false)
        {
            Bounds b = model.GetBounds();
            return Box(go, b.center, b.size, trigger);
        }

        /// <summary>
        /// Adiciona um BoxCollider com centro e tamanho explícitos.
        /// </summary>
        /// <param name="go">Objeto.</param>
        /// <param name="center">Centro local.</param>
        /// <param name="size">Tamanho.</param>
        /// <param name="trigger">true para gatilho.</param>
        /// <returns>O colisor.</returns>
        private static BoxCollider Box(GameObject go, Vector3 center, Vector3 size, bool trigger = false)
        {
            BoxCollider c = go.AddComponent<BoxCollider>();
            c.center = center;
            c.size = size;
            c.isTrigger = trigger;
            return c;
        }

        /// <summary>
        /// Adiciona uma luz pontual como filha.
        /// </summary>
        /// <param name="parent">Objeto pai.</param>
        /// <param name="name">Nome da luz.</param>
        /// <param name="pos">Posição local.</param>
        /// <param name="color">Cor.</param>
        /// <param name="range">Alcance.</param>
        /// <param name="intensity">Intensidade.</param>
        /// <param name="flicker">true para adicionar o tremeluzir de fogo.</param>
        /// <returns>A luz.</returns>
        public static Light AddLight(GameObject parent, string name, Vector3 pos, Color color, float range, float intensity, bool flicker = false)
        {
            GameObject l = new GameObject(name);
            l.transform.SetParent(parent.transform, false);
            l.transform.localPosition = pos;
            Light light = l.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            if (flicker) l.AddComponent<LightFlicker>();
            return light;
        }

        /// <summary>
        /// Coloca uma instância de um prefab de partículas como filha.
        /// </summary>
        /// <param name="parent">Objeto pai.</param>
        /// <param name="fx">Nome do prefab de partículas.</param>
        /// <param name="pos">Posição local.</param>
        /// <returns>O ParticleSystem da instância.</returns>
        public static ParticleSystem AddFx(GameObject parent, string fx, Vector3 pos)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(ParticleFactory.Prefabs[fx]);
            inst.transform.SetParent(parent.transform, false);
            inst.transform.localPosition = pos;
            return inst.GetComponent<ParticleSystem>();
        }

        /// <summary>
        /// Finaliza o prefab: adiciona a etiqueta de categoria, salva e remove a cópia temporária.
        /// </summary>
        /// <param name="go">Objeto raiz.</param>
        /// <param name="cat">Categoria.</param>
        /// <param name="display">Nome legível.</param>
        /// <returns>O prefab salvo.</returns>
        private static GameObject Save(GameObject go, AssetCategoryType cat, string display)
        {
            AssetCategory tag = go.AddComponent<AssetCategory>();
            tag.category = cat;
            tag.displayName = display;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, BuilderCore.PrefabsPath + "/" + go.name + ".prefab");
            Object.DestroyImmediate(go);
            Prefabs[prefab.name] = prefab;
            return prefab;
        }

        /// <summary>
        /// Atalho: cria um prefab estático só com malha e (opcionalmente) colisor ajustado.
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="model">Modelo voxel.</param>
        /// <param name="cat">Categoria.</param>
        /// <param name="display">Nome legível.</param>
        /// <param name="collider">true para adicionar colisor.</param>
        /// <returns>O prefab salvo.</returns>
        private static GameObject Simple(string id, VoxelModel model, AssetCategoryType cat, string display, bool collider)
        {
            GameObject go = Begin(id, model);
            if (collider) Fit(go, model);
            return Save(go, cat, display);
        }

        /// <summary>Atalho para Vector3.</summary>
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        // ================================================================== Vegetação

        /// <summary>
        /// Árvores, arbustos, samambaias, capim, cogumelos, flores, raízes e trepadeiras.
        /// </summary>
        private static void BuildVegetation()
        {
            const AssetCategoryType C = AssetCategoryType.Vegetacao;

            VoxelModel oak = new VoxelModel()
                .Add(V(0, 1.6f, 0), V(0.7f, 3.2f, 0.7f), Palette.Bark)
                .Add(V(0, 3.7f, 0), V(3.2f, 1.4f, 3.2f), Palette.Leaves)
                .Add(V(0, 4.7f, 0), V(2.2f, 1f, 2.2f), Palette.Leaves)
                .Add(V(1.2f, 3.3f, 0.6f), V(1.4f, 1.2f, 1.4f), Palette.LeavesLight)
                .Add(V(-1.1f, 3.5f, -0.8f), V(1.2f, 1f, 1.2f), Palette.LeavesLight)
                .Add(V(0.4f, 0.15f, 0.4f), V(0.5f, 0.3f, 0.3f), Palette.Roots)
                .Add(V(-0.45f, 0.12f, -0.2f), V(0.3f, 0.25f, 0.5f), Palette.Roots);
            GameObject go = Begin("Arvore_Carvalho", oak);
            Box(go, V(0, 1.6f, 0), V(0.7f, 3.2f, 0.7f));
            Save(go, C, "Árvore (carvalho)");

            VoxelModel pine = new VoxelModel()
                .Add(V(0, 1.4f, 0), V(0.5f, 2.8f, 0.5f), Palette.Bark)
                .Add(V(0, 2.4f, 0), V(2.8f, 0.9f, 2.8f), Palette.LeavesDark)
                .Add(V(0, 3.3f, 0), V(2.1f, 0.9f, 2.1f), Palette.LeavesDark)
                .Add(V(0, 4.2f, 0), V(1.4f, 0.9f, 1.4f), Palette.LeavesDark)
                .Add(V(0, 5f, 0), V(0.7f, 0.8f, 0.7f), Palette.LeavesDark);
            go = Begin("Arvore_Pinheiro", pine);
            Box(go, V(0, 1.4f, 0), V(0.5f, 2.8f, 0.5f));
            Save(go, C, "Árvore (pinheiro)");

            VoxelModel jungle = new VoxelModel()
                .Add(V(0, 2.5f, 0), V(0.6f, 5f, 0.6f), Palette.Bark)
                .Add(V(0, 5.3f, 0), V(3.6f, 0.8f, 3.6f), Palette.Leaves)
                .Add(V(0, 5.9f, 0), V(2f, 0.6f, 2f), Palette.LeavesLight)
                .Add(V(0.8f, 3.2f, 0), V(0.9f, 0.25f, 0.25f), Palette.Bark)
                .Add(V(1.3f, 3.5f, 0), V(0.8f, 0.6f, 0.8f), Palette.Leaves);
            go = Begin("Arvore_Tropical", jungle);
            Box(go, V(0, 2.5f, 0), V(0.6f, 5f, 0.6f));
            Save(go, C, "Árvore (tropical)");

            Simple("Arbusto", new VoxelModel()
                .Add(V(0, 0.4f, 0), V(1.2f, 0.8f, 1f), Palette.Leaves)
                .Add(V(0.3f, 0.75f, 0.1f), V(0.7f, 0.5f, 0.7f), Palette.LeavesLight)
                .Add(V(-0.4f, 0.3f, 0.3f), V(0.6f, 0.6f, 0.6f), Palette.LeavesDark), C, "Arbusto", false);

            VoxelModel fern = new VoxelModel();
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f;
                Vector3 dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                fern.Add(dir * 0.3f + V(0, 0.25f, 0), V(0.12f, 0.06f, 0.6f), Palette.LeavesLight, V(-25f, a, 0f));
            }
            Simple("Samambaia", fern, C, "Samambaia", false);

            VoxelModel grass = new VoxelModel();
            for (int i = 0; i < 7; i++)
            {
                float h = 0.2f + (i % 3) * 0.1f;
                grass.Add(V((i % 3 - 1) * 0.12f, h * 0.5f, (i / 3 - 1) * 0.12f), V(0.05f, h, 0.05f), Palette.Grass, V(0, i * 25f, (i % 2 == 0 ? 8f : -8f)));
            }
            Simple("Capim", grass, C, "Tufo de capim", false);

            Simple("Cogumelo_Vermelho", new VoxelModel()
                .Add(V(0, 0.15f, 0), V(0.12f, 0.3f, 0.12f), Palette.Stem)
                .Add(V(0, 0.35f, 0), V(0.42f, 0.14f, 0.42f), Palette.MushroomRed)
                .Add(V(0, 0.45f, 0), V(0.26f, 0.08f, 0.26f), Palette.MushroomRed), C, "Cogumelo vermelho", false);

            Simple("Cogumelos_Marrons", new VoxelModel()
                .Add(V(0, 0.1f, 0), V(0.08f, 0.2f, 0.08f), Palette.Stem).Add(V(0, 0.22f, 0), V(0.26f, 0.08f, 0.26f), Palette.MushroomBrown)
                .Add(V(0.2f, 0.07f, 0.1f), V(0.06f, 0.14f, 0.06f), Palette.Stem).Add(V(0.2f, 0.16f, 0.1f), V(0.18f, 0.06f, 0.18f), Palette.MushroomBrown)
                .Add(V(-0.15f, 0.06f, 0.15f), V(0.05f, 0.12f, 0.05f), Palette.Stem).Add(V(-0.15f, 0.13f, 0.15f), V(0.14f, 0.05f, 0.14f), Palette.MushroomBrown),
                C, "Cogumelos marrons", false);

            Simple("Flor_Amarela", Flower(Palette.FlowerYellow), C, "Flor amarela", false);
            Simple("Flor_Roxa", Flower(Palette.FlowerPurple), C, "Flor roxa", false);

            VoxelModel roots = new VoxelModel();
            for (int i = 0; i < 6; i++)
            {
                float len = 0.8f + (i * 37 % 10) / 10f * 1.8f;
                roots.Add(V((i % 3 - 1) * 0.35f, -len * 0.5f, (i / 3 - 0.5f) * 0.4f), V(0.12f, len, 0.12f), Palette.Roots, V(0, 0, (i % 2 == 0 ? 5f : -6f)));
            }
            roots.Add(V(0, -0.1f, 0), V(1.2f, 0.2f, 0.9f), Palette.Roots);
            Simple("Raizes_Penduradas", roots, C, "Raízes penduradas", false);

            VoxelModel vines = new VoxelModel();
            for (int i = 0; i < 9; i++)
            {
                float len = 1.6f + (i * 53 % 10) / 10f * 1.4f;
                vines.Add(V(-1f + i * 0.25f, 3f - len * 0.5f, 0), V(0.18f, len, 0.12f), (i % 2 == 0) ? Palette.Vine : Palette.Leaves);
            }
            vines.Add(V(0, 3f, 0), V(2.3f, 0.25f, 0.2f), Palette.Leaves);
            Simple("Cortina_de_Trepadeiras", vines, C, "Cortina de trepadeiras", false);

            Simple("Tronco_Caido", new VoxelModel()
                .Add(V(0, 0.3f, 0), V(0.6f, 0.6f, 3.2f), Palette.Bark)
                .Add(V(0, 0.62f, 0.4f), V(0.5f, 0.06f, 1f), Palette.Grass)
                .Add(V(0.25f, 0.4f, -1f), V(0.2f, 0.2f, 0.2f), Palette.MushroomBrown), C, "Tronco caído", true);

            Simple("Musgo", new VoxelModel()
                .Add(V(0, 0.03f, 0), V(1.2f, 0.06f, 0.9f), Palette.Vine)
                .Add(V(0.3f, 0.05f, 0.2f), V(0.5f, 0.06f, 0.5f), Palette.LeavesLight), C, "Tapete de musgo", false);
        }

        /// <summary>
        /// Modelo de uma flor simples (caule, folhas e pétalas).
        /// </summary>
        /// <param name="petal">Material das pétalas.</param>
        /// <returns>O modelo.</returns>
        private static VoxelModel Flower(Material petal)
        {
            return new VoxelModel()
                .Add(V(0, 0.18f, 0), V(0.04f, 0.36f, 0.04f), Palette.Vine)
                .Add(V(0.06f, 0.12f, 0), V(0.1f, 0.03f, 0.05f), Palette.LeavesLight)
                .Add(V(0, 0.4f, 0), V(0.18f, 0.06f, 0.18f), petal)
                .Add(V(0, 0.44f, 0), V(0.07f, 0.05f, 0.07f), Palette.FlowerYellow);
        }

        // ================================================================== Arquitetura

        /// <summary>
        /// Pilares, blocos, entulho, estátuas, cabeça de pedra, totem, relevo, viga e obelisco.
        /// </summary>
        private static void BuildArchitecture()
        {
            const AssetCategoryType C = AssetCategoryType.Arquitetura;

            Simple("Pilar", Pillar(Palette.StoneBrick, 4f), C, "Pilar de pedra", true);
            Simple("Pilar_Templo", Pillar(Palette.TempleBrick, 5f), C, "Pilar do templo", true);

            Simple("Pilar_Quebrado", new VoxelModel()
                .Add(V(0, 0.15f, 0), V(1.1f, 0.3f, 1.1f), Palette.StoneBrick)
                .Add(V(0, 1.1f, 0), V(0.8f, 1.6f, 0.8f), Palette.StoneBrick)
                .Add(V(0.1f, 2.05f, 0), V(0.6f, 0.4f, 0.7f), Palette.StoneBrick, V(0, 0, 12))
                .Add(V(1.1f, 0.3f, 0.4f), V(0.8f, 0.6f, 0.8f), Palette.StoneBrick, V(0, 20, 80)), C, "Pilar quebrado", true);

            Simple("Bloco_de_Pedra", new VoxelModel()
                .Add(V(0, 0.45f, 0), V(1.2f, 0.9f, 0.9f), Palette.StoneBrick)
                .Add(V(0.2f, 0.95f, 0.1f), V(0.5f, 0.1f, 0.4f), Palette.Vine), C, "Bloco de pedra", true);

            VoxelModel rubble = new VoxelModel();
            for (int i = 0; i < 7; i++)
            {
                float s = 0.15f + (i * 29 % 10) / 10f * 0.3f;
                rubble.Add(V(Mathf.Cos(i * 2.1f) * 0.6f, s * 0.5f, Mathf.Sin(i * 2.1f) * 0.5f), V(s, s, s * 1.2f), i % 3 == 0 ? Palette.StoneBrick : Palette.Rock, V(0, i * 33f, 0));
            }
            Simple("Entulho", rubble, C, "Entulho", false);

            VoxelModel statue = new VoxelModel()
                .Add(V(0, 0.2f, 0), V(1.1f, 0.4f, 1.1f), Palette.StoneDark)
                .Add(V(-0.2f, 0.85f, 0), V(0.3f, 0.9f, 0.35f), Palette.StoneBrick)
                .Add(V(0.2f, 0.85f, 0), V(0.3f, 0.9f, 0.35f), Palette.StoneBrick)
                .Add(V(0, 1.75f, 0), V(0.8f, 1f, 0.45f), Palette.StoneBrick)
                .Add(V(0, 2.55f, 0), V(0.55f, 0.6f, 0.5f), Palette.StoneBrick)
                .Add(V(0, 2.6f, 0.26f), V(0.4f, 0.08f, 0.04f), Palette.StoneDark)
                .Add(V(0, 2.95f, 0), V(0.7f, 0.2f, 0.6f), Palette.StoneDark)
                .Add(V(-0.5f, 1.7f, 0.1f), V(0.22f, 0.8f, 0.25f), Palette.StoneBrick)
                .Add(V(0.5f, 1.8f, 0.15f), V(0.22f, 0.7f, 0.25f), Palette.StoneBrick)
                .Add(V(0.62f, 1.9f, 0.2f), V(0.08f, 3f, 0.08f), Palette.WoodDark)
                .Add(V(0.62f, 3.5f, 0.2f), V(0.16f, 0.35f, 0.08f), Palette.Iron);
            GameObject go = Begin("Estatua_Guardiao", statue);
            Box(go, V(0, 1.5f, 0), V(1.1f, 3f, 1.1f));
            Save(go, C, "Estátua do guardião");

            Simple("Cabeca_de_Pedra", new VoxelModel()
                .Add(V(0, 0.9f, 0), V(1.6f, 1.8f, 1.4f), Palette.StoneDark)
                .Add(V(-0.38f, 1.2f, 0.71f), V(0.35f, 0.18f, 0.05f), Palette.Void)
                .Add(V(0.38f, 1.2f, 0.71f), V(0.35f, 0.18f, 0.05f), Palette.Void)
                .Add(V(0, 0.9f, 0.78f), V(0.3f, 0.45f, 0.2f), Palette.StoneBrick)
                .Add(V(0, 0.45f, 0.71f), V(0.7f, 0.15f, 0.05f), Palette.Void)
                .Add(V(0, 1.85f, 0), V(1.7f, 0.2f, 1.5f), Palette.StoneBrick)
                .Add(V(0.4f, 1.96f, 0.3f), V(0.6f, 0.05f, 0.5f), Palette.Vine), C, "Cabeça de pedra", true);

            Simple("Totem", new VoxelModel()
                .Add(V(0, 0.5f, 0), V(0.7f, 1f, 0.7f), Palette.WoodDark)
                .Add(V(0, 0.6f, 0.36f), V(0.5f, 0.12f, 0.04f), Palette.ClothRed)
                .Add(V(0, 1.5f, 0), V(0.75f, 1f, 0.75f), Palette.Wood)
                .Add(V(-0.15f, 1.65f, 0.38f), V(0.14f, 0.14f, 0.04f), Palette.Void)
                .Add(V(0.15f, 1.65f, 0.38f), V(0.14f, 0.14f, 0.04f), Palette.Void)
                .Add(V(0, 2.5f, 0), V(0.7f, 1f, 0.7f), Palette.WoodDark)
                .Add(V(0, 2.9f, 0), V(1.8f, 0.2f, 0.3f), Palette.ClothRed)
                .Add(V(0, 3.15f, 0), V(0.4f, 0.3f, 0.4f), Palette.Gold), C, "Totem", true);

            VoxelModel relief = new VoxelModel().Add(V(0, 0.75f, 0), V(2f, 1.5f, 0.15f), Palette.TempleBrick);
            for (int i = 0; i < 5; i++)
                relief.Add(V(-0.8f + i * 0.4f, 0.4f + (i % 2) * 0.6f, 0.1f), V(0.25f, 0.25f, 0.06f), Palette.StoneDark);
            relief.Add(V(0, 0.75f, 0.1f), V(0.5f, 0.5f, 0.06f), Palette.Gold);
            Simple("Relevo_na_Parede", relief, C, "Relevo entalhado", true);

            Simple("Viga_de_Madeira", new VoxelModel().Add(V(0, 0, 0), V(0.35f, 0.35f, 4f), Palette.WoodDark), C, "Viga de madeira", false);

            Simple("Obelisco", new VoxelModel()
                .Add(V(0, 0.25f, 0), V(1.2f, 0.5f, 1.2f), Palette.TempleBrick)
                .Add(V(0, 1.5f, 0), V(0.8f, 2f, 0.8f), Palette.TempleBrick)
                .Add(V(0, 3f, 0), V(0.6f, 1f, 0.6f), Palette.TempleBrick)
                .Add(V(0, 3.7f, 0), V(0.35f, 0.4f, 0.35f), Palette.Gold), C, "Obelisco", true);

            Simple("Arco_de_Pedra", new VoxelModel()
                .Add(V(-1.4f, 1.6f, 0), V(0.7f, 3.2f, 0.8f), Palette.TempleBrick)
                .Add(V(1.4f, 1.6f, 0), V(0.7f, 3.2f, 0.8f), Palette.TempleBrick)
                .Add(V(0, 3.5f, 0), V(3.6f, 0.6f, 0.9f), Palette.TempleBrick)
                .Add(V(0, 3.9f, 0), V(1f, 0.3f, 0.6f), Palette.Gold), C, "Arco de pedra", false);
        }

        /// <summary>
        /// Modelo de pilar com base e capitel.
        /// </summary>
        /// <param name="mat">Material da pedra.</param>
        /// <param name="height">Altura total.</param>
        /// <returns>O modelo.</returns>
        private static VoxelModel Pillar(Material mat, float height)
        {
            return new VoxelModel()
                .Add(V(0, 0.15f, 0), V(1.1f, 0.3f, 1.1f), mat)
                .Add(V(0, height * 0.5f, 0), V(0.8f, height - 0.6f, 0.8f), mat)
                .Add(V(0, height - 0.15f, 0), V(1.1f, 0.3f, 1.1f), mat)
                .Add(V(0.41f, height * 0.4f, 0), V(0.04f, height * 0.4f, 0.3f), Palette.Vine);
        }

        // ================================================================== Props

        /// <summary>
        /// Caixotes, barris, vasos, crânio, ossos, corda, mochila, diário, pá, saco, mesa, barraca e baú.
        /// </summary>
        private static void BuildProps()
        {
            const AssetCategoryType C = AssetCategoryType.Props;

            Simple("Caixote", Crate(1f, Palette.Wood), C, "Caixote", true);

            Simple("Barril", new VoxelModel()
                .Add(V(0, 0.5f, 0), V(0.8f, 1f, 0.6f), Palette.WoodDark)
                .Add(V(0, 0.5f, 0), V(0.6f, 1f, 0.8f), Palette.WoodDark)
                .Add(V(0, 0.2f, 0), V(0.84f, 0.08f, 0.84f), Palette.Iron)
                .Add(V(0, 0.8f, 0), V(0.84f, 0.08f, 0.84f), Palette.Iron), C, "Barril", true);

            Simple("Vaso_Pequeno", new VoxelModel()
                .Add(V(0, 0.2f, 0), V(0.45f, 0.4f, 0.45f), Palette.Clay)
                .Add(V(0, 0.45f, 0), V(0.28f, 0.12f, 0.28f), Palette.Clay)
                .Add(V(0, 0.25f, 0), V(0.47f, 0.06f, 0.47f), Palette.ClothRed), C, "Vaso pequeno", true);

            Simple("Vaso_Grande", new VoxelModel()
                .Add(V(0, 0.1f, 0), V(0.5f, 0.2f, 0.5f), Palette.Clay)
                .Add(V(0, 0.5f, 0), V(0.75f, 0.6f, 0.75f), Palette.Clay)
                .Add(V(0, 0.9f, 0), V(0.45f, 0.2f, 0.45f), Palette.Clay)
                .Add(V(0, 0.55f, 0), V(0.77f, 0.08f, 0.77f), Palette.Gold), C, "Vaso grande", true);

            Simple("Cranio", new VoxelModel()
                .Add(V(0, 0.14f, 0), V(0.26f, 0.22f, 0.28f), Palette.Bone)
                .Add(V(0, 0.04f, 0.06f), V(0.18f, 0.08f, 0.16f), Palette.Bone)
                .Add(V(-0.06f, 0.15f, 0.141f), V(0.07f, 0.07f, 0.01f), Palette.Void)
                .Add(V(0.06f, 0.15f, 0.141f), V(0.07f, 0.07f, 0.01f), Palette.Void), C, "Crânio", false);

            Simple("Ossos", new VoxelModel()
                .Add(V(0, 0.03f, 0), V(0.06f, 0.06f, 0.6f), Palette.Bone, V(0, 30, 0))
                .Add(V(0.2f, 0.03f, 0.1f), V(0.06f, 0.06f, 0.45f), Palette.Bone, V(0, -50, 0))
                .Add(V(-0.15f, 0.03f, -0.15f), V(0.05f, 0.05f, 0.35f), Palette.Bone, V(0, 80, 0)), C, "Ossos", false);

            VoxelModel rope = new VoxelModel();
            for (int i = 0; i < 3; i++)
            {
                float r = 0.32f - i * 0.04f, y = 0.04f + i * 0.07f;
                rope.Add(V(0, y, r), V(r * 2f, 0.07f, 0.07f), Palette.Rope).Add(V(0, y, -r), V(r * 2f, 0.07f, 0.07f), Palette.Rope)
                    .Add(V(r, y, 0), V(0.07f, 0.07f, r * 2f), Palette.Rope).Add(V(-r, y, 0), V(0.07f, 0.07f, r * 2f), Palette.Rope);
            }
            Simple("Rolo_de_Corda", rope, C, "Rolo de corda", false);

            Simple("Mochila_do_Explorador", new VoxelModel()
                .Add(V(0, 0.3f, 0), V(0.5f, 0.6f, 0.3f), Palette.Backpack)
                .Add(V(0, 0.45f, 0.17f), V(0.4f, 0.2f, 0.06f), Palette.Leather)
                .Add(V(0, 0.68f, 0), V(0.6f, 0.16f, 0.16f), Palette.ClothRed), C, "Mochila do explorador", false);

            Simple("Diario_do_Explorador", new VoxelModel()
                .Add(V(0, 0.03f, 0), V(0.3f, 0.06f, 0.4f), Palette.Leather)
                .Add(V(0.01f, 0.03f, 0), V(0.29f, 0.04f, 0.38f), Palette.Paper)
                .Add(V(0, 0.065f, 0.1f), V(0.31f, 0.01f, 0.03f), Palette.ClothRed), C, "Diário do explorador", false);

            Simple("Pa", new VoxelModel()
                .Add(V(0, 0.6f, 0), V(0.05f, 1.2f, 0.05f), Palette.Wood)
                .Add(V(0, 0.05f, 0), V(0.25f, 0.3f, 0.04f), Palette.Iron), C, "Pá", false);

            Simple("Saco", new VoxelModel()
                .Add(V(0, 0.28f, 0), V(0.5f, 0.56f, 0.45f), Palette.ClothTan)
                .Add(V(0, 0.62f, 0), V(0.2f, 0.12f, 0.2f), Palette.ClothTan)
                .Add(V(0, 0.58f, 0), V(0.24f, 0.04f, 0.24f), Palette.Rope), C, "Saco de suprimentos", false);

            Simple("Mesa_de_Campo", new VoxelModel()
                .Add(V(0, 0.75f, 0), V(1.6f, 0.08f, 0.8f), Palette.Wood)
                .Add(V(-0.7f, 0.37f, -0.32f), V(0.08f, 0.74f, 0.08f), Palette.WoodDark).Add(V(0.7f, 0.37f, -0.32f), V(0.08f, 0.74f, 0.08f), Palette.WoodDark)
                .Add(V(-0.7f, 0.37f, 0.32f), V(0.08f, 0.74f, 0.08f), Palette.WoodDark).Add(V(0.7f, 0.37f, 0.32f), V(0.08f, 0.74f, 0.08f), Palette.WoodDark)
                .Add(V(0.3f, 0.8f, 0.05f), V(0.5f, 0.02f, 0.4f), Palette.Paper), C, "Mesa de campo (com mapa)", true);

            Simple("Barraca", new VoxelModel()
                .Add(V(-0.55f, 0.75f, 0), V(0.06f, 1.6f, 2.2f), Palette.ClothTan, V(0, 0, -35))
                .Add(V(0.55f, 0.75f, 0), V(0.06f, 1.6f, 2.2f), Palette.ClothTan, V(0, 0, 35))
                .Add(V(0, 1.4f, 1.1f), V(0.06f, 1.4f, 0.06f), Palette.WoodDark)
                .Add(V(0, 1.4f, -1.1f), V(0.06f, 1.4f, 0.06f), Palette.WoodDark), C, "Barraca do acampamento", true);

            VoxelModel chest = new VoxelModel()
                .Add(V(0, 0.3f, 0), V(1f, 0.6f, 0.65f), Palette.Wood)
                .Add(V(0, 0.68f, 0), V(1f, 0.16f, 0.65f), Palette.WoodDark)
                .Add(V(0, 0.45f, 0.335f), V(0.15f, 0.2f, 0.02f), Palette.Gold)
                .Add(V(-0.45f, 0.4f, 0), V(0.06f, 0.8f, 0.67f), Palette.Gold)
                .Add(V(0.45f, 0.4f, 0), V(0.06f, 0.8f, 0.67f), Palette.Gold);
            Simple("Bau_de_Tesouro", chest, AssetCategoryType.Tesouros, "Baú de tesouro", true);
        }

        /// <summary>
        /// Modelo de caixote com moldura escura.
        /// </summary>
        /// <param name="s">Tamanho do lado.</param>
        /// <param name="mat">Material das tábuas.</param>
        /// <returns>O modelo.</returns>
        private static VoxelModel Crate(float s, Material mat)
        {
            float h = s * 0.5f, e = s * 0.1f;
            VoxelModel m = new VoxelModel().Add(V(0, h, 0), V(s, s, s), mat);
            // Arestas verticais escuras.
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * (h - e * 0.5f + 0.01f);
                float z = (i < 2 ? -1 : 1) * (h - e * 0.5f + 0.01f);
                m.Add(V(x, h, z), V(e, s + 0.02f, e), Palette.WoodDark);
            }
            m.Add(V(0, s - e * 0.5f + 0.01f, 0), V(s + 0.02f, e, s + 0.02f), Palette.WoodDark);
            m.Add(V(0, e * 0.5f - 0.01f, 0), V(s + 0.02f, e, s + 0.02f), Palette.WoodDark);
            return m;
        }

        // ================================================================== Tesouros

        /// <summary>
        /// Moeda, joias, coração, ídolo, máscara, colar de jade e pilha de ouro.
        /// </summary>
        private static void BuildTreasures()
        {
            const AssetCategoryType C = AssetCategoryType.Tesouros;

            Collectable("Moeda_de_Ouro", new VoxelModel()
                .Add(V(0, 0, 0), V(0.32f, 0.32f, 0.06f), Palette.Gold)
                .Add(V(0, 0, 0), V(0.22f, 0.4f, 0.06f), Palette.Gold)
                .Add(V(0, 0, 0), V(0.4f, 0.22f, 0.06f), Palette.Gold)
                .Add(V(0, 0, 0.035f), V(0.1f, 0.1f, 0.02f), Palette.IdolGlow), CollectibleType.Coin, 1, "", "Moeda de ouro", C, false);

            Collectable("Joia_Vermelha", Gem(Palette.Ruby), CollectibleType.Gem, 1, "", "Joia vermelha (rubi)", C, false);
            Collectable("Joia_Azul", Gem(Palette.Sapphire), CollectibleType.Gem, 1, "", "Joia azul (safira)", C, false);
            Collectable("Joia_Verde", Gem(Palette.Emerald), CollectibleType.Gem, 1, "", "Joia verde (esmeralda)", C, false);

            VoxelModel heart = new VoxelModel()
                .Add(V(-0.1f, 0.05f, 0), V(0.18f, 0.2f, 0.12f), Palette.HeartGlow)
                .Add(V(0.1f, 0.05f, 0), V(0.18f, 0.2f, 0.12f), Palette.HeartGlow)
                .Add(V(0, -0.08f, 0), V(0.22f, 0.18f, 0.12f), Palette.HeartGlow)
                .Add(V(0, -0.18f, 0), V(0.1f, 0.08f, 0.12f), Palette.HeartGlow);
            Collectable("Coracao", heart, CollectibleType.Heart, 1, "", "Coração (cura)", C, false);

            Simple("Idolo_de_Anang", Idol(), C, "Ídolo de Añang", false);

            VoxelModel mask = new VoxelModel()
                .Add(V(0, 0, 0), V(0.5f, 0.6f, 0.1f), Palette.Gold)
                .Add(V(-0.12f, 0.08f, 0.051f), V(0.1f, 0.06f, 0.02f), Palette.Void)
                .Add(V(0.12f, 0.08f, 0.051f), V(0.1f, 0.06f, 0.02f), Palette.Void)
                .Add(V(0, -0.15f, 0.051f), V(0.18f, 0.04f, 0.02f), Palette.Void)
                .Add(V(0, 0.22f, 0.06f), V(0.1f, 0.1f, 0.04f), Palette.Jade)
                .Add(V(-0.3f, 0, 0), V(0.1f, 0.3f, 0.08f), Palette.ClothRed)
                .Add(V(0.3f, 0, 0), V(0.1f, 0.3f, 0.08f), Palette.ClothRed);
            Collectable("Mascara_Charrua", mask, CollectibleType.Artifact, 1, "Máscara Charrúa", "Máscara Charrúa (artefato)", C, true);

            VoxelModel necklace = new VoxelModel();
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                necklace.Add(V(Mathf.Cos(a) * 0.25f, Mathf.Sin(a) * 0.25f, 0), V(0.08f, 0.08f, 0.08f), Palette.Jade);
            }
            necklace.Add(V(0, -0.33f, 0), V(0.14f, 0.16f, 0.05f), Palette.Gold);
            Collectable("Colar_de_Jade", necklace, CollectibleType.Artifact, 1, "Colar de Jade", "Colar de Jade (artefato)", C, true);

            VoxelModel pile = new VoxelModel()
                .Add(V(0, 0.08f, 0), V(1f, 0.16f, 0.8f), Palette.Gold)
                .Add(V(0.1f, 0.22f, 0), V(0.6f, 0.14f, 0.5f), Palette.Gold)
                .Add(V(0, 0.32f, 0.05f), V(0.3f, 0.1f, 0.3f), Palette.Gold)
                .Add(V(-0.35f, 0.2f, 0.2f), V(0.15f, 0.15f, 0.15f), Palette.Ruby, V(45, 45, 0));
            Simple("Pilha_de_Ouro", pile, C, "Pilha de ouro", false);
        }

        /// <summary>
        /// Modelo de joia lapidada (cubos girados 45°).
        /// </summary>
        /// <param name="mat">Material da joia.</param>
        /// <returns>O modelo.</returns>
        private static VoxelModel Gem(Material mat)
        {
            return new VoxelModel()
                .Add(V(0, 0, 0), V(0.22f, 0.22f, 0.22f), mat, V(45, 0, 45))
                .Add(V(0, 0, 0), V(0.2f, 0.2f, 0.2f), mat, V(0, 45, 0));
        }

        /// <summary>
        /// Modelo do Ídolo de Añang (figura dourada com olhos de esmeralda).
        /// </summary>
        /// <returns>O modelo.</returns>
        public static VoxelModel Idol()
        {
            return new VoxelModel()
                .Add(V(0, 0.05f, 0), V(0.36f, 0.1f, 0.3f), Palette.IdolGlow)
                .Add(V(0, 0.28f, 0), V(0.26f, 0.36f, 0.22f), Palette.IdolGlow)
                .Add(V(0, 0.56f, 0), V(0.3f, 0.24f, 0.26f), Palette.IdolGlow)
                .Add(V(-0.07f, 0.58f, 0.131f), V(0.06f, 0.05f, 0.02f), Palette.Emerald)
                .Add(V(0.07f, 0.58f, 0.131f), V(0.06f, 0.05f, 0.02f), Palette.Emerald)
                .Add(V(0, 0.72f, 0), V(0.38f, 0.06f, 0.1f), Palette.IdolGlow)
                .Add(V(-0.17f, 0.3f, 0), V(0.08f, 0.24f, 0.1f), Palette.IdolGlow)
                .Add(V(0.17f, 0.3f, 0), V(0.08f, 0.24f, 0.1f), Palette.IdolGlow);
        }

        /// <summary>
        /// Cria um prefab coletável: visual flutuando, gatilho esférico, Tag, Collectible, Spinner e brilho.
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="model">Modelo voxel.</param>
        /// <param name="type">Tipo de coletável.</param>
        /// <param name="value">Valor.</param>
        /// <param name="artifactName">Nome do artefato (somente artefatos).</param>
        /// <param name="display">Nome legível.</param>
        /// <param name="cat">Categoria.</param>
        /// <param name="glint">true para adicionar brilho contínuo.</param>
        private static void Collectable(string id, VoxelModel model, CollectibleType type, int value, string artifactName, string display, AssetCategoryType cat, bool glint)
        {
            GameObject go = new GameObject(id);
            go.tag = type == CollectibleType.Artifact ? GameTags.Artifact : GameTags.Collectible;
            GameObject visual = Child(go, "Visual", V(0, 0.6f, 0), model);
            Spinner spin = visual.AddComponent<Spinner>();
            spin.degreesPerSecond = V(0, type == CollectibleType.Artifact ? 45f : 120f, 0);
            spin.bobAmplitude = 0.08f;

            SphereCollider trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.center = V(0, 0.6f, 0);
            trigger.radius = 0.55f;

            Collectible c = go.AddComponent<Collectible>();
            c.type = type;
            c.value = value;
            c.artifactName = artifactName;
            c.pickupEffect = ParticleFactory.Prefabs["FX_Sparkle"];

            if (glint)
            {
                AddFx(go, "FX_Glint", V(0, 0.6f, 0));
                AddLight(go, "Brilho", V(0, 0.8f, 0), new Color(1f, 0.85f, 0.4f), 3f, 1.2f);
            }
            Save(go, cat, display);
        }
    }
}
