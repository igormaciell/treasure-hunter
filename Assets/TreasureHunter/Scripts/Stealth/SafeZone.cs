using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Área segura: nichos de teto baixo e fendas estreitas por onde os ogros não passam (fora do NavMesh).
    /// Dentro dela John não pode ser atacado e só é visto de muito perto; um ogro que o perseguia
    /// fica bufando na entrada e acaba desistindo.
    /// </summary>
    public class SafeZone : StealthVolume
    {
        /// <summary>A partir desta distância o ogro não enxerga John dentro da área.</summary>
        public float visibleWithin = 3f;
        /// <summary>A área também serve de ponto de retorno (o checkpoint do trecho é definido pelo StealthSector).</summary>
        public bool showHint = true;

        private bool hintShown;

        /// <summary>
        /// Dica na primeira vez que John entra numa área segura.
        /// </summary>
        public void OnPlayerEnter()
        {
            if (!showHint || hintShown) return;
            hintShown = true;
            StealthUtil.Hint("Área segura: os ogros não cabem aqui. Espere e observe as patrulhas.");
        }
    }
}
