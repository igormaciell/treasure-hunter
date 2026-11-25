using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace TreasureHunter
{
    /// <summary>
    /// Objeto quebrável do cenário (caixote frágil, vaso, barril). Ao quebrar:
    /// esconde o modelo inteiro, espalha destroços sorteados de um VETOR DE PREFABS (AddForce + AddTorque),
    /// cria partículas, toca o som e pode soltar um item sorteado de outro vetor (coração, moeda...).
    /// O barril explosivo causa dano em área (inimigos e jogador), empurra objetos físicos com
    /// AddExplosionForce e pode detonar outros barris (reação em cadeia).
    /// Volta ao estado inicial quando a sala é reiniciada.
    /// </summary>
    public class Destructible : MonoBehaviour, IDamageable, IResettable
    {
        /// <summary>Sala do objeto.</summary>
        public int roomIndex;
        /// <summary>Resistência (pontos de vida).</summary>
        public float maxHealth = 1f;
        /// <summary>Modelo inteiro (escondido ao quebrar).</summary>
        public GameObject intactVisual;

        [Header("Destroços e efeitos (vetores)")]
        public GameObject[] debrisPrefabs;
        public int debrisCount = 5;
        public float debrisForce = 4f;
        public float debrisLifetime = 6f;
        public GameObject[] breakEffects;
        public AudioClip breakClip;

        [Header("Itens (vetor sorteado)")]
        [Range(0f, 1f)] public float lootChance = 0.5f;
        public GameObject[] lootPrefabs;

        [Header("Explosão")]
        public bool explosive = false;
        public float explosionRadius = 4f;
        public float explosionDamage = 6f;
        public int explosionPlayerDamage = 1;
        public float explosionForce = 14f;
        public float chainDelay = 0.15f;
        public GameObject[] explosionEffects;
        public AudioClip explosionClip;

        private float health;
        private bool broken;
        private Collider[] colliders;
        private NavMeshObstacle obstacle;
        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly Collider[] buffer = new Collider[48];

        /// <summary>Sala do objeto (IResettable).</summary>
        public int RoomIndex => roomIndex;
        /// <summary>Inteiro enquanto não quebrou.</summary>
        public bool IsAlive => !broken;
        /// <summary>Objetos do cenário são neutros.</summary>
        public Team Team => Team.Neutral;

        /// <summary>
        /// Guarda os colisores e o obstáculo de navegação.
        /// </summary>
        private void Awake()
        {
            health = maxHealth;
            colliders = GetComponents<Collider>();
            obstacle = GetComponent<NavMeshObstacle>();
        }

        /// <summary>
        /// Recebe um golpe; quebra quando a resistência acaba.
        /// </summary>
        /// <param name="info">Dados do golpe.</param>
        /// <returns>true se o golpe foi aceito.</returns>
        public bool TakeHit(DamageInfo info)
        {
            if (broken) return false;
            health -= info.amount;
            if (health <= 0f)
            {
                if (explosive && info.source == DamageSource.Explosion) StartCoroutine(BreakLater(info));
                else Break(info);
            }
            return true;
        }

        /// <summary>
        /// Espera um instante antes de quebrar (reação em cadeia dos barris fica mais bonita).
        /// </summary>
        /// <param name="info">Golpe recebido.</param>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator BreakLater(DamageInfo info)
        {
            broken = true;
            yield return new WaitForSeconds(chainDelay);
            broken = false;
            Break(info);
        }

        /// <summary>
        /// Quebra o objeto: destroços, efeitos, som, item e (se for explosivo) explosão.
        /// </summary>
        /// <param name="info">Golpe que quebrou.</param>
        private void Break(DamageInfo info)
        {
            if (broken) return;
            broken = true;
            if (intactVisual != null) intactVisual.SetActive(false);
            foreach (Collider c in colliders) if (c != null) c.enabled = false;
            if (obstacle != null) obstacle.enabled = false;

            Vector3 center = transform.position + Vector3.up * 0.5f;
            SpawnDebris(center, info.direction);
            Projectile.SpawnEffects(breakEffects, center, Vector3.up);
            CombatAudio.Play(breakClip, center, 0.9f);

            // Sorteia um item do vetor de prefabs.
            if (lootPrefabs != null && lootPrefabs.Length > 0 && Random.value < lootChance)
            {
                GameObject prefab = lootPrefabs[Random.Range(0, lootPrefabs.Length)];
                if (prefab != null) spawned.Add(Instantiate(prefab, transform.position + Vector3.up * 0.1f, Quaternion.identity));
            }

            if (explosive) Explode(center);
        }

        /// <summary>
        /// Espalha destroços sorteados do vetor, com força para fora e giro aleatório.
        /// </summary>
        /// <param name="center">Centro do objeto.</param>
        /// <param name="hitDir">Direção do golpe (os destroços voam para esse lado).</param>
        private void SpawnDebris(Vector3 center, Vector3 hitDir)
        {
            if (debrisPrefabs == null || debrisPrefabs.Length == 0) return;
            for (int i = 0; i < debrisCount; i++)
            {
                GameObject prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
                if (prefab == null) continue;
                Vector3 offset = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.2f, 0.35f), Random.Range(-0.35f, 0.35f));
                GameObject d = Instantiate(prefab, center + offset, Random.rotation);
                Rigidbody rb = d.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 push = (offset.normalized + Vector3.up * 0.8f + hitDir * 0.6f).normalized;
                    rb.AddForce(push * debrisForce * Random.Range(0.6f, 1.2f), ForceMode.VelocityChange);
                    rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.VelocityChange);
                }
                spawned.Add(d);
                Destroy(d, debrisLifetime + Random.Range(0f, 1.5f));
            }
        }

        /// <summary>
        /// Explosão: dano decrescente com a distância, empurrão em objetos físicos, tremor e barulho.
        /// </summary>
        /// <param name="center">Centro da explosão.</param>
        private void Explode(Vector3 center)
        {
            Projectile.SpawnEffects(explosionEffects, center, Vector3.up);
            CombatAudio.Play(explosionClip, center, 1f, 0.05f);
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.5f, 0.18f);
            NoiseSystem.Emit(center, 22f, NoiseType.Explosion, gameObject, false);

            HashSet<IDamageable> done = new HashSet<IDamageable>();
            int n = Physics.OverlapSphereNonAlloc(center, explosionRadius, buffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = buffer[i];
                Rigidbody rb = c.attachedRigidbody;
                if (rb != null && !rb.isKinematic) rb.AddExplosionForce(explosionForce, center, explosionRadius, 1f, ForceMode.Impulse);

                IDamageable d = c.GetComponentInParent<IDamageable>();
                if (d == null || ReferenceEquals(d, this) || !d.IsAlive || done.Contains(d)) continue;
                done.Add(d);
                Vector3 to = c.bounds.center - center;
                float dist = to.magnitude;
                float falloff = Mathf.Clamp01(1f - dist / explosionRadius);
                Vector3 dir = dist > 0.01f ? to / dist : Vector3.up;
                if (d.Team == Team.Player)
                {
                    // O jogador só se fere se estiver bem perto do barril.
                    if (dist < explosionRadius * 0.7f)
                        d.TakeHit(new DamageInfo(explosionPlayerDamage, c.bounds.center, dir, 8f, DamageSource.Explosion, Team.Neutral, gameObject));
                }
                else
                {
                    float amount = Mathf.Max(explosionDamage * 0.5f, explosionDamage * falloff);
                    d.TakeHit(new DamageInfo(amount, c.bounds.center, dir, 9f, DamageSource.Explosion, Team.Neutral, gameObject));
                }
            }
        }

        /// <summary>
        /// Restaura o objeto inteiro e apaga destroços e itens que ele criou (IResettable).
        /// </summary>
        public void ResetState()
        {
            StopAllCoroutines();
            foreach (GameObject g in spawned) if (g != null) Destroy(g);
            spawned.Clear();
            broken = false;
            health = maxHealth;
            if (intactVisual != null) intactVisual.SetActive(true);
            foreach (Collider c in colliders) if (c != null) c.enabled = true;
            if (obstacle != null) obstacle.enabled = true;
        }
    }
}
