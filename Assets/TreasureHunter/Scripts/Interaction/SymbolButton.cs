using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Botão de pedra com um símbolo antigo. Ao ser pressionado (E), afunda um pouco
    /// e informa o símbolo ao enigma de sequência.
    /// </summary>
    public class SymbolButton : Interactable
    {
        /// <summary>Face do botão onde o símbolo é desenhado.</summary>
        public Renderer face;
        /// <summary>Parte que afunda ao pressionar.</summary>
        public Transform pressPart;
        /// <summary>Luz que indica que o mecanismo está energizado.</summary>
        public Light powerLight;

        /// <summary>Id do símbolo deste botão.</summary>
        public int Symbol { get; private set; }

        private SymbolSequencePuzzle puzzle;
        private Vector3 pressStart;
        private float pressTimer;

        /// <summary>
        /// Guarda a posição inicial da parte móvel.
        /// </summary>
        private void Awake()
        {
            if (pressPart != null) pressStart = pressPart.localPosition;
        }

        /// <summary>
        /// Anima a parte móvel voltando depois de pressionada.
        /// </summary>
        private void Update()
        {
            if (pressPart == null) return;
            pressTimer = Mathf.Max(0f, pressTimer - Time.deltaTime);
            float depth = pressTimer > 0f ? 0.06f : 0f;
            pressPart.localPosition = Vector3.MoveTowards(pressPart.localPosition, pressStart + Vector3.back * depth, Time.deltaTime);
        }

        /// <summary>
        /// Configura o botão com o símbolo sorteado pelo enigma.
        /// </summary>
        /// <param name="owner">Enigma ao qual o botão pertence.</param>
        /// <param name="symbol">Id do símbolo.</param>
        /// <param name="material">Material do símbolo apagado.</param>
        public void Setup(SymbolSequencePuzzle owner, int symbol, Material material)
        {
            puzzle = owner;
            Symbol = symbol;
            if (face != null) face.sharedMaterial = material;
            SetPoweredLight(owner.Powered);
        }

        /// <summary>
        /// Troca o material da face para aceso/apagado.
        /// </summary>
        /// <param name="lit">true para aceso.</param>
        /// <param name="material">Material a aplicar.</param>
        public void SetLit(bool lit, Material material)
        {
            if (face != null) face.sharedMaterial = material;
        }

        /// <summary>
        /// Liga a luz que mostra que o botão está energizado.
        /// </summary>
        /// <param name="on">true para ligar.</param>
        public void SetPoweredLight(bool on)
        {
            if (powerLight != null) powerLight.enabled = on;
        }

        /// <summary>
        /// Não aceita interação depois que o enigma foi resolvido.
        /// </summary>
        /// <param name="interactor">Quem está interagindo.</param>
        /// <returns>true se o enigma existe e ainda não foi resolvido.</returns>
        public override bool CanInteract(PlayerInteractor interactor)
        {
            return base.CanInteract(interactor) && puzzle != null && !puzzle.Solved;
        }

        /// <summary>
        /// Pressiona o botão: anima e envia o símbolo ao enigma (ou avisa se estiver sem energia).
        /// </summary>
        /// <param name="interactor">Quem está interagindo.</param>
        public override void Interact(PlayerInteractor interactor)
        {
            if (!puzzle.Powered)
            {
                if (GameManager.Instance != null && GameManager.Instance.ui != null) GameManager.Instance.ui.ShowLine(puzzle.UnpoweredMessage);
                return;
            }
            pressTimer = 0.2f;
            puzzle.Press(this);
        }
    }
}
