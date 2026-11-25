using UnityEngine;
using UnityEngine.AI;

namespace TreasureHunter
{
    /// <summary>
    /// Grade de ferro (portão levadiço) entre as salas do Nível 2. Sobe e desce com Translate.
    /// Fecha atrás de John quando o combate começa e abre quando todos os inimigos da sala caem.
    /// Enquanto fechada, um NavMeshObstacle bloqueia a passagem dos esqueletos no NavMesh.
    /// </summary>
    public class IronGate : SignalReceiver
    {
        /// <summary>Altura que a grade sobe ao abrir.</summary>
        public float openHeight = 3.8f;
        /// <summary>Velocidade (m/s).</summary>
        public float speed = 2f;
        /// <summary>Começa aberta.</summary>
        public bool startOpen = false;
        /// <summary>Poeira enquanto se move.</summary>
        public ParticleSystem dust;
        /// <summary>Obstáculo de navegação (ativo com a grade fechada).</summary>
        public NavMeshObstacle obstacle;

        private Vector3 closedPosition;
        private float offset;
        private bool wantOpen;
        private bool moving;

        /// <summary>Indica se a grade está totalmente aberta.</summary>
        public bool IsFullyOpen => offset >= openHeight - 0.01f;

        /// <summary>
        /// Guarda a posição fechada e aplica o estado inicial.
        /// </summary>
        private void Awake()
        {
            closedPosition = transform.position;
            wantOpen = startOpen;
            offset = startOpen ? openHeight : 0f;
            transform.position = closedPosition + Vector3.up * offset;
            UpdateObstacle();
        }

        /// <summary>
        /// Move a grade com Translate até a altura desejada.
        /// </summary>
        private void Update()
        {
            float target = wantOpen ? openHeight : 0f;
            if (Mathf.Approximately(offset, target))
            {
                if (moving) { moving = false; if (dust != null) dust.Stop(); }
                return;
            }
            float step = Mathf.MoveTowards(offset, target, speed * Time.deltaTime) - offset;
            offset += step;
            transform.Translate(Vector3.up * step, Space.World);
            UpdateObstacle();
        }

        /// <summary>
        /// Reação ao sinal (sala liberada): abre quando ligado.
        /// </summary>
        /// <param name="on">Novo estado.</param>
        protected override void OnStateChanged(bool on)
        {
            SetOpen(on);
        }

        /// <summary>
        /// Abre ou fecha a grade, com som de correntes, poeira e tremor.
        /// </summary>
        /// <param name="open">true para abrir.</param>
        public void SetOpen(bool open)
        {
            if (wantOpen == open) return;
            wantOpen = open;
            moving = true;
            if (dust != null) dust.Play();
            CombatAudio a = CombatAudio.Instance;
            if (a != null && a.gateMove != null) CombatAudio.Play(a.gateMove, transform.position, 1f, 0.03f);
            else if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.doorGrind, transform.position);
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.4f, 0.04f);
        }

        /// <summary>
        /// Liga o obstáculo de navegação enquanto a grade bloqueia a passagem.
        /// </summary>
        private void UpdateObstacle()
        {
            if (obstacle != null) obstacle.enabled = offset < openHeight * 0.6f;
        }
    }
}
