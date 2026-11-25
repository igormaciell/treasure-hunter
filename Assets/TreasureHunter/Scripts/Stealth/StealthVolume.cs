using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Base das áreas de furtividade (gatilhos): esconderijo, área segura, chão barulhento e zona de luz/sombra.
    /// Quando John entra ou sai do gatilho, a área se registra no <see cref="PlayerStealth"/>, que combina
    /// todas as áreas em que ele está para calcular visibilidade e ruído.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class StealthVolume : MonoBehaviour
    {
        /// <summary>Nome mostrado no HUD ("Cogumelos altos", "Fenda estreita"...).</summary>
        public string label = "";

        /// <summary>Garante que o colisor é gatilho.</summary>
        protected virtual void Awake()
        {
            Collider c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        /// <summary>John entrou.</summary>
        /// <param name="other">Colisor.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(GameTags.Player)) return;
            PlayerStealth p = other.GetComponentInParent<PlayerStealth>();
            if (p != null) p.EnterVolume(this);
        }

        /// <summary>John saiu.</summary>
        /// <param name="other">Colisor.</param>
        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(GameTags.Player)) return;
            PlayerStealth p = other.GetComponentInParent<PlayerStealth>();
            if (p != null) p.ExitVolume(this);
        }

        /// <summary>Desligar a área remove John dela.</summary>
        protected virtual void OnDisable()
        {
            if (PlayerStealth.Instance != null) PlayerStealth.Instance.ExitVolume(this);
        }
    }
}
