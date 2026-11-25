using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Zona de luz ou sombra que vale para a furtividade mesmo quando a luz da cena é "Baked" ou de área
    /// (essas luzes não existem durante o jogo): feixes de luz do teto, poças brilhantes, cantos escuros.
    /// </summary>
    public class LightZone : StealthVolume
    {
        /// <summary>Luz somada à exposição de John (0 a 1).</summary>
        [Range(0f, 1f)] public float bonus = 0f;
        /// <summary>Multiplicador da exposição (menor que 1 = sombra densa).</summary>
        [Range(0f, 1f)] public float multiplier = 1f;
    }
}
