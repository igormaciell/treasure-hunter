using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Inimigo de curta distância: o esqueleto da faca (rápido, ataque curto) e o esqueleto brutamontes
    /// (lento, machado pesado, golpe largo que tira 2 corações e não é interrompido por tiros).
    /// Persegue John pelo NavMesh e, ao chegar perto, prepara o golpe (sinal visível e sonoro), golpeia
    /// e se recupera. Um golpe corpo a corpo ou uma pedra durante a preparação interrompe o ataque.
    /// </summary>
    public class MeleeEnemy : EnemyBase
    {
        private enum Phase { Chase, Windup, Recover }

        [Header("Ataque corpo a corpo")]
        public float attackRange = 1.6f;
        public float attackWindup = 0.45f;
        public float attackRecover = 0.55f;
        public float attackCooldown = 1.0f;
        public int damage = 1;
        public float hitRadius = 1.0f;
        public float hitForce = 6f;
        /// <summary>Giro durante a preparação (mais lento: dá para desviar para o lado).</summary>
        public float windupTurnSpeed = 220f;
        /// <summary>Som extra do golpe (machado).</summary>
        public AudioClip swingClip;

        private Phase phase = Phase.Chase;
        private float phaseEnd;
        private float nextAttack;

        /// <summary>
        /// Persegue, prepara o golpe quando está perto, golpeia e se recupera.
        /// </summary>
        protected override void UpdateCombat()
        {
            if (player == null) return;
            float dist = FlatDistanceToPlayer;

            switch (phase)
            {
                case Phase.Chase:
                    if (dist <= attackRange && Time.time >= nextAttack && canSeePlayer)
                    {
                        StartWindup();
                        break;
                    }
                    if (dist <= attackRange * 0.75f)
                    {
                        // Perto demais: espera o próximo ataque virado para John.
                        StopAgent();
                        Face(PlayerChest, turnSpeed);
                    }
                    else
                    {
                        float speed = dist > 3.5f ? runSpeed : Mathf.Lerp(walkSpeed, runSpeed, 0.5f);
                        MoveTo(ChaseTarget(), speed);
                    }
                    break;

                case Phase.Windup:
                    StopAgent();
                    Face(PlayerChest, windupTurnSpeed);
                    if (Time.time >= phaseEnd)
                    {
                        DoHit();
                        phase = Phase.Recover;
                        phaseEnd = Time.time + attackRecover;
                    }
                    break;

                case Phase.Recover:
                    StopAgent();
                    if (Time.time >= phaseEnd)
                    {
                        phase = Phase.Chase;
                        nextAttack = Time.time + attackCooldown;
                    }
                    break;
            }
        }

        /// <summary>
        /// Para onde correr: John, se estiver visível ou tiver sido visto há pouco; senão o último lugar onde foi ouvido.
        /// Como a sala está fechada, depois de alguns segundos o esqueleto "fareja" John e vai direto a ele.
        /// </summary>
        /// <returns>Destino da perseguição.</returns>
        private Vector3 ChaseTarget()
        {
            if (canSeePlayer || Time.time - lastSeenTime > 3f) return player.position;
            return lastKnownPlayerPos;
        }

        /// <summary>
        /// Começa a preparação do golpe: animação de ataque e som.
        /// </summary>
        private void StartWindup()
        {
            phase = Phase.Windup;
            phaseEnd = Time.time + attackWindup;
            TriggerAttackAnimation();
            CombatAudio.PlayRandom(attackClips, transform.position + Vector3.up, 0.9f);
        }

        /// <summary>
        /// Aplica o golpe se John ainda estiver no alcance, à frente do inimigo.
        /// </summary>
        private void DoHit()
        {
            Vector3 center = transform.position + Vector3.up * 1.0f + transform.forward * (attackRange * 0.7f);
            CombatAudio a = CombatAudio.Instance;
            if (swingClip != null) CombatAudio.Play(swingClip, center, 0.9f);
            else if (a != null) CombatAudio.Play(a.swingWhoosh, center, 0.6f);

            if (player == null || playerHealth == null || playerHealth.Current <= 0) return;
            Vector3 chest = PlayerChest;
            if ((chest - center).magnitude > hitRadius + 0.4f) return;
            IDamageable target = player.GetComponent<IDamageable>();
            if (target != null)
                target.TakeHit(new DamageInfo(damage, chest, transform.forward, hitForce, DamageSource.EnemyMelee, Team.Enemy, gameObject));
            else
                playerHealth.TakeDamage(damage, transform.position);
        }

        /// <summary>
        /// Um atordoamento durante a preparação cancela o golpe.
        /// </summary>
        protected override void OnStunned()
        {
            if (phase == Phase.Windup)
            {
                phase = Phase.Recover;
                phaseEnd = Time.time + 0.2f;
            }
        }

        /// <summary>
        /// Gira para um ponto com uma velocidade de giro específica.
        /// </summary>
        /// <param name="point">Ponto alvo.</param>
        /// <param name="speed">Graus por segundo.</param>
        private void Face(Vector3 point, float speed)
        {
            faceTarget = true;
            faceTargetPoint = point;
            currentTurnSpeed = speed;
        }
    }
}
