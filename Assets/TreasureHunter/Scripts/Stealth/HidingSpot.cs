using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Esconderijo (vegetação alta, fenda, pilha de peles): agachado aqui dentro, John quase some da vista dos
    /// ogros — a menos que um deles esteja muito perto ou já o tenha visto entrar.
    /// </summary>
    public class HidingSpot : StealthVolume
    {
        /// <summary>Multiplicador da visibilidade agachado.</summary>
        [Range(0f, 1f)] public float crouchedFactor = 0.12f;
        /// <summary>Multiplicador da visibilidade em pé.</summary>
        [Range(0f, 1f)] public float standingFactor = 0.6f;
        /// <summary>A vegetação balança e faz um pouco de barulho ao passar correndo.</summary>
        public float rustleNoise = 2.5f;

        /// <summary>Folhas e esporos que balançam ao atravessar (opcional).</summary>
        public ParticleSystem rustleEffect;
        /// <summary>Som do mato se mexendo (mais baixo se John entrar agachado).</summary>
        public AudioClip rustleClip;
        /// <summary>Volume do som em pé (agachado toca a 35%).</summary>
        [Range(0f, 1f)] public float rustleVolume = 0.6f;

        /// <summary>
        /// Efeito ao entrar (o mato se mexe).
        /// </summary>
        /// <param name="player">Jogador.</param>
        public void OnPlayerEnter(PlayerStealth player)
        {
            if (rustleEffect != null) rustleEffect.Play();
            if (rustleClip != null && player != null)
                AudioSource.PlayClipAtPoint(rustleClip, player.transform.position, rustleVolume * (player.IsCrouched ? 0.35f : 1f));
            if (player != null && !player.IsCrouched && rustleNoise > 0f)
                NoiseSystem.Emit(player.transform.position, rustleNoise, NoiseType.Handling, player.gameObject);
        }
    }
}
