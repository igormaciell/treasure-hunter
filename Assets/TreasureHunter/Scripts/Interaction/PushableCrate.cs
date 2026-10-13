using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Caixote pesado que o jogador empurra com o corpo (AddForce). Desliza com atrito,
    /// para quando o jogador solta, faz som de arrasto e pode voltar à posição inicial
    /// quando a sala é reiniciada.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PushableCrate : MonoBehaviour, IResettable
    {
        /// <summary>Sala à qual o caixote pertence.</summary>
        public int roomIndex;
        /// <summary>Velocidade máxima de deslizamento.</summary>
        public float maxSpeed = 2.2f;
        /// <summary>Fonte de áudio em loop do arrasto.</summary>
        public AudioSource scrapeSource;

        /// <summary>Sala à qual o caixote pertence (IResettable).</summary>
        public int RoomIndex => roomIndex;

        private Rigidbody rb;
        private Vector3 startPosition;
        private Quaternion startRotation;

        /// <summary>
        /// Guarda o estado inicial e configura a física do caixote.
        /// </summary>
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            startPosition = transform.position;
            startRotation = transform.rotation;
            // O caixote não tomba: apenas desliza e cai.
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        /// <summary>
        /// Ajusta o volume do som de arrasto de acordo com a velocidade horizontal.
        /// </summary>
        private void Update()
        {
            if (scrapeSource == null) return;
            Vector3 v = rb.linearVelocity;
            float speed = new Vector3(v.x, 0f, v.z).magnitude;
            float targetVolume = Mathf.Clamp01(speed / maxSpeed) * 0.6f;
            scrapeSource.volume = Mathf.Lerp(scrapeSource.volume, targetVolume, Time.deltaTime * 10f);
            if (targetVolume > 0.02f && !scrapeSource.isPlaying) scrapeSource.Play();
            else if (targetVolume <= 0.02f && scrapeSource.volume < 0.02f && scrapeSource.isPlaying) scrapeSource.Stop();
        }

        /// <summary>
        /// Empurra o caixote numa direção horizontal, respeitando a velocidade máxima.
        /// </summary>
        /// <param name="direction">Direção do empurrão (normalizada).</param>
        /// <param name="force">Aceleração aplicada.</param>
        public void Push(Vector3 direction, float force)
        {
            direction.y = 0f;

            // Escolhe o eixo dominante (empurrar "em grade" facilita alinhar com a placa).
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z)) direction = new Vector3(Mathf.Sign(direction.x), 0f, 0f);
            else direction = new Vector3(0f, 0f, Mathf.Sign(direction.z));

            float along = Vector3.Dot(rb.linearVelocity, direction);
            if (along < maxSpeed) rb.AddForce(direction * force, ForceMode.Acceleration);
        }

        /// <summary>
        /// Devolve o caixote à posição e ao estado iniciais (IResettable).
        /// </summary>
        public void ResetState()
        {
            // Zera a velocidade e teleporta o corpo físico de volta ao início.
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = startPosition;
            rb.rotation = startRotation;
            transform.SetPositionAndRotation(startPosition, startRotation);
        }
    }
}
