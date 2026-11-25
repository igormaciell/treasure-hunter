using UnityEngine;
using UnityEngine.UI;

namespace TreasureHunter
{
    /// <summary>
    /// Interface de combate do Nível 2, somada ao HUD do Nível 1 (corações, missão, avisos):
    /// painel da arma (ícone, nome, munição no tambor | reserva, total de disparos restantes, tambor com
    /// 6 balas, pedras e teclas), barra de recarga, mira que abre ao atirar e fica vermelha sobre inimigos,
    /// marcador de acerto, contador de inimigos da sala e avisos curtos (ex.: "Tambor vazio!").
    /// </summary>
    public class CombatHUD : MonoBehaviour
    {
        /// <summary>Instância única acessível por qualquer script.</summary>
        public static CombatHUD Instance { get; private set; }

        /// <summary>Sistema de combate do jogador.</summary>
        public PlayerCombat combat;

        [Header("Painel da arma")]
        public GameObject weaponPanel;
        public Image weaponIcon;
        public Sprite revolverSprite;
        public Sprite knifeSprite;
        public Text weaponName;
        public Text ammoText;
        public Text totalText;
        public Text keysText;
        public Image[] cylinderSlots;
        public Sprite bulletFull;
        public Sprite bulletEmpty;
        public Text stonesText;

        [Header("Recarga")]
        public GameObject reloadGroup;
        public RectTransform reloadFill;

        [Header("Mira")]
        public RectTransform crosshair;
        /// <summary>Quatro traços da mira: cima, baixo, esquerda, direita.</summary>
        public RectTransform[] crosshairArms;
        public Image[] crosshairImages;
        public CanvasGroup hitMarker;
        public Image[] hitMarkerImages;
        public Text hitText;

        [Header("Inimigos")]
        public GameObject enemyPanel;
        public Text enemyText;

        [Header("Aviso")]
        public CanvasGroup hintGroup;
        public Text hintText;

        [Header("Cores")]
        public Color crosshairColor = new Color(1f, 1f, 1f, 0.9f);
        public Color crosshairEnemyColor = new Color(1f, 0.3f, 0.25f, 1f);
        public Color lowAmmoColor = new Color(1f, 0.45f, 0.35f);
        public Color ammoColor = new Color(1f, 0.82f, 0.35f);

        private float spread = 16f;
        private float kick;
        private float hitAlpha;
        private float hintUntil;
        private bool subscribed;

        /// <summary>
        /// Registra o singleton e esconde os elementos até serem necessários.
        /// </summary>
        private void Awake()
        {
            Instance = this;
            if (enemyPanel != null) enemyPanel.SetActive(false);
            if (reloadGroup != null) reloadGroup.SetActive(false);
            if (hintGroup != null) hintGroup.alpha = 0f;
            if (hitMarker != null) hitMarker.alpha = 0f;
        }

        /// <summary>
        /// Liga-se aos eventos do combate e desenha o estado inicial.
        /// </summary>
        private void Start()
        {
            if (combat == null) combat = PlayerCombat.Instance;
            Subscribe(true);
            Refresh();
        }

        /// <summary>
        /// Desliga-se dos eventos ao sair da cena.
        /// </summary>
        private void OnDestroy()
        {
            Subscribe(false);
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Inscreve ou cancela a inscrição nos eventos do PlayerCombat.
        /// </summary>
        /// <param name="on">true para inscrever.</param>
        private void Subscribe(bool on)
        {
            if (combat == null || subscribed == on) return;
            subscribed = on;
            if (on)
            {
                combat.Changed += Refresh;
                combat.HitConfirmed += OnHit;
                combat.Hint += ShowHint;
                combat.Fired += OnFired;
            }
            else
            {
                combat.Changed -= Refresh;
                combat.HitConfirmed -= OnHit;
                combat.Hint -= ShowHint;
                combat.Fired -= OnFired;
            }
        }

        /// <summary>
        /// Anima a mira, a barra de recarga, o marcador de acerto e os avisos (tempo real: funciona na pausa).
        /// </summary>
        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool armed = combat != null && combat.IsArmed;
            if (weaponPanel != null && weaponPanel.activeSelf != armed) weaponPanel.SetActive(armed);

            bool showCrosshair = armed && combat.Weapon == WeaponSlot.Revolver && combat.HasRevolver;
            if (crosshair != null && crosshair.gameObject.activeSelf != showCrosshair) crosshair.gameObject.SetActive(showCrosshair);
            if (showCrosshair) AnimateCrosshair(dt);

            if (reloadGroup != null)
            {
                bool reloading = combat != null && combat.IsReloading;
                if (reloadGroup.activeSelf != reloading) reloadGroup.SetActive(reloading);
                if (reloading && reloadFill != null) reloadFill.localScale = new Vector3(combat.ReloadProgress, 1f, 1f);
            }

            if (hitMarker != null)
            {
                hitAlpha = Mathf.MoveTowards(hitAlpha, 0f, dt * 3f);
                hitMarker.alpha = hitAlpha;
            }
            if (hintGroup != null)
                hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, Time.unscaledTime < hintUntil ? 1f : 0f, dt * 4f);
        }

