using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Alavanca enferrujada que dispara uma armadilha do cenário (por exemplo, o teto instável).
    /// Funciona como a alavanca do Nível 1 (gira o cabo e envia o sinal), mas volta à posição inicial
    /// quando a sala é reiniciada.
    /// </summary>
    public class TrapLever : Interactable, IResettable
    {
        /// <summary>Sala da alavanca.</summary>
        public int roomIndex;
        /// <summary>Cabo que gira.</summary>
        public Transform handle;
        /// <summary>Armadilhas ligadas à alavanca.</summary>
        public SignalReceiver[] targets;
        /// <summary>Ângulo do cabo antes de puxar.</summary>
        public float offAngle = -40f;
        /// <summary>Ângulo do cabo depois de puxar.</summary>
        public float onAngle = 40f;
        /// <summary>Velocidade do cabo (graus por segundo).</summary>
        public float rotateSpeed = 260f;
        /// <summary>Fala de John ao puxar.</summary>
        public string lineOnUse = "";

        private bool isOn;
        private float angle;

        /// <summary>Sala da alavanca (IResettable).</summary>
        public int RoomIndex => roomIndex;

        /// <summary>
        /// Posiciona o cabo e acende o destaque.
        /// </summary>
        private void Start()
        {
            angle = offAngle;
            Apply();
            SetHighlight(true);
        }

        /// <summary>
        /// Gira o cabo até o ângulo do estado atual.
        /// </summary>
        private void Update()
        {
            angle = Mathf.MoveTowards(angle, isOn ? onAngle : offAngle, rotateSpeed * Time.deltaTime);
            Apply();
        }

        /// <summary>
        /// Aplica o ângulo ao cabo.
        /// </summary>
        private void Apply()
        {
            if (handle != null) handle.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }

        /// <summary>
        /// Só pode ser puxada uma vez por tentativa.
        /// </summary>
        /// <param name="interactor">Quem interage.</param>
        /// <returns>true se ainda não foi puxada.</returns>
        public override bool CanInteract(PlayerInteractor interactor)
        {
            return base.CanInteract(interactor) && !isOn;
        }

        /// <summary>
        /// Puxa a alavanca e dispara a armadilha.
        /// </summary>
        /// <param name="interactor">Quem interage.</param>
        public override void Interact(PlayerInteractor interactor)
        {
            isOn = true;
            SetHighlight(false);
            SignalReceiver.Broadcast(targets, true, this);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.lever, transform.position);
            if (!string.IsNullOrEmpty(lineOnUse) && GameManager.Instance != null && GameManager.Instance.ui != null)
                GameManager.Instance.ui.ShowLine(lineOnUse);
        }

        /// <summary>
        /// Volta a alavanca para a posição inicial (IResettable).
        /// </summary>
        public void ResetState()
        {
            isOn = false;
            SetHighlight(true);
        }
    }
}
