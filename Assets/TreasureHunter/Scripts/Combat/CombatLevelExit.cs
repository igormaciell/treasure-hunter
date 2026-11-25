using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Passagem para o Nível 3. Ao ser alcançada, conclui o Nível 2 (resumo do GameManager) e acrescenta
    /// as estatísticas do combate: inimigos derrotados, disparos usados, precisão e a faca.
    /// O botão "Próximo nível" do resumo carrega a cena do Nível 3.
    /// </summary>
    public class CombatLevelExit : MonoBehaviour
    {
        /// <summary>Habilidade desbloqueada ao sair (opcional).</summary>
        public string skillOnExit = "";

        private bool done;

        /// <summary>
        /// Conclui o nível quando John entra no gatilho.
        /// </summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (done || !other.CompareTag(GameTags.Player)) return;
            done = true;
            if (!string.IsNullOrEmpty(skillOnExit)) PlayerCombat.UnlockSkill(skillOnExit);
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            gm.CompleteLevel();

            PlayerCombat pc = PlayerCombat.Instance;
            if (gm.ui != null && gm.ui.completeSummary != null && pc != null)
            {
                int shots = pc.ShotsFired;
                int accuracy = shots > 0 ? Mathf.RoundToInt(100f * pc.BulletHits / shots) : 0;
                gm.ui.completeSummary.text +=
                    "\nInimigos derrotados: " + EnemyBase.TotalKilled + " / " + CombatRoom.TotalEnemiesInLevel() +
                    "\nDisparos usados: " + shots + " / " + pc.startingShots + "   ·   Precisão: " + accuracy + "%" +
                    "\nFaca: " + (pc.HasKnife ? "encontrada" : "não encontrada");
            }
        }
    }
}
