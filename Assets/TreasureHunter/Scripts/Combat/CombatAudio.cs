using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Banco de sons do combate (Nível 2). Os sons são tocados pelo AudioManager já existente
    /// (pool de fontes de áudio), então este componente só guarda os clipes e oferece atalhos.
    /// Fica separado do AudioManager para não alterar o Nível 1.
    /// </summary>
    public class CombatAudio : MonoBehaviour
    {
        /// <summary>Instância única acessível por qualquer script.</summary>
        public static CombatAudio Instance { get; private set; }

        [Header("Revólver")]
        public AudioClip revolverShot;
        public AudioClip dryFire;
        public AudioClip reload;
        public AudioClip ricochet;
        public AudioClip boneHit;

        [Header("Corpo a corpo")]
        public AudioClip pistolWhip;
        public AudioClip knifeSlash;
        public AudioClip knifeHit;
        public AudioClip swingWhoosh;

        [Header("Pedras")]
        public AudioClip stoneThrow;
        public AudioClip stonePickup;
        public AudioClip stoneImpact;

        [Header("Arco e flecha")]
        public AudioClip bowDraw;
        public AudioClip bowRelease;
        public AudioClip arrowImpact;

        [Header("Esqueletos")]
        public AudioClip[] skeletonAlert;
        public AudioClip[] skeletonAttack;
        public AudioClip[] skeletonHurt;
        public AudioClip[] skeletonDeath;
        public AudioClip[] boneSteps;
        public AudioClip skeletonRise;
        public AudioClip bruteRoar;
        public AudioClip axeSwing;

        [Header("Cenário")]
        public AudioClip barrelExplosion;
        public AudioClip crateBreak;
        public AudioClip spikeTrap;
        public AudioClip pendulum;
        public AudioClip gateMove;
        public AudioClip knifePickup;
        public AudioClip weaponPickup;
        public AudioClip roomClear;
        public AudioClip dummyHit;

        /// <summary>
        /// Registra o singleton.
        /// </summary>
        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Libera o singleton ao trocar de cena.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Toca um som posicionado no mundo (3D).
        /// </summary>
        /// <param name="clip">Clipe (nulo = nada acontece).</param>
        /// <param name="position">Posição de origem.</param>
        /// <param name="volume">Volume relativo.</param>
        /// <param name="pitchVariation">Variação aleatória de tom.</param>
        public static void Play(AudioClip clip, Vector3 position, float volume = 1f, float pitchVariation = 0.08f)
        {
            if (clip != null && AudioManager.Instance != null) AudioManager.Instance.PlayAt(clip, position, volume, pitchVariation);
        }

        /// <summary>
        /// Toca um som de interface (2D).
        /// </summary>
        /// <param name="clip">Clipe (nulo = nada acontece).</param>
        /// <param name="volume">Volume relativo.</param>
        public static void Play2D(AudioClip clip, float volume = 1f)
        {
            if (clip != null && AudioManager.Instance != null) AudioManager.Instance.Play2D(clip, volume);
        }

        /// <summary>
        /// Sorteia e toca um clipe de um vetor de sons.
        /// </summary>
        /// <param name="clips">Vetor de clipes.</param>
        /// <param name="position">Posição de origem.</param>
        /// <param name="volume">Volume relativo.</param>
        public static void PlayRandom(AudioClip[] clips, Vector3 position, float volume = 1f)
        {
            if (clips == null || clips.Length == 0) return;
            Play(clips[Random.Range(0, clips.Length)], position, volume, 0.12f);
        }
    }
}
