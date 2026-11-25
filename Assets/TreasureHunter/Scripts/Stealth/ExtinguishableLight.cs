using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Tocha de parede, braseiro ou lampião dos ogros que John pode apagar (E) para criar sombra no caminho
    /// e acender de novo. Apagar faz um pouco de barulho (chiado). Volta acesa quando o trecho é reiniciado.
    /// </summary>
    public class ExtinguishableLight : Interactable, IResettable
    {
        /// <summary>Trecho ao qual pertence (reinício).</summary>
        public int roomIndex;
        public Light[] lights;
        public ParticleSystem[] flames;
        /// <summary>Partes que brilham só acesas (brasa, chama voxel).</summary>
        public GameObject[] litVisuals;
        public GameObject[] extinguishEffects;
        public AudioClip extinguishClip;
        public AudioClip igniteClip;
        public float noiseRadius = 3f;
        public bool startLit = true;

        /// <summary>Está acesa.</summary>
        public bool Lit { get; private set; }
        /// <summary>Trecho (IResettable).</summary>
        public int RoomIndex => roomIndex;

        private float[] intensities;

        /// <summary>Guarda as intensidades e aplica o estado inicial.</summary>
        private void Awake()
        {
            intensities = new float[lights != null ? lights.Length : 0];
            for (int i = 0; i < intensities.Length; i++) intensities[i] = lights[i] != null ? lights[i].intensity : 1f;
            Apply(startLit, false);
        }

        /// <summary>Texto da interação.</summary>
        /// <returns>"Apagar" ou "Acender".</returns>
        public override string GetPrompt()
        {
            return Lit ? "Apagar a chama" : "Acender a chama";
        }

        /// <summary>
        /// Apaga ou acende.
        /// </summary>
        /// <param name="interactor">Jogador.</param>
        public override void Interact(PlayerInteractor interactor)
        {
            Apply(!Lit, true);
            if (!Lit) StealthUtil.UnlockSkill("sombras");
        }

        /// <summary>
        /// Aplica o estado.
        /// </summary>
        /// <param name="lit">Acesa.</param>
        /// <param name="feedback">Com som, fumaça e ruído.</param>
        public void Apply(bool lit, bool feedback)
        {
            Lit = lit;
            if (lights != null)
                for (int i = 0; i < lights.Length; i++)
                    if (lights[i] != null)
                    {
                        lights[i].enabled = lit;
                        if (lit && intensities != null && i < intensities.Length) lights[i].intensity = intensities[i];
                    }
            if (flames != null)
                foreach (ParticleSystem f in flames)
                {
                    if (f == null) continue;
                    if (lit) f.Play(true);
                    else f.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            if (litVisuals != null) foreach (GameObject g in litVisuals) if (g != null) g.SetActive(lit);
            if (!feedback) return;
            Vector3 p = transform.position + Vector3.up * 0.4f;
            if (!lit) StealthUtil.SpawnAll(extinguishEffects, p);
            AudioClip clip = lit ? igniteClip : extinguishClip;
            if (clip != null) AudioSource.PlayClipAtPoint(clip, p, 0.8f);
            NoiseSystem.Emit(transform.position, noiseRadius, NoiseType.Handling, gameObject);
            StealthUtil.Hint(lit ? "Chama acesa." : "Chama apagada: este canto ficou na sombra.");
        }

        /// <summary>Volta ao estado inicial (reinício do trecho).</summary>
        public void ResetState()
        {
            Apply(startLit, false);
        }
    }
}
