using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Liga o jogador ao sistema de dano do combate: converte os golpes (flechas, facas,
    /// armadilhas) em corações perdidos usando o PlayerHealth do Nível 1, sem alterá-lo.
    /// </summary>
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerHitbox : MonoBehaviour, IDamageable
    {
        private PlayerHealth health;

        /// <summary>O jogador está vivo enquanto tiver corações.</summary>
        public bool IsAlive => health != null && health.Current > 0;
        /// <summary>Lado do jogador.</summary>
        public Team Team => Team.Player;

        /// <summary>
        /// Guarda a referência à vida do jogador.
        /// </summary>
        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
        }

        /// <summary>
        /// Aplica o golpe como corações perdidos (no mínimo 1). A direção do empurrão vem do atacante.
        /// </summary>
        /// <param name="info">Dados do golpe.</param>
        /// <returns>true se o dano foi aplicado (false durante a invulnerabilidade).</returns>
        public bool TakeHit(DamageInfo info)
        {
            if (health == null || info.team == Team.Player) return false;
            int hearts = Mathf.Max(1, Mathf.RoundToInt(info.amount));
            Vector3 source = info.attacker != null ? info.attacker.transform.position : info.point - info.direction;
            return health.TakeDamage(hearts, source);
        }
    }
}
