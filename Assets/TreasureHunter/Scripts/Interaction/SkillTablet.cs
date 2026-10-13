using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Tábua de pedra com inscrições (elemento do tema "Skills"). Ao ser lida (E), mostra
    /// uma dica sobre a habilidade da sala, sem entregar a solução do enigma.
    /// </summary>
    public class SkillTablet : Interactable
    {
        /// <summary>Título exibido no aviso.</summary>
        public string title = "Inscrição antiga";
        /// <summary>Texto da inscrição.</summary>
        [TextArea] public string text = "";

        /// <summary>
        /// Acende o destaque da tábua.
        /// </summary>
        private void Start()
        {
            SetHighlight(true);
        }

        /// <summary>
        /// Mostra a inscrição na tela.
        /// </summary>
        /// <param name="interactor">Quem está interagindo.</param>
        public override void Interact(PlayerInteractor interactor)
        {
            if (GameManager.Instance != null && GameManager.Instance.ui != null)
                GameManager.Instance.ui.ShowToast(title, text, 6f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.torchWhoosh, transform.position, 0.4f);
        }
    }
}
