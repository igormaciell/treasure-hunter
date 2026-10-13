using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Vida de John Craft em corações. Recebe dano de perigos (pedras, tronco, quedas),
    /// aplica um empurrão de reação, concede invulnerabilidade temporária e
    /// avisa o GameManager quando a vida acaba.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerHealth : MonoBehaviour
    {
        /// <summary>Quantidade máxima de corações.</summary>
        public int maxHearts = 5;
        /// <summary>Tempo (s) de invulnerabilidade depois de levar dano.</summary>
        public float invulnerabilityTime = 1.0f;
        /// <summary>Força do empurrão ao levar dano.</summary>
        public float knockbackForce = 6f;
        /// <summary>Renderizadores que piscam durante a invulnerabilidade.</summary>
        public Renderer[] blinkRenderers;

        /// <summary>Corações atuais.</summary>
        public int Current { get; private set; }

        private PlayerController controller;
        private float invulnerableUntil;

        /// <summary>
        /// Inicializa a vida cheia.
        /// </summary>
        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            Current = maxHearts;
        }

        /// <summary>
        /// Atualiza a interface com a vida inicial.
        /// </summary>
        private void Start()
        {
            RefreshUI();
        }

        /// <summary>
        /// Faz o modelo piscar enquanto o jogador está invulnerável.
        /// </summary>
        private void Update()
        {
            if (blinkRenderers == null) return;
            bool invulnerable = Time.time < invulnerableUntil;
            // Alterna a visibilidade 12 vezes por segundo durante a invulnerabilidade.
            bool visible = !invulnerable || Mathf.FloorToInt(Time.time * 12f) % 2 == 0;
            foreach (Renderer r in blinkRenderers)
            {
                if (r != null) r.enabled = visible;
            }
        }

        /// <summary>
        /// Aplica dano ao jogador.
        /// </summary>
        /// <param name="amount">Quantidade de corações perdidos.</param>
        /// <param name="sourcePosition">Posição de onde veio o dano (define a direção do empurrão).</param>
        /// <returns>true se o dano foi aplicado; false se o jogador estava invulnerável ou já sem vida.</returns>
        public bool TakeDamage(int amount, Vector3 sourcePosition)
        {
            if (Current <= 0 || Time.time < invulnerableUntil) return false;
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return false;

            Current = Mathf.Max(0, Current - amount);
            invulnerableUntil = Time.time + invulnerabilityTime;

            // Empurra o jogador para longe da fonte do dano, levemente para cima.
            Vector3 away = transform.position - sourcePosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -controller.Facing;
            Vector3 impulse = away.normalized * knockbackForce + Vector3.up * 3f;
            controller.Knockback(impulse);

            // Feedback de áudio, câmera e interface.
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.hurt, transform.position);
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.25f, 0.15f);
            RefreshUI();
            if (GameManager.Instance != null && GameManager.Instance.ui != null) GameManager.Instance.ui.FlashDamage();

            if (Current <= 0 && GameManager.Instance != null) GameManager.Instance.GameOver();
            return true;
        }

        /// <summary>
        /// Recupera corações (itens de cura).
        /// </summary>
        /// <param name="amount">Quantidade de corações recuperados.</param>
        /// <returns>true se algum coração foi recuperado.</returns>
        public bool Heal(int amount)
        {
            if (Current >= maxHearts) return false;
            Current = Mathf.Min(maxHearts, Current + amount);
            RefreshUI();
            return true;
        }

        /// <summary>
        /// Restaura a vida cheia (usado ao continuar depois de uma derrota).
        /// </summary>
        public void ResetHealth()
        {
            Current = maxHearts;
            invulnerableUntil = Time.time + invulnerabilityTime;
            RefreshUI();
        }

        /// <summary>
        /// Envia a vida atual para o HUD.
        /// </summary>
        private void RefreshUI()
        {
            if (GameManager.Instance != null && GameManager.Instance.ui != null)
                GameManager.Instance.ui.UpdateHearts(Current, maxHearts);
        }
    }
}
