using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Plataforma que vai e volta entre dois pontos (Rigidbody cinemático movido com MovePosition),
    /// com pausa nas pontas. Serve para plataformas laterais e elevadores.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(Rigidbody), typeof(PlatformMotion))]
    public class MovingPlatform : MonoBehaviour
    {
        /// <summary>Deslocamento do ponto B em relação à posição inicial (A).</summary>
        public Vector3 offset = new Vector3(4f, 0f, 0f);
        /// <summary>Velocidade (m/s).</summary>
        public float speed = 1.6f;
        /// <summary>Pausa em cada ponta (s).</summary>
        public float waitTime = 0.8f;
        /// <summary>Fase inicial (0 a 1) para plataformas vizinhas não andarem juntas.</summary>
        [Range(0f, 1f)] public float startPhase = 0f;

        private Rigidbody rb;
        private PlatformMotion motion;
        private Vector3 a, b;
        private float t;
        private int direction = 1;
        private float waitUntil;

        /// <summary>Configura o corpo cinemático e os pontos.</summary>
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            motion = GetComponent<PlatformMotion>();
            a = transform.position;
            b = a + offset;
            t = startPhase;
            rb.position = Vector3.Lerp(a, b, Ease(t));
        }

        /// <summary>
        /// Avança o percurso (ida e volta) e registra o deslocamento para quem está em cima.
        /// </summary>
        private void FixedUpdate()
        {
            Vector3 current = rb.position;
            if (Time.time >= waitUntil)
            {
                float length = Mathf.Max(0.01f, offset.magnitude);
                t += direction * speed * Time.fixedDeltaTime / length;
                if (t >= 1f || t <= 0f)
                {
                    t = Mathf.Clamp01(t);
                    direction = -direction;
                    waitUntil = Time.time + waitTime;
                }
            }
            Vector3 next = Vector3.Lerp(a, b, Ease(t));
            motion.Record(current, next - current, Quaternion.identity);
            rb.MovePosition(next);
        }

        /// <summary>Suaviza a partida e a chegada.</summary>
        /// <param name="x">0 a 1.</param>
        /// <returns>Valor suavizado.</returns>
        private static float Ease(float x)
        {
            return x * x * (3f - 2f * x);
        }

        /// <summary>Mostra o percurso no editor.</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 start = Application.isPlaying ? a : transform.position;
            Gizmos.DrawWireSphere(start, 0.3f);
            Gizmos.DrawWireSphere(start + offset, 0.3f);
        }
    }
}
