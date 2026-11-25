using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Câmera em terceira pessoa que orbita ao redor de John com o mouse,
    /// evita atravessar paredes (SphereCast) e pode tremer (armadilhas, explosões).
    /// No Nível 2 também fica sobre o ombro e aproxima ao mirar (os valores padrão mantêm o Nível 1 igual).
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
        /// <summary>Camadas que bloqueiam a câmera (o Nível 2 ignora inimigos e projéteis).</summary>
        public LayerMask collisionMask = Physics.DefaultRaycastLayers;

        /// <summary>Deslocamento lateral do pivô (x = direita), sempre aplicado. Zero no Nível 1.</summary>
        [Header("Ombro e mira (Nível 2)")]
        public Vector3 shoulderOffset = Vector3.zero;
        /// <summary>Deslocamento do pivô enquanto mira.</summary>
        public Vector3 aimShoulderOffset = new Vector3(0.65f, 0.1f, 0f);
        /// <summary>Distância da câmera enquanto mira.</summary>
        public float aimDistance = 2.1f;
        /// <summary>Campo de visão enquanto mira.</summary>
        public float aimFieldOfView = 48f;
        /// <summary>Sensibilidade relativa do mouse enquanto mira.</summary>
        public float aimSensitivityScale = 0.6f;

        /// <summary>Ligado pelo sistema de combate enquanto o jogador mira (botão direito).</summary>
        public bool Aiming { get; set; }
        /// <summary>Ângulo horizontal atual da órbita.</summary>
        public float Yaw => yaw;
        /// <summary>Ângulo vertical atual da órbita (positivo = olhando para baixo).</summary>
        public float Pitch => pitch;

        private float yaw;
        private float pitch = 15f;
        private float currentDistance;
        private Vector3 smoothedTarget;
        private float shakeTime;
        private float shakeDuration;
        private float shakeMagnitude;
        private float aimBlend;
        private Camera cam;
        private float baseFieldOfView = 60f;

        /// <summary>
        /// Registra o singleton e inicializa ângulos a partir da rotação atual.
        /// </summary>
        private void Awake()
        {
            Instance = this;
            yaw = transform.eulerAngles.y;
            currentDistance = distance;
            cam = GetComponent<Camera>();
            if (cam != null) baseFieldOfView = cam.fieldOfView;
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
            // Transição suave para a mira sobre o ombro (Nível 2). No Nível 1 fica sempre em zero.
            aimBlend = Mathf.MoveTowards(aimBlend, Aiming ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            if (canAct)
            {
                Vector2 look = GameInput.Look();
                float sens = sensitivity * Mathf.Lerp(1f, aimSensitivityScale, aimBlend);
                yaw += look.x * sens;
                pitch -= look.y * sens;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            // Suaviza o ponto seguido para evitar trancos.
            Vector3 desiredTarget = target.position + targetOffset;
            smoothedTarget = Vector3.Lerp(smoothedTarget, desiredTarget, 1f - Mathf.Exp(-followSmooth * Time.unscaledDeltaTime));

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rotation * Vector3.back;

            // Pivô sobre o ombro (Nível 2): desloca para o lado sem entrar na parede.
            Vector3 pivot = smoothedTarget;
            Vector3 shoulder = Vector3.Lerp(shoulderOffset, aimShoulderOffset, aimBlend);
            if (shoulder.sqrMagnitude > 0.0001f)
            {
                Vector3 side = Quaternion.Euler(0f, yaw, 0f) * shoulder;
                float len = side.magnitude;
                if (Physics.SphereCast(smoothedTarget, collisionRadius, side / len, out RaycastHit sideHit, len, collisionMask, QueryTriggerInteraction.Ignore))
                    len = Mathf.Max(0f, sideHit.distance - 0.05f);
                pivot = smoothedTarget + side.normalized * len;
            }
            float baseDistance = Mathf.Lerp(distance, aimDistance, aimBlend);
            if (cam != null) cam.fieldOfView = Mathf.Lerp(baseFieldOfView, aimFieldOfView, aimBlend);

            // Se houver parede entre o personagem e a câmera, aproxima a câmera.
            float wanted = baseDistance;
            if (Physics.SphereCast(pivot, collisionRadius, back, out RaycastHit hit, baseDistance,
                collisionMask, QueryTriggerInteraction.Ignore))
            {
                wanted = Mathf.Max(minDistance, hit.distance - 0.05f);
            }
            // Aproxima rápido (para não atravessar) e afasta devagar.
            float speed = wanted < currentDistance ? 30f : 4f;
            currentDistance = Mathf.Lerp(currentDistance, wanted, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));

            transform.position = pivot + back * currentDistance + GetShakeOffset();
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
