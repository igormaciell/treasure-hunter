using UnityEngine;

namespace TreasureHunter
{
    /// <summary>Tipo de arma coletável.</summary>
    public enum PickupKind { Revolver, Knife }

    /// <summary>
    /// Arma que John pode pegar: o revólver (com os 25 disparos do nível, pego com E na mesa da cripta)
    /// ou a faca deixada pelos esqueletos (pega ao encostar ou com E).
    /// </summary>
    public class WeaponPickup : Interactable
    {
        /// <summary>Qual arma este objeto entrega.</summary>
        public PickupKind kind = PickupKind.Knife;
        /// <summary>Disparos entregues junto com o revólver.</summary>
        public int shots = 25;
        /// <summary>Pega ao encostar (faca). O revólver exige a tecla E.</summary>
        public bool autoPickup = true;
        /// <summary>Efeito criado ao coletar.</summary>
        public GameObject pickupEffect;
        /// <summary>Mecanismos avisados ao coletar (opcional).</summary>
        public SignalReceiver[] targets;
        /// <summary>Título do aviso exibido ao coletar.</summary>
        public string toastTitle = "";
        /// <summary>Texto do aviso exibido ao coletar.</summary>
        [TextArea] public string toastBody = "";
        /// <summary>Dica de tutorial exibida ao coletar.</summary>
        [TextArea] public string tutorialOnPickup = "";
        /// <summary>Fala de John ao coletar.</summary>
        public string lineOnPickup = "";
        /// <summary>Nova missão ao coletar (vazio = mantém).</summary>
        [TextArea] public string objectiveOnPickup = "";

        private bool taken;

        /// <summary>
        /// Pode ser coletada enquanto ainda estiver aqui.
        /// </summary>
        /// <param name="interactor">Quem interage.</param>
        /// <returns>true se ainda não foi coletada.</returns>
        public override bool CanInteract(PlayerInteractor interactor)
        {
            return base.CanInteract(interactor) && !taken;
        }

        /// <summary>
        /// Coleta com a tecla E.
        /// </summary>
        /// <param name="interactor">Quem interage.</param>
        public override void Interact(PlayerInteractor interactor)
        {
            Take(interactor.GetComponent<PlayerCombat>());
        }

        /// <summary>
        /// Coleta ao encostar (quando autoPickup está ligado).
        /// </summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (!autoPickup || taken || !other.CompareTag(GameTags.Player)) return;
            Take(other.GetComponentInParent<PlayerCombat>());
        }

        /// <summary>
        /// Entrega a arma, mostra avisos/dicas, avisa os mecanismos e remove o objeto.
        /// </summary>
        /// <param name="combat">Sistema de combate do jogador.</param>
        private void Take(PlayerCombat combat)
        {
            if (taken || combat == null) return;
            taken = true;
            bool isNew;
            if (kind == PickupKind.Revolver)
            {
                isNew = !combat.HasRevolver;
                combat.GiveRevolver(shots);
            }
            else
            {
                isNew = combat.GiveKnife();
            }

            CombatAudio a = CombatAudio.Instance;
            if (a != null) CombatAudio.Play(kind == PickupKind.Knife ? a.knifePickup : a.weaponPickup, transform.position);
            if (pickupEffect != null) Instantiate(pickupEffect, transform.position + Vector3.up * 0.5f, Quaternion.identity);

            UIManager ui = GameManager.Instance != null ? GameManager.Instance.ui : null;
            if (ui != null)
            {
                if (!isNew) ui.ShowToast("Você já tem esta arma", kind == PickupKind.Knife ? "Uma faca basta." : "", 2f);
                else
                {
                    if (!string.IsNullOrEmpty(toastTitle)) ui.ShowToast(toastTitle, toastBody, 4.5f);
                    if (!string.IsNullOrEmpty(tutorialOnPickup)) ui.ShowTutorial(tutorialOnPickup, 9f);
                    if (!string.IsNullOrEmpty(lineOnPickup)) ui.ShowLine(lineOnPickup);
                    if (!string.IsNullOrEmpty(objectiveOnPickup)) ui.SetObjective(objectiveOnPickup);
                }
                ui.HidePrompt();
            }
            SignalReceiver.Broadcast(targets, true, this);
            Destroy(gameObject);
        }
    }
}
