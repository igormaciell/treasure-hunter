using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Início do Nível 4: John chega com o revólver e poucas balas (as que sobraram), sem pedras.
    /// Mostra a fala de abertura.
    /// </summary>
    public class StealthLevelStart : MonoBehaviour
    {
        /// <summary>Balas no começo do nível (o revólver nunca recebe mais).</summary>
        public int startingShots = 6;
        public string openingLine = "Que cheiro horrível... e esses roncos. Ogros! Melhor não fazer barulho.";

        /// <summary>Entrega o revólver e mostra a fala.</summary>
        private void Start()
        {
            PlayerCombat combat = PlayerCombat.Instance;
            if (combat != null)
            {
                combat.startingShots = startingShots;
                combat.GiveRevolver(startingShots);
            }
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.ui != null && !string.IsNullOrEmpty(openingLine)) gm.ui.ShowLine(openingLine, 5f);
        }
    }
}
