using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Controla a movimentação de John Craft em terceira pessoa usando física (Rigidbody).
    /// - Movimento por AddForce (aceleração em direção à velocidade desejada).
    /// - Salto por AddForce com altura variável (segurar Espaço pula mais alto).
    /// - Tolerância de salto após sair da borda (coyote time) e salto "memorizado" antes de pousar (buffer), como no GDD.
    /// - Empurra objetos com a tag "Pushable".
    /// - Alimenta o Animator com os parâmetros das animações parado/andando/correndo/pulando/caindo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        /// <summary>Modelo visual que gira para a direção do movimento (o Rigidbody não gira).</summary>
        [Header("Referências")]
        public Transform model;
        /// <summary>Animator do modelo voxel.</summary>
        public Animator animator;
        /// <summary>Câmera usada como referência de direção (frente = para onde a câmera olha).</summary>
        public Transform cameraTransform;

        [Header("Movimento")]
        public float walkSpeed = 3.2f;
        public float runSpeed = 6.2f;
        public float acceleration = 14f;
        public float airControl = 0.45f;
        public float turnSpeed = 12f;

        [Header("Salto")]
        public float jumpVelocity = 5.6f;
        public float fallGravityMultiplier = 2.0f;
        public float lowJumpGravityMultiplier = 2.6f;
        public float coyoteTime = 0.15f;
        public float jumpBufferTime = 0.15f;

        [Header("Chão")]
        public float groundCheckDistance = 0.2f;
        public float maxSlopeAngle = 50f;

        [Header("Empurrar")]
        public float pushForce = 38f;

        [Header("Passos")]
        public float stepIntervalWalk = 0.5f;
        public float stepIntervalRun = 0.32f;

        /// <summary>Indica se o personagem está apoiado no chão.</summary>
        public bool IsGrounded { get; private set; }
        /// <summary>Direção em que o modelo está olhando (usada pela interação).</summary>
        public Vector3 Facing => model != null ? model.forward : transform.forward;

        private Rigidbody rb;
        private CapsuleCollider capsule;
        private Vector3 groundNormal = Vector3.up;
        private Vector3 desiredMove;
        private bool running;
        private float lastGroundedTime = -10f;
        private float lastJumpPressedTime = -10f;
        private float lastJumpTime = -10f;
        private bool jumpHeld;
        private bool wasGrounded;
        private float airTime;
        private float stepTimer;
        private bool pushing;
        private float pushingTimer;

        // Hashes dos parâmetros do Animator (mais eficiente que usar strings a cada quadro).
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int VerticalHash = Animator.StringToHash("VerticalVelocity");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int PushHash = Animator.StringToHash("Pushing");

        /// <summary>
        /// Obtém os componentes e configura o Rigidbody (rotação travada, interpolação).
        /// </summary>
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            // A rotação é feita no modelo; o corpo físico nunca tomba.
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        /// <summary>
        /// Lê a entrada a cada quadro (o movimento físico é aplicado no FixedUpdate)
        /// e atualiza animações e sons de passos.
        /// </summary>
        private void Update()
        {
            bool canAct = GameManager.Instance == null || GameManager.Instance.CanPlayerAct();
            Vector2 input = canAct ? GameInput.Move() : Vector2.zero;
            running = canAct && GameInput.RunHeld();
            jumpHeld = canAct && GameInput.JumpHeld();
            if (canAct && GameInput.JumpPressed()) lastJumpPressedTime = Time.time;

            // Converte a entrada para o espaço da câmera (frente da câmera projetada no chão).
            Vector3 forward = Vector3.forward, right = Vector3.right;
            if (cameraTransform != null)
            {
                forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            }
            desiredMove = forward * input.y + right * input.x;

            RotateModel();
            UpdateAnimator();
            UpdateFootsteps();

            // O estado "empurrando" expira se não houver contato recente com um objeto empurrável.
            pushingTimer -= Time.deltaTime;
            if (pushingTimer <= 0f) pushing = false;
        }

        /// <summary>
        /// Passo de física: verifica o chão, aplica forças de movimento, salto e gravidade extra.
        /// </summary>
        private void FixedUpdate()
        {
            CheckGround();
            ApplyMovement();
            HandleJump();
            ApplyExtraGravity();
        }

        /// <summary>
        /// Lança uma esfera para baixo a partir da base da cápsula para descobrir se há chão.
        /// </summary>
        private void CheckGround()
        {
            float radius = capsule.radius * 0.9f;
            Vector3 origin = transform.position + capsule.center - Vector3.up * (capsule.height * 0.5f - capsule.radius) + Vector3.up * 0.05f;
            // Physics.DefaultRaycastLayers ignora a camada "Ignore Raycast", onde o jogador está.
            bool hit = Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit info,
                groundCheckDistance + 0.05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            // Logo após o salto o chão é ignorado por um instante, para o impulso não ser cancelado.
            bool justJumped = Time.time - lastJumpTime < 0.2f;
            IsGrounded = hit && !justJumped && Vector3.Angle(info.normal, Vector3.up) <= maxSlopeAngle;
            groundNormal = hit ? info.normal : Vector3.up;

            if (IsGrounded)
            {
                // Toca o som de pouso se ficou tempo suficiente no ar.
                if (!wasGrounded && airTime > 0.35f && AudioManager.Instance != null)
                    AudioManager.Instance.PlayAt(AudioManager.Instance.land, transform.position, 0.7f);
                lastGroundedTime = Time.time;
                airTime = 0f;
                // Ao pousar, descarta um pedido de animação de pulo que tenha ficado pendente.
                if (!wasGrounded && animator != null) animator.ResetTrigger(JumpHash);
            }
            else
            {
                airTime += Time.fixedDeltaTime;
            }
            wasGrounded = IsGrounded;
        }

        /// <summary>
        /// Aplica uma força horizontal proporcional à diferença entre a velocidade desejada e a atual.
        /// </summary>
        private void ApplyMovement()
        {
            float targetSpeed = running ? runSpeed : walkSpeed;
            Vector3 targetVelocity = desiredMove * targetSpeed;
            Vector3 velocity = rb.linearVelocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);

            // No chão, o movimento segue a inclinação da rampa.
            if (IsGrounded) targetVelocity = Vector3.ProjectOnPlane(targetVelocity, groundNormal);

            // No chão a diferença inclui a componente vertical (segue a rampa); no ar, só a horizontal.
            Vector3 delta = targetVelocity - (IsGrounded ? velocity : horizontal);
            if (!IsGrounded) delta.y = 0f;
            float control = IsGrounded ? 1f : airControl;
            rb.AddForce(delta * acceleration * control, ForceMode.Acceleration);

            // Em rampas, cancela a componente da gravidade que faria o personagem escorregar.
            if (IsGrounded && groundNormal != Vector3.up)
            {
                rb.AddForce(-Vector3.ProjectOnPlane(Physics.gravity, groundNormal), ForceMode.Acceleration);
            }
        }

        /// <summary>
        /// Executa o salto quando houve pedido recente (buffer) e o personagem esteve no chão recentemente (coyote time).
        /// </summary>
        private void HandleJump()
        {
            bool buffered = Time.time - lastJumpPressedTime <= jumpBufferTime;
            bool coyote = Time.time - lastGroundedTime <= coyoteTime;
            if (!buffered || !coyote) return;

            // Zera a velocidade vertical e aplica um impulso para cima com AddForce.
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(v.x, 0f, v.z);
            rb.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);

            lastJumpPressedTime = -10f;
            lastGroundedTime = -10f;
            lastJumpTime = Time.time;
            IsGrounded = false;
            if (animator != null) animator.SetTrigger(JumpHash);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.jump, transform.position, 0.6f);
        }

        /// <summary>
        /// Deixa a queda mais rápida e encurta o salto quando Espaço é solto (salto de altura variável).
        /// </summary>
        private void ApplyExtraGravity()
        {
            if (IsGrounded) return;
            float vy = rb.linearVelocity.y;
            if (vy < 0f)
                rb.AddForce(Physics.gravity * (fallGravityMultiplier - 1f), ForceMode.Acceleration);
            else if (vy > 0f && !jumpHeld)
                rb.AddForce(Physics.gravity * (lowJumpGravityMultiplier - 1f), ForceMode.Acceleration);
        }

        /// <summary>
        /// Gira suavemente o modelo para a direção do movimento.
        /// </summary>
        private void RotateModel()
        {
            if (model == null || desiredMove.sqrMagnitude < 0.01f) return;
            Quaternion target = Quaternion.LookRotation(desiredMove, Vector3.up);
            model.rotation = Quaternion.Slerp(model.rotation, target, turnSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Envia velocidade, estado de chão e velocidade vertical ao Animator.
        /// </summary>
        private void UpdateAnimator()
        {
            if (animator == null) return;
            Vector3 v = rb.linearVelocity;
            float horizontalSpeed = new Vector3(v.x, 0f, v.z).magnitude;
            animator.SetFloat(SpeedHash, horizontalSpeed, 0.1f, Time.deltaTime);
            animator.SetBool(GroundedHash, IsGrounded);
            animator.SetFloat(VerticalHash, v.y);
            animator.SetBool(PushHash, pushing);
        }

        /// <summary>
        /// Toca sons de passos em intervalos que dependem de andar ou correr.
        /// </summary>
        private void UpdateFootsteps()
        {
            Vector3 v = rb.linearVelocity;
            float speed = new Vector3(v.x, 0f, v.z).magnitude;
            if (!IsGrounded || speed < 0.6f) { stepTimer = 0f; return; }

            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                stepTimer = speed > walkSpeed + 0.5f ? stepIntervalRun : stepIntervalWalk;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayFootstep(transform.position, 0.45f);
            }
        }

        /// <summary>
        /// Enquanto encosta num objeto empurrável e anda em direção a ele, aplica força para empurrá-lo.
        /// </summary>
        /// <param name="collision">Informações do contato com o outro objeto.</param>
        private void OnCollisionStay(Collision collision)
        {
            if (!collision.collider.CompareTag(GameTags.Pushable)) return;
            if (desiredMove.sqrMagnitude < 0.1f || !IsGrounded) return;

            PushableCrate crate = collision.collider.GetComponentInParent<PushableCrate>();
            if (crate == null) return;

            // Só empurra se o jogador estiver indo na direção do caixote.
            Vector3 toCrate = Vector3.ProjectOnPlane(crate.transform.position - transform.position, Vector3.up).normalized;
            if (Vector3.Dot(toCrate, desiredMove.normalized) < 0.5f) return;

            crate.Push(desiredMove.normalized, pushForce);
            pushing = true;
            pushingTimer = 0.15f;
        }

        /// <summary>
        /// Move o personagem instantaneamente para uma posição (checkpoint/queda), zerando a velocidade.
        /// </summary>
        /// <param name="position">Nova posição.</param>
        /// <param name="rotation">Nova rotação do modelo.</param>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            transform.position = position;
            if (model != null) model.rotation = rotation;
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.SnapBehindTarget(rotation);
        }

        /// <summary>
        /// Aplica um empurrão instantâneo (por exemplo, ao levar dano).
        /// </summary>
        /// <param name="impulse">Vetor de impulso (direção × intensidade).</param>
        public void Knockback(Vector3 impulse)
        {
            rb.AddForce(impulse, ForceMode.VelocityChange);
        }
    }
}
