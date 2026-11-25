using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace TreasureHunter
{
    /// <summary>
    /// Ogro do Nível 4: não é um inimigo de combate — é uma ameaça a ser evitada. Patrulha um trajeto pelo
    /// NavMesh, enxerga com um <see cref="VisionSensor"/> (campo de visão reduzido, Raycast, luz) e ouve com um
    /// <see cref="HearingSensor"/>. Máquina de estados com transições graduais:
    /// Patrulhando → Suspeitando (para e vira devagar para o estímulo) → Investigando (vai até a última posição
    /// suspeita e procura em volta) → Retornando (volta ao trajeto). Se a atenção enche, ruge e passa a
    /// Perseguindo (corre até John); de perto, Atacando: um golpe de clava mata na hora.
    /// Um disparo revela a origem imediatamente. Balas só fazem o ogro cambalear (vida enorme).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class OgreAI : MonoBehaviour, IDamageable
    {
        /// <summary>Estados da máquina de estados.</summary>
        public enum State { Patrolling, Suspicious, Investigating, Chasing, Returning, Attacking, Staggered }

        /// <summary>Ogros ativos na cena.</summary>
        public static readonly List<OgreAI> All = new List<OgreAI>();
        /// <summary>Disparado a cada troca de estado (ogro, anterior, novo).</summary>
        public static event Action<OgreAI, State, State> StateChanged;

        [Header("Identificação")]
        public string displayName = "Ogro";

        [Header("Patrulha")]
        public PatrolRoute route;
        public int startPoint;

        [Header("Sentidos")]
        public VisionSensor vision;
        public HearingSensor hearing;
        /// <summary>Atenção a partir da qual o ogro desconfia.</summary>
        public float suspicionThreshold = 0.3f;

        [Header("Velocidades (m/s) e giro (graus/s)")]
        public float patrolSpeed = 1.35f;
        public float investigateSpeed = 1.8f;
        public float chaseSpeed = 4.0f;
        public float returnSpeed = 1.5f;
        public float patrolTurn = 110f;
        public float chaseTurn = 260f;

        /// <summary>Pausa antes de virar para o estímulo (o ogro "processa" o que viu/ouviu).</summary>
        [Header("Tempos (s)")]
        public float reactionTime = 0.4f;
        public float suspiciousTime = 2.2f;
        public float lookAroundTime = 3.5f;
        public float searchRadius = 5f;
        /// <summary>Depois de perder John de vista, ainda "sabe" para onde ele foi por este tempo.</summary>
        public float chaseMemory = 1.5f;
        /// <summary>Sem ver John por este tempo, desiste da perseguição e investiga.</summary>
        public float loseTime = 4.5f;
        /// <summary>Tempo bufando na entrada de uma área segura antes de desistir.</summary>
        public float blockedTime = 4f;
        /// <summary>Duração do estado de alerta (mais sensível) depois de uma perseguição/investigação.</summary>
        public float alertDuration = 18f;
        /// <summary>Duração do rugido ao avistar John (ele para e ruge antes de correr).</summary>
        public float roarDuration = 0.9f;

        [Header("Ataque")]
        public float attackRange = 2.3f;
        public float hitRange = 2.9f;
        public float attackAngle = 75f;
        public float attackWindup = 0.8f;
        public float attackRecover = 1.0f;
        public Transform attackPoint;

        [Header("Resistência")]
        public float maxHealth = 600f;
        public float staggerTime = 1.2f;

        [Header("Referências")]
        public Animator animator;
        public AlertIndicator indicator;
        public AudioSource voice;
        public AudioSource feet;
        public AudioSource breath;
        public Transform mouth;

        [Header("Efeitos (vetores de prefabs)")]
        public GameObject[] stepEffects;
        public GameObject[] smashEffects;
        public GameObject[] alertEffects;
        public GameObject[] suspiciousEffects;
        public GameObject[] hurtEffects;
        public GameObject[] breathEffects;

        [Header("Sons (vetores sorteados)")]
        public AudioClip[] stepClips;
        public AudioClip[] growlClips;
        public AudioClip[] suspiciousClips;
        public AudioClip[] sniffClips;
        public AudioClip[] roarClips;
        public AudioClip[] attackClips;
        public AudioClip[] smashClips;
        public AudioClip[] staggerClips;
        public AudioClip[] frustratedClips;
        public float stepInterval = 0.62f;

        /// <summary>Estado atual.</summary>
        public State CurrentState { get; private set; } = State.Patrolling;
        /// <summary>Desligado (ainda não entrou em cena).</summary>
        public bool Dormant { get; private set; }
        /// <summary>Vida (nunca chega a zero: o ogro não é para ser vencido).</summary>
        public float Health { get; private set; }
        /// <summary>Sempre vivo.</summary>
        public bool IsAlive => true;
        /// <summary>Lado dos inimigos.</summary>
        public Team Team => Team.Enemy;
        /// <summary>Atenção atual (0 a 1).</summary>
        public float Awareness => vision != null ? vision.Awareness : 0f;
        /// <summary>Em alerta (mais sensível) depois de uma perseguição ou investigação.</summary>
        public bool Alert => Time.time < alertUntil;
        /// <summary>Vezes que este ogro começou uma perseguição.</summary>
        public int TimesAlerted { get; private set; }
        /// <summary>Trajeto atual.</summary>
        public PatrolRoute CurrentRoute => route;
        /// <summary>Ponto de interesse atual (estímulo, alvo da investigação ou última posição conhecida).</summary>
        public Vector3 FocusPoint { get; private set; }

        private NavMeshAgent agent;
        private int waypoint;
        private int direction = 1;
        private bool waiting;
        private float waitTimer;
        private float stateTimer;
        private float reactionTimer;
        private Vector3 stimulus;
        private bool stimulusFromSight;
        private bool lookPhase;
        private float lookTimer;
        private int searchesLeft;
        private Vector3 investigateTarget;
        private Vector3 lastKnown;
        private float lastSeen = -100f;
        private float blockedTimer;
        private float roarUntil;
        private float roarCooldownUntil;
        private float attackTimer;
        private bool hitDone;
        private float alertUntil;
        private float nextGrowl;
        private float nextBreathPuff;
        private float stepTimer;
        private bool stepLeft;
        private bool faceTarget;
        private Vector3 facePoint;
        private float turnSpeed;
        private Vector3 lastDestination = new Vector3(9999f, 9999f, 9999f);
        private float nextRepath;
        private float lookPhaseOffset;
        private readonly HashSet<int> animatorParams = new HashSet<int>();

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int ModeHash = Animator.StringToHash("Mode");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int RoarHash = Animator.StringToHash("Roar");
        private static readonly int StaggerHash = Animator.StringToHash("Stagger");

        // ================================================================== Ciclo de vida

        /// <summary>Configura o agente (o giro é feito pelo script, para ser suave).</summary>
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
            agent.speed = patrolSpeed;
            Health = maxHealth;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (vision == null) vision = GetComponent<VisionSensor>();
            if (hearing == null) hearing = GetComponent<HearingSensor>();
            if (animator != null)
                foreach (AnimatorControllerParameter p in animator.parameters) animatorParams.Add(p.nameHash);
            lookPhaseOffset = UnityEngine.Random.Range(0f, 10f);
        }

        /// <summary>Registra e ouve ruídos.</summary>
        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            if (hearing != null) hearing.Heard += OnHeard;
        }

        /// <summary>Cancela o registro.</summary>
        private void OnDisable()
        {
            All.Remove(this);
            if (hearing != null) hearing.Heard -= OnHeard;
        }

        /// <summary>Posiciona no trajeto. (Ogros que só aparecem em trechos seguintes ficam desligados na cena
        /// e são ligados pelo <see cref="StealthSector"/>.)</summary>
        private void Start()
        {
            if (route != null) ResetToRoute();
            nextGrowl = Time.time + UnityEngine.Random.Range(4f, 10f);
        }

        // ================================================================== Atribuição e reinício

        /// <summary>
        /// Coloca o ogro num trajeto (ao começar um trecho).
        /// </summary>
        /// <param name="newRoute">Trajeto.</param>
        /// <param name="start">Ponto inicial.</param>
        /// <param name="warp">Teletransportar para o ponto inicial (fora da vista de John).</param>
        public void AssignRoute(PatrolRoute newRoute, int start, bool warp)
        {
            bool wasDormant = Dormant || !gameObject.activeSelf;
            route = newRoute;
            startPoint = start;
            SetDormant(false);
            if (warp || wasDormant) ResetToRoute();
        }

        /// <summary>
        /// Volta ao ponto inicial do trajeto, patrulhando e sem lembrar de nada (reinício do trecho).
        /// </summary>
        public void ResetToRoute()
        {
            if (route == null || route.Count == 0) return;
            int i = Mathf.Clamp(startPoint, 0, route.Count - 1);
            Vector3 pos = SampleNav(route.Point(i), 4f);
            if (agent != null && agent.isActiveAndEnabled) agent.Warp(pos);
            else transform.position = pos;
            Vector3 f = route.Facing(i) * Vector3.forward;
            f.y = 0f;
            if (f.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);
            waypoint = i;
            direction = 1;
            waiting = true;
            waitTimer = UnityEngine.Random.Range(0.5f, 1.5f);
            alertUntil = 0f;
            blockedTimer = 0f;
            roarUntil = 0f;
            lastSeen = -100f;
            if (vision != null) vision.ResetAwareness();
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.Rebind();
                animator.Update(0f);
            }
            SetState(State.Patrolling);
            Stop();
            if (indicator != null) indicator.Set(AlertIndicator.Mode.Hidden, 0f);
        }

        /// <summary>
        /// Liga/desliga o ogro (fora de cena enquanto o trecho dele não começou).
        /// </summary>
        /// <param name="dormant">Desligado.</param>
        public void SetDormant(bool dormant)
        {
            Dormant = dormant;
            if (gameObject.activeSelf == dormant) gameObject.SetActive(!dormant);
        }

        // ================================================================== Laço principal

        /// <summary>
        /// Percepção → estado → giro, animação, passos, sons e indicador.
        /// </summary>
        private void Update()
        {
            if (Dormant || agent == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (vision != null)
            {
                vision.sensitivity = Alert ? 1.4f : 1f;
                vision.Tick(dt);
            }
            faceTarget = false;
            turnSpeed = patrolTurn;

            switch (CurrentState)
            {
                case State.Patrolling: UpdatePatrol(dt); break;
                case State.Suspicious: UpdateSuspicious(dt); break;
                case State.Investigating: UpdateInvestigate(dt); break;
                case State.Chasing: UpdateChase(dt); break;
                case State.Returning: UpdateReturn(); break;
                case State.Attacking: UpdateAttack(dt); break;
                case State.Staggered: UpdateStagger(dt); break;
            }

            UpdateRotation(dt);
            UpdateAnimator();
            UpdateSteps(dt);
            UpdateAmbience();
            UpdateIndicator();
        }

        /// <summary>John está morto ou ausente.</summary>
        private static bool PlayerGone => PlayerStealth.Instance == null || PlayerStealth.Instance.IsDead;

        /// <summary>Limiar de suspeita (menor em alerta).</summary>
        private float SuspicionLevel => Alert ? suspicionThreshold * 0.7f : suspicionThreshold;

        /// <summary>
        /// Visão: atenção cheia → perseguição; acima do limiar → suspeita.
        /// </summary>
        /// <param name="allowSuspicion">Pode entrar em suspeita a partir do estado atual.</param>
        /// <returns>true se o estado mudou.</returns>
        private bool CheckVision(bool allowSuspicion)
        {
            if (vision == null || PlayerGone) return false;
            if (vision.Awareness >= 1f)
            {
                StartChase(vision.LastSeenPosition, true);
                return true;
            }
            if (allowSuspicion && vision.Awareness >= SuspicionLevel)
            {
                EnterSuspicious(vision.LastSeenPosition, true);
                return true;
            }
            return false;
        }

        // ================================================================== Patrulhando

        /// <summary>
        /// Anda de ponto em ponto; em cada ponto para e olha para a direção marcada.
        /// </summary>
        /// <param name="dt">Tempo do quadro.</param>
        private void UpdatePatrol(float dt)
        {
            if (CheckVision(true)) return;
            if (route == null || route.Count == 0) { Stop(); return; }
            if (waiting)
            {
                Stop();
                waitTimer -= dt;
                if (route.faceWaypointRotation)
                {
                    Vector3 f = route.Facing(waypoint) * Vector3.forward;
                    Face(transform.position + f * 3f, 70f);
                }
                if (waitTimer <= 0f)
                {
                    waiting = false;
                    waypoint = route.Next(waypoint, ref direction);
                }
                return;
            }
            Vector3 target = route.Point(waypoint);
            MoveTo(target, patrolSpeed);
            if (Reached(target))
            {
                waitTimer = route.Wait(waypoint);
                waiting = waitTimer > 0.01f;
                if (!waiting) waypoint = route.Next(waypoint, ref direction);
            }
        }

        // ================================================================== Suspeitando

        /// <summary>
        /// Percebeu algo: para, espera um instante e vira devagar para o estímulo.
        /// </summary>
        /// <param name="point">Onde foi o estímulo.</param>
        /// <param name="fromSight">Foi visual (senão, um ruído).</param>
        public void EnterSuspicious(Vector3 point, bool fromSight)
        {
            stimulus = point;
            FocusPoint = point;
            stimulusFromSight = fromSight;
            stateTimer = suspiciousTime + UnityEngine.Random.Range(0f, 0.5f);
            reactionTimer = reactionTime;
            bool was = CurrentState == State.Suspicious;
            SetState(State.Suspicious);
            Stop();
            if (was) return;
            PlayVoice(suspiciousClips, 0.85f);
            StealthUtil.SpawnAll(suspiciousEffects, HeadPosition + Vector3.up * 0.8f);
            StealthAudio a = StealthAudio.Instance;
            if (a != null && FlatDistance(PlayerPosition) < 22f) StealthAudio.Play2D(a.suspiciousStinger, 0.45f);
        }

        /// <summary>
        /// Encara o estímulo; se a atenção encher, persegue; ao fim do tempo, investiga (ou desiste se foi só um vulto).
        /// </summary>
        /// <param name="dt">Tempo do quadro.</param>
        private void UpdateSuspicious(float dt)
        {
            Stop();
            if (CheckVision(false)) return;
            if (vision != null && vision.CanSeeTarget && vision.Awareness >= SuspicionLevel) stimulus = vision.LastSeenPosition;
            FocusPoint = stimulus;
            reactionTimer -= dt;
            if (reactionTimer <= 0f) Face(stimulus, 100f);
            stateTimer -= dt;
            if (stateTimer > 0f) return;
            if (stimulusFromSight && vision != null && vision.Awareness < 0.12f)
            {
                // Foi só um vulto: resmunga e volta à patrulha.
                PlayVoice(growlClips, 0.6f);
                StartReturn();
            }
            else StartInvestigation(stimulus, Alert ? 2 : 1);
        }

        // ================================================================== Investigando

        /// <summary>
        /// Vai até a posição suspeita e procura em volta.
        /// </summary>
        /// <param name="point">Ponto.</param>
        /// <param name="searches">Pontos extras a vasculhar perto dali.</param>
        public void StartInvestigation(Vector3 point, int searches)
        {
            investigateTarget = SampleNav(point, 3f);
            FocusPoint = investigateTarget;
            lookPhase = false;
            searchesLeft = searches;
            alertUntil = Mathf.Max(alertUntil, Time.time + alertDuration * 0.5f);
            SetState(State.Investigating);
        }

        /// <summary>
        /// Anda até o ponto; lá fareja e olha em volta; depois vasculha pontos próximos e retorna.
        /// </summary>
        /// <param name="dt">Tempo do quadro.</param>
        private void UpdateInvestigate(float dt)
        {
            if (CheckVision(false)) return;
            if (vision != null && vision.CanSeeTarget && vision.Awareness >= SuspicionLevel)
            {
                investigateTarget = SampleNav(vision.LastSeenPosition, 3f);
                lookPhase = false;
            }
            FocusPoint = investigateTarget;
            if (!lookPhase)
            {
                MoveTo(investigateTarget, investigateSpeed);
                if (Reached(investigateTarget) || PathEnded())
                {
                    lookPhase = true;
                    lookTimer = lookAroundTime;
                    PlayVoice(sniffClips, 0.8f);
                    StealthUtil.SpawnAll(breathEffects, MouthPosition);
                }
                return;
            }
            Stop();
            lookTimer -= dt;
            float yaw = Mathf.Sin((Time.time + lookPhaseOffset) * 1.3f) * 70f;
            Vector3 look = Quaternion.Euler(0f, yaw, 0f) * (investigateTarget - transform.position + transform.forward * 0.1f);
            look.y = 0f;
            Face(transform.position + look.normalized * 3f, 60f);
            if (lookTimer > 0f) return;
            if (searchesLeft > 0)
            {
                searchesLeft--;
                investigateTarget = RandomNear(investigateTarget, searchRadius);
                lookPhase = false;
            }
            else
            {
                PlayVoice(frustratedClips, 0.6f);
                StartReturn();
            }
        }

        // ================================================================== Perseguindo

        /// <summary>
        /// Avistou John (ou ouviu um disparo): ruge, marca a posição e corre atrás dele.
        /// </summary>
        /// <param name="point">Posição de John (ou da origem do disparo).</param>
        /// <param name="roar">Rugir antes de correr.</param>
        public void StartChase(Vector3 point, bool roar)
        {
            bool was = CurrentState == State.Chasing;
            lastKnown = point;
            FocusPoint = point;
            lastSeen = Time.time;
            blockedTimer = 0f;
            alertUntil = Time.time + alertDuration;
            if (vision != null) vision.ForceDetect(point);
            SetState(State.Chasing);
            if (was) return;
            TimesAlerted++;
            if (PlayerStealth.Instance != null) PlayerStealth.Instance.TimesSpotted++;
            StealthUtil.SpawnAll(alertEffects, HeadPosition + Vector3.up * 0.9f);
            StealthAudio a = StealthAudio.Instance;
            if (a != null) StealthAudio.Play2D(a.spottedStinger, 0.7f);
            if (roar && Time.time >= roarCooldownUntil)
            {
                roarUntil = Time.time + roarDuration;
                roarCooldownUntil = Time.time + 8f;
                Trigger(RoarHash);
                PlayVoice(roarClips, 1f);
                if (ThirdPersonCamera.Instance != null && FlatDistance(PlayerPosition) < 15f) ThirdPersonCamera.Instance.Shake(0.4f, 0.08f);
            }
        }

        /// <summary>
        /// Corre até a última posição conhecida; ataca de perto; bufa na entrada de áreas seguras;
        /// desiste se perder John de vista por muito tempo.
        /// </summary>
        /// <param name="dt">Tempo do quadro.</param>
        private void UpdateChase(float dt)
        {
            PlayerStealth p = PlayerStealth.Instance;
            if (p == null || p.IsDead) { StartReturn(); return; }
            turnSpeed = chaseTurn;
            Vector3 pp = p.transform.position;
            if (vision != null && vision.CanSeeTarget)
            {
                lastKnown = pp;
                lastSeen = Time.time;
                vision.IgnoreHiding = true;
            }
            else if (Time.time - lastSeen < chaseMemory)
            {
                lastKnown = pp;
            }
            FocusPoint = lastKnown;

            // Rugido: para e encara antes de correr (dá um instante para John reagir).
            if (Time.time < roarUntil)
            {
                Stop();
                Face(pp, 200f);
                return;
            }

            // Área segura: chega o mais perto que consegue, bufa e acaba desistindo.
            if (p.InSafeZone)
            {
                blockedTimer += dt;
                MoveTo(lastKnown, chaseSpeed * 0.6f);
                if (PathEnded() || Reached(lastKnown)) { Stop(); Face(pp, 120f); }
                if (blockedTimer >= blockedTime)
                {
                    PlayVoice(frustratedClips, 1f);
                    StartInvestigation(transform.position + transform.forward * 2f, 1);
                }
                return;
            }
            blockedTimer = 0f;

            float dist = FlatDistance(pp);
            if (dist <= attackRange && (vision == null || vision.CanSeeTarget || dist < 1.6f))
            {
                StartAttack();
                return;
            }
            MoveTo(lastKnown, chaseSpeed);
            if (dist < 4f) Face(pp, chaseTurn);

            bool lost = Time.time - lastSeen > loseTime;
            bool arrivedBlind = (Reached(lastKnown) || PathEnded()) && Time.time - lastSeen > chaseMemory;
            if (lost || arrivedBlind)
            {
                PlayVoice(frustratedClips, 0.9f);
                StealthAudio a = StealthAudio.Instance;
                if (a != null) StealthAudio.Play2D(a.lostStinger, 0.5f);
                if (vision != null) vision.IgnoreHiding = false;
                StartInvestigation(lastKnown, 2);
            }
        }

        // ================================================================== Atacando

        /// <summary>
        /// Começa o golpe de clava (preparação visível antes do impacto).
        /// </summary>
        private void StartAttack()
        {
            attackTimer = 0f;
            hitDone = false;
            SetState(State.Attacking);
            Stop();
            Trigger(AttackHash);
            PlayVoice(attackClips, 1f);
        }

        /// <summary>
        /// Prepara (girando para John), golpeia e se recupera.
        /// </summary>
        /// <param name="dt">Tempo do quadro.</param>
        private void UpdateAttack(float dt)
        {
            Stop();
            attackTimer += dt;
            if (attackTimer < attackWindup * 0.8f && !PlayerGone) Face(PlayerPosition, 220f);
            if (!hitDone && attackTimer >= attackWindup)
            {
                hitDone = true;
                Smash();
            }
            if (attackTimer < attackWindup + attackRecover) return;
            if (PlayerGone) StartReturn();
            else
            {
                SetState(State.Chasing);
                lastSeen = Time.time;
            }
        }

        /// <summary>
        /// Impacto da clava: poeira, pedrinhas, som, tremor; se John estiver ao alcance e à frente, morre.
        /// </summary>
        private void Smash()
        {
            Vector3 c = attackPoint != null ? attackPoint.position : transform.position + transform.forward * 1.6f;
            Vector3 ground = new Vector3(c.x, transform.position.y + 0.05f, c.z);
            StealthUtil.SpawnAll(smashEffects, ground);
            AudioClip clip = StealthUtil.Pick(smashClips);
            if (clip != null && feet != null) feet.PlayOneShot(clip, 1f);
            float pd = FlatDistance(PlayerPosition);
            if (ThirdPersonCamera.Instance != null && pd < 12f) ThirdPersonCamera.Instance.Shake(0.35f, Mathf.Lerp(0.25f, 0.05f, pd / 12f));

            PlayerStealth p = PlayerStealth.Instance;
            if (p == null || p.IsDead || p.InSafeZone) return;
            Vector3 to = p.transform.position - transform.position;
            to.y = 0f;
            if (to.magnitude <= hitRange && Vector3.Angle(transform.forward, to) <= attackAngle)
                p.Die(transform);
        }

        // ================================================================== Atordoado

        /// <summary>
        /// Uma bala acertou: cambaleia e depois vai furioso até a origem do disparo.
        /// </summary>
        /// <param name="from">Origem do disparo.</param>
        private void Stagger(Vector3 from)
        {
            lastKnown = from;
            FocusPoint = from;
            lastSeen = Time.time;
            stateTimer = staggerTime;
            SetState(State.Staggered);
            Stop();
            Trigger(StaggerHash);
            PlayVoice(staggerClips, 1f);
        }

        /// <summary>
        /// Espera o cambaleio e persegue.
        /// </summary>
        /// <param name="dt">Tempo do quadro.</param>
        private void UpdateStagger(float dt)
        {
            Stop();
            stateTimer -= dt;
            if (stateTimer <= 0f) StartChase(lastKnown, true);
        }

        // ================================================================== Retornando

        /// <summary>
        /// Volta ao ponto mais próximo do trajeto.
        /// </summary>
        public void StartReturn()
        {
            if (vision != null) vision.IgnoreHiding = false;
            waypoint = route != null ? route.Closest(transform.position) : 0;
            SetState(State.Returning);
        }

        /// <summary>
        /// Caminha de volta ao trajeto (ainda atento) e retoma a patrulha.
        /// </summary>
        private void UpdateReturn()
        {
            if (CheckVision(true)) return;
            if (route == null || route.Count == 0) { SetState(State.Patrolling); return; }
            Vector3 t = route.Point(waypoint);
            FocusPoint = t;
            MoveTo(t, returnSpeed);
            if (Reached(t))
            {
                waiting = true;
                waitTimer = Mathf.Max(1f, route.Wait(waypoint));
                SetState(State.Patrolling);
            }
        }

        // ================================================================== Audição e dano

        /// <summary>
        /// Ouviu um ruído: disparo → perseguição imediata até a origem; pedra → investiga o ponto do impacto;
        /// passos/pouso → desconfia de uma posição aproximada.
        /// </summary>
        /// <param name="noise">Ruído.</param>
        /// <param name="strength">Intensidade percebida (0 a 1).</param>
        private void OnHeard(NoiseEvent noise, float strength)
        {
            if (Dormant || PlayerGone) return;
            if (CurrentState == State.Attacking) return;
            if (noise.type == NoiseType.Gunshot)
            {
                if (CurrentState == State.Staggered) { lastKnown = noise.position; return; }
                StartChase(noise.position, true);
                return;
            }
            Vector3 point = noise.type == NoiseType.Impact ? noise.position : Approximate(noise.position);
            if (noise.type == NoiseType.Impact && CurrentState != State.Chasing) StealthUtil.UnlockSkill("distracao");
            switch (CurrentState)
            {
                case State.Chasing:
                    if (noise.byPlayer && strength > 0.2f) { lastKnown = noise.position; lastSeen = Mathf.Max(lastSeen, Time.time - chaseMemory * 0.5f); }
                    break;
                case State.Staggered:
                    break;
                case State.Suspicious:
                    stimulus = point;
                    stimulusFromSight = false;
                    stateTimer = Mathf.Max(stateTimer, 1f);
                    break;
                case State.Investigating:
                    investigateTarget = SampleNav(point, 3f);
                    lookPhase = false;
                    searchesLeft = Mathf.Max(searchesLeft, 1);
                    break;
                default:
                    if (strength < 0.04f && noise.type != NoiseType.Impact) return;
                    if (vision != null) vision.AddAwareness(0.12f * strength);
                    EnterSuspicious(point, false);
                    break;
            }
        }

        /// <summary>
        /// Recebe um golpe: a vida é enorme (nunca zera). Bala → cambaleia e persegue a origem; pedra → desconfia
        /// de onde veio; golpe corpo a corpo → contra-ataca.
        /// </summary>
        /// <param name="info">Golpe.</param>
        /// <returns>true se o golpe foi aplicado.</returns>
        public bool TakeHit(DamageInfo info)
        {
            if (info.team == Team.Enemy) return false;
            Health = Mathf.Max(1f, Health - info.amount);
            StealthUtil.SpawnAll(hurtEffects, info.point);
            if (Dormant) return true;
            Vector3 from = info.attacker != null ? info.attacker.transform.position : PlayerPosition;
            switch (info.source)
            {
                case DamageSource.Bullet:
                    Stagger(from);
                    break;
                case DamageSource.Stone:
                    if (CurrentState != State.Chasing && CurrentState != State.Attacking && CurrentState != State.Staggered)
                    {
                        if (vision != null) vision.AddAwareness(0.5f);
                        EnterSuspicious(from, false);
                    }
                    break;
                default:
                    if (CurrentState != State.Attacking) StartChase(from, false);
                    break;
            }
            return true;
        }

        // ================================================================== Movimento

        /// <summary>
        /// Anda até um destino pelo NavMesh (recalcula só quando o destino muda bastante).
        /// </summary>
        /// <param name="destination">Destino.</param>
        /// <param name="speed">Velocidade.</param>
        private void MoveTo(Vector3 destination, float speed)
        {
            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
            agent.isStopped = false;
            agent.speed = speed;
            if ((destination - lastDestination).sqrMagnitude > 0.25f || Time.time >= nextRepath)
            {
                agent.SetDestination(destination);
                lastDestination = destination;
                nextRepath = Time.time + 0.5f;
            }
        }

        /// <summary>Para o agente.</summary>
        private void Stop()
        {
            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Chegou perto do ponto?
        /// </summary>
        /// <param name="target">Ponto.</param>
        /// <returns>true se chegou.</returns>
        private bool Reached(Vector3 target)
        {
            if (FlatDistance(target) < 0.7f) return true;
            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh || agent.pathPending) return false;
            return agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.35f && (lastDestination - target).sqrMagnitude < 1f;
        }

        /// <summary>
        /// O caminho terminou antes do destino (destino fora do NavMesh, por exemplo numa área segura).
        /// </summary>
        /// <returns>true se não há como chegar mais perto.</returns>
        private bool PathEnded()
        {
            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh || agent.pathPending) return false;
            return agent.pathStatus != NavMeshPathStatus.PathComplete && agent.remainingDistance <= agent.stoppingDistance + 0.5f;
        }

        /// <summary>
        /// Gira para um ponto neste quadro.
        /// </summary>
        /// <param name="point">Ponto.</param>
        /// <param name="speed">Graus por segundo.</param>
        private void Face(Vector3 point, float speed)
        {
            faceTarget = true;
            facePoint = point;
            turnSpeed = speed;
        }

        /// <summary>
        /// Gira o corpo suavemente (para o alvo ou para a direção do movimento).
        /// </summary>
        /// <param name="dt">Tempo do quadro.</param>
        private void UpdateRotation(float dt)
        {
            Vector3 dir = faceTarget ? facePoint - transform.position : (agent.isActiveAndEnabled ? agent.velocity : Vector3.zero);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), turnSpeed * dt);
        }

        /// <summary>
        /// Ponto válido do NavMesh mais próximo.
        /// </summary>
        /// <param name="p">Ponto.</param>
        /// <param name="radius">Raio de busca.</param>
        /// <returns>Ponto no NavMesh (ou o próprio ponto).</returns>
        private static Vector3 SampleNav(Vector3 p, float radius)
        {
            return NavMesh.SamplePosition(p, out NavMeshHit hit, radius, NavMesh.AllAreas) ? hit.position : p;
        }

        /// <summary>
        /// Ponto aleatório do NavMesh perto de um centro (vasculhar).
        /// </summary>
        /// <param name="center">Centro.</param>
        /// <param name="radius">Raio.</param>
        /// <returns>Ponto.</returns>
        private static Vector3 RandomNear(Vector3 center, float radius)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 r = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 p = center + new Vector3(r.x, 0f, r.y);
                if (NavMesh.SamplePosition(p, out NavMeshHit hit, 1.5f, NavMesh.AllAreas)) return hit.position;
            }
            return center;
        }

        /// <summary>
        /// Posição aproximada de um ruído (quanto mais longe, menos precisa).
        /// </summary>
        /// <param name="p">Origem real.</param>
        /// <returns>Posição estimada.</returns>
        private Vector3 Approximate(Vector3 p)
        {
            float err = 0.8f + 0.12f * FlatDistance(p);
            Vector2 r = UnityEngine.Random.insideUnitCircle * err;
            return p + new Vector3(r.x, 0f, r.y);
        }

        // ================================================================== Animação, passos, sons e indicador

        /// <summary>
        /// Velocidade e "modo" (0 normal, 1 desconfiado, 2 farejando/procurando) para o Animator.
        /// </summary>
        private void UpdateAnimator()
        {
            if (animator == null) return;
            Vector3 v = agent.isActiveAndEnabled ? agent.velocity : Vector3.zero;
            float speed = new Vector3(v.x, 0f, v.z).magnitude;
            if (animatorParams.Contains(SpeedHash)) animator.SetFloat(SpeedHash, speed, 0.15f, Time.deltaTime);
            int mode = 0;
            if (CurrentState == State.Suspicious || (CurrentState == State.Chasing && blockedTimer > 0.5f)) mode = 1;
            else if (CurrentState == State.Investigating && lookPhase) mode = 2;
            if (animatorParams.Contains(ModeHash)) animator.SetInteger(ModeHash, mode);
        }

        /// <summary>
        /// Passos pesados: som, poeira e o chão tremendo quando John está perto.
        /// </summary>
        /// <param name="dt">Tempo do quadro.</param>
        private void UpdateSteps(float dt)
        {
            Vector3 v = agent.isActiveAndEnabled ? agent.velocity : Vector3.zero;
            float speed = new Vector3(v.x, 0f, v.z).magnitude;
            if (speed < 0.3f) { stepTimer = 0.1f; return; }
            stepTimer -= dt;
            if (stepTimer > 0f) return;
            stepTimer = speed > patrolSpeed * 1.8f ? stepInterval * 0.55f : stepInterval;
            stepLeft = !stepLeft;
            AudioClip clip = StealthUtil.Pick(stepClips);
            if (clip != null && feet != null)
            {
                feet.pitch = UnityEngine.Random.Range(0.9f, 1.05f);
                feet.PlayOneShot(clip, CurrentState == State.Chasing ? 1f : 0.75f);
            }
            Vector3 foot = transform.position + transform.right * (stepLeft ? -0.4f : 0.4f) + Vector3.up * 0.05f;
            StealthUtil.SpawnRandom(stepEffects, foot);
            float pd = FlatDistance(PlayerPosition);
            if (ThirdPersonCamera.Instance != null && pd < 9f)
                ThirdPersonCamera.Instance.Shake(0.12f, 0.035f * (1f - pd / 9f) * (CurrentState == State.Chasing ? 1.6f : 1f));
        }

        /// <summary>
        /// Respiração (mais forte ao perseguir), rosnados ocasionais na patrulha e bafo no ar frio.
        /// </summary>
        private void UpdateAmbience()
        {
            bool chasing = CurrentState == State.Chasing || CurrentState == State.Attacking;
            if (breath != null)
            {
                breath.volume = Mathf.MoveTowards(breath.volume, chasing ? 0.9f : 0.45f, Time.deltaTime);
                breath.pitch = Mathf.MoveTowards(breath.pitch, chasing ? 1.25f : 1f, Time.deltaTime * 0.5f);
            }
            if (Time.time >= nextGrowl)
            {
                nextGrowl = Time.time + UnityEngine.Random.Range(8f, 15f);
                if (CurrentState == State.Patrolling || CurrentState == State.Returning) PlayVoice(growlClips, 0.7f);
            }
            if (Time.time >= nextBreathPuff)
            {
                nextBreathPuff = Time.time + UnityEngine.Random.Range(2.5f, 4f) * (chasing ? 0.5f : 1f);
                StealthUtil.SpawnRandom(breathEffects, MouthPosition, mouth);
            }
        }

        /// <summary>
        /// "?" / "!" e barra de atenção sobre a cabeça.
        /// </summary>
        private void UpdateIndicator()
        {
            if (indicator == null) return;
            float a = Awareness;
            switch (CurrentState)
            {
                case State.Chasing:
                case State.Attacking:
                case State.Staggered:
                    indicator.Set(AlertIndicator.Mode.Alarmed, 1f);
                    break;
                case State.Suspicious:
                    indicator.Set(AlertIndicator.Mode.Suspicious, a);
                    break;
                case State.Investigating:
                    indicator.Set(AlertIndicator.Mode.Searching, a);
                    break;
                default:
                    indicator.Set(a > 0.04f ? AlertIndicator.Mode.Watching : AlertIndicator.Mode.Hidden, a);
                    break;
            }
        }

        /// <summary>
        /// Toca um som sorteado na "voz" do ogro.
        /// </summary>
        /// <param name="clips">Vetor de sons.</param>
        /// <param name="volume">Volume.</param>
        private void PlayVoice(AudioClip[] clips, float volume)
        {
            AudioClip c = StealthUtil.Pick(clips);
            if (c == null || voice == null) return;
            voice.pitch = UnityEngine.Random.Range(0.92f, 1.05f);
            voice.PlayOneShot(c, volume);
        }

        /// <summary>
        /// Dispara um gatilho do Animator (se existir).
        /// </summary>
        /// <param name="hash">Gatilho.</param>
        private void Trigger(int hash)
        {
            if (animator != null && animatorParams.Contains(hash)) animator.SetTrigger(hash);
        }

        /// <summary>
        /// Troca de estado (avisa o HUD).
        /// </summary>
        /// <param name="s">Novo estado.</param>
        private void SetState(State s)
        {
            if (s == CurrentState) return;
            State old = CurrentState;
            CurrentState = s;
            StateChanged?.Invoke(this, old, s);
        }

        /// <summary>Posição de John (ou a do próprio ogro, se não houver jogador).</summary>
        private Vector3 PlayerPosition => PlayerStealth.Instance != null ? PlayerStealth.Instance.transform.position : transform.position;
        /// <summary>Altura da cabeça.</summary>
        private Vector3 HeadPosition => vision != null && vision.eye != null ? vision.eye.position : transform.position + Vector3.up * 2.4f;
        /// <summary>Boca (bafo).</summary>
        private Vector3 MouthPosition => mouth != null ? mouth.position : HeadPosition + transform.forward * 0.4f;

        /// <summary>
        /// Distância horizontal até um ponto.
        /// </summary>
        /// <param name="p">Ponto.</param>
        /// <returns>Distância.</returns>
        private float FlatDistance(Vector3 p)
        {
            return StealthUtil.FlatDistance(transform.position, p);
        }

        /// <summary>
        /// Nome do estado em português (HUD).
        /// </summary>
        /// <param name="s">Estado.</param>
        /// <returns>Texto.</returns>
        public static string StateName(State s)
        {
            switch (s)
            {
                case State.Patrolling: return "patrulhando";
                case State.Suspicious: return "desconfiado";
                case State.Investigating: return "investigando";
                case State.Chasing: return "PERSEGUINDO";
                case State.Returning: return "retornando";
                case State.Attacking: return "ATACANDO";
                default: return "cambaleando";
            }
        }

        /// <summary>Zera a lista global (nova partida).</summary>
        public static void ResetStatics()
        {
            All.Clear();
        }
    }
}
