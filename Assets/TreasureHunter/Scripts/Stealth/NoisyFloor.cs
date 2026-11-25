using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Chão barulhento (cascalho solto, ossos, poças): multiplica o ruído dos passos e toca um som extra.
    /// Agachado o ruído ainda aumenta, mas bem menos.
    /// </summary>
    public class NoisyFloor : StealthVolume
    {
        /// <summary>Multiplicador do raio do ruído dos passos.</summary>
        public float noiseMultiplier = 2.2f;
        /// <summary>Multiplicador agachado.</summary>
        public float crouchedMultiplier = 3f;
        /// <summary>Sons extras dos passos (vetor sorteado).</summary>
        public AudioClip[] stepClips;
        /// <summary>Partículas de pedrinhas/ossos nos passos (vetor sorteado).</summary>
        public GameObject[] stepEffects;

        /// <summary>
        /// Toca o som e o efeito do passo (chamado pelo PlayerStealth).
        /// </summary>
        /// <param name="position">Posição do pé.</param>
        /// <param name="crouched">John está agachado.</param>
        public void PlayStep(Vector3 position, bool crouched)
        {
            AudioClip clip = StealthUtil.Pick(stepClips);
            if (clip != null) AudioSource.PlayClipAtPoint(clip, position, crouched ? 0.25f : 0.7f);
            if (!crouched) StealthUtil.SpawnRandom(stepEffects, position + Vector3.up * 0.05f);
        }
    }
}
