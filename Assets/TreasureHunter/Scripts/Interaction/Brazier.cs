using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Braseiro antigo que o jogador acende com a tocha (E). Ao acender, liga fogo,
    /// fumaça e luz, e envia sinal (por exemplo, para revelar os símbolos da Sala 3).
    /// </summary>
    public class Brazier : Interactable
    {
        /// <summary>Partículas de fogo.</summary>
        public ParticleSystem fire;
        /// <summary>Partículas de fumaça.</summary>
        public ParticleSystem smoke;
        /// <summary>Luz do fogo.</summary>
        public Light fireLight;
        /// <summary>Receptores avisados quando o braseiro acende.</summary>
        public SignalReceiver[] targets;
        /// <summary>Começa aceso (decoração).</summary>
        public bool startLit = false;
        /// <summary>Habilidade desbloqueada ao acender (opcional).</summary>
        public string skillOnLight = "";

        /// <summary>Indica se o braseiro está aceso.</summary>
        public bool IsLit { get; private set; }

        private float baseIntensity = 3f;
        private float noiseSeed;

        /// <summary>
        /// Aplica o estado inicial (aceso ou apagado).
        /// </summary>
        private void Start()
        {
            noiseSeed = Random.value * 10f;
            if (fireLight != null) baseIntensity = fireLight.intensity;
            SetLitVisuals(startLit);
            IsLit = startLit;
        }

        /// <summary>
        /// Faz a luz do fogo tremeluzir.
        /// </summary>
        private void Update()
        {
            if (!IsLit || fireLight == null) return;
            float n = Mathf.PerlinNoise(Time.time * 6f, noiseSeed);
            fireLight.intensity = baseIntensity * (0.75f + n * 0.5f);
        }

        /// <summary>
        /// Só pode ser aceso uma vez.
        /// </summary>
        /// <param name="interactor">Quem está interagindo.</param>
        /// <returns>true se ainda estiver apagado.</returns>
        public override bool CanInteract(PlayerInteractor interactor)
        {
            return base.CanInteract(interactor) && !IsLit;
        }

        /// <summary>
        /// Acende o braseiro com a tocha.
        /// </summary>
        /// <param name="interactor">Quem está interagindo.</param>
        public override void Interact(PlayerInteractor interactor)
        {
            IsLit = true;
            SetLitVisuals(true);
            SignalReceiver.Broadcast(targets, true, this);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.fireIgnite, transform.position);
            if (!string.IsNullOrEmpty(skillOnLight) && SkillManager.Instance != null) SkillManager.Instance.Unlock(skillOnLight);
        }

        /// <summary>
        /// Liga ou desliga fogo, fumaça e luz.
        /// </summary>
        /// <param name="lit">true para aceso.</param>
        private void SetLitVisuals(bool lit)
        {
            if (fire != null) { if (lit) fire.Play(); else fire.Stop(); }
            if (smoke != null) { if (lit) smoke.Play(); else smoke.Stop(); }
            if (fireLight != null) fireLight.enabled = lit;
        }
    }
}
