using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Percepção sonora reutilizável: recebe os ruídos do <see cref="NoiseSystem"/>, verifica a distância
    /// (com alcance reduzido se houver parede entre a origem e o ouvido) e avisa o dono com a intensidade
    /// percebida (1 = colado na origem, 0 = no limite do alcance).
    /// </summary>
    public class HearingSensor : MonoBehaviour, INoiseListener
    {
        /// <summary>Ponto de escuta (cabeça).</summary>
        public Transform ear;
        /// <summary>Multiplicador do alcance dos ruídos (ouvido bom &gt; 1).</summary>
        public float hearingMultiplier = 1.15f;
        /// <summary>Alcance restante quando há parede no caminho.</summary>
        public float occlusionFactor = 0.55f;
        /// <summary>Disparos atravessam melhor as paredes.</summary>
        public float gunshotOcclusionFactor = 0.85f;
        /// <summary>Ignora ruídos do cenário (só ouve o jogador e as pedras).</summary>
        public bool onlyPlayerAndImpacts = false;

        /// <summary>Último ruído ouvido.</summary>
        public NoiseEvent LastHeard { get; private set; }
        /// <summary>Instante do último ruído ouvido.</summary>
        public float LastHeardTime { get; private set; } = -100f;
        /// <summary>Disparado ao ouvir algo (ruído, intensidade 0 a 1).</summary>
        public event System.Action<NoiseEvent, float> Heard;

        /// <summary>Começa a ouvir.</summary>
        private void OnEnable()
        {
            NoiseSystem.Register(this);
        }

        /// <summary>Para de ouvir.</summary>
        private void OnDisable()
        {
            NoiseSystem.Unregister(this);
        }

        /// <summary>
        /// Um ruído aconteceu: ouviu?
        /// </summary>
        /// <param name="noise">Ruído.</param>
        public void OnNoise(NoiseEvent noise)
        {
            if (!isActiveAndEnabled) return;
            if (onlyPlayerAndImpacts && !noise.byPlayer && noise.type != NoiseType.Impact) return;
            Vector3 earPos = ear != null ? ear.position : transform.position + Vector3.up * 2f;
            float d = Vector3.Distance(earPos, noise.position);
            float r = noise.radius * hearingMultiplier;
            if (d > r) return;
            if (Physics.Linecast(earPos, noise.position + Vector3.up * 0.4f, StealthUtil.SightMask, QueryTriggerInteraction.Ignore))
            {
                r *= noise.type == NoiseType.Gunshot ? gunshotOcclusionFactor : occlusionFactor;
                if (d > r) return;
            }
            float strength = Mathf.Clamp01(1f - d / Mathf.Max(0.1f, r));
            LastHeard = noise;
            LastHeardTime = Time.time;
            Heard?.Invoke(noise, strength);
        }
    }
}
