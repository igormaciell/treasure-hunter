using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace TreasureHunter
{
    /// <summary>
    /// Esqueleto arqueiro: inimigo de longa distância.
    /// - Mantém distância: recua se John chegar perto demais.
    /// - Procura posições de ataque: escolhe, entre os pontos de ataque da sala, o melhor lugar com linha de visada
    ///   até John, na distância preferida, de preferência pelos lados/atrás dele (para surpreendê-lo) e livre de outros arqueiros.
    /// - Se John se esconde atrás de uma cobertura, troca de posição para flanqueá-lo.
    /// - Puxa a corda (sinal visível e sonoro) e dispara uma flecha física com trajetória em arco,
    ///   calculando o ângulo de lançamento e antecipando o movimento de John.
    /// </summary>
    public class RangedEnemy : EnemyBase
    {
        private enum Phase { Reposition, Draw, Recover }

        /// <summary>Vetor de prefabs de flechas (uma é sorteada a cada disparo).</summary>
        [Header("Arco")]
        public GameObject[] arrowPrefabs;
        public Transform arrowSpawn;
        public float arrowSpeed = 22f;
        public float arrowDamage = 1f;
        public float drawTime = 0.9f;
        public float shotCooldown = 1.6f;
        public int shotsBeforeRelocate = 2;
        public float leadFactor = 0.5f;
        public float inaccuracy = 1.5f;

        [Header("Distância e posicionamento")]
        public float preferredMin = 7f;
        public float preferredMax = 15f;
        public float retreatDistance = 4f;
        public float hiddenRelocateTime = 1.5f;
        /// <summary>Pontos de ataque da sala (preenchidos pela sala ao criar o arqueiro).</summary>
        public Transform[] attackPoints;

        private static readonly Dictionary<Transform, RangedEnemy> reserved = new Dictionary<Transform, RangedEnemy>();

        private Phase phase = Phase.Reposition;
        private float phaseEnd;
        private float nextShot;
        private int shotsSinceMove;
        private float hiddenSince = -1f;
        private bool hasDestination;
        private Vector3 destination;
        private Transform currentPoint;
        private float nextPick;
        private float nextRetreat;

        /// <summary>
        /// Esquece todas as reservas de pontos (início do nível).
        /// </summary>
        public static void ClearReservations()
        {
            reserved.Clear();
        }

        /// <summary>
        /// Comportamento à distância: reposicionar, puxar a corda, disparar e se recuperar.
        /// </summary>
        protected override void UpdateCombat()
        {
            if (player == null) return;
            float dist = FlatDistanceToPlayer;
            bool los = canSeePlayer;

            // Contagem do tempo em que John está escondido (atrás de cobertura).
            if (los) hiddenSince = -1f;
            else if (hiddenSince < 0f) hiddenSince = Time.time;
            bool hiddenTooLong = hiddenSince >= 0f && Time.time - hiddenSince > hiddenRelocateTime;

            switch (phase)
            {
                case Phase.Reposition:
                    if (dist < retreatDistance) { Retreat(); }
                    else if (!hasDestination || ReachedDestination())
                    {
                        hasDestination = false;
                        if (los && dist <= preferredMax + 4f && Time.time >= nextShot) { StartDraw(); break; }
                        if (!los || hiddenTooLong || dist > preferredMax + 4f) PickNewPoint();
                        else { StopAgent(); FacePlayer(); }
                    }
                    if (hasDestination) MoveTo(destination, runSpeed);
                    break;

                case Phase.Draw:
                    StopAgent();
                    FacePlayer();
                    if (dist < retreatDistance * 0.7f) { phase = Phase.Reposition; Retreat(); break; }
                    if (hiddenTooLong) { phase = Phase.Reposition; PickNewPoint(); break; }
                    if (Time.time >= phaseEnd)
                    {
                        Fire();
                        shotsSinceMove++;
                        nextShot = Time.time + shotCooldown;
                        phase = Phase.Recover;
                        phaseEnd = Time.time + 0.45f;
                    }
                    break;

                case Phase.Recover:
                    StopAgent();
                    FacePlayer();
                    if (Time.time < phaseEnd) break;
                    if (shotsSinceMove >= shotsBeforeRelocate || hiddenTooLong || dist < retreatDistance)
                    {
                        // Muda de lugar depois de alguns disparos: fica mais difícil de acertar e pode surpreender John.
                        shotsSinceMove = 0;
                        phase = Phase.Reposition;
                        if (dist < retreatDistance) Retreat(); else PickNewPoint();
                    }
                    else if (Time.time >= nextShot && los) StartDraw();
                    else if (!los) { phase = Phase.Reposition; }
                    break;
            }
        }

        /// <summary>
        /// Começa a puxar a corda (animação de disparo e som do arco).
        /// </summary>
        private void StartDraw()
        {
            phase = Phase.Draw;
            phaseEnd = Time.time + drawTime;
            StopAgent();
            TriggerAttackAnimation();
            CombatAudio a = CombatAudio.Instance;
            if (a != null) CombatAudio.Play(a.bowDraw, transform.position + Vector3.up * 1.4f, 0.8f);
        }

        /// <summary>
        /// Dispara uma flecha em arco até John, antecipando seu movimento.
        /// </summary>
        private void Fire()
        {
            if (arrowPrefabs == null || arrowPrefabs.Length == 0) return;
            GameObject prefab = arrowPrefabs[Random.Range(0, arrowPrefabs.Length)];
            if (prefab == null) return;

            Vector3 origin = arrowSpawn != null ? arrowSpawn.position : transform.position + Vector3.up * 1.4f + transform.forward * 0.6f;
            Vector3 target = PlayerChest;
            // Antecipação: onde John estará quando a flecha chegar.
            float flightTime = Vector3.Distance(origin, target) / arrowSpeed;
            if (playerBody != null)
            {
                Vector3 pv = playerBody.linearVelocity;
                pv.y = 0f;
                target += pv * flightTime * leadFactor;
            }

            Vector3 velocity = BallisticVelocity(origin, target, arrowSpeed);
            Vector2 r = Random.insideUnitCircle * inaccuracy;
            velocity = Quaternion.LookRotation(velocity) * Quaternion.Euler(r.y, r.x, 0f) * Vector3.forward * velocity.magnitude;

            GameObject go = Instantiate(prefab, origin, Quaternion.LookRotation(velocity));
            Projectile p = go.GetComponent<Projectile>();
            if (p != null)
            {
                p.damage = arrowDamage;
                p.Launch(velocity, gameObject, Team.Enemy);
            }
            CombatAudio a = CombatAudio.Instance;
            if (a != null) CombatAudio.Play(a.bowRelease, origin, 0.9f);
        }

        /// <summary>
        /// Calcula a velocidade de lançamento para acertar um alvo com gravidade (trajetória baixa da parábola).
        /// </summary>
        /// <param name="origin">Ponto de saída.</param>
        /// <param name="target">Alvo.</param>
        /// <param name="speed">Rapidez de lançamento.</param>
        /// <returns>Vetor velocidade inicial.</returns>
        public static Vector3 BallisticVelocity(Vector3 origin, Vector3 target, float speed)
        {
            Vector3 delta = target - origin;
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            float x = flat.magnitude;
            if (x < 0.05f) return delta.normalized * speed;
            float y = delta.y;
            float g = -Physics.gravity.y;
            float v2 = speed * speed;
            float disc = v2 * v2 - g * (g * x * x + 2f * y * v2);
            float angle = disc < 0f ? 45f * Mathf.Deg2Rad : Mathf.Atan((v2 - Mathf.Sqrt(disc)) / (g * x));
            Vector3 dir = flat / x;
            return dir * (Mathf.Cos(angle) * speed) + Vector3.up * (Mathf.Sin(angle) * speed);
        }

        /// <summary>
        /// Escolhe o melhor ponto de ataque: na distância preferida, com linha de visada até John,
        /// pelos lados/atrás dele (surpresa), perto e sem outro arqueiro.
        /// </summary>
        private void PickNewPoint()
        {
            if (Time.time < nextPick) return;
            nextPick = Time.time + 0.5f;
            Transform previous = currentPoint;
            Release();
            Transform best = null;
            float bestScore = float.MinValue;
            Vector3 chest = PlayerChest;
            Vector3 facing = player != null ? FlatForward(player) : Vector3.forward;
            PlayerController pc = player != null ? player.GetComponent<PlayerController>() : null;
            if (pc != null) facing = pc.Facing;

            if (attackPoints != null)
            {
                foreach (Transform p in attackPoints)
                {
                    if (p == null || p == previous) continue;
                    if (reserved.TryGetValue(p, out RangedEnemy owner) && owner != null && owner != this && owner.IsAlive) continue;
                    Vector3 toPoint = p.position - player.position;
                    toPoint.y = 0f;
                    float d = toPoint.magnitude;
                    float mid = (preferredMin + preferredMax) * 0.5f;
                    float score = -Mathf.Abs(d - mid) * 0.6f;
                    if (d < preferredMin * 0.8f) score -= 10f;
                    score += HasLineOfSight(p.position + Vector3.up * eyeHeight, chest) ? 6f : -4f;
                    // Pontos ao lado ou atrás de John valem mais: o arqueiro tenta surpreendê-lo.
                    if (d > 0.01f) score += (1f - Vector3.Dot(facing, toPoint / d)) * 1.6f;
                    score -= Vector3.Distance(transform.position, p.position) * 0.12f;
                    score += Random.Range(0f, 1f);
                    if (score > bestScore) { bestScore = score; best = p; }
                }
            }

            if (best != null)
            {
                reserved[best] = this;
                currentPoint = best;
                SetDestination(best.position);
                return;
            }

            // Sem pontos de ataque: procura um lugar em volta de John na distância preferida.
            for (int i = 0; i < 8; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
                Vector3 candidate = player.position + dir * Random.Range(preferredMin, preferredMax);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas) &&
                    HasLineOfSight(hit.position + Vector3.up * eyeHeight, chest))
                {
                    SetDestination(hit.position);
                    return;
                }
            }
        }

        /// <summary>
        /// Recua para longe de John (mantendo a distância de tiro).
        /// </summary>
        private void Retreat()
        {
            if (Time.time < nextRetreat) return;
            nextRetreat = Time.time + 0.6f;
            Release();
            Vector3 away = transform.position - player.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            away.Normalize();
            for (int i = 0; i < 6; i++)
            {
                Vector3 side = Quaternion.Euler(0f, Random.Range(-60f, 60f), 0f) * away;
                Vector3 candidate = transform.position + side * Random.Range(4f, 7f);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                {
                    SetDestination(hit.position);
                    return;
                }
            }
            PickNewPoint();
        }

        /// <summary>
        /// Define o destino do reposicionamento.
        /// </summary>
        /// <param name="point">Destino.</param>
        private void SetDestination(Vector3 point)
        {
            destination = point;
            hasDestination = true;
            MoveTo(point, runSpeed);
        }

        /// <summary>
        /// Gira para John.
        /// </summary>
        private void FacePlayer()
        {
            faceTarget = true;
            faceTargetPoint = PlayerChest;
        }

        /// <summary>
        /// Libera o ponto de ataque reservado.
        /// </summary>
        private void Release()
        {
            if (currentPoint != null && reserved.TryGetValue(currentPoint, out RangedEnemy owner) && owner == this) reserved.Remove(currentPoint);
            currentPoint = null;
        }

        /// <summary>
        /// Atordoado durante a puxada da corda: perde o disparo.
        /// </summary>
        protected override void OnStunned()
        {
            if (phase == Phase.Draw)
            {
                phase = Phase.Recover;
                phaseEnd = Time.time + 0.3f;
            }
        }

        /// <summary>
        /// Ao morrer, libera o ponto de ataque.
        /// </summary>
        protected override void OnDied()
        {
            Release();
        }

        /// <summary>
        /// Também libera o ponto ao ser removido da cena.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            Release();
        }

        /// <summary>
        /// Frente de um objeto projetada no chão.
        /// </summary>
        /// <param name="t">Objeto.</param>
        /// <returns>Direção no plano.</returns>
        private static Vector3 FlatForward(Transform t)
        {
            Vector3 f = t.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward;
        }
    }
}
