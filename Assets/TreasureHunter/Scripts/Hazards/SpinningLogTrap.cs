using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Tronco com espinhos que guarda o portão da Sala 4. Gira sem parar em torno de um eixo vertical,
    /// movido por TORQUE (AddTorque), e só para quando recebe o sinal da alavanca:
    /// então freia (torque contrário), recolhe os espinhos e afunda no chão, liberando a passagem.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SpinningLogTrap : SignalReceiver
    {
        /// <summary>Torque (aceleração angular) aplicado a cada passo de física enquanto gira.</summary>
        public float torque = 40f;
        /// <summary>Velocidade angular máxima (rad/s).</summary>
        public float maxAngularSpeed = 1.6f;
        /// <summary>Torque de frenagem aplicado depois que a alavanca é puxada.</summary>
        public float brakeTorque = 3f;
        /// <summary>Quanto o tronco afunda no chão depois de parar (metros).</summary>
        public float sinkDepth = 0.85f;
        /// <summary>Velocidade com que o tronco afunda (metros por segundo).</summary>
        public float sinkSpeed = 0.6f;
        /// <summary>Fonte de som em loop do giro (opcional).</summary>
        public AudioSource whooshSource;
        /// <summary>Objeto com os gatilhos de dano dos espinhos (desligado quando o tronco para).</summary>
        public GameObject spikes;
        /// <summary>Frase de John quando o tronco termina de parar (opcional).</summary>
        public string lineOnStop = "";

        /// <summary>Estados da armadilha.</summary>
        private enum TrapState { Spinning, Braking, Sinking, Stopped }

        /// <summary>Indica se o tronco ainda está girando ou freando (armadilha ativa).</summary>
        public bool IsActive => state == TrapState.Spinning || state == TrapState.Braking;

        private Rigidbody rb;
        private TrapState state = TrapState.Spinning;
        private float sunk;

        /// <summary>
        /// Configura o corpo físico: posição travada e rotação somente no eixo Y.
        /// </summary>
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.maxAngularVelocity = maxAngularSpeed;
            rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        /// <summary>
        /// Acorda o corpo físico para garantir que o giro comece assim que a cena abre.
        /// </summary>
        private void Start()
        {
            rb.WakeUp();
        }

        /// <summary>
        /// Passo de física: acelera o giro com torque ou, depois da alavanca, freia com torque contrário.
        /// </summary>
        private void FixedUpdate()
        {
            if (state == TrapState.Spinning)
            {
                // Mantém o giro: aplica torque em Y até atingir a velocidade máxima.
                if (rb.angularVelocity.y < maxAngularSpeed)
                    rb.AddTorque(Vector3.up * torque, ForceMode.Acceleration);
            }
            else if (state == TrapState.Braking)
            {
                float w = rb.angularVelocity.y;
                if (Mathf.Abs(w) > 0.08f)
                {
                    // Torque no sentido contrário ao giro: o tronco desacelera aos poucos.
                    rb.AddTorque(Vector3.up * -Mathf.Sign(w) * brakeTorque, ForceMode.Acceleration);
                }
                else
                {
                    StopAndStartSinking();
                }
            }

            if (whooshSource != null)
                whooshSource.pitch = 0.8f + Mathf.Abs(rb.angularVelocity.y) / maxAngularSpeed * 0.4f;
        }

        /// <summary>
        /// Afunda o tronco no chão (Translate) depois que ele parou.
        /// </summary>
        private void Update()
        {
            if (state != TrapState.Sinking) return;
            float step = Mathf.Min(sinkSpeed * Time.deltaTime, sinkDepth - sunk);
            sunk += step;
            transform.Translate(Vector3.down * step, Space.World);
            if (sunk >= sinkDepth - 0.001f) state = TrapState.Stopped;
        }

        /// <summary>
        /// Zera o giro, transforma o tronco em cinemático, recolhe os espinhos e começa a afundar.
        /// </summary>
        private void StopAndStartSinking()
        {
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            if (spikes != null) spikes.SetActive(false);
            if (whooshSource != null) whooshSource.Stop();
            state = TrapState.Sinking;

            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.doorGrind, transform.position, 0.7f);
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.5f, 0.04f);
            if (!string.IsNullOrEmpty(lineOnStop) && GameManager.Instance != null && GameManager.Instance.ui != null)
                GameManager.Instance.ui.ShowLine(lineOnStop);
        }

        /// <summary>
        /// Recebe o sinal da alavanca: começa a frear (somente uma vez).
        /// </summary>
        /// <param name="on">Novo estado do sinal.</param>
        protected override void OnStateChanged(bool on)
        {
            if (on && state == TrapState.Spinning) state = TrapState.Braking;
        }
    }
}
