using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Armadilha de espinhos no piso. Quando alguém pisa na placa (jogador ou esqueleto),
    /// ela estala e, depois de um instante, os espinhos sobem (Translate) ferindo todos sobre ela.
    /// Os espinhos descem e a armadilha rearma após um intervalo. Atrair esqueletos para cá é uma estratégia.
    /// </summary>
    public class SpikeTrap : MonoBehaviour, IResettable
    {
        private enum State { Idle, Armed, Active, Cooldown }

        /// <summary>Sala da armadilha.</summary>
        public int roomIndex;
        /// <summary>Grupo dos espinhos (move no eixo Y local).</summary>
        public Transform spikes;
        /// <summary>Altura local dos espinhos escondidos.</summary>
        public float hiddenY = -0.55f;
        /// <summary>Altura local dos espinhos levantados.</summary>
        public float raisedY = 0f;
        /// <summary>Tempo entre pisar e os espinhos subirem.</summary>
        public float armDelay = 0.45f;
        /// <summary>Tempo com os espinhos levantados.</summary>
        public float activeTime = 1.1f;
        /// <summary>Tempo até rearmar.</summary>
        public float cooldown = 1.2f;
        /// <summary>Velocidade dos espinhos (m/s).</summary>
        public float moveSpeed = 9f;
        /// <summary>Corações retirados do jogador.</summary>
        public int playerDamage = 1;
        /// <summary>Dano aos inimigos.</summary>
        public float enemyDamage = 4f;
        /// <summary>Tamanho da área ferida (centro um pouco acima do piso).</summary>
        public Vector3 damageBox = new Vector3(1.9f, 1.4f, 1.9f);
        /// <summary>Efeitos ao subir (poeira).</summary>
        public GameObject[] effects;

        private State state = State.Idle;
        private float timer;
        private readonly HashSet<IDamageable> damaged = new HashSet<IDamageable>();
        private readonly Collider[] buffer = new Collider[16];

        /// <summary>Sala da armadilha (IResettable).</summary>
        public int RoomIndex => roomIndex;

        /// <summary>
        /// Começa com os espinhos escondidos.
        /// </summary>
        private void Start()
        {
            SetSpikeY(hiddenY);
        }

        /// <summary>Alguém pisou na placa.</summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            TryArm(other);
        }

        /// <summary>Alguém continua sobre a placa.</summary>
        /// <param name="other">Colisor que permanece.</param>
        private void OnTriggerStay(Collider other)
        {
            TryArm(other);
        }

        /// <summary>
        /// Arma a armadilha se quem pisou for um personagem vivo.
        /// </summary>
        /// <param name="other">Colisor.</param>
        private void TryArm(Collider other)
        {
            if (state != State.Idle) return;
            IDamageable d = other.GetComponentInParent<IDamageable>();
            if (d == null || !d.IsAlive || d.Team == Team.Neutral) return;
            state = State.Armed;
            timer = Time.time + armDelay;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.plateClick, transform.position, 0.8f);
        }

        /// <summary>
        /// Máquina de estados: armada → espinhos sobem → ferem → descem → rearma.
        /// </summary>
        private void Update()
        {
            switch (state)
            {
                case State.Armed:
                    if (Time.time >= timer)
                    {
                        state = State.Active;
                        timer = Time.time + activeTime;
                        damaged.Clear();
                        CombatAudio a = CombatAudio.Instance;
                        if (a != null) CombatAudio.Play(a.spikeTrap, transform.position);
                        Projectile.SpawnEffects(effects, transform.position + Vector3.up * 0.1f, Vector3.up);
                    }
                    break;
                case State.Active:
                    DamageArea();
                    if (Time.time >= timer) { state = State.Cooldown; timer = Time.time + cooldown; }
                    break;
                case State.Cooldown:
                    if (Time.time >= timer) state = State.Idle;
                    break;
            }
            MoveSpikes(state == State.Active ? raisedY : hiddenY);
        }

        /// <summary>
        /// Fere uma vez cada personagem que estiver sobre a armadilha enquanto os espinhos estão levantados.
        /// </summary>
        private void DamageArea()
        {
            Vector3 center = transform.position + Vector3.up * (damageBox.y * 0.5f);
            int n = Physics.OverlapBoxNonAlloc(center, damageBox * 0.5f, buffer, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                IDamageable d = buffer[i].GetComponentInParent<IDamageable>();
                if (d == null || !d.IsAlive || d.Team == Team.Neutral || damaged.Contains(d)) continue;
                damaged.Add(d);
                float amount = d.Team == Team.Player ? playerDamage : enemyDamage;
                d.TakeHit(new DamageInfo(amount, buffer[i].bounds.center, Vector3.up, 5f, DamageSource.Trap, Team.Neutral, gameObject));
            }
        }

        /// <summary>
        /// Move os espinhos com Translate até a altura desejada.
        /// </summary>
        /// <param name="targetY">Altura local desejada.</param>
        private void MoveSpikes(float targetY)
        {
            if (spikes == null) return;
            float y = spikes.localPosition.y;
            if (Mathf.Approximately(y, targetY)) return;
            float step = Mathf.MoveTowards(y, targetY, moveSpeed * Time.deltaTime) - y;
            spikes.Translate(Vector3.up * step, Space.Self);
        }

        /// <summary>
        /// Posiciona os espinhos imediatamente numa altura.
        /// </summary>
        /// <param name="y">Altura local.</param>
        private void SetSpikeY(float y)
        {
            if (spikes == null) return;
            Vector3 p = spikes.localPosition;
            spikes.localPosition = new Vector3(p.x, y, p.z);
        }

        /// <summary>
        /// Volta ao estado inicial (IResettable).
        /// </summary>
        public void ResetState()
        {
            state = State.Idle;
            SetSpikeY(hiddenY);
        }
    }
}
