using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Saída do nível de parkour: conclui o nível e acrescenta ao resumo as estatísticas da travessia
    /// (quedas no abismo, totens colocados, bordas escaladas e saltos).
    /// </summary>
    public class ParkourLevelExit : MonoBehaviour
    {
        /// <summary>Habilidade desbloqueada ao sair.</summary>
        public string skillOnExit = "parkour";

        private bool done;

        /// <summary>Conclui ao contato do jogador.</summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (done || !other.CompareTag(GameTags.Player)) return;
            done = true;
            ParkourUtil.UnlockSkill(skillOnExit);
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            gm.CompleteLevel();

            ParkourController parkour = other.GetComponentInParent<ParkourController>();
            TotemAbility totem = other.GetComponentInParent<TotemAbility>();
            if (gm.ui != null && gm.ui.completeSummary != null)
            {
                gm.ui.completeSummary.text +=
                    "\nQuedas no abismo: " + VoidZone.Falls +
                    "   ·   Totens colocados: " + (totem != null ? totem.Placements : 0) +
                    "\nBordas escaladas: " + (parkour != null ? parkour.LedgesClimbed : 0) +
                    "   ·   Saltos com pouso: " + (parkour != null ? parkour.Landings : 0) +
                    (totem != null && totem.Unlocked ? "\nTotem de Checkpoint guardado para os próximos níveis." : "");
            }
        }
    }
}
