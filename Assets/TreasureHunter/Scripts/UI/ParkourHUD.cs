using UnityEngine;
using UnityEngine.UI;

namespace TreasureHunter
{
    /// <summary>
    /// HUD do parkour (Nível 3): situação do totem, mensagem de posição válida/inválida enquanto T está pressionado,
    /// checkpoint ativo, quedas no abismo, aviso de queda e dicas rápidas.
    /// </summary>
    public class ParkourHUD : MonoBehaviour
    {
        /// <summary>Instância da cena.</summary>
        public static ParkourHUD Instance { get; private set; }

        public TotemAbility totem;
        public ParkourController parkour;

        [Header("Totem")]
        public GameObject totemPanel;
        public Image totemIcon;
        public Text totemStatus;
        public Text placementText;
        public Color validColor = new Color(0.45f, 1f, 0.55f);
        public Color invalidColor = new Color(1f, 0.45f, 0.4f);

        [Header("Checkpoint e quedas")]
        public Text checkpointText;
        public Text fallsText;
        public CanvasGroup voidWarning;

        [Header("Dicas")]
        public CanvasGroup hintGroup;
        public Text hintText;

        private float hintUntil;

        /// <summary>Registra a instância e esconde os avisos.</summary>
        private void Awake()
        {
            Instance = this;
            if (hintGroup != null) hintGroup.alpha = 0f;
            if (voidWarning != null) voidWarning.alpha = 0f;
        }

        /// <summary>Encontra o jogador se as referências estiverem vazias.</summary>
        private void Start()
        {
            if (parkour == null) parkour = ParkourController.Instance;
            if (totem == null && parkour != null) totem = parkour.GetComponent<TotemAbility>();
        }

        /// <summary>Libera a instância.</summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Atualiza os textos e os avisos.</summary>
        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool hasTotem = totem != null && totem.Unlocked;
            if (totemPanel != null && totemPanel.activeSelf != hasTotem) totemPanel.SetActive(hasTotem);
            if (hasTotem && totemStatus != null)
            {
                totemStatus.text = totem.Totem != null && totem.Totem.Placed
                    ? (ReferenceEquals(ParkourCheckpoints.Current, totem.Totem) ? "Totem: checkpoint ativo\n[T] segure para mover" : "Totem colocado\n[T] segure para mover")
                    : "Totem pronto\n[T] segure para colocar";
            }
            if (totemIcon != null && hasTotem)
                totemIcon.color = totem.Aiming ? (totem.CandidateValid ? validColor : invalidColor) : Color.white;

            if (placementText != null)
            {
                bool show = hasTotem && totem.Aiming;
                if (placementText.gameObject.activeSelf != show) placementText.gameObject.SetActive(show);
                if (show)
                {
                    placementText.text = (totem.CandidateValid ? "[OK] " : "[X] ") + totem.CandidateMessage + "\n<size=20>Botão direito: cancelar</size>";
                    placementText.color = totem.CandidateValid ? validColor : invalidColor;
                }
            }

            if (checkpointText != null) checkpointText.text = "Checkpoint: " + ParkourCheckpoints.Label;
            if (fallsText != null) fallsText.text = "Quedas no abismo: " + VoidZone.Falls;
            if (voidWarning != null)
            {
                bool falling = parkour != null && parkour.FallingIntoVoid;
                voidWarning.alpha = Mathf.MoveTowards(voidWarning.alpha, falling ? 1f : 0f, dt * 5f);
            }
            if (hintGroup != null)
                hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, Time.unscaledTime < hintUntil ? 1f : 0f, dt * 4f);
        }

        /// <summary>
        /// Mostra uma dica curta por alguns segundos.
        /// </summary>
        /// <param name="text">Texto.</param>
        public void ShowHint(string text)
        {
            if (hintText != null) hintText.text = text;
            hintUntil = Time.unscaledTime + 3.2f;
        }
    }
}
