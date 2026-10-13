using System.Collections;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Fim da queda da introdução: quando John cai pelo buraco do templo e toca este gatilho,
    /// a tela escurece, ele é levado ao início do Nível 1 no subterrâneo, a atmosfera muda
    /// para a de caverna e o primeiro tutorial aparece.
    /// </summary>
    public class FallTransition : MonoBehaviour
    {
        /// <summary>Ponto onde John acorda no subterrâneo.</summary>
        public Transform destination;
        /// <summary>Índice do perfil de atmosfera a aplicar (1 = subterrâneo).</summary>
        public int atmosphereIndex = 1;
        /// <summary>Título da sala exibido ao acordar.</summary>
        public string roomTitle = "Nível 1 — A Entrada do Templo";
        /// <summary>Subtítulo exibido ao acordar.</summary>
        public string roomSubtitle = "Sala 1: Câmara das Raízes";
        /// <summary>Nova missão.</summary>
        [TextArea] public string objective = "Encontre uma saída das primeiras câmaras.";
        /// <summary>Frase de John ao acordar.</summary>
        public string wakeLine = "Argh... Que queda. A saída lá em cima foi bloqueada. Preciso achar outro caminho.";
        /// <summary>Popup tutorial exibido ao acordar.</summary>
        [TextArea] public string tutorial = "Explore a sala. Procure algo diferente entre as raízes. [E] interage com objetos.";

        private bool used;

        /// <summary>
        /// Inicia a transição quando o jogador entra no gatilho.
        /// </summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (used || !other.CompareTag(GameTags.Player)) return;
            used = true;
            StartCoroutine(TransitionRoutine());
        }

        /// <summary>
        /// Escurece a tela, teleporta, troca a atmosfera e clareia.
        /// </summary>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator TransitionRoutine()
        {
            GameManager gm = GameManager.Instance;
            UIManager ui = gm != null ? gm.ui : null;

            if (gm != null) gm.SetCutscene(true);
            if (ui != null) yield return ui.Fade(1f, 0.35f);

            // Teleporta, define o checkpoint da Sala 1 e aplica a atmosfera do subterrâneo.
            if (gm != null && destination != null)
            {
                gm.player.Teleport(destination.position, destination.rotation);
                gm.SetCheckpoint(destination.position, destination.rotation, 1);
            }
            if (AtmosphereController.Instance != null) AtmosphereController.Instance.Apply(atmosphereIndex);
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(AudioManager.Instance.land);

            yield return new WaitForSeconds(0.8f);
            if (ui != null)
            {
                ui.SetObjective(objective);
                yield return ui.Fade(0f, 1.2f);
                ui.ShowRoomTitle(roomTitle, roomSubtitle);
                ui.ShowLine(wakeLine);
                ui.ShowTutorial(tutorial);
            }
            if (gm != null) gm.SetCutscene(false);
        }
    }
}
