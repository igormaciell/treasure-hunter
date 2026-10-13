using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Porta monumental de pedra que sobe (Translate) quando recebe os sinais necessários
    /// e desce quando os perde (se não estiver travada). Solta poeira (fumaça) e treme a câmera.
    /// </summary>
    public class StoneDoor : SignalReceiver
    {
        /// <summary>Altura que a porta sobe ao abrir.</summary>
        public float openHeight = 4.2f;
        /// <summary>Velocidade de abertura em metros por segundo.</summary>
        public float speed = 1.4f;
        /// <summary>Poeira emitida enquanto a porta se move.</summary>
        public ParticleSystem dust;
        /// <summary>Começa aberta (usado na porta do templo, que se fecha na armadilha).</summary>
        public bool startOpen = false;
        /// <summary>Habilidade desbloqueada quando a porta abre pela primeira vez (opcional).</summary>
        public string skillOnOpen = "";

        private Vector3 closedPosition;
        private float offset;
        private bool wantOpen;
        private bool moving;
        private bool openedOnce;

        /// <summary>
        /// Guarda a posição fechada e aplica o estado inicial.
        /// </summary>
        private void Awake()
        {
            closedPosition = transform.position;
            wantOpen = startOpen;
            offset = startOpen ? openHeight : 0f;
            transform.position = closedPosition + Vector3.up * offset;
        }

        /// <summary>
        /// Move a porta com transform.Translate até a altura desejada.
        /// </summary>
        private void Update()
        {
            float target = wantOpen ? openHeight : 0f;
            if (Mathf.Approximately(offset, target))
            {
                if (moving) StopMoving();
                return;
            }

            // Calcula o passo deste quadro sem ultrapassar o alvo e move a porta no eixo Y do mundo.
            float step = Mathf.MoveTowards(offset, target, speed * Time.deltaTime) - offset;
            offset += step;
            transform.Translate(Vector3.up * step, Space.World);
        }

        /// <summary>
        /// Reação ao sinal: abre quando ligado, fecha quando desligado.
        /// </summary>
        /// <param name="on">Novo estado.</param>
        protected override void OnStateChanged(bool on)
        {
            SetOpen(on);
        }

        /// <summary>
        /// Manda a porta abrir ou fechar, com som, poeira e tremor.
        /// </summary>
        /// <param name="open">true para abrir.</param>
        public void SetOpen(bool open)
        {
            if (wantOpen == open) return;
            wantOpen = open;
            moving = true;

            if (dust != null) dust.Play();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.doorGrind, transform.position);
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.6f, 0.05f);

            if (open && !openedOnce)
            {
                openedOnce = true;
                if (!string.IsNullOrEmpty(skillOnOpen) && SkillManager.Instance != null) SkillManager.Instance.Unlock(skillOnOpen);
            }
        }

        /// <summary>
        /// Encerra o efeito de poeira quando a porta para.
        /// </summary>
        private void StopMoving()
        {
            moving = false;
            if (dust != null) dust.Stop();
        }
    }
}
