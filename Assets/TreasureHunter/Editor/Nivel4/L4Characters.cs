using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// "John_Craft_Furtivo": parte do John armado do Nível 2 (revólver, pedras, mira — sem alterá-lo), troca a
    /// lanterna do cinto pela tocha do Nível 1 na mão esquerda (agora pode ser apagada), acrescenta a
    /// furtividade (agachar, ruído, visibilidade, morte com um golpe) e um controller com as animações de
    /// agachado (parado e andando) e de queda ao ser golpeado.
    /// </summary>
    public static class L4Characters
    {
        /// <summary>Prefab do jogador do Nível 4.</summary>
        public static GameObject PlayerPrefab;

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        private static float[] T(params float[] v) => v;

        /// <summary>Altura do quadril agachado.</summary>
        private const float CrouchHip = 0.62f;

        /// <summary>
        /// Cria o jogador do Nível 4.
        /// </summary>
        /// <returns>true se o John do Nível 2 foi encontrado.</returns>
        public static bool BuildAll()
        {
            GameObject source = L2Core.L2Prefab("John_Craft_Combate");
            GameObject torchSource = L2Core.L1("John_Craft");
            if (source == null || torchSource == null) return false;

            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "John_Craft_Furtivo";
            Transform model = root.transform.Find("Model");
            Transform body = model.Find("Body");
            Transform armL = body.Find("ArmL");

            // Sem a lanterna do cinto: no escuro, só a tocha (que denuncia John).
            for (int i = body.childCount - 1; i >= 0; i--)
            {
                Transform ch = body.GetChild(i);
                if (ch.name.StartsWith("Lanterna")) Object.DestroyImmediate(ch.gameObject);
            }

            // Tocha do Nível 1, agora na mão esquerda.
            GameObject l1 = (GameObject)PrefabUtility.InstantiatePrefab(torchSource);
            PrefabUtility.UnpackPrefabInstance(l1, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Transform torch = l1.transform.Find("Model/Body/ArmR/Tocha");
            Transform torchVisual = null;
            Light torchLight = null;
            ParticleSystem flame = null;
            if (torch != null)
            {
                torch.SetParent(armL, false);
                torch.localPosition = V(0f, -0.5f, 0.06f);
                torchVisual = torch.Find("Visual");
                torchLight = torch.GetComponentInChildren<Light>(true);
                flame = torch.GetComponentInChildren<ParticleSystem>(true);
            }
            Object.DestroyImmediate(l1);

            // Luz fraca para o jogador enxergar John no escuro (não conta para a visibilidade).
            GameObject fillGo = new GameObject("LuzDeVisao");
            fillGo.transform.SetParent(root.transform, false);
            fillGo.transform.localPosition = V(0f, 2.3f, -0.6f);
            Light fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(0.55f, 0.65f, 0.85f);
            fill.range = 5.5f;
            fill.intensity = 0.45f;
            fill.shadows = LightShadows.None;

            StealthTorch st = root.AddComponent<StealthTorch>();
            st.torchLight = torchLight;
            st.flame = flame;
            st.torchVisual = torchVisual;
            st.armPivot = armL;
            st.startLit = false;
            st.igniteEffects = new[] { L2Core.Get("FX4_Faiscas") };
            st.extinguishEffects = new[] { L2Core.Get("FX4_Fumaca_Apagada") };

            PlayerController pc = root.GetComponent<PlayerController>();
            PlayerStealth ps = root.AddComponent<PlayerStealth>();
            ps.controller = pc;
            ps.animator = model.GetComponent<Animator>();
            ps.torch = st;
            ps.visionFill = fill;
            ps.deathEffects = new[] { L2Core.Get("FX4_Poeira_do_Passo") };

            PlayerCombat combat = root.GetComponent<PlayerCombat>();
            if (combat != null) combat.startingShots = 6;

            L2Core.SetLayerRecursive(root, 2);
            model.GetComponent<Animator>().runtimeAnimatorController = BuildController(model);
            PlayerHealth health = root.GetComponent<PlayerHealth>();
            if (health != null) health.blinkRenderers = model.GetComponentsInChildren<MeshRenderer>(true);
            PlayerPrefab = L2Core.Save(root, AssetCategoryType.Personagem, "John Craft (furtivo: agachar, tocha, revólver)");
            return true;
        }

        // ================================================================== Animações

        /// <summary>
        /// Controller: o mesmo do Nível 2 (locomoção + camada dos braços) com o agachar (parado e andando)
        /// e a queda ao ser golpeado.
        /// </summary>
        /// <param name="model">Raiz do Animator.</param>
        /// <returns>O controller.</returns>
        private static AnimatorController BuildController(Transform model)
        {
            AnimatorController ctrl = L2Characters.PlayerController(model, "John_Craft_Furtivo");
            AnimationClip crouchIdle = L2Anim.Make("John4_Agachado", 2f, true, CrouchIdle);
            AnimationClip crouchWalk = L2Anim.Make("John4_Agachado_Andando", 1.1f, true, CrouchWalk);
            AnimationClip death = L2Anim.Make("John4_Golpeado", 1.1f, false, Death);
            ctrl.AddParameter("Crouch", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState loco = null, fall = null;
            foreach (ChildAnimatorState cs in sm.states)
            {
                if (cs.state.name == "Locomocao") loco = cs.state;
                if (cs.state.name == "Caindo") fall = cs.state;
            }
            AnimatorState crouch = ctrl.CreateBlendTreeInController("Agachado", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(crouchIdle, 0f);
            tree.AddChild(crouchWalk, 1.6f);
            AnimatorState dead = sm.AddState("Golpeado");
            dead.motion = death;

            AnimatorStateTransition t;
            if (loco != null)
            {
                t = loco.AddTransition(crouch);
                t.AddCondition(AnimatorConditionMode.If, 0f, "Crouch");
                L2Anim.Instant(t, 0.2f);
                t = crouch.AddTransition(loco);
                t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Crouch");
                L2Anim.Instant(t, 0.2f);
            }
            if (fall != null)
            {
                t = crouch.AddTransition(fall);
                t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
                t.AddCondition(AnimatorConditionMode.Less, -2f, "VerticalVelocity");
                L2Anim.Instant(t, 0.15f);
            }
            t = sm.AddAnyStateTransition(dead);
            t.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            t.canTransitionToSelf = false;
            L2Anim.Instant(t, 0.05f);
            if (loco != null)
            {
                t = dead.AddTransition(loco);
                t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
                L2Anim.Instant(t, 0.1f);
            }
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        /// <summary>Agachado parado: quadril baixo, uma perna à frente e outra atrás, corpo inclinado, cabeça erguida.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void CrouchIdle(AnimationClip c, float l)
        {
            L2Anim.HipKeys(c, T(0f, l * 0.5f, l), T(CrouchHip, CrouchHip - 0.015f, CrouchHip));
            L2Anim.Swing(c, L2Anim.Body, l, 28f, 1.5f, 1f, 0f);
            L2Anim.Swing(c, L2Anim.Head, l, -24f, 2f, 1f, 0.3f);
            L2Anim.Hold(c, L2Anim.LegL, l, -50f, 0f, -3f);
            L2Anim.Hold(c, L2Anim.LegR, l, 46f, 0f, 3f);
            L2Anim.Swing(c, L2Anim.ArmL, l, -35f, 3f, 1f, 0f, -8f);
            L2Anim.Swing(c, L2Anim.ArmR, l, -30f, 3f, 1f, 0.5f, 8f);
        }

        /// <summary>
        /// Agachado andando: pernas alternadas abrindo para os lados no meio do passo (os pés não atravessam o
        /// chão com o quadril baixo), corpo inclinado e passos curtos.
        /// </summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void CrouchWalk(AnimationClip c, float l)
        {
            const int n = 12;
            const float amp = 46f, legLen = 0.95f;
            float[] t = new float[n + 1], hip = new float[n + 1];
            float[] lx = new float[n + 1], lz = new float[n + 1], rx = new float[n + 1], rz = new float[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float u = i / (float)n;
                t[i] = l * u;
                hip[i] = CrouchHip + 0.02f * Mathf.Cos(u * 4f * Mathf.PI);
                float xa = amp * Mathf.Sin(u * 2f * Mathf.PI);
                lx[i] = xa;
                rx[i] = -xa;
                float cx = Mathf.Cos(xa * Mathf.Deg2Rad) * legLen;
                float splay = cx > hip[i] ? Mathf.Acos(Mathf.Clamp01(hip[i] / cx)) * Mathf.Rad2Deg : 0f;
                lz[i] = -splay;
                rz[i] = splay;
            }
            L2Anim.HipKeys(c, t, hip);
            L2Anim.Keys(c, L2Anim.LegL, t, lx, null, lz);
            L2Anim.Keys(c, L2Anim.LegR, t, rx, null, rz);
            L2Anim.Swing(c, L2Anim.Body, l, 30f, 2f, 2f, 0f);
            L2Anim.Swing(c, L2Anim.Head, l, -26f, 2f, 2f, 0.5f);
            L2Anim.Swing(c, L2Anim.ArmL, l, -35f, 10f, 1f, 0.5f, -8f);
            L2Anim.Swing(c, L2Anim.ArmR, l, -30f, 10f, 1f, 0f, 8f);
        }

        /// <summary>Golpeado: tranco, joelhos cedem e John cai de bruços.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Death(AnimationClip c, float l)
        {
            float[] t = T(0f, 0.12f, 0.45f, 0.8f, l);
            L2Anim.HipKeys(c, t, T(L2Anim.HipHeight, L2Anim.HipHeight - 0.05f, 0.55f, 0.25f, 0.22f));
            L2Anim.Keys(c, L2Anim.Body, t, T(0f, -25f, 30f, 78f, 82f), null, T(0f, 8f, 12f, 18f, 18f));
            L2Anim.Keys(c, L2Anim.Head, t, T(0f, -30f, 10f, -20f, -15f), null, T(0f, 10f, 5f, 25f, 25f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(0f, -60f, -90f, -160f, -165f), null, T(0f, -25f, -20f, -15f, -15f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(0f, -60f, -80f, -150f, -150f), null, T(0f, 25f, 20f, 20f, 20f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(0f, 15f, -55f, -20f, -15f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(0f, 10f, -45f, -10f, -5f));
        }
    }
}
