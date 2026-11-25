using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Prefabs do combate: armas (revólver, faca, arco, machado), projéteis físicos (bala, flecha, pedra),
    /// itens (revólver na mesa, faca coletável, pilha de pedras), armadilhas (espinhos, lâmina pendular,
    /// teto instável com pedregulhos e sua alavanca), mecanismos (grade de ferro, boneco de treino),
    /// objetos quebráveis (caixote frágil, barril explosivo, urna) e seus destroços.
    /// </summary>
    public static class L2Combat
    {
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        // ================================================================== Modelos de armas (usados também nos personagens)

        /// <summary>Revólver (cano em +Z, cabo para baixo). O tambor é uma peça separada (gira na recarga).</summary>
        /// <returns>Modelo do corpo do revólver.</returns>
        public static VoxelModel RevolverBody()
        {
            return new VoxelModel()
                .Add(V(0, 0.02f, 0.02f), V(0.05f, 0.08f, 0.14f), L2Palette.Gunmetal)
                .Add(V(0, 0.045f, 0.16f), V(0.035f, 0.035f, 0.2f), L2Palette.Gunmetal)
                .Add(V(0, 0.07f, 0.25f), V(0.012f, 0.02f, 0.012f), L2Palette.Brass)
                .Add(V(0, -0.06f, -0.05f), V(0.045f, 0.13f, 0.06f), L2Palette.Grip, V(15, 0, 0))
                .Add(V(0, 0.08f, -0.045f), V(0.02f, 0.03f, 0.03f), L2Palette.Gunmetal)
                .Add(V(0, -0.03f, 0.035f), V(0.015f, 0.035f, 0.06f), L2Palette.Brass);
        }

        /// <summary>Tambor do revólver (6 câmaras).</summary>
        /// <returns>Modelo do tambor.</returns>
        public static VoxelModel RevolverCylinder()
        {
            return new VoxelModel()
                .Add(V(0, 0, 0), V(0.075f, 0.075f, 0.07f), L2Palette.Gunmetal)
                .Add(V(0, 0, 0), V(0.05f, 0.09f, 0.072f), L2Palette.Gunmetal, V(0, 0, 45))
                .Add(V(0, 0, 0.037f), V(0.02f, 0.02f, 0.005f), L2Palette.Brass);
        }

        /// <summary>Faca de John (lâmina em +Z).</summary>
        /// <returns>Modelo.</returns>
        public static VoxelModel Knife()
        {
            return new VoxelModel()
                .Add(V(0, 0, -0.06f), V(0.03f, 0.04f, 0.12f), L2Palette.Leather)
                .Add(V(0, 0, 0.005f), V(0.08f, 0.02f, 0.02f), L2Palette.Brass)
                .Add(V(0, 0, 0.11f), V(0.015f, 0.035f, 0.2f), L2Palette.Blade)
                .Add(V(0, -0.006f, 0.225f), V(0.012f, 0.02f, 0.04f), L2Palette.Blade);
        }

        /// <summary>Faca enferrujada dos esqueletos (lâmina em +Z).</summary>
        /// <returns>Modelo.</returns>
        public static VoxelModel RustyKnife()
        {
            return new VoxelModel()
                .Add(V(0, 0, -0.06f), V(0.035f, 0.04f, 0.12f), L2Palette.Cloth)
                .Add(V(0, 0, 0.13f), V(0.018f, 0.05f, 0.26f), L2Palette.Iron)
                .Add(V(0, 0.01f, 0.27f), V(0.015f, 0.03f, 0.04f), L2Palette.Iron);
        }

        /// <summary>Machado pesado do brutamontes (lâmina em +Z, na ponta do cabo).</summary>
        /// <returns>Modelo.</returns>
        public static VoxelModel Axe()
        {
            return new VoxelModel()
                .Add(V(0, 0, 0.35f), V(0.06f, 0.06f, 1.1f), L2Palette.WoodDark)
                .Add(V(0, 0.16f, 0.82f), V(0.05f, 0.42f, 0.28f), L2Palette.Iron)
                .Add(V(0, 0.38f, 0.86f), V(0.05f, 0.08f, 0.2f), L2Palette.Iron)
                .Add(V(0, -0.06f, 0.82f), V(0.05f, 0.1f, 0.16f), L2Palette.IronDark);
        }

        /// <summary>Arco dos arqueiros (braços do arco ao longo de Z).</summary>
        /// <returns>Modelo.</returns>
        public static VoxelModel Bow()
        {
            return new VoxelModel()
                .Add(V(0, 0, 0), V(0.05f, 0.05f, 0.14f), L2Palette.Leather)
                .Add(V(0, -0.05f, 0.3f), V(0.035f, 0.035f, 0.48f), L2Palette.WoodDark, V(-14, 0, 0))
                .Add(V(0, -0.05f, -0.3f), V(0.035f, 0.035f, 0.48f), L2Palette.WoodDark, V(14, 0, 0))
                .Add(V(0, 0.06f, 0), V(0.01f, 0.01f, 1.02f), L2Palette.Rope);
        }

        /// <summary>Aljava com flechas (presa nas costas).</summary>
        /// <returns>Modelo.</returns>
        public static VoxelModel Quiver()
        {
            return new VoxelModel()
                .Add(V(0, 0, 0), V(0.16f, 0.5f, 0.14f), L2Palette.Leather)
                .Add(V(-0.03f, 0.32f, 0), V(0.02f, 0.2f, 0.02f), L2Palette.WoodDark)
                .Add(V(0.03f, 0.34f, 0.02f), V(0.02f, 0.22f, 0.02f), L2Palette.WoodDark)
                .Add(V(-0.03f, 0.44f, 0), V(0.05f, 0.06f, 0.01f), L2Palette.FeatherRed)
                .Add(V(0.03f, 0.46f, 0.02f), V(0.05f, 0.06f, 0.01f), L2Palette.Feather);
        }

        // ================================================================== Construção

        /// <summary>
        /// Cria todos os prefabs do combate.
        /// </summary>
        public static void BuildAll()
        {
            BuildDebris();
            BuildProjectiles();
            BuildPickups();
            BuildTraps();
            BuildMechanisms();
            BuildDestructibles();
        }

        /// <summary>
        /// Destroços físicos soltos pelos objetos quebráveis.
        /// </summary>
        private static void BuildDebris()
        {
            Debris("Lasca_de_Madeira", "Lascas de madeira (destroço)", new VoxelModel().Add(Vector3.zero, V(0.5f, 0.06f, 0.12f), L2Palette.Wood), V(0.5f, 0.06f, 0.12f), 0.5f);
            Debris("Aduela_de_Barril", "Aduelas de barril (destroço)", new VoxelModel()
                .Add(Vector3.zero, V(0.12f, 0.7f, 0.04f), L2Palette.BarrelRed)
                .Add(V(0, 0.2f, 0.025f), V(0.13f, 0.05f, 0.01f), L2Palette.IronDark), V(0.12f, 0.7f, 0.05f), 0.6f);
            Debris("Caco_de_Urna", "Cacos de urna (destroço)", new VoxelModel().Add(Vector3.zero, V(0.18f, 0.06f, 0.14f), L2Palette.BoneDark, V(0, 0, 20)), V(0.18f, 0.08f, 0.14f), 0.3f);
        }

        /// <summary>
        /// Cria um destroço físico (Rigidbody + colisor) na camada dos itens (não atrapalha tiros nem inimigos).
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="display">Nome legível.</param>
        /// <param name="model">Modelo.</param>
        /// <param name="size">Tamanho do colisor.</param>
        /// <param name="mass">Massa.</param>
        private static void Debris(string id, string display, VoxelModel model, Vector3 size, float mass)
        {
            GameObject go = L2Core.Begin(id, model);
            L2Core.Box(go, Vector3.zero, size);
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            go.layer = L2Core.Layer(CombatLayers.InteractableLayerName);
            L2Core.Save(go, AssetCategoryType.Props, display);
        }

        /// <summary>
        /// Bala do revólver, flecha dos arqueiros e pedra arremessada.
        /// </summary>
        private static void BuildProjectiles()
        {
            int projLayer = L2Core.Layer(CombatLayers.ProjectileLayerName);
            Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");

            // Bala: rápida, sem gravidade, rastro dourado curto.
            GameObject bullet = L2Core.Begin("Bala", new VoxelModel().Add(Vector3.zero, V(0.025f, 0.025f, 0.07f), L2Palette.Brass));
            bullet.tag = CombatTags.Projectile;
            bullet.layer = projLayer;
            L2Core.Sphere(bullet, Vector3.zero, 0.05f, true);
            Rigidbody brb = bullet.AddComponent<Rigidbody>();
            brb.mass = 0.02f;
            brb.useGravity = false;
            Projectile bp = bullet.AddComponent<Projectile>();
            bp.damage = 2f;
            bp.source = DamageSource.Bullet;
            bp.force = 3f;
            bp.lifetime = 2.5f;
            bp.radius = 0.04f;
            bp.useGravity = false;
            bp.surfaceEffects = new[] { L2Core.Get("FX2_Faiscas"), L2Core.Get("FX2_Poeira_de_Impacto"), L2Core.L1("FX_Pebbles") };
            bp.hitEffects = new[] { L2Core.Get("FX2_Lascas_de_Osso"), L2Core.Get("FX2_Poeira_de_Impacto") };
            bp.surfaceClips = L2Core.Clips("bullet_ricochet_1", "bullet_ricochet_2");
            bp.hitClips = L2Core.Clips("bullet_bone_hit");
            bp.trail = AddTrail(bullet, trailShader, "mat2_rastro_bala", new Color(1f, 0.85f, 0.45f, 0.9f), 0.07f, 0.03f);
            L2Core.Save(bullet, AssetCategoryType.Armas, "Bala do revólver (projétil)");

            // Flecha: origem na ponta; usa gravidade (trajetória em arco) e fica cravada nas paredes.
            GameObject arrow = L2Core.Begin("Flecha", new VoxelModel()
                .Add(V(0, 0, -0.4f), V(0.025f, 0.025f, 0.72f), L2Palette.WoodDark)
                .Add(V(0, 0, -0.03f), V(0.05f, 0.05f, 0.07f), L2Palette.Iron, V(0, 0, 45))
                .Add(V(0, 0.035f, -0.7f), V(0.01f, 0.06f, 0.12f), L2Palette.Feather)
                .Add(V(0.035f, 0, -0.7f), V(0.06f, 0.01f, 0.12f), L2Palette.FeatherRed));
            arrow.tag = CombatTags.Projectile;
            arrow.layer = projLayer;
            L2Core.Sphere(arrow, V(0, 0, -0.08f), 0.07f, true);
            Rigidbody arb = arrow.AddComponent<Rigidbody>();
            arb.mass = 0.05f;
            arb.useGravity = true;
            Projectile ap = arrow.AddComponent<Projectile>();
            ap.damage = 1f;
            ap.source = DamageSource.Arrow;
            ap.force = 3f;
            ap.lifetime = 5f;
            ap.radius = 0.05f;
            ap.useGravity = true;
            ap.stickOnImpact = true;
            ap.stickTime = 8f;
            ap.surfaceEffects = new[] { L2Core.Get("FX2_Poeira_de_Impacto") };
            ap.hitEffects = new[] { L2Core.Get("FX2_Poeira_de_Impacto") };
            ap.surfaceClips = L2Core.Clips("arrow_impact");
            ap.hitClips = L2Core.Clips("arrow_impact");
            ap.trail = AddTrail(arrow, trailShader, "mat2_rastro_flecha", new Color(0.9f, 0.9f, 0.85f, 0.35f), 0.15f, 0.02f);
            L2Core.Save(arrow, AssetCategoryType.Armas, "Flecha (projétil)");

            // Pedra arremessada: corpo físico normal (quica), atordoa e faz barulho.
            GameObject stone = L2Core.Begin("Pedra_Arremessada", new VoxelModel()
                .Add(Vector3.zero, V(0.18f, 0.15f, 0.17f), L2Palette.Rock)
                .Add(V(0.04f, 0.03f, 0), V(0.12f, 0.12f, 0.14f), L2Palette.Rock, V(30, 20, 10)));
            stone.tag = CombatTags.Projectile;
            stone.layer = projLayer;
            L2Core.Box(stone, Vector3.zero, V(0.18f, 0.16f, 0.17f));
            Rigidbody srb = stone.AddComponent<Rigidbody>();
            srb.mass = 0.6f;
            ThrownStone ts = stone.AddComponent<ThrownStone>();
            ts.impactEffects = new[] { L2Core.Get("FX2_Poeira_de_Impacto") };
            L2Core.Save(stone, AssetCategoryType.Armas, "Pedra arremessada (projétil)");
        }

        /// <summary>
        /// Adiciona um rastro (TrailRenderer) a um projétil.
        /// </summary>
        /// <param name="go">Projétil.</param>
        /// <param name="shader">Shader das partículas.</param>
        /// <param name="matName">Nome do material.</param>
        /// <param name="color">Cor.</param>
        /// <param name="time">Duração do rastro.</param>
        /// <param name="width">Largura inicial.</param>
        /// <returns>O rastro.</returns>
        private static TrailRenderer AddTrail(GameObject go, Shader shader, string matName, Color color, float time, float width)
        {
            GameObject t = new GameObject("Rastro");
            t.transform.SetParent(go.transform, false);
            TrailRenderer trail = t.AddComponent<TrailRenderer>();
            Material m = new Material(shader) { name = matName };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            BuilderCore.SaveAsset(m, L2Core.MaterialsPath + "/" + matName + ".mat");
            trail.sharedMaterial = m;
            trail.time = time;
            trail.startWidth = width;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = g;
            return trail;
        }

        /// <summary>
        /// Revólver sobre a mesa (pego com E) e a faca coletável (deixada pelos esqueletos).
        /// </summary>
        private static void BuildPickups()
        {
            int interactable = L2Core.Layer(CombatLayers.InteractableLayerName);

            // Mesa com o revólver, o coldre e uma caixa de balas.
            VoxelModel table = new VoxelModel()
                .Add(V(0, 0.75f, 0), V(1.3f, 0.08f, 0.7f), L2Palette.Wood)
                .Add(V(-0.55f, 0.37f, -0.27f), V(0.08f, 0.74f, 0.08f), L2Palette.WoodDark)
                .Add(V(0.55f, 0.37f, -0.27f), V(0.08f, 0.74f, 0.08f), L2Palette.WoodDark)
                .Add(V(-0.55f, 0.37f, 0.27f), V(0.08f, 0.74f, 0.08f), L2Palette.WoodDark)
                .Add(V(0.55f, 0.37f, 0.27f), V(0.08f, 0.74f, 0.08f), L2Palette.WoodDark)
                .Add(V(0.35f, 0.83f, 0.1f), V(0.3f, 0.08f, 0.15f), L2Palette.Leather)
                .Add(V(-0.4f, 0.84f, -0.12f), V(0.2f, 0.1f, 0.14f), L2Palette.Brass)
                .Add(V(0.1f, 0.8f, -0.2f), V(0.35f, 0.02f, 0.25f), L2Palette.Paper);
            GameObject go = L2Core.Begin("Mesa_do_Revolver", table);
            L2Core.Box(go, V(0, 0.4f, 0), V(1.3f, 0.8f, 0.7f));

            GameObject pickup = new GameObject("Revolver_Coleta");
            pickup.transform.SetParent(go.transform, false);
            pickup.transform.localPosition = V(-0.1f, 0.84f, 0.1f);
            pickup.tag = CombatTags.Weapon;
            pickup.layer = interactable;
            GameObject gun = L2Core.Child(pickup, "Modelo", Vector3.zero, RevolverBody());
            gun.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            L2Core.Child(gun, "Tambor", V(0, 0.03f, 0.04f), RevolverCylinder());
            L2Core.Sphere(pickup, Vector3.zero, 0.6f, true);
            WeaponPickup wp = pickup.AddComponent<WeaponPickup>();
            wp.kind = PickupKind.Revolver;
            wp.shots = 25;
            wp.autoPickup = false;
            wp.prompt = "Pegar o revólver";
            wp.pickupEffect = L2Core.L1("FX_Sparkle");
            wp.toastTitle = "Revólver de John";
            wp.toastBody = "25 balas para o nível inteiro: tambor de 6 + 19 na reserva. Não há mais munição nas catacumbas!";
            wp.tutorialOnPickup = "Clique esquerdo: atirar  ·  Botão direito: mirar\n[R] recarregar o tambor  ·  [V] coronhada (não gasta balas)\n[G] arremessar pedra  ·  Tiro na cabeça causa o dobro de dano.";
            wp.lineOnPickup = "Meu velho revólver... só 25 balas. Vou ter que escolher bem cada tiro.";
            wp.objectiveOnPickup = "Treine a coronhada [V] no boneco e avance pela cripta.";
            wp.highlightLight = L2Core.PointLight(pickup, "Destaque", V(0, 0.35f, 0), new Color(1f, 0.85f, 0.5f), 2.2f, 1.2f);
            L2Core.Save(go, AssetCategoryType.Armas, "Revólver na mesa (coleta)");

            // Faca coletável (gira e brilha; pega ao encostar).
            GameObject knife = new GameObject("Faca_Coletavel");
            knife.tag = CombatTags.Weapon;
            knife.layer = interactable;
            GameObject visual = L2Core.Child(knife, "Visual", V(0, 0.5f, 0), Knife());
            visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Spinner spin = visual.AddComponent<Spinner>();
            spin.degreesPerSecond = V(0, 120f, 0);
            spin.bobAmplitude = 0.08f;
            L2Core.Sphere(knife, V(0, 0.5f, 0), 0.6f, true);
            WeaponPickup kp = knife.AddComponent<WeaponPickup>();
            kp.kind = PickupKind.Knife;
            kp.autoPickup = true;
            kp.prompt = "Pegar a faca";
            kp.pickupEffect = L2Core.L1("FX_Sparkle");
            kp.toastTitle = "Faca encontrada!";
            kp.toastBody = "Golpes rápidos e fortes de perto. [2] ou Q para empunhar; clique ou [V] para golpear.";
            kp.lineOnPickup = "Uma faca! Agora posso economizar balas.";
            L2Core.AddFx(knife, "FX_Glint", V(0, 0.5f, 0));
            L2Core.PointLight(knife, "Brilho", V(0, 0.8f, 0), new Color(0.8f, 0.9f, 1f), 2.5f, 1f);
            L2Core.Save(knife, AssetCategoryType.Armas, "Faca (coletável)");
        }

        /// <summary>
        /// Espinhos no piso, lâmina pendular, pedregulhos, teto instável e a alavanca enferrujada.
        /// </summary>
        private static void BuildTraps()
        {
            const AssetCategoryType C = AssetCategoryType.Armadilhas;

            // Armadilha de espinhos (placa 2×2 m; espinhos escondidos abaixo do piso).
            VoxelModel plate = new VoxelModel().Add(V(0, 0.02f, 0), V(2f, 0.04f, 2f), L2Palette.IronDark);
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    plate.Add(V(-0.72f + i * 0.48f, 0.041f, -0.72f + j * 0.48f), V(0.14f, 0.005f, 0.14f), L2Palette.Void);
            GameObject go = L2Core.Begin("Armadilha_de_Espinhos", plate);
            VoxelModel spikes = new VoxelModel();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Vector3 p = V(-0.72f + i * 0.48f, 0, -0.72f + j * 0.48f);
                    spikes.Add(p + V(0, 0.25f, 0), V(0.09f, 0.5f, 0.09f), L2Palette.Iron)
                          .Add(p + V(0, 0.56f, 0), V(0.05f, 0.12f, 0.05f), L2Palette.Blade, V(0, 45, 0));
                }
            }
            GameObject spikeGroup = L2Core.Child(go, "Espinhos", V(0, -0.55f, 0), spikes);
            L2Core.Box(go, V(0, 0.4f, 0), V(1.9f, 0.8f, 1.9f), true);
            SpikeTrap st = go.AddComponent<SpikeTrap>();
            st.spikes = spikeGroup.transform;
            st.effects = new[] { L2Core.Get("FX2_Poeira_dos_Espinhos") };
            L2Core.Save(go, C, "Armadilha de espinhos");

            // Lâmina pendular: suporte fixo no teto + braço físico (Rigidbody + HingeJoint) que balança ao longo de Z.
            go = L2Core.Begin("Lamina_Pendular", new VoxelModel()
                .Add(V(0, 0.1f, 0), V(0.6f, 0.2f, 0.6f), L2Palette.IronDark)
                .Add(V(0, 0, 0), V(0.2f, 0.2f, 0.4f), L2Palette.Iron));
            GameObject arm = L2Core.Child(go, "Braco", Vector3.zero, new VoxelModel()
                .Add(V(0, -1.5f, 0), V(0.1f, 3f, 0.1f), L2Palette.IronDark)
                .Add(V(0, -3.05f, 0), V(3.1f, 0.35f, 0.06f), L2Palette.Blade)
                .Add(V(0, -3.3f, 0), V(2.6f, 0.15f, 0.05f), L2Palette.Blade)
                .Add(V(0, -2.85f, 0), V(3.2f, 0.1f, 0.1f), L2Palette.IronDark));
            Rigidbody rb = arm.AddComponent<Rigidbody>();
            rb.mass = 40f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.02f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            HingeJoint hinge = arm.AddComponent<HingeJoint>();
            hinge.anchor = Vector3.zero;
            hinge.axis = V(1, 0, 0);
            hinge.autoConfigureConnectedAnchor = true;
            hinge.useLimits = true;
            hinge.limits = new JointLimits { min = -80f, max = 80f };
            PendulumTrap pend = arm.AddComponent<PendulumTrap>();
            pend.armLength = 3.05f;
            GameObject bladeTrigger = new GameObject("Lamina");
            bladeTrigger.transform.SetParent(arm.transform, false);
            L2Core.Box(bladeTrigger, V(0, -3.1f, 0), V(3.1f, 0.6f, 0.3f), true);
            TrapDamage td = bladeTrigger.AddComponent<TrapDamage>();
            td.velocitySource = rb;
            td.enemyDamage = 5f;
            L2Core.Save(go, C, "Lâmina pendular");

            // Pedregulhos que despencam do teto instável (vetor de dois tamanhos).
            Boulder("Pedregulho_Grande", "Pedregulho grande", 0.9f, 300f);
            Boulder("Pedregulho_Medio", "Pedregulho médio", 0.6f, 150f);

            // Teto instável: rachaduras + armadilha com 9 pontos de queda.
            VoxelModel cracks = new VoxelModel();
            for (int i = 0; i < 9; i++)
            {
                float a = i * 0.7f;
                cracks.Add(V(Mathf.Cos(a * 3f) * 1.8f, 0, Mathf.Sin(a * 2f) * 1.8f), V(1.6f, 0.05f, 0.12f), L2Palette.Void, V(0, i * 40f, 0));
            }
            cracks.Add(V(0, -0.05f, 0), V(0.8f, 0.1f, 0.8f), L2Palette.StoneDark, V(0, 20, 0));
            go = L2Core.Begin("Teto_Instavel", cracks);
            CeilingCollapseTrap ct = go.AddComponent<CeilingCollapseTrap>();
            ct.rockPrefabs = new[] { L2Core.Get("Pedregulho_Grande"), L2Core.Get("Pedregulho_Medio") };
            ct.warningEffect = L2Core.L1("FX_WarningDust");
            Transform[] points = new Transform[9];
            for (int i = 0; i < 9; i++)
            {
                GameObject p = new GameObject("PontoDeQueda_" + (i + 1));
                p.transform.SetParent(go.transform, false);
                p.transform.localPosition = V(-2.4f + (i % 3) * 2.4f, -0.6f, -2.4f + (i / 3) * 2.4f);
                points[i] = p.transform;
            }
            ct.dropPoints = points;
            ct.rockCount = 8;
            L2Core.Save(go, C, "Teto instável (desabamento)");

            // Alavanca enferrujada (dispara o teto instável).
            go = L2Core.Begin("Alavanca_Enferrujada", new VoxelModel()
                .Add(V(0, 0.1f, 0), V(0.6f, 0.2f, 0.4f), L2Palette.StoneDark)
                .Add(V(0, 0.25f, 0), V(0.4f, 0.1f, 0.25f), L2Palette.Iron));
            GameObject handle = L2Core.Child(go, "Cabo", V(0, 0.3f, 0), new VoxelModel()
                .Add(V(0, 0.4f, 0), V(0.08f, 0.8f, 0.08f), L2Palette.IronDark)
                .Add(V(0, 0.82f, 0), V(0.14f, 0.14f, 0.14f), L2Palette.Iron));
            L2Core.Box(go, V(0, 0.5f, 0), V(0.6f, 1f, 0.6f));
            go.layer = L2Core.Layer(CombatLayers.InteractableLayerName);
            TrapLever lever = go.AddComponent<TrapLever>();
            lever.handle = handle.transform;
            lever.prompt = "Puxar a alavanca";
            lever.firstTimeTip = "Esta alavanca derruba o teto rachado! Atraia os esqueletos para baixo dele antes de puxar.";
            lever.lineOnUse = "Cuidado aí embaixo!";
            lever.highlightLight = L2Core.PointLight(go, "Destaque", V(0, 1.4f, 0.3f), new Color(1f, 0.6f, 0.3f), 2.5f, 1.2f);
            L2Core.Save(go, C, "Alavanca enferrujada");
        }

        /// <summary>
        /// Pedregulho físico que esmaga ao cair (CrushingRock).
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="display">Nome legível.</param>
        /// <param name="size">Tamanho.</param>
        /// <param name="mass">Massa.</param>
        private static void Boulder(string id, string display, float size, float mass)
        {
            GameObject go = L2Core.Begin(id, new VoxelModel()
                .Add(Vector3.zero, V(size, size * 0.85f, size), L2Palette.Rock)
                .Add(V(size * 0.2f, size * 0.15f, 0), V(size * 0.7f, size * 0.7f, size * 0.8f), L2Palette.Rock, V(25, 35, 15))
                .Add(V(-size * 0.2f, -size * 0.1f, size * 0.1f), V(size * 0.6f, size * 0.6f, size * 0.6f), L2Palette.StoneDark, V(10, 60, 30)));
            L2Core.Box(go, Vector3.zero, V(size, size * 0.85f, size));
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            CrushingRock cr = go.AddComponent<CrushingRock>();
            cr.impactEffects = new[] { L2Core.L1("FX_Explosion"), L2Core.L1("FX_Pebbles") };
            L2Core.Save(go, AssetCategoryType.Armadilhas, display);
        }

        /// <summary>
        /// Grade de ferro (portão levadiço), boneco de treino e pilha de pedras.
        /// </summary>
        private static void BuildMechanisms()
        {
            const AssetCategoryType C = AssetCategoryType.Mecanismos;

            // Grade de ferro: barras verticais com pontas, travessas, colisor, obstáculo de navegação e poeira.
            VoxelModel gate = new VoxelModel();
            for (int i = 0; i < 12; i++)
            {
                float x = -1.65f + i * 0.3f;
                gate.Add(V(x, 1.9f, 0), V(0.08f, 3.8f, 0.08f), L2Palette.IronDark)
                    .Add(V(x, -0.08f, 0), V(0.05f, 0.16f, 0.05f), L2Palette.Iron, V(0, 45, 0));
            }
            foreach (float y in new[] { 0.6f, 2f, 3.4f })
                gate.Add(V(0, y, 0), V(3.6f, 0.1f, 0.12f), L2Palette.Iron);
            GameObject go = L2Core.Begin("Grade_de_Ferro", gate);
            L2Core.Box(go, V(0, 1.9f, 0), V(3.6f, 3.8f, 0.25f));
            NavMeshObstacle obstacle = L2Core.Obstacle(go, V(0, 1.9f, 0), V(3.6f, 3.8f, 0.6f));
            obstacle.carveOnlyStationary = false;
            IronGate ig = go.AddComponent<IronGate>();
            ig.obstacle = obstacle;
            ig.openHeight = 3.9f;
            ig.dust = L2Core.AddFx(go, "FX_DoorDust", V(0, 0.1f, 0));
            ig.requiredSignals = 1;
            ig.latch = true;
            L2Core.Save(go, C, "Grade de ferro (portão levadiço)");

            // Boneco de treino: poste fixo + corpo de palha que balança.
            go = L2Core.Begin("Boneco_de_Treino", new VoxelModel()
                .Add(V(0, 0.05f, 0), V(0.7f, 0.1f, 0.7f), L2Palette.WoodDark)
                .Add(V(0, 0.4f, 0), V(0.12f, 0.7f, 0.12f), L2Palette.WoodDark));
            GameObject body = L2Core.Child(go, "Corpo", V(0, 0.7f, 0), new VoxelModel()
                .Add(V(0, 0.5f, 0), V(0.5f, 0.75f, 0.35f), L2Palette.Straw)
                .Add(V(0, 0.5f, 0), V(0.52f, 0.08f, 0.37f), L2Palette.Rope)
                .Add(V(0, 0.75f, 0), V(1.1f, 0.12f, 0.12f), L2Palette.WoodDark)
                .Add(V(0, 1.1f, 0), V(0.35f, 0.35f, 0.32f), L2Palette.Cloth)
                .Add(V(-0.07f, 1.13f, 0.165f), V(0.06f, 0.06f, 0.01f), L2Palette.Void)
                .Add(V(0.07f, 1.13f, 0.165f), V(0.06f, 0.06f, 0.01f), L2Palette.Void)
                .Add(V(0, 0.98f, 0.165f), V(0.14f, 0.03f, 0.01f), L2Palette.Void));
            L2Core.Box(go, V(0, 1.1f, 0), V(0.6f, 2.2f, 0.5f));
            TrainingDummy dummy = go.AddComponent<TrainingDummy>();
            dummy.pivot = body.transform;
            dummy.hitEffects = new[] { L2Core.Get("FX2_Palha") };
            L2Core.Save(go, C, "Boneco de treino");

            // Pilha de pedras (pegar com E, arremessar com G).
            go = new GameObject("Pilha_de_Pedras");
            go.layer = L2Core.Layer(CombatLayers.InteractableLayerName);
            L2Core.Child(go, "Base", Vector3.zero, new VoxelModel().Add(V(0, 0.05f, 0), V(0.9f, 0.1f, 0.7f), L2Palette.Rock));
            GameObject[] stones = new GameObject[5];
            for (int i = 0; i < 5; i++)
            {
                float a = i * 1.3f;
                stones[i] = L2Core.Child(go, "Pedra_" + (i + 1), V(Mathf.Cos(a) * 0.25f, 0.15f + (i == 4 ? 0.12f : 0f), Mathf.Sin(a) * 0.2f),
                    new VoxelModel().Add(Vector3.zero, V(0.18f, 0.14f, 0.17f), L2Palette.Rock, V(i * 20, i * 47, 0)));
            }
            L2Core.Box(go, V(0, 0.3f, 0), V(1.2f, 0.6f, 1f), true);
            StonePile pile = go.AddComponent<StonePile>();
            pile.stones = 5;
            pile.stoneVisuals = stones;
            pile.prompt = "Pegar pedra";
            pile.firstTimeTip = "Pedras podem ser arremessadas com [G]: atordoam os esqueletos e fazem barulho para distraí-los.";
            L2Core.Save(go, C, "Pilha de pedras");
        }

        /// <summary>
        /// Caixote frágil, barril explosivo e urna funerária (quebráveis).
        /// </summary>
        private static void BuildDestructibles()
        {
            GameObject heart = L2Core.L1("Coracao"), coin = L2Core.L1("Moeda_de_Ouro");
            GameObject ruby = L2Core.L1("Joia_Vermelha"), sapphire = L2Core.L1("Joia_Azul"), emerald = L2Core.L1("Joia_Verde");
            AudioClip crateBreak = L2Core.Clip("crate_break");

            // Caixote frágil: pode soltar um coração ou tesouro.
            GameObject go = new GameObject("Caixote_Fragil");
            go.tag = CombatTags.Destructible;
            VoxelModel crate = new VoxelModel().Add(V(0, 0.45f, 0), V(0.9f, 0.9f, 0.9f), L2Palette.Wood);
            foreach (float x in new[] { -0.41f, 0.41f })
                foreach (float z in new[] { -0.41f, 0.41f })
                    crate.Add(V(x, 0.45f, z), V(0.1f, 0.92f, 0.1f), L2Palette.WoodDark);
            crate.Add(V(0, 0.45f, 0.455f), V(0.9f, 0.1f, 0.02f), L2Palette.WoodDark, V(0, 0, 45));
            GameObject intact = L2Core.Child(go, "Inteiro", Vector3.zero, crate);
            L2Core.Box(go, V(0, 0.45f, 0), V(0.9f, 0.9f, 0.9f));
            L2Core.Obstacle(go, V(0, 0.45f, 0), V(0.9f, 0.9f, 0.9f));
            Destructible d = go.AddComponent<Destructible>();
            d.intactVisual = intact;
            d.maxHealth = 1f;
            d.debrisPrefabs = new[] { L2Core.Get("Lasca_de_Madeira") };
            d.debrisCount = 6;
            d.breakEffects = new[] { L2Core.Get("FX2_Lascas_de_Madeira"), L2Core.Get("FX2_Poeira_de_Impacto") };
            d.breakClip = crateBreak;
            d.lootChance = 0.45f;
            d.lootPrefabs = new[] { heart, heart, coin, ruby };
            L2Core.Save(go, AssetCategoryType.Props, "Caixote frágil (quebrável)");

            // Barril explosivo: explode em área (fere esqueletos e empurra objetos), pode detonar outros barris.
            go = new GameObject("Barril_Explosivo");
            go.tag = CombatTags.Destructible;
            VoxelModel barrel = new VoxelModel()
                .Add(V(0, 0.5f, 0), V(0.8f, 1f, 0.6f), L2Palette.BarrelRed)
                .Add(V(0, 0.5f, 0), V(0.6f, 1f, 0.8f), L2Palette.BarrelRed)
                .Add(V(0, 0.2f, 0), V(0.84f, 0.08f, 0.84f), L2Palette.IronDark)
                .Add(V(0, 0.8f, 0), V(0.84f, 0.08f, 0.84f), L2Palette.IronDark)
                .Add(V(0, 0.52f, 0.41f), V(0.22f, 0.22f, 0.01f), L2Palette.Bone)
                .Add(V(-0.04f, 0.55f, 0.415f), V(0.05f, 0.05f, 0.01f), L2Palette.Void)
                .Add(V(0.04f, 0.55f, 0.415f), V(0.05f, 0.05f, 0.01f), L2Palette.Void)
                .Add(V(0.15f, 1.02f, 0.1f), V(0.06f, 0.08f, 0.06f), L2Palette.Rope);
            intact = L2Core.Child(go, "Inteiro", Vector3.zero, barrel);
            L2Core.Box(go, V(0, 0.5f, 0), V(0.8f, 1f, 0.8f));
            L2Core.Obstacle(go, V(0, 0.5f, 0), V(0.8f, 1f, 0.8f));
            d = go.AddComponent<Destructible>();
            d.intactVisual = intact;
            d.maxHealth = 1f;
            d.debrisPrefabs = new[] { L2Core.Get("Aduela_de_Barril") };
            d.debrisCount = 7;
            d.debrisForce = 7f;
            d.breakEffects = new[] { L2Core.Get("FX2_Lascas_de_Madeira") };
            d.breakClip = crateBreak;
            d.lootChance = 0f;
            d.explosive = true;
            d.explosionRadius = 4f;
            d.explosionDamage = 7f;
            d.explosionPlayerDamage = 1;
            d.explosionEffects = new[] { L2Core.Get("FX2_Explosao_do_Barril"), L2Core.L1("FX_Explosion"), L2Core.L1("FX_Pebbles") };
            d.explosionClip = L2Core.Clip("barrel_explosion");
            L2Core.Save(go, AssetCategoryType.Armadilhas, "Barril explosivo");

            // Urna funerária: costuma guardar moedas e joias.
            go = new GameObject("Urna_Funeraria");
            go.tag = CombatTags.Destructible;
            intact = L2Core.Child(go, "Inteiro", Vector3.zero, new VoxelModel()
                .Add(V(0, 0.08f, 0), V(0.4f, 0.16f, 0.4f), L2Palette.BoneDark)
                .Add(V(0, 0.4f, 0), V(0.6f, 0.5f, 0.6f), L2Palette.BoneDark)
                .Add(V(0, 0.75f, 0), V(0.35f, 0.2f, 0.35f), L2Palette.BoneDark)
                .Add(V(0, 0.42f, 0), V(0.62f, 0.06f, 0.62f), L2Palette.Gold)
                .Add(V(0, 0.88f, 0), V(0.4f, 0.06f, 0.4f), L2Palette.StoneDark));
            L2Core.Box(go, V(0, 0.45f, 0), V(0.6f, 0.9f, 0.6f));
            d = go.AddComponent<Destructible>();
            d.intactVisual = intact;
            d.maxHealth = 1f;
            d.debrisPrefabs = new[] { L2Core.Get("Caco_de_Urna") };
            d.debrisCount = 6;
            d.breakEffects = new[] { L2Core.Get("FX2_Poeira_de_Impacto") };
            d.breakClip = L2Core.Clip("urn_break");
            d.lootChance = 0.7f;
            d.lootPrefabs = new[] { coin, coin, sapphire, emerald, heart };
            L2Core.Save(go, AssetCategoryType.Props, "Urna funerária (quebrável)");
        }
    }
}
