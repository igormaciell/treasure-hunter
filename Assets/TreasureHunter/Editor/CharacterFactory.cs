using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TreasureHunter.EditorTools
{
    /// <summary>
    /// Cria John Craft em estilo voxel (cabeça, chapéu, tronco, braços, pernas, mochila e tocha),
    /// os clipes de animação (parado, andando, correndo, pulando, caindo e empurrando)
    /// e o Animator Controller com as transições entre eles.
    /// </summary>
    public static class CharacterFactory
    {
        /// <summary>Prefab do jogador gerado.</summary>
        public static GameObject PlayerPrefab;

        private const string BodyPath = "Body";
        private const string HeadPath = "Body/Head";
        private const string ArmLPath = "Body/ArmL";
        private const string LegLPath = "Body/LegL";
        private const string LegRPath = "Body/LegR";
        private const float HipHeight = 0.95f;

        /// <summary>
        /// Cria animações, controller e prefab do jogador.
        /// </summary>
        public static void Build()
        {
            AnimatorController controller = BuildController();
            PlayerPrefab = BuildPlayer(controller);
        }

        // ================================================================== Modelo

        /// <summary>
        /// Monta a hierarquia do personagem e os componentes de jogo, e salva como prefab.
        /// </summary>
        /// <param name="controller">Animator Controller do personagem.</param>
        /// <returns>O prefab do jogador.</returns>
        private static GameObject BuildPlayer(AnimatorController controller)
        {
            GameObject root = new GameObject("John_Craft");
            root.tag = GameTags.Player;
            root.layer = 2; // "Ignore Raycast": as sondagens de chão e da câmera ignoram o próprio jogador.

            // Física: cápsula sem atrito e Rigidbody.
            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = 0.32f;
            capsule.center = new Vector3(0f, 0.9f, 0f);
            capsule.sharedMaterial = PropFactory.PlayerPhysics;
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 70f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;

            // Modelo visual (gira para a direção do movimento) com Animator.
            GameObject model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            model.layer = 2;
            Animator animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            GameObject body = Part(model, "Body", new Vector3(0, HipHeight, 0), new VoxelModel()
                .Add(new Vector3(0, 0.3f, 0), new Vector3(0.52f, 0.6f, 0.3f), Palette.Shirt)
                .Add(new Vector3(0, 0.04f, 0), new Vector3(0.54f, 0.1f, 0.32f), Palette.Belt)
                .Add(new Vector3(0.18f, 0.04f, 0.17f), new Vector3(0.08f, 0.08f, 0.02f), Palette.Gold)
                .Add(new Vector3(0, 0.3f, -0.24f), new Vector3(0.44f, 0.5f, 0.2f), Palette.Backpack)
                .Add(new Vector3(0, 0.58f, -0.24f), new Vector3(0.5f, 0.14f, 0.14f), Palette.ClothRed)
                .Add(new Vector3(-0.3f, 0.02f, 0f), new Vector3(0.1f, 0.22f, 0.16f), Palette.Leather)
                .Add(new Vector3(0, 0.45f, 0.155f), new Vector3(0.12f, 0.2f, 0.02f), Palette.Pants));

            Part(body, "Head", new Vector3(0, 0.62f, 0), new VoxelModel()
                .Add(new Vector3(0, 0.2f, 0), new Vector3(0.4f, 0.4f, 0.4f), Palette.Skin)
                .Add(new Vector3(-0.09f, 0.22f, 0.201f), new Vector3(0.07f, 0.07f, 0.01f), Palette.Eyes)
                .Add(new Vector3(0.09f, 0.22f, 0.201f), new Vector3(0.07f, 0.07f, 0.01f), Palette.Eyes)
                .Add(new Vector3(0, 0.08f, 0.201f), new Vector3(0.18f, 0.06f, 0.01f), Palette.Hair)
                .Add(new Vector3(0, 0.3f, -0.12f), new Vector3(0.42f, 0.22f, 0.18f), Palette.Hair)
                .Add(new Vector3(0, 0.41f, 0), new Vector3(0.66f, 0.05f, 0.66f), Palette.Hat)
                .Add(new Vector3(0, 0.52f, 0), new Vector3(0.44f, 0.2f, 0.44f), Palette.Hat)
                .Add(new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.05f, 0.45f), Palette.Belt));

            VoxelModel arm = new VoxelModel()
                .Add(new Vector3(0, -0.15f, 0), new Vector3(0.18f, 0.32f, 0.18f), Palette.Shirt)
                .Add(new Vector3(0, -0.42f, 0), new Vector3(0.16f, 0.24f, 0.16f), Palette.Skin);
            Part(body, "ArmL", new Vector3(-0.35f, 0.56f, 0), arm);
            GameObject armR = Part(body, "ArmR", new Vector3(0.35f, 0.56f, 0), arm);

            VoxelModel leg = new VoxelModel()
                .Add(new Vector3(0, -0.35f, 0), new Vector3(0.22f, 0.7f, 0.24f), Palette.Pants)
                .Add(new Vector3(0, -0.82f, 0.03f), new Vector3(0.24f, 0.26f, 0.3f), Palette.Boots);
            Part(body, "LegL", new Vector3(-0.13f, 0f, 0), leg);
            Part(body, "LegR", new Vector3(0.13f, 0f, 0), leg);

            // Tocha na mão direita (o visual fica sempre de pé; o braço sobe ao erguê-la).
            GameObject torch = new GameObject("Tocha");
            torch.transform.SetParent(armR.transform, false);
            torch.transform.localPosition = new Vector3(0, -0.5f, 0.06f);
            GameObject torchVisual = Part(torch, "Visual", Vector3.zero, new VoxelModel()
                .Add(new Vector3(0, 0.15f, 0), new Vector3(0.07f, 0.55f, 0.07f), Palette.WoodDark)
                .Add(new Vector3(0, 0.44f, 0), new Vector3(0.12f, 0.1f, 0.12f), Palette.Rope)
                .Add(new Vector3(0, 0.52f, 0), new Vector3(0.1f, 0.08f, 0.1f), Palette.FireGlow));
            ParticleSystem flame = PropFactory.AddFx(torchVisual, "FX_Fire", new Vector3(0, 0.55f, 0));
            flame.transform.localScale = Vector3.one * 0.7f;
            GameObject lightGo = new GameObject("LuzDaTocha");
            lightGo.transform.SetParent(torchVisual.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 0.75f, 0.1f);
            Light torchLight = lightGo.AddComponent<Light>();
            torchLight.type = LightType.Point;
            torchLight.color = new Color(1f, 0.66f, 0.32f);
            torchLight.range = 7f;
            torchLight.intensity = 2.2f;
            torchLight.shadows = LightShadows.Soft;
            torchLight.shadowStrength = 0.8f;

            // Componentes de jogo.
            PlayerController pc = root.AddComponent<PlayerController>();
            pc.model = model.transform;
            pc.animator = animator;
            PlayerHealth health = root.AddComponent<PlayerHealth>();
            health.blinkRenderers = model.GetComponentsInChildren<MeshRenderer>(true);
            root.AddComponent<PlayerInteractor>();
            TorchTool tool = root.AddComponent<TorchTool>();
            tool.torchLight = torchLight;
            tool.armPivot = armR.transform;
            tool.torchVisual = torchVisual.transform;
            tool.flame = flame;

            AssetCategory cat = root.AddComponent<AssetCategory>();
            cat.category = AssetCategoryType.Personagem;
            cat.displayName = "John Craft (jogador)";

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BuilderCore.PrefabsPath + "/John_Craft.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// Cria uma parte do corpo (pivô na articulação) com sua malha voxel.
        /// </summary>
        /// <param name="parent">Parte pai.</param>
        /// <param name="name">Nome da parte (usado nos caminhos da animação).</param>
        /// <param name="localPos">Posição da articulação.</param>
        /// <param name="model">Modelo voxel da parte.</param>
        /// <returns>O objeto da parte.</returns>
        private static GameObject Part(GameObject parent, string name, Vector3 localPos, VoxelModel model)
        {
            GameObject go = new GameObject(name);
            go.layer = 2;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            model.ApplyTo(go, BuilderCore.MeshesPath + "/John_" + parent.name + "_" + name + ".asset");
            return go;
        }

        // ================================================================== Animações

        /// <summary>
        /// Cria os clipes e o Animator Controller (parâmetros, blend tree de locomoção e transições).
        /// </summary>
        /// <returns>O controller salvo.</returns>
        private static AnimatorController BuildController()
        {
            AnimationClip idle = MakeClip("John_Parado", 2f, true, Idle);
            AnimationClip walk = MakeClip("John_Andando", 0.9f, true, (c, l) => Locomotion(c, l, 30f, 25f, 0.03f, 3f));
            AnimationClip run = MakeClip("John_Correndo", 0.55f, true, (c, l) => Locomotion(c, l, 55f, 50f, 0.06f, 12f));
            AnimationClip jump = MakeClip("John_Pulando", 0.4f, false, Jump);
            AnimationClip fall = MakeClip("John_Caindo", 0.8f, true, Fall);
            AnimationClip push = MakeClip("John_Empurrando", 1f, true, Push);

            string path = BuilderCore.AnimationsPath + "/John_Craft.controller";
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Pushing", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            // Locomoção: blend tree 1D por velocidade (parado → andando → correndo).
            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomocao", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, 3.2f);
            tree.AddChild(run, 6.2f);
            sm.defaultState = locomotion;

            AnimatorState jumpState = sm.AddState("Pulando");
            jumpState.motion = jump;
            AnimatorState fallState = sm.AddState("Caindo");
            fallState.motion = fall;
            AnimatorState pushState = sm.AddState("Empurrando");
            pushState.motion = push;

            // Qualquer estado → Pulando (gatilho Jump).
            AnimatorStateTransition toJump = sm.AddAnyStateTransition(jumpState);
            toJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
            Setup(toJump, 0.05f);
            toJump.canTransitionToSelf = false;

            // Pulando → Caindo quando começa a descer; Pulando → Locomoção ao pousar.
            AnimatorStateTransition jumpToFall = jumpState.AddTransition(fallState);
            jumpToFall.AddCondition(AnimatorConditionMode.Less, 0f, "VerticalVelocity");
            Setup(jumpToFall, 0.15f);
            AnimatorStateTransition jumpToLoco = jumpState.AddTransition(locomotion);
            jumpToLoco.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            jumpToLoco.AddCondition(AnimatorConditionMode.Less, 0.5f, "VerticalVelocity");
            Setup(jumpToLoco, 0.1f);

            // Locomoção → Caindo ao sair do chão sem pular (ex.: borda ou buraco).
            AnimatorStateTransition locoToFall = locomotion.AddTransition(fallState);
            locoToFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            locoToFall.AddCondition(AnimatorConditionMode.Less, -2f, "VerticalVelocity");
            Setup(locoToFall, 0.15f);
            AnimatorStateTransition fallToLoco = fallState.AddTransition(locomotion);
            fallToLoco.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            Setup(fallToLoco, 0.1f);

            // Empurrar caixote.
            AnimatorStateTransition locoToPush = locomotion.AddTransition(pushState);
            locoToPush.AddCondition(AnimatorConditionMode.If, 0f, "Pushing");
            Setup(locoToPush, 0.15f);
            AnimatorStateTransition pushToLoco = pushState.AddTransition(locomotion);
            pushToLoco.AddCondition(AnimatorConditionMode.IfNot, 0f, "Pushing");
            Setup(pushToLoco, 0.2f);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        /// <summary>
        /// Configura uma transição sem tempo de saída e com a duração informada.
        /// </summary>
        /// <param name="t">Transição.</param>
        /// <param name="duration">Duração da mistura em segundos.</param>
        private static void Setup(AnimatorStateTransition t, float duration)
        {
            t.hasExitTime = false;
            t.duration = duration;
            t.hasFixedDuration = true;
        }

        /// <summary>
        /// Cria um clipe, preenche suas curvas pela função informada e o salva.
        /// </summary>
        /// <param name="name">Nome do clipe.</param>
        /// <param name="length">Duração em segundos.</param>
        /// <param name="loop">true para repetir.</param>
        /// <param name="fill">Função que adiciona as curvas (clipe, duração).</param>
        /// <returns>O clipe salvo.</returns>
        private static AnimationClip MakeClip(string name, float length, bool loop, System.Action<AnimationClip, float> fill)
        {
            AnimationClip clip = new AnimationClip { name = name, frameRate = 30f };
            fill(clip, length);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            BuilderCore.SaveAsset(clip, BuilderCore.AnimationsPath + "/" + name + ".anim");
            return clip;
        }

        /// <summary>
        /// Adiciona uma curva de rotação (eixo X) senoidal a uma parte do corpo; Y e Z ficam em zero.
        /// </summary>
        /// <param name="clip">Clipe.</param>
        /// <param name="path">Caminho da parte (ex.: "Body/LegL").</param>
        /// <param name="length">Duração do clipe.</param>
        /// <param name="offset">Ângulo médio.</param>
        /// <param name="amplitude">Amplitude da oscilação.</param>
        /// <param name="cycles">Ciclos por clipe.</param>
        /// <param name="phase">Fase (0 a 1).</param>
        /// <param name="zAngle">Ângulo fixo no eixo Z (abrir braços/pernas).</param>
        private static void Swing(AnimationClip clip, string path, float length, float offset, float amplitude, float cycles, float phase, float zAngle = 0f)
        {
            AnimationCurve x = new AnimationCurve();
            const int samples = 12;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                x.AddKey(t * length, offset + amplitude * Mathf.Sin((t * cycles + phase) * Mathf.PI * 2f));
            }
            SetEuler(clip, path, x, AnimationCurve.Constant(0f, length, 0f), AnimationCurve.Constant(0f, length, zAngle));
        }

        /// <summary>
        /// Grava curvas de rotação em ângulos de Euler para uma parte.
        /// </summary>
        /// <param name="clip">Clipe.</param>
        /// <param name="path">Caminho da parte.</param>
        /// <param name="x">Curva do eixo X.</param>
        /// <param name="y">Curva do eixo Y.</param>
        /// <param name="z">Curva do eixo Z.</param>
        private static void SetEuler(AnimationClip clip, string path, AnimationCurve x, AnimationCurve y, AnimationCurve z)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.x"), x);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.y"), y);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.z"), z);
        }

        /// <summary>
        /// Grava a posição do quadril (balanço vertical do corpo).
        /// </summary>
        /// <param name="clip">Clipe.</param>
        /// <param name="length">Duração.</param>
        /// <param name="bob">Amplitude do balanço.</param>
        /// <param name="cycles">Ciclos por clipe.</param>
        private static void BodyBob(AnimationClip clip, float length, float bob, float cycles)
        {
            AnimationCurve y = new AnimationCurve();
            const int samples = 12;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                y.AddKey(t * length, HipHeight - bob * 0.5f + bob * 0.5f * Mathf.Cos(t * cycles * Mathf.PI * 2f));
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(BodyPath, typeof(Transform), "m_LocalPosition.x"), AnimationCurve.Constant(0f, length, 0f));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(BodyPath, typeof(Transform), "m_LocalPosition.y"), y);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(BodyPath, typeof(Transform), "m_LocalPosition.z"), AnimationCurve.Constant(0f, length, 0f));
        }

        /// <summary>Animação "parado": respiração leve e cabeça olhando ao redor.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Idle(AnimationClip c, float l)
        {
            BodyBob(c, l, 0.02f, 1f);
            Swing(c, BodyPath, l, 0f, 1.5f, 1f, 0f);
            Swing(c, HeadPath, l, 0f, 3f, 1f, 0.25f);
            Swing(c, ArmLPath, l, -4f, 3f, 1f, 0f, -4f);
            Swing(c, LegLPath, l, 0f, 0f, 1f, 0f, -2f);
            Swing(c, LegRPath, l, 0f, 0f, 1f, 0f, 2f);
        }

        /// <summary>Animação de locomoção (andar/correr): pernas e braço alternados, corpo inclinado.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        /// <param name="legAmp">Amplitude das pernas.</param>
        /// <param name="armAmp">Amplitude do braço.</param>
        /// <param name="bob">Balanço vertical.</param>
        /// <param name="lean">Inclinação do corpo para frente.</param>
        private static void Locomotion(AnimationClip c, float l, float legAmp, float armAmp, float bob, float lean)
        {
            BodyBob(c, l, bob, 2f);
            Swing(c, BodyPath, l, lean, 2f, 2f, 0f);
            Swing(c, HeadPath, l, -lean * 0.5f, 2f, 2f, 0.5f);
            Swing(c, LegLPath, l, 0f, legAmp, 1f, 0f);
            Swing(c, LegRPath, l, 0f, legAmp, 1f, 0.5f);
            Swing(c, ArmLPath, l, 0f, armAmp, 1f, 0.5f, -5f);
        }

        /// <summary>Animação "pulando": pernas encolhidas e braço para cima.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Jump(AnimationClip c, float l)
        {
            BodyBob(c, l, 0f, 1f);
            SetEuler(c, BodyPath, AnimationCurve.EaseInOut(0, 0, l, -6f), AnimationCurve.Constant(0, l, 0), AnimationCurve.Constant(0, l, 0));
            SetEuler(c, HeadPath, AnimationCurve.EaseInOut(0, 0, l, -8f), AnimationCurve.Constant(0, l, 0), AnimationCurve.Constant(0, l, 0));
            SetEuler(c, LegLPath, AnimationCurve.EaseInOut(0, 0, l, -45f), AnimationCurve.Constant(0, l, 0), AnimationCurve.Constant(0, l, 0));
            SetEuler(c, LegRPath, AnimationCurve.EaseInOut(0, 0, l, 25f), AnimationCurve.Constant(0, l, 0), AnimationCurve.Constant(0, l, 0));
            SetEuler(c, ArmLPath, AnimationCurve.EaseInOut(0, 0, l, -150f), AnimationCurve.Constant(0, l, 0), AnimationCurve.Constant(0, l, -10f));
        }

        /// <summary>Animação "caindo": braço e pernas se debatendo.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Fall(AnimationClip c, float l)
        {
            BodyBob(c, l, 0f, 1f);
            Swing(c, BodyPath, l, 4f, 3f, 1f, 0f);
            Swing(c, HeadPath, l, 10f, 4f, 2f, 0f);
            Swing(c, LegLPath, l, -15f, 15f, 2f, 0f, -8f);
            Swing(c, LegRPath, l, 10f, 15f, 2f, 0.5f, 8f);
            Swing(c, ArmLPath, l, -130f, 25f, 2f, 0.25f, -25f);
        }

        /// <summary>Animação "empurrando": corpo inclinado, braço à frente e passos curtos.</summary>
        /// <param name="c">Clipe.</param>
        /// <param name="l">Duração.</param>
        private static void Push(AnimationClip c, float l)
        {
            BodyBob(c, l, 0.03f, 2f);
            Swing(c, BodyPath, l, 22f, 2f, 2f, 0f);
            Swing(c, HeadPath, l, -15f, 2f, 2f, 0f);
            Swing(c, LegLPath, l, -10f, 20f, 1f, 0f);
            Swing(c, LegRPath, l, -10f, 20f, 1f, 0.5f);
            Swing(c, ArmLPath, l, -85f, 5f, 2f, 0f);
        }
    }
}
