using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Lâmina pendular presa ao teto por uma articulação (HingeJoint). O balanço é físico (gravidade);
    /// para não parar, o script aplica Torque (AddTorque) a favor do movimento perto do ponto mais baixo,
    /// mantendo a amplitude desejada. A lâmina (TrapDamage) fere quem passar na hora errada — inclusive esqueletos.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(HingeJoint))]
    public class PendulumTrap : MonoBehaviour
    {
        /// <summary>Comprimento do braço (pivô até a lâmina), usado para a inércia.</summary>
        public float armLength = 3f;
        /// <summary>Amplitude desejada do balanço (graus).</summary>
        public float targetAmplitude = 55f;
        /// <summary>Aceleração angular (rad/s²) aplicada por torque para manter o balanço.</summary>
        public float pumpAcceleration = 2.5f;
        /// <summary>Velocidade angular do empurrão inicial (rad/s).</summary>
        public float startSpin = 1.6f;

        private Rigidbody rb;
        private HingeJoint hinge;
        private float amplitude;
        private float lastVelocitySign = 1f;
        private float nextSound;
        private float inertiaAtPivot;

        /// <summary>
        /// Configura a massa como um pêndulo simples (quase toda na lâmina, na ponta do braço)
        /// e dá o empurrão inicial girando em torno do pivô.
        /// </summary>
        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            hinge = GetComponent<HingeJoint>();
            rb.maxAngularVelocity = 12f;
            rb.centerOfMass = new Vector3(0f, -armLength, 0f);
            rb.inertiaTensor = Vector3.one * rb.mass * 0.2f;
            rb.inertiaTensorRotation = Quaternion.identity;
            // Momento de inércia em relação ao pivô (teorema dos eixos paralelos).
            inertiaAtPivot = rb.inertiaTensor.x + rb.mass * armLength * armLength;

            // Empurrão inicial: velocidade angular e a velocidade linear coerente com a rotação em torno do pivô.
            Vector3 axis = WorldAxis();
            rb.angularVelocity = axis * startSpin;
            rb.linearVelocity = Vector3.Cross(axis * startSpin, rb.worldCenterOfMass - transform.position);
        }

        /// <summary>
        /// Mede a amplitude a cada virada e bombeia energia no ponto mais baixo, a favor do movimento.
        /// </summary>
        private void FixedUpdate()
        {
            float angle = hinge.angle;
            float velocity = hinge.velocity;
            float sign = velocity >= 0f ? 1f : -1f;
            if (sign != lastVelocitySign)
            {
                // Virada do balanço: o ângulo atual é a amplitude alcançada.
                amplitude = Mathf.Abs(angle);
                lastVelocitySign = sign;
            }

            // Torque = aceleração desejada × inércia no pivô, aplicado a favor do movimento.
            if (Mathf.Abs(angle) < 12f && amplitude < targetAmplitude)
                rb.AddTorque(WorldAxis() * (sign * pumpAcceleration * inertiaAtPivot), ForceMode.Force);

            // Som de corte passando perto do chão.
            if (Mathf.Abs(angle) < 6f && Mathf.Abs(velocity) > 40f && Time.time >= nextSound)
            {
                nextSound = Time.time + 0.5f;
                CombatAudio a = CombatAudio.Instance;
                if (a != null) CombatAudio.Play(a.pendulum, transform.position + Vector3.down * armLength, 0.8f);
            }
        }

        /// <summary>
        /// Eixo da articulação no espaço do mundo.
        /// </summary>
        /// <returns>Eixo normalizado.</returns>
        private Vector3 WorldAxis()
        {
            return transform.TransformDirection(hinge.axis).normalized;
        }
    }
}
