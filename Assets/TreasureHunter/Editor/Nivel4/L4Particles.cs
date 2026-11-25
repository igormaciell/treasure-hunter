using UnityEditor;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Partículas do Nível 4: poeira no ar, fumaça, pedrinhas, poeira dos passos dos ogros, impacto da clava,
    /// bafo dos ogros, alerta (!), suspeita (?), fogueira, brasas, esporos, gotas, névoa rasteira, fumaça ao
    /// apagar uma chama, faíscas ao acender, pedrinhas caindo do teto, cascalho chutado e folhas do esconderijo.
    /// </summary>
    public static class L4Particles
    {
        private static Material alpha, add;

        /// <summary>
        /// Cria todos os efeitos.
        /// </summary>
        public static void BuildAll()
        {
            alpha = L2Core.Particle("mat4_particula_alfa", false);
            add = L2Core.Particle("mat4_particula_aditiva", true);

            DustMotes();
            Smoke();
            Pebbles();
            StepDust();
            ClubImpact();
            Breath();
            AlertBurst();
            SuspicionMotes();
            Campfire();
            Embers();
            Spores();
            Drips();
            GroundFog();
            ExtinguishSmoke();
            IgniteSparks();
            CeilingPebbles();
            GravelKick();
            LeafRustle();
        }

        // ================================================================== Auxiliares

        /// <summary>Objeto com ParticleSystem parado e etiqueta de categoria.</summary>
        /// <param name="id">Nome do prefab.</param>
        /// <param name="display">Nome legível.</param>
        /// <param name="mat">Material.</param>
        /// <param name="go">Saída: objeto.</param>
        /// <returns>O sistema.</returns>
        private static ParticleSystem Begin(string id, string display, Material mat, out GameObject go)
        {
            go = new GameObject(id);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            L2Core.Tag(go, AssetCategoryType.Efeitos, display);
            return ps;
        }

        /// <summary>Sistema filho.</summary>
        /// <param name="parent">Pai.</param>
        /// <param name="name">Nome.</param>
        /// <param name="mat">Material.</param>
        /// <returns>O sistema.</returns>
        private static ParticleSystem Sub(GameObject parent, string name, Material mat)
        {
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent.transform, false);
            ParticleSystem ps = g.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            g.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            return ps;
        }

        /// <summary>Salva o prefab.</summary>
        /// <param name="go">Objeto.</param>
        private static void Save(GameObject go)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, L2Core.PrefabsPath + "/" + go.name + ".prefab");
            Object.DestroyImmediate(go);
            L2Core.Prefabs[prefab.name] = prefab;
        }

        /// <summary>Disparo único.</summary>
        /// <param name="ps">Sistema.</param>
        /// <param name="count">Partículas.</param>
        /// <param name="duration">Duração.</param>
        /// <param name="destroy">Destruir ao terminar.</param>
        private static void OneShot(ParticleSystem ps, short count, float duration, bool destroy = true)
        {
            var main = ps.main;
            main.loop = false;
            main.duration = duration;
            main.playOnAwake = true;
            if (destroy) main.stopAction = ParticleSystemStopAction.Destroy;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        }

        /// <summary>Efeito contínuo.</summary>
        /// <param name="ps">Sistema.</param>
        /// <param name="duration">Ciclo.</param>
        /// <param name="rate">Partículas por segundo.</param>
        private static void Looping(ParticleSystem ps, float duration, float rate)
        {
            var main = ps.main;
            main.loop = true;
            main.duration = duration;
            main.playOnAwake = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = rate;
        }

        /// <summary>Gradiente de cor/transparência.</summary>
        /// <param name="c0">Cor inicial.</param>
        /// <param name="c1">Cor final.</param>
        /// <param name="a0">Alfa inicial.</param>
        /// <param name="aMid">Alfa no meio.</param>
        /// <returns>O gradiente.</returns>
        private static Gradient Grad(Color c0, Color c1, float a0, float aMid)
        {
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c0, 0f), new GradientColorKey(c1, 1f) },
                      new[] { new GradientAlphaKey(a0, 0f), new GradientAlphaKey(aMid, 0.3f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        /// <summary>Curva de tamanho.</summary>
        /// <param name="start">Inicial.</param>
        /// <param name="end">Final.</param>
        /// <returns>A curva.</returns>
        private static ParticleSystem.MinMaxCurve SizeCurve(float start, float end)
        {
            return new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, start, 1f, end));
        }

        /// <summary>Partículas sólidas (cubinhos) com gravidade e colisão.</summary>
        /// <param name="ps">Sistema.</param>
        /// <param name="go">Objeto.</param>
        /// <param name="minSize">Tamanho mínimo.</param>
        /// <param name="maxSize">Tamanho máximo.</param>
        private static void Solid(ParticleSystem ps, GameObject go, float minSize, float maxSize)
        {
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = ParticleFactory.CubeMesh();
            var main = ps.main;
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startRotation3D = true;
            main.gravityModifier = 1.6f;
            var colm = ps.collision; colm.enabled = true; colm.type = ParticleSystemCollisionType.World;
            colm.bounce = 0.3f; colm.dampen = 0.35f; colm.lifetimeLoss = 0f;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
        }

        /// <summary>Anel rente ao chão (poeira de passos e impactos).</summary>
        /// <param name="ps">Sistema.</param>
        /// <param name="radius">Raio de origem.</param>
        private static void GroundRing(ParticleSystem ps, float radius)
        {
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 88f; sh.radius = radius; sh.rotation = new Vector3(-90f, 0f, 0f);
            var limit = ps.limitVelocityOverLifetime; limit.enabled = true; limit.limit = 0.3f; limit.dampen = 0.25f;
        }

        // ================================================================== Efeitos

        /// <summary>Poeira suspensa no ar (contínua, bem lenta).</summary>
        private static void DustMotes()
        {
            ParticleSystem ps = Begin("FX4_Poeira_no_Ar", "Poeira suspensa", alpha, out GameObject go);
            Looping(ps, 8f, 5f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.68f, 0.55f, 0.5f));
            main.maxParticles = 60;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(8f, 3f, 8f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.15f; noise.frequency = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0f, 1f));
            Save(go);
        }

        /// <summary>Coluna de fumaça (fogueira, frestas).</summary>
        private static void Smoke()
        {
            ParticleSystem ps = Begin("FX4_Fumaca", "Fumaça", alpha, out GameObject go);
            Looping(ps, 6f, 4f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.22f, 0.21f, 0.2f, 0.45f));
            main.gravityModifier = -0.02f;
            main.maxParticles = 50;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = 0.4f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.3f, 0.8f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.6f, 3f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.4f; noise.frequency = 0.3f;
            Save(go);
        }

        /// <summary>Pedrinhas que pulam do chão.</summary>
        private static void Pebbles()
        {
            ParticleSystem ps = Begin("FX4_Pedrinhas", "Pedrinhas", L4Palette.Rock, out GameObject go);
            OneShot(ps, 12, 1.5f);
            Solid(ps, go, 0.04f, 0.11f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 50f; sh.radius = 0.3f; sh.rotation = new Vector3(-90f, 0f, 0f);
            Save(go);
        }

        /// <summary>Poeira do passo pesado do ogro.</summary>
        private static void StepDust()
        {
            ParticleSystem ps = Begin("FX4_Poeira_do_Passo", "Poeira do passo do ogro", alpha, out GameObject go);
            OneShot(ps, 10, 1.2f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.3f, 0.25f, 0.6f));
            GroundRing(ps, 0.3f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.8f, 0.5f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.7f, 2f);
            Save(go);
        }

        /// <summary>Impacto da clava: anel de poeira, pedrinhas e clarão de faíscas.</summary>
        private static void ClubImpact()
        {
            ParticleSystem ps = Begin("FX4_Impacto_da_Clava", "Impacto da clava", alpha, out GameObject go);
            OneShot(ps, 26, 2f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.38f, 0.33f, 0.27f, 0.7f));
            GroundRing(ps, 0.6f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.9f, 0.5f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.8f, 2.6f);
            ParticleSystem rocks = Sub(go, "Pedras", L4Palette.RockDark);
            OneShot(rocks, 16, 1.8f, false);
            Solid(rocks, rocks.gameObject, 0.06f, 0.16f);
            var rm = rocks.main; rm.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.8f); rm.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
            var rs = rocks.shape; rs.shapeType = ParticleSystemShapeType.Cone; rs.angle = 45f; rs.radius = 0.4f; rs.rotation = new Vector3(-90f, 0f, 0f);
            ParticleSystem sparks = Sub(go, "Faiscas", add);
            OneShot(sparks, 14, 0.8f, false);
            var sm = sparks.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f); sm.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f); sm.gravityModifier = 1f;
            sm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.35f));
            var ss = sparks.shape; ss.shapeType = ParticleSystemShapeType.Hemisphere; ss.radius = 0.2f;
            Save(go);
        }

        /// <summary>Bafo do ogro no ar frio (nuvenzinha que some).</summary>
        private static void Breath()
        {
            ParticleSystem ps = Begin("FX4_Bafo", "Bafo do ogro", alpha, out GameObject go);
            OneShot(ps, 6, 1.6f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.78f, 0.8f, 0.35f));
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 20f; sh.radius = 0.05f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.6f, 0.4f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.6f, 2.4f);
            Save(go);
        }

        /// <summary>Estouro vermelho do alerta (ogro avistou John).</summary>
        private static void AlertBurst()
        {
            ParticleSystem ps = Begin("FX4_Alerta", "Efeito de alerta (perseguição)", add, out GameObject go);
            OneShot(ps, 24, 1f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.2f, 0.1f), new Color(1f, 0.55f, 0.2f));
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 1f, 0.8f));
            ParticleSystem ring = Sub(go, "Anel", add);
            OneShot(ring, 1, 0.5f, false);
            var rm = ring.main; rm.startLifetime = 0.45f; rm.startSpeed = 0f; rm.startSize = 0.6f;
            rm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.25f, 0.15f, 0.8f));
            var rs = ring.shape; rs.shapeType = ParticleSystemShapeType.Sphere; rs.radius = 0.01f;
            var rsz = ring.sizeOverLifetime; rsz.enabled = true; rsz.size = SizeCurve(0.5f, 4f);
            var rc = ring.colorOverLifetime; rc.enabled = true; rc.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 1f, 0.5f));
            Save(go);
        }

        /// <summary>Pontinhos amarelos que giram (ogro desconfiado).</summary>
        private static void SuspicionMotes()
        {
            ParticleSystem ps = Begin("FX4_Suspeita", "Efeito de suspeita (investigação)", add, out GameObject go);
            OneShot(ps, 12, 1.4f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.25f));
            main.gravityModifier = -0.15f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.35f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.orbitalY = new ParticleSystem.MinMaxCurve(3f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 1f, 0.9f));
            Save(go);
        }

        /// <summary>Chamas da fogueira dos ogros.</summary>
        private static void Campfire()
        {
            ParticleSystem ps = Begin("FX4_Fogueira", "Chamas da fogueira", add, out GameObject go);
            Looping(ps, 2f, 26f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.15f), new Color(1f, 0.8f, 0.3f));
            main.maxParticles = 60;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 10f; sh.radius = 0.5f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Grad(new Color(1f, 0.9f, 0.5f), new Color(0.8f, 0.2f, 0.05f), 0.9f, 0.8f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(1f, 0.2f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.4f; noise.frequency = 1.2f;
            Save(go);
        }

        /// <summary>Brasas subindo (fogueira, braseiros).</summary>
        private static void Embers()
        {
            ParticleSystem ps = Begin("FX4_Brasas", "Brasas", add, out GameObject go);
            Looping(ps, 3f, 6f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0.1f), new Color(1f, 0.75f, 0.3f));
            main.gravityModifier = -0.05f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 20f; sh.radius = 0.4f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.5f; noise.frequency = 0.8f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 1f, 0.9f));
            Save(go);
        }

        /// <summary>Esporos brilhantes dos fungos.</summary>
        private static void Spores()
        {
            ParticleSystem ps = Begin("FX4_Esporos", "Esporos de fungo", add, out GameObject go);
            Looping(ps, 6f, 3f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.3f, 1f, 0.85f), new Color(1f, 0.7f, 0.3f));
            main.gravityModifier = -0.01f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1.2f;
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.2f; noise.frequency = 0.4f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0f, 0.8f));
            Save(go);
        }

        /// <summary>Gotas caindo do teto.</summary>
        private static void Drips()
        {
            ParticleSystem ps = Begin("FX4_Gotas", "Gotas do teto", add, out GameObject go);
            Looping(ps, 4f, 1.2f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.6f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.7f, 0.8f, 0.8f));
            main.gravityModifier = 1f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(4f, 0.1f, 4f);
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.05f;
            r.lengthScale = 1f;
            Save(go);
        }

        /// <summary>Névoa rasteira nas áreas de sombra.</summary>
        private static void GroundFog()
        {
            ParticleSystem ps = Begin("FX4_Nevoa_Rasteira", "Névoa rasteira", alpha, out GameObject go);
            Looping(ps, 10f, 2.5f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.25f, 0.27f, 0.3f, 0.22f));
            main.maxParticles = 40;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(10f, 0.2f, 10f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0f, 1f));
            Save(go);
        }

        /// <summary>Fumaça ao apagar uma chama.</summary>
        private static void ExtinguishSmoke()
        {
            ParticleSystem ps = Begin("FX4_Fumaca_Apagada", "Fumaça da chama apagada", alpha, out GameObject go);
            OneShot(ps, 12, 2.5f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 0.4f, 0.42f, 0.5f));
            main.gravityModifier = -0.08f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 15f; sh.radius = 0.1f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.7f, 0.6f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.6f, 2.5f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.3f; noise.frequency = 0.8f;
            Save(go);
        }

        /// <summary>Faíscas ao acender a tocha.</summary>
        private static void IgniteSparks()
        {
            ParticleSystem ps = Begin("FX4_Faiscas", "Faíscas ao acender", add, out GameObject go);
            OneShot(ps, 18, 0.8f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.7f, 0.3f));
            main.gravityModifier = 0.6f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.08f;
            Save(go);
        }

        /// <summary>Pedrinhas e poeira caindo do teto (contínuo e raro; perto dos passos dos ogros).</summary>
        private static void CeilingPebbles()
        {
            ParticleSystem ps = Begin("FX4_Pedrinhas_do_Teto", "Pedrinhas caindo do teto", L4Palette.RockDark, out GameObject go);
            Looping(ps, 6f, 0.6f);
            Solid(ps, go, 0.03f, 0.08f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f);
            main.startSpeed = 0f;
            main.gravityModifier = 1.2f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(5f, 0.1f, 5f);
            ParticleSystem dust = Sub(go, "Poeira", alpha);
            Looping(dust, 6f, 0.8f);
            var dm = dust.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f); dm.startSpeed = 0f;
            dm.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f); dm.gravityModifier = 0.15f;
            dm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 0.36f, 0.3f, 0.35f));
            var ds = dust.shape; ds.shapeType = ParticleSystemShapeType.Box; ds.scale = new Vector3(5f, 0.1f, 5f);
            var dc = dust.colorOverLifetime; dc.enabled = true; dc.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.6f, 0.6f));
            Save(go);
        }

        /// <summary>Cascalho chutado (passos em chão barulhento).</summary>
        private static void GravelKick()
        {
            ParticleSystem ps = Begin("FX4_Cascalho", "Cascalho chutado", L4Palette.Gravel, out GameObject go);
            OneShot(ps, 6, 1f);
            Solid(ps, go, 0.03f, 0.06f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 40f; sh.radius = 0.15f; sh.rotation = new Vector3(-90f, 0f, 0f);
            Save(go);
        }

        /// <summary>Folhas e esporos que se soltam quando John entra no esconderijo.</summary>
        private static void LeafRustle()
        {
            ParticleSystem ps = Begin("FX4_Folhas", "Folhas do esconderijo", alpha, out GameObject go);
            OneShot(ps, 14, 2f, false);
            var main = ps.main;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.25f, 0.45f, 0.3f, 0.9f), new Color(0.3f, 0.8f, 0.7f, 0.9f));
            main.gravityModifier = 0.1f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.8f;
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.4f; noise.frequency = 0.8f;
            Save(go);
        }
    }
}
