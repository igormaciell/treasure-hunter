using UnityEditor;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Segunda parte da fábrica de prefabs: iluminação, armadilhas e mecanismos interativos.
    /// </summary>
    public static partial class PropFactory
    {
        // ================================================================== Iluminação

        /// <summary>
        /// Tocha de parede, braseiro, velas, lanterna, fogueira e feixe de luz.
        /// </summary>
        private static void BuildLighting()
        {
            const AssetCategoryType C = AssetCategoryType.Iluminacao;
            Color fire = new Color(1f, 0.62f, 0.28f);

            // Tocha de parede (frente = +Z, presa na parede).
            GameObject go = Begin("Tocha_de_Parede", new VoxelModel()
                .Add(V(0, 0, 0.05f), V(0.2f, 0.3f, 0.1f), Palette.Iron)
                .Add(V(0, 0.2f, 0.22f), V(0.08f, 0.6f, 0.08f), Palette.WoodDark, V(25, 0, 0))
                .Add(V(0, 0.48f, 0.35f), V(0.14f, 0.12f, 0.14f), Palette.FireGlow));
            AddFx(go, "FX_Fire", V(0, 0.55f, 0.36f)).transform.localScale = Vector3.one * 0.8f;
            AddLight(go, "Luz", V(0, 0.8f, 0.6f), fire, 7f, 1.8f, true);
            Save(go, C, "Tocha de parede");

            // Braseiro interativo (aceso pelo jogador).
            VoxelModel brazier = new VoxelModel()
                .Add(V(0, 0.08f, 0), V(0.8f, 0.16f, 0.8f), Palette.StoneDark)
                .Add(V(-0.25f, 0.45f, -0.25f), V(0.08f, 0.6f, 0.08f), Palette.Iron)
                .Add(V(0.25f, 0.45f, -0.25f), V(0.08f, 0.6f, 0.08f), Palette.Iron)
                .Add(V(-0.25f, 0.45f, 0.25f), V(0.08f, 0.6f, 0.08f), Palette.Iron)
                .Add(V(0.25f, 0.45f, 0.25f), V(0.08f, 0.6f, 0.08f), Palette.Iron)
                .Add(V(0, 0.82f, 0), V(0.95f, 0.3f, 0.95f), Palette.Iron)
                .Add(V(0, 0.98f, 0), V(0.75f, 0.05f, 0.75f), Palette.Coals);
            go = Begin("Braseiro", brazier);
            Fit(go, brazier);
            ParticleSystem fireFx = AddFx(go, "FX_Fire", V(0, 1.05f, 0));
            fireFx.transform.localScale = Vector3.one * 1.8f;
            ParticleSystem smoke = AddFx(go, "FX_Smoke", V(0, 1.5f, 0));
            Light bl = AddLight(go, "Luz", V(0, 1.6f, 0), fire, 10f, 3f);
            Brazier b = go.AddComponent<Brazier>();
            b.fire = fireFx; b.smoke = smoke; b.fireLight = bl;
            b.prompt = "Acender braseiro com a tocha";
            b.firstTimeTip = "Braseiros podem ser acesos com a tocha [E]. A luz revela o que estava escondido.";
            Save(go, C, "Braseiro");

            // Braseiro decorativo já aceso.
            go = Begin("Braseiro_Aceso", brazier);
            Fit(go, brazier);
            AddFx(go, "FX_Fire", V(0, 1.05f, 0)).transform.localScale = Vector3.one * 1.8f;
            AddFx(go, "FX_Smoke", V(0, 1.5f, 0));
            AddLight(go, "Luz", V(0, 1.6f, 0), fire, 10f, 3f, true);
            Save(go, C, "Braseiro aceso");

            go = Begin("Velas", new VoxelModel()
                .Add(V(0, 0.12f, 0), V(0.08f, 0.24f, 0.08f), Palette.Paper)
                .Add(V(0.12f, 0.08f, 0.05f), V(0.07f, 0.16f, 0.07f), Palette.Paper)
                .Add(V(-0.1f, 0.1f, 0.08f), V(0.07f, 0.2f, 0.07f), Palette.Paper)
                .Add(V(0, 0.27f, 0), V(0.04f, 0.06f, 0.04f), Palette.CandleFlame)
                .Add(V(0.12f, 0.19f, 0.05f), V(0.04f, 0.06f, 0.04f), Palette.CandleFlame)
                .Add(V(-0.1f, 0.23f, 0.08f), V(0.04f, 0.06f, 0.04f), Palette.CandleFlame));
            AddLight(go, "Luz", V(0, 0.5f, 0), new Color(1f, 0.8f, 0.5f), 3f, 0.9f, true);
            Save(go, C, "Velas");

            go = Begin("Lanterna_Pendurada", new VoxelModel()
                .Add(V(0, -0.6f, 0), V(0.04f, 1.2f, 0.04f), Palette.Iron)
                .Add(V(0, -1.35f, 0), V(0.3f, 0.36f, 0.3f), Palette.Iron)
                .Add(V(0, -1.35f, 0), V(0.22f, 0.3f, 0.32f), Palette.CandleFlame)
                .Add(V(0, -1.35f, 0), V(0.32f, 0.3f, 0.22f), Palette.CandleFlame));
            AddLight(go, "Luz", V(0, -1.4f, 0), new Color(1f, 0.75f, 0.45f), 6f, 1.4f, true);
            Save(go, C, "Lanterna pendurada");

            go = Begin("Fogueira", new VoxelModel()
                .Add(V(0, 0.1f, 0), V(0.15f, 0.15f, 0.9f), Palette.Bark, V(0, 45, 0))
                .Add(V(0, 0.1f, 0), V(0.15f, 0.15f, 0.9f), Palette.Bark, V(0, -45, 0))
                .Add(V(0, 0.06f, 0), V(0.5f, 0.06f, 0.5f), Palette.Coals)
                .Add(V(0.55f, 0.1f, 0), V(0.2f, 0.2f, 0.25f), Palette.Rock)
                .Add(V(-0.55f, 0.1f, 0), V(0.2f, 0.2f, 0.25f), Palette.Rock)
                .Add(V(0, 0.1f, 0.55f), V(0.25f, 0.2f, 0.2f), Palette.Rock)
                .Add(V(0, 0.1f, -0.55f), V(0.25f, 0.2f, 0.2f), Palette.Rock));
            AddFx(go, "FX_Fire", V(0, 0.15f, 0)).transform.localScale = Vector3.one * 1.6f;
            AddFx(go, "FX_Smoke", V(0, 0.8f, 0));
            AddLight(go, "Luz", V(0, 0.8f, 0), fire, 8f, 2.2f, true);
            Save(go, C, "Fogueira do acampamento");

            // Feixe de luz vindo de uma fresta no teto (luz spot + poeira flutuando).
            go = new GameObject("Feixe_de_Luz");
            GameObject spotGo = new GameObject("Spot");
            spotGo.transform.SetParent(go.transform, false);
            spotGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Light spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1f, 0.93f, 0.75f);
            spot.range = 9f;
            spot.spotAngle = 40f;
            spot.intensity = 3.5f;
            spot.shadows = LightShadows.Soft;
            AddFx(go, "FX_DustMotes", V(0, -2.5f, 0));
            Save(go, C, "Feixe de luz");
        }

        // ================================================================== Armadilhas

        /// <summary>
        /// Pedras que caem (vetor de prefabs: pequena, média e grande), tronco giratório e blocos do piso que desabam.
        /// </summary>
        private static void BuildTraps()
        {
            const AssetCategoryType C = AssetCategoryType.Armadilhas;
            Rock("Pedra_Pequena", 0.45f, 20f, "Pedra que cai (pequena)");
            Rock("Pedra_Media", 0.7f, 45f, "Pedra que cai (média)");
            Rock("Pedra_Grande", 0.95f, 80f, "Pedra que cai (grande)");

            // Tronco giratório com espinhos (torque).
            VoxelModel log = new VoxelModel()
                .Add(V(0, 0.65f, 0), V(0.9f, 1.3f, 0.9f), Palette.StoneDark)
                .Add(V(0, 0.5f, 0), V(6.6f, 0.45f, 0.45f), Palette.Bark);
            for (int i = -6; i <= 6; i++)
            {
                if (Mathf.Abs(i) < 2) continue;
                float x = i * 0.5f;
                log.Add(V(x, 0.5f, 0.3f), V(0.08f, 0.08f, 0.25f), Palette.Iron)
                   .Add(V(x, 0.5f, -0.3f), V(0.08f, 0.08f, 0.25f), Palette.Iron)
                   .Add(V(x, 0.78f, 0), V(0.08f, 0.2f, 0.08f), Palette.Iron);
            }
            GameObject go = Begin("Tronco_Giratorio", log);
            Box(go, V(0, 0.5f, 0), V(6.6f, 0.45f, 0.45f));
            Box(go, V(0, 0.65f, 0), V(0.9f, 1.3f, 0.9f));
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = 800f;
            rb.useGravity = false;
            rb.angularDamping = 0.05f;
            rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            GameObject spikes = new GameObject("Espinhos");
            spikes.transform.SetParent(go.transform, false);
            spikes.tag = GameTags.Hazard;
            Box(spikes, V(-2.1f, 0.55f, 0), V(2.4f, 0.75f, 0.9f), true);
            Box(spikes, V(2.1f, 0.55f, 0), V(2.4f, 0.75f, 0.9f), true);
            spikes.AddComponent<Hazard>();
            AudioSource whoosh = go.AddComponent<AudioSource>();
            whoosh.clip = BuilderCore.Clip("log_whoosh");
            whoosh.loop = true; whoosh.playOnAwake = true; whoosh.spatialBlend = 1f; whoosh.volume = 0.5f;
            whoosh.rolloffMode = AudioRolloffMode.Linear; whoosh.minDistance = 2f; whoosh.maxDistance = 16f;
            SpinningLogTrap trap = go.AddComponent<SpinningLogTrap>();
            trap.whooshSource = whoosh;
            Save(go, C, "Tronco giratório com espinhos");

            // Bloco do piso do templo que desaba na armadilha da introdução.
            VoxelModel tile = new VoxelModel()
                .Add(V(0, -0.25f, 0), V(2f, 0.5f, 2f), Palette.StoneFloor)
                .Add(V(0.3f, -0.001f, 0.2f), V(0.8f, 0.01f, 0.05f), Palette.StoneDark);
            go = Begin("Bloco_do_Piso", tile);
            go.tag = GameTags.Ground;
            Fit(go, tile);
            Rigidbody trb = go.AddComponent<Rigidbody>();
            trb.isKinematic = true;
            trb.mass = 200f;
            Save(go, C, "Bloco do piso que desaba");
        }

        /// <summary>
        /// Cria um prefab de pedra que cai (Rigidbody, Tag "Rock" e FallingRock).
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <param name="size">Tamanho aproximado.</param>
        /// <param name="mass">Massa.</param>
        /// <param name="display">Nome legível.</param>
        private static void Rock(string id, float size, float mass, string display)
        {
            float s = size;
            VoxelModel m = new VoxelModel()
                .Add(V(0, 0, 0), V(s, s * 0.85f, s * 0.9f), Palette.Rock)
                .Add(V(s * 0.25f, s * 0.2f, 0.1f * s), V(s * 0.6f, s * 0.6f, s * 0.6f), Palette.Rock, V(20, 30, 10))
                .Add(V(-s * 0.2f, -s * 0.15f, -0.15f * s), V(s * 0.55f, s * 0.5f, s * 0.6f), Palette.StoneDark, V(-15, 10, 25));
            GameObject go = Begin(id, m);
            go.tag = GameTags.Rock;
            Box(go, Vector3.zero, Vector3.one * s * 0.9f);
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            FallingRock fr = go.AddComponent<FallingRock>();
            fr.explosionPrefab = ParticleFactory.Prefabs["FX_Explosion"];
            fr.pebblesPrefab = ParticleFactory.Prefabs["FX_Pebbles"];
            Save(go, AssetCategoryType.Armadilhas, display);
        }

        // ================================================================== Mecanismos

        /// <summary>
        /// Alavanca, placa de pressão, porta de pedra, botão de símbolo, pedestais, caixote empurrável,
        /// plataforma com alçapão, tábua de habilidade e moldura de símbolo.
        /// </summary>
        private static void BuildMechanisms()
        {
            const AssetCategoryType C = AssetCategoryType.Mecanismos;

            // Alavanca: base fixa + cabo (filho) que gira no eixo X.
            VoxelModel leverBase = new VoxelModel()
                .Add(V(0, 0.15f, 0), V(0.6f, 0.3f, 0.45f), Palette.StoneDark)
                .Add(V(0, 0.32f, 0), V(0.3f, 0.06f, 0.3f), Palette.Iron);
            GameObject go = Begin("Alavanca", leverBase);
            Box(go, V(0, 0.5f, 0), V(0.7f, 1f, 0.6f));
            GameObject handle = Child(go, "Cabo", V(0, 0.32f, 0), new VoxelModel()
                .Add(V(0, 0.4f, 0), V(0.08f, 0.8f, 0.08f), Palette.Wood)
                .Add(V(0, 0.82f, 0), V(0.16f, 0.16f, 0.16f), Palette.ClothRed));
            Lever lever = go.AddComponent<Lever>();
            lever.handle = handle.transform;
            lever.prompt = "Puxar alavanca";
            lever.firstTimeTip = "Alavancas acionam mecanismos antigos. Aproxime-se e pressione [E].";
            lever.highlightLight = AddLight(go, "Destaque", V(0, 1.2f, 0.3f), new Color(1f, 0.85f, 0.5f), 2.2f, 0.9f);
            Save(go, C, "Alavanca");

            // Placa de pressão: moldura + placa móvel + gatilho.
            go = Begin("Placa_de_Pressao", new VoxelModel()
                .Add(V(0, 0.04f, 0), V(1.9f, 0.08f, 1.9f), Palette.StoneDark));
            GameObject plate = Child(go, "Placa", V(0, 0.08f, 0), new VoxelModel()
                .Add(V(0, 0.04f, 0), V(1.45f, 0.1f, 1.45f), Palette.TempleBrick)
                .Add(V(0, 0.095f, 0), V(0.5f, 0.01f, 0.5f), Palette.Gold));
            Box(go, V(0, 0.6f, 0), V(1.3f, 1f, 1.3f), true);
            PressurePlate pp = go.AddComponent<PressurePlate>();
            pp.plateVisual = plate.transform;
            Light ind = AddLight(go, "Indicador", V(0, 0.6f, 0), new Color(0.4f, 1f, 0.6f), 3f, 1.5f);
            pp.indicator = ind;
            Save(go, C, "Placa de pressão");

            // Porta de pedra (sobe ao abrir).
            VoxelModel door = new VoxelModel()
                .Add(V(0, 1.75f, 0), V(3f, 3.5f, 0.6f), Palette.TempleBrick)
                .Add(V(0, 1.75f, 0.31f), V(2.4f, 2.9f, 0.04f), Palette.StoneDark)
                .Add(V(0, 2.2f, 0.34f), V(0.8f, 0.8f, 0.04f), Palette.Gold)
                .Add(V(0, 1.75f, -0.31f), V(2.4f, 2.9f, 0.04f), Palette.StoneDark);
            go = Begin("Porta_de_Pedra", door);
            Fit(go, door);
            StoneDoor sd = go.AddComponent<StoneDoor>();
            sd.dust = AddFx(go, "FX_DoorDust", V(0, 0.1f, 0));
            Save(go, C, "Porta de pedra");

            // Botão de símbolo (face com o símbolo voltada para +Z).
            VoxelModel buttonBase = new VoxelModel()
                .Add(V(0, 0.5f, 0), V(0.75f, 1f, 0.55f), Palette.StoneDark)
                .Add(V(0, 1.03f, 0), V(0.85f, 0.06f, 0.65f), Palette.TempleBrick);
            go = Begin("Botao_de_Simbolo", buttonBase);
            Fit(go, buttonBase);
            GameObject press = Child(go, "Face", V(0, 0.62f, 0.3f), new VoxelModel().Add(V(0, 0, 0), V(0.5f, 0.5f, 0.08f), Palette.Glyph[0]));
            SymbolButton btn = go.AddComponent<SymbolButton>();
            btn.face = press.GetComponent<MeshRenderer>();
            btn.pressPart = press.transform;
            btn.prompt = "Pressionar símbolo";
            btn.firstTimeTip = "Pressione os símbolos na ordem certa. Errar apenas reinicia o mecanismo.";
            btn.powerLight = AddLight(go, "Energia", V(0, 0.65f, 0.6f), new Color(0.5f, 0.9f, 1f), 1.6f, 0.8f);
            Save(go, C, "Botão de símbolo");

            VoxelModel pedestal = new VoxelModel()
                .Add(V(0, 0.1f, 0), V(1f, 0.2f, 1f), Palette.TempleBrick)
                .Add(V(0, 0.6f, 0), V(0.7f, 0.8f, 0.7f), Palette.TempleBrick)
                .Add(V(0, 1.05f, 0), V(0.95f, 0.1f, 0.95f), Palette.StoneDark)
                .Add(V(0, 0.6f, 0.36f), V(0.3f, 0.3f, 0.02f), Palette.Gold);
            Simple("Pedestal", pedestal, C, "Pedestal", true);

            // Pedestal do Ídolo (interativo; usado na introdução).
            go = Begin("Pedestal_do_Idolo", pedestal);
            Fit(go, pedestal);
            GameObject idol = Child(go, "Idolo", V(0, 1.1f, 0), Idol());
            AddFx(idol, "FX_Glint", V(0, 0.4f, 0));
            ArtifactPedestal ap = go.AddComponent<ArtifactPedestal>();
            ap.artifactVisual = idol;
            ap.prompt = "Pegar o Ídolo de Añang";
            ap.highlightLight = AddLight(go, "Brilho", V(0, 2f, 0.4f), new Color(1f, 0.8f, 0.35f), 4f, 2f);
            Save(go, C, "Pedestal do Ídolo");

            // Caixote empurrável (Rigidbody + Tag "Pushable").
            VoxelModel crate = Crate(1.2f, Palette.Wood);
            crate.Add(V(0, 0.6f, 0.611f), V(0.5f, 0.5f, 0.02f), Palette.Iron);
            go = Begin("Caixote_Empurravel", crate);
            go.tag = GameTags.Pushable;
            BoxCollider bc = Box(go, V(0, 0.6f, 0), V(1.2f, 1.2f, 1.2f));
            bc.sharedMaterial = CratePhysics;
            Rigidbody crb = go.AddComponent<Rigidbody>();
            crb.mass = 30f;
            crb.linearDamping = 2.5f;
            crb.interpolation = RigidbodyInterpolation.Interpolate;
            crb.constraints = RigidbodyConstraints.FreezeRotation;
            AudioSource scrape = go.AddComponent<AudioSource>();
            scrape.clip = BuilderCore.Clip("crate_scrape");
            scrape.loop = true; scrape.playOnAwake = false; scrape.spatialBlend = 1f; scrape.volume = 0f;
            scrape.rolloffMode = AudioRolloffMode.Linear; scrape.minDistance = 2f; scrape.maxDistance = 15f;
            PushableCrate pc = go.AddComponent<PushableCrate>();
            pc.scrapeSource = scrape;
            Save(go, C, "Caixote empurrável");

            // Plataforma suspensa com alçapão (Sala 4): postes + dobradiça + tábua.
            VoxelModel shelf = new VoxelModel();
            float[] px = { -1.15f, 1.15f };
            foreach (float x in px)
                foreach (float z in px)
                    shelf.Add(V(x, 1.75f, z), V(0.22f, 3.5f, 0.22f), Palette.WoodDark);
            shelf.Add(V(0, 3.45f, -1.15f), V(2.5f, 0.15f, 0.2f), Palette.WoodDark)
                 .Add(V(0, 3.45f, 1.15f), V(2.5f, 0.15f, 0.2f), Palette.WoodDark)
                 .Add(V(0, 3.9f, 0), V(0.1f, 0.8f, 0.1f), Palette.Rope);
            go = Begin("Plataforma_com_Alcapao", shelf);
            foreach (float x in px)
                foreach (float z in px)
                    Box(go, V(x, 1.75f, z), V(0.22f, 3.5f, 0.22f));
            GameObject hinge = new GameObject("Dobradica");
            hinge.transform.SetParent(go.transform, false);
            hinge.transform.localPosition = V(-1.05f, 3.4f, 0);
            GameObject board = Child(hinge, "Tabua", V(1.05f, 0, 0), new VoxelModel().Add(V(0, 0, 0), V(2.1f, 0.15f, 2.1f), Palette.Wood));
            BoxCollider boardCol = Box(board, Vector3.zero, V(2.1f, 0.15f, 2.1f));
            HatchRelease hr = go.AddComponent<HatchRelease>();
            hr.hinge = hinge.transform;
            hr.supportCollider = boardCol;
            hr.openAngle = -95f;
            hr.dust = AddFx(go, "FX_Smoke", V(0, 3.4f, 0));
            hr.dust.Stop();
            var dustMain = hr.dust.main; dustMain.playOnAwake = false; dustMain.loop = false;
            hr.lineOnOpen = "O alçapão cedeu! O caixote caiu lá embaixo.";
            Save(go, C, "Plataforma com alçapão");

            // Tábua de habilidade (inscrição que ensina a habilidade da sala).
            VoxelModel tablet = new VoxelModel()
                .Add(V(0, 0.15f, 0), V(1.2f, 0.3f, 0.5f), Palette.StoneDark)
                .Add(V(0, 1f, 0), V(1f, 1.4f, 0.2f), Palette.TempleBrick, V(-8, 0, 0))
                .Add(V(0, 1.25f, 0.1f), V(0.4f, 0.4f, 0.04f), Palette.CrystalBlue, V(-8, 0, 0));
            for (int i = 0; i < 4; i++) tablet.Add(V(0, 0.65f + i * 0.13f, 0.06f + i * 0.018f), V(0.7f, 0.04f, 0.04f), Palette.StoneDark, V(-8, 0, 0));
            go = Begin("Tabua_de_Habilidade", tablet);
            Box(go, V(0, 0.85f, 0), V(1.2f, 1.7f, 0.5f));
            SkillTablet st = go.AddComponent<SkillTablet>();
            st.prompt = "Ler inscrição";
            st.highlightLight = AddLight(go, "Brilho", V(0, 1.3f, 0.6f), new Color(0.5f, 0.85f, 1f), 3f, 1f);
            Save(go, AssetCategoryType.Inscricoes, "Tábua de habilidade");

            // Moldura de símbolo para a parede (a face é revelada pela tocha).
            VoxelModel frame = new VoxelModel()
                .Add(V(0, 0, -0.05f), V(1f, 1f, 0.1f), Palette.StoneDark);
            go = Begin("Moldura_de_Simbolo", frame);
            Child(go, "Face", V(0, 0, 0.03f), new VoxelModel().Add(V(0, 0, 0), V(0.78f, 0.78f, 0.04f), Palette.GlyphLit[0]));
            Save(go, AssetCategoryType.Inscricoes, "Moldura de símbolo");
        }
    }
}
