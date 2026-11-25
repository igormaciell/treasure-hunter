using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Banco de sons da furtividade (singleton): coração acelerado, alertas, morte, agachar, tocha,
    /// cascalho. Os sons dos ogros ficam em cada ogro (passos, respiração, rosnados).
    /// </summary>
    public class StealthAudio : MonoBehaviour
    {
        /// <summary>Instância ativa.</summary>
        public static StealthAudio Instance { get; private set; }

        [Header("Tensão")]
        public AudioClip heartbeat;
        public AudioClip suspiciousStinger;
        public AudioClip spottedStinger;
        public AudioClip lostStinger;

        [Header("Jogador")]
        public AudioClip playerDeath;
        public AudioClip crouchDown;
        public AudioClip standUp;
        public AudioClip torchIgnite;
        public AudioClip torchExtinguish;
        public AudioClip[] gravelSteps;
        public AudioClip checkpoint;

        /// <summary>Registra o singleton.</summary>
        private void Awake()
        {
            Instance = this;
        }

        /// <summary>Libera o singleton.</summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Toca um som 3D num ponto.
        /// </summary>
        /// <param name="clip">Som.</param>
        /// <param name="position">Posição.</param>
        /// <param name="volume">Volume.</param>
        public static void Play(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            GameObject go = new GameObject("Som_" + clip.name);
            go.transform.position = position;
            AudioSource s = go.AddComponent<AudioSource>();
            s.clip = clip;
            s.volume = volume;
            s.spatialBlend = 1f;
            s.minDistance = 2f;
            s.maxDistance = 40f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.pitch = Random.Range(0.95f, 1.05f);
            s.Play();
            Destroy(go, clip.length / Mathf.Max(0.5f, s.pitch) + 0.2f);
        }

        /// <summary>
        /// Toca um som 2D (interface, alertas).
        /// </summary>
        /// <param name="clip">Som.</param>
        /// <param name="volume">Volume.</param>
        public static void Play2D(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            GameObject go = new GameObject("Som2D_" + clip.name);
            AudioSource s = go.AddComponent<AudioSource>();
            s.clip = clip;
            s.volume = volume;
            s.spatialBlend = 0f;
            s.ignoreListenerPause = false;
            s.Play();
            Destroy(go, clip.length + 0.2f);
        }
    }
}
