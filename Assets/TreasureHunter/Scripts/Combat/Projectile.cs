using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Projétil físico (bala do revólver ou flecha dos esqueletos arqueiros).
    /// É lançado com Rigidbody.AddForce (VelocityChange); a flecha usa a gravidade e faz trajetória curva.
    /// A colisão é detectada de duas formas, para não "atravessar" paredes finas em alta velocidade:
    /// uma varredura (SphereCast) do trecho percorrido a cada passo de física e o gatilho do próprio colisor.
    /// No impacto: causa dano ao IDamageable acertado (com bônus na cabeça), cria as partículas do vetor de
    /// efeitos, toca o som e empurra objetos físicos. Flechas ficam cravadas nas paredes por alguns segundos.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        /// <summary>Projéteis em voo (usado para limpar a sala ao reiniciar).</summary>
        public static readonly List<Projectile> Active = new List<Projectile>();

        [Header("Voo")]
        public float lifetime = 4f;
        public float radius = 0.05f;
        public bool useGravity = false;
        public bool alignToVelocity = true;

        [Header("Dano")]
        public float damage = 2f;
        public DamageSource source = DamageSource.Bullet;
        public float force = 4f;

        [Header("Impacto")]
        public bool stickOnImpact = false;
        public float stickTime = 6f;
        public float stickDepth = 0.12f;

        /// <summary>Partículas criadas ao acertar o cenário (faíscas, poeira, pedrinhas...).</summary>
        [Header("Efeitos (vetores de prefabs)")]
        public GameObject[] surfaceEffects;
        /// <summary>Partículas criadas ao acertar um personagem (lascas de osso...).</summary>
        public GameObject[] hitEffects;

        [Header("Sons")]
        public AudioClip[] surfaceClips;
        public AudioClip[] hitClips;

        /// <summary>Rastro visual (opcional).</summary>
        public TrailRenderer trail;

        /// <summary>Lado de quem disparou.</summary>
        public Team Team { get; private set; } = Team.Neutral;
        /// <summary>Quem disparou.</summary>
        public GameObject Owner { get; private set; }

        private Rigidbody rb;
        private Vector3 lastPosition;
        private bool done;
        private bool launched;
        private float dieAt;
        private readonly RaycastHit[] hits = new RaycastHit[8];

        /// <summary>
        /// Configura o Rigidbody (o colisor é gatilho: o projétil nunca quica).
        /// </summary>
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = useGravity;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            lastPosition = transform.position;
        }

        /// <summary>Registra o projétil na lista dos ativos.</summary>
        private void OnEnable()
        {
            Active.Add(this);
        }

        /// <summary>Remove o projétil da lista dos ativos.</summary>
        private void OnDisable()
        {
            Active.Remove(this);
        }

        /// <summary>
        /// Dispara o projétil com uma velocidade inicial (AddForce em modo VelocityChange).
        /// </summary>
        /// <param name="velocity">Velocidade inicial (direção × rapidez).</param>
        /// <param name="owner">Quem disparou (nunca é atingido pelo próprio tiro).</param>
        /// <param name="team">Lado de quem disparou.</param>
        public void Launch(Vector3 velocity, GameObject owner, Team team)
        {
            Owner = owner;
            Team = team;
            lastPosition = transform.position;
            dieAt = Time.time + lifetime;
            launched = true;
            rb.useGravity = useGravity;
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(velocity, ForceMode.VelocityChange);
            if (alignToVelocity && velocity.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(velocity);
        }

        /// <summary>
        /// A cada passo de física varre o trecho percorrido desde o passo anterior procurando algo acertado
        /// e alinha o projétil à direção do voo (a flecha aponta para onde está indo).
        /// </summary>
        private void FixedUpdate()
        {
            if (done || !launched) return;
            if (Time.time >= dieAt) { Finish(); return; }

            Vector3 pos = rb.position;
            Vector3 delta = pos - lastPosition;
            float dist = delta.magnitude;
            if (dist > 0.0001f)
            {
                Vector3 dir = delta / dist;
                int n = Physics.SphereCastNonAlloc(lastPosition, radius, dir, hits, dist, CombatLayers.ProjectileMask, QueryTriggerInteraction.Ignore);
                int best = -1;
                float bestDistance = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    if (ShouldIgnore(hits[i].collider)) continue;
                    if (hits[i].distance < bestDistance) { bestDistance = hits[i].distance; best = i; }
                }
                if (best >= 0)
                {
                    RaycastHit h = hits[best];
                    // Sondagem que começou já encostada no alvo devolve ponto zero: usa a posição anterior.
                    Vector3 point = h.distance <= 0f ? lastPosition : h.point;
                    Vector3 normal = h.distance <= 0f ? -dir : h.normal;
                    Impact(h.collider, point, normal, dir);
                    return;
                }
            }
            lastPosition = pos;

            if (alignToVelocity)
            {
                Vector3 v = rb.linearVelocity;
                if (v.sqrMagnitude > 0.5f) rb.MoveRotation(Quaternion.LookRotation(v));
            }
        }

        /// <summary>
        /// Segunda forma de detecção: o colisor (gatilho) do projétil tocou algo sólido.
        /// </summary>
        /// <param name="other">Colisor tocado.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (done || !launched || ShouldIgnore(other)) return;
            Vector3 v = rb.linearVelocity;
            Vector3 dir = v.sqrMagnitude > 0.01f ? v.normalized : transform.forward;
            Vector3 point = other.ClosestPoint(transform.position);
            Impact(other, point, -dir, dir);
        }

        /// <summary>
        /// Diz se um colisor deve ser ignorado: gatilhos, o próprio atirador e aliados.
        /// </summary>
        /// <param name="c">Colisor.</param>
        /// <returns>true para atravessar sem efeito.</returns>
        private bool ShouldIgnore(Collider c)
        {
            if (c == null || c.isTrigger) return true;
            if (Owner != null && c.transform.IsChildOf(Owner.transform)) return true;
            if (Team == Team.Neutral) return false;
            IDamageable d = c.GetComponentInParent<IDamageable>();
            return d != null && d.Team == Team;
        }

        /// <summary>
        /// Trata o impacto: dano, efeitos, som e empurrão; depois crava (flecha) ou some (bala).
        /// </summary>
        /// <param name="col">Colisor acertado.</param>
        /// <param name="point">Ponto do impacto.</param>
        /// <param name="normal">Normal da superfície.</param>
        /// <param name="dir">Direção do voo.</param>
        private void Impact(Collider col, Vector3 point, Vector3 normal, Vector3 dir)
        {
            done = true;
            IDamageable target = col.GetComponentInParent<IDamageable>();
            bool applied = false;
            bool character = false;

            if (target != null && target.IsAlive)
            {
                Hitbox hb = col.GetComponent<Hitbox>();
                DamageInfo info = new DamageInfo(damage * (hb != null ? hb.multiplier : 1f), point, dir, force, source, Team, Owner);
                info.headshot = hb != null && hb.isHead;
                applied = target.TakeHit(info);
                character = target.Team != Team.Neutral;
            }

            if (applied && character)
            {
                SpawnEffects(hitEffects, point, normal);
                CombatAudio.PlayRandom(hitClips, point);
            }
            else
            {
                SpawnEffects(surfaceEffects, point, normal);
                CombatAudio.PlayRandom(surfaceClips, point, 0.8f);
                // Empurra objetos soltos (destroços, pedras) com a força do tiro.
                Rigidbody other = col.attachedRigidbody;
                if (other != null && !other.isKinematic) other.AddForceAtPosition(dir * force, point, ForceMode.Impulse);
            }

            if (stickOnImpact && !applied) Stick(col, point, dir);
            else Finish();
        }

        /// <summary>
        /// Crava a flecha na superfície: para a física, alinha à direção do voo e some depois de um tempo.
        /// </summary>
        /// <param name="col">Superfície atingida.</param>
        /// <param name="point">Ponto do impacto.</param>
        /// <param name="dir">Direção do voo.</param>
        private void Stick(Collider col, Vector3 point, Vector3 dir)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.detectCollisions = false;
            // A origem do modelo da flecha fica na ponta: basta afundar a ponta um pouco na superfície.
            transform.SetPositionAndRotation(point + dir * stickDepth, Quaternion.LookRotation(dir));
            // Acompanha objetos que se movem (portões), mas não objetos físicos soltos.
            if (col.attachedRigidbody == null) transform.SetParent(col.transform, true);
            if (trail != null) trail.emitting = false;
            Destroy(gameObject, stickTime);
        }

        /// <summary>
        /// Remove o projétil, deixando o rastro terminar de sumir sozinho.
        /// </summary>
        private void Finish()
        {
            done = true;
            if (trail != null)
            {
                trail.transform.SetParent(null, true);
                trail.emitting = false;
                Destroy(trail.gameObject, trail.time);
            }
            Destroy(gameObject);
        }

        /// <summary>
        /// Instancia todos os efeitos de um vetor de prefabs no ponto do impacto, virados para fora da superfície.
        /// </summary>
        /// <param name="effects">Vetor de prefabs de partículas.</param>
        /// <param name="point">Ponto.</param>
        /// <param name="normal">Normal da superfície.</param>
        public static void SpawnEffects(GameObject[] effects, Vector3 point, Vector3 normal)
        {
            if (effects == null) return;
            Quaternion rot = normal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(normal) : Quaternion.identity;
            foreach (GameObject fx in effects)
            {
                if (fx != null) Instantiate(fx, point + normal * 0.03f, rot);
            }
        }

        /// <summary>
        /// Remove todos os projéteis em voo (usado ao reiniciar uma sala).
        /// </summary>
        public static void ClearAll()
        {
            Projectile[] copy = Active.ToArray();
            foreach (Projectile p in copy)
            {
                if (p != null) Destroy(p.gameObject);
            }
            Active.Clear();
        }
    }
}
