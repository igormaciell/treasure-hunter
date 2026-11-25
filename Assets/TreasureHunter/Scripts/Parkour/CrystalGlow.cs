using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Energia interna dos cristais: a emissão e a luz pulsam devagar (cada cristal com sua fase) e dão um clarão
    /// quando John pousa em cima. Usa MaterialPropertyBlock para não criar cópias dos materiais.
    /// </summary>
    public class CrystalGlow : MonoBehaviour, ILandingReceiver
    {
        /// <summary>Renderizadores com material emissivo (vazio = todos os filhos).</summary>
        public Renderer[] glowRenderers;
        /// <summary>Luzes que acompanham o pulso.</summary>
        public Light[] lights;
        /// <summary>Cor da emissão no máximo.</summary>
        [ColorUsage(false, true)] public Color emission = new Color(0.4f, 1.2f, 2f);
        /// <summary>Pulsos por segundo.</summary>
        public float pulseSpeed = 0.6f;
        /// <summary>Intensidade mínima (fração do máximo).</summary>
        [Range(0f, 1f)] public float minimum = 0.55f;
        /// <summary>Clarão ao pousar (multiplicador extra).</summary>
        public float landFlash = 1.2f;

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock block;
        private float[] baseIntensity;
        private float phase;
        private float flash;

        /// <summary>Guarda as intensidades das luzes e sorteia a fase.</summary>
        private void Awake()
        {
            block = new MaterialPropertyBlock();
            if (glowRenderers == null || glowRenderers.Length == 0) glowRenderers = GetComponentsInChildren<MeshRenderer>(true);
            if (lights == null || lights.Length == 0) lights = GetComponentsInChildren<Light>(true);
            baseIntensity = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++) baseIntensity[i] = lights[i] != null ? lights[i].intensity : 0f;
            phase = Random.value * Mathf.PI * 2f;
        }

        /// <summary>Atualiza o pulso.</summary>
        private void Update()
        {
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 2.5f);
            float k = Mathf.Lerp(minimum, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f + phase)) + flash;
            block.SetColor(EmissionId, emission * k);
            foreach (Renderer r in glowRenderers) if (r != null) r.SetPropertyBlock(block);
            for (int i = 0; i < lights.Length; i++) if (lights[i] != null) lights[i].intensity = baseIntensity[i] * k;
        }

        /// <summary>Clarão e som quando John pousa no cristal.</summary>
        /// <param name="player">Jogador.</param>
        /// <param name="dropHeight">Altura da queda.</param>
        public void OnPlayerLanded(ParkourController player, float dropHeight)
        {
            flash = landFlash;
            ParkourAudio a = ParkourAudio.Instance;
            if (a != null) ParkourAudio.Play(a.crystalChime, transform.position, 0.6f);
        }

        /// <summary>Sem efeito contínuo.</summary>
        /// <param name="player">Jogador.</param>
        public void OnPlayerStanding(ParkourController player) { }
    }
}
