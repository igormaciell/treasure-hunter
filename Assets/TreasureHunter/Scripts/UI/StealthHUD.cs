using UnityEngine;
using UnityEngine.UI;

namespace TreasureHunter
{
    /// <summary>
    /// HUD da furtividade (Nível 4): olho de visibilidade (escondido / nas sombras / visível / iluminado),
    /// postura, tocha, medidor de ruído, lugar atual (esconderijo, área segura), estado de cada ogro,
    /// setas apontando para ogros alertados, aviso de perigo com vinheta vermelha e batimento cardíaco.
    /// </summary>
    public class StealthHUD : MonoBehaviour
    {
        /// <summary>Instância ativa.</summary>
        public static StealthHUD Instance { get; private set; }

        [Header("Fontes")]
        public PlayerStealth stealth;
        public StealthTorch torch;

        [Header("Visibilidade")]
        public Image eyeIcon;
        public Sprite eyeClosed;
        public Sprite eyeHalf;
        public Sprite eyeOpen;
        public RectTransform visibilityFill;
        public Image visibilityFillImage;
        public Text visibilityText;
        public Text stanceText;
        public Text spotText;

        [Header("Tocha e ruído")]
        public Image torchIcon;
        public Sprite torchOn;
        public Sprite torchOff;
        public Text torchText;
        public RectTransform noiseFill;
        public Image noiseFillImage;
        public Text noiseText;

        [Header("Ogros e perigo")]
        public Text ogreList;
        public CanvasGroup alertGroup;
        public Text alertText;
        public Image vignette;
        public RectTransform[] arrows;
        public float arrowRadius = 210f;
        public AudioSource heartbeat;

        [Header("Dicas")]
        public CanvasGroup hintGroup;
        public Text hintText;

        private float noiseLevel;
        private float hintUntil;
        private float danger;
        private float lastNoiseTimeSeen = -100f;
        private Image[] arrowImages;

        /// <summary>Registra o singleton.</summary>
        private void Awake()
        {
            Instance = this;
            if (arrows != null)
            {
                arrowImages = new Image[arrows.Length];
                for (int i = 0; i < arrows.Length; i++) arrowImages[i] = arrows[i] != null ? arrows[i].GetComponent<Image>() : null;
            }
        }

        /// <summary>Libera o singleton.</summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Mostra uma dica curta.
        /// </summary>
        /// <param name="text">Texto.</param>
        public void ShowHint(string text)
        {
            if (hintText == null) return;
            hintText.text = text;
            hintUntil = Time.unscaledTime + 3.2f;
        }

        /// <summary>
        /// Atualiza todos os indicadores.
        /// </summary>
        private void Update()
        {
            if (stealth == null) stealth = PlayerStealth.Instance;
            if (stealth == null) return;
            UpdateVisibility();
            UpdateTorchAndNoise();
            UpdateOgres();
            if (hintGroup != null)
                hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, Time.unscaledTime < hintUntil ? 1f : 0f, Time.unscaledDeltaTime * 3f);
        }

        /// <summary>
        /// Olho, barra de visibilidade, postura e lugar atual.
        /// </summary>
        private void UpdateVisibility()
        {
            float v = Mathf.Clamp01(stealth.VisibilityFactor / 1.1f);
            string label;
            Color color;
            Sprite eye;
            if (stealth.InSafeZone) { label = "SEGURO"; color = new Color(0.45f, 0.9f, 1f); eye = eyeClosed; }
            else if (stealth.IsHidden || v < 0.08f) { label = "ESCONDIDO"; color = new Color(0.45f, 1f, 0.55f); eye = eyeClosed; }
            else if (v < 0.3f) { label = "NAS SOMBRAS"; color = new Color(0.75f, 0.95f, 0.5f); eye = eyeHalf; }
            else if (v < 0.65f) { label = "VISÍVEL"; color = new Color(1f, 0.8f, 0.3f); eye = eyeOpen; }
            else { label = "ILUMINADO"; color = new Color(1f, 0.4f, 0.25f); eye = eyeOpen; }
            if (visibilityText != null) { visibilityText.text = label; visibilityText.color = color; }
            if (eyeIcon != null) { if (eye != null) eyeIcon.sprite = eye; eyeIcon.color = color; }
            if (visibilityFill != null) visibilityFill.localScale = new Vector3(Mathf.Max(0.02f, v), 1f, 1f);
            if (visibilityFillImage != null) visibilityFillImage.color = color;
            if (stanceText != null)
                stanceText.text = stealth.IsCrouched ? "Agachado  <color=#9a9a9a>[C / Ctrl]</color>" : "De pé  <color=#9a9a9a>[C] agachar</color>";
            if (spotText != null) spotText.text = stealth.CurrentSpotLabel;
        }

