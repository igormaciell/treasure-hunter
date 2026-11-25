using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Início de um trecho do percurso: mostra título, missão, tutorial e fala, e pode desbloquear uma habilidade.
    /// Diferente do RoomTrigger do Nível 1, NÃO muda o checkpoint (no parkour isso é papel das pedras e do totem).
    /// </summary>
    public class SectionTrigger : MonoBehaviour
    {
        public string title = "";
        public string subtitle = "";
        [TextArea] public string objective = "";
        [TextArea] public string tutorial = "";
        public string line = "";
        public string skillOnEnter = "";

        private bool used;

        /// <summary>Mostra os textos na primeira passagem do jogador.</summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (used || !other.CompareTag(GameTags.Player)) return;
            used = true;
            UIManager ui = GameManager.Instance != null ? GameManager.Instance.ui : null;
            if (ui != null)
            {
                if (!string.IsNullOrEmpty(title)) ui.ShowRoomTitle(title, subtitle);
                if (!string.IsNullOrEmpty(objective)) ui.SetObjective(objective);
                if (!string.IsNullOrEmpty(tutorial)) ui.ShowTutorial(tutorial, 8f);
                if (!string.IsNullOrEmpty(line)) ui.ShowLine(line);
            }
            ParkourUtil.UnlockSkill(skillOnEnter);
        }
    }
}
