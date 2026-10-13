using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Cria os prefabs de partículas do nível: fogo, fumaça, explosão, pedrinhas, poeira,
    /// aviso de queda, brilho de tesouro, folhas caindo e poeira no feixe de luz.
    /// </summary>
    public static class ParticleFactory
    {
        /// <summary>Prefabs de partículas criados, por nome.</summary>
        public static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();

        /// <summary>
        /// Cria todos os prefabs de partículas.
        /// </summary>
        public static void BuildAll()
        {
            Prefabs.Clear();
            Material alpha = MaterialFactory.Particle("mat_particle_alpha", false);
            Material add = MaterialFactory.Particle("mat_particle_add", true);
            Material rock = MaterialFactory.Get("mat_rock");

            Fire(add);
            Smoke(alpha);
            Explosion(alpha, add);
            Pebbles(rock);
            DoorDust(alpha);
            WarningDust(alpha);
            Sparkle(add);
            Glint(add);
            Leaves(alpha);
            DustMotes(add);
        }

        /// <summary>
        /// Cria o objeto com ParticleSystem, já parado, e devolve o sistema para configuração.
        /// </summary>
        /// <param name="name">Nome do prefab.</param>
        /// <param name="mat">Material das partículas.</param>
        /// <param name="go">Saída: GameObject criado.</param>
        /// <returns>O ParticleSystem.</returns>
        private static ParticleSystem Begin(string name, Material mat, out GameObject go)
        {
            go = new GameObject(name);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            AssetCategory cat = go.AddComponent<AssetCategory>();
            cat.category = AssetCategoryType.Efeitos;
            cat.displayName = name;
            return ps;
        }

        /// <summary>
        /// Salva o objeto como prefab e destrói a cópia temporária da cena.
        /// </summary>
        /// <param name="go">Objeto configurado.</param>
        /// <returns>O prefab salvo.</returns>
        private static GameObject Save(GameObject go)
        {
            string path = BuilderCore.PrefabsPath + "/" + go.name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Prefabs[prefab.name] = prefab;
            return prefab;
        }

        /// <summary>
        /// Cria um gradiente de cor/transparência para "Color over Lifetime".
        /// </summary>
        /// <param name="c0">Cor inicial.</param>
        /// <param name="c1">Cor final.</param>
        /// <param name="a0">Alfa inicial.</param>
        /// <param name="aMid">Alfa no meio da vida.</param>
        /// <returns>O gradiente.</returns>
        private static Gradient Grad(Color c0, Color c1, float a0, float aMid)
        {
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c0, 0f), new GradientColorKey(c1, 1f) },
                      new[] { new GradientAlphaKey(a0, 0f), new GradientAlphaKey(aMid, 0.3f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        /// <summary>
        /// Curva linear de tamanho ao longo da vida.
        /// </summary>
        /// <param name="start">Multiplicador inicial.</param>
        /// <param name="end">Multiplicador final.</param>
        /// <returns>A curva.</returns>
        private static ParticleSystem.MinMaxCurve SizeCurve(float start, float end)
        {
            return new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, start, 1f, end));
        }

        /// <summary>Fogo de tochas e braseiros (aditivo, em loop).</summary>
        /// <param name="mat">Material aditivo.</param>
        private static void Fire(Material mat)
        {
            ParticleSystem ps = Begin("FX_Fire", mat, out GameObject go);
            var main = ps.main;
            main.loop = true; main.duration = 1f; main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.3f), new Color(1f, 0.5f, 0.1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;
            var em = ps.emission; em.rateOverTime = 28f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = 0.08f;
            sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Grad(new Color(1f, 0.9f, 0.5f), new Color(0.9f, 0.2f, 0.05f), 0.9f, 0.8f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(1f, 0.2f);
            Save(go);
        }

        /// <summary>Fumaça cinza que sobe devagar (braseiros, desabamento).</summary>
        /// <param name="mat">Material com transparência.</param>
        private static void Smoke(Material mat)
        {
            ParticleSystem ps = Begin("FX_Smoke", mat, out GameObject go);
            var main = ps.main;
            main.loop = true; main.duration = 2f; main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.33f, 0.3f), new Color(0.5f, 0.48f, 0.45f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 6f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 8f; sh.radius = 0.15f;
            sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0f, 0.35f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.6f, 2.2f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.4f;
            Save(go);
        }

        /// <summary>Explosão do impacto da pedra: clarão alaranjado + nuvem de poeira (dispara uma vez e se destrói).</summary>
        /// <param name="alpha">Material com transparência.</param>
        /// <param name="add">Material aditivo.</param>
        private static void Explosion(Material alpha, Material add)
        {
            ParticleSystem ps = Begin("FX_Explosion", alpha, out GameObject go);
            var main = ps.main;
            main.loop = false; main.duration = 1f; main.playOnAwake = true; main.stopAction = ParticleSystemStopAction.Destroy;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.4f, 0.33f), new Color(0.62f, 0.56f, 0.47f));
            main.gravityModifier = 0.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 0.3f;
            sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.9f, 0.6f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.7f, 1.8f);
            var limit = ps.limitVelocityOverLifetime; limit.enabled = true; limit.limit = 0.5f; limit.dampen = 0.15f;

            // Clarão aditivo rápido como filho.
            GameObject flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(go.transform, false);
            ParticleSystem flash = flashGo.AddComponent<ParticleSystem>();
            flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            flashGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = add;
            var fm = flash.main;
            fm.loop = false; fm.duration = 0.3f; fm.playOnAwake = true;
            fm.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            fm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            fm.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            fm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.3f), new Color(1f, 0.45f, 0.1f));
            fm.simulationSpace = ParticleSystemSimulationSpace.World;
            var fe = flash.emission; fe.rateOverTime = 0f; fe.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
            var fs = flash.shape; fs.shapeType = ParticleSystemShapeType.Sphere; fs.radius = 0.2f;
            Save(go);
        }

        /// <summary>Pedrinhas: pequenos cubos de rocha com gravidade e colisão com o cenário.</summary>
        /// <param name="rock">Material de rocha (opaco).</param>
        private static void Pebbles(Material rock)
        {
            ParticleSystem ps = Begin("FX_Pebbles", rock, out GameObject go);
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = CubeMesh();
            var main = ps.main;
            main.loop = false; main.duration = 1f; main.playOnAwake = true; main.stopAction = ParticleSystemStopAction.Destroy;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.17f);
            main.startRotation3D = true;
            main.gravityModifier = 1.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 50f; sh.radius = 0.2f;
            sh.rotation = new Vector3(-90f, 0f, 0f);
            var colm = ps.collision; colm.enabled = true; colm.type = ParticleSystemCollisionType.World;
            colm.bounce = 0.35f; colm.dampen = 0.25f; colm.lifetimeLoss = 0f;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);
            Save(go);
        }

        /// <summary>Poeira das portas de pedra (loop enquanto a porta se move; tocado por script).</summary>
        /// <param name="mat">Material com transparência.</param>
        private static void DoorDust(Material mat)
        {
            ParticleSystem ps = Begin("FX_DoorDust", mat, out GameObject go);
            var main = ps.main;
            main.loop = true; main.duration = 1f; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.48f, 0.38f), new Color(0.68f, 0.6f, 0.5f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 26f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(3f, 0.2f, 0.8f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0f, 0.45f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.6f, 1.6f);
            Save(go);
        }

        /// <summary>Aviso de queda de pedra: poeira e grãos caindo do teto (uma vez).</summary>
        /// <param name="mat">Material com transparência.</param>
        private static void WarningDust(Material mat)
        {
            ParticleSystem ps = Begin("FX_WarningDust", mat, out GameObject go);
            var main = ps.main;
            main.loop = false; main.duration = 0.6f; main.playOnAwake = true; main.stopAction = ParticleSystemStopAction.Destroy;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.55f, 0.45f), new Color(0.75f, 0.7f, 0.6f));
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 40f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.35f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.8f, 0.6f));
            Save(go);
        }

        /// <summary>Brilho ao coletar um tesouro (estrelinhas douradas, uma vez).</summary>
        /// <param name="mat">Material aditivo.</param>
        private static void Sparkle(Material mat)
        {
            ParticleSystem ps = Begin("FX_Sparkle", mat, out GameObject go);
            var main = ps.main;
            main.loop = false; main.duration = 0.5f; main.playOnAwake = true; main.stopAction = ParticleSystemStopAction.Destroy;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.4f), new Color(1f, 1f, 0.8f));
            main.gravityModifier = -0.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(1f, 0f);
            Save(go);
        }

        /// <summary>Brilho contínuo e discreto em volta dos artefatos.</summary>
        /// <param name="mat">Material aditivo.</param>
        private static void Glint(Material mat)
        {
            ParticleSystem ps = Begin("FX_Glint", mat, out GameObject go);
            var main = ps.main;
            main.loop = true; main.duration = 1f; main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.5f), Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 6f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.4f;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0f, 1f);
            Save(go);
        }

        /// <summary>Folhas caindo na floresta da introdução.</summary>
        /// <param name="mat">Material com transparência.</param>
        private static void Leaves(Material mat)
        {
            ParticleSystem ps = Begin("FX_Leaves", mat, out GameObject go);
            var main = ps.main;
            main.loop = true; main.duration = 5f; main.playOnAwake = true; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.6f, 0.15f), new Color(0.75f, 0.6f, 0.2f));
            main.gravityModifier = 0.03f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 150;
            var em = ps.emission; em.rateOverTime = 12f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(40f, 1f, 40f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.3f;
            Save(go);
        }

        /// <summary>Poeira flutuando nos feixes de luz.</summary>
        /// <param name="mat">Material aditivo.</param>
        private static void DustMotes(Material mat)
        {
            ParticleSystem ps = Begin("FX_DustMotes", mat, out GameObject go);
            var main = ps.main;
            main.loop = true; main.duration = 5f; main.playOnAwake = true; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.8f, 0.6f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 8f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(2.5f, 4f, 2.5f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0f, 0.8f));
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.1f; noise.frequency = 0.2f;
            Save(go);
        }

        /// <summary>
        /// Obtém a malha de cubo embutida do Unity (usada nas pedrinhas).
        /// </summary>
        /// <returns>A malha do cubo.</returns>
        public static Mesh CubeMesh()
        {
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh m = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);
            return m;
        }
    }
}
