using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Pedestal do templo da superfície com o primeiro artefato (Ídolo de Añang).
    /// Retirar o artefato aciona a armadilha que derruba John no complexo subterrâneo.
    /// </summary>
    public class ArtifactPedestal : Interactable
    {
        /// <summary>Objeto visual do artefato sobre o pedestal.</summary>
        public GameObject artifactVisual;
        /// <summary>Nome do artefato registrado no resumo.</summary>
        public string artifactName = "Ídolo de Añang";
        /// <summary>Armadilha disparada ao retirar o artefato.</summary>
        public TempleCollapseTrap trap;
        /// <summary>Habilidade desbloqueada ao pegar o artefato.</summary>
        public string skillOnTake = "exploracao";

        private bool taken;

        /// <summary>
        /// Acende o destaque do pedestal.
        /// </summary>
        private void Start()
        {
            SetHighlight(true);
        }

        /// <summary>
        /// Só pode ser usado enquanto o artefato estiver no lugar.
        /// </summary>
        /// <param name="interactor">Quem está interagindo.</param>
        /// <returns>true se o artefato ainda não foi retirado.</returns>
        public override bool CanInteract(PlayerInteractor interactor)
        {
            return base.CanInteract(interactor) && !taken;
        }

        /// <summary>
        /// Retira o artefato, registra a coleta e dispara a armadilha.
        /// </summary>
        /// <param name="interactor">Quem está interagindo.</param>
        public override void Interact(PlayerInteractor interactor)
        {
            taken = true;
            SetHighlight(false);
            if (artifactVisual != null) artifactVisual.SetActive(false);

            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(AudioManager.Instance.artifact);
            if (GameManager.Instance != null) GameManager.Instance.AddArtifact(artifactName);
            if (SkillManager.Instance != null) SkillManager.Instance.Unlock(skillOnTake);
            if (trap != null) trap.Trigger();
        }
    }
}
