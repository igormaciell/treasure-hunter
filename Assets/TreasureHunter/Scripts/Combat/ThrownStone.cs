using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Pedra arremessada por John (tecla G). Voa com AddForce e gira com AddTorque.
    /// No primeiro impacto forte contra um inimigo causa dano leve e o deixa atordoado;
    /// ao cair, faz barulho e atrai os inimigos próximos (distração). Depois vira um objeto comum e some.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ThrownStone : MonoBehaviour
    {
        /// <summary>Dano ao acertar um inimigo.</summary>
        public float damage = 1f;
        /// <summary>Velocidade mínima de impacto para causar dano.</summary>
        public float minDamageSpeed = 4f;
        /// <summary>Raio do barulho que atrai os inimigos.</summary>
        public float noiseRadius = 9f;
        /// <summary>Tempo até a pedra sumir.</summary>
        public float lifetime = 8f;
        /// <summary>Efeitos de impacto (poeira, pedrinhas).</summary>
        public GameObject[] impactEffects;

        private Rigidbody rb;
        private GameObject owner;
        private bool armed = true;
        private bool madeNoise;

        /// <summary>
        /// Guarda o Rigidbody.
        /// </summary>
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        /// <summary>
        /// Lança a pedra: força inicial (VelocityChange) e giro aleatório (torque).
        /// </summary>
        /// <param name="velocity">Velocidade inicial.</param>
        /// <param name="thrower">Quem arremessou (a pedra não colide com ele).</param>
        public void Launch(Vector3 velocity, GameObject thrower)
        {
            owner = thrower;
            if (owner != null)
            {
                Collider mine = GetComponent<Collider>();
                foreach (Collider c in owner.GetComponentsInChildren<Collider>())
                {
                    if (mine != null && c != null) Physics.IgnoreCollision(mine, c, true);
                }
            }
            rb.AddForce(velocity, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 12f, ForceMode.VelocityChange);
            Destroy(gameObject, lifetime);
        }

        /// <summary>
        /// Trata o impacto: dano (só no primeiro impacto forte), efeitos, som e barulho de distração.
        /// </summary>
        /// <param name="collision">Dados da colisão.</param>
        private void OnCollisionEnter(Collision collision)
        {
            float speed = collision.relativeVelocity.magnitude;
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

            if (speed > 2f)
            {
                CombatAudio a = CombatAudio.Instance;
                if (a != null) CombatAudio.Play(a.stoneImpact, point, Mathf.Clamp01(speed / 12f));
                if (armed) Projectile.SpawnEffects(impactEffects, point, collision.contactCount > 0 ? collision.GetContact(0).normal : Vector3.up);
            }

            if (armed && speed >= minDamageSpeed)
            {
                IDamageable d = collision.collider.GetComponentInParent<IDamageable>();
                if (d != null && d.IsAlive && d.Team != Team.Player)
                {
                    Vector3 dir = rb.linearVelocity.sqrMagnitude > 0.01f ? rb.linearVelocity.normalized : -collision.relativeVelocity.normalized;
                    d.TakeHit(new DamageInfo(damage, point, dir, 3f, DamageSource.Stone, Team.Player, owner));
                }
            }

            // Depois do primeiro impacto a pedra não machuca mais; o barulho atrai quem estiver perto.
            armed = false;
            if (!madeNoise)
            {
                madeNoise = true;
                NoiseSystem.Emit(point, noiseRadius, NoiseType.Impact, gameObject, false);
            }
        }
    }
}
