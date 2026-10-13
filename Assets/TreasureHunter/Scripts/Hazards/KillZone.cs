using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Área de segurança abaixo do cenário: se o jogador cair nela, perde um coração
    /// e volta ao último checkpoint. Objetos empurráveis que caírem voltam ao início.
    /// </summary>
    public class KillZone : MonoBehaviour
    {
        /// <summary>Corações perdidos ao cair.</summary>
        public int damage = 1;

        /// <summary>
        /// Trata o objeto que caiu, identificado pela Tag.
        /// </summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(GameTags.Player))
            {
                PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
                if (health != null) health.TakeDamage(damage, other.transform.position);
                if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing) GameManager.Instance.RespawnPlayer();
            }
            else if (other.CompareTag(GameTags.Pushable))
            {
                PushableCrate crate = other.GetComponentInParent<PushableCrate>();
                if (crate != null) crate.ResetState();
            }
        }
    }
}
