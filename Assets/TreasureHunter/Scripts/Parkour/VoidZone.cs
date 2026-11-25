using System.Collections;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// O Void no fundo da caverna: um gatilho enorme abaixo das plataformas. Quem cai escurece a tela e volta
    /// ao último checkpoint válido (pedra fixa ou totem). Não tira vida (configurável).
    /// </summary>
    public class VoidZone : MonoBehaviour
    {
        /// <summary>Instância da cena.</summary>
        public static VoidZone Instance { get; private set; }
        /// <summary>Quedas no abismo nesta partida do nível.</summary>
        public static int Falls { get; private set; }

        /// <summary>Corações perdidos por queda (0 = nenhum).</summary>
        public int damage = 0;
        public float fadeOut = 0.35f;
        public float fadeIn = 0.45f;
        /// <summary>Efeitos no ponto de retorno (vetor; o totem tem os seus).</summary>
        public GameObject[] respawnEffects;

        private bool busy;

        /// <summary>Registra a instância e zera os contadores do nível.</summary>
        private void Awake()
        {
            Instance = this;
            Falls = 0;
            ParkourCheckpoints.ResetStatics();
        }

        /// <summary>Libera a instância.</summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Jogador caiu no Void.</summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(GameTags.Player)) Respawn();
        }

        /// <summary>
        /// Leva John de volta ao checkpoint (com escurecimento). Pode ser chamado pela altura de segurança.
        /// </summary>
        public void Respawn()
        {
            if (busy) return;
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            StartCoroutine(RespawnRoutine(gm));
        }

        /// <summary>
        /// Escurece, teletransporta, mostra os efeitos e clareia.
        /// </summary>
        /// <param name="gm">Gerenciador do jogo.</param>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator RespawnRoutine(GameManager gm)
        {
            busy = true;
            Falls++;
            ParkourAudio a = ParkourAudio.Instance;
            if (a != null) ParkourAudio.Play2D(a.voidWhoosh, 0.9f);
            if (gm.ui != null) yield return gm.ui.Fade(1f, fadeOut);

            ParkourController parkour = gm.player != null ? gm.player.GetComponent<ParkourController>() : null;
            if (parkour != null) parkour.CancelMovement();
            gm.RespawnPlayer();
            if (parkour != null) parkour.CancelMovement();
            if (damage > 0 && gm.playerHealth != null) gm.playerHealth.TakeDamage(damage, gm.player.transform.position);

            TotemAbility totem = gm.player != null ? gm.player.GetComponent<TotemAbility>() : null;
            bool atTotem = totem != null && totem.Totem != null && ReferenceEquals(ParkourCheckpoints.Current, totem.Totem);
            if (atTotem) totem.OnRespawned();
            else ParkourUtil.SpawnAll(respawnEffects, gm.player.transform.position);

            if (Falls == 1 || (Falls % 4 == 0))
            {
                string tip = totem != null && totem.Unlocked
                    ? (atTotem ? "Você voltou ao totem." : "Dica: segure T para colocar o totem perto dos saltos difíceis.")
                    : "Você caiu no abismo e voltou ao último checkpoint.";
                ParkourUtil.Hint(tip);
            }

            if (gm.ui != null) yield return gm.ui.Fade(0f, fadeIn);
            busy = false;
        }
    }
}
