using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Furtividade de John: agachar (C alterna, Ctrl segura), ruído das ações (passos, corrida, pulo, pouso),
    /// quanto ele está exposto à luz (luzes da cena + zonas + tocha) e quanto pode ser visto (postura,
    /// movimento, esconderijos, áreas seguras). Os ogros consultam <see cref="VisibilityFactor"/> e ouvem
    /// os ruídos emitidos pelo <see cref="NoiseSystem"/>. Também cuida da morte com um golpe de ogro.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerStealth : MonoBehaviour
    {
        /// <summary>Instância ativa (o jogador).</summary>
        public static PlayerStealth Instance { get; private set; }

        [Header("Referências")]
        public PlayerController controller;
        public Animator animator;
        public StealthTorch torch;
        /// <summary>Luz fraca que ajuda o jogador a enxergar John no escuro (não conta para a furtividade).</summary>
        public Light visionFill;

        [Header("Agachar")]
        public float crouchSpeedMultiplier = 0.5f;
        public float standingHeight = 1.8f;
        public float crouchedHeight = 1.15f;
        public float standingCameraHeight = 1.55f;
        public float crouchedCameraHeight = 1.05f;

        [Header("Ruído (raio em metros)")]
        public float crouchStepNoise = 0.8f;
        public float walkStepNoise = 4f;
        public float runStepNoise = 10f;
        public float jumpNoise = 4.5f;
        public float landNoise = 5f;
        /// <summary>Ruído extra do pouso por segundo no ar.</summary>
        public float landNoisePerAirSecond = 6f;

        /// <summary>Luz ambiente mínima (escuridão total ainda deixa ver a silhueta bem de perto).</summary>
        [Header("Luz")]
        public float ambientExposure = 0.1f;
        /// <summary>Luzes da cena mais fracas que isto são ignoradas.</summary>
        public float minLightIntensity = 0.15f;

        [Header("Visibilidade")]
        public float crouchedFactor = 0.5f;
        public float stillFactor = 0.7f;
        public float crouchMovingFactor = 0.85f;
        public float runningFactor = 1.3f;

        [Header("Morte")]
        public float deathDelay = 1.6f;
        public GameObject[] deathEffects;

        /// <summary>Agachado.</summary>
        public bool IsCrouched { get; private set; }
        /// <summary>Morto (golpe de ogro) até voltar ao checkpoint.</summary>
        public bool IsDead { get; private set; }
        /// <summary>Exposição à luz no ponto de John (0 = breu, 1 = muito iluminado).</summary>
        public float Exposure { get; private set; }
        /// <summary>Velocidade horizontal atual.</summary>
        public float Speed { get; private set; }
        /// <summary>Correndo de fato.</summary>
        public bool IsRunning => controller != null && controller.IsRunning && Speed > controller.walkSpeed + 0.4f;
        /// <summary>Andando (inclui agachado).</summary>
        public bool IsMoving => Speed > 0.4f;
        /// <summary>Tocha acesa.</summary>
        public bool TorchLit => torch != null && torch.Lit;
        /// <summary>Dentro de uma área segura.</summary>
        public bool InSafeZone => safeZones > 0;
        /// <summary>Dentro de um esconderijo.</summary>
        public bool InHidingSpot => hiding.Count > 0;
        /// <summary>Agachado dentro de um esconderijo (quase invisível).</summary>
        public bool IsHidden => InHidingSpot && IsCrouched && !TorchLit;
        /// <summary>Nome do esconderijo/área atual (para o HUD).</summary>
        public string CurrentSpotLabel { get; private set; } = "";
        /// <summary>Multiplicador final da visibilidade (sem distância e linha de visada, que dependem de cada ogro).</summary>
        public float VisibilityFactor { get; private set; }
        /// <summary>Vezes que John foi visto por um ogro (perseguição iniciada).</summary>
        public int TimesSpotted { get; set; }
        /// <summary>Mortes (golpes de ogro).</summary>
        public int Deaths { get; private set; }
        /// <summary>Ponto central do corpo (alvo dos testes de visão).</summary>
        public Vector3 Center => transform.position + Vector3.up * (IsCrouched ? 0.65f : 1.1f);
        /// <summary>Altura da cabeça para os testes de visão.</summary>
        public float HeadHeight => IsCrouched ? 1.0f : 1.65f;

        /// <summary>Disparado ao agachar/levantar.</summary>
        public event System.Action<bool> CrouchChanged;
        /// <summary>Disparado quando John morre e quando volta.</summary>
        public event System.Action<bool> DeathChanged;

        private CapsuleCollider capsule;
        private Rigidbody rb;
        private bool toggleCrouch;
        private float stepTimer;
        private float exposureTimer;
        private int safeZones;
        private readonly List<HidingSpot> hiding = new List<HidingSpot>();
        private readonly List<NoisyFloor> noisy = new List<NoisyFloor>();
        private readonly List<LightZone> zones = new List<LightZone>();
        private readonly List<SafeZone> safe = new List<SafeZone>();
        private static readonly List<Light> SceneLights = new List<Light>();
        private float lightsRefresh;
        private static readonly int CrouchHash = Animator.StringToHash("Crouch");
        private static readonly int DeadHash = Animator.StringToHash("Dead");
        private readonly HashSet<int> animatorParams = new HashSet<int>();

        // ================================================================== Ciclo de vida

        /// <summary>Referências e singleton.</summary>
        private void Awake()
        {
            Instance = this;
            if (controller == null) controller = GetComponent<PlayerController>();
            if (animator == null && controller != null) animator = controller.animator;
            if (torch == null) torch = GetComponent<StealthTorch>();
            capsule = GetComponent<CapsuleCollider>();
            rb = GetComponent<Rigidbody>();
            NoiseSystem.ResetPlayerNoise();
        }

        /// <summary>Eventos do controle (pulo e pouso fazem barulho).</summary>
        private void OnEnable()
        {
            if (controller != null)
            {
                controller.Jumped += OnJumped;
                controller.Landed += OnLanded;
            }
        }

        /// <summary>Cancela os eventos.</summary>
        private void OnDisable()
        {
            if (controller != null)
            {
                controller.Jumped -= OnJumped;
                controller.Landed -= OnLanded;
            }
            if (Instance == this) Instance = null;
        }

        /// <summary>Lê os parâmetros do Animator e as luzes da cena.</summary>
        private void Start()
        {
            if (animator != null)
                foreach (AnimatorControllerParameter p in animator.parameters) animatorParams.Add(p.nameHash);
            RefreshLights();
            UpdateExposure();
        }

        /// <summary>
        /// Entrada de agachar, ruído dos passos, exposição à luz e visibilidade.
        /// </summary>
        private void Update()
        {
            Vector3 v = rb != null ? rb.linearVelocity : Vector3.zero;
            Speed = new Vector3(v.x, 0f, v.z).magnitude;
            bool canAct = GameManager.Instance == null || GameManager.Instance.CanPlayerAct();

            if (canAct && !IsDead) HandleCrouchInput();
            if (!IsDead) UpdateSteps();

            exposureTimer -= Time.deltaTime;
            if (exposureTimer <= 0f)
            {
                exposureTimer = 0.15f;
                UpdateExposure();
            }
            UpdateVisibility();
            UpdateCamera();
        }

        // ================================================================== Agachar

        /// <summary>
        /// C alterna, Ctrl segura; Shift (correr) ou Espaço levantam, se houver espaço.
        /// </summary>
        private void HandleCrouchInput()
        {
            if (StealthInput.CrouchTogglePressed()) toggleCrouch = !toggleCrouch;
            bool wants = toggleCrouch || StealthInput.CrouchHeld();
            if (IsCrouched && (GameInput.JumpPressed() || (GameInput.RunHeld() && controller.HasMoveInput)))
            {
                toggleCrouch = false;
                wants = StealthInput.CrouchHeld();
            }
            if (wants != IsCrouched)
            {
                if (wants) SetCrouched(true);
                else if (CanStand()) SetCrouched(false);
                else
                {
                    toggleCrouch = true;
                    StealthUtil.Hint("Não há espaço para ficar de pé aqui.");
                }
            }
        }

        /// <summary>
        /// Verifica se há espaço acima para ficar de pé.
        /// </summary>
        /// <returns>true se pode levantar.</returns>
        public bool CanStand()
        {
            float r = capsule != null ? capsule.radius * 0.95f : 0.3f;
            Vector3 bottom = transform.position + Vector3.up * (crouchedHeight - r);
            Vector3 top = transform.position + Vector3.up * (standingHeight - r + 0.02f);
            return !Physics.CheckCapsule(bottom, top, r, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Agacha ou levanta: cápsula menor, velocidade reduzida, sem corrida nem pulo, passos abafados.
        /// </summary>
        /// <param name="crouched">Agachado.</param>
        public void SetCrouched(bool crouched)
        {
            if (IsCrouched == crouched) return;
            IsCrouched = crouched;
            toggleCrouch = crouched && toggleCrouch;
            if (capsule != null)
            {
                capsule.height = crouched ? crouchedHeight : standingHeight;
                capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);
            }
            if (controller != null)
            {
                controller.speedMultiplier = crouched ? crouchSpeedMultiplier : 1f;
                controller.runBlocked = crouched;
                controller.jumpBlocked = crouched;
                controller.footstepVolume = crouched ? 0.25f : 1f;
            }
            SetBool(CrouchHash, crouched);
            StealthAudio a = StealthAudio.Instance;
            if (a != null) StealthAudio.Play(crouched ? a.crouchDown : a.standUp, transform.position + Vector3.up * 0.6f, 0.35f);
            if (crouched) StealthUtil.UnlockSkill("agachar");
            CrouchChanged?.Invoke(crouched);
        }

        // ================================================================== Ruído

        /// <summary>
        /// A cada passo emite um ruído conforme a postura, a velocidade e o chão.
        /// </summary>
        private void UpdateSteps()
        {
            if (controller == null || !controller.IsGrounded || Speed < 0.4f)
            {
                stepTimer = Mathf.Min(stepTimer, 0.1f);
                return;
            }
            stepTimer -= Time.deltaTime;
            if (stepTimer > 0f) return;

            bool run = IsRunning;
            stepTimer = IsCrouched ? 0.75f : run ? 0.3f : 0.48f;
            float radius = IsCrouched ? crouchStepNoise : run ? runStepNoise : walkStepNoise;
            NoisyFloor floor = noisy.Count > 0 ? noisy[noisy.Count - 1] : null;
            if (floor != null)
            {
                radius *= IsCrouched ? floor.crouchedMultiplier : floor.noiseMultiplier;
                floor.PlayStep(transform.position, IsCrouched);
            }
            NoiseSystem.Emit(transform.position, radius, run ? NoiseType.Run : NoiseType.Footstep, gameObject);
        }

        /// <summary>Pulo: ruído perceptível.</summary>
        private void OnJumped()
        {
            NoiseSystem.Emit(transform.position, jumpNoise, NoiseType.Jump, gameObject);
        }

        /// <summary>
        /// Pouso: ruído maior quanto mais tempo no ar.
        /// </summary>
        /// <param name="airTime">Tempo no ar.</param>
        private void OnLanded(float airTime)
        {
            float r = landNoise + Mathf.Max(0f, airTime - 0.4f) * landNoisePerAirSecond;
            if (noisy.Count > 0) r *= 1.5f;
            NoiseSystem.Emit(transform.position, r, NoiseType.Land, gameObject);
        }

        // ================================================================== Luz e visibilidade

        /// <summary>
        /// Atualiza a lista de luzes da cena (pontuais e spots; a tocha e a luz de visão de John ficam de fora).
        /// </summary>
        public static void RefreshLights()
        {
            SceneLights.Clear();
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l == null || (l.type != LightType.Point && l.type != LightType.Spot)) continue;
                if (Instance != null && l.transform.IsChildOf(Instance.transform)) continue;
                SceneLights.Add(l);
            }
        }

        /// <summary>
        /// Exposição: luz ambiente + cada luz próxima (diminui com a distância; quase nada se houver parede
        /// no caminho) + zonas de luz, vezes as zonas de sombra. Com a tocha acesa a exposição é máxima.
        /// </summary>
        private void UpdateExposure()
        {
            lightsRefresh -= 0.15f;
            if (lightsRefresh <= 0f)
            {
                lightsRefresh = 3f;
                RefreshLights();
            }

            Vector3 p = transform.position + Vector3.up * 1.0f;
            float sum = ambientExposure;
            foreach (Light l in SceneLights)
            {
                if (l == null || !l.enabled || !l.gameObject.activeInHierarchy || l.intensity < minLightIntensity) continue;
                Vector3 lp = l.transform.position;
                float d = Vector3.Distance(p, lp);
                if (d >= l.range) continue;
                float k = 1f - d / l.range;
                float contribution = l.intensity * 0.45f * k * k;
                if (l.type == LightType.Spot)
                {
                    float angle = Vector3.Angle(l.transform.forward, p - lp);
                    if (angle > l.spotAngle * 0.5f) continue;
                }
                // Para um pouco antes da luz (a própria luminária tem colisor).
                Vector3 end = lp + (p - lp).normalized * Mathf.Min(0.5f, d * 0.5f);
                if (Physics.Linecast(p, end, StealthUtil.SightMask, QueryTriggerInteraction.Ignore)) contribution *= 0.12f;
                sum += contribution;
            }
            float mult = 1f;
            foreach (LightZone z in zones)
            {
                if (z == null) continue;
                sum += z.bonus;
                mult *= z.multiplier;
            }
            Exposure = Mathf.Clamp01(sum * mult);
            if (TorchLit) Exposure = 1f;
        }

        /// <summary>
        /// Combina luz, postura, movimento e esconderijo num único fator (0 a ~1,3).
        /// </summary>
        private void UpdateVisibility()
        {
            if (IsDead) { VisibilityFactor = 0f; return; }
            float stance = IsCrouched ? crouchedFactor : 1f;
            float motion = !IsMoving ? stillFactor : IsRunning ? runningFactor : IsCrouched ? crouchMovingFactor : 1f;
            float hide = 1f;
            if (InHidingSpot && !TorchLit)
            {
                HidingSpot h = hiding[hiding.Count - 1];
                hide = IsCrouched ? h.crouchedFactor : h.standingFactor;
            }
            VisibilityFactor = Exposure * stance * motion * hide;
            if (TorchLit) VisibilityFactor = Mathf.Max(VisibilityFactor, IsCrouched ? 0.9f : 1.2f);
        }

        /// <summary>
        /// A câmera abaixa junto com John ao agachar.
        /// </summary>
        private void UpdateCamera()
        {
            ThirdPersonCamera cam = ThirdPersonCamera.Instance;
            if (cam == null) return;
            float target = IsCrouched ? crouchedCameraHeight : standingCameraHeight;
            Vector3 off = cam.targetOffset;
            off.y = Mathf.MoveTowards(off.y, target, Time.deltaTime * 2.5f);
            cam.targetOffset = off;
        }

        // ================================================================== Áreas

        /// <summary>
        /// Entrou numa área de furtividade.
        /// </summary>
        /// <param name="v">Área.</param>
        public void EnterVolume(StealthVolume v)
        {
            switch (v)
            {
                case HidingSpot h:
                    if (!hiding.Contains(h)) { hiding.Add(h); h.OnPlayerEnter(this); }
                    break;
                case SafeZone s:
                    if (!safe.Contains(s)) { safe.Add(s); safeZones = safe.Count; s.OnPlayerEnter(); }
                    break;
                case NoisyFloor n:
                    if (!noisy.Contains(n)) noisy.Add(n);
                    break;
                case LightZone z:
                    if (!zones.Contains(z)) zones.Add(z);
                    break;
            }
            RefreshLabel();
        }

        /// <summary>
        /// Saiu de uma área de furtividade.
        /// </summary>
        /// <param name="v">Área.</param>
        public void ExitVolume(StealthVolume v)
        {
            if (v is HidingSpot h) hiding.Remove(h);
            else if (v is SafeZone s) { safe.Remove(s); safeZones = safe.Count; }
            else if (v is NoisyFloor n) noisy.Remove(n);
            else if (v is LightZone z) zones.Remove(z);
            RefreshLabel();
        }

        /// <summary>
        /// Nome do lugar atual para o HUD.
        /// </summary>
        private void RefreshLabel()
        {
            if (safe.Count > 0) CurrentSpotLabel = string.IsNullOrEmpty(safe[safe.Count - 1].label) ? "Área segura" : safe[safe.Count - 1].label;
            else if (hiding.Count > 0) CurrentSpotLabel = string.IsNullOrEmpty(hiding[hiding.Count - 1].label) ? "Esconderijo" : hiding[hiding.Count - 1].label;
            else if (noisy.Count > 0) CurrentSpotLabel = string.IsNullOrEmpty(noisy[noisy.Count - 1].label) ? "Chão barulhento" : noisy[noisy.Count - 1].label;
            else CurrentSpotLabel = "";
        }

        /// <summary>
        /// Esvazia as listas de áreas (ao voltar ao checkpoint por teletransporte).
        /// </summary>
        public void ClearVolumes()
        {
            hiding.Clear();
            safe.Clear();
            noisy.Clear();
            zones.Clear();
            safeZones = 0;
            RefreshLabel();
        }

        // ================================================================== Morte e retorno

        /// <summary>
        /// Golpe de ogro: John cai, a tela escurece e aparece a tela de derrota ("Voltar ao checkpoint").
        /// </summary>
        /// <param name="killer">Ogro que golpeou.</param>
        public void Die(Transform killer)
        {
            if (IsDead) return;
            IsDead = true;
            Deaths++;
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.SetCutscene(true);
            if (torch != null && torch.Lit) torch.SetLit(false, false);
            if (rb != null)
            {
                Vector3 away = killer != null ? (transform.position - killer.position) : -transform.forward;
                away.y = 0f;
                rb.linearVelocity = away.normalized * 3f + Vector3.up * 2f;
            }
            if (controller != null && controller.model != null && killer != null)
            {
                Vector3 look = killer.position - transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f) controller.model.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
            }
            SetBool(DeadHash, true);
            StealthAudio a = StealthAudio.Instance;
            if (a != null) StealthAudio.Play(a.playerDeath, transform.position + Vector3.up, 1f);
            StealthUtil.SpawnAll(deathEffects, transform.position + Vector3.up * 0.3f);
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.5f, 0.25f);
            if (gm != null && gm.ui != null) gm.ui.FlashDamage();
            DeathChanged?.Invoke(true);
            StartCoroutine(DeathRoutine());
        }

        /// <summary>
        /// Espera a animação de queda e mostra a derrota.
        /// </summary>
        /// <returns>Corrotina.</returns>
        private IEnumerator DeathRoutine()
        {
            yield return new WaitForSeconds(deathDelay);
            GameManager gm = GameManager.Instance;
            if (gm == null) yield break;
            gm.SetCutscene(false);
            gm.GameOver();
        }

        /// <summary>
        /// Volta à vida no checkpoint (chamado quando o trecho é reiniciado).
        /// </summary>
        public void Revive()
        {
            StopAllCoroutines();
            bool wasDead = IsDead;
            IsDead = false;
            SetBool(DeadHash, false);
            if (IsCrouched && CanStand()) SetCrouched(false);
            toggleCrouch = false;
            ClearVolumes();
            if (GameManager.Instance != null) GameManager.Instance.SetCutscene(false);
            if (wasDead) DeathChanged?.Invoke(false);
        }

        /// <summary>
        /// Muda um bool do Animator se o parâmetro existir.
        /// </summary>
        /// <param name="hash">Parâmetro.</param>
        /// <param name="value">Valor.</param>
        private void SetBool(int hash, bool value)
        {
            if (animator != null && animatorParams.Contains(hash)) animator.SetBool(hash, value);
        }
    }
}
