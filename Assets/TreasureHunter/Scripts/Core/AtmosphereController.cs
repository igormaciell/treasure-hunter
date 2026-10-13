using System;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Conjunto de parâmetros visuais/sonoros de uma área (superfície ou subterrâneo).
    /// </summary>
    [Serializable]
    public class AtmosphereProfile
    {
        public string name = "Área";
        public Color ambientColor = new Color(0.3f, 0.3f, 0.3f);
        public Color fogColor = Color.gray;
        public float fogDensity = 0.02f;
        public bool sunEnabled = true;
        public AudioClip ambience;
    }

    /// <summary>
    /// Aplica a atmosfera (luz ambiente, neblina, sol e som ambiente) de cada área do nível.
    /// O Nível 1 começa na floresta iluminada e, após a queda, muda para o subterrâneo escuro.
    /// </summary>
    public class AtmosphereController : MonoBehaviour
    {
        /// <summary>Instância única acessível por qualquer script.</summary>
        public static AtmosphereController Instance { get; private set; }

        /// <summary>Luz direcional que representa o sol da superfície.</summary>
        public Light sun;
        /// <summary>Perfis disponíveis (0 = superfície, 1 = subterrâneo).</summary>
        public AtmosphereProfile[] profiles;
        /// <summary>Perfil aplicado ao iniciar a cena.</summary>
        public int startProfile = 0;

        /// <summary>
        /// Registra o singleton.
        /// </summary>
        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Aplica o perfil inicial.
        /// </summary>
        private void Start()
        {
            Apply(startProfile);
        }

        /// <summary>
        /// Aplica um perfil de atmosfera às configurações de renderização da cena.
        /// </summary>
        /// <param name="index">Índice do perfil no vetor profiles.</param>
        public void Apply(int index)
        {
            if (profiles == null || index < 0 || index >= profiles.Length) return;
            AtmosphereProfile p = profiles[index];

            // Luz ambiente e neblina controlam a sensação de espaço aberto ou fechado.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = p.ambientColor;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = p.fogColor;
            RenderSettings.fogDensity = p.fogDensity;

            if (sun != null) sun.enabled = p.sunEnabled;
            if (AudioManager.Instance != null) AudioManager.Instance.SetAmbience(p.ambience);
        }
    }
}
