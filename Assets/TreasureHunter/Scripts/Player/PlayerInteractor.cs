using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Procura o objeto interativo mais próximo à frente de John, mostra o prompt no HUD
    /// e executa a interação quando o jogador pressiona E.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : MonoBehaviour
    {
        /// <summary>Raio de busca de objetos interativos.</summary>
        public float radius = 1.7f;
        /// <summary>Distância à frente do jogador onde fica o centro da busca.</summary>
        public float forwardOffset = 0.7f;

        /// <summary>Objeto atualmente em foco (ou nulo).</summary>
        public Interactable Current { get; private set; }

        private PlayerController controller;
        private readonly Collider[] buffer = new Collider[24];

        /// <summary>
        /// Guarda a referência ao controlador do jogador.
        /// </summary>
        private void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        /// <summary>
        /// A cada quadro escolhe o melhor alvo, atualiza o HUD e trata a tecla E.
        /// </summary>
        private void Update()
        {
            bool canAct = GameManager.Instance == null || GameManager.Instance.CanPlayerAct();
            Interactable best = canAct ? FindBest() : null;

            // Atualiza o foco e o prompt quando o alvo muda.
            if (best != Current)
            {
                Current = best;
                if (Current != null) Current.OnFocus();
            }
            UIManager ui = GameManager.Instance != null ? GameManager.Instance.ui : null;
            if (ui != null)
            {
                if (Current != null) ui.ShowPrompt("[E] " + Current.GetPrompt());
                else ui.HidePrompt();
            }

            if (Current != null && canAct && GameInput.InteractPressed())
            {
                Current.Interact(this);
            }
        }

        /// <summary>
        /// Busca, com uma esfera de sobreposição, o objeto interativo mais próximo e mais à frente.
        /// </summary>
        /// <returns>O melhor Interactable encontrado, ou nulo.</returns>
        private Interactable FindBest()
        {
            Vector3 center = transform.position + Vector3.up * 0.9f + controller.Facing * forwardOffset;
            int count = Physics.OverlapSphereNonAlloc(center, radius, buffer, ~0, QueryTriggerInteraction.Collide);

            Interactable best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Interactable it = buffer[i].GetComponentInParent<Interactable>();
                if (it == null || !it.CanInteract(this)) continue;

                // Pontuação = distância, com bônus para objetos à frente do personagem.
                Vector3 to = it.transform.position - transform.position;
                float dist = to.magnitude;
                float facing = Vector3.Dot(controller.Facing, to.normalized);
                float score = dist - facing * 0.6f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = it;
                }
            }
            return best;
        }

        /// <summary>
        /// Desenha a área de busca no editor para facilitar ajustes.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Vector3 facing = controller != null ? controller.Facing : transform.forward;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f + facing * forwardOffset, radius);
        }
    }
}
