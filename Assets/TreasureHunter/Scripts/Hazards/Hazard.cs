using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Causa dano ao jogador quando ele toca este objeto (gatilho ou colisão).
    /// Usado nos espinhos do tronco giratório e em outras armadilhas.
    /// </summary>
    public class Hazard : MonoBehaviour
    {
        /// <summary>Corações retirados por contato.</summary>
        public int damage = 1;

        /// <summary>
        /// Contato via gatilho: aplica dano se for o jogador (verificado pela Tag).
        /// </summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            TryDamage(other);
        }

        /// <summary>
        /// Permanência no gatilho: tenta aplicar dano de novo (a invulnerabilidade limita a frequência).
        /// </summary>
        /// <param name="other">Colisor que permanece.</param>
        private void OnTriggerStay(Collider other)
        {
            TryDamage(other);
        }

        /// <summary>
        /// Contato via colisão física.
        /// </summary>
        /// <param name="collision">Dados da colisão.</param>
        private void OnCollisionEnter(Collision collision)
        {
            TryDamage(collision.collider);
        }

        /// <summary>
        /// Aplica dano caso o colisor pertença ao jogador.
        /// </summary>
        /// <param name="other">Colisor tocado.</param>
        private void TryDamage(Collider other)
        {
            if (!other.CompareTag(GameTags.Player)) return;
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeDamage(damage, transform.position);
        }
    }
}
