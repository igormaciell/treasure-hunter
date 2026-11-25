using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Percepção visual reutilizável: campo de visão por ângulo e distância, Raycast (Linecast) até três pontos
    /// do corpo de John (cabeça, peito e quadril — coberturas baixas escondem quem está agachado), visão
    /// periférica mais fraca, e o fator de visibilidade do jogador (luz, tocha, postura, movimento,
    /// esconderijo). O resultado alimenta um medidor de atenção (0 a 1): acima do limiar de suspeita o dono
    /// desconfia; em 1 ele detecta. Usado pelos ogros, mas serve para qualquer vigia.
    /// </summary>
    public class VisionSensor : MonoBehaviour
    {
        /// <summary>Ponto de onde parte a visão (cabeça).</summary>
        [Header("Olhos")]
        public Transform eye;
        /// <summary>Alcance normal (m).</summary>
        public float range = 13f;
        /// <summary>Ângulo total do campo de visão (graus).</summary>
        public float fieldOfView = 100f;
        /// <summary>Metade do ângulo da visão "focada" (fora dela a percepção cai).</summary>
        public float focusAngle = 30f;
        /// <summary>Multiplicador fora do foco (visão periférica).</summary>
        public float peripheralFactor = 0.55f;
        /// <summary>Muito perto e à vista: detecção quase imediata.</summary>
        public float closeRange = 2.2f;
        /// <summary>Raio em que o dono "sente" John mesmo fora do campo de visão (se ele estiver de pé ou se mexendo).</summary>
        public float presenceRadius = 1.6f;
        /// <summary>Com a tocha acesa, John é visto de mais longe.</summary>
        public float torchRangeMultiplier = 1.7f;

        /// <summary>Velocidade de enchimento (por segundo, com visibilidade 1).</summary>
        [Header("Medidor de atenção")]
        public float gain = 1.5f;
        /// <summary>Velocidade de esvaziamento sem ver nada.</summary>
        public float decay = 0.22f;
        /// <summary>Multiplicador de sensibilidade (sobe quando o dono está em alerta).</summary>
        public float sensitivity = 1f;

        /// <summary>Medidor de atenção (0 a 1).</summary>
        public float Awareness { get; private set; }
        /// <summary>Visibilidade instantânea de John neste momento (0 = invisível).</summary>
        public float Visibility { get; private set; }
        /// <summary>John está à vista (linha de visada livre e dentro do campo) neste momento.</summary>
        public bool CanSeeTarget { get; private set; }
        /// <summary>Última posição em que John foi visto.</summary>
        public Vector3 LastSeenPosition { get; private set; }
        /// <summary>Instante em que John foi visto pela última vez.</summary>
        public float LastSeenTime { get; private set; } = -100f;
        /// <summary>Fração do corpo visível (0, 1/3, 2/3 ou 1).</summary>
        public float BodyFraction { get; private set; }
        /// <summary>Distância até John.</summary>
        public float Distance { get; private set; }
        /// <summary>true quando ignorar esconderijos (o dono viu John entrar neles).</summary>
        public bool IgnoreHiding { get; set; }

        private float nextSample;
        private const float SampleInterval = 0.1f;

        /// <summary>
        /// Atualiza a percepção (chamado pelo dono a cada quadro).
        /// </summary>
        /// <param name="dt">Tempo do quadro.</param>
        public void Tick(float dt)
        {
            if (Time.time >= nextSample)
            {
                nextSample = Time.time + SampleInterval;
                Visibility = Sample();
            }
            if (Visibility > 0.001f)
            {
                Awareness = Mathf.Clamp01(Awareness + Visibility * gain * sensitivity * dt);
            }
            else
            {
                Awareness = Mathf.Max(0f, Awareness - decay * dt);
            }
        }

        /// <summary>
        /// Calcula a visibilidade de John agora.
        /// </summary>
        /// <returns>0 se não pode ser visto; maior quanto mais fácil de ver.</returns>
        private float Sample()
        {
            CanSeeTarget = false;
            BodyFraction = 0f;
            PlayerStealth p = PlayerStealth.Instance;
            if (p == null || p.IsDead) return 0f;
            Transform e = eye != null ? eye : transform;
            Vector3 eyePos = e.position;
            Vector3 feet = p.transform.position;
            Vector3 to = feet - transform.position;
            to.y = 0f;
            float d = to.magnitude;
            Distance = d;

            float effRange = range * (p.TorchLit ? torchRangeMultiplier : 1f);
            if (d > effRange) return 0f;

            // Área segura: só de muito perto.
            if (p.InSafeZone && d > 3f) return 0f;

            // Campo de visão.
            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            float angle = d > 0.01f ? Vector3.Angle(fwd, to) : 0f;
            bool inCone = angle <= fieldOfView * 0.5f;
            if (!inCone)
            {
                // Fora do campo: só "sente" John bem perto, se ele estiver de pé ou se mexendo.
                bool noticeable = !p.IsCrouched || p.IsMoving;
                if (d <= presenceRadius && noticeable && Unblocked(eyePos, p.Center))
                {
                    LastSeenPosition = feet;
                    LastSeenTime = Time.time;
                    return 0.5f;
                }
                return 0f;
            }

            // Linha de visada até cabeça, peito e quadril.
            int visible = 0;
            if (Unblocked(eyePos, feet + Vector3.up * p.HeadHeight)) visible++;
            if (Unblocked(eyePos, p.Center)) visible++;
            if (Unblocked(eyePos, feet + Vector3.up * 0.35f)) visible++;
            BodyFraction = visible / 3f;
            if (visible == 0) return 0f;
            CanSeeTarget = true;

            float playerFactor = p.VisibilityFactor;
            if (IgnoreHiding && p.InHidingSpot) playerFactor = Mathf.Max(playerFactor, p.Exposure * (p.IsCrouched ? 0.5f : 1f));
            float distFactor = d <= closeRange ? 1.4f : Mathf.Lerp(1f, 0.25f, (d - closeRange) / Mathf.Max(0.1f, effRange - closeRange));
            float focus = angle <= focusAngle ? 1f : peripheralFactor;
            float v = playerFactor * distFactor * focus * BodyFraction;

            // Bem na frente e perto: não dá para passar despercebido.
            if (d <= closeRange && !p.InSafeZone) v = Mathf.Max(v, 1.2f);
            if (v > 0.02f)
            {
                LastSeenPosition = feet;
                LastSeenTime = Time.time;
            }
            return v;
        }

        /// <summary>
        /// Nada do cenário entre dois pontos?
        /// </summary>
        /// <param name="from">Origem.</param>
        /// <param name="to">Destino.</param>
        /// <returns>true se a linha está livre.</returns>
        private static bool Unblocked(Vector3 from, Vector3 to)
        {
            return !Physics.Linecast(from, to, StealthUtil.SightMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Soma atenção (ruídos, golpes).
        /// </summary>
        /// <param name="amount">Quanto somar.</param>
        public void AddAwareness(float amount)
        {
            Awareness = Mathf.Clamp01(Awareness + amount);
        }

        /// <summary>
        /// Detecção imediata (disparo, golpe).
        /// </summary>
        /// <param name="position">Onde John está.</param>
        public void ForceDetect(Vector3 position)
        {
            Awareness = 1f;
            LastSeenPosition = position;
            LastSeenTime = Time.time;
        }

        /// <summary>
        /// Zera a percepção (reinício).
        /// </summary>
        public void ResetAwareness()
        {
            Awareness = 0f;
            Visibility = 0f;
            CanSeeTarget = false;
            LastSeenTime = -100f;
            IgnoreHiding = false;
        }

        /// <summary>
        /// Desenha o cone de visão no editor.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Transform e = eye != null ? eye : transform;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
            Vector3 o = e.position;
            Vector3 f = transform.forward;
            Vector3 l = Quaternion.Euler(0f, -fieldOfView * 0.5f, 0f) * f;
            Vector3 r = Quaternion.Euler(0f, fieldOfView * 0.5f, 0f) * f;
            Gizmos.DrawLine(o, o + l * range);
            Gizmos.DrawLine(o, o + r * range);
            Gizmos.DrawLine(o + l * range, o + f * range);
            Gizmos.DrawLine(o + r * range, o + f * range);
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, closeRange);
        }
    }
}
