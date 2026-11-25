using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Região de acerto extra de um personagem (por exemplo, a cabeça dos esqueletos).
    /// O projétil que acerta este colisor multiplica o dano. O dano vai para o
    /// IDamageable do objeto pai (o inimigo).
    /// </summary>
    public class Hitbox : MonoBehaviour
    {
        /// <summary>Multiplicador de dano desta região.</summary>
        public float multiplier = 2f;
        /// <summary>Verdadeiro para a cabeça (aviso "tiro na cabeça" no HUD).</summary>
        public bool isHead = true;
    }
}
