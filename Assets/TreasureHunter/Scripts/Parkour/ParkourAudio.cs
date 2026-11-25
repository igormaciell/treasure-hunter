using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Banco de sons do parkour (um por cena, no objeto dos gerenciadores).
    /// </summary>
    public class ParkourAudio : MonoBehaviour
    {
        /// <summary>Instância da cena.</summary>
        public static ParkourAudio Instance { get; private set; }

        [Header("Movimento")]
        public AudioClip landSoft;
        public AudioClip landHard;
        public AudioClip ledgeGrab;
        public AudioClip ledgeClimb;
        public AudioClip fallWind;
        public AudioClip voidWhoosh;

        [Header("Cristais e totem")]
        public AudioClip crystalHit;
        public AudioClip crystalBreak;
        public AudioClip crystalChime;
        public AudioClip totemPickup;
        public AudioClip totemPlace;
        public AudioClip totemInvalid;
        public AudioClip totemRespawn;
        public AudioClip checkpoint;

        [Header("Plataformas")]
        public AudioClip platformCrack;
        public AudioClip platformCrumble;
        public AudioClip platformRestore;

        /// <summary>Registra a instância.</summary>
        private void Awake()
        {
            Instance = this;
        }

        /// <summary>Libera a instância.</summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Toca um som 3D pelo AudioManager.
        /// </summary>
        /// <param name="clip">Som.</param>
        /// <param name="position">Posição.</param>
        /// <param name="volume">Volume.</param>
        public static void Play(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip != null && AudioManager.Instance != null) AudioManager.Instance.PlayAt(clip, position, volume);
        }

        /// <summary>
        /// Toca um som 2D (interface) pelo AudioManager.
        /// </summary>
        /// <param name="clip">Som.</param>
        /// <param name="volume">Volume.</param>
        public static void Play2D(AudioClip clip, float volume = 1f)
        {
            if (clip != null && AudioManager.Instance != null) AudioManager.Instance.Play2D(clip, volume);
        }
    }
}
