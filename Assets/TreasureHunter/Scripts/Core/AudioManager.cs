using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Gerencia todo o som do jogo: efeitos sonoros (3D e 2D), música e som ambiente.
    /// Mantém um pequeno conjunto (pool) de AudioSources reutilizáveis para não criar
    /// e destruir objetos a cada som tocado.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        /// <summary>Instância única acessível por qualquer script.</summary>
        public static AudioManager Instance { get; private set; }

        [Header("Efeitos do jogador")]
        public AudioClip[] footsteps;
        public AudioClip jump;
        public AudioClip land;
        public AudioClip hurt;
        public AudioClip torchWhoosh;

        [Header("Mecanismos")]
        public AudioClip lever;
        public AudioClip doorGrind;
        public AudioClip plateClick;
        public AudioClip buttonPress;
        public AudioClip puzzleWrong;
        public AudioClip puzzleSolved;
        public AudioClip crateScrape;
        public AudioClip fireIgnite;

        [Header("Perigos")]
        public AudioClip rockExplosion;
        public AudioClip rumble;

        [Header("Coleta e interface")]
        public AudioClip coin;
        public AudioClip gem;
        public AudioClip artifact;
        public AudioClip skillUnlock;
        public AudioClip uiClick;
        public AudioClip levelComplete;

        [Header("Ambiente e música")]
        public AudioClip forestAmbience;
        public AudioClip caveAmbience;
        public AudioClip music;

        [Header("Volumes")]
        [Range(0f, 1f)] public float sfxVolume = 0.9f;
        [Range(0f, 1f)] public float musicVolume = 0.35f;
        [Range(0f, 1f)] public float ambienceVolume = 0.6f;

        private readonly List<AudioSource> pool = new List<AudioSource>();
        private AudioSource musicSource;
        private AudioSource ambienceSource;
        private const int PoolSize = 16;

        /// <summary>
        /// Inicializa o singleton e cria as fontes de áudio (pool de efeitos, música e ambiente).
        /// </summary>
        private void Awake()
        {
            // Garante uma única instância do gerenciador na cena.
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Cria as fontes de áudio filhas usadas para efeitos sonoros.
            for (int i = 0; i < PoolSize; i++)
            {
                AudioSource s = CreateSource("Sfx_" + i, false);
                pool.Add(s);
            }
            musicSource = CreateSource("Music", true);
            ambienceSource = CreateSource("Ambience", true);
        }

        /// <summary>
        /// Começa a tocar a música do nível, se houver.
        /// </summary>
        private void Start()
        {
            if (music != null)
            {
                musicSource.clip = music;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }
        }

        /// <summary>
        /// Cria um AudioSource filho deste objeto.
        /// </summary>
        /// <param name="name">Nome do GameObject criado.</param>
        /// <param name="loop">Se o som deve se repetir (música/ambiente).</param>
        /// <returns>O AudioSource criado.</returns>
        private AudioSource CreateSource(string name, bool loop)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            AudioSource s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            // Sons em loop são 2D; os efeitos começam 2D e viram 3D quando têm posição.
            s.spatialBlend = 0f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = 2f;
            s.maxDistance = 30f;
            return s;
        }

        /// <summary>
        /// Procura no pool uma fonte de áudio que não esteja tocando.
        /// </summary>
        /// <returns>Uma fonte livre, ou a primeira do pool caso todas estejam ocupadas.</returns>
        private AudioSource GetFreeSource()
        {
            foreach (AudioSource s in pool)
            {
                if (!s.isPlaying) return s;
            }
            return pool[0];
        }

        /// <summary>
        /// Toca um efeito sonoro posicionado no mundo (som 3D).
        /// </summary>
        /// <param name="clip">Som a ser tocado. Se for nulo, nada acontece.</param>
        /// <param name="position">Posição de onde o som parte.</param>
        /// <param name="volume">Volume relativo (0 a 1).</param>
        /// <param name="pitchVariation">Variação aleatória de tom, para o som não ficar repetitivo.</param>
        public void PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float pitchVariation = 0.08f)
        {
            if (clip == null) return;
            AudioSource s = GetFreeSource();
            // Posiciona a fonte e a torna 3D.
            s.transform.position = position;
            s.spatialBlend = 1f;
            s.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            s.volume = volume * sfxVolume;
            s.clip = clip;
            s.Play();
        }

        /// <summary>
        /// Toca um efeito sonoro "de interface" (2D, mesmo volume em qualquer lugar).
        /// </summary>
        /// <param name="clip">Som a ser tocado. Se for nulo, nada acontece.</param>
        /// <param name="volume">Volume relativo (0 a 1).</param>
        public void Play2D(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource s = GetFreeSource();
            s.spatialBlend = 0f;
            s.pitch = 1f;
            s.volume = volume * sfxVolume;
            s.clip = clip;
            s.Play();
        }

        /// <summary>
        /// Toca um passo aleatório do vetor de passos.
        /// </summary>
        /// <param name="position">Posição dos pés do personagem.</param>
        /// <param name="volume">Volume relativo (0 a 1).</param>
        public void PlayFootstep(Vector3 position, float volume)
        {
            if (footsteps == null || footsteps.Length == 0) return;
            // Escolhe um índice aleatório do vetor de sons de passos.
            AudioClip clip = footsteps[Random.Range(0, footsteps.Length)];
            PlayAt(clip, position, volume, 0.12f);
        }

        /// <summary>
        /// Troca o som ambiente (floresta ou caverna).
        /// </summary>
        /// <param name="clip">Novo som ambiente em loop. Nulo silencia o ambiente.</param>
        public void SetAmbience(AudioClip clip)
        {
            if (ambienceSource == null) return;
            if (ambienceSource.clip == clip && ambienceSource.isPlaying) return;
            ambienceSource.clip = clip;
            ambienceSource.volume = ambienceVolume;
            if (clip != null) ambienceSource.Play(); else ambienceSource.Stop();
        }

        /// <summary>
        /// Abaixa ou restaura o volume da música (usado na pausa).
        /// </summary>
        /// <param name="muffled">true para deixar a música mais baixa.</param>
        public void SetMusicMuffled(bool muffled)
        {
            if (musicSource != null) musicSource.volume = muffled ? musicVolume * 0.35f : musicVolume;
        }
    }
}
