using UnityEditor;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Partículas do Nível 2: clarão e fumaça do disparo, faíscas e poeira de impacto, lascas de osso e de madeira,
    /// poeira de osso da morte dos esqueletos, surgimento dos esqueletos, explosão do barril, brasas, gotas d'água,
    /// névoa rasteira, palha do boneco, almas flutuando e poeira dos espinhos.
    /// </summary>
    public static class L2Particles
    {
        private static Material alpha, add;

        /// <summary>
        /// Cria todos os prefabs de partículas do Nível 2.
        /// </summary>
        public static void BuildAll()
        {
            alpha = L2Core.Particle("mat2_particula_alfa", false);
            add = L2Core.Particle("mat2_particula_aditiva", true);

            MuzzleFlash();
            GunSmoke();
            Sparks();
            ImpactDust();
            Chips("FX2_Lascas_de_Osso", "Lascas de osso", L2Palette.Bone, 10, 0.04f, 0.09f);
            Chips("FX2_Lascas_de_Madeira", "Lascas de madeira", L2Palette.Wood, 14, 0.05f, 0.14f);
            BoneDust();
            SpawnDust();
            BarrelExplosion();
            Embers();
            Drips();
            GroundFog();
            Straw();
            Wisps();
            SpikeDust();
        }

        // ================================================================== Auxiliares

        /// <summary>
        /// Cria o objeto com ParticleSystem parado e a etiqueta de categoria.
        /// </summary>
        /// <param name="id">Nome do prefab.</param>
        /// <param name="display">Nome legível.</param>
        /// <param name="mat">Material.</param>
        /// <param name="go">Saída: objeto criado.</param>
        /// <returns>O sistema de partículas.</returns>
        private static ParticleSystem Begin(string id, string display, Material mat, out GameObject go)
        {
            go = new GameObject(id);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            L2Core.Tag(go, AssetCategoryType.Efeitos, display);
            return ps;
        }

        /// <summary>
        /// Cria um sistema filho (camada extra do efeito).
        /// </summary>
        /// <param name="parent">Objeto pai.</param>
        /// <param name="name">Nome.</param>
        /// <param name="mat">Material.</param>
        /// <returns>O sistema filho.</returns>
        private static ParticleSystem Sub(GameObject parent, string name, Material mat)
        {
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent.transform, false);
            ParticleSystem ps = g.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            g.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            return ps;
        }

        /// <summary>
        /// Salva o prefab e registra no dicionário do Nível 2.
        /// </summary>
        /// <param name="go">Objeto configurado.</param>
        private static void Save(GameObject go)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, L2Core.PrefabsPath + "/" + go.name + ".prefab");
            Object.DestroyImmediate(go);
            L2Core.Prefabs[prefab.name] = prefab;
        }

        /// <summary>
        /// Configuração de um disparo único (burst) que se destrói ao acabar.
        /// </summary>
        /// <param name="ps">Sistema.</param>
        /// <param name="count">Quantidade de partículas.</param>
        /// <param name="duration">Duração.</param>
        /// <param name="destroy">true para destruir o objeto ao terminar.</param>
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

        /// <summary>
        /// Gradiente de cor/transparência.
        /// </summary>
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

        /// <summary>
        /// Curva de tamanho ao longo da vida.
        /// </summary>
        /// <param name="start">Multiplicador inicial.</param>
        /// <param name="end">Multiplicador final.</param>
        /// <returns>A curva.</returns>
        private static ParticleSystem.MinMaxCurve SizeCurve(float start, float end)
        {
            return new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, start, 1f, end));
        }

        // ================================================================== Efeitos

        /// <summary>Clarão do disparo (aditivo, rapidíssimo), apontado para a frente do cano.</summary>
        private static void MuzzleFlash()
        {
            ParticleSystem ps = Begin("FX2_Clarao_do_Disparo", "Clarão do disparo", add, out GameObject go);
            OneShot(ps, 12, 0.1f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.5f), new Color(1f, 0.55f, 0.15f));
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 14f; sh.radius = 0.02f;
            ParticleSystem core = Sub(go, "Nucleo", add);
            OneShot(core, 1, 0.08f, false);
            var cm = core.main;
            cm.startLifetime = 0.05f; cm.startSpeed = 0f; cm.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.5f);
            cm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.5f));
            var cs = core.shape; cs.shapeType = ParticleSystemShapeType.Sphere; cs.radius = 0.01f;
            Save(go);
        }

        /// <summary>Fumaça que sai do cano depois do tiro.</summary>
        private static void GunSmoke()
        {
            ParticleSystem ps = Begin("FX2_Fumaca_do_Disparo", "Fumaça do disparo", alpha, out GameObject go);
            OneShot(ps, 7, 1.2f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.73f, 0.7f, 0.5f));
            main.gravityModifier = -0.05f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 20f; sh.radius = 0.03f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.6f, 0.4f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.6f, 2.4f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.3f; noise.frequency = 0.6f;
            Save(go);
        }

        /// <summary>Faíscas do tiro acertando pedra ou metal (esticadas pela velocidade).</summary>
        private static void Sparks()
        {
            ParticleSystem ps = Begin("FX2_Faiscas", "Faíscas de impacto", add, out GameObject go);
            OneShot(ps, 14, 0.4f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.6f), new Color(1f, 0.6f, 0.2f));
            main.gravityModifier = 1.2f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 40f; sh.radius = 0.02f;
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.04f;
            r.lengthScale = 1.5f;
            Save(go);
        }

        /// <summary>Nuvem de poeira no ponto do impacto.</summary>
        private static void ImpactDust()
        {
            ParticleSystem ps = Begin("FX2_Poeira_de_Impacto", "Poeira de impacto", alpha, out GameObject go);
            OneShot(ps, 10, 1f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 0.45f, 0.38f), new Color(0.65f, 0.6f, 0.52f));
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 45f; sh.radius = 0.05f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.8f, 0.5f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.7f, 2f);
            var limit = ps.limitVelocityOverLifetime; limit.enabled = true; limit.limit = 0.4f; limit.dampen = 0.2f;
            Save(go);
        }

        /// <summary>Lascas sólidas (cubinhos) com gravidade e colisão com o cenário.</summary>
        /// <param name="id">Nome do prefab.</param>
        /// <param name="display">Nome legível.</param>
        /// <param name="mat">Material das lascas.</param>
        /// <param name="count">Quantidade.</param>
        /// <param name="minSize">Tamanho mínimo.</param>
        /// <param name="maxSize">Tamanho máximo.</param>
        private static void Chips(string id, string display, Material mat, short count, float minSize, float maxSize)
        {
            ParticleSystem ps = Begin(id, display, mat, out GameObject go);
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = ParticleFactory.CubeMesh();
            OneShot(ps, count, 1f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startRotation3D = true;
            main.gravityModifier = 1.8f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 50f; sh.radius = 0.08f;
            var colm = ps.collision; colm.enabled = true; colm.type = ParticleSystemCollisionType.World;
            colm.bounce = 0.3f; colm.dampen = 0.3f; colm.lifetimeLoss = 0f;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-5f, 5f);
            Save(go);
        }

        /// <summary>Poeira de osso e lascas quando um esqueleto desmorona.</summary>
        private static void BoneDust()
        {
            ParticleSystem ps = Begin("FX2_Poeira_de_Osso", "Poeira de osso (morte)", alpha, out GameObject go);
            OneShot(ps, 28, 1.6f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.72f, 0.68f, 0.58f), new Color(0.55f, 0.5f, 0.42f));
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.5f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.7f, 0.45f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.6f, 1.8f);

            ParticleSystem chips = Sub(go, "Lascas", L2Palette.Bone);
            ParticleSystemRenderer r = chips.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = ParticleFactory.CubeMesh();
            OneShot(chips, 14, 1f, false);
            var cm = chips.main;
            cm.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            cm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            cm.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            cm.startRotation3D = true;
            cm.gravityModifier = 1.6f;
            var csh = chips.shape; csh.shapeType = ParticleSystemShapeType.Hemisphere; csh.radius = 0.3f; csh.rotation = new Vector3(-90f, 0f, 0f);
            var colm = chips.collision; colm.enabled = true; colm.type = ParticleSystemCollisionType.World; colm.bounce = 0.25f; colm.dampen = 0.35f;
            Save(go);
        }

        /// <summary>Esqueleto surgindo do chão: poeira subindo e chamas fantasmagóricas esverdeadas.</summary>
        private static void SpawnDust()
        {
            ParticleSystem ps = Begin("FX2_Surgimento", "Surgimento dos esqueletos", alpha, out GameObject go);
            var main = ps.main;
            main.loop = false; main.duration = 1.2f; main.playOnAwake = true; main.stopAction = ParticleSystemStopAction.Destroy;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.4f, 0.34f), new Color(0.6f, 0.55f, 0.47f));
            var em = ps.emission; em.rateOverTime = 30f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 25f; sh.radius = 0.7f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.6f, 0.45f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.6f, 1.6f);

            ParticleSystem wisps = Sub(go, "Chamas", add);
            var wm = wisps.main;
            wm.loop = false; wm.duration = 1.2f; wm.playOnAwake = true; wm.simulationSpace = ParticleSystemSimulationSpace.World;
            wm.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            wm.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.2f);
            wm.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            wm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 1f, 0.6f), new Color(0.6f, 0.9f, 1f));
            var we = wisps.emission; we.rateOverTime = 22f;
            var ws = wisps.shape; ws.shapeType = ParticleSystemShapeType.Cone; ws.angle = 10f; ws.radius = 0.5f; ws.rotation = new Vector3(-90f, 0f, 0f);
            var wsz = wisps.sizeOverLifetime; wsz.enabled = true; wsz.size = SizeCurve(1f, 0f);
            Save(go);
        }

        /// <summary>Explosão do barril: bola de fogo, fumaça escura e brasas.</summary>
        private static void BarrelExplosion()
        {
            ParticleSystem ps = Begin("FX2_Explosao_do_Barril", "Explosão do barril", alpha, out GameObject go);
            OneShot(ps, 30, 2f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.15f, 0.13f, 0.12f), new Color(0.32f, 0.29f, 0.26f));
            main.gravityModifier = -0.15f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.6f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.85f, 0.6f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.6f, 2.2f);
            var limit = ps.limitVelocityOverLifetime; limit.enabled = true; limit.limit = 0.6f; limit.dampen = 0.15f;

            ParticleSystem fire = Sub(go, "Fogo", add);
            OneShot(fire, 26, 0.6f, false);
            var fm = fire.main;
            fm.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            fm.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            fm.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            fm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f), new Color(1f, 0.4f, 0.08f));
            var fs = fire.shape; fs.shapeType = ParticleSystemShapeType.Sphere; fs.radius = 0.4f;
            var fsz = fire.sizeOverLifetime; fsz.enabled = true; fsz.size = SizeCurve(1f, 0.3f);

            ParticleSystem embers = Sub(go, "Brasas", add);
            OneShot(embers, 30, 1.5f, false);
            var em2 = embers.main;
            em2.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            em2.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
            em2.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            em2.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.7f, 0.2f));
            em2.gravityModifier = 0.8f;
            var es = embers.shape; es.shapeType = ParticleSystemShapeType.Sphere; es.radius = 0.3f;
            ParticleSystemRenderer er = embers.GetComponent<ParticleSystemRenderer>();
            er.renderMode = ParticleSystemRenderMode.Stretch;
            er.velocityScale = 0.03f;
            er.lengthScale = 1.5f;
            Save(go);
        }

        /// <summary>Brasas subindo devagar (braseiros e destroços em chamas).</summary>
        private static void Embers()
        {
            ParticleSystem ps = Begin("FX2_Brasas", "Brasas no ar", add, out GameObject go);
            var main = ps.main;
            main.loop = true; main.duration = 4f; main.playOnAwake = true; main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.65f, 0.2f), new Color(1f, 0.4f, 0.1f));
            main.gravityModifier = -0.1f;
            var em = ps.emission; em.rateOverTime = 5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(0.8f, 0.8f, 0.1f); sh.rotation = new Vector3(-90f, 0f, 0f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.4f; noise.frequency = 0.8f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 1f, 0.8f));
            Save(go);
        }

        /// <summary>Gotas d'água pingando do teto (ambiente de abandono).</summary>
        private static void Drips()
        {
            ParticleSystem ps = Begin("FX2_Gotas", "Gotas pingando", alpha, out GameObject go);
            var main = ps.main;
            main.loop = true; main.duration = 3f; main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 3f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.05f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.8f, 0.9f, 0.8f));
            main.gravityModifier = 1f;
            var em = ps.emission; em.rateOverTime = 1.2f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.05f;
            var colm = ps.collision; colm.enabled = true; colm.type = ParticleSystemCollisionType.World; colm.lifetimeLoss = 1f; colm.bounce = 0f;
            Save(go);
        }

        /// <summary>Névoa rasteira sobre o piso (visibilidade reduzida).</summary>
        private static void GroundFog()
        {
            ParticleSystem ps = Begin("FX2_Nevoa_Rasteira", "Névoa rasteira", alpha, out GameObject go);
            var main = ps.main;
            main.loop = true; main.duration = 10f; main.playOnAwake = true; main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.58f, 0.6f, 0.18f));
            main.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 4f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(16f, 0.2f, 16f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0f, 1f));
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.08f; noise.frequency = 0.15f;
            Save(go);
        }

        /// <summary>Palha voando do boneco de treino.</summary>
        private static void Straw()
        {
            ParticleSystem ps = Begin("FX2_Palha", "Palha do boneco", L2Palette.Straw, out GameObject go);
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = ParticleFactory.CubeMesh();
            OneShot(ps, 12, 1f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startRotation3D = true;
            main.gravityModifier = 0.6f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 60f; sh.radius = 0.1f;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            Save(go);
        }

        /// <summary>Luzes fantasmagóricas flutuando sobre sarcófagos e altares.</summary>
        private static void Wisps()
        {
            ParticleSystem ps = Begin("FX2_Almas", "Almas flutuando", add, out GameObject go);
            var main = ps.main;
            main.loop = true; main.duration = 5f; main.playOnAwake = true; main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 1f, 0.9f, 0.8f), new Color(0.6f, 0.8f, 1f, 0.8f));
            main.gravityModifier = -0.02f;
            var em = ps.emission; em.rateOverTime = 2.5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.6f, 0.6f, 0.8f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0f, 1f));
            Save(go);
        }

        /// <summary>Poeira levantada quando os espinhos sobem.</summary>
        private static void SpikeDust()
        {
            ParticleSystem ps = Begin("FX2_Poeira_dos_Espinhos", "Poeira dos espinhos", alpha, out GameObject go);
            OneShot(ps, 16, 0.8f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 0.45f, 0.38f), new Color(0.62f, 0.57f, 0.5f));
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.8f, 1.8f, 0.1f); sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.7f, 0.4f));
            Save(go);
        }
    }
}
