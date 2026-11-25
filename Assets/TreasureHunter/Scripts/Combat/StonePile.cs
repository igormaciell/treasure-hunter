using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Pilha de pedras soltas: com E, John guarda uma pedra no bolso (máximo de 3) para arremessar com G.
    /// As pedras visíveis somem conforme são pegas e voltam quando a sala é reiniciada.
    /// </summary>
    public class StonePile : Interactable, IResettable
    {
        /// <summary>Sala à qual a pilha pertence.</summary>
        public int roomIndex;
        /// <summary>Quantidade de pedras na pilha.</summary>
        public int stones = 5;
        /// <summary>Pedras visíveis (uma some a cada coleta).</summary>
        public GameObject[] stoneVisuals;

        private int left;

        /// <summary>Sala da pilha (IResettable).</summary>
        public int RoomIndex => roomIndex;

        /// <summary>
        /// Enche a pilha ao começar.
        /// </summary>
        private void Awake()
        {
            left = stones;
            Refresh();
        }

        /// <summary>
        /// Só aceita interação enquanto houver pedras.
        /// </summary>
        /// <param name="interactor">Quem interage.</param>
        /// <returns>true se ainda há pedras.</returns>
        public override bool CanInteract(PlayerInteractor interactor)
        {
            return base.CanInteract(interactor) && left > 0;
        }

        /// <summary>
        /// Mostra quantas pedras restam no prompt.
        /// </summary>
        /// <returns>Texto do prompt.</returns>
        public override string GetPrompt()
        {
            return prompt + " (" + left + ")";
        }

        /// <summary>
        /// Guarda uma pedra no bolso de John, se houver espaço.
        /// </summary>
        /// <param name="interactor">Quem interage.</param>
        public override void Interact(PlayerInteractor interactor)
        {
            PlayerCombat combat = interactor.GetComponent<PlayerCombat>();
            if (combat == null || left <= 0) return;
            if (!combat.AddStone())
            {
                combat.ShowHint("Seus bolsos já estão cheios (3 pedras). Arremesse com [G].");
                return;
            }
            left--;
            CombatAudio a = CombatAudio.Instance;
            if (a != null) CombatAudio.Play(a.stonePickup, transform.position, 0.8f);
            Refresh();
        }

        /// <summary>
        /// Esconde as pedras visíveis que já foram pegas.
        /// </summary>
        private void Refresh()
        {
            if (stoneVisuals == null) return;
            int visible = Mathf.CeilToInt(stoneVisuals.Length * (left / (float)Mathf.Max(1, stones)));
            for (int i = 0; i < stoneVisuals.Length; i++)
            {
                if (stoneVisuals[i] != null) stoneVisuals[i].SetActive(i < visible);
            }
        }

        /// <summary>
        /// Enche a pilha de novo (IResettable).
        /// </summary>
        public void ResetState()
        {
            left = stones;
            Refresh();
        }
    }
}
