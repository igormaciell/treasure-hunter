using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Parte cortante de uma armadilha (lâmina do pêndulo). Fere quem encostar — jogador ou inimigo —
    /// com um intervalo mínimo por alvo. Atrair os esqueletos para as armadilhas é uma estratégia válida.
    /// </summary>
    public class TrapDamage : MonoBehaviour
    {
        /// <summary>Corações retirados do jogador.</summary>
        public int playerDamage = 1;
        /// <summary>Dano aos inimigos.</summary>
        public float enemyDamage = 4f;
        /// <summary>Intervalo mínimo entre dois golpes no mesmo alvo.</summary>
        public float perTargetCooldown = 0.8f;
        /// <summary>Força do empurrão.</summary>
        public float force = 6f;
        /// <summary>Corpo físico que dá a direção do golpe (opcional).</summary>
        public Rigidbody velocitySource;

        private readonly Dictionary<IDamageable, float> lastHit = new Dictionary<IDamageable, float>();

        /// <summary>Contato com o gatilho.</summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            Hit(other);
        }

        /// <summary>Permanência no gatilho.</summary>
        /// <param name="other">Colisor que permanece.</param>
        private void OnTriggerStay(Collider other)
        {
            Hit(other);
        }

        /// <summary>
        /// Fere o alvo, respeitando o intervalo por alvo.
        /// </summary>
        /// <param name="other">Colisor tocado.</param>
        private void Hit(Collider other)
        {
            IDamageable d = other.GetComponentInParent<IDamageable>();
            if (d == null || !d.IsAlive || d.Team == Team.Neutral) return;
            if (lastHit.TryGetValue(d, out float t) && Time.time - t < perTargetCooldown) return;
            lastHit[d] = Time.time;

            Vector3 dir = velocitySource != null && velocitySource.linearVelocity.sqrMagnitude > 0.1f
                ? velocitySource.linearVelocity.normalized
                : (other.transform.position - transform.position).normalized;
            float amount = d.Team == Team.Player ? playerDamage : enemyDamage;
            d.TakeHit(new DamageInfo(amount, other.ClosestPoint(transform.position), dir, force, DamageSource.Trap, Team.Neutral, gameObject));
        }
    }
}
