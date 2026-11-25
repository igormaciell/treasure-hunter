using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>Armas que John pode empunhar.</summary>
    public enum WeaponSlot { Revolver = 0, Knife = 1 }

    /// <summary>
    /// Estado do arsenal de John guardado no início de cada sala de combate,
    /// para restaurar ao voltar ao checkpoint (nunca soma balas além das 25 do nível).
    /// </summary>
    [Serializable]
    public struct CombatSnapshot
    {
        public bool hasRevolver;
        public bool hasKnife;
        public int cylinder;
        public int reserve;
        public int stones;
        public int shotsFired;
        public WeaponSlot weapon;
    }

    /// <summary>
    /// Sistema de combate de John Craft (Nível 2).
    /// - Revólver com exatamente 25 disparos no nível: tambor de 6 balas + reserva; recarga manual (R) que só
    ///   move balas da reserva para o tambor (nunca cria balas). Disparo físico (Projectile) com mira pelo centro da tela.
    /// - Coronhada (V): golpe fraco sempre disponível, empurra e atordoa o inimigo.
    /// - Faca (achada com 35% de chance nos esqueletos da faca): golpe rápido e mais forte (clique ou V).
    /// - Pedras (G): arremesso com AddForce e AddTorque, atordoam e distraem.
    /// - Mira (botão direito): câmera sobre o ombro, menos dispersão, andar mais lento.
    /// - Alimenta a camada de animação dos braços (mirar, disparar, recarregar, golpear, arremessar) e o HUD.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        /// <summary>Instância única acessível por qualquer script.</summary>
        public static PlayerCombat Instance { get; private set; }

        [Header("Referências")]
        public PlayerController controller;
        public Animator animator;
        public Transform model;
        public Transform rightArm;
        public Transform muzzle;
        public Transform throwPoint;
        public GameObject revolverModel;
        public GameObject knifeModel;
        public Light muzzleLight;

        [Header("Revólver (25 disparos no nível)")]
        public int startingShots = 25;
        public int cylinderSize = 6;
        public float fireInterval = 0.3f;
        public float reloadTime = 1.5f;
        public float bulletSpeed = 85f;
        public float bulletDamage = 2f;
        public float hipSpread = 1.4f;
        public float aimSpread = 0.15f;
        public float moveSpread = 1.2f;
        public float maxRange = 80f;
        public float noiseRadius = 26f;

        /// <summary>Projéteis do jogador: 0 = bala do revólver, 1 = pedra arremessada.</summary>
        [Header("Vetores de prefabs")]
        public GameObject[] projectilePrefabs;
        /// <summary>Efeitos do disparo (clarão, fumaça).</summary>
        public GameObject[] muzzleEffects;
        /// <summary>Efeitos dos golpes corpo a corpo.</summary>
        public GameObject[] meleeHitEffects;

        [Header("Coronhada")]
        public float whipDamage = 1f;
        public float whipRange = 1.2f;
        public float whipRadius = 0.85f;
        public float whipCooldown = 0.75f;
        public float whipForce = 7f;
        public float whipDelay = 0.16f;

        [Header("Faca")]
        public float knifeDamage = 2f;
        public float knifeRange = 1.35f;
        public float knifeRadius = 0.75f;
        public float knifeCooldown = 0.42f;
        public float knifeForce = 3.5f;
        public float knifeDelay = 0.1f;

        [Header("Pedras")]
        public int maxStones = 3;
        public float throwSpeed = 15f;
        public float throwUp = 2.5f;
        public float throwCooldown = 0.6f;

        [Header("Mira e postura")]
        public float aimMoveMultiplier = 0.55f;
        public float stanceTime = 1.1f;
        public float armPitchFactor = 0.85f;
        public float meleeAssistRange = 3f;

        /// <summary>John já pegou o revólver.</summary>
        public bool HasRevolver { get; private set; }
        /// <summary>John já pegou uma faca.</summary>
        public bool HasKnife { get; private set; }
        /// <summary>Balas no tambor.</summary>
        public int Cylinder { get; private set; }
        /// <summary>Balas na reserva.</summary>
        public int Reserve { get; private set; }
        /// <summary>Pedras carregadas.</summary>
        public int Stones { get; private set; }
        /// <summary>Disparos feitos no nível.</summary>
        public int ShotsFired { get; private set; }
        /// <summary>Disparos que acertaram inimigos.</summary>
        public int BulletHits { get; private set; }
        /// <summary>Arma empunhada.</summary>
        public WeaponSlot Weapon { get; private set; } = WeaponSlot.Revolver;
        /// <summary>Recarga em andamento.</summary>
        public bool IsReloading { get; private set; }
        /// <summary>Mirando (botão direito).</summary>
        public bool IsAiming { get; private set; }
        /// <summary>Disparos ainda disponíveis (tambor + reserva).</summary>
        public int ShotsLeft => Cylinder + Reserve;
        /// <summary>John tem alguma arma.</summary>
        public bool IsArmed => HasRevolver || HasKnife;
        /// <summary>Progresso da recarga (0 a 1).</summary>
        public float ReloadProgress => IsReloading ? Mathf.Clamp01(1f - (reloadEnd - Time.time) / reloadTime) : 0f;
        /// <summary>Velocidade horizontal atual (usada pela dispersão e pela mira do HUD).</summary>
        public float MoveSpeed { get; private set; }

        /// <summary>Disparado quando munição, arma ou pedras mudam (o HUD se atualiza).</summary>
        public event Action Changed;
        /// <summary>Disparado quando um golpe do jogador acerta um inimigo (true = o inimigo morreu).</summary>
        public event Action<bool, bool> HitConfirmed;
        /// <summary>Mensagem curta de ajuda para o HUD (ex.: "Tambor vazio! [R] recarregar").</summary>
        public event Action<string> Hint;
        /// <summary>Disparado a cada tiro (o HUD abre a mira).</summary>
        public event Action Fired;

        private PlayerHealth health;
        private Rigidbody body;
        private Camera cam;
        private float baseWalk, baseRun;
        private float nextFire, nextMelee, nextThrow, reloadEnd, stanceUntil, facingOverrideUntil, muzzleLightUntil;
        private Vector3 facingOverride;
        private int lastHealth;
        private readonly HashSet<IDamageable> meleeHits = new HashSet<IDamageable>();
        private readonly Collider[] overlap = new Collider[24];

        private static readonly int AimingHash = Animator.StringToHash("Aiming");
        private static readonly int WeaponHash = Animator.StringToHash("Weapon");
        private static readonly int ShootHash = Animator.StringToHash("Shoot");
        private static readonly int ReloadHash = Animator.StringToHash("Reload");
        private static readonly int WhipHash = Animator.StringToHash("Whip");
        private static readonly int StabHash = Animator.StringToHash("Stab");
        private static readonly int ThrowHash = Animator.StringToHash("Throw");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");

        /// <summary>
        /// Registra o singleton, guarda as velocidades originais e esconde as armas até serem coletadas.
        /// </summary>
        private void Awake()
        {
            Instance = this;
            if (controller == null) controller = GetComponent<PlayerController>();
            health = GetComponent<PlayerHealth>();
            body = GetComponent<Rigidbody>();
            baseWalk = controller.walkSpeed;
            baseRun = controller.runSpeed;
            if (revolverModel != null) revolverModel.SetActive(false);
            if (knifeModel != null) knifeModel.SetActive(false);
            if (muzzleLight != null) muzzleLight.enabled = false;
        }

        /// <summary>
        /// Encontra a câmera e começa a ouvir os golpes nos inimigos.
        /// </summary>
        private void Start()
        {
            cam = Camera.main;
            lastHealth = health != null ? health.Current : 0;
            Changed?.Invoke();
        }

        /// <summary>Inscreve-se no evento de dano dos inimigos.</summary>
        private void OnEnable()
        {
            EnemyBase.Damaged += HandleEnemyDamaged;
        }

        /// <summary>Cancela a inscrição no evento de dano dos inimigos.</summary>
        private void OnDisable()
        {
            EnemyBase.Damaged -= HandleEnemyDamaged;
            if (Instance == this) Instance = null;
        }

        // ================================================================== Ciclo do quadro

        /// <summary>
        /// Lê as entradas de combate e atualiza recarga, mira, animações e luz do disparo.
        /// </summary>
        private void Update()
        {
            Vector3 v = body != null ? body.linearVelocity : Vector3.zero;
            MoveSpeed = new Vector3(v.x, 0f, v.z).magnitude;
            TrackHealth();
            if (IsReloading && Time.time >= reloadEnd) FinishReload();
            if (muzzleLight != null && muzzleLight.enabled && Time.time >= muzzleLightUntil) muzzleLight.enabled = false;

            bool canAct = GameManager.Instance == null || GameManager.Instance.CanPlayerAct();
            if (!canAct)
            {
                SetAiming(false);
                UpdateAnimator();
                return;
            }

            HandleWeaponSwitch();
            SetAiming(HasRevolver && Weapon == WeaponSlot.Revolver && !IsReloading && CombatInput.AimHeld());

            if (CombatInput.FirePressed())
            {
                if (Weapon == WeaponSlot.Revolver && HasRevolver) TryShoot();
                else if (Weapon == WeaponSlot.Knife && HasKnife) TryMelee(true);
            }
            if (CombatInput.MeleePressed() && IsArmed) TryMelee(Weapon == WeaponSlot.Knife && HasKnife);
            if (CombatInput.ReloadPressed() && HasRevolver) TryReload();
            if (CombatInput.ThrowPressed()) TryThrow();

            UpdateAnimator();
        }

        /// <summary>
        /// Depois do Animator: vira John para onde a câmera aponta (postura de combate) e inclina
        /// o braço da arma conforme a mira vertical.
        /// </summary>
        private void LateUpdate()
        {
            if (model == null) return;
            bool stance = IsAiming || Time.time < stanceUntil;
            if (stance)
            {
                Vector3 dir = Time.time < facingOverrideUntil ? facingOverride : CameraFlatForward();
                if (dir.sqrMagnitude > 0.001f)
                {
                    float k = 1f - Mathf.Exp(-(IsAiming ? 25f : 16f) * Time.deltaTime);
                    model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(dir, Vector3.up), k);
                }
            }

            // Inclina o braço da arma conforme a câmera olha para cima ou para baixo (só com o braço animado).
            if (rightArm != null && IsAiming && ThirdPersonCamera.Instance != null)
            {
                float pitch = ThirdPersonCamera.Instance.Pitch;
                rightArm.localRotation = Quaternion.Euler(pitch * armPitchFactor, 0f, 0f) * rightArm.localRotation;
            }
        }

        // ================================================================== Armas

        /// <summary>
        /// Entrega o revólver com o total de disparos do nível (tambor cheio + reserva).
        /// </summary>
        /// <param name="shots">Total de disparos (25 no Nível 2).</param>
        public void GiveRevolver(int shots)
        {
            if (HasRevolver) return;
            HasRevolver = true;
            int total = Mathf.Clamp(shots, 0, startingShots);
            Cylinder = Mathf.Min(cylinderSize, total);
            Reserve = total - Cylinder;
            Equip(WeaponSlot.Revolver);
            Changed?.Invoke();
        }

        /// <summary>
        /// Entrega a faca (só existe uma; coletar outra não muda nada).
        /// </summary>
        /// <returns>true se John ainda não tinha faca.</returns>
        public bool GiveKnife()
        {
            if (HasKnife) return false;
            HasKnife = true;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Adiciona uma pedra ao bolso (máximo de 3).
        /// </summary>
        /// <returns>true se havia espaço.</returns>
        public bool AddStone()
        {
            if (Stones >= maxStones) return false;
            Stones++;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Empunha uma arma (cancela a recarga em andamento sem gastar balas).
        /// </summary>
        /// <param name="slot">Arma desejada.</param>
        public void Equip(WeaponSlot slot)
        {
            if (slot == WeaponSlot.Knife && !HasKnife) return;
            if (slot == WeaponSlot.Revolver && !HasRevolver) return;
            if (IsReloading) CancelReload();
            Weapon = slot;
            if (revolverModel != null) revolverModel.SetActive(HasRevolver && slot == WeaponSlot.Revolver);
            if (knifeModel != null) knifeModel.SetActive(HasKnife && slot == WeaponSlot.Knife);
            Changed?.Invoke();
        }

        /// <summary>
        /// Trata as teclas 1, 2 e Q/roda do mouse.
        /// </summary>
        private void HandleWeaponSwitch()
        {
            if (CombatInput.Weapon1Pressed()) Equip(WeaponSlot.Revolver);
            if (CombatInput.Weapon2Pressed())
            {
                if (HasKnife) Equip(WeaponSlot.Knife);
                else Hint?.Invoke("Você ainda não tem uma faca. Os esqueletos às vezes deixam cair a deles.");
            }
            if (CombatInput.NextWeaponPressed() && HasKnife && HasRevolver)
                Equip(Weapon == WeaponSlot.Revolver ? WeaponSlot.Knife : WeaponSlot.Revolver);
        }

        // ================================================================== Revólver

        /// <summary>
        /// Dispara uma bala na direção do centro da tela, se houver bala no tambor.
        /// </summary>
        private void TryShoot()
        {
            if (IsReloading || Time.time < nextFire) return;
            Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.4f + Facing() * 0.5f;
            if (Cylinder <= 0)
            {
                nextFire = Time.time + 0.25f;
                CombatAudio a = CombatAudio.Instance;
                if (a != null) CombatAudio.Play(a.dryFire, origin, 0.8f);
                Hint?.Invoke(Reserve > 0 ? "Tambor vazio! Aperte [R] para recarregar."
                                         : "Sem munição! Use a coronhada [V], a faca ou pedras [G].");
                return;
            }

            Cylinder--;
            ShotsFired++;
            nextFire = Time.time + fireInterval;
            stanceUntil = Time.time + stanceTime;

            // Mira: do cano até o ponto que está no centro da tela, com uma pequena dispersão.
            Vector3 target = AimPoint();
            Vector3 dir = (target - origin).normalized;
            float spread = IsAiming ? aimSpread : hipSpread;
            if (MoveSpeed > 0.5f) spread += moveSpread * (IsAiming ? 0.3f : 1f);
            dir = ApplySpread(dir, spread);

            // Se o cano estiver dentro de uma parede (encostado na cobertura), o tiro sai do peito.
            Vector3 chest = transform.position + Vector3.up * 1.3f;
            if (Physics.Linecast(chest, origin, CombatLayers.AimMask, QueryTriggerInteraction.Ignore)) origin = chest;

            FaceInstantly(dir);
            SpawnProjectile(0, origin, dir * bulletSpeed, bulletDamage);

            // Efeitos: clarão, fumaça, luz, som, tremor da câmera e barulho que alerta os inimigos.
            if (muzzleEffects != null && muzzle != null)
            {
                foreach (GameObject fx in muzzleEffects)
                {
                    if (fx != null) Instantiate(fx, muzzle.position, Quaternion.LookRotation(dir));
                }
            }
            if (muzzleLight != null) { muzzleLight.enabled = true; muzzleLightUntil = Time.time + 0.06f; }
            CombatAudio au = CombatAudio.Instance;
            if (au != null) CombatAudio.Play(au.revolverShot, origin, 1f, 0.05f);
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.12f, 0.05f);
            if (animator != null) animator.SetTrigger(ShootHash);
            // O disparo é o ruído mais forte do jogo: esqueletos acordam e ogros descobrem na hora de onde veio.
            NoiseSystem.Emit(transform.position, noiseRadius, NoiseType.Gunshot, gameObject);
            Fired?.Invoke();

            if (Cylinder == 0)
                Hint?.Invoke(Reserve > 0 ? "Tambor vazio — aperte [R] para recarregar." : "Essa foi a última bala! Agora só coronhada [V], faca e pedras.");
            Changed?.Invoke();
        }

        /// <summary>
        /// Começa a recarga: só move balas da reserva para o tambor (o total de 25 nunca aumenta).
        /// </summary>
        private void TryReload()
        {
            if (IsReloading) return;
            if (Cylinder >= cylinderSize) { Hint?.Invoke("O tambor já está cheio."); return; }
            if (Reserve <= 0) { Hint?.Invoke("Não há mais balas na reserva."); return; }
            if (Weapon != WeaponSlot.Revolver) Equip(WeaponSlot.Revolver);
            IsReloading = true;
            reloadEnd = Time.time + reloadTime;
            SetAiming(false);
            if (animator != null) animator.SetTrigger(ReloadHash);
            CombatAudio a = CombatAudio.Instance;
            if (a != null) CombatAudio.Play(a.reload, transform.position + Vector3.up, 0.9f, 0.02f);
            Changed?.Invoke();
        }

        /// <summary>
        /// Termina a recarga, passando da reserva para o tambor o que couber.
        /// </summary>
        private void FinishReload()
        {
            IsReloading = false;
            int n = Mathf.Min(cylinderSize - Cylinder, Reserve);
            Cylinder += n;
            Reserve -= n;
            UnlockSkill("recarga");
            Changed?.Invoke();
        }

        /// <summary>
        /// Interrompe a recarga sem mover balas.
        /// </summary>
        private void CancelReload()
        {
            IsReloading = false;
            if (animator != null) animator.ResetTrigger(ReloadHash);
            Changed?.Invoke();
        }

        /// <summary>
        /// Calcula o ponto do mundo que está no centro da tela, ignorando o que fica entre a câmera e John.
        /// </summary>
        /// <returns>Ponto mirado.</returns>
        public Vector3 AimPoint()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return transform.position + Vector3.up * 1.4f + Facing() * maxRange;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float skip = Mathf.Max(0f, Vector3.Dot(transform.position + Vector3.up * 1.3f - ray.origin, ray.direction));
            Vector3 start = ray.origin + ray.direction * skip;
            if (Physics.Raycast(start, ray.direction, out RaycastHit hit, maxRange, CombatLayers.AimMask, QueryTriggerInteraction.Ignore))
                return hit.point;
            return start + ray.direction * maxRange;
        }

        /// <summary>
        /// Indica se o centro da tela está sobre um inimigo vivo (a mira do HUD fica vermelha).
        /// </summary>
        /// <returns>true se a mira está sobre um inimigo.</returns>
        public bool IsAimingAtEnemy()
        {
            if (cam == null) return false;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float skip = Mathf.Max(0f, Vector3.Dot(transform.position + Vector3.up * 1.3f - ray.origin, ray.direction));
            if (!Physics.Raycast(ray.origin + ray.direction * skip, ray.direction, out RaycastHit hit, maxRange, CombatLayers.AimMask, QueryTriggerInteraction.Ignore))
                return false;
            IDamageable e = hit.collider.GetComponentInParent<IDamageable>();
            return e != null && e.IsAlive && e.Team == Team.Enemy;
        }

        /// <summary>
        /// Desvia levemente uma direção dentro de um cone (dispersão do tiro).
        /// </summary>
        /// <param name="dir">Direção original.</param>
        /// <param name="degrees">Abertura máxima do cone.</param>
        /// <returns>Direção desviada.</returns>
        private static Vector3 ApplySpread(Vector3 dir, float degrees)
        {
            if (degrees <= 0f) return dir;
            Vector2 r = UnityEngine.Random.insideUnitCircle * degrees;
            return Quaternion.LookRotation(dir) * Quaternion.Euler(r.y, r.x, 0f) * Vector3.forward;
        }

        /// <summary>
        /// Cria um projétil do vetor de prefabs e o lança.
        /// </summary>
        /// <param name="index">Índice no vetor (0 = bala).</param>
        /// <param name="position">Posição inicial.</param>
        /// <param name="velocity">Velocidade inicial.</param>
        /// <param name="damage">Dano.</param>
        /// <returns>O objeto criado (ou nulo).</returns>
        private GameObject SpawnProjectile(int index, Vector3 position, Vector3 velocity, float damage)
        {
            if (projectilePrefabs == null || index >= projectilePrefabs.Length || projectilePrefabs[index] == null) return null;
            GameObject go = Instantiate(projectilePrefabs[index], position, Quaternion.LookRotation(velocity));
            Projectile p = go.GetComponent<Projectile>();
            if (p != null)
            {
                p.damage = damage;
                p.Launch(velocity, gameObject, Team.Player);
            }
            return go;
        }

        // ================================================================== Corpo a corpo

        /// <summary>
        /// Golpe corpo a corpo: coronhada (fraca, empurra) ou faca (rápida, mais forte).
        /// Vira John para o inimigo mais próximo à frente (ajuda de mira) e aplica o golpe após a preparação.
        /// </summary>
        /// <param name="knife">true para a faca; false para a coronhada.</param>
        private void TryMelee(bool knife)
        {
            if (Time.time < nextMelee || IsReloading) return;
            nextMelee = Time.time + (knife ? knifeCooldown : whipCooldown);
            stanceUntil = Time.time + 0.6f;

            EnemyBase target = FindMeleeTarget();
            if (target != null)
            {
                Vector3 to = target.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f) FaceInstantly(to.normalized);
            }

            if (animator != null) animator.SetTrigger(knife ? StabHash : WhipHash);
            CombatAudio a = CombatAudio.Instance;
            if (a != null) CombatAudio.Play(knife ? a.knifeSlash : a.swingWhoosh, transform.position + Vector3.up, 0.7f);
            StartCoroutine(MeleeHit(knife));
        }

        /// <summary>
        /// Aplica o golpe depois do tempo de preparação: esfera de alcance à frente de John.
        /// </summary>
        /// <param name="knife">true para a faca.</param>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator MeleeHit(bool knife)
        {
            yield return new WaitForSeconds(knife ? knifeDelay : whipDelay);
            if (GameManager.Instance != null && !GameManager.Instance.CanPlayerAct()) yield break;

            float range = knife ? knifeRange : whipRange;
            float radius = knife ? knifeRadius : whipRadius;
            Vector3 facing = Facing();
            Vector3 center = transform.position + Vector3.up * 1.0f + facing * (range * 0.65f);
            int n = Physics.OverlapSphereNonAlloc(center, radius, overlap, CombatLayers.AimMask, QueryTriggerInteraction.Ignore);
            meleeHits.Clear();
            bool any = false;
            for (int i = 0; i < n; i++)
            {
                IDamageable d = overlap[i].GetComponentInParent<IDamageable>();
                if (d == null || !d.IsAlive || d.Team == Team.Player || meleeHits.Contains(d)) continue;
                meleeHits.Add(d);
                Vector3 point = overlap[i].ClosestPoint(center);
                DamageInfo info = new DamageInfo(knife ? knifeDamage : whipDamage, point, facing, knife ? knifeForce : whipForce,
                                                 knife ? DamageSource.Knife : DamageSource.PistolWhip, Team.Player, gameObject);
                if (d.TakeHit(info))
                {
                    any = true;
                    Projectile.SpawnEffects(meleeHitEffects, point, -facing);
                }
            }

            if (any)
            {
                CombatAudio a = CombatAudio.Instance;
                if (a != null) CombatAudio.Play(knife ? a.knifeHit : a.pistolWhip, center, 0.9f);
                if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.1f, 0.04f);
                UnlockSkill("corpo_a_corpo");
            }
        }

        /// <summary>
        /// Procura o inimigo vivo mais próximo à frente da câmera, dentro do alcance da ajuda de mira.
        /// </summary>
        /// <returns>O inimigo ou nulo.</returns>
        private EnemyBase FindMeleeTarget()
        {
            Vector3 forward = CameraFlatForward();
            if (forward.sqrMagnitude < 0.001f) forward = Facing();
            EnemyBase best = null;
            float bestScore = float.MaxValue;
            foreach (EnemyBase e in EnemyBase.All)
            {
                if (e == null || !e.IsAlive) continue;
                Vector3 to = e.transform.position - transform.position;
                to.y = 0f;
                float d = to.magnitude;
                if (d > meleeAssistRange || d < 0.01f) continue;
                if (Vector3.Angle(forward, to) > 75f) continue;
                if (d < bestScore) { bestScore = d; best = e; }
            }
            return best;
        }

        // ================================================================== Pedras

        /// <summary>
        /// Arremessa uma pedra na direção da mira, em arco (AddForce) e girando (AddTorque).
        /// </summary>
        private void TryThrow()
        {
            if (Time.time < nextThrow) return;
            if (Stones <= 0)
            {
                Hint?.Invoke("Você não tem pedras. Pegue algumas nas pilhas de pedras [E].");
                return;
            }
            if (projectilePrefabs == null || projectilePrefabs.Length < 2 || projectilePrefabs[1] == null) return;

            Stones--;
            nextThrow = Time.time + throwCooldown;
            stanceUntil = Time.time + 0.6f;
            Vector3 origin = throwPoint != null ? throwPoint.position : transform.position + Vector3.up * 1.6f + Facing() * 0.4f;
            Vector3 dir = (AimPoint() - origin).normalized;
            FaceInstantly(dir);
            if (animator != null) animator.SetTrigger(ThrowHash);

            GameObject go = Instantiate(projectilePrefabs[1], origin, UnityEngine.Random.rotation);
            ThrownStone stone = go.GetComponent<ThrownStone>();
            if (stone != null) stone.Launch(dir * throwSpeed + Vector3.up * throwUp, gameObject);
            CombatAudio a = CombatAudio.Instance;
            if (a != null) CombatAudio.Play(a.stoneThrow, origin, 0.7f);
            Changed?.Invoke();
        }

        // ================================================================== Estado e eventos

        /// <summary>
        /// Guarda o estado do arsenal (início de uma sala de combate).
        /// </summary>
        /// <returns>Cópia do estado.</returns>
        public CombatSnapshot Capture()
        {
            return new CombatSnapshot
            {
                hasRevolver = HasRevolver, hasKnife = HasKnife, cylinder = Cylinder, reserve = Reserve,
                stones = Stones, shotsFired = ShotsFired, weapon = Weapon
            };
        }

        /// <summary>
        /// Restaura o arsenal guardado (ao voltar ao checkpoint da sala).
        /// </summary>
        /// <param name="s">Estado guardado.</param>
        public void Restore(CombatSnapshot s)
        {
            IsReloading = false;
            HasRevolver = s.hasRevolver;
            HasKnife = s.hasKnife;
            Cylinder = s.cylinder;
            Reserve = s.reserve;
            Stones = s.stones;
            ShotsFired = s.shotsFired;
            Weapon = s.weapon;
            Equip(s.weapon);
            Changed?.Invoke();
        }

        /// <summary>
        /// Reação a um golpe em qualquer inimigo: estatísticas, habilidades e marcador de acerto.
        /// </summary>
        /// <param name="enemy">Inimigo atingido.</param>
        /// <param name="info">Dados do golpe.</param>
        /// <param name="killed">true se o inimigo morreu.</param>
        private void HandleEnemyDamaged(EnemyBase enemy, DamageInfo info, bool killed)
        {
            if (info.IsEnvironmental) UnlockSkill("improviso");
            if (info.team != Team.Player && !info.IsEnvironmental) return;
            if (info.source == DamageSource.Bullet)
            {
                BulletHits++;
                UnlockSkill("pontaria");
            }
            HitConfirmed?.Invoke(killed, info.headshot);
        }

        /// <summary>
        /// Percebe que John levou dano (a vida diminuiu) e toca a animação de dor.
        /// </summary>
        private void TrackHealth()
        {
            if (health == null) return;
            int current = health.Current;
            if (current < lastHealth && animator != null) animator.SetTrigger(HurtHash);
            lastHealth = current;
        }

        /// <summary>
        /// Liga/desliga a mira: câmera sobre o ombro e andar mais lento (sem correr).
        /// </summary>
        /// <param name="aim">true para mirar.</param>
        private void SetAiming(bool aim)
        {
            if (aim == IsAiming) return;
            IsAiming = aim;
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Aiming = aim;
            if (controller != null)
            {
                controller.walkSpeed = aim ? baseWalk * aimMoveMultiplier : baseWalk;
                controller.runSpeed = aim ? baseWalk * aimMoveMultiplier : baseRun;
            }
        }

        /// <summary>
        /// Envia os parâmetros da camada dos braços ao Animator.
        /// </summary>
        private void UpdateAnimator()
        {
            if (animator == null) return;
            animator.SetBool(AimingHash, IsAiming);
            animator.SetInteger(WeaponHash, (int)Weapon);
            if (animator.layerCount > 1) animator.SetLayerWeight(1, IsArmed ? 1f : 0f);
        }

        /// <summary>
        /// Vira o modelo imediatamente para uma direção (no plano) e mantém a postura por um instante.
        /// </summary>
        /// <param name="dir">Direção.</param>
        private void FaceInstantly(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f || model == null) return;
            dir.Normalize();
            model.rotation = Quaternion.LookRotation(dir, Vector3.up);
            facingOverride = dir;
            facingOverrideUntil = Time.time + 0.35f;
        }

        /// <summary>
        /// Direção para onde o modelo está olhando.
        /// </summary>
        /// <returns>Vetor no plano.</returns>
        private Vector3 Facing()
        {
            return controller != null ? controller.Facing : transform.forward;
        }

        /// <summary>
        /// Frente da câmera projetada no chão.
        /// </summary>
        /// <returns>Direção no plano (ou zero sem câmera).</returns>
        private Vector3 CameraFlatForward()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return Vector3.zero;
            Vector3 f = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.zero;
        }

        /// <summary>
        /// Desbloqueia uma habilidade do Nível 2.
        /// </summary>
        /// <param name="id">Identificador da habilidade.</param>
        public static void UnlockSkill(string id)
        {
            if (SkillManager.Instance != null) SkillManager.Instance.Unlock(id);
        }

        /// <summary>
        /// Mostra uma dica curta no HUD de combate.
        /// </summary>
        /// <param name="text">Texto da dica.</param>
        public void ShowHint(string text)
        {
            Hint?.Invoke(text);
        }
    }
}
