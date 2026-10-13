using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Alçapão de madeira de uma plataforma suspensa (Sala 4). Ao receber o sinal da alavanca,
    /// gira em torno da dobradiça (rotação) e solta o caixote que estava apoiado nele.
    /// </summary>
    public class HatchRelease : SignalReceiver
    {
        /// <summary>Dobradiça que gira para abrir o alçapão.</summary>
        public Transform hinge;
        /// <summary>Colisor que sustenta o caixote (desligado ao abrir).</summary>
        public Collider supportCollider;
        /// <summary>Ângulo final de abertura.</summary>
        public float openAngle = 95f;
        /// <summary>Velocidade de abertura (graus por segundo).</summary>
        public float openSpeed = 260f;
        /// <summary>Poeira solta ao abrir.</summary>
        public ParticleSystem dust;
        /// <summary>Frase de John ao liberar o caixote.</summary>
        public string lineOnOpen = "";

        private float angle;
        private bool open;

        /// <summary>
        /// Gira a dobradiça até o ângulo de abertura.
        /// </summary>
        private void Update()
        {
            if (!open || hinge == null) return;
            angle = Mathf.MoveTowards(angle, openAngle, openSpeed * Time.deltaTime);
            hinge.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// Abre o alçapão ao receber o sinal.
        /// </summary>
        /// <param name="on">Novo estado.</param>
        protected override void OnStateChanged(bool on)
        {
            if (!on || open) return;
            open = true;
            if (supportCollider != null) supportCollider.enabled = false;
            if (dust != null) dust.Play();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.doorGrind, transform.position, 0.6f);
            if (!string.IsNullOrEmpty(lineOnOpen) && GameManager.Instance != null && GameManager.Instance.ui != null)
                GameManager.Instance.ui.ShowLine(lineOnOpen);
        }
    }
}
