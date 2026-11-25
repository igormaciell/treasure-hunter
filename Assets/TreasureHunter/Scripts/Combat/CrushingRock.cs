using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Pedregulho que despenca do teto instável. Cai com AddForce e gira com AddTorque; ao bater,
    /// esmaga quem estiver embaixo (dano alto nos esqueletos, 2 corações no jogador) e se desfaz em pedrinhas.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CrushingRock : MonoBehaviour
    {
        /// <summary>Dano aos inimigos.</summary>
        public float enemyDamage = 10f;
        /// <summary>Corações retirados do jogador.</summary>
        public int playerDamage = 2;
        /// <summary>Velocidade mínima para ferir.</summary>
        public float minSpeed = 3f;
        /// <summary>Efeitos do impacto (explosão de poeira e pedrinhas).</summary>
        public GameObject[] impactEffects;
        /// <summary>Tempo máximo de vida.</summary>
        public float lifetime = 7f;

        private bool spent;
        private float spawnTime;

        /// <summary>
        /// Dá o empurrão para baixo e o giro aleatório.
        /// </summary>
        private void Start()
        {
            spawnTime = Time.time;
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.AddForce(Vector3.down * 4f, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 6f, ForceMode.VelocityChange);
            Destroy(gameObject, lifetime);
        }

        /// <summary>
        /// Fere o que acertar com força e se desfaz ao bater no chão.
        /// </summary>
        /// <param name="collision">Dados da colisão.</param>
        private void OnCollisionEnter(Collision collision)
        {
            if (spent) return;
            if (collision.collider.GetComponentInParent<CrushingRock>() != null) return;
            if (Time.time - spawnTime < 0.1f) return;
            float speed = collision.relativeVelocity.magnitude;
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

            IDamageable d = collision.collider.GetComponentInParent<IDamageable>();
            if (d != null && d.IsAlive && speed >= minSpeed && d.Team != Team.Neutral)
            {
                float amount = d.Team == Team.Player ? playerDamage : enemyDamage;
                d.TakeHit(new DamageInfo(amount, point, Vector3.down, 6f, DamageSource.Trap, Team.Neutral, gameObject));
            }

            spent = true;
            Projectile.SpawnEffects(impactEffects, point, Vector3.up);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.rockExplosion, point, 0.9f);
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.2f, 0.07f);
            Destroy(gameObject);
        }
    }
}
