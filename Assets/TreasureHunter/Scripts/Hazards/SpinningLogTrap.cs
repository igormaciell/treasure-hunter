using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Tronco com espinhos que gira em torno de um eixo vertical, movido por TORQUE (AddTorque).
    /// O jogador precisa saltar sobre ele no momento certo. Os espinhos (filhos com Hazard) causam dano.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SpinningLogTrap : MonoBehaviour
    {
        /// <summary>Torque aplicado a cada passo de física.</summary>
        public float torque = 40f;
        /// <summary>Velocidade angular máxima (rad/s).</summary>
        public float maxAngularSpeed = 1.6f;
        /// <summary>Fonte de som em loop do giro (opcional).</summary>
        public AudioSource whooshSource;

        private Rigidbody rb;

        /// <summary>
        /// Configura o corpo físico: posição travada e rotação somente no eixo Y.
        /// </summary>
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.maxAngularVelocity = maxAngularSpeed;
            rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        /// <summary>
        /// Aplica torque em Y até atingir a velocidade máxima de giro.
        /// </summary>
        private void FixedUpdate()
        {
            if (rb.angularVelocity.y < maxAngularSpeed)
                rb.AddTorque(Vector3.up * torque, ForceMode.Acceleration);

            if (whooshSource != null) whooshSource.pitch = 0.8f + rb.angularVelocity.y / maxAngularSpeed * 0.4f;
        }
    }
}
