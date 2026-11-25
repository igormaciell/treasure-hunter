using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Funções genéricas para criar clipes de animação de "transform" (personagens voxel com a mesma hierarquia
    /// Body/Head/ArmL/ArmR/LegL/LegR). Usadas pelos esqueletos e pela versão de combate de John.
    /// </summary>
    public static class L2Anim
    {
        public const string Body = "Body";
        public const string Head = "Body/Head";
        public const string ArmL = "Body/ArmL";
        public const string ArmR = "Body/ArmR";
        public const string LegL = "Body/LegL";
        public const string LegR = "Body/LegR";

        /// <summary>Altura do quadril (posição local do Body) dos personagens.</summary>
        public const float HipHeight = 0.95f;

        /// <summary>
        /// Cria um clipe, preenche suas curvas e o salva na pasta de animações do Nível 2.
        /// </summary>
        /// <param name="name">Nome do clipe.</param>
        /// <param name="length">Duração (s).</param>
        /// <param name="loop">true para repetir.</param>
        /// <param name="fill">Função que adiciona as curvas.</param>
        /// <returns>O clipe salvo.</returns>
        public static AnimationClip Make(string name, float length, bool loop, Action<AnimationClip, float> fill)
        {
            AnimationClip clip = new AnimationClip { name = name, frameRate = 30f };
            fill(clip, length);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            BuilderCore.SaveAsset(clip, L2Core.AnimationsPath + "/" + name + ".anim");
            return clip;
        }

        /// <summary>
        /// Oscilação senoidal no eixo X de uma parte (Y e Z constantes).
        /// </summary>
        /// <param name="c">Clipe.</param>
        /// <param name="path">Caminho da parte.</param>
        /// <param name="l">Duração.</param>
        /// <param name="offset">Ângulo médio.</param>
        /// <param name="amp">Amplitude.</param>
        /// <param name="cycles">Ciclos por clipe.</param>
        /// <param name="phase">Fase (0 a 1).</param>
        /// <param name="z">Ângulo fixo em Z.</param>
        /// <param name="y">Ângulo fixo em Y.</param>
        public static void Swing(AnimationClip c, string path, float l, float offset, float amp, float cycles, float phase, float z = 0f, float y = 0f)
        {
            AnimationCurve x = new AnimationCurve();
            const int samples = 16;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                x.AddKey(t * l, offset + amp * Mathf.Sin((t * cycles + phase) * Mathf.PI * 2f));
            }
            Smooth(x);
            Euler(c, path, x, AnimationCurve.Constant(0f, l, y), AnimationCurve.Constant(0f, l, z));
        }

        /// <summary>
        /// Pose por quadros-chave (ângulos de Euler em cada instante).
        /// </summary>
        /// <param name="c">Clipe.</param>
        /// <param name="path">Caminho da parte.</param>
        /// <param name="t">Instantes.</param>
        /// <param name="x">Ângulos em X.</param>
        /// <param name="y">Ângulos em Y (nulo = zero).</param>
        /// <param name="z">Ângulos em Z (nulo = zero).</param>
        public static void Keys(AnimationClip c, string path, float[] t, float[] x, float[] y = null, float[] z = null)
        {
            Euler(c, path, Curve(t, x), Curve(t, y), Curve(t, z));
        }

        /// <summary>
        /// Pose constante durante todo o clipe.
        /// </summary>
        /// <param name="c">Clipe.</param>
        /// <param name="path">Caminho.</param>
        /// <param name="l">Duração.</param>
        /// <param name="x">Ângulo X.</param>
        /// <param name="y">Ângulo Y.</param>
        /// <param name="z">Ângulo Z.</param>
        public static void Hold(AnimationClip c, string path, float l, float x, float y = 0f, float z = 0f)
        {
            Euler(c, path, AnimationCurve.Constant(0f, l, x), AnimationCurve.Constant(0f, l, y), AnimationCurve.Constant(0f, l, z));
        }

        /// <summary>
        /// Balanço vertical do quadril (Body).
        /// </summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        /// <param name="bob">Amplitude.</param>
        /// <param name="cycles">Ciclos por clipe.</param>
        public static void HipBob(AnimationClip c, float l, float bob, float cycles)
        {
            AnimationCurve y = new AnimationCurve();
            const int samples = 16;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                y.AddKey(t * l, HipHeight - bob * 0.5f + bob * 0.5f * Mathf.Cos(t * cycles * Mathf.PI * 2f));
            }
            Smooth(y);
            HipCurve(c, y);
        }

        /// <summary>
        /// Altura do quadril por quadros-chave (levantar do chão, desmoronar).
        /// </summary>
        /// <param name="c">Clipe.</param>
        /// <param name="t">Instantes.</param>
        /// <param name="y">Alturas.</param>
        public static void HipKeys(AnimationClip c, float[] t, float[] y)
        {
            HipCurve(c, Curve(t, y));
        }

        /// <summary>
        /// Grava a curva de altura do Body (X e Z ficam em zero).
        /// </summary>
        /// <param name="c">Clipe.</param>
        /// <param name="y">Curva da altura.</param>
        private static void HipCurve(AnimationClip c, AnimationCurve y)
        {
            float l = y.length > 0 ? y.keys[y.length - 1].time : 1f;
            AnimationUtility.SetEditorCurve(c, EditorCurveBinding.FloatCurve(Body, typeof(Transform), "m_LocalPosition.x"), AnimationCurve.Constant(0f, l, 0f));
            AnimationUtility.SetEditorCurve(c, EditorCurveBinding.FloatCurve(Body, typeof(Transform), "m_LocalPosition.y"), y);
            AnimationUtility.SetEditorCurve(c, EditorCurveBinding.FloatCurve(Body, typeof(Transform), "m_LocalPosition.z"), AnimationCurve.Constant(0f, l, 0f));
        }

        /// <summary>
        /// Monta uma curva suave a partir de instantes e valores (nulo = zero constante).
        /// </summary>
        /// <param name="t">Instantes.</param>
        /// <param name="v">Valores.</param>
        /// <returns>A curva.</returns>
        private static AnimationCurve Curve(float[] t, float[] v)
        {
            AnimationCurve curve = new AnimationCurve();
            for (int i = 0; i < t.Length; i++) curve.AddKey(t[i], v != null ? v[Mathf.Min(i, v.Length - 1)] : 0f);
            Smooth(curve);
            return curve;
        }

        /// <summary>
        /// Suaviza as tangentes de todas as chaves (movimento sem "degraus").
        /// </summary>
        /// <param name="curve">Curva.</param>
        private static void Smooth(AnimationCurve curve)
        {
            for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
        }

        /// <summary>
        /// Grava as três curvas de rotação (Euler) de uma parte.
        /// </summary>
        /// <param name="c">Clipe.</param>
        /// <param name="path">Caminho.</param>
        /// <param name="x">Curva X.</param>
        /// <param name="y">Curva Y.</param>
        /// <param name="z">Curva Z.</param>
        private static void Euler(AnimationClip c, string path, AnimationCurve x, AnimationCurve y, AnimationCurve z)
        {
            AnimationUtility.SetEditorCurve(c, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.x"), x);
            AnimationUtility.SetEditorCurve(c, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.y"), y);
            AnimationUtility.SetEditorCurve(c, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.z"), z);
        }

        /// <summary>
        /// Transição sem tempo de saída (resposta imediata a um parâmetro).
        /// </summary>
        /// <param name="t">Transição.</param>
        /// <param name="duration">Duração da mistura.</param>
        public static void Instant(AnimatorStateTransition t, float duration)
        {
            t.hasExitTime = false;
            t.duration = duration;
            t.hasFixedDuration = true;
        }

        /// <summary>
        /// Transição que espera o clipe terminar.
        /// </summary>
        /// <param name="t">Transição.</param>
        /// <param name="duration">Duração da mistura.</param>
        /// <param name="exitTime">Momento normalizado da saída.</param>
        public static void AfterClip(AnimatorStateTransition t, float duration, float exitTime = 0.95f)
        {
            t.hasExitTime = true;
            t.exitTime = exitTime;
            t.duration = duration;
            t.hasFixedDuration = true;
        }

        /// <summary>
        /// Monta e salva uma AvatarMask que libera só os caminhos informados (e seus filhos).
        /// </summary>
        /// <param name="name">Nome do arquivo.</param>
        /// <param name="root">Raiz do Animator (para listar os caminhos).</param>
        /// <param name="activePrefixes">Caminhos liberados.</param>
        /// <returns>A máscara.</returns>
        public static AvatarMask Mask(string name, Transform root, params string[] activePrefixes)
        {
            AvatarMask mask = new AvatarMask { name = name };
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            mask.transformCount = all.Length;
            for (int i = 0; i < all.Length; i++)
            {
                string path = AnimationUtility.CalculateTransformPath(all[i], root);
                mask.SetTransformPath(i, path);
                bool active = false;
                foreach (string p in activePrefixes)
                    if (path == p || path.StartsWith(p + "/", StringComparison.Ordinal)) active = true;
                mask.SetTransformActive(i, active);
            }
            BuilderCore.SaveAsset(mask, L2Core.AnimationsPath + "/" + name + ".mask");
            return mask;
        }
    }

    /// <summary>
    /// Personagens do Nível 2: os esqueletos (faca, arco e machado, duas variações cada), com modelos voxel,
    /// animações (surgir, parado, andando, correndo, ataque, dano e morte), Animator Controllers, NavMeshAgent,
    /// colisores (corpo + cabeça para tiro crítico) e sons; e a versão de combate de John Craft (revólver, faca,
    /// lanterna no cinto e camada de animação dos braços).
    /// </summary>
    public static class L2Characters
    {
        /// <summary>Prefab do jogador de combate.</summary>
        public static GameObject PlayerPrefab;

        private enum Kind { Faca, Arco, Machado }

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        private static float[] T(params float[] v) => v;

        /// <summary>
        /// Cria os inimigos e o jogador de combate.
        /// </summary>
        public static void BuildAll()
        {
            BuildEnemies();
            BuildPlayer();
        }

        // ================================================================== Esqueletos

        /// <summary>
        /// Clipes compartilhados, controllers por tipo de arma e os seis prefabs de esqueleto.
        /// </summary>
        private static void BuildEnemies()
        {
            AnimationClip rise = L2Anim.Make("Esqueleto_Levantando", 1.3f, false, Rise);
            AnimationClip idle = L2Anim.Make("Esqueleto_Parado", 2.4f, true, SkeletonIdle);
            AnimationClip walk = L2Anim.Make("Esqueleto_Andando", 1.1f, true, (c, l) => SkeletonLocomotion(c, l, 28f, 22f, 0.035f, 6f, 0f));
            AnimationClip run = L2Anim.Make("Esqueleto_Correndo", 0.62f, true, (c, l) => SkeletonLocomotion(c, l, 50f, 40f, 0.07f, 16f, -25f));
            AnimationClip hurt = L2Anim.Make("Esqueleto_Dano", 0.45f, false, Hurt);
            AnimationClip death = L2Anim.Make("Esqueleto_Morte", 1.2f, false, Death);
            AnimationClip stab = L2Anim.Make("Esqueleto_Ataque_Faca", 1.0f, false, KnifeAttack);
            AnimationClip shoot = L2Anim.Make("Esqueleto_Disparo_Arco", 1.15f, false, BowShot);
            AnimationClip chop = L2Anim.Make("Esqueleto_Ataque_Machado", 1.6f, false, AxeAttack);
            AnimationClip bruteWalk = L2Anim.Make("Brutamontes_Andando", 1.4f, true, (c, l) => SkeletonLocomotion(c, l, 22f, 14f, 0.06f, 8f, -10f));
            AnimationClip bruteRun = L2Anim.Make("Brutamontes_Correndo", 0.8f, true, (c, l) => SkeletonLocomotion(c, l, 40f, 26f, 0.1f, 18f, -30f));

            AnimatorController knifeCtrl = EnemyController("Esqueleto_Faca", rise, idle, walk, run, stab, hurt, death, 1.3f, 3.4f);
            AnimatorController bowCtrl = EnemyController("Esqueleto_Arqueiro", rise, idle, walk, run, shoot, hurt, death, 1.4f, 3.6f);
            AnimatorController axeCtrl = EnemyController("Esqueleto_Brutamontes", rise, idle, bruteWalk, bruteRun, chop, hurt, death, 1.0f, 2.6f);

            Skeleton("Esqueleto_Faca", "Esqueleto com faca", Kind.Faca, 0, knifeCtrl);
            Skeleton("Esqueleto_Faca_Elmo", "Esqueleto com faca (elmo)", Kind.Faca, 1, knifeCtrl);
            Skeleton("Esqueleto_Arqueiro", "Esqueleto arqueiro", Kind.Arco, 0, bowCtrl);
            Skeleton("Esqueleto_Arqueiro_Capuz", "Esqueleto arqueiro (capuz)", Kind.Arco, 1, bowCtrl);
            Skeleton("Esqueleto_Brutamontes", "Brutamontes de ossos", Kind.Machado, 0, axeCtrl);
            Skeleton("Esqueleto_Brutamontes_Elmo", "Brutamontes de ossos (elmo)", Kind.Machado, 1, axeCtrl);
        }

        /// <summary>
        /// Monta um esqueleto completo e salva o prefab.
        /// </summary>
        /// <param name="id">Identificador do prefab.</param>
        /// <param name="display">Nome legível.</param>
        /// <param name="kind">Arma (define o script de IA).</param>
        /// <param name="variant">Variação visual (0 ou 1).</param>
        /// <param name="controller">Animator Controller.</param>
        private static void Skeleton(string id, string display, Kind kind, int variant, AnimatorController controller)
        {
            bool brute = kind == Kind.Machado;
            float scale = brute ? 1.3f : 1f;
            int enemyLayer = L2Core.Layer(CombatLayers.EnemyLayerName);

            GameObject root = new GameObject(id);
            root.tag = CombatTags.Enemy;

            // Corpo físico: cápsula + Rigidbody cinemático (o NavMeshAgent move o objeto).
            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = brute ? 2.1f : 1.6f;
            capsule.radius = brute ? 0.5f : 0.33f;
            capsule.center = V(0f, capsule.height * 0.5f, 0f);
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.radius = brute ? 0.5f : 0.38f;
            agent.height = brute ? 2.3f : 1.85f;
            agent.baseOffset = 0f;
            agent.speed = brute ? 1.0f : 1.3f;
            agent.angularSpeed = 0f;
            agent.acceleration = brute ? 8f : 12f;
            agent.stoppingDistance = kind == Kind.Faca ? 0.9f : brute ? 1.4f : 0.3f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            agent.avoidancePriority = kind == Kind.Arco ? 40 : brute ? 30 : 50;

            // Modelo animado.
            GameObject model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            model.transform.localScale = Vector3.one * scale;
            Animator animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            Material eyes = kind == Kind.Faca ? L2Palette.EyeRed : kind == Kind.Arco ? L2Palette.EyeGreen : L2Palette.EyeOrange;
            GameObject body = Part(id, model, "Body", V(0, L2Anim.HipHeight, 0), SkeletonBody(kind, variant));
            GameObject head = Part(id, body, "Head", V(0, 0.62f, 0), SkeletonHead(kind, variant, eyes));
            GameObject armL = Part(id, body, "ArmL", V(-0.32f, 0.56f, 0), SkeletonArm(brute));
            GameObject armR = Part(id, body, "ArmR", V(0.32f, 0.56f, 0), SkeletonArm(brute));
            Part(id, body, "LegL", V(-0.12f, 0, 0), SkeletonLeg());
            Part(id, body, "LegR", V(0.12f, 0, 0), SkeletonLeg());

            // Arma na mão (filha do braço, acompanha a animação).
            GameObject weapon;
            switch (kind)
            {
                case Kind.Faca:
                    weapon = Part(id, armR, "Faca", V(0, -0.62f, 0.03f), L2Combat.RustyKnife());
                    break;
                case Kind.Arco:
                    weapon = Part(id, armL, "Arco", V(0, -0.62f, 0.02f), L2Combat.Bow());
                    GameObject quiver = Part(id, body, "Aljava", V(0.1f, 0.4f, -0.2f), L2Combat.Quiver());
                    quiver.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
                    break;
                default:
                    weapon = Part(id, armR, "Machado", V(0, -0.62f, 0.03f), L2Combat.Axe());
                    weapon.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
                    break;
            }
            weapon.tag = CombatTags.Weapon;

            // Cabeça: colisor próprio com multiplicador de dano (tiro na cabeça = dobro).
            SphereCollider headCol = head.AddComponent<SphereCollider>();
            headCol.center = V(0, 0.2f, 0.01f);
            headCol.radius = 0.22f;
            Hitbox hb = head.AddComponent<Hitbox>();
            hb.multiplier = 2f;
            hb.isHead = true;

            // Luz fraca dos olhos (realça a silhueta no escuro).
            Color eyeColor = kind == Kind.Faca ? new Color(1f, 0.25f, 0.15f) : kind == Kind.Arco ? new Color(0.35f, 1f, 0.45f) : new Color(1f, 0.55f, 0.1f);
            L2Core.PointLight(head, "BrilhoDosOlhos", V(0, 0.24f, 0.3f), eyeColor, brute ? 2.4f : 1.6f, 0.9f);

            // Inteligência artificial.
            EnemyBase enemy;
            if (kind == Kind.Arco)
            {
                RangedEnemy r = root.AddComponent<RangedEnemy>();
                r.displayName = "Esqueleto arqueiro";
                r.maxHealth = 2f;
                r.walkSpeed = 1.4f;
                r.runSpeed = 3.6f;
                r.sightRange = 26f;
                r.arrowPrefabs = new[] { L2Core.Get("Flecha") };
                GameObject spawn = new GameObject("SaidaDaFlecha");
                spawn.transform.SetParent(root.transform, false);
                spawn.transform.localPosition = V(0f, 1.45f, 0.55f);
                r.arrowSpawn = spawn.transform;
                r.arrowSpeed = 22f;
                r.arrowDamage = 1f;
                r.preferredMin = 7f;
                r.preferredMax = 15f;
                enemy = r;
            }
            else
            {
                MeleeEnemy m = root.AddComponent<MeleeEnemy>();
                if (brute)
                {
                    m.displayName = "Brutamontes de ossos";
                    m.maxHealth = 6f;
                    m.walkSpeed = 1.0f;
                    m.runSpeed = 2.6f;
                    m.turnSpeed = 260f;
                    m.bulletStun = 0f;
                    m.meleeStunMultiplier = 0.5f;
                    m.knockbackScale = 0.3f;
                    m.eyeHeight = 2.0f;
                    m.attackRange = 2.1f;
                    m.attackWindup = 0.85f;
                    m.attackRecover = 0.8f;
                    m.attackCooldown = 1.4f;
                    m.damage = 2;
                    m.hitRadius = 1.4f;
                    m.hitForce = 10f;
                    m.windupTurnSpeed = 120f;
                    m.swingClip = L2Core.Clip("axe_swing");
                    m.stepInterval = 0.7f;
                }
                else
                {
                    m.displayName = "Esqueleto com faca";
                    m.maxHealth = 3f;
                    m.walkSpeed = 1.3f;
                    m.runSpeed = 3.4f;
                    m.attackRange = 1.6f;
                    m.attackWindup = 0.45f;
                    m.attackRecover = 0.55f;
                    m.attackCooldown = 1.0f;
                    m.damage = 1;
                    m.swingClip = L2Core.Clip("swing_whoosh");

                    // 35% de chance de deixar a faca coletável ao morrer.
                    EnemyLoot loot = root.AddComponent<EnemyLoot>();
                    loot.dropChance = 0.35f;
                    loot.dropPrefabs = new[] { L2Core.Get("Faca_Coletavel") };
                }
                enemy = m;
            }

            enemy.animator = animator;
            enemy.hitColliders = new Collider[] { capsule, headCol };
            enemy.loot = root.GetComponent<EnemyLoot>();
            enemy.hurtEffects = new[] { L2Core.Get("FX2_Lascas_de_Osso") };
            enemy.deathEffects = new[] { L2Core.Get("FX2_Poeira_de_Osso"), L2Core.Get("FX2_Lascas_de_Osso") };
            enemy.spawnEffect = L2Core.Get("FX2_Surgimento");
            enemy.alertClips = brute ? L2Core.Clips("brute_roar", "skeleton_alert_2") : L2Core.Clips("skeleton_alert_1", "skeleton_alert_2");
            enemy.attackClips = L2Core.Clips("skeleton_attack_1", "skeleton_attack_2");
            enemy.hurtClips = L2Core.Clips("skeleton_hurt_1", "skeleton_hurt_2");
            enemy.deathClips = L2Core.Clips("skeleton_death_1", "skeleton_death_2");
            enemy.stepClips = L2Core.Clips("bone_step_1", "bone_step_2");

            L2Core.SetLayerRecursive(root, enemyLayer);
            L2Core.Save(root, AssetCategoryType.Inimigos, display);
        }

        /// <summary>
        /// Cria uma parte do corpo (pivô na articulação) com malha própria.
        /// </summary>
        /// <param name="id">Prefab (prefixo do arquivo da malha).</param>
        /// <param name="parent">Pai.</param>
        /// <param name="name">Nome (usado nos caminhos da animação).</param>
        /// <param name="localPos">Posição da articulação.</param>
        /// <param name="model">Modelo voxel.</param>
        /// <returns>A parte.</returns>
        private static GameObject Part(string id, GameObject parent, string name, Vector3 localPos, VoxelModel model)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.layer = parent.layer;
            if (model != null && !model.IsEmpty) model.ApplyTo(go, L2Core.MeshesPath + "/" + id + "_" + name + ".asset");
            return go;
        }

        /// <summary>Tronco do esqueleto: bacia, coluna, costelas e trapos/armadura conforme o tipo.</summary>
        /// <param name="kind">Tipo.</param>
        /// <param name="variant">Variação.</param>
        /// <returns>Modelo.</returns>
        private static VoxelModel SkeletonBody(Kind kind, int variant)
        {
            VoxelModel m = new VoxelModel()
                .Add(V(0, 0.02f, 0), V(0.4f, 0.12f, 0.2f), L2Palette.Bone)
                .Add(V(0, 0.2f, -0.04f), V(0.08f, 0.3f, 0.08f), L2Palette.BoneDark)
                .Add(V(0, 0.42f, 0), V(0.44f, 0.3f, 0.26f), L2Palette.Bone)
                .Add(V(0, 0.36f, 0.131f), V(0.36f, 0.03f, 0.01f), L2Palette.Void)
                .Add(V(0, 0.44f, 0.131f), V(0.36f, 0.03f, 0.01f), L2Palette.Void)
                .Add(V(0, 0.52f, 0.131f), V(0.32f, 0.03f, 0.01f), L2Palette.Void)
                .Add(V(0, 0.42f, 0.136f), V(0.06f, 0.26f, 0.01f), L2Palette.BoneDark)
                .Add(V(0, 0.57f, 0), V(0.6f, 0.06f, 0.14f), L2Palette.Bone)
                .Add(V(0, 0.07f, 0), V(0.42f, 0.06f, 0.22f), L2Palette.Leather);
            switch (kind)
            {
                case Kind.Faca:
                    m.Add(V(0, -0.1f, 0.11f), V(0.28f, 0.26f, 0.02f), variant == 0 ? L2Palette.ClothRed : L2Palette.Cloth)
                     .Add(V(0.05f, 0.4f, 0.14f), V(0.5f, 0.08f, 0.02f), L2Palette.ClothRed, V(0, 0, 35));
                    if (variant == 1) m.Add(V(0.26f, 0.58f, 0), V(0.18f, 0.08f, 0.2f), L2Palette.Iron);
                    break;
                case Kind.Arco:
                    m.Add(V(0, -0.1f, 0.11f), V(0.28f, 0.26f, 0.02f), L2Palette.ClothGreen)
                     .Add(V(0, 0.3f, -0.15f), V(0.46f, 0.6f, 0.03f), variant == 0 ? L2Palette.ClothGreen : L2Palette.Cloth)
                     .Add(V(-0.1f, 0.42f, 0.14f), V(0.06f, 0.5f, 0.02f), L2Palette.Leather, V(0, 0, -30));
                    break;
                default:
                    m.Add(V(0, 0.42f, 0.15f), V(0.5f, 0.32f, 0.04f), L2Palette.Iron)
                     .Add(V(0, 0.42f, -0.15f), V(0.5f, 0.32f, 0.04f), L2Palette.IronDark)
                     .Add(V(-0.32f, 0.6f, 0), V(0.22f, 0.1f, 0.28f), L2Palette.Iron)
                     .Add(V(0.32f, 0.6f, 0), V(0.22f, 0.1f, 0.28f), L2Palette.Iron)
                     .Add(V(0, -0.1f, 0.12f), V(0.36f, 0.3f, 0.03f), L2Palette.Leather);
                    if (variant == 1)
                        m.Add(V(-0.36f, 0.68f, 0), V(0.05f, 0.14f, 0.05f), L2Palette.Blade)
                         .Add(V(0.36f, 0.68f, 0), V(0.05f, 0.14f, 0.05f), L2Palette.Blade);
                    break;
            }
            return m;
        }

        /// <summary>Crânio com órbitas, olhos brilhantes e acessório conforme o tipo.</summary>
        /// <param name="kind">Tipo.</param>
        /// <param name="variant">Variação.</param>
        /// <param name="eyes">Material dos olhos.</param>
        /// <returns>Modelo.</returns>
        private static VoxelModel SkeletonHead(Kind kind, int variant, Material eyes)
        {
            VoxelModel m = new VoxelModel()
                .Add(V(0, 0.04f, 0), V(0.07f, 0.1f, 0.07f), L2Palette.BoneDark)
                .Add(V(0, 0.24f, 0), V(0.34f, 0.3f, 0.34f), L2Palette.Bone)
                .Add(V(0, 0.08f, 0.04f), V(0.26f, 0.08f, 0.26f), L2Palette.Bone)
                .Add(V(-0.08f, 0.24f, 0.171f), V(0.1f, 0.09f, 0.01f), L2Palette.Void)
                .Add(V(0.08f, 0.24f, 0.171f), V(0.1f, 0.09f, 0.01f), L2Palette.Void)
                .Add(V(-0.08f, 0.24f, 0.176f), V(0.04f, 0.04f, 0.01f), eyes)
                .Add(V(0.08f, 0.24f, 0.176f), V(0.04f, 0.04f, 0.01f), eyes)
                .Add(V(0, 0.15f, 0.171f), V(0.04f, 0.05f, 0.01f), L2Palette.Void)
                .Add(V(0, 0.09f, 0.171f), V(0.2f, 0.02f, 0.01f), L2Palette.BoneDark);
            switch (kind)
            {
                case Kind.Faca:
                    if (variant == 1)
                        m.Add(V(0, 0.38f, 0), V(0.38f, 0.1f, 0.38f), L2Palette.Iron)
                         .Add(V(0, 0.33f, 0), V(0.4f, 0.04f, 0.4f), L2Palette.IronDark)
                         .Add(V(0, 0.28f, 0.19f), V(0.04f, 0.16f, 0.02f), L2Palette.Iron);
                    break;
                case Kind.Arco:
                    if (variant == 0)
                        m.Add(V(0, 0.36f, -0.03f), V(0.4f, 0.16f, 0.4f), L2Palette.ClothGreen)
                         .Add(V(0, 0.22f, -0.17f), V(0.4f, 0.3f, 0.08f), L2Palette.ClothGreen)
                         .Add(V(-0.19f, 0.24f, 0), V(0.04f, 0.26f, 0.32f), L2Palette.ClothGreen)
                         .Add(V(0.19f, 0.24f, 0), V(0.04f, 0.26f, 0.32f), L2Palette.ClothGreen);
                    else
                        m.Add(V(0, 0.4f, 0), V(0.36f, 0.08f, 0.36f), L2Palette.Leather)
                         .Add(V(0.12f, 0.5f, -0.05f), V(0.03f, 0.2f, 0.06f), L2Palette.FeatherRed, V(0, 0, -20));
                    break;
                default:
                    m.Add(V(0, 0.4f, 0), V(0.4f, 0.14f, 0.4f), variant == 0 ? L2Palette.Iron : L2Palette.IronDark)
                     .Add(V(0, 0.3f, 0.19f), V(0.32f, 0.05f, 0.03f), L2Palette.IronDark);
                    if (variant == 0)
                        m.Add(V(-0.26f, 0.5f, 0), V(0.14f, 0.06f, 0.06f), L2Palette.Bone, V(0, 0, 30))
                         .Add(V(0.26f, 0.5f, 0), V(0.14f, 0.06f, 0.06f), L2Palette.Bone, V(0, 0, -30))
                         .Add(V(-0.32f, 0.6f, 0), V(0.05f, 0.14f, 0.05f), L2Palette.Bone)
                         .Add(V(0.32f, 0.6f, 0), V(0.05f, 0.14f, 0.05f), L2Palette.Bone);
                    else
                        m.Add(V(0, 0.24f, 0.19f), V(0.36f, 0.08f, 0.02f), L2Palette.IronDark)
                         .Add(V(0, 0.52f, 0), V(0.06f, 0.12f, 0.3f), L2Palette.ClothRed);
                    break;
            }
            return m;
        }

        /// <summary>Braço de ossos (úmero, cotovelo, antebraço e mão).</summary>
        /// <param name="thick">true para o brutamontes.</param>
        /// <returns>Modelo.</returns>
        private static VoxelModel SkeletonArm(bool thick)
        {
            float k = thick ? 1.4f : 1f;
            return new VoxelModel()
                .Add(V(0, -0.16f, 0), V(0.07f * k, 0.3f, 0.07f * k), L2Palette.Bone)
                .Add(V(0, -0.31f, 0), V(0.09f * k, 0.06f, 0.09f * k), L2Palette.BoneDark)
                .Add(V(0, -0.45f, 0), V(0.06f * k, 0.26f, 0.06f * k), L2Palette.Bone)
                .Add(V(0, -0.6f, 0.01f), V(0.09f * k, 0.08f, 0.07f * k), L2Palette.BoneDark);
        }

        /// <summary>Perna de ossos (fêmur, joelho, canela e pé).</summary>
        /// <returns>Modelo.</returns>
        private static VoxelModel SkeletonLeg()
        {
            return new VoxelModel()
                .Add(V(0, -0.22f, 0), V(0.08f, 0.4f, 0.08f), L2Palette.Bone)
                .Add(V(0, -0.44f, 0.01f), V(0.1f, 0.07f, 0.1f), L2Palette.BoneDark)
                .Add(V(0, -0.66f, 0), V(0.07f, 0.38f, 0.07f), L2Palette.Bone)
                .Add(V(0, -0.91f, 0.05f), V(0.12f, 0.06f, 0.2f), L2Palette.BoneDark);
        }

        // ------------------------------------------------------------------ Animações dos esqueletos

        /// <summary>Surgir do chão: o corpo sobe de baixo do piso com os braços se arrastando.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Rise(AnimationClip c, float l)
        {
            float[] t = T(0f, 0.5f, 0.9f, l);
            L2Anim.HipKeys(c, t, T(-0.75f, 0.1f, 0.95f, 0.95f));
            L2Anim.Keys(c, L2Anim.Body, t, T(40f, 30f, 12f, 0f));
            L2Anim.Keys(c, L2Anim.Head, t, T(-25f, -15f, 10f, 0f), T(0f, 20f, -15f, 0f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(-165f, -130f, -40f, 0f), null, T(-10f, -15f, -10f, -4f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(-150f, -150f, -60f, 0f), null, T(10f, 15f, 10f, 4f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(-30f, -20f, -5f, 0f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(-10f, -25f, 5f, 0f));
        }

        /// <summary>Parado: balanço lento, cabeça olhando ao redor, mandíbula de lado.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void SkeletonIdle(AnimationClip c, float l)
        {
            L2Anim.HipBob(c, l, 0.03f, 1f);
            L2Anim.Swing(c, L2Anim.Body, l, 6f, 2f, 1f, 0f);
            L2Anim.Keys(c, L2Anim.Head, T(0f, l * 0.25f, l * 0.5f, l * 0.75f, l), T(-4f, -2f, -4f, -6f, -4f), T(0f, 22f, 0f, -22f, 0f), T(6f, 4f, 6f, 8f, 6f));
            L2Anim.Swing(c, L2Anim.ArmL, l, -6f, 4f, 1f, 0f, -6f);
            L2Anim.Swing(c, L2Anim.ArmR, l, -10f, 4f, 1f, 0.5f, 6f);
            L2Anim.Hold(c, L2Anim.LegL, l, 0f, 0f, -3f);
            L2Anim.Hold(c, L2Anim.LegR, l, 0f, 0f, 3f);
        }

        /// <summary>Andar/correr: pernas e braços alternados, corpo inclinado e ombros tortos.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        /// <param name="legAmp">Amplitude das pernas.</param>
        /// <param name="armAmp">Amplitude dos braços.</param>
        /// <param name="bob">Balanço vertical.</param>
        /// <param name="lean">Inclinação.</param>
        /// <param name="armOffset">Braços estendidos para a frente (ângulo médio).</param>
        private static void SkeletonLocomotion(AnimationClip c, float l, float legAmp, float armAmp, float bob, float lean, float armOffset)
        {
            L2Anim.HipBob(c, l, bob, 2f);
            L2Anim.Swing(c, L2Anim.Body, l, lean, 3f, 2f, 0f, 3f);
            L2Anim.Swing(c, L2Anim.Head, l, -lean * 0.5f, 4f, 2f, 0.4f, -5f);
            L2Anim.Swing(c, L2Anim.LegL, l, 0f, legAmp, 1f, 0f);
            L2Anim.Swing(c, L2Anim.LegR, l, 0f, legAmp, 1f, 0.5f);
            L2Anim.Swing(c, L2Anim.ArmL, l, armOffset, armAmp, 1f, 0.5f, -6f);
            L2Anim.Swing(c, L2Anim.ArmR, l, armOffset, armAmp, 1f, 0f, 6f);
        }

        /// <summary>Dano: tranco para trás com os braços abertos.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Hurt(AnimationClip c, float l)
        {
            float[] t = T(0f, 0.1f, l);
            L2Anim.HipKeys(c, t, T(0.95f, 0.9f, 0.95f));
            L2Anim.Keys(c, L2Anim.Body, t, T(0f, -22f, 0f), T(0f, 10f, 0f));
            L2Anim.Keys(c, L2Anim.Head, t, T(0f, -30f, 0f), null, T(0f, 15f, 0f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(0f, -45f, 0f), null, T(0f, -35f, 0f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(0f, -45f, 0f), null, T(0f, 35f, 0f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(0f, 10f, 0f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(0f, -10f, 0f));
        }

        /// <summary>Morte: os ossos desmoronam até o esqueleto ficar sentado e tombado.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Death(AnimationClip c, float l)
        {
            float[] t = T(0f, 0.35f, 0.8f, l);
            L2Anim.HipKeys(c, t, T(0.95f, 0.75f, 0.3f, 0.2f));
            L2Anim.Keys(c, L2Anim.Body, t, T(0f, -12f, 30f, 48f), null, T(0f, 5f, 12f, 18f));
            L2Anim.Keys(c, L2Anim.Head, t, T(0f, -25f, 30f, 55f), null, T(0f, 0f, 15f, 25f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(0f, -60f, 10f, 25f), null, T(0f, -20f, -30f, -35f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(0f, -60f, 10f, 25f), null, T(0f, 20f, 30f, 35f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(0f, -20f, -80f, -88f), null, T(0f, -5f, -15f, -20f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(0f, -10f, -75f, -85f), null, T(0f, 5f, 15f, 20f));
        }

        /// <summary>Ataque com faca: ergue o braço (preparação de 0,45 s), golpeia de cima para baixo e recua.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void KnifeAttack(AnimationClip c, float l)
        {
            float[] t = T(0f, 0.35f, 0.45f, 0.58f, 0.8f, l);
            L2Anim.HipKeys(c, t, T(0.95f, 0.95f, 0.93f, 0.9f, 0.93f, 0.95f));
            L2Anim.Keys(c, L2Anim.Body, t, T(0f, -10f, -10f, 20f, 12f, 0f), T(0f, -20f, -20f, 15f, 5f, 0f));
            L2Anim.Keys(c, L2Anim.Head, t, T(0f, -5f, -5f, 10f, 5f, 0f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(-10f, -165f, -170f, -35f, -25f, 0f), null, T(5f, 20f, 20f, 5f, 5f, 4f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(0f, -35f, -35f, 25f, 10f, 0f), null, T(-5f, -20f, -20f, -10f, -5f, -4f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(0f, 10f, 10f, -30f, -15f, 0f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(0f, -5f, -5f, 15f, 8f, 0f));
        }

        /// <summary>Disparo do arco: ergue o arco, puxa a corda (0,9 s), solta e abaixa.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void BowShot(AnimationClip c, float l)
        {
            float[] t = T(0f, 0.3f, 0.88f, 0.95f, l);
            L2Anim.HipKeys(c, t, T(0.95f, 0.92f, 0.92f, 0.92f, 0.95f));
            L2Anim.Keys(c, L2Anim.Body, t, T(0f, 4f, 4f, -4f, 0f), T(0f, -30f, -34f, -30f, 0f));
            L2Anim.Keys(c, L2Anim.Head, t, T(0f, 0f, 0f, 0f, 0f), T(0f, 28f, 32f, 28f, 0f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(0f, -90f, -92f, -95f, -15f), T(0f, 30f, 32f, 30f, 0f), T(-4f, 0f, 0f, 0f, -4f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(0f, -95f, -80f, -100f, -10f), T(0f, 10f, -40f, 20f, 0f), T(4f, 10f, 30f, 5f, 4f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(0f, -15f, -15f, -15f, 0f), null, T(0f, -6f, -6f, -6f, 0f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(0f, 10f, 10f, 10f, 0f), null, T(0f, 6f, 6f, 6f, 0f));
        }

        /// <summary>Golpe de machado: ergue com as duas mãos (0,85 s), desce com força, fica cravado e volta.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void AxeAttack(AnimationClip c, float l)
        {
            float[] t = T(0f, 0.75f, 0.85f, 1.0f, 1.3f, l);
            L2Anim.HipKeys(c, t, T(0.95f, 0.98f, 0.98f, 0.82f, 0.85f, 0.95f));
            L2Anim.Keys(c, L2Anim.Body, t, T(0f, -14f, -15f, 28f, 25f, 0f));
            L2Anim.Keys(c, L2Anim.Head, t, T(0f, -10f, -10f, 15f, 12f, 0f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(0f, -170f, -175f, -40f, -35f, 0f), null, T(4f, -10f, -12f, -15f, -15f, 4f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(0f, -165f, -170f, -45f, -40f, 0f), null, T(-4f, 18f, 20f, 25f, 25f, -4f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(0f, 0f, 0f, -35f, -30f, 0f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(0f, 5f, 5f, 20f, 18f, 0f));
        }

        /// <summary>
        /// Animator Controller dos esqueletos: Levantando → Locomoção (blend tree) e, de qualquer estado,
        /// Ataque (gatilho Attack), Dano (gatilho Hurt) e Morte (bool Dead).
        /// </summary>
        /// <param name="name">Nome do controller.</param>
        /// <param name="rise">Clipe de surgimento.</param>
        /// <param name="idle">Parado.</param>
        /// <param name="walk">Andando.</param>
        /// <param name="run">Correndo.</param>
        /// <param name="attack">Ataque.</param>
        /// <param name="hurt">Dano.</param>
        /// <param name="death">Morte.</param>
        /// <param name="walkSpeed">Velocidade de andar (limiar do blend).</param>
        /// <param name="runSpeed">Velocidade de correr (limiar do blend).</param>
        /// <returns>O controller salvo.</returns>
        private static AnimatorController EnemyController(string name, AnimationClip rise, AnimationClip idle, AnimationClip walk, AnimationClip run,
            AnimationClip attack, AnimationClip hurt, AnimationClip death, float walkSpeed, float runSpeed)
        {
            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(L2Core.AnimationsPath + "/" + name + ".controller");
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

            AnimatorState riseState = sm.AddState("Levantando");
            riseState.motion = rise;
            sm.defaultState = riseState;

            AnimatorState loco = ctrl.CreateBlendTreeInController("Locomocao", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, walkSpeed);
            tree.AddChild(run, runSpeed);

            AnimatorState atk = sm.AddState("Ataque");
            atk.motion = attack;
            AnimatorState dmg = sm.AddState("Dano");
            dmg.motion = hurt;
            AnimatorState die = sm.AddState("Morte");
            die.motion = death;

            L2Anim.AfterClip(riseState.AddTransition(loco), 0.15f, 1f);

            AnimatorStateTransition toAtk = sm.AddAnyStateTransition(atk);
            toAtk.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            toAtk.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
            toAtk.canTransitionToSelf = false;
            L2Anim.Instant(toAtk, 0.06f);
            L2Anim.AfterClip(atk.AddTransition(loco), 0.15f);

            AnimatorStateTransition toDmg = sm.AddAnyStateTransition(dmg);
            toDmg.AddCondition(AnimatorConditionMode.If, 0f, "Hurt");
            toDmg.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
            toDmg.canTransitionToSelf = true;
            L2Anim.Instant(toDmg, 0.04f);
            L2Anim.AfterClip(dmg.AddTransition(loco), 0.12f);

            AnimatorStateTransition toDie = sm.AddAnyStateTransition(die);
            toDie.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            toDie.canTransitionToSelf = false;
            L2Anim.Instant(toDie, 0.08f);

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        // ================================================================== John Craft (combate)

        /// <summary>
        /// Cria "John_Craft_Combate" a partir do prefab do Nível 1 (sem alterá-lo): troca a tocha por uma lanterna
        /// no cinto, coloca o revólver e a faca na mão direita, o controller com a camada dos braços e os scripts de combate.
        /// </summary>
        private static void BuildPlayer()
        {
            GameObject source = L2Core.L1("John_Craft");
            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "John_Craft_Combate";

            // Remove a tocha (ferramenta do Nível 1).
            TorchTool torch = root.GetComponent<TorchTool>();
            if (torch != null) UnityEngine.Object.DestroyImmediate(torch);
            Transform model = root.transform.Find("Model");
            Transform body = model.Find("Body");
            Transform armR = body.Find("ArmR");
            Transform torchVisual = armR.Find("Tocha");
            if (torchVisual != null) UnityEngine.Object.DestroyImmediate(torchVisual.gameObject);

            // Lanterna presa ao cinto (luz pessoal, sempre acesa).
            GameObject lanternPrefab = L2Core.Get("Lanterna_do_Cinto");
            if (lanternPrefab != null)
            {
                GameObject lantern = (GameObject)PrefabUtility.InstantiatePrefab(lanternPrefab);
                lantern.transform.SetParent(body, false);
                lantern.transform.localPosition = V(-0.3f, 0.02f, 0.12f);
            }

            // Revólver (cano para frente quando o braço aponta para frente).
            GameObject revolver = Part("John", armR.gameObject, "Revolver", V(0f, -0.52f, 0.06f), L2Combat.RevolverBody());
            revolver.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part("John", revolver, "Tambor", V(0f, 0.045f, 0.035f), L2Combat.RevolverCylinder());
            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(revolver.transform, false);
            muzzle.transform.localPosition = V(0f, 0.045f, 0.28f);
            Light flash = L2Core.PointLight(muzzle, "LuzDoDisparo", Vector3.zero, new Color(1f, 0.78f, 0.4f), 7f, 4f);
            flash.enabled = false;
            revolver.tag = CombatTags.Weapon;

            // Faca (lâmina para frente quando o braço aponta para frente).
            GameObject knife = Part("John", armR.gameObject, "Faca", V(0f, -0.55f, 0.05f), L2Combat.Knife());
            knife.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            knife.tag = CombatTags.Weapon;

            GameObject throwPoint = new GameObject("PontoDeArremesso");
            throwPoint.transform.SetParent(model, false);
            throwPoint.transform.localPosition = V(0.3f, 1.65f, 0.35f);

            L2Core.SetLayerRecursive(root, 2);

            // Controller com a camada "Bracos" (revólver, faca, arremesso).
            Animator animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = PlayerController(model);

            PlayerCombat combat = root.AddComponent<PlayerCombat>();
            combat.controller = root.GetComponent<PlayerController>();
            combat.animator = animator;
            combat.model = model;
            combat.rightArm = armR;
            combat.muzzle = muzzle.transform;
            combat.throwPoint = throwPoint.transform;
            combat.revolverModel = revolver;
            combat.knifeModel = knife;
            combat.muzzleLight = flash;
            combat.projectilePrefabs = new[] { L2Core.Get("Bala"), L2Core.Get("Pedra_Arremessada") };
            combat.muzzleEffects = new[] { L2Core.Get("FX2_Clarao_do_Disparo"), L2Core.Get("FX2_Fumaca_do_Disparo") };
            combat.meleeHitEffects = new[] { L2Core.Get("FX2_Lascas_de_Osso"), L2Core.Get("FX2_Poeira_de_Impacto") };
            root.AddComponent<PlayerHitbox>();

            PlayerHealth health = root.GetComponent<PlayerHealth>();
            health.blinkRenderers = model.GetComponentsInChildren<MeshRenderer>(true);

            PlayerPrefab = L2Core.Save(root, AssetCategoryType.Personagem, "John Craft (combate)");
        }

        /// <summary>
        /// Controller de John no Nível 2: camada base (locomoção, pulo, queda, empurrar e dano) e a camada
        /// "Bracos" (máscara só no braço direito) com revólver abaixado, mira, disparo, recarga, coronhada,
        /// faca, golpe de faca e arremesso de pedra.
        /// </summary>
        /// <param name="model">Raiz do Animator (para a máscara).</param>
        /// <returns>O controller.</returns>
        private static AnimatorController PlayerController(Transform model)
        {
            // Clipes da camada base (iguais aos do Nível 1, agora com o braço direito balançando).
            AnimationClip idle = L2Anim.Make("John2_Parado", 2f, true, JohnIdle);
            AnimationClip walk = L2Anim.Make("John2_Andando", 0.9f, true, (c, l) => JohnLocomotion(c, l, 30f, 25f, 0.03f, 3f));
            AnimationClip run = L2Anim.Make("John2_Correndo", 0.55f, true, (c, l) => JohnLocomotion(c, l, 55f, 50f, 0.06f, 12f));
            AnimationClip jump = L2Anim.Make("John2_Pulando", 0.4f, false, JohnJump);
            AnimationClip fall = L2Anim.Make("John2_Caindo", 0.8f, true, JohnFall);
            AnimationClip push = L2Anim.Make("John2_Empurrando", 1f, true, JohnPush);
            AnimationClip hurt = L2Anim.Make("John2_Dano", 0.4f, false, JohnHurt);

            // Clipes da camada dos braços (só o braço direito).
            AnimationClip low = L2Anim.Make("John2_Revolver_Baixo", 1f, true, (c, l) => ArmHold(c, l, -28f, 0f, 4f));
            AnimationClip aim = L2Anim.Make("John2_Mirando", 1f, true, (c, l) => ArmHold(c, l, -88f, 0f, 0f));
            AnimationClip shot = L2Anim.Make("John2_Disparo", 0.25f, false, (c, l) => L2Anim.Keys(c, L2Anim.ArmR, T(0f, 0.05f, l), T(-88f, -114f, -88f)));
            AnimationClip reload = L2Anim.Make("John2_Recarga", 1.5f, false, (c, l) =>
                L2Anim.Keys(c, L2Anim.ArmR, T(0f, 0.2f, 0.6f, 0.9f, 1.2f, l), T(-30f, -62f, -60f, -58f, -60f, -30f), T(0f, -20f, -20f, -15f, -20f, 0f), T(0f, -50f, -55f, -38f, -50f, 4f)));
            AnimationClip whip = L2Anim.Make("John2_Coronhada", 0.45f, false, (c, l) =>
                L2Anim.Keys(c, L2Anim.ArmR, T(0f, 0.1f, 0.2f, l), T(-40f, -145f, -45f, -28f), T(0f, -15f, 10f, 0f), T(4f, 10f, -5f, 4f)));
            AnimationClip knifeIdle = L2Anim.Make("John2_Faca_Baixa", 1f, true, (c, l) => ArmHold(c, l, -22f, 0f, 6f));
            AnimationClip stab = L2Anim.Make("John2_Faca_Golpe", 0.35f, false, (c, l) =>
                L2Anim.Keys(c, L2Anim.ArmR, T(0f, 0.05f, 0.12f, 0.22f, l), T(-22f, -45f, -95f, -85f, -22f), T(0f, 20f, -30f, -25f, 0f), T(6f, 10f, 0f, 0f, 6f)));
            AnimationClip toss = L2Anim.Make("John2_Arremesso", 0.5f, false, (c, l) =>
                L2Anim.Keys(c, L2Anim.ArmR, T(0f, 0.18f, 0.28f, l), T(-30f, -175f, -60f, -28f), null, T(4f, 15f, 5f, 4f)));

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(L2Core.AnimationsPath + "/John_Craft_Combate.controller");
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Pushing", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Aiming", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Weapon", AnimatorControllerParameterType.Int);
            foreach (string trig in new[] { "Shoot", "Reload", "Whip", "Stab", "Throw" })
                ctrl.AddParameter(trig, AnimatorControllerParameterType.Trigger);

            // ---------------- Camada base
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
            AnimatorState pushS = sm.AddState("Empurrando"); pushS.motion = push;
            AnimatorState hurtS = sm.AddState("Dano"); hurtS.motion = hurt;

            AnimatorStateTransition t = sm.AddAnyStateTransition(jumpS);
            t.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
            t.canTransitionToSelf = false;
            L2Anim.Instant(t, 0.05f);
            t = jumpS.AddTransition(fallS);
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
            t = sm.AddAnyStateTransition(hurtS);
            t.AddCondition(AnimatorConditionMode.If, 0f, "Hurt");
            t.canTransitionToSelf = false;
            L2Anim.Instant(t, 0.05f);
            L2Anim.AfterClip(hurtS.AddTransition(loco), 0.12f);

            // ---------------- Camada dos braços (peso controlado pelo PlayerCombat: 1 com arma, 0 sem arma)
            ctrl.AddLayer("Bracos");
            AnimatorControllerLayer[] layers = ctrl.layers;
            layers[1].avatarMask = L2Anim.Mask("John_Braco_Direito", model, L2Anim.ArmR);
            layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
            layers[1].defaultWeight = 0f;
            ctrl.layers = layers;
            AnimatorStateMachine arms = ctrl.layers[1].stateMachine;

            AnimatorState lowS = arms.AddState("Revolver_Baixo"); lowS.motion = low;
            AnimatorState aimS = arms.AddState("Mirando"); aimS.motion = aim;
            AnimatorState shotS = arms.AddState("Disparo"); shotS.motion = shot;
            AnimatorState reloadS = arms.AddState("Recarga"); reloadS.motion = reload;
            AnimatorState whipS = arms.AddState("Coronhada"); whipS.motion = whip;
            AnimatorState knifeS = arms.AddState("Faca_Baixa"); knifeS.motion = knifeIdle;
            AnimatorState stabS = arms.AddState("Faca_Golpe"); stabS.motion = stab;
            AnimatorState tossS = arms.AddState("Arremesso"); tossS.motion = toss;
            arms.defaultState = lowS;

            // Posturas básicas (mira e troca de arma).
            t = lowS.AddTransition(aimS);
            t.AddCondition(AnimatorConditionMode.If, 0f, "Aiming");
            L2Anim.Instant(t, 0.1f);
            t = aimS.AddTransition(lowS);
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Aiming");
            L2Anim.Instant(t, 0.15f);
            foreach (AnimatorState s in new[] { lowS, aimS })
            {
                t = s.AddTransition(knifeS);
                t.AddCondition(AnimatorConditionMode.Equals, 1f, "Weapon");
                L2Anim.Instant(t, 0.15f);
            }
            t = knifeS.AddTransition(lowS);
            t.AddCondition(AnimatorConditionMode.Equals, 0f, "Weapon");
            L2Anim.Instant(t, 0.15f);

            // Ações (gatilhos) a partir de qualquer estado.
            ActionState(arms, shotS, "Shoot", true, 0.03f);
            ActionState(arms, reloadS, "Reload", false, 0.1f);
            ActionState(arms, whipS, "Whip", false, 0.05f);
            ActionState(arms, stabS, "Stab", true, 0.04f);
            ActionState(arms, tossS, "Throw", false, 0.05f);

            // Fim de cada ação → postura correspondente à arma e à mira.
            foreach (AnimatorState s in new[] { shotS, reloadS, whipS, stabS, tossS })
            {
                t = s.AddTransition(aimS);
                t.AddCondition(AnimatorConditionMode.Equals, 0f, "Weapon");
                t.AddCondition(AnimatorConditionMode.If, 0f, "Aiming");
                L2Anim.AfterClip(t, 0.08f, 0.9f);
                t = s.AddTransition(lowS);
                t.AddCondition(AnimatorConditionMode.Equals, 0f, "Weapon");
                t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Aiming");
                L2Anim.AfterClip(t, 0.12f, 0.9f);
                t = s.AddTransition(knifeS);
                t.AddCondition(AnimatorConditionMode.Equals, 1f, "Weapon");
                L2Anim.AfterClip(t, 0.12f, 0.9f);
            }

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        /// <summary>
        /// Transição de "qualquer estado" para uma ação disparada por gatilho.
        /// </summary>
        /// <param name="sm">Máquina de estados da camada.</param>
        /// <param name="state">Estado da ação.</param>
        /// <param name="trigger">Gatilho.</param>
        /// <param name="toSelf">Permite reiniciar a própria ação (disparos seguidos).</param>
        /// <param name="duration">Duração da mistura.</param>
        private static void ActionState(AnimatorStateMachine sm, AnimatorState state, string trigger, bool toSelf, float duration)
        {
            AnimatorStateTransition t = sm.AddAnyStateTransition(state);
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            t.canTransitionToSelf = toSelf;
            L2Anim.Instant(t, duration);
        }

        /// <summary>Pose fixa do braço direito com um leve balanço.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        /// <param name="x">Ângulo X.</param>
        /// <param name="y">Ângulo Y.</param>
        /// <param name="z">Ângulo Z.</param>
        private static void ArmHold(AnimationClip c, float l, float x, float y, float z)
        {
            L2Anim.Swing(c, L2Anim.ArmR, l, x, 1.5f, 1f, 0f, z, y);
        }

        /// <summary>John parado (como no Nível 1, com o braço direito solto).</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void JohnIdle(AnimationClip c, float l)
        {
            L2Anim.HipBob(c, l, 0.02f, 1f);
            L2Anim.Swing(c, L2Anim.Body, l, 0f, 1.5f, 1f, 0f);
            L2Anim.Swing(c, L2Anim.Head, l, 0f, 3f, 1f, 0.25f);
            L2Anim.Swing(c, L2Anim.ArmL, l, -4f, 3f, 1f, 0f, -4f);
            L2Anim.Swing(c, L2Anim.ArmR, l, -4f, 3f, 1f, 0.5f, 4f);
            L2Anim.Hold(c, L2Anim.LegL, l, 0f, 0f, -2f);
            L2Anim.Hold(c, L2Anim.LegR, l, 0f, 0f, 2f);
        }

        /// <summary>John andando/correndo.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        /// <param name="legAmp">Amplitude das pernas.</param>
        /// <param name="armAmp">Amplitude dos braços.</param>
        /// <param name="bob">Balanço vertical.</param>
        /// <param name="lean">Inclinação.</param>
        private static void JohnLocomotion(AnimationClip c, float l, float legAmp, float armAmp, float bob, float lean)
        {
            L2Anim.HipBob(c, l, bob, 2f);
            L2Anim.Swing(c, L2Anim.Body, l, lean, 2f, 2f, 0f);
            L2Anim.Swing(c, L2Anim.Head, l, -lean * 0.5f, 2f, 2f, 0.5f);
            L2Anim.Swing(c, L2Anim.LegL, l, 0f, legAmp, 1f, 0f);
            L2Anim.Swing(c, L2Anim.LegR, l, 0f, legAmp, 1f, 0.5f);
            L2Anim.Swing(c, L2Anim.ArmL, l, 0f, armAmp, 1f, 0.5f, -5f);
            L2Anim.Swing(c, L2Anim.ArmR, l, 0f, armAmp, 1f, 0f, 5f);
        }

        /// <summary>John pulando.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void JohnJump(AnimationClip c, float l)
        {
            float[] t = T(0f, l);
            L2Anim.HipKeys(c, t, T(L2Anim.HipHeight, L2Anim.HipHeight));
            L2Anim.Keys(c, L2Anim.Body, t, T(0f, -6f));
            L2Anim.Keys(c, L2Anim.Head, t, T(0f, -8f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(0f, -45f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(0f, 25f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(0f, -150f), null, T(0f, -10f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(0f, -40f), null, T(0f, 15f));
        }

        /// <summary>John caindo.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void JohnFall(AnimationClip c, float l)
        {
            L2Anim.HipBob(c, l, 0f, 1f);
            L2Anim.Swing(c, L2Anim.Body, l, 4f, 3f, 1f, 0f);
            L2Anim.Swing(c, L2Anim.Head, l, 10f, 4f, 2f, 0f);
            L2Anim.Swing(c, L2Anim.LegL, l, -15f, 15f, 2f, 0f, -8f);
            L2Anim.Swing(c, L2Anim.LegR, l, 10f, 15f, 2f, 0.5f, 8f);
            L2Anim.Swing(c, L2Anim.ArmL, l, -130f, 25f, 2f, 0.25f, -25f);
            L2Anim.Swing(c, L2Anim.ArmR, l, -120f, 25f, 2f, 0.75f, 25f);
        }

        /// <summary>John empurrando.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void JohnPush(AnimationClip c, float l)
        {
            L2Anim.HipBob(c, l, 0.03f, 2f);
            L2Anim.Swing(c, L2Anim.Body, l, 22f, 2f, 2f, 0f);
            L2Anim.Swing(c, L2Anim.Head, l, -15f, 2f, 2f, 0f);
            L2Anim.Swing(c, L2Anim.LegL, l, -10f, 20f, 1f, 0f);
            L2Anim.Swing(c, L2Anim.LegR, l, -10f, 20f, 1f, 0.5f);
            L2Anim.Swing(c, L2Anim.ArmL, l, -85f, 5f, 2f, 0f);
            L2Anim.Swing(c, L2Anim.ArmR, l, -85f, 5f, 2f, 0.5f);
        }

        /// <summary>John recebendo dano (tranco para trás).</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void JohnHurt(AnimationClip c, float l)
        {
            float[] t = T(0f, 0.08f, l);
            L2Anim.HipKeys(c, t, T(L2Anim.HipHeight, L2Anim.HipHeight - 0.05f, L2Anim.HipHeight));
            L2Anim.Keys(c, L2Anim.Body, t, T(0f, -18f, 0f));
            L2Anim.Keys(c, L2Anim.Head, t, T(0f, -20f, 0f), null, T(0f, 10f, 0f));
            L2Anim.Keys(c, L2Anim.ArmL, t, T(0f, -35f, 0f), null, T(0f, -25f, 0f));
            L2Anim.Keys(c, L2Anim.ArmR, t, T(0f, -35f, 0f), null, T(0f, 25f, 0f));
            L2Anim.Keys(c, L2Anim.LegL, t, T(0f, 12f, 0f));
            L2Anim.Keys(c, L2Anim.LegR, t, T(0f, -8f, 0f));
        }
    }
}
