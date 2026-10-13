using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Alavanca antiga: ao ser puxada (E), gira o cabo (rotação), toca som e
    /// envia um sinal aos mecanismos ligados (portas, liberação de caixote).
    /// </summary>
    public class Lever : Interactable
    {
        /// <summary>Parte móvel que gira ao puxar.</summary>
        public Transform handle;
        /// <summary>Mecanismos que recebem o sinal.</summary>
        public SignalReceiver[] targets;
        /// <summary>Ângulo do cabo antes de puxar.</summary>
        public float offAngle = -40f;
        /// <summary>Ângulo do cabo depois de puxar.</summary>
        public float onAngle = 40f;
        /// <summary>Velocidade da rotação do cabo (graus por segundo).</summary>
        public float rotateSpeed = 220f;
        /// <summary>Se verdadeiro, só pode ser puxada uma vez.</summary>
        public bool oneShot = true;
        /// <summary>Habilidade desbloqueada ao puxar (opcional).</summary>
        public string skillOnUse = "";
        /// <summary>Frase dita por John ao puxar (opcional).</summary>
        public string lineOnUse = "";

        /// <summary>Indica se a alavanca está na posição ligada.</summary>
        public bool IsOn { get; private set; }

        private float currentAngle;

        /// <summary>
        /// Coloca o cabo na posição inicial e acende o destaque.
        /// </summary>
        private void Start()
        {
            currentAngle = offAngle;
            ApplyAngle();
            SetHighlight(true);
        }

        /// <summary>
        /// Gira o cabo gradualmente até o ângulo do estado atual.
        /// </summary>
        private void Update()
        {
            float target = IsOn ? onAngle : offAngle;
            currentAngle = Mathf.MoveTowards(currentAngle, target, rotateSpeed * Time.deltaTime);
            ApplyAngle();
        }

        /// <summary>
        /// Aplica o ângulo atual ao cabo (rotação no eixo X local).
        /// </summary>
        private void ApplyAngle()
        {
            if (handle != null) handle.localRotation = Quaternion.Euler(currentAngle, 0f, 0f);
        }

        /// <summary>
        /// Só permite puxar novamente se não for de uso único.
        /// </summary>
        /// <param name="interactor">Quem está interagindo.</param>
        /// <returns>true se ainda pode ser usada.</returns>
        public override bool CanInteract(PlayerInteractor interactor)
        {
            return base.CanInteract(interactor) && !(oneShot && IsOn);
        }

        /// <summary>
        /// Puxa a alavanca: muda o estado, envia sinal aos alvos e dá o retorno ao jogador.
        /// </summary>
        /// <param name="interactor">Quem está interagindo.</param>
        public override void Interact(PlayerInteractor interactor)
        {
            IsOn = !IsOn;
            SignalReceiver.Broadcast(targets, IsOn, this);
            if (oneShot) SetHighlight(false);

            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.lever, transform.position);
            if (!string.IsNullOrEmpty(skillOnUse) && SkillManager.Instance != null) SkillManager.Instance.Unlock(skillOnUse);
            if (!string.IsNullOrEmpty(lineOnUse) && GameManager.Instance != null && GameManager.Instance.ui != null)
                GameManager.Instance.ui.ShowLine(lineOnUse);
        }
    }
}
