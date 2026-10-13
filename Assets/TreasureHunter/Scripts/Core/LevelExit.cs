using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Saída do Nível 1. Ao ser alcançada pelo jogador, conclui o nível e mostra o resumo.
    /// </summary>
    public class LevelExit : MonoBehaviour
    {
        /// <summary>Habilidade desbloqueada ao concluir (Combinação).</summary>
        public string skillOnExit = "combinacao";

        private bool done;

        /// <summary>
        /// Conclui o nível quando o jogador (Tag "Player") entra no gatilho.
        /// </summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (done || !other.CompareTag(GameTags.Player)) return;
            done = true;
            if (!string.IsNullOrEmpty(skillOnExit) && SkillManager.Instance != null) SkillManager.Instance.Unlock(skillOnExit);
            if (GameManager.Instance != null) GameManager.Instance.CompleteLevel();
        }
    }
}
