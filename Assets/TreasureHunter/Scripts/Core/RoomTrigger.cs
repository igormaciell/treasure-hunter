using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Gatilho na entrada de cada sala. Na primeira passagem do jogador: salva o checkpoint,
    /// mostra o nome da sala, atualiza a missão, exibe a dica e pode desbloquear uma habilidade.
    /// </summary>
    public class RoomTrigger : MonoBehaviour
    {
        /// <summary>Índice da sala (usado no reinício da sala).</summary>
        public int roomIndex = 1;
        /// <summary>Título grande exibido ao entrar.</summary>
        public string title = "";
        /// <summary>Subtítulo exibido ao entrar.</summary>
        public string subtitle = "";
        /// <summary>Nova missão (vazio = mantém a atual).</summary>
        [TextArea] public string objective = "";
        /// <summary>Popup tutorial (vazio = nenhum).</summary>
        [TextArea] public string tutorial = "";
        /// <summary>Frase de John (vazio = nenhuma).</summary>
        public string line = "";
        /// <summary>Habilidade desbloqueada ao entrar (vazio = nenhuma).</summary>
        public string skillOnEnter = "";
        /// <summary>Ponto de renascimento (se nulo, usa a posição do gatilho).</summary>
        public Transform respawnPoint;

        private bool used;

        /// <summary>
        /// Executa as ações da sala quando o jogador (Tag "Player") entra pela primeira vez.
        /// </summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (used || !other.CompareTag(GameTags.Player)) return;
            used = true;

            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            // Salva o checkpoint desta sala.
            Transform p = respawnPoint != null ? respawnPoint : transform;
            gm.SetCheckpoint(p.position, p.rotation, roomIndex);

            UIManager ui = gm.ui;
            if (ui != null)
            {
                if (!string.IsNullOrEmpty(title)) ui.ShowRoomTitle(title, subtitle);
                if (!string.IsNullOrEmpty(objective)) ui.SetObjective(objective);
                if (!string.IsNullOrEmpty(tutorial)) ui.ShowTutorial(tutorial);
                if (!string.IsNullOrEmpty(line)) ui.ShowLine(line);
            }
            if (!string.IsNullOrEmpty(skillOnEnter) && SkillManager.Instance != null) SkillManager.Instance.Unlock(skillOnEnter);
        }
    }
}
