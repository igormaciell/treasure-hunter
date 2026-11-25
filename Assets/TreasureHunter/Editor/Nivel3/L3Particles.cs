using UnityEditor;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Partículas do Nível 3: brilho dos cristais, estilhaços, poeira e pedrinhas do pouso, fumaça de queda,
    /// névoa do abismo, ativação do totem, reaparecimento, esporos, desmoronamento, gotas, rachadura e coleta.
    /// </summary>
    public static class L3Particles
    {
        private static Material alpha, add;

        /// <summary>
        /// Cria todos os efeitos.
        /// </summary>
        public static void BuildAll()
        {
            alpha = L2Core.Particle("mat3_particula_alfa", false);
            add = L2Core.Particle("mat3_particula_aditiva", true);

            CrystalSparkles();
            CrystalShards();
            LandingDust();
            Pebbles();
            HeavySmoke();
            AbyssMist();
            TotemActivation();
            Respawn();
            Spores();
            CrumbleDust();
            Drips();
            CrackDust();
            TotemCollect();
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

        /// <summary>Sistema filho (camada extra).</summary>
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

        /// <summary>Disparo único que se destrói ao terminar.</summary>
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

        /// <summary>Configuração de efeito contínuo.</summary>
        /// <param name="ps">Sistema.</param>
        /// <param name="duration">Duração do ciclo.</param>
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

        /// <summary>Curva de tamanho ao longo da vida.</summary>
        /// <param name="start">Multiplicador inicial.</param>
        /// <param name="end">Multiplicador final.</param>
        /// <returns>A curva.</returns>
        private static ParticleSystem.MinMaxCurve SizeCurve(float start, float end)
        {
            return new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, start, 1f, end));
        }

        /// <summary>Partículas sólidas (cubinhos) com gravidade e colisão.</summary>
        /// <param name="ps">Sistema.</param>
        /// <param name="go">Objeto do sistema.</param>
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
            colm.bounce = 0.35f; colm.dampen = 0.3f; colm.lifetimeLoss = 0f;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
        }

        // ================================================================== Efeitos

        /// <summary>Brilho que sai dos cristais (contínuo, sobe devagar).</summary>
        private static void CrystalSparkles()
        {
            ParticleSystem ps = Begin("FX3_Brilho_de_Cristal", "Brilho dos cristais", add, out GameObject go);
            Looping(ps, 4f, 7f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.95f, 1f), new Color(0.75f, 0.6f, 1f));
            main.gravityModifier = -0.04f;
            main.maxParticles = 40;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.6f;
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.2f; noise.frequency = 0.6f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0f, 1f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(1f, 0.2f);
            Save(go);
        }

        /// <summary>Estilhaços de cristal: lascas sólidas + clarão + faíscas.</summary>
        private static void CrystalShards()
        {
            ParticleSystem ps = Begin("FX3_Estilhacos_de_Cristal", "Estilhaços de cristal", L3Palette.CrystalCyan, out GameObject go);
            OneShot(ps, 30, 1.6f);
            Solid(ps, go, 0.05f, 0.16f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.4f;
            ParticleSystem flash = Sub(go, "Clarao", add);
            OneShot(flash, 1, 0.3f, false);
            var fm = flash.main; fm.startLifetime = 0.25f; fm.startSpeed = 0f; fm.startSize = 3f;
            fm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.95f, 1f, 0.9f));
            var fs = flash.shape; fs.shapeType = ParticleSystemShapeType.Sphere; fs.radius = 0.01f;
            var fc = flash.colorOverLifetime; fc.enabled = true; fc.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 1f, 0.6f));
            ParticleSystem sparks = Sub(go, "Faiscas", add);
            OneShot(sparks, 40, 1.2f, false);
            var sm = sparks.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f); sm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f); sm.gravityModifier = 0.5f;
            sm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 1f, 1f), new Color(0.8f, 0.7f, 1f));
            var ss = sparks.shape; ss.shapeType = ParticleSystemShapeType.Sphere; ss.radius = 0.3f;
            Save(go);
        }

        /// <summary>Anel de poeira no pouso (sai rente ao chão).</summary>
        private static void LandingDust()
        {
            ParticleSystem ps = Begin("FX3_Poeira_de_Pouso", "Poeira de pouso", alpha, out GameObject go);
            OneShot(ps, 16, 1f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.47f, 0.52f, 0.7f), new Color(0.6f, 0.62f, 0.68f, 0.7f));
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 88f; sh.radius = 0.25f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.9f, 0.6f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.7f, 2.2f);
            var limit = ps.limitVelocityOverLifetime; limit.enabled = true; limit.limit = 0.3f; limit.dampen = 0.25f;
            Save(go);
        }

        /// <summary>Pedrinhas que pulam do chão.</summary>
        private static void Pebbles()
        {
            ParticleSystem ps = Begin("FX3_Pedrinhas", "Pedrinhas", L3Palette.Rock, out GameObject go);
            OneShot(ps, 10, 1.4f);
            Solid(ps, go, 0.04f, 0.1f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 55f; sh.radius = 0.2f; sh.rotation = new Vector3(-90f, 0f, 0f);
            Save(go);
        }

        /// <summary>Fumaça grossa de pouso forte e de plataformas desmoronando.</summary>
        private static void HeavySmoke()
        {
            ParticleSystem ps = Begin("FX3_Fumaca_de_Queda", "Fumaça de queda", alpha, out GameObject go);
            OneShot(ps, 14, 2f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.36f, 0.4f, 0.6f));
            main.gravityModifier = -0.03f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 70f; sh.radius = 0.5f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.8f, 0.5f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.8f, 2.6f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.3f; noise.frequency = 0.4f;
            Save(go);
        }

        /// <summary>Névoa escura e lenta no fundo do abismo (com faíscas roxas raras).</summary>
        private static void AbyssMist()
        {
            ParticleSystem ps = Begin("FX3_Nevoa_do_Abismo", "Névoa do abismo", alpha, out GameObject go);
            Looping(ps, 12f, 6f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(6f, 11f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.12f, 0.1f, 0.22f, 0.35f), new Color(0.08f, 0.12f, 0.2f, 0.35f));
            main.maxParticles = 90;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(34f, 3f, 34f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0f, 1f));
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.15f; noise.frequency = 0.1f;
            ParticleSystem motes = Sub(go, "Faiscas_Roxas", add);
            Looping(motes, 6f, 3f);
            var mm = motes.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f); mm.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            mm.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f); mm.gravityModifier = -0.05f;
            mm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.4f, 1f), new Color(0.4f, 0.6f, 1f));
            var ms = motes.shape; ms.shapeType = ParticleSystemShapeType.Box; ms.scale = new Vector3(34f, 6f, 34f);
            var mc = motes.colorOverLifetime; mc.enabled = true; mc.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0f, 1f));
            Save(go);
        }

        /// <summary>Coluna de luz e anel quando o totem é colocado.</summary>
        private static void TotemActivation()
        {
            ParticleSystem ps = Begin("FX3_Ativacao_do_Totem", "Ativação do totem", add, out GameObject go);
            OneShot(ps, 40, 1.4f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 1f, 0.95f), new Color(0.7f, 1f, 1f));
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6f; sh.radius = 0.35f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 1f, 0.8f));
            ParticleSystem ring = Sub(go, "Anel", add);
            OneShot(ring, 36, 0.8f, false);
            var rm = ring.main; rm.startLifetime = 0.6f; rm.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 3f); rm.startSize = 0.1f;
            rm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 1f, 0.9f));
            var rs = ring.shape; rs.shapeType = ParticleSystemShapeType.Cone; rs.angle = 89f; rs.radius = 0.1f; rs.rotation = new Vector3(-90f, 0f, 0f);
            Save(go);
        }

        /// <summary>Feixe de partículas onde John reaparece.</summary>
        private static void Respawn()
        {
            ParticleSystem ps = Begin("FX3_Reaparecimento", "Reaparecimento", add, out GameObject go);
            OneShot(ps, 30, 1.2f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.9f, 1f), new Color(1f, 1f, 1f));
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.5f; sh.rotation = new Vector3(-90f, 0f, 0f);
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.08f; r.lengthScale = 2f;
            Save(go);
        }

        /// <summary>Esporos luminosos flutuando no ar da caverna (contínuo, área grande).</summary>
        private static void Spores()
        {
            ParticleSystem ps = Begin("FX3_Esporos", "Esporos luminosos", add, out GameObject go);
            Looping(ps, 8f, 5f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 1f, 0.7f), new Color(0.4f, 0.8f, 1f));
            main.maxParticles = 80;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(20f, 8f, 20f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0f, 1f));
            Save(go);
        }

        /// <summary>Poeira e cascalho caindo quando uma plataforma desmorona.</summary>
        private static void CrumbleDust()
        {
            ParticleSystem ps = Begin("FX3_Desmoronamento", "Poeira de desmoronamento", alpha, out GameObject go);
            OneShot(ps, 24, 2f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 0.41f, 0.45f, 0.6f));
            main.gravityModifier = 0.25f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(2.4f, 0.3f, 2.4f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 0.8f, 0.5f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = SizeCurve(0.7f, 2f);
            ParticleSystem bits = Sub(go, "Cascalho", L3Palette.Gravel);
            OneShot(bits, 14, 2f, false);
            Solid(bits, bits.gameObject, 0.05f, 0.14f);
            var bm = bits.main; bm.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f); bm.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1f);
            var bs = bits.shape; bs.shapeType = ParticleSystemShapeType.Box; bs.scale = new Vector3(2.2f, 0.2f, 2.2f);
            Save(go);
        }

        /// <summary>Gotas pingando das estalactites.</summary>
        private static void Drips()
        {
            ParticleSystem ps = Begin("FX3_Gotas", "Gotas da caverna", add, out GameObject go);
            Looping(ps, 3f, 0.8f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.2f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.05f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.85f, 1f, 0.8f));
            main.gravityModifier = 1f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.05f;
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.03f; r.lengthScale = 1.5f;
            Save(go);
        }

        /// <summary>Pó de cristal ao rachar (cristal do totem, plataformas frágeis).</summary>
        private static void CrackDust()
        {
            ParticleSystem ps = Begin("FX3_Rachadura", "Pó de rachadura", add, out GameObject go);
            OneShot(ps, 14, 0.8f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.95f, 1f), new Color(0.6f, 0.7f, 0.9f));
            main.gravityModifier = 0.6f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
            Save(go);
        }

        /// <summary>Espiral de brilhos ao coletar o totem.</summary>
        private static void TotemCollect()
        {
            ParticleSystem ps = Begin("FX3_Coleta_do_Totem", "Coleta do totem", add, out GameObject go);
            OneShot(ps, 50, 1.6f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.5f), new Color(0.5f, 1f, 0.95f));
            main.gravityModifier = -0.3f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.7f;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.orbitalY = new ParticleSystem.MinMaxCurve(4f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 1f, 0.9f));
            Save(go);
        }
    }
}
