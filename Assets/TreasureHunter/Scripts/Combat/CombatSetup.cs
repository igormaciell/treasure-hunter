using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Preparação do combate ao abrir a cena do Nível 2: configura a matriz de colisão das camadas
    /// (projéteis, inimigos, itens) e zera os contadores globais de uma partida anterior.
    /// Roda antes dos outros scripts.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public class CombatSetup : MonoBehaviour
    {
        /// <summary>
        /// Configura camadas e zera estatísticas.
        /// </summary>
        private void Awake()
        {
            CombatLayers.ResetCache();
            CombatLayers.SetupCollisions();
            EnemyBase.ResetStatics();
            RangedEnemy.ClearReservations();
            CombatRoom.ResetStatics();
            Projectile.Active.Clear();
        }
    }
}