        /// <summary>
        /// Abre a mira conforme movimento/disparo, fecha ao mirar e muda de cor sobre inimigos.
        /// </summary>
        /// <param name="dt">Tempo do quadro.</param>
        private void AnimateCrosshair(float dt)
        {
            float target = combat.IsAiming ? 7f : 16f;
            target += Mathf.Min(combat.MoveSpeed, 6f) * (combat.IsAiming ? 1.5f : 4f);
            kick = Mathf.MoveTowards(kick, 0f, dt * 60f);
            spread = Mathf.Lerp(spread, target + kick, 1f - Mathf.Exp(-14f * dt));
            if (crosshairArms != null && crosshairArms.Length >= 4)
            {
                if (crosshairArms[0] != null) crosshairArms[0].anchoredPosition = new Vector2(0f, spread);
                if (crosshairArms[1] != null) crosshairArms[1].anchoredPosition = new Vector2(0f, -spread);
                if (crosshairArms[2] != null) crosshairArms[2].anchoredPosition = new Vector2(-spread, 0f);
                if (crosshairArms[3] != null) crosshairArms[3].anchoredPosition = new Vector2(spread, 0f);
            }
            Color c = combat.IsAimingAtEnemy() ? crosshairEnemyColor : crosshairColor;
            if (crosshairImages != null) foreach (Image i in crosshairImages) if (i != null) i.color = c;
        }

        /// <summary>
        /// Redesenha o painel da arma (chamado quando munição, arma ou pedras mudam).
        /// </summary>
        public void Refresh()
        {
            if (combat == null) return;
            bool revolver = combat.Weapon == WeaponSlot.Revolver;
            if (weaponIcon != null) weaponIcon.sprite = revolver ? revolverSprite : knifeSprite;
            if (weaponName != null) weaponName.text = revolver ? "Revólver" : "Faca";

            if (ammoText != null)
            {
                ammoText.text = revolver ? combat.Cylinder + " | " + combat.Reserve : "—";
                ammoText.color = combat.ShotsLeft <= 3 ? lowAmmoColor : ammoColor;
            }
            if (totalText != null)
                totalText.text = "Disparos restantes: " + combat.ShotsLeft + " / " + combat.startingShots;

            if (cylinderSlots != null)
            {
                for (int i = 0; i < cylinderSlots.Length; i++)
                {
                    if (cylinderSlots[i] == null) continue;
                    cylinderSlots[i].sprite = i < combat.Cylinder ? bulletFull : bulletEmpty;
                    cylinderSlots[i].color = revolver ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                }
            }
            if (stonesText != null) stonesText.text = "Pedras: " + combat.Stones + " / " + combat.maxStones + "   [G] arremessar";
            if (keysText != null)
            {
                keysText.text = revolver
                    ? "Clique: atirar · Botão direito: mirar · [R] recarregar · [V] coronhada" + (combat.HasKnife ? " · [2] faca" : "")
                    : "Clique ou [V]: golpe de faca · [1] revólver";
            }
        }

        /// <summary>
        /// Mostra o contador de inimigos da sala.
        /// </summary>
        /// <param name="remaining">Inimigos restantes.</param>
        /// <param name="total">Total da sala.</param>
        public void ShowEnemies(int remaining, int total)
        {
            if (enemyPanel != null && !enemyPanel.activeSelf) enemyPanel.SetActive(true);
            if (enemyText != null) enemyText.text = "Inimigos: " + remaining + " / " + total;
        }

        /// <summary>
        /// Esconde o contador de inimigos.
        /// </summary>
        public void HideEnemies()
        {
            if (enemyPanel != null) enemyPanel.SetActive(false);
        }

        /// <summary>
        /// Mostra um aviso curto acima da mira.
        /// </summary>
        /// <param name="text">Texto do aviso.</param>
        public void ShowHint(string text)
        {
            if (hintText != null) hintText.text = text;
            hintUntil = Time.unscaledTime + 3f;
        }

        /// <summary>
        /// Marcador de acerto (branco) ou de inimigo derrotado (vermelho).
        /// </summary>
        /// <param name="killed">true se o inimigo morreu.</param>
        /// <param name="headshot">true se foi na cabeça.</param>
        private void OnHit(bool killed, bool headshot)
        {
            hitAlpha = 1f;
            Color c = killed ? new Color(1f, 0.25f, 0.2f) : Color.white;
            if (hitMarkerImages != null) foreach (Image i in hitMarkerImages) if (i != null) i.color = c;
            if (hitText != null) hitText.text = headshot ? "NA CABEÇA!" : (killed ? "DERROTADO" : "");
        }

        /// <summary>
        /// Abre a mira no momento do disparo.
        /// </summary>
        private void OnFired()
        {
            kick = 14f;
        }
    }
}
