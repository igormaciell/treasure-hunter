using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Pedra que cai do teto. Recebe AddForce para baixo e AddTorque para girar no ar.
    /// Ao colidir com o chão ou com o jogador, "explode": cria a explosão de poeira e
    /// as pedrinhas (partículas), toca o som e se destrói. Se acertar o jogador, causa dano.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FallingRock : MonoBehaviour
    {
        /// <summary>Corações retirados ao acertar o jogador.</summary>
        public int damage = 1;
        /// <summary>Velocidade inicial para baixo.</summary>
        public float initialDownSpeed = 3f;
        /// <summary>Intensidade do giro aleatório.</summary>
        public float spin = 8f;
        /// <summary>Prefab de explosão (poeira).</summary>
        public GameObject explosionPrefab;
        /// <summary>Prefab das pedrinhas que se espalham.</summary>
        public GameObject pebblesPrefab;
        /// <summary>Tempo máximo de vida (segurança).</summary>
        public float lifetime = 6f;

        private bool exploded;
        private float spawnTime;

        /// <summary>
        /// Aplica força inicial para baixo e torque aleatório.
        /// </summary>
        private void Start()
        {
            spawnTime = Time.time;
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.AddForce(Vector3.down * initialDownSpeed, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * spin, ForceMode.VelocityChange);
            Destroy(gameObject, lifetime);
        }

        /// <summary>
        /// Ao bater em algo, verifica pela Tag se foi o jogador (dano) e explode.
        /// </summary>
        /// <param name="collision">Dados da colisão.</param>
        private void OnCollisionEnter(Collision collision)
        {
            if (exploded) return;
            // Pedras não explodem ao bater em outras pedras.
            if (collision.collider.CompareTag(GameTags.Rock)) return;
            bool hitPlayer = collision.collider.CompareTag(GameTags.Player);
            // Nos primeiros instantes ignora encostar no teto/paredes de onde a pedra acabou de sair.
            if (!hitPlayer && Time.time - spawnTime < 0.15f) return;

            if (hitPlayer)
            {
                PlayerHealth health = collision.collider.GetComponentInParent<PlayerHealth>();
                if (health != null) health.TakeDamage(damage, transform.position);
            }
            Explode(collision.GetContact(0).point);
        }

        /// <summary>
        /// Cria os efeitos de explosão e pedrinhas, toca o som, treme a câmera e destrói a pedra.
        /// </summary>
        /// <param name="point">Ponto do impacto.</param>
        private void Explode(Vector3 point)
        {
            exploded = true;
            if (explosionPrefab != null) Instantiate(explosionPrefab, point, Quaternion.identity);
            if (pebblesPrefab != null) Instantiate(pebblesPrefab, point + Vector3.up * 0.2f, Quaternion.identity);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.rockExplosion, point, 0.8f);

            // Treme a câmera proporcionalmente à proximidade do jogador.
            if (ThirdPersonCamera.Instance != null && GameManager.Instance != null && GameManager.Instance.player != null)
            {
                float d = Vector3.Distance(point, GameManager.Instance.player.transform.position);
                if (d < 8f) ThirdPersonCamera.Instance.Shake(0.25f, 0.08f * (1f - d / 8f));
            }
            Destroy(gameObject);
        }
    }
}