        /// <summary>
        /// Tocha e medidor de ruído (o último ruído de John, que vai sumindo).
        /// </summary>
        private void UpdateTorchAndNoise()
        {
            bool lit = torch != null && torch.Lit;
            if (torchIcon != null)
            {
                Sprite s = lit ? torchOn : torchOff;
                if (s != null) torchIcon.sprite = s;
                torchIcon.color = lit ? new Color(1f, 0.75f, 0.35f) : new Color(0.6f, 0.6f, 0.65f);
            }
            if (torchText != null) torchText.text = lit ? "Tocha ACESA  <color=#9a9a9a>[F]</color>" : "Tocha apagada  <color=#9a9a9a>[F]</color>";

            if (NoiseSystem.LastPlayerNoiseTime > lastNoiseTimeSeen)
            {
                lastNoiseTimeSeen = NoiseSystem.LastPlayerNoiseTime;
                float n = Mathf.Clamp01(NoiseSystem.LastPlayerNoise.radius / 12f);
                if (NoiseSystem.LastPlayerNoise.type == NoiseType.Gunshot) n = 1f;
                noiseLevel = Mathf.Max(noiseLevel, n);
            }
            noiseLevel = Mathf.MoveTowards(noiseLevel, 0f, Time.deltaTime * 0.8f);
            if (noiseFill != null) noiseFill.localScale = new Vector3(Mathf.Max(0.02f, noiseLevel), 1f, 1f);
            Color nc = noiseLevel < 0.2f ? new Color(0.5f, 0.9f, 0.6f) : noiseLevel < 0.55f ? new Color(1f, 0.8f, 0.3f) : new Color(1f, 0.35f, 0.25f);
            if (noiseFillImage != null) noiseFillImage.color = nc;
            if (noiseText != null)
                noiseText.text = noiseLevel < 0.08f ? "Ruído: silêncio" : noiseLevel < 0.2f ? "Ruído: baixo" : noiseLevel < 0.55f ? "Ruído: médio" : "Ruído: ALTO";
        }

        /// <summary>
        /// Lista dos ogros, aviso de perigo, vinheta, setas e batimento.
        /// </summary>
        private void UpdateOgres()
        {
            float target = 0f;
            string worst = "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int arrowIndex = 0;
            Camera cam = Camera.main;
            foreach (OgreAI o in OgreAI.All)
            {
                if (o == null || o.Dormant) continue;
                OgreAI.State s = o.CurrentState;
                float d = StealthUtil.FlatDistance(o.transform.position, stealth.transform.position);
                string col = "#cfcfcf";
                float level = o.Awareness * 0.45f;
                if (s == OgreAI.State.Chasing || s == OgreAI.State.Attacking || s == OgreAI.State.Staggered) { level = 1f; col = "#ff5a45"; }
                else if (s == OgreAI.State.Suspicious) { level = 0.55f + o.Awareness * 0.3f; col = "#ffd84a"; }
                else if (s == OgreAI.State.Investigating) { level = 0.5f; col = "#ffa040"; }
                level *= Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((d - 8f) / 25f));
                if (level > target)
                {
                    target = level;
                    worst = s == OgreAI.State.Chasing || s == OgreAI.State.Attacking || s == OgreAI.State.Staggered ? o.displayName + " te viu! CORRA e se esconda!"
                          : s == OgreAI.State.Suspicious ? o.displayName + " desconfiou de algo... fique imóvel."
                          : s == OgreAI.State.Investigating ? o.displayName + " está procurando. Saia de perto."
                          : "";
                }
                sb.Append("<color=").Append(col).Append('>').Append(o.displayName).Append(" — ").Append(OgreAI.StateName(s))
                  .Append("</color>  <color=#8a8a8a>").Append(Mathf.RoundToInt(d)).Append(" m</color>\n");

                // Setas para ogros alertados.
                bool alerted = s != OgreAI.State.Patrolling && s != OgreAI.State.Returning;
                if (alerted && arrows != null && arrowIndex < arrows.Length && arrows[arrowIndex] != null && cam != null && d < 30f)
                {
                    Vector3 to = o.transform.position - cam.transform.position;
                    Vector3 f = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                    Vector3 r = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
                    Vector2 dir2 = new Vector2(Vector3.Dot(to, r), Vector3.Dot(to, f));
                    if (dir2.sqrMagnitude > 0.01f)
                    {
                        dir2.Normalize();
                        RectTransform a = arrows[arrowIndex];
                        a.gameObject.SetActive(true);
                        a.anchoredPosition = dir2 * arrowRadius;
                        a.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir2.y, dir2.x) * Mathf.Rad2Deg - 90f);
                        if (arrowImages != null && arrowImages[arrowIndex] != null)
                        {
                            ColorUtility.TryParseHtmlString(col, out Color c);
                            arrowImages[arrowIndex].color = c;
                        }
                        arrowIndex++;
                    }
                }
            }
            if (arrows != null)
                for (int i = arrowIndex; i < arrows.Length; i++) if (arrows[i] != null && arrows[i].gameObject.activeSelf) arrows[i].gameObject.SetActive(false);
            if (ogreList != null) ogreList.text = sb.ToString();

            if (stealth.IsDead) { target = 1f; worst = "Um golpe de ogro é fatal..."; }
            danger = Mathf.MoveTowards(danger, target, Time.deltaTime * (target > danger ? 2.5f : 0.6f));
            if (alertGroup != null) alertGroup.alpha = Mathf.MoveTowards(alertGroup.alpha, string.IsNullOrEmpty(worst) ? 0f : 1f, Time.deltaTime * 4f);
            if (alertText != null && !string.IsNullOrEmpty(worst))
            {
                alertText.text = worst;
                alertText.color = target >= 0.95f ? new Color(1f, 0.35f, 0.25f) : new Color(1f, 0.8f, 0.35f);
            }
            if (vignette != null)
            {
                float pulse = danger >= 0.9f ? 0.12f * Mathf.Sin(Time.time * 9f) : 0f;
                Color vc = danger >= 0.9f ? new Color(0.75f, 0.03f, 0.02f) : new Color(0.55f, 0.35f, 0.02f);
                vc.a = Mathf.Clamp01(danger * 0.55f + pulse);
                vignette.color = vc;
            }
            if (heartbeat != null)
            {
                heartbeat.volume = Mathf.Clamp01((danger - 0.25f) * 1.2f);
                heartbeat.pitch = 0.9f + danger * 0.45f;
                if (heartbeat.volume > 0.01f && !heartbeat.isPlaying) heartbeat.Play();
                else if (heartbeat.volume <= 0.01f && heartbeat.isPlaying) heartbeat.Stop();
            }
        }
    }
}
