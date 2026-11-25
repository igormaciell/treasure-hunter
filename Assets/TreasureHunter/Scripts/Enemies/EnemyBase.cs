using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace TreasureHunter
{
    /// <summary>
    /// Base de todos os inimigos do Nível 2 (esqueletos). Cuida do que é comum:
    /// vida, percepção (visão com campo de visão e linha de visada, audição de tiros e barulhos),
    /// movimento pelo NavMesh (NavMeshAgent), giro, animações (parado/andando/correndo, ataque, dano e morte),
    /// atordoamento e empurrão ao levar golpes, morte com efeitos, sons e item sorteado (EnemyLoot).
    /// As subclasses definem o comportamento de combate conforme a arma: faca/machado (MeleeEnemy) ou arco (RangedEnemy).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        /// <summary>Inimigos ativos na cena.</summary>
        public static readonly List<EnemyBase> All = new List<EnemyBase>();
        /// <summary>Disparado sempre que um inimigo recebe dano (inimigo, golpe, morreu?).</summary>
        public static event Action<EnemyBase, DamageInfo, bool> Damaged;
        /// <summary>Total de inimigos derrotados no nível.</summary>
        public static int TotalKilled { get; private set; }

        [Header("Identificação")]
        public string displayName = "Esqueleto";

        [Header("Vida e reação")]
        public float maxHealth = 3f;
        /// <summary>Atordoamento causado por tiro (0 = tiros não interrompem; usado no brutamontes).</summary>
        public float bulletStun = 0.25f;
        /// <summary>Multiplicador do atordoamento dos golpes corpo a corpo.</summary>
        public float meleeStunMultiplier = 1f;
        /// <summary>Multiplicador do empurrão recebido.</summary>
        public float knockbackScale = 1f;

        [Header("Percepção")]
        public float sightRange = 20f;
        public float sightAngle = 150f;
        public float eyeHeight = 1.6f;
        public float allyAlertRadius = 10f;

        [Header("Movimento")]
        public float walkSpeed = 1.3f;
        public float runSpeed = 3.4f;
        public float turnSpeed = 480f;
        /// <summary>Duração da animação de "levantar do chão" ao surgir.</summary>
        public float spawnDuration = 1.4f;

        [Header("Referências")]
        public Animator animator;
        public Collider[] hitColliders;
        public EnemyLoot loot;

        [Header("Efeitos (vetores de prefabs)")]
        public GameObject[] hurtEffects;
        public GameObject[] deathEffects;
        public GameObject spawnEffect;

        [Header("Sons")]
        public AudioClip[] alertClips;
        public AudioClip[] attackClips;
        public AudioClip[] hurtClips;
        public AudioClip[] deathClips;
        public AudioClip[] stepClips;
        public float stepInterval = 0.5f;

        /// <summary>Tempo que o corpo fica no chão antes de afundar e sumir.</summary>
        public float corpseTime = 6f;

        /// <summary>Disparado quando o inimigo morre.</summary>
        public event Action<EnemyBase> Died;

        /// <summary>Vida atual.</summary>
        public float Health { get; protected set; }
        /// <summary>Vivo até a vida acabar.</summary>
        public bool IsAlive => !dead;
        /// <summary>Lado dos inimigos.</summary>
        public Team Team => Team.Enemy;
        /// <summary>Sala que criou o inimigo.</summary>
        public CombatRoom Room { get; set; }
        /// <summary>Se verdadeiro, já surge sabendo onde John está; senão fica de guarda até ver ou ouvir algo.</summary>
        public bool StartAlerted { get; set; } = true;
        /// <summary>O inimigo já percebeu John.</summary>
        public bool Alerted => alerted;

        protected NavMeshAgent agent;
        protected Transform player;
        protected Rigidbody playerBody;
        protected PlayerHealth playerHealth;
        protected bool alerted;
        protected bool dead;
        protected bool spawning = true;
        protected float stunUntil;
        protected Vector3 lastKnownPlayerPos;
        protected float lastSeenTime = -100f;
        protected bool canSeePlayer;
        /// <summary>Quando verdadeiro neste quadro, o inimigo gira para faceTargetPoint em vez da direção do movimento.</summary>
        protected bool faceTarget;
        protected Vector3 faceTargetPoint;
        protected float currentTurnSpeed;

        private float spawnEnd;
        private float nextSightCheck;
        private float stepTimer;
        private float nextRepath;
        private Vector3 lastDestination = new Vector3(9999f, 9999f, 9999f);
        private bool announced;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        // ================================================================== Ciclo de vida

        /// <summary>
        /// Configura o agente de navegação (o giro é feito pelo script).
        /// </summary>
        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
            agent.speed = walkSpeed;
            Health = maxHealth;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (loot == null) loot = GetComponent<EnemyLoot>();
        }

        /// <summary>Registra o inimigo na lista global.</summary>
        protected virtual void OnEnable()
        {
            All.Add(this);
        }

        /// <summary>Remove o inimigo da lista global.</summary>
        protected virtual void OnDisable()
        {
            All.Remove(this);
        }

        /// <summary>
        /// Encontra o jogador e começa a animação de surgir do chão.
        /// </summary>
        protected virtual void Start()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.player != null)
            {
                player = gm.player.transform;
                playerBody = gm.player.GetComponent<Rigidbody>();
                playerHealth = gm.playerHealth;
            }
            spawnEnd = Time.time + spawnDuration;
            lastKnownPlayerPos = player != null ? player.position : transform.position;
            if (spawnEffect != null) Instantiate(spawnEffect, transform.position, Quaternion.identity);
            CombatAudio a = CombatAudio.Instance;
            if (a != null) CombatAudio.Play(a.skeletonRise, transform.position, 0.8f);
        }

        /// <summary>
        /// Laço principal: surgimento, percepção, atordoamento, combate (subclasse), giro, animação e passos.
        /// </summary>
        protected virtual void Update()
        {
            if (dead) return;
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            {
                StopAgent();
                SetSpeedParam(0f);
                return;
            }

            if (spawning)
            {
                StopAgent();
                if (Time.time < spawnEnd) return;
                spawning = false;
                // Quem surge alerta já vem atrás de John, mas não acorda os vizinhos de guarda.
                if (StartAlerted) BecomeAlerted(false);
            }

            Perceive();
            faceTarget = false;
            currentTurnSpeed = turnSpeed;

            if (Time.time < stunUntil) StopAgent();
            else if (alerted) UpdateCombat();
            else UpdateIdle();

            UpdateRotation();
            UpdateAnimatorAndSteps();
        }

        // ================================================================== Comportamento

        /// <summary>
        /// Comportamento de combate (definido pela arma do inimigo).
        /// </summary>
        protected abstract void UpdateCombat();

        /// <summary>
        /// De guarda: parado, olhando ao redor devagar, até ver ou ouvir John.
        /// </summary>
        protected virtual void UpdateIdle()
        {
            StopAgent();
            faceTarget = true;
            currentTurnSpeed = 25f;
            float yaw = Mathf.Sin(Time.time * 0.4f + GetInstanceID()) * 50f;
            faceTargetPoint = transform.position + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 3f;
        }

        /// <summary>
        /// Chamado quando o inimigo é atordoado (interrompe ataques em preparação).
        /// </summary>
        protected virtual void OnStunned() { }

        /// <summary>
        /// Chamado na primeira vez que o inimigo percebe John.
        /// </summary>
        protected virtual void OnAlerted() { }

        // ================================================================== Percepção

        /// <summary>
        /// Verifica (algumas vezes por segundo) se John está visível e atualiza a última posição conhecida.
        /// </summary>
        private void Perceive()
        {
            if (Time.time < nextSightCheck) return;
            nextSightCheck = Time.time + 0.15f;
            canSeePlayer = CheckSight();
            if (canSeePlayer)
            {
                lastKnownPlayerPos = player.position;
                lastSeenTime = Time.time;
                if (!alerted) BecomeAlerted();
            }
        }

        /// <summary>
        /// Visão: alcance, campo de visão (só antes de ser alertado) e linha de visada sem obstáculos.
        /// </summary>
        /// <returns>true se John está visível.</returns>
        protected bool CheckSight()
        {
            if (player == null || playerHealth == null || playerHealth.Current <= 0) return false;
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 target = PlayerChest;
            Vector3 to = target - eye;
            if (to.magnitude > sightRange) return false;
            if (!alerted)
            {
                Vector3 flat = new Vector3(to.x, 0f, to.z);
                if (Vector3.Angle(transform.forward, flat) > sightAngle * 0.5f) return false;
            }
            return HasLineOfSight(eye, target);
        }

        /// <summary>
        /// Linha de visada entre dois pontos (só o cenário bloqueia).
        /// </summary>
        /// <param name="from">Origem.</param>
        /// <param name="to">Destino.</param>
        /// <returns>true se nada do cenário está no caminho.</returns>
        public static bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            return !Physics.Linecast(from, to, CombatLayers.SightMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Torna o inimigo alerta (grito na primeira vez) e avisa os aliados próximos.
        /// </summary>
        /// <param name="propagate">true para avisar os aliados de guarda dentro do raio.</param>
        protected void BecomeAlerted(bool propagate = true)
        {
            alerted = true;
            if (!announced)
            {
                announced = true;
                CombatAudio.PlayRandom(alertClips, transform.position + Vector3.up, 0.9f);
                OnAlerted();
            }
            if (!propagate) return;
            foreach (EnemyBase e in All.ToArray())
            {
                if (e == null || e == this || !e.IsAlive || e.alerted || e.spawning) continue;
                if ((e.transform.position - transform.position).sqrMagnitude <= allyAlertRadius * allyAlertRadius)
                    e.Hear(lastKnownPlayerPos);
            }
        }

        /// <summary>
        /// Ouve um barulho (tiro, explosão, pedra): fica alerta e vai conferir.
        /// </summary>
        /// <param name="position">Origem do barulho.</param>
        public void Hear(Vector3 position)
        {
            if (dead) return;
            if (!canSeePlayer) lastKnownPlayerPos = position;
            if (spawning) { StartAlerted = true; return; }
            if (!alerted) BecomeAlerted();
        }

        /// <summary>
        /// Avisa todos os inimigos dentro de um raio sobre um barulho.
        /// </summary>
        /// <param name="position">Origem do barulho.</param>
        /// <param name="radius">Raio.</param>
        public static void MakeNoise(Vector3 position, float radius)
        {
            float r2 = radius * radius;
            foreach (EnemyBase e in All.ToArray())
            {
                if (e != null && e.IsAlive && (e.transform.position - position).sqrMagnitude <= r2) e.Hear(position);
            }
        }

        /// <summary>Ponto do peito de John (alvo de visão e ataques).</summary>
        protected Vector3 PlayerChest => player != null ? player.position + Vector3.up * 1.2f : transform.position;

        /// <summary>Distância horizontal até John.</summary>
        protected float FlatDistanceToPlayer
        {
            get
            {
                if (player == null) return float.MaxValue;
                Vector3 d = player.position - transform.position;
                d.y = 0f;
                return d.magnitude;
            }
        }

        // ================================================================== Movimento

        /// <summary>
        /// Anda/corre até um destino pelo NavMesh (recalcula o caminho só quando o destino muda bastante).
        /// </summary>
        /// <param name="destination">Destino.</param>
        /// <param name="speed">Velocidade.</param>
        protected void MoveTo(Vector3 destination, float speed)
        {
            if (!agent.enabled || !agent.isOnNavMesh) return;
            agent.isStopped = false;
            agent.speed = speed;
            if ((destination - lastDestination).sqrMagnitude > 0.36f || Time.time >= nextRepath)
            {
                agent.SetDestination(destination);
                lastDestination = destination;
                nextRepath = Time.time + 0.4f;
            }
        }

        /// <summary>
        /// Para o agente imediatamente.
        /// </summary>
        protected void StopAgent()
        {
            if (!agent.enabled || !agent.isOnNavMesh) return;
            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Chegou ao destino atual?
        /// </summary>
        /// <returns>true se o caminho terminou.</returns>
        protected bool ReachedDestination()
        {
            if (!agent.enabled || !agent.isOnNavMesh) return true;
            return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.35f;
        }

        /// <summary>
        /// Gira o corpo para o alvo (ataques) ou para a direção do movimento.
        /// </summary>
        private void UpdateRotation()
        {
            Vector3 dir;
            if (faceTarget) dir = faceTargetPoint - transform.position;
            else dir = agent.enabled ? agent.velocity : Vector3.zero;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), currentTurnSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Envia a velocidade ao Animator e toca passos de ossos.
        /// </summary>
        private void UpdateAnimatorAndSteps()
        {
            Vector3 v = agent.enabled ? agent.velocity : Vector3.zero;
            float speed = new Vector3(v.x, 0f, v.z).magnitude;
            SetSpeedParam(speed);
            if (speed < 0.4f) { stepTimer = 0f; return; }
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                stepTimer = speed > runSpeed * 0.7f ? stepInterval * 0.6f : stepInterval;
                CombatAudio.PlayRandom(stepClips, transform.position, 0.35f);
            }
        }

        /// <summary>
        /// Atualiza o parâmetro de velocidade do Animator (blend tree parado/andando/correndo).
        /// </summary>
        /// <param name="speed">Velocidade horizontal.</param>
        private void SetSpeedParam(float speed)
        {
            if (animator != null) animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
        }

        /// <summary>
        /// Dispara a animação de ataque.
        /// </summary>
        protected void TriggerAttackAnimation()
        {
            if (animator != null) animator.SetTrigger(AttackHash);
        }

        // ================================================================== Dano e morte

        /// <summary>
        /// Recebe um golpe: perde vida, solta lascas de osso, fica alerta, é atordoado e empurrado; morre se a vida acabar.
        /// Golpes de outros inimigos são ignorados (sem fogo amigo).
        /// </summary>
        /// <param name="info">Dados do golpe.</param>
        /// <returns>true se o dano foi aplicado.</returns>
        public bool TakeHit(DamageInfo info)
        {
            if (dead || info.team == Team.Enemy) return false;
            Health -= info.amount;
            Projectile.SpawnEffects(hurtEffects, info.point, -info.direction);
            if (info.attacker != null && info.team == Team.Player) lastKnownPlayerPos = info.attacker.transform.position;
            if (spawning) { StartAlerted = true; }
            else if (!alerted) BecomeAlerted();

            bool killed = Health <= 0f;
            Damaged?.Invoke(this, info, killed);
            if (killed)
            {
                Die(info);
                return true;
            }

            CombatAudio.PlayRandom(hurtClips, transform.position + Vector3.up, 0.85f);
            float stun = StunFor(info);
            if (stun > 0f)
            {
                stunUntil = Mathf.Max(stunUntil, Time.time + stun);
                if (animator != null) animator.SetTrigger(HurtHash);
                OnStunned();
            }
            if (info.force > 0f && !spawning) StartCoroutine(Knockback(info.direction * info.force * knockbackScale));
            return true;
        }

        /// <summary>
        /// Tempo de atordoamento conforme a origem do golpe.
        /// </summary>
        /// <param name="info">Golpe.</param>
        /// <returns>Segundos de atordoamento.</returns>
        protected virtual float StunFor(DamageInfo info)
        {
            switch (info.source)
            {
                case DamageSource.Bullet: return bulletStun;
                case DamageSource.PistolWhip: return 0.75f * meleeStunMultiplier;
                case DamageSource.Knife: return 0.4f * meleeStunMultiplier;
                case DamageSource.Stone: return 1.6f;
                case DamageSource.Explosion: return 1.2f;
                case DamageSource.Trap: return 0.8f;
                default: return 0.3f;
            }
        }

        /// <summary>
        /// Empurrão curto ao longo do NavMesh (diminui até parar).
        /// </summary>
        /// <param name="velocity">Velocidade inicial do empurrão.</param>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator Knockback(Vector3 velocity)
        {
            velocity.y = 0f;
            const float duration = 0.2f;
            float t = 0f;
            while (t < duration && !dead)
            {
                t += Time.deltaTime;
                if (agent.enabled && agent.isOnNavMesh) agent.Move(velocity * (1f - t / duration) * Time.deltaTime);
                yield return null;
            }
        }

        /// <summary>
        /// Morte: para o agente, desliga os colisores, toca a animação de desmoronar, efeitos, som e o item sorteado.
        /// </summary>
        /// <param name="info">Golpe fatal.</param>
        protected virtual void Die(DamageInfo info)
        {
            dead = true;
            Health = 0f;
            StopAllCoroutines();
            if (agent.enabled && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
            agent.enabled = false;
            if (hitColliders != null) foreach (Collider c in hitColliders) if (c != null) c.enabled = false;
            if (animator != null) animator.SetBool(DeadHash, true);

            Projectile.SpawnEffects(deathEffects, transform.position + Vector3.up * 0.8f, Vector3.up);
            CombatAudio.PlayRandom(deathClips, transform.position + Vector3.up, 1f);
            TotalKilled++;
            if (loot != null) loot.TryDrop(transform.position, Room);
            OnDied();
            Died?.Invoke(this);
            StartCoroutine(Despawn());
        }

        /// <summary>
        /// Chamado na morte (as subclasses liberam recursos, como pontos de ataque reservados).
        /// </summary>
        protected virtual void OnDied() { }

        /// <summary>
        /// Deixa o corpo no chão por um tempo, afunda devagar e remove o objeto.
        /// </summary>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator Despawn()
        {
            yield return new WaitForSeconds(corpseTime);
            Vector3 start = transform.position;
            float t = 0f;
            while (t < 1.5f)
            {
                t += Time.deltaTime;
                transform.position = start + Vector3.down * (t / 1.5f) * 0.9f;
                yield return null;
            }
            Destroy(gameObject);
        }

        /// <summary>
        /// Zera os contadores globais (início de uma nova partida do nível).
        /// </summary>
        public static void ResetStatics()
        {
            TotalKilled = 0;
            All.Clear();
        }
    }
}
