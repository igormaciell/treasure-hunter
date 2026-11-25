using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Os três ogros do Nível 4 (modelos voxel com a hierarquia Body/Head/ArmL/ArmR/LegL/LegR), suas animações
    /// (parado, andando, correndo, atacando, investigando, desconfiado, rugindo e cambaleando), o Animator
    /// Controller, o indicador de alerta e os componentes de IA (OgreAI + VisionSensor + HearingSensor).
    ///   Grukk — pele verde, clava de madeira (o primeiro, ensina as regras).
    ///   Morg  — pele cinza, armadura de ossos e clava de fêmur (ouve melhor, enxerga menos).
    ///   Brann — pele avermelhada, martelo de pedra e um lampião aceso (a luz dele revela John).
    /// </summary>
    public static class L4Ogres
    {
        /// <summary>Altura do quadril dos ogros.</summary>
        public const float Hip = 1.3f;

        public static GameObject Grukk, Morg, Brann;

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        private static float[] T(params float[] v) => v;

        /// <summary>Variações dos ogros.</summary>
        private enum Kind { Grukk, Morg, Brann }

        /// <summary>
        /// Cria o indicador, as animações, o controller e os três ogros.
        /// </summary>
        public static void BuildAll()
        {
            BuildIndicator();
            AnimatorController ctrl = BuildController();
            Grukk = BuildOgre("Ogro_Grukk", "Grukk", "Ogro Grukk (clava)", Kind.Grukk, ctrl);
            Morg = BuildOgre("Ogro_Morg", "Morg", "Ogro Morg (armadura de ossos)", Kind.Morg, ctrl);
            Brann = BuildOgre("Ogro_Brann", "Brann", "Ogro Brann (martelo e lampião)", Kind.Brann, ctrl);
        }

        // ================================================================== Indicador de alerta

        /// <summary>
        /// "?" / "!" e barra de atenção (quads voxel com materiais de partícula, coloridos pelo script).
        /// </summary>
        private static void BuildIndicator()
        {
            GameObject root = new GameObject("Indicador_de_Alerta");
            GameObject q = L2Core.Child(root, "Interrogacao", Vector3.zero, new VoxelModel().AddFitted(Vector3.zero, V(0.62f, 0.62f, 0.01f), L4Palette.AlertQuestion));
            GameObject e = L2Core.Child(root, "Exclamacao", Vector3.zero, new VoxelModel().AddFitted(Vector3.zero, V(0.62f, 0.62f, 0.01f), L4Palette.AlertExclaim));
            GameObject back = L2Core.Child(root, "BarraFundo", V(0, -0.46f, 0.01f), new VoxelModel().Add(Vector3.zero, V(0.96f, 0.13f, 0.01f), L4Palette.AlertBarBack));
            GameObject pivot = new GameObject("BarraPivo");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = V(-0.45f, -0.46f, 0f);
            GameObject fill = L2Core.Child(pivot, "BarraCheia", V(0.45f, 0f, 0f), new VoxelModel().Add(Vector3.zero, V(0.9f, 0.08f, 0.012f), L4Palette.AlertBar));
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            AlertIndicator ai = root.AddComponent<AlertIndicator>();
            ai.question = q.GetComponent<MeshRenderer>();
            ai.exclamation = e.GetComponent<MeshRenderer>();
            ai.barBack = back.GetComponent<MeshRenderer>();
            ai.barFill = fill.GetComponent<MeshRenderer>();
            ai.fillPivot = pivot.transform;
            L2Core.Save(root, AssetCategoryType.Interface, "Indicador de alerta dos ogros (? / !)");
        }

        // ================================================================== Animações

        /// <summary>
        /// Balanço do quadril em torno de uma altura.
        /// </summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        /// <param name="baseY">Altura média.</param>
        /// <param name="amp">Amplitude.</param>
        /// <param name="cycles">Ciclos.</param>
        private static void Bob(AnimationClip c, float l, float baseY, float amp, float cycles)
        {
            const int n = 12;
            float[] t = new float[n + 1], y = new float[n + 1];
            for (int i = 0; i <= n; i++)
            {
                t[i] = l * i / n;
                y[i] = baseY - amp * 0.5f + amp * 0.5f * Mathf.Cos(i / (float)n * cycles * Mathf.PI * 2f);
            }
            L2Anim.HipKeys(c, t, y);
        }

        /// <summary>
        /// Rotação amostrada (x, y e z como funções do tempo normalizado).
        /// </summary>
        /// <param name="c">Clipe.</param>
        /// <param name="path">Parte.</param>
        /// <param name="l">Duração.</param>
        /// <param name="fx">Ângulo X.</param>
        /// <param name="fy">Ângulo Y.</param>
        /// <param name="fz">Ângulo Z.</param>
        private static void Sampled(AnimationClip c, string path, float l, System.Func<float, float> fx, System.Func<float, float> fy, System.Func<float, float> fz)
        {
            const int n = 12;
            float[] t = new float[n + 1], x = new float[n + 1], y = new float[n + 1], z = new float[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float u = i / (float)n;
                t[i] = l * u;
                x[i] = fx(u);
                y[i] = fy != null ? fy(u) : 0f;
                z[i] = fz != null ? fz(u) : 0f;
            }
            L2Anim.Keys(c, path, t, x, y, z);
        }

        /// <summary>Parado: respiração pesada, ombros subindo, braços pendurados.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Idle(AnimationClip c, float l)
        {
            Bob(c, l, Hip, 0.04f, 1f);
            L2Anim.Swing(c, L2Anim.Body, l, 12f, 2.5f, 1f, 0f);
            L2Anim.Keys(c, L2Anim.Head, T(0f, l * 0.3f, l * 0.6f, l), T(-6f, -3f, -8f, -6f), T(0f, 18f, -12f, 0f));
            L2Anim.Swing(c, L2Anim.ArmL, l, -8f, 3f, 1f, 0f, -10f);
            L2Anim.Swing(c, L2Anim.ArmR, l, -10f, 3f, 1f, 0.5f, 10f);
            L2Anim.Hold(c, L2Anim.LegL, l, 0f, 0f, -5f);
            L2Anim.Hold(c, L2Anim.LegR, l, 0f, 0f, 5f);
        }

        /// <summary>Andar/correr pesado: passos largos, corpo balançando de lado.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        /// <param name="legAmp">Amplitude das pernas.</param>
        /// <param name="armAmp">Amplitude dos braços.</param>
        /// <param name="lean">Inclinação.</param>
        /// <param name="bob">Balanço vertical.</param>
        /// <param name="armOffset">Braços à frente.</param>
        private static void Locomotion(AnimationClip c, float l, float legAmp, float armAmp, float lean, float bob, float armOffset)
        {
            Bob(c, l, Hip, bob, 2f);
            Sampled(c, L2Anim.Body, l, u => lean + 2f * Mathf.Sin(u * 4f * Mathf.PI), u => 4f * Mathf.Sin(u * 2f * Mathf.PI), u => 5f * Mathf.Sin(u * 2f * Mathf.PI));
            Sampled(c, L2Anim.Head, l, u => -lean * 0.6f + 3f * Mathf.Sin(u * 4f * Mathf.PI + 1f), u => -4f * Mathf.Sin(u * 2f * Mathf.PI), null);
            L2Anim.Swing(c, L2Anim.LegL, l, 0f, legAmp, 1f, 0f, -4f);
            L2Anim.Swing(c, L2Anim.LegR, l, 0f, legAmp, 1f, 0.5f, 4f);
            L2Anim.Swing(c, L2Anim.ArmL, l, armOffset, armAmp, 1f, 0.5f, -10f);
            L2Anim.Swing(c, L2Anim.ArmR, l, armOffset, armAmp * 0.8f, 1f, 0f, 10f);
        }

        /// <summary>Ataque: ergue a arma com as duas mãos (0,75 s), golpeia o chão em 0,8 s, fica curvado e volta.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Attack(AnimationClip c, float l)
        {
            float[] t = T(0f, 0.35f, 0.72f, 0.82f, 1.05f, 1.45f, l);
            L2Anim.HipKeys(c, t, T(Hip, Hip + 0.03f, Hip + 0.05f, Hip - 0.22f, Hip - 0.2f, Hip - 0.05f, Hip));
            L2Anim.Keys(c, L2Anim.Body, t, T(12f, -5f, -18f, 38f, 35f, 18f, 12f), T(0f, -10f, -12f, 5f, 5f, 0f, 0f));
            L2Anim.Keys(c, L2Anim.Head, t, T(-6f, -15f, -22f, 18f, 15f, 0f, -6f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(-10f, -120f, -175f, -25f, -20f, -15f, -10f), null, T(10f, 0f, -10f, 5f, 8f, 10f, 10f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(-8f, -110f, -165f, -30f, -25f, -15f, -8f), null, T(-10f, 5f, 15f, -5f, -8f, -10f, -10f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(0f, 8f, 12f, -38f, -35f, -10f, 0f), null, T(-5f, -5f, -5f, -8f, -8f, -5f, -5f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(0f, -4f, -6f, 22f, 20f, 6f, 0f), null, T(5f, 5f, 5f, 8f, 8f, 5f, 5f));
        }

        /// <summary>Investigando: curvado, farejando, a cabeça vira de um lado para o outro.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Investigate(AnimationClip c, float l)
        {
            Bob(c, l, Hip - 0.12f, 0.05f, 2f);
            L2Anim.Swing(c, L2Anim.Body, l, 32f, 4f, 2f, 0f);
            L2Anim.Keys(c, L2Anim.Head, T(0f, l * 0.25f, l * 0.5f, l * 0.75f, l), T(-12f, -18f, -8f, -18f, -12f), T(0f, 45f, 0f, -45f, 0f), T(0f, 8f, 0f, -8f, 0f));
            L2Anim.Keys(c, L2Anim.ArmL, T(0f, l * 0.5f, l), T(-30f, -45f, -30f), null, T(-12f, -18f, -12f));
            L2Anim.Swing(c, L2Anim.ArmR, l, -25f, 6f, 1f, 0.3f, 12f);
            L2Anim.Hold(c, L2Anim.LegL, l, -22f, 0f, -8f);
            L2Anim.Hold(c, L2Anim.LegR, l, 14f, 0f, 8f);
        }

        /// <summary>Desconfiado: ereto, cabeça inclinada, braços levemente erguidos.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Suspicious(AnimationClip c, float l)
        {
            Bob(c, l, Hip, 0.03f, 1f);
            L2Anim.Swing(c, L2Anim.Body, l, 4f, 2f, 1f, 0f);
            L2Anim.Keys(c, L2Anim.Head, T(0f, l * 0.5f, l), T(-4f, -10f, -4f), T(0f, 10f, 0f), T(-14f, 14f, -14f));
            L2Anim.Swing(c, L2Anim.ArmL, l, -20f, 4f, 1f, 0f, -16f);
            L2Anim.Swing(c, L2Anim.ArmR, l, -25f, 4f, 1f, 0.5f, 14f);
            L2Anim.Hold(c, L2Anim.LegL, l, -6f, 0f, -6f);
            L2Anim.Hold(c, L2Anim.LegR, l, 6f, 0f, 6f);
        }

        /// <summary>Rugido: estufa o peito, joga a cabeça para trás e abre os braços.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Roar(AnimationClip c, float l)
        {
            float[] t = T(0f, 0.25f, 0.6f, l);
            L2Anim.HipKeys(c, t, T(Hip, Hip + 0.05f, Hip + 0.05f, Hip));
            L2Anim.Keys(c, L2Anim.Body, t, T(12f, -14f, -12f, 12f));
            L2Anim.Keys(c, L2Anim.Head, t, T(-6f, -38f, -30f, -6f), T(0f, 6f, -6f, 0f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(-8f, -55f, -50f, -8f), null, T(-10f, -50f, -45f, -10f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(-10f, -55f, -50f, -10f), null, T(10f, 50f, 45f, 10f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(0f, -10f, -10f, 0f), null, T(-5f, -10f, -10f, -5f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(0f, 8f, 8f, 0f), null, T(5f, 10f, 10f, 5f));
        }

        /// <summary>Cambaleando (atingido por bala): tranco para trás, braço protegendo o rosto.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Stagger(AnimationClip c, float l)
        {
            float[] t = T(0f, 0.15f, 0.5f, 0.85f, l);
            L2Anim.HipKeys(c, t, T(Hip, Hip - 0.1f, Hip - 0.15f, Hip - 0.05f, Hip));
            L2Anim.Keys(c, L2Anim.Body, t, T(12f, -28f, -20f, 5f, 12f), T(0f, 15f, 10f, 0f, 0f));
            L2Anim.Keys(c, L2Anim.Head, t, T(-6f, -30f, -15f, -5f, -6f), null, T(0f, 15f, 10f, 0f, 0f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(-8f, -120f, -130f, -40f, -8f), null, T(-10f, -30f, -30f, -15f, -10f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(-10f, -40f, -30f, -15f, -10f), null, T(10f, 40f, 35f, 15f, 10f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(0f, 20f, 25f, 5f, 0f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(0f, -15f, -10f, 0f, 0f));
        }

        /// <summary>
        /// Animator Controller dos ogros: locomoção (parado/andando/correndo), desconfiado e investigando
        /// (parâmetro Mode) e, de qualquer estado, ataque, rugido e cambaleio (gatilhos).
        /// </summary>
        /// <returns>O controller.</returns>
        private static AnimatorController BuildController()
        {
            AnimationClip idle = L2Anim.Make("Ogro_Parado", 3f, true, Idle);
            AnimationClip walk = L2Anim.Make("Ogro_Andando", 1.5f, true, (c, l) => Locomotion(c, l, 24f, 16f, 14f, 0.07f, -10f));
            AnimationClip run = L2Anim.Make("Ogro_Correndo", 0.85f, true, (c, l) => Locomotion(c, l, 40f, 32f, 28f, 0.12f, -28f));
            AnimationClip attack = L2Anim.Make("Ogro_Atacando", 1.8f, false, Attack);
            AnimationClip search = L2Anim.Make("Ogro_Investigando", 3.2f, true, Investigate);
            AnimationClip suspicious = L2Anim.Make("Ogro_Desconfiado", 1.6f, true, Suspicious);
            AnimationClip roar = L2Anim.Make("Ogro_Rugindo", 1f, false, Roar);
            AnimationClip stagger = L2Anim.Make("Ogro_Cambaleando", 1.2f, false, Stagger);

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(L2Core.AnimationsPath + "/Ogro.controller");
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Mode", AnimatorControllerParameterType.Int);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Roar", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Stagger", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState loco = ctrl.CreateBlendTreeInController("Locomocao", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, 1.4f);
            tree.AddChild(run, 4f);
            sm.defaultState = loco;
            AnimatorState susS = sm.AddState("Desconfiado"); susS.motion = suspicious;
            AnimatorState invS = sm.AddState("Investigando"); invS.motion = search;
            AnimatorState atkS = sm.AddState("Atacando"); atkS.motion = attack;
            AnimatorState roarS = sm.AddState("Rugindo"); roarS.motion = roar;
            AnimatorState stgS = sm.AddState("Cambaleando"); stgS.motion = stagger;

            AnimatorStateTransition t = loco.AddTransition(susS);
            t.AddCondition(AnimatorConditionMode.Equals, 1f, "Mode");
            t.AddCondition(AnimatorConditionMode.Less, 0.3f, "Speed");
            L2Anim.Instant(t, 0.3f);
            t = susS.AddTransition(loco);
            t.AddCondition(AnimatorConditionMode.NotEqual, 1f, "Mode");
            L2Anim.Instant(t, 0.3f);
            t = susS.AddTransition(loco);
            t.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");
            L2Anim.Instant(t, 0.25f);
            t = loco.AddTransition(invS);
            t.AddCondition(AnimatorConditionMode.Equals, 2f, "Mode");
            t.AddCondition(AnimatorConditionMode.Less, 0.3f, "Speed");
            L2Anim.Instant(t, 0.35f);
            t = invS.AddTransition(loco);
            t.AddCondition(AnimatorConditionMode.NotEqual, 2f, "Mode");
            L2Anim.Instant(t, 0.3f);
            t = invS.AddTransition(loco);
            t.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");
            L2Anim.Instant(t, 0.25f);

            foreach (var pair in new[] { (atkS, "Attack", 0.08f), (roarS, "Roar", 0.12f), (stgS, "Stagger", 0.06f) })
            {
                t = sm.AddAnyStateTransition(pair.Item1);
                t.AddCondition(AnimatorConditionMode.If, 0f, pair.Item2);
                t.canTransitionToSelf = false;
                L2Anim.Instant(t, pair.Item3);
                L2Anim.AfterClip(pair.Item1.AddTransition(loco), 0.2f, 0.92f);
            }
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        // ================================================================== Modelos

        /// <summary>
        /// Monta um ogro completo e salva o prefab.
        /// </summary>
        /// <param name="id">Prefab.</param>
        /// <param name="name">Nome do ogro.</param>
        /// <param name="display">Nome legível.</param>
        /// <param name="kind">Variação.</param>
        /// <param name="ctrl">Controller.</param>
        /// <returns>O prefab.</returns>
        private static GameObject BuildOgre(string id, string name, string display, Kind kind, AnimatorController ctrl)
        {
            Material skin = kind == Kind.Grukk ? L4Palette.SkinGreen : kind == Kind.Morg ? L4Palette.SkinGrey : L4Palette.SkinRed;
            Material eyes = kind == Kind.Brann ? L4Palette.EyeRed : L4Palette.EyeYellow;
            int enemyLayer = L2Core.Layer(CombatLayers.EnemyLayerName);

            GameObject root = new GameObject(id);
            root.tag = StealthTags.Ogre;
            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 2.9f;
            capsule.radius = 0.7f;
            capsule.center = V(0f, 1.45f, 0f);
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.65f;
            agent.height = 3f;
            agent.baseOffset = 0f;
            agent.speed = 1.35f;
            agent.angularSpeed = 0f;
            agent.acceleration = 6f;
            agent.stoppingDistance = 0.3f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            agent.avoidancePriority = kind == Kind.Grukk ? 40 : kind == Kind.Morg ? 45 : 50;

            GameObject model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            Animator animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = ctrl;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            GameObject body = Part(id, model, "Body", V(0, Hip, 0), Torso(kind, skin));
            GameObject head = Part(id, body, "Head", V(0, 1.12f, 0.22f), HeadModel(kind, skin, eyes));
            GameObject armL = Part(id, body, "ArmL", V(-0.84f, 0.95f, 0), Arm(skin, kind, true));
            GameObject armR = Part(id, body, "ArmR", V(0.84f, 0.95f, 0), Arm(skin, kind, false));
            Part(id, body, "LegL", V(-0.36f, 0, 0), Leg(skin, kind));
            Part(id, body, "LegR", V(0.36f, 0, 0), Leg(skin, kind));

            // Arma na mão direita (segue a animação).
            GameObject weapon;
            switch (kind)
            {
                case Kind.Grukk:
                    weapon = Part(id, armR, "Clava", V(0, -1.38f, 0.08f), new VoxelModel()
                        .Add(V(0, -0.25f, 0), V(0.16f, 0.6f, 0.16f), L4Palette.ClubWood)
                        .Add(V(0, -0.85f, 0), V(0.42f, 0.8f, 0.42f), L4Palette.ClubWood, V(0, 20, 0))
                        .Add(V(0.12f, -0.95f, 0.1f), V(0.12f, 0.16f, 0.12f), L4Palette.Iron)
                        .Add(V(-0.14f, -0.75f, -0.1f), V(0.1f, 0.14f, 0.1f), L4Palette.Iron));
                    break;
                case Kind.Morg:
                    weapon = Part(id, armR, "Clava_de_Osso", V(0, -1.38f, 0.08f), new VoxelModel()
                        .Add(V(0, -0.55f, 0), V(0.2f, 1.1f, 0.2f), L4Palette.Bone)
                        .Add(V(0, -1.15f, 0), V(0.5f, 0.3f, 0.36f), L4Palette.Bone)
                        .Add(V(0, -0.02f, 0), V(0.34f, 0.2f, 0.26f), L4Palette.BoneDark));
                    break;
                default:
                    weapon = Part(id, armR, "Martelo", V(0, -1.38f, 0.08f), new VoxelModel()
                        .Add(V(0, -0.45f, 0), V(0.14f, 0.95f, 0.14f), L4Palette.WoodDark)
                        .Add(V(0, -1f, 0), V(0.5f, 0.42f, 0.75f), L4Palette.StoneHead, V(0, 0, 4))
                        .Add(V(0, -0.98f, 0), V(0.55f, 0.1f, 0.8f), L4Palette.Rope));
                    // Lampião na mão esquerda: luz que se move e revela quem estiver perto.
                    GameObject lantern = Part(id, armL, "Lampiao", V(0, -1.45f, 0.1f), new VoxelModel()
                        .Add(V(0, -0.1f, 0), V(0.04f, 0.2f, 0.04f), L4Palette.Iron)
                        .Add(V(0, -0.38f, 0), V(0.32f, 0.38f, 0.32f), L4Palette.LanternGlass)
                        .Add(V(0, -0.18f, 0), V(0.38f, 0.06f, 0.38f), L4Palette.Iron)
                        .Add(V(0, -0.58f, 0), V(0.38f, 0.06f, 0.38f), L4Palette.Iron));
                    Light ll = L2Core.PointLight(lantern, "LuzDoLampiao", V(0, -0.38f, 0), new Color(1f, 0.7f, 0.35f), 7f, 1.5f, true);
                    ll.shadows = LightShadows.Soft;
                    break;
            }
            weapon.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
            weapon.tag = CombatTags.Weapon;

            // Olhos (ponto da visão), boca (bafo), cabeça (colisor) e ponto do golpe.
            GameObject eye = new GameObject("Olhos");
            eye.transform.SetParent(head.transform, false);
            eye.transform.localPosition = V(0, 0.38f, 0.3f);
            GameObject mouth = new GameObject("Boca");
            mouth.transform.SetParent(head.transform, false);
            mouth.transform.localPosition = V(0, 0.12f, 0.45f);
            SphereCollider headCol = head.AddComponent<SphereCollider>();
            headCol.center = V(0, 0.3f, 0.05f);
            headCol.radius = 0.38f;
            L2Core.PointLight(head, "BrilhoDosOlhos", V(0, 0.4f, 0.45f), kind == Kind.Brann ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.85f, 0.3f), 1.6f, 0.7f);
            GameObject hit = new GameObject("PontoDoGolpe");
            hit.transform.SetParent(root.transform, false);
            hit.transform.localPosition = V(0, 0.2f, 1.9f);

            // Indicador de alerta.
            GameObject ind = (GameObject)PrefabUtility.InstantiatePrefab(L2Core.Get("Indicador_de_Alerta"));
            ind.transform.SetParent(root.transform, false);
            ind.transform.localPosition = V(0, 3.75f, 0);

            // Sons.
            AudioSource voice = AudioChild(root, "Voz", V(0, 2.6f, 0.3f), 3f, 38f);
            AudioSource feet = AudioChild(root, "Passos", V(0, 0.2f, 0), 2.5f, 30f);
            AudioSource breath = AudioChild(root, "Respiracao", V(0, 2.6f, 0.3f), 1.5f, 13f);
            breath.clip = L2Core.Clip("ogre_breath");
            breath.loop = true;
            breath.playOnAwake = true;
            breath.volume = 0.45f;

            // Sentidos.
            VisionSensor vision = root.AddComponent<VisionSensor>();
            vision.eye = eye.transform;
            HearingSensor hearing = root.AddComponent<HearingSensor>();
            hearing.ear = head.transform;
            OgreAI ai = root.AddComponent<OgreAI>();
            ai.displayName = name;
            switch (kind)
            {
                case Kind.Grukk:
                    vision.range = 13f; vision.fieldOfView = 100f; hearing.hearingMultiplier = 1.15f;
                    ai.patrolSpeed = 1.35f; ai.chaseSpeed = 4f;
                    break;
                case Kind.Morg:
                    vision.range = 11.5f; vision.fieldOfView = 90f; hearing.hearingMultiplier = 1.4f;
                    ai.patrolSpeed = 1.2f; ai.chaseSpeed = 3.7f;
                    break;
                default:
                    vision.range = 14f; vision.fieldOfView = 110f; hearing.hearingMultiplier = 1f;
                    ai.patrolSpeed = 1.4f; ai.chaseSpeed = 4.2f;
                    break;
            }
            ai.vision = vision;
            ai.hearing = hearing;
            ai.animator = animator;
            ai.indicator = ind.GetComponent<AlertIndicator>();
            ai.voice = voice;
            ai.feet = feet;
            ai.breath = breath;
            ai.mouth = mouth.transform;
            ai.attackPoint = hit.transform;
            ai.stepEffects = new[] { L2Core.Get("FX4_Poeira_do_Passo") };
            ai.smashEffects = new[] { L2Core.Get("FX4_Impacto_da_Clava"), L2Core.Get("FX4_Pedrinhas") };
            ai.alertEffects = new[] { L2Core.Get("FX4_Alerta") };
            ai.suspiciousEffects = new[] { L2Core.Get("FX4_Suspeita") };
            ai.hurtEffects = new[] { L2Core.Get("FX4_Poeira_do_Passo"), L2Core.Get("FX4_Faiscas") };
            ai.breathEffects = new[] { L2Core.Get("FX4_Bafo") };
            ai.stepClips = L2Core.Clips("ogre_step_1", "ogre_step_2");
            ai.growlClips = L2Core.Clips("ogre_growl_1", "ogre_growl_2");
            ai.suspiciousClips = L2Core.Clips("ogre_suspicious_1", "ogre_suspicious_2");
            ai.sniffClips = L2Core.Clips("ogre_sniff");
            ai.roarClips = L2Core.Clips("ogre_roar_1", "ogre_roar_2");
            ai.attackClips = L2Core.Clips("ogre_attack");
            ai.smashClips = L2Core.Clips("ogre_smash");
            ai.staggerClips = L2Core.Clips("ogre_stagger");
            ai.frustratedClips = L2Core.Clips("ogre_frustrated");

            L2Core.SetLayerRecursive(root, enemyLayer);
            ind.layer = 2;
            foreach (Transform tr in ind.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = 2;
            return L2Core.Save(root, AssetCategoryType.Inimigos, display);
        }

        /// <summary>
        /// Fonte de som 3D filha.
        /// </summary>
        /// <param name="parent">Pai.</param>
        /// <param name="name">Nome.</param>
        /// <param name="pos">Posição.</param>
        /// <param name="min">Distância mínima.</param>
        /// <param name="max">Distância máxima.</param>
        /// <returns>A fonte.</returns>
        private static AudioSource AudioChild(GameObject parent, string name, Vector3 pos, float min, float max)
        {
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent.transform, false);
            g.transform.localPosition = pos;
            AudioSource s = g.AddComponent<AudioSource>();
            s.spatialBlend = 1f;
            s.minDistance = min;
            s.maxDistance = max;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.playOnAwake = false;
            return s;
        }

        /// <summary>
        /// Parte do corpo com malha própria (pivô na articulação).
        /// </summary>
        /// <param name="id">Prefab.</param>
        /// <param name="parent">Pai.</param>
        /// <param name="name">Nome.</param>
        /// <param name="localPos">Posição.</param>
        /// <param name="model">Modelo.</param>
        /// <returns>A parte.</returns>
        private static GameObject Part(string id, GameObject parent, string name, Vector3 localPos, VoxelModel model)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            if (model != null && !model.IsEmpty) model.ApplyTo(go, L2Core.MeshesPath + "/" + id + "_" + name + ".asset");
            return go;
        }

        /// <summary>Tronco corcunda com barriga, tanga e cinto (e armadura de ossos no Morg, peles no Brann).</summary>
        /// <param name="kind">Variação.</param>
        /// <param name="skin">Pele.</param>
        /// <returns>Modelo.</returns>
        private static VoxelModel Torso(Kind kind, Material skin)
        {
            VoxelModel m = new VoxelModel()
                .Add(V(0, 0.6f, -0.05f), V(1.3f, 1.05f, 0.85f), skin)
                .Add(V(0, 0.4f, 0.28f), V(1.1f, 0.8f, 0.42f), skin)
                .Add(V(0, 1.05f, -0.12f), V(1.55f, 0.38f, 0.8f), skin, V(-8, 0, 0))
                .Add(V(0, 0.05f, 0), V(1.36f, 0.16f, 0.92f), L4Palette.Leather)
                .Add(V(0, -0.2f, 0.42f), V(0.8f, 0.5f, 0.06f), L4Palette.Leather)
                .Add(V(0, -0.2f, -0.42f), V(0.8f, 0.45f, 0.06f), L4Palette.Leather)
                .Add(V(0.35f, 0.06f, 0.47f), V(0.18f, 0.18f, 0.04f), L4Palette.Iron)
                .Add(V(0, 0.75f, 0.5f), V(0.5f, 0.08f, 0.02f), L4Palette.Nail);
            switch (kind)
            {
                case Kind.Morg:
                    m.Add(V(-0.7f, 1.15f, -0.05f), V(0.55f, 0.18f, 0.7f), L4Palette.Bone, V(0, 0, 18))
                     .Add(V(0.7f, 1.15f, -0.05f), V(0.55f, 0.18f, 0.7f), L4Palette.Bone, V(0, 0, -18))
                     .Add(V(0, 0.7f, 0.5f), V(0.9f, 0.12f, 0.05f), L4Palette.BoneDark)
                     .Add(V(0, 0.55f, 0.51f), V(0.8f, 0.1f, 0.05f), L4Palette.Bone)
                     .Add(V(0, 0.4f, 0.5f), V(0.7f, 0.1f, 0.05f), L4Palette.BoneDark)
                     .Add(V(0.2f, 0.95f, 0.45f), V(0.25f, 0.25f, 0.2f), L4Palette.Bone);
                    break;
                case Kind.Brann:
                    m.Add(V(0, 1.12f, -0.2f), V(1.7f, 0.3f, 0.95f), L4Palette.Fur, V(-8, 0, 0))
                     .Add(V(0.3f, 0.6f, 0.47f), V(0.12f, 1.2f, 0.06f), L4Palette.Iron, V(0, 0, 35))
                     .Add(V(0, 0.85f, -0.48f), V(0.9f, 0.7f, 0.12f), L4Palette.FurDark);
                    break;
                default:
                    m.Add(V(-0.25f, 0.8f, 0.48f), V(0.6f, 0.1f, 0.05f), L4Palette.Leather, V(0, 0, 40))
                     .Add(V(0, 0.9f, -0.48f), V(0.5f, 0.5f, 0.05f), L4Palette.Fur);
                    break;
            }
            return m;
        }

        /// <summary>Cabeça: crânio largo, mandíbula com presas, sobrancelha pesada, orelhas e olhos brilhantes.</summary>
        /// <param name="kind">Variação.</param>
        /// <param name="skin">Pele.</param>
        /// <param name="eyes">Olhos.</param>
        /// <returns>Modelo.</returns>
        private static VoxelModel HeadModel(Kind kind, Material skin, Material eyes)
        {
            VoxelModel m = new VoxelModel()
                .Add(V(0, 0.32f, 0), V(0.68f, 0.6f, 0.62f), skin)
                .Add(V(0, 0.08f, 0.16f), V(0.64f, 0.24f, 0.5f), skin)
                .Add(V(0, 0.17f, 0.415f), V(0.48f, 0.06f, 0.02f), L4Palette.Teeth)
                .Add(V(-0.2f, 0.25f, 0.4f), V(0.08f, 0.24f, 0.08f), L4Palette.Tusk, V(-10, 0, 10))
                .Add(V(0.2f, 0.25f, 0.4f), V(0.08f, 0.24f, 0.08f), L4Palette.Tusk, V(-10, 0, -10))
                .Add(V(0, 0.48f, 0.3f), V(0.66f, 0.1f, 0.12f), skin)
                .Add(V(-0.16f, 0.39f, 0.311f), V(0.11f, 0.06f, 0.01f), eyes)
                .Add(V(0.16f, 0.39f, 0.311f), V(0.11f, 0.06f, 0.01f), eyes)
                .Add(V(0, 0.3f, 0.36f), V(0.16f, 0.16f, 0.12f), skin)
                .Add(V(-0.38f, 0.36f, 0), V(0.12f, 0.26f, 0.18f), skin, V(0, 0, 20))
                .Add(V(0.38f, 0.36f, 0), V(0.12f, 0.26f, 0.18f), skin, V(0, 0, -20));
            switch (kind)
            {
                case Kind.Morg:
                    m.Add(V(0, 0.66f, 0), V(0.62f, 0.2f, 0.6f), L4Palette.Bone)
                     .Add(V(0, 0.6f, 0.31f), V(0.5f, 0.12f, 0.02f), L4Palette.BoneDark)
                     .Add(V(-0.3f, 0.78f, 0), V(0.08f, 0.26f, 0.08f), L4Palette.Tusk, V(0, 0, 25))
                     .Add(V(0.3f, 0.78f, 0), V(0.08f, 0.26f, 0.08f), L4Palette.Tusk, V(0, 0, -25));
                    break;
                case Kind.Brann:
                    m.Add(V(0, 0.64f, -0.1f), V(0.5f, 0.12f, 0.4f), L4Palette.FurDark)
                     .Add(V(0.12f, 0.55f, 0.32f), V(0.04f, 0.2f, 0.02f), L4Palette.Embers, V(0, 0, 20));
                    break;
                default:
                    m.Add(V(0, 0.64f, -0.05f), V(0.3f, 0.1f, 0.4f), L4Palette.FurDark);
                    break;
            }
            return m;
        }

        /// <summary>Braço longo e grosso (mãos quase nos joelhos).</summary>
        /// <param name="skin">Pele.</param>
        /// <param name="kind">Variação.</param>
        /// <param name="left">Braço esquerdo.</param>
        /// <returns>Modelo.</returns>
        private static VoxelModel Arm(Material skin, Kind kind, bool left)
        {
            VoxelModel m = new VoxelModel()
                .Add(V(0, -0.4f, 0), V(0.44f, 0.82f, 0.44f), skin)
                .Add(V(0, -1.0f, 0.04f), V(0.52f, 0.56f, 0.52f), skin)
                .Add(V(0, -1.36f, 0.08f), V(0.44f, 0.26f, 0.42f), skin)
                .Add(V(0, -1.5f, 0.2f), V(0.36f, 0.08f, 0.12f), L4Palette.Nail);
            if (kind == Kind.Morg) m.Add(V(0, -0.95f, 0.05f), V(0.56f, 0.18f, 0.56f), L4Palette.Bone);
            if (kind == Kind.Brann && !left) m.Add(V(0, -0.85f, 0.04f), V(0.56f, 0.1f, 0.56f), L4Palette.Iron);
            if (kind == Kind.Grukk) m.Add(V(0, -0.72f, 0.04f), V(0.5f, 0.12f, 0.5f), L4Palette.Leather);
            return m;
        }

        /// <summary>Perna curta e grossa com pé largo.</summary>
        /// <param name="skin">Pele.</param>
        /// <param name="kind">Variação.</param>
        /// <returns>Modelo.</returns>
        private static VoxelModel Leg(Material skin, Kind kind)
        {
            VoxelModel m = new VoxelModel()
                .Add(V(0, -0.38f, 0), V(0.52f, 0.78f, 0.56f), skin)
                .Add(V(0, -0.95f, 0.02f), V(0.46f, 0.46f, 0.5f), skin)
                .Add(V(0, -1.22f, 0.12f), V(0.52f, 0.16f, 0.72f), skin)
                .Add(V(0, -1.24f, 0.46f), V(0.5f, 0.08f, 0.06f), L4Palette.Nail);
            if (kind == Kind.Brann) m.Add(V(0, -0.8f, 0.02f), V(0.56f, 0.3f, 0.6f), L4Palette.Fur);
            if (kind == Kind.Morg) m.Add(V(0, -0.65f, 0.28f), V(0.4f, 0.4f, 0.06f), L4Palette.Bone);
            return m;
        }
    }
}
