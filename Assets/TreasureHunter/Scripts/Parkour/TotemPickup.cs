using System.Collections;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// O Totem de Checkpoint liberado pelo cristal: flutua e brilha; John coleta encostando ou com E.
    /// A coleta libera a habilidade (TotemAbility) e salva o progresso para os próximos níveis.
    /// </summary>
    public class TotemPickup : Interactable
    {
        /// <summary>Parte visual (sobe ao aparecer).</summary>
        public Transform visual;
        public float riseHeight = 0.8f;
        public float riseTime = 0.8f;
        public GameObject[] pickupEffects;
        public string toastTitle = "Totem de Checkpoint!";
        [TextArea] public string toastBody = "Segure [T] para ver onde o totem fica (verde = pode, vermelho = não pode). Solte [T] para colocá-lo.";
        [TextArea] public string tutorial = "Segure T e aponte a câmera para um chão plano perto de você.\nVerde: solte T para colocar o totem. Vermelho: o motivo aparece na tela.\nSe cair no abismo, você volta para o totem. Use T de novo para mudá-lo de lugar.";
        [TextArea] public string objective = "Teste o totem nos pilares ao lado e siga para a travessia.";

        private bool taken;
        private Vector3 visualTarget;

        /// <summary>Animação de surgimento ao ser ativado.</summary>
        private void OnEnable()
        {
            if (visual == null) return;
            if (visualTarget == Vector3.zero) visualTarget = visual.localPosition;
            StartCoroutine(Rise());
        }

        /// <summary>
        /// Sobe de dentro do cristal quebrado.
        /// </summary>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator Rise()
        {
            Vector3 start = visualTarget - Vector3.up * riseHeight;
            float t = 0f;
            while (t < riseTime)
            {
                t += Time.deltaTime;
                float k = t / riseTime;
                visual.localPosition = Vector3.Lerp(start, visualTarget, k * k * (3f - 2f * k));
                yield return null;
            }
            visual.localPosition = visualTarget;
        }

        /// <summary>Pode coletar enquanto não foi coletado.</summary>
        /// <param name="interactor">Jogador.</param>
        /// <returns>true se disponível.</returns>
        public override bool CanInteract(PlayerInteractor interactor)
        {
            return !taken;
        }

        /// <summary>Coleta com E.</summary>
        /// <param name="interactor">Jogador.</param>
        public override void Interact(PlayerInteractor interactor)
        {
            Take(interactor.GetComponent<TotemAbility>());
        }

        /// <summary>Coleta ao encostar.</summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(GameTags.Player)) Take(other.GetComponentInParent<TotemAbility>());
        }

        /// <summary>
        /// Libera a habilidade, salva o progresso, mostra o tutorial e remove o item.
        /// </summary>
        /// <param name="ability">Habilidade do jogador.</param>
        private void Take(TotemAbility ability)
        {
            if (taken || ability == null) return;
            taken = true;
            ability.Unlock(true);
            ParkourUtil.SpawnAll(pickupEffects, transform.position + Vector3.up * 0.8f);
            ParkourAudio a = ParkourAudio.Instance;
            if (a != null) ParkourAudio.Play2D(a.totemPickup, 1f);
            ParkourUtil.UnlockSkill("totem");
            UIManager ui = GameManager.Instance != null ? GameManager.Instance.ui : null;
            if (ui != null)
            {
                ui.HidePrompt();
                ui.ShowToast(toastTitle, toastBody, 5f);
                ui.ShowTutorial(tutorial, 12f);
                ui.SetObjective(objective);
                ui.ShowLine("Com isto posso marcar meu próprio caminho de volta.");
            }
            Destroy(gameObject);
        }
    }
}
