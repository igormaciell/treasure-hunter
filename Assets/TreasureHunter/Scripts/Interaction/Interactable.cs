using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Classe base de tudo com que o jogador pode interagir pressionando E
    /// (alavancas, botões de símbolo, braseiros, pedestais, tábuas de habilidade).
    /// Implementa a interação contextual descrita no GDD: o mesmo botão serve para tudo.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        /// <summary>Texto mostrado no HUD quando o jogador está perto (ex.: "Puxar alavanca").</summary>
        public string prompt = "Interagir";
        /// <summary>Texto de ajuda mostrado uma única vez na primeira aproximação (popup tutorial). Vazio = sem popup.</summary>
        [TextArea] public string firstTimeTip = "";
        /// <summary>Luz opcional que dá o "brilho discreto" nos objetos utilizáveis.</summary>
        public Light highlightLight;

        private bool tipShown;

        /// <summary>
        /// Diz se o objeto aceita interação agora.
        /// </summary>
        /// <param name="interactor">Quem está tentando interagir.</param>
        /// <returns>true se a interação está disponível.</returns>
        public virtual bool CanInteract(PlayerInteractor interactor)
        {
            return enabled;
        }

        /// <summary>
        /// Texto exibido no HUD para este objeto.
        /// </summary>
        /// <returns>Texto do prompt sem a tecla (o HUD acrescenta "[E]").</returns>
        public virtual string GetPrompt()
        {
            return prompt;
        }

        /// <summary>
        /// Executa a ação do objeto. Cada tipo de objeto implementa a sua.
        /// </summary>
        /// <param name="interactor">O componente de interação do jogador.</param>
        public abstract void Interact(PlayerInteractor interactor);

        /// <summary>
        /// Chamado quando o objeto vira o foco do jogador; mostra o popup de ajuda na primeira vez.
        /// </summary>
        public virtual void OnFocus()
        {
            if (tipShown || string.IsNullOrEmpty(firstTimeTip)) return;
            tipShown = true;
            if (GameManager.Instance != null && GameManager.Instance.ui != null)
                GameManager.Instance.ui.ShowTutorial(firstTimeTip);
        }

        /// <summary>
        /// Liga ou desliga o brilho de destaque.
        /// </summary>
        /// <param name="on">true para acender o destaque.</param>
        protected void SetHighlight(bool on)
        {
            if (highlightLight != null) highlightLight.enabled = on;
        }
    }
}
