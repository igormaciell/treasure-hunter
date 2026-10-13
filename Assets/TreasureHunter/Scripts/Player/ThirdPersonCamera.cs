using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Câmera em terceira pessoa que orbita ao redor de John com o mouse,
    /// evita atravessar paredes (SphereCast) e pode tremer (armadilhas, explosões).
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        /// <summary>Instância única acessível por qualquer script (para tremores e reposicionamento).</summary>
        public static ThirdPersonCamera Instance { get; private set; }

        /// <summary>Alvo seguido pela câmera (o jogador).</summary>
        [Header("Alvo")]
        public Transform target;
        public Vector3 targetOffset = new Vector3(0f, 1.55f, 0f);

        [Header("Órbita")]
        public float distance = 4.2f;
        public float minDistance = 0.6f;
        public float sensitivity = 2.2f;
        public float minPitch = -30f;
        public float maxPitch = 70f;
        public float followSmooth = 18f;

        [Header("Colisão")]
        public float collisionRadius = 0.25f;

        private float yaw;
        private float pitch = 15f;
        private float currentDistance;
        private Vector3 smoothedTarget;
        private float shakeTime;
        private float shakeDuration;
        private float shakeMagnitude;

        /// <summary>
        /// Registra o singleton e inicializa ângulos a partir da rotação atual.
        /// </summary>
        private void Awake()
        {
            Instance = this;
            yaw = transform.eulerAngles.y;
            currentDistance = distance;
        }

        /// <summary>
        /// Posiciona a câmera imediatamente atrás do alvo ao começar.
        /// </summary>
        private void Start()
        {
            if (target != null) smoothedTarget = target.position + targetOffset;
        }

        /// <summary>
        /// Atualiza a câmera depois de todo o movimento do quadro (LateUpdate),
        /// aplicando rotação pelo mouse, colisão com o cenário e tremor.
        /// </summary>
        private void LateUpdate()
        {
            if (target == null) return;

            // Gira a órbita conforme o mouse, somente quando o jogador pode agir.
            bool canAct = GameManager.Instance == null || GameManager.Instance.CanPlayerAct();
            if (canAct)
            {
                Vector2 look = GameInput.Look();
                yaw += look.x * sensitivity;
                pitch -= look.y * sensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            // Suaviza o ponto seguido para evitar trancos.
            Vector3 desiredTarget = target.position + targetOffset;
            smoothedTarget = Vector3.Lerp(smoothedTarget, desiredTarget, 1f - Mathf.Exp(-followSmooth * Time.unscaledDeltaTime));

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rotation * Vector3.back;

            // Se houver parede entre o personagem e a câmera, aproxima a câmera.
            float wanted = distance;
            if (Physics.SphereCast(smoothedTarget, collisionRadius, back, out RaycastHit hit, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                wanted = Mathf.Max(minDistance, hit.distance - 0.05f);
            }
            // Aproxima rápido (para não atravessar) e afasta devagar.
            float speed = wanted < currentDistance ? 30f : 4f;
            currentDistance = Mathf.Lerp(currentDistance, wanted, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));

            transform.position = smoothedTarget + back * currentDistance + GetShakeOffset();
            transform.rotation = rotation;
        }

        /// <summary>
        /// Calcula o deslocamento aleatório do tremor, que diminui com o tempo.
        /// </summary>
        /// <returns>Deslocamento a ser somado à posição da câmera.</returns>
        private Vector3 GetShakeOffset()
        {
            if (shakeTime <= 0f) return Vector3.zero;
            shakeTime -= Time.unscaledDeltaTime;
            float strength = shakeMagnitude * Mathf.Clamp01(shakeTime / shakeDuration);
            return Random.insideUnitSphere * strength;
        }

        /// <summary>
        /// Faz a câmera tremer.
        /// </summary>
        /// <param name="duration">Duração do tremor em segundos.</param>
        /// <param name="magnitude">Intensidade máxima do deslocamento (em metros).</param>
        public void Shake(float duration, float magnitude)
        {
            // Mantém o tremor mais forte caso já exista um em andamento.
            if (shakeTime > 0f && magnitude < shakeMagnitude) return;
            shakeDuration = Mathf.Max(0.01f, duration);
            shakeTime = duration;
            shakeMagnitude = magnitude;
        }

        /// <summary>
        /// Coloca a câmera imediatamente atrás do personagem (após teletransporte).
        /// </summary>
        /// <param name="facing">Rotação para onde o personagem está olhando.</param>
        public void SnapBehindTarget(Quaternion facing)
        {
            yaw = facing.eulerAngles.y;
            pitch = 15f;
            if (target != null) smoothedTarget = target.position + targetOffset;
            currentDistance = distance;
        }
    }
}
