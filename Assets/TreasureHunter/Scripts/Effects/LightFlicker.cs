using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Faz uma luz tremeluzir como fogo (tochas de parede), usando ruído Perlin.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        /// <summary>Variação relativa da intensidade (0.3 = ±30%).</summary>
        public float amount = 0.3f;
        /// <summary>Velocidade da variação.</summary>
        public float speed = 6f;

        private Light lightComponent;
        private float baseIntensity;
        private float seed;

        /// <summary>
        /// Guarda a intensidade original e sorteia uma semente de ruído.
        /// </summary>
        private void Awake()
        {
            lightComponent = GetComponent<Light>();
            baseIntensity = lightComponent.intensity;
            seed = Random.value * 100f;
        }

        /// <summary>
        /// Varia a intensidade a cada quadro.
        /// </summary>
        private void Update()
        {
            float n = Mathf.PerlinNoise(Time.time * speed, seed) * 2f - 1f;
            lightComponent.intensity = baseIntensity * (1f + n * amount);
        }
    }
}
