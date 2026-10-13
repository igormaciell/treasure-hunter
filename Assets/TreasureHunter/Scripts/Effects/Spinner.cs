using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Gira continuamente um objeto (transform.Rotate) e, opcionalmente, o faz flutuar.
    /// Usado nos tesouros, no artefato e na câmera do menu principal.
    /// </summary>
    public class Spinner : MonoBehaviour
    {
        /// <summary>Velocidade de rotação em graus por segundo, por eixo.</summary>
        public Vector3 degreesPerSecond = new Vector3(0f, 90f, 0f);
        /// <summary>Amplitude da flutuação vertical (0 = sem flutuação).</summary>
        public float bobAmplitude = 0.1f;
        /// <summary>Frequência da flutuação.</summary>
        public float bobFrequency = 1.5f;
        /// <summary>Usa tempo não escalado (continua girando com o jogo pausado).</summary>
        public bool unscaledTime = false;

        private Vector3 startLocalPosition;
        private float phase;

        /// <summary>
        /// Guarda a posição inicial e sorteia uma fase para os objetos não flutuarem em sincronia.
        /// </summary>
        private void Start()
        {
            startLocalPosition = transform.localPosition;
            phase = Random.value * Mathf.PI * 2f;
        }

        /// <summary>
        /// Aplica a rotação e a flutuação.
        /// </summary>
        private void Update()
        {
            float dt = unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = unscaledTime ? Time.unscaledTime : Time.time;
            transform.Rotate(degreesPerSecond * dt, Space.Self);
            if (bobAmplitude > 0f)
                transform.localPosition = startLocalPosition + Vector3.up * Mathf.Sin(t * bobFrequency + phase) * bobAmplitude;
        }
    }
}
