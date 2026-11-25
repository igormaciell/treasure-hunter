using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Disco de pedra que gira em torno do eixo vertical movido por física: um Rigidbody com a posição travada
    /// recebe AddTorque a cada passo até atingir a velocidade desejada (o atrito angular freia).
    /// Quem está em cima é levado junto (PlatformMotion).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(Rigidbody), typeof(PlatformMotion))]
    public class SpinningPlatform : MonoBehaviour
    {
        /// <summary>Velocidade desejada (graus por segundo; negativo gira ao contrário).</summary>
        public float targetDegreesPerSecond = 28f;
        /// <summary>Ganho do controle de torque.</summary>
        public float torqueGain = 3f;
        /// <summary>Pausa periódica (o disco desacelera e volta a girar) — 0 desliga.</summary>
        public float pauseEvery = 0f;
        public float pauseDuration = 1.5f;

        private Rigidbody rb;
        private PlatformMotion motion;
        private float nextPause, pauseEnd;

        /// <summary>
        /// Trava a posição e a inclinação: só a rotação em Y fica livre.
        /// </summary>
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            motion = GetComponent<PlatformMotion>();
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.angularDamping = 0.6f;
            rb.maxAngularVelocity = 6f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            nextPause = pauseEvery > 0f ? Time.time + pauseEvery : float.MaxValue;
        }

        /// <summary>
        /// Aplica o torque que aproxima a velocidade angular do alvo e registra a rotação do passo.
        /// </summary>
        private void FixedUpdate()
        {
            float target = targetDegreesPerSecond * Mathf.Deg2Rad;
            if (Time.time >= nextPause)
            {
                pauseEnd = Time.time + pauseDuration;
                nextPause = pauseEnd + pauseEvery;
            }
            if (Time.time < pauseEnd) target = 0f;

            float current = rb.angularVelocity.y;
            float accel = (target - current) * torqueGain;
            rb.AddTorque(Vector3.up * accel, ForceMode.Acceleration);

            // Rotação prevista neste passo (velocidade atual + aceleração aplicada).
            float omega = current + accel * Time.fixedDeltaTime;
            Quaternion step = Quaternion.AngleAxis(omega * Mathf.Rad2Deg * Time.fixedDeltaTime, Vector3.up);
            motion.Record(rb.position, Vector3.zero, step);
        }
    }
}
