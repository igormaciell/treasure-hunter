using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Parkour de John (Nível 3 em diante). Trabalha junto com o PlayerController, que continua cuidando de
    /// andar, correr, pular (coyote time e buffer) e da movimentação aérea. Este script acrescenta:
    /// - agarrar e subir bordas (cristais, rochas, caixas, muros e torres) detectadas por Raycast;
    /// - detecção de pouso (altura da queda, pouso forte, poeira, som e animação "Aterrissando");
    /// - detecção de queda no abismo (nada abaixo de John) e retorno de segurança ao checkpoint;
    /// - transporte de John por plataformas móveis e giratórias;
    /// - aviso às superfícies que reagem ao pouso (plataformas frágeis, cristais que brilham).
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(PlayerController), typeof(Rigidbody), typeof(CapsuleCollider))]
    public class ParkourController : MonoBehaviour
    {
        /// <summary>Instância do jogador na cena.</summary>
        public static ParkourController Instance { get; private set; }

        /// <summary>Animator do modelo (parâmetros Land, HardLand, Mantle, Hurt).</summary>
        [Header("Referências")]
        public Animator animator;
        /// <summary>Modelo visual (gira para encarar a parede ao escalar).</summary>
        public Transform model;

        [Header("Agarrar bordas")]
        public bool ledgeGrab = true;
        /// <summary>Altura mínima do topo da borda acima dos pés (abaixo disso é só um degrau).</summary>
        public float minLedgeHeight = 0.6f;
        /// <summary>Altura máxima alcançada pelas mãos acima dos pés.</summary>
        public float maxLedgeHeight = 2.3f;
        /// <summary>Distância extra (além do raio da cápsula) para encontrar a parede à frente.</summary>
        public float wallCheckDistance = 0.5f;
        /// <summary>Só agarra perto do topo do pulo ou caindo (velocidade vertical máxima).</summary>
        public float maxUpSpeedToGrab = 3f;
        /// <summary>Inclinação máxima do topo da borda.</summary>
        public float maxLedgeSlope = 35f;
        public float hangTime = 0.14f;
        public float climbTime = 0.36f;
        public float grabCooldown = 0.4f;

        /// <summary>Tempo mínimo no ar para contar como pouso (e não um degrau).</summary>
        [Header("Pouso")]
        public float minAirTimeForLanding = 0.2f;
        /// <summary>Queda a partir da qual o pouso é forte (agacha, treme a câmera e reduz a velocidade).</summary>
        public float hardLandingDrop = 3.5f;
        public float hardLandingSlowTime = 0.35f;
        public GameObject[] landingEffects;
        public GameObject[] hardLandingEffects;

        /// <summary>Distância do Raycast para baixo que procura chão durante a queda.</summary>
        [Header("Queda no abismo")]
        public float voidCheckDistance = 30f;
        /// <summary>Queda mínima (abaixo do ponto mais alto do salto) para avisar que John está caindo no abismo.</summary>
        public float voidAlarmDrop = 3.5f;
        /// <summary>Altura de segurança: abaixo dela John volta ao checkpoint mesmo sem tocar o gatilho do abismo.</summary>
        public float killHeight = -40f;

        /// <summary>John está escalando uma borda.</summary>
        public bool IsMantling { get; private set; }
        /// <summary>John está apoiado no chão (e não escalando).</summary>
        public bool IsGrounded => controller != null && controller.IsGrounded && !IsMantling;
        /// <summary>Tempo no ar do salto atual.</summary>
        public float AirTime { get; private set; }
        /// <summary>Nada abaixo de John: ele está caindo no abismo.</summary>
        public bool FallingIntoVoid { get; private set; }
        /// <summary>Quantidade de bordas escaladas.</summary>
        public int LedgesClimbed { get; private set; }
        /// <summary>Quantidade de pousos depois de saltos.</summary>
        public int Landings { get; private set; }
        /// <summary>Colisor sob os pés (nulo no ar).</summary>
        public Collider GroundCollider { get; private set; }
        /// <summary>Velocidade horizontal atual.</summary>
        public float HorizontalSpeed { get; private set; }

        /// <summary>Pouso (altura da queda, forte?).</summary>
        public event Action<float, bool> Landed;
        /// <summary>Uma borda foi escalada.</summary>
        public event Action LedgeClimbed;
        /// <summary>Começou (true) ou parou (false) de cair no abismo.</summary>
        public event Action<bool> VoidFallChanged;

        private PlayerController controller;
        private PlayerHealth health;
        private Rigidbody rb;
        private CapsuleCollider capsule;
        private bool wasGrounded = true;
        private float peakY;
        private float nextGrab;
        private float baseWalk, baseRun;
        private float slowUntil;
        private int lastHealth;
        private PlatformMotion lastPlatform;
        private Vector3 lastPlatformVelocity;
        private readonly HashSet<int> animatorParams = new HashSet<int>();
        private static readonly ILandingReceiver[] NoReceivers = new ILandingReceiver[0];
        private Collider receiversOf;
        private ILandingReceiver[] cachedReceivers = NoReceivers;

        // Escalada.
        private float mantleTime;
        private Vector3 mantleStart, mantleHang, mantleTop, mantleStand;

        private static readonly int LandHash = Animator.StringToHash("Land");
        private static readonly int HardLandHash = Animator.StringToHash("HardLand");
        private static readonly int MantleHash = Animator.StringToHash("Mantle");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");

        /// <summary>
        /// Guarda referências e as velocidades originais do PlayerController.
        /// </summary>
        private void Awake()
        {
            Instance = this;
            controller = GetComponent<PlayerController>();
            health = GetComponent<PlayerHealth>();
            rb = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            if (animator == null) animator = controller.animator;
            if (model == null) model = controller.model;
            baseWalk = controller.walkSpeed;
            baseRun = controller.runSpeed;
        }

        /// <summary>
        /// Lê quais parâmetros o Animator tem (o script funciona também com controllers mais simples).
        /// </summary>
        private void Start()
        {
            if (animator != null)
                foreach (AnimatorControllerParameter p in animator.parameters) animatorParams.Add(p.nameHash);
            lastHealth = health != null ? health.Current : 0;
            peakY = transform.position.y;
        }

        /// <summary>Libera a instância.</summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Animação de dano e fim da lentidão do pouso forte.
        /// </summary>
        private void Update()
        {
            if (health != null)
            {
                if (health.Current < lastHealth) Trigger(HurtHash);
                lastHealth = health.Current;
            }
            if (slowUntil > 0f && Time.time >= slowUntil)
            {
                slowUntil = 0f;
                controller.walkSpeed = baseWalk;
                controller.runSpeed = baseRun;
            }
        }

        /// <summary>
        /// Passo de física (depois do PlayerController): escalada, chão, pouso, plataformas, bordas e abismo.
        /// </summary>
        private void FixedUpdate()
        {
            if (IsMantling)
            {
                StepMantle();
                return;
            }

            Vector3 v = rb.linearVelocity;
            HorizontalSpeed = new Vector3(v.x, 0f, v.z).magnitude;
            bool grounded = controller.IsGrounded;
            ProbeGround(grounded, out RaycastHit groundHit);

            if (grounded)
            {
                if (!wasGrounded) OnLanded();
                AirTime = 0f;
                peakY = rb.position.y;
                CarryWithPlatform();
                NotifyStanding();
                SetVoidFall(false);
            }
            else
            {
                if (wasGrounded) OnTakeOff();
                AirTime += Time.fixedDeltaTime;
                peakY = Mathf.Max(peakY, rb.position.y);
                TryLedgeGrab();
                CheckVoidFall(v);
            }

            if (rb.position.y < killHeight && VoidZone.Instance != null) VoidZone.Instance.Respawn();
            wasGrounded = grounded;
        }

        // ================================================================== Chão e pouso

        /// <summary>
        /// Descobre o colisor sob os pés (para plataformas móveis e superfícies que reagem ao pouso).
        /// </summary>
        /// <param name="grounded">Estado de chão do PlayerController.</param>
        /// <param name="hit">Resultado do SphereCast.</param>
        private void ProbeGround(bool grounded, out RaycastHit hit)
        {
            float r = capsule.radius * 0.9f;
            Vector3 origin = rb.position + Vector3.up * (r + 0.08f);
            bool found = Physics.SphereCast(origin, r, Vector3.down, out hit, 0.35f, ParkourLayers.SurfaceMask, QueryTriggerInteraction.Ignore);
            GroundCollider = grounded && found ? hit.collider : null;
        }

        /// <summary>
        /// Pouso: altura da queda, efeitos, som, animação (normal ou forte) e aviso à superfície.
        /// </summary>
        private void OnLanded()
        {
            float drop = Mathf.Max(0f, peakY - rb.position.y);
            if (AirTime < minAirTimeForLanding) return;
            bool hard = drop >= hardLandingDrop;
            Landings++;

            Trigger(hard ? HardLandHash : LandHash);
            ParkourUtil.SpawnAll(hard ? hardLandingEffects : landingEffects, rb.position + Vector3.up * 0.05f);
            ParkourAudio a = ParkourAudio.Instance;
            if (a != null) ParkourAudio.Play(hard ? a.landHard : a.landSoft, rb.position, hard ? 1f : Mathf.Clamp(0.35f + drop * 0.15f, 0.35f, 0.8f));
            if (hard)
            {
                if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.3f, 0.12f);
                controller.walkSpeed = baseWalk * 0.4f;
                controller.runSpeed = baseWalk * 0.4f;
                slowUntil = Time.time + hardLandingSlowTime;
            }

            foreach (ILandingReceiver receiver in Receivers()) receiver.OnPlayerLanded(this, drop);
            Landed?.Invoke(drop, hard);
        }

        /// <summary>
        /// Saída do chão: herda a velocidade da plataforma móvel (o pulo acompanha o movimento dela).
        /// </summary>
        private void OnTakeOff()
        {
            peakY = rb.position.y;
            if (lastPlatform != null)
            {
                Vector3 pv = lastPlatformVelocity;
                pv.y = Mathf.Max(0f, pv.y);
                rb.AddForce(pv, ForceMode.VelocityChange);
            }
            lastPlatform = null;
        }

        /// <summary>
        /// Em cima de uma plataforma móvel/giratória, move John junto com ela.
        /// </summary>
        private void CarryWithPlatform()
        {
            PlatformMotion pm = GroundCollider != null ? GroundCollider.GetComponentInParent<PlatformMotion>() : null;
            lastPlatform = pm;
            if (pm == null) return;
            Vector3 delta = pm.DeltaAt(rb.position);
            lastPlatformVelocity = delta / Time.fixedDeltaTime;
            rb.position += delta;
            if (model != null && pm.StepRotation != Quaternion.identity)
            {
                float yaw = pm.StepRotation.eulerAngles.y;
                model.rotation = Quaternion.Euler(0f, yaw, 0f) * model.rotation;
            }
        }

        /// <summary>
        /// Avisa a superfície que John continua em cima dela.
        /// </summary>
        private void NotifyStanding()
        {
            foreach (ILandingReceiver receiver in Receivers()) receiver.OnPlayerStanding(this);
        }

        /// <summary>
        /// Objetos que reagem ao pouso no colisor sob os pés (guardados enquanto o chão for o mesmo).
        /// </summary>
        /// <returns>Vetor (pode ser vazio).</returns>
        private ILandingReceiver[] Receivers()
        {
            if (GroundCollider == null) return NoReceivers;
            if (GroundCollider != receiversOf)
            {
                receiversOf = GroundCollider;
                cachedReceivers = GroundCollider.GetComponentsInParent<ILandingReceiver>();
            }
            return cachedReceivers;
        }

        // ================================================================== Abismo

        /// <summary>
        /// Caindo depressa e sem nada embaixo: John está caindo no abismo.
        /// </summary>
        /// <param name="v">Velocidade atual.</param>
        private void CheckVoidFall(Vector3 v)
        {
            // Só depois de cair bem mais do que qualquer salto do percurso (evita alarme falso nos saltos entre plataformas).
            if (FallingIntoVoid || AirTime < 0.35f || v.y > -4f || peakY - rb.position.y < voidAlarmDrop) return;
            bool ground = Physics.Raycast(rb.position + Vector3.up * 0.2f, Vector3.down, voidCheckDistance, ParkourLayers.SurfaceMask, QueryTriggerInteraction.Ignore);
            if (!ground) SetVoidFall(true);
        }

        /// <summary>
        /// Atualiza o estado de queda no abismo (som do vento e evento para o HUD).
        /// </summary>
        /// <param name="on">Caindo no abismo.</param>
        private void SetVoidFall(bool on)
        {
            if (FallingIntoVoid == on) return;
            FallingIntoVoid = on;
            if (on)
            {
                ParkourAudio a = ParkourAudio.Instance;
                if (a != null) ParkourAudio.Play2D(a.fallWind, 0.7f);
            }
            VoidFallChanged?.Invoke(on);
        }

        // ================================================================== Bordas

        /// <summary>
        /// Procura uma borda à frente: parede íngreme (Raycast em três alturas) com topo plano ao alcance das mãos
        /// (Raycast para baixo) e espaço livre para ficar em pé (CheckCapsule). Se achar, começa a escalada.
        /// </summary>
        private void TryLedgeGrab()
        {
            if (!ledgeGrab || Time.time < nextGrab || AirTime < 0.08f) return;
            if (GameManager.Instance != null && !GameManager.Instance.CanPlayerAct()) return;
            if (rb.linearVelocity.y > maxUpSpeedToGrab) return;

            Vector3 dir = DesiredDirection();
            if (dir.sqrMagnitude < 0.01f) return;

            float r = capsule.radius;
            Vector3 feet = rb.position;
            int mask = ParkourLayers.SurfaceMask;
            RaycastHit wall = default;
            bool found = false;
            foreach (float h in new[] { 1.5f, 0.95f, 0.4f })
            {
                if (Physics.Raycast(feet + Vector3.up * h, dir, out RaycastHit hit, r + wallCheckDistance, mask, QueryTriggerInteraction.Ignore)
                    && Vector3.Angle(hit.normal, Vector3.up) > 60f)
                {
                    wall = hit;
                    found = true;
                    break;
                }
            }
            if (!found) return;

            Vector3 wallNormal = new Vector3(wall.normal.x, 0f, wall.normal.z).normalized;
            if (wallNormal.sqrMagnitude < 0.5f || Vector3.Dot(wallNormal, dir) > -0.3f) return;

            // Topo da borda: um pouco para dentro da parede, de cima para baixo.
            Vector3 probe = wall.point - wallNormal * 0.25f;
            probe.y = feet.y + maxLedgeHeight + 0.3f;
            float probeLength = maxLedgeHeight + 0.3f - minLedgeHeight + 0.05f;
            if (!Physics.Raycast(probe, Vector3.down, out RaycastHit top, probeLength, mask, QueryTriggerInteraction.Ignore)) return;
            if (Vector3.Angle(top.normal, Vector3.up) > maxLedgeSlope) return;
            float ledgeHeight = top.point.y - feet.y;
            if (ledgeHeight < minLedgeHeight || ledgeHeight > maxLedgeHeight) return;

            // Lugar para ficar em pé em cima da borda.
            Vector3 edge = new Vector3(wall.point.x, top.point.y, wall.point.z);
            Vector3 stand = edge - wallNormal * (r + 0.15f) + Vector3.up * 0.03f;
            if (!Physics.Raycast(stand + Vector3.up * 0.4f, Vector3.down, 0.7f, mask, QueryTriggerInteraction.Ignore)) return;
            if (Physics.CheckCapsule(stand + Vector3.up * (r + 0.08f), stand + Vector3.up * (capsule.height - r), r * 0.95f, mask, QueryTriggerInteraction.Ignore)) return;

            Vector3 hang = edge + wallNormal * (r + 0.04f);
            hang.y = Mathf.Max(feet.y, top.point.y - 1.6f);
            StartMantle(hang, new Vector3(hang.x, top.point.y + 0.05f, hang.z), stand, wallNormal);
        }

        /// <summary>
        /// Direção que o jogador quer seguir (teclas em relação à câmera); sem teclas, a direção do movimento.
        /// </summary>
        /// <returns>Direção horizontal normalizada (ou zero).</returns>
        private Vector3 DesiredDirection()
        {
            Vector2 input = GameInput.Move();
            if (input.sqrMagnitude > 0.01f && controller.cameraTransform != null)
            {
                Vector3 f = Vector3.ProjectOnPlane(controller.cameraTransform.forward, Vector3.up).normalized;
                Vector3 rgt = Vector3.ProjectOnPlane(controller.cameraTransform.right, Vector3.up).normalized;
                return (f * input.y + rgt * input.x).normalized;
            }
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            return v.magnitude > 1.5f ? v.normalized : Vector3.zero;
        }

        /// <summary>
        /// Começa a escalada: o corpo vira cinemático e segue a trajetória pendurar → subir → avançar.
        /// </summary>
        /// <param name="hang">Posição pendurado (mãos na borda).</param>
        /// <param name="up">Posição no alto, ainda junto da parede.</param>
        /// <param name="stand">Posição final em pé sobre a borda.</param>
        /// <param name="wallNormal">Normal da parede (para onde John olha é o oposto).</param>
        private void StartMantle(Vector3 hang, Vector3 up, Vector3 stand, Vector3 wallNormal)
        {
            IsMantling = true;
            mantleTime = 0f;
            mantleStart = rb.position;
            mantleHang = hang;
            mantleTop = up;
            mantleStand = stand;
            rb.linearVelocity = Vector3.zero;
            controller.enabled = false;
            rb.isKinematic = true;
            if (model != null) model.rotation = Quaternion.LookRotation(-wallNormal, Vector3.up);
            Trigger(MantleHash);
            ParkourAudio a = ParkourAudio.Instance;
            if (a != null) ParkourAudio.Play(a.ledgeGrab, hang + Vector3.up * 1.5f, 0.9f);
            SetVoidFall(false);
        }

        /// <summary>
        /// Avança a escalada a cada passo de física.
        /// </summary>
        private void StepMantle()
        {
            mantleTime += Time.fixedDeltaTime;
            Vector3 target;
            if (mantleTime < hangTime)
            {
                target = Vector3.Lerp(mantleStart, mantleHang, Smooth(mantleTime / hangTime));
            }
            else
            {
                float u = Mathf.Clamp01((mantleTime - hangTime) / climbTime);
                target = u < 0.6f
                    ? Vector3.Lerp(mantleHang, mantleTop, Smooth(u / 0.6f))
                    : Vector3.Lerp(mantleTop, mantleStand, Smooth((u - 0.6f) / 0.4f));
                if (u >= 1f)
                {
                    rb.MovePosition(mantleStand);
                    FinishMantle(true);
                    return;
                }
            }
            rb.MovePosition(target);
        }

        /// <summary>
        /// Termina a escalada e devolve o controle ao PlayerController.
        /// </summary>
        /// <param name="completed">true se John chegou ao topo (conta a escalada).</param>
        private void FinishMantle(bool completed)
        {
            IsMantling = false;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            controller.enabled = true;
            nextGrab = Time.time + grabCooldown;
            wasGrounded = false;
            AirTime = 0f;
            peakY = mantleStand.y;
            if (!completed) return;
            LedgesClimbed++;
            ParkourAudio a = ParkourAudio.Instance;
            if (a != null) ParkourAudio.Play(a.ledgeClimb, mantleStand, 0.8f);
            ParkourUtil.SpawnAll(landingEffects, mantleStand);
            ParkourUtil.UnlockSkill("escalada");
            LedgeClimbed?.Invoke();
        }

        /// <summary>
        /// Interrompe qualquer movimento especial (usado ao voltar ao checkpoint).
        /// </summary>
        public void CancelMovement()
        {
            if (IsMantling) FinishMantle(false);
            SetVoidFall(false);
            AirTime = 0f;
            peakY = rb.position.y;
            lastPlatform = null;
            wasGrounded = true;
        }

        // ================================================================== Auxiliares

        /// <summary>
        /// Dispara um gatilho do Animator, se o controller tiver esse parâmetro.
        /// </summary>
        /// <param name="hash">Hash do parâmetro.</param>
        public void Trigger(int hash)
        {
            if (animator != null && animatorParams.Contains(hash)) animator.SetTrigger(hash);
        }

        /// <summary>
        /// Dispara um gatilho do Animator pelo nome.
        /// </summary>
        /// <param name="name">Nome do parâmetro.</param>
        public void Trigger(string name)
        {
            Trigger(Animator.StringToHash(name));
        }

        /// <summary>
        /// Suavização (smoothstep).
        /// </summary>
        /// <param name="t">0 a 1.</param>
        /// <returns>Valor suavizado.</returns>
        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
