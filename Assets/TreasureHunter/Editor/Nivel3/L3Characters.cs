using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// John Craft do parkour (Nível 3): criado a partir do prefab do Nível 1 (sem alterá-lo), sem a tocha,
    /// com lanterna de cristal, ParkourController (bordas, pouso, abismo), TotemAbility (totem de checkpoint)
    /// e um Animator com parado, andar, correr, pular, cair, aterrissar (normal e forte), escalar borda,
    /// empurrar, dano e colocar o totem.
    /// </summary>
    public static class L3Characters
    {
        /// <summary>Prefab do jogador do parkour.</summary>
        public static GameObject PlayerPrefab;

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        private static float[] T(params float[] v) => v;
        private const float Hip = L2Anim.HipHeight;

        /// <summary>
        /// Cria o controller e o prefab John_Craft_Parkour.
        /// </summary>
        public static void BuildAll()
        {
            GameObject source = L2Core.L1("John_Craft");
            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "John_Craft_Parkour";

            TorchTool torch = root.GetComponent<TorchTool>();
            if (torch != null) Object.DestroyImmediate(torch);
            Transform model = root.transform.Find("Model");
            Transform body = model.Find("Body");
            Transform torchVisual = body.Find("ArmR/Tocha");
            if (torchVisual != null) Object.DestroyImmediate(torchVisual.gameObject);

            GameObject lanternPrefab = L2Core.Get("Lanterna_de_Cristal");
            if (lanternPrefab != null)
            {
                GameObject lantern = (GameObject)PrefabUtility.InstantiatePrefab(lanternPrefab);
                lantern.transform.SetParent(body, false);
                lantern.transform.localPosition = V(-0.3f, 0.02f, 0.12f);
            }
            L2Core.SetLayerRecursive(root, 2);

            Animator animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = Controller();

            // Movimento um pouco mais solto no ar (mesmo script, só o valor do prefab muda).
            PlayerController pc = root.GetComponent<PlayerController>();
            pc.airControl = 0.6f;

            ParkourController parkour = root.AddComponent<ParkourController>();
            parkour.animator = animator;
            parkour.model = model;
            parkour.landingEffects = new[] { L2Core.Get("FX3_Poeira_de_Pouso") };
            parkour.hardLandingEffects = new[] { L2Core.Get("FX3_Poeira_de_Pouso"), L2Core.Get("FX3_Fumaca_de_Queda"), L2Core.Get("FX3_Pedrinhas") };

            TotemAbility totem = root.AddComponent<TotemAbility>();
            totem.totemPrefab = L2Core.Get("Totem_de_Checkpoint");
            totem.ghostPrefab = L2Core.Get("Totem_Fantasma");
            totem.unlocked = false;
            totem.loadSavedProgress = false;

            PlayerHealth health = root.GetComponent<PlayerHealth>();
            health.blinkRenderers = model.GetComponentsInChildren<MeshRenderer>(true);

            PlayerPrefab = L2Core.Save(root, AssetCategoryType.Personagem, "John Craft (parkour)");
        }

        // ================================================================== Animator

        /// <summary>
        /// Controller do parkour: locomoção (blend tree), pulo, queda, aterrissagem normal e forte, escalada,
        /// empurrar, dano e colocar o totem.
        /// </summary>
        /// <returns>O controller salvo.</returns>
        private static AnimatorController Controller()
        {
            AnimationClip idle = L2Anim.Make("John3_Parado", 2f, true, Idle);
            AnimationClip walk = L2Anim.Make("John3_Andando", 0.9f, true, (c, l) => Locomotion(c, l, 30f, 25f, 0.03f, 3f, 0f));
            AnimationClip run = L2Anim.Make("John3_Correndo", 0.5f, true, (c, l) => Locomotion(c, l, 58f, 55f, 0.07f, 14f, -12f));
            AnimationClip jump = L2Anim.Make("John3_Pulando", 0.4f, false, Jump);
            AnimationClip fall = L2Anim.Make("John3_Caindo", 0.8f, true, Fall);
            AnimationClip land = L2Anim.Make("John3_Aterrissando", 0.3f, false, (c, l) => Land(c, l, 0.2f, 45f));
            AnimationClip hardLand = L2Anim.Make("John3_Aterrissagem_Forte", 0.6f, false, (c, l) => Land(c, l, 0.42f, 85f));
            AnimationClip climb = L2Anim.Make("John3_Escalando", 0.5f, false, Climb);
            AnimationClip push = L2Anim.Make("John3_Empurrando", 1f, true, Push);
            AnimationClip hurt = L2Anim.Make("John3_Dano", 0.4f, false, Hurt);
            AnimationClip place = L2Anim.Make("John3_Colocando_Totem", 0.7f, false, PlaceTotem);

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(L2Core.AnimationsPath + "/John_Craft_Parkour.controller");
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Pushing", AnimatorControllerParameterType.Bool);
            foreach (string trig in new[] { "Hurt", "Land", "HardLand", "Mantle", "Place" })
                ctrl.AddParameter(trig, AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState loco = ctrl.CreateBlendTreeInController("Locomocao", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, 3.2f);
            tree.AddChild(run, 6.2f);
            sm.defaultState = loco;

            AnimatorState jumpS = sm.AddState("Pulando"); jumpS.motion = jump;
            AnimatorState fallS = sm.AddState("Caindo"); fallS.motion = fall;
            AnimatorState landS = sm.AddState("Aterrissando"); landS.motion = land;
            AnimatorState hardS = sm.AddState("Aterrissagem_Forte"); hardS.motion = hardLand;
            AnimatorState climbS = sm.AddState("Escalando"); climbS.motion = climb;
            AnimatorState pushS = sm.AddState("Empurrando"); pushS.motion = push;
            AnimatorState hurtS = sm.AddState("Dano"); hurtS.motion = hurt;
            AnimatorState placeS = sm.AddState("Colocando_Totem"); placeS.motion = place;

            // Gatilhos de qualquer estado (ordem = prioridade).
            Any(sm, climbS, "Mantle", 0.05f);
            Any(sm, hardS, "HardLand", 0.04f);
            Any(sm, landS, "Land", 0.04f);
            Any(sm, jumpS, "Jump", 0.05f);
            Any(sm, hurtS, "Hurt", 0.05f);
            Any(sm, placeS, "Place", 0.08f);

            AnimatorStateTransition t = jumpS.AddTransition(fallS);
            t.AddCondition(AnimatorConditionMode.Less, 0f, "VerticalVelocity");
            L2Anim.Instant(t, 0.15f);
            t = jumpS.AddTransition(loco);
            t.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            t.AddCondition(AnimatorConditionMode.Less, 0.5f, "VerticalVelocity");
            L2Anim.Instant(t, 0.1f);
            t = loco.AddTransition(fallS);
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            t.AddCondition(AnimatorConditionMode.Less, -2f, "VerticalVelocity");
            L2Anim.Instant(t, 0.15f);
            t = fallS.AddTransition(loco);
            t.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            L2Anim.Instant(t, 0.1f);
            t = loco.AddTransition(pushS);
            t.AddCondition(AnimatorConditionMode.If, 0f, "Pushing");
            L2Anim.Instant(t, 0.15f);
            t = pushS.AddTransition(loco);
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Pushing");
            L2Anim.Instant(t, 0.2f);
            foreach (AnimatorState s in new[] { landS, hardS, climbS, hurtS, placeS })
                L2Anim.AfterClip(s.AddTransition(loco), 0.1f, 0.9f);

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        /// <summary>
        /// Transição de qualquer estado por gatilho.
        /// </summary>
        /// <param name="sm">Máquina de estados.</param>
        /// <param name="state">Destino.</param>
        /// <param name="trigger">Gatilho.</param>
        /// <param name="duration">Mistura.</param>
        private static void Any(AnimatorStateMachine sm, AnimatorState state, string trigger, float duration)
        {
            AnimatorStateTransition t = sm.AddAnyStateTransition(state);
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            t.canTransitionToSelf = false;
            L2Anim.Instant(t, duration);
        }

        // ================================================================== Clipes

        /// <summary>Parado: respiração e olhar ao redor.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Idle(AnimationClip c, float l)
        {
            L2Anim.HipBob(c, l, 0.02f, 1f);
            L2Anim.Swing(c, L2Anim.Body, l, 0f, 1.5f, 1f, 0f);
            L2Anim.Keys(c, L2Anim.Head, T(0f, l * 0.3f, l * 0.6f, l), T(0f, 2f, 0f, 0f), T(0f, 15f, -15f, 0f));
            L2Anim.Swing(c, L2Anim.ArmL, l, -4f, 3f, 1f, 0f, -5f);
            L2Anim.Swing(c, L2Anim.ArmR, l, -4f, 3f, 1f, 0.5f, 5f);
            L2Anim.Hold(c, L2Anim.LegL, l, 0f, 0f, -2f);
            L2Anim.Hold(c, L2Anim.LegR, l, 0f, 0f, 2f);
        }

        /// <summary>Andar/correr com braços balançando (correndo: braços dobrados e corpo inclinado).</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        /// <param name="legAmp">Amplitude das pernas.</param>
        /// <param name="armAmp">Amplitude dos braços.</param>
        /// <param name="bob">Balanço.</param>
        /// <param name="lean">Inclinação.</param>
        /// <param name="armOffset">Ângulo médio dos braços.</param>
        private static void Locomotion(AnimationClip c, float l, float legAmp, float armAmp, float bob, float lean, float armOffset)
        {
            L2Anim.HipBob(c, l, bob, 2f);
            L2Anim.Swing(c, L2Anim.Body, l, lean, 2f, 2f, 0f);
            L2Anim.Swing(c, L2Anim.Head, l, -lean * 0.5f, 2f, 2f, 0.5f);
            L2Anim.Swing(c, L2Anim.LegL, l, 0f, legAmp, 1f, 0f);
            L2Anim.Swing(c, L2Anim.LegR, l, 0f, legAmp, 1f, 0.5f);
            L2Anim.Swing(c, L2Anim.ArmL, l, armOffset, armAmp, 1f, 0.5f, -6f);
            L2Anim.Swing(c, L2Anim.ArmR, l, armOffset, armAmp, 1f, 0f, 6f);
        }

        /// <summary>Pulando: impulso com uma perna, outra recolhida, braços para cima e para a frente.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Jump(AnimationClip c, float l)
        {
            float[] t = T(0f, 0.12f, l);
            L2Anim.HipKeys(c, t, T(Hip, Hip, Hip));
            L2Anim.Keys(c, L2Anim.Body, t, T(4f, -8f, -6f));
            L2Anim.Keys(c, L2Anim.Head, t, T(0f, -10f, -8f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(0f, -60f, -50f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(0f, 25f, 20f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(-20f, -150f, -140f), null, T(-5f, -15f, -15f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(-20f, -130f, -120f), null, T(5f, 15f, 15f));
        }

        /// <summary>Caindo: braços abertos para equilibrar e pernas preparando o pouso.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Fall(AnimationClip c, float l)
        {
            L2Anim.HipBob(c, l, 0f, 1f);
            L2Anim.Swing(c, L2Anim.Body, l, 6f, 3f, 1f, 0f);
            L2Anim.Swing(c, L2Anim.Head, l, 12f, 4f, 2f, 0f);
            L2Anim.Swing(c, L2Anim.LegL, l, -25f, 10f, 2f, 0f, -6f);
            L2Anim.Swing(c, L2Anim.LegR, l, -10f, 10f, 2f, 0.5f, 6f);
            L2Anim.Swing(c, L2Anim.ArmL, l, -100f, 20f, 2f, 0.25f, -55f);
            L2Anim.Swing(c, L2Anim.ArmR, l, -100f, 20f, 2f, 0.75f, 55f);
        }

        /// <summary>Aterrissando: o quadril desce, joelhos dobram e o corpo se inclina; depois volta.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        /// <param name="dip">Quanto o quadril desce.</param>
        /// <param name="knee">Ângulo das pernas.</param>
        private static void Land(AnimationClip c, float l, float dip, float knee)
        {
            float[] t = T(0f, l * 0.25f, l * 0.6f, l);
            L2Anim.HipKeys(c, t, T(Hip - dip * 0.3f, Hip - dip, Hip - dip * 0.8f, Hip));
            L2Anim.Keys(c, L2Anim.Body, t, T(10f, knee * 0.4f, knee * 0.3f, 2f));
            L2Anim.Keys(c, L2Anim.Head, t, T(0f, -knee * 0.25f, -knee * 0.2f, 0f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(-15f, -knee, -knee * 0.8f, 0f), null, T(-4f, -8f, -8f, -2f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(-15f, -knee * 0.9f, -knee * 0.7f, 0f), null, T(4f, 8f, 8f, 2f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(-60f, -35f, -25f, -4f), null, T(-30f, -20f, -10f, -4f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(-60f, -35f + dip * 60f, -25f, -4f), null, T(30f, 20f, 10f, 4f));
        }

        /// <summary>Escalando: pendura com os braços no alto, puxa o corpo e sobe a perna na borda.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Climb(AnimationClip c, float l)
        {
            float[] t = T(0f, 0.14f, 0.3f, 0.42f, l);
            L2Anim.HipKeys(c, t, T(Hip, Hip, Hip, Hip - 0.1f, Hip));
            L2Anim.Keys(c, L2Anim.Body, t, T(0f, -5f, 25f, 35f, 5f));
            L2Anim.Keys(c, L2Anim.Head, t, T(-20f, -25f, -10f, 0f, 0f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(-170f, -175f, -110f, -40f, -5f), null, T(-10f, -10f, -15f, -20f, -4f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(-170f, -175f, -110f, -40f, -5f), null, T(10f, 10f, 15f, 20f, 4f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(10f, 15f, -90f, -60f, 0f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(5f, 20f, 10f, -30f, 0f));
        }

        /// <summary>Empurrando (como no Nível 1, com os dois braços).</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Push(AnimationClip c, float l)
        {
            L2Anim.HipBob(c, l, 0.03f, 2f);
            L2Anim.Swing(c, L2Anim.Body, l, 22f, 2f, 2f, 0f);
            L2Anim.Swing(c, L2Anim.Head, l, -15f, 2f, 2f, 0f);
            L2Anim.Swing(c, L2Anim.LegL, l, -10f, 20f, 1f, 0f);
            L2Anim.Swing(c, L2Anim.LegR, l, -10f, 20f, 1f, 0.5f);
            L2Anim.Swing(c, L2Anim.ArmL, l, -85f, 5f, 2f, 0f);
            L2Anim.Swing(c, L2Anim.ArmR, l, -85f, 5f, 2f, 0.5f);
        }

        /// <summary>Dano: tranco para trás.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Hurt(AnimationClip c, float l)
        {
            float[] t = T(0f, 0.08f, l);
            L2Anim.HipKeys(c, t, T(Hip, Hip - 0.05f, Hip));
            L2Anim.Keys(c, L2Anim.Body, t, T(0f, -18f, 0f));
            L2Anim.Keys(c, L2Anim.Head, t, T(0f, -20f, 0f), null, T(0f, 10f, 0f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(0f, -35f, 0f), null, T(0f, -25f, 0f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(0f, -35f, 0f), null, T(0f, 25f, 0f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(0f, 12f, 0f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(0f, -8f, 0f));
        }

        /// <summary>Colocando o totem: ajoelha, apoia o totem no chão com a mão direita e levanta.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void PlaceTotem(AnimationClip c, float l)
        {
            float[] t = T(0f, 0.2f, 0.45f, l);
            L2Anim.HipKeys(c, t, T(Hip, Hip - 0.35f, Hip - 0.35f, Hip));
            L2Anim.Keys(c, L2Anim.Body, t, T(0f, 25f, 20f, 0f));
            L2Anim.Keys(c, L2Anim.Head, t, T(0f, 20f, 15f, 0f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(0f, -85f, -85f, 0f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(0f, 40f, 40f, 0f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(-5f, -70f, -50f, -5f), null, T(4f, 10f, 10f, 4f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(-5f, -20f, -20f, -5f), null, T(-4f, -15f, -15f, -4f));
        }
    }
}
