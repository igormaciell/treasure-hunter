using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Tipos de itens coletáveis.
    /// </summary>
    public enum CollectibleType { Coin, Gem, Artifact, Heart }

    /// <summary>
    /// Item coletável (moeda, joia, artefato ou coração). É recolhido ao ser tocado pelo
    /// jogador (gatilho + Tag), toca um som, solta um brilho e registra a coleta.
    /// </summary>
    public class Collectible : MonoBehaviour
    {
        /// <summary>Tipo do item.</summary>
        public CollectibleType type = CollectibleType.Coin;
        /// <summary>Valor somado (moedas/joias) ou corações recuperados.</summary>
        public int value = 1;
        /// <summary>Nome do artefato (somente para o tipo Artifact).</summary>
        public string artifactName = "Artefato";
        /// <summary>Efeito de brilho criado ao coletar.</summary>
        public GameObject pickupEffect;

        private bool collected;

        /// <summary>
        /// Recolhe o item quando o jogador o toca.
        /// </summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (collected || !other.CompareTag(GameTags.Player)) return;

            // Corações só são consumidos se o jogador não estiver com a vida cheia.
            if (type == CollectibleType.Heart)
            {
                PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
                if (health == null || !health.Heal(value)) return;
            }

            collected = true;
            ApplyReward();
            if (pickupEffect != null) Instantiate(pickupEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }

        /// <summary>
        /// Registra a recompensa no GameManager e toca o som correspondente.
        /// </summary>
        private void ApplyReward()
        {
            GameManager gm = GameManager.Instance;
            AudioManager am = AudioManager.Instance;
            switch (type)
            {
                case CollectibleType.Coin:
                    if (gm != null) gm.AddTreasure(type, value);
                    if (am != null) am.PlayAt(am.coin, transform.position);
                    break;
                case CollectibleType.Gem:
                    if (gm != null) gm.AddTreasure(type, value);
                    if (am != null) am.PlayAt(am.gem, transform.position);
                    break;
                case CollectibleType.Artifact:
                    if (gm != null) gm.AddArtifact(artifactName);
                    if (am != null) am.Play2D(am.artifact);
                    break;
                case CollectibleType.Heart:
                    if (am != null) am.PlayAt(am.gem, transform.position);
                    break;
            }
        }
    }
}
