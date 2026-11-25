using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Saída do Nível 4 (para o Nível 5): conclui o nível e acrescenta ao resumo as estatísticas da
    /// furtividade — vezes visto, mortes, disparos e se John passou sem ser notado.
    /// </summary>
    public class StealthLevelExit : MonoBehaviour
    {
        public string skillOnExit = "furtividade";
        public string ghostSkill = "fantasma";

        private bool done;

        /// <summary>
        /// John chegou à saída.
        /// </summary>
        /// <param name="other">Colisor.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (done || !other.CompareTag(GameTags.Player)) return;
            done = true;
            PlayerStealth stealth = other.GetComponentInParent<PlayerStealth>();
            PlayerCombat combat = other.GetComponentInParent<PlayerCombat>();
            StealthTorch torch = other.GetComponentInParent<StealthTorch>();
            int spotted = stealth != null ? stealth.TimesSpotted : 0;
            StealthUtil.UnlockSkill(skillOnExit);
            if (spotted == 0) StealthUtil.UnlockSkill(ghostSkill);
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            gm.CompleteLevel();
            if (gm.ui != null && gm.ui.completeSummary != null)
            {
                gm.ui.completeSummary.text +=
                    "\nVezes visto pelos ogros: " + spotted +
                    "   ·   Golpes de ogro: " + (stealth != null ? stealth.Deaths : 0) +
                    "\nDisparos: " + (combat != null ? combat.ShotsFired : 0) +
                    "   ·   Tocha acesa: " + (torch != null ? torch.TimesLit : 0) + " vez(es)" +
                    (spotted == 0 ? "\nFANTASMA: atravessou as galerias sem ser visto!" : "");
            }
        }
    }
}
