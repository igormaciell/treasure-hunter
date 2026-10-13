using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// A tocha que acompanha John (ferramenta do Nível 1). Ilumina o caminho e, quando erguida
    /// (F ou botão esquerdo do mouse), aumenta o alcance da luz e revela inscrições escondidas
    /// (TorchRevealable) dentro do raio de revelação.
    /// </summary>
    public class TorchTool : MonoBehaviour
    {
        /// <summary>Luz pontual da chama.</summary>
        public Light torchLight;
        /// <summary>Transform do braço que segura a tocha (sobe quando erguida).</summary>
        public Transform armPivot;
        /// <summary>Visual da tocha (bastão + chama), mantido sempre de pé independentemente do braço.</summary>
        public Transform torchVisual;
        /// <summary>Partículas de fogo da tocha.</summary>
        public ParticleSystem flame;

        [Header("Luz")]
        public float lowRange = 7f;
        public float lowIntensity = 2.2f;
        public float raisedRange = 12f;
        public float raisedIntensity = 4.5f;
        public float revealRadius = 5.5f;

        [Header("Braço")]
        public float loweredAngle = -35f;
        public float raisedAngle = -150f;

        /// <summary>Indica se a tocha está erguida.</summary>
        public bool Raised { get; private set; }

        /// <summary>Nome da ferramenta exibido no HUD.</summary>
        public string toolName = "Tocha";

        private float flicker;
        private float armAngle = -35f;

        /// <summary>
        /// Mostra a ferramenta no HUD ao começar.
        /// </summary>
        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.ui != null)
                GameManager.Instance.ui.SetTool(toolName, false);
        }

        /// <summary>
        /// Trata a tecla da ferramenta, anima a chama/luz e revela objetos próximos.
        /// </summary>
        private void Update()
        {
            bool canAct = GameManager.Instance == null || GameManager.Instance.CanPlayerAct();
            if (canAct && GameInput.ToolPressed()) SetRaised(!Raised);
            if (canAct && GameInput.SwitchToolPressed() && GameManager.Instance != null && GameManager.Instance.ui != null)
                GameManager.Instance.ui.ShowTutorial("A tocha é a única ferramenta por enquanto. Novas ferramentas surgirão nos próximos níveis.");

            AnimateLight();
            if (Raised) RevealNearby();
        }

        /// <summary>
        /// Posiciona o braço depois do Animator (LateUpdate), para a pose da tocha prevalecer.
        /// </summary>
        private void LateUpdate()
        {
            AnimateArm();
        }

        /// <summary>
        /// Ergue ou abaixa a tocha.
        /// </summary>
        /// <param name="raised">true para erguer.</param>
        public void SetRaised(bool raised)
        {
            Raised = raised;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.torchWhoosh, transform.position, 0.5f);
            if (GameManager.Instance != null && GameManager.Instance.ui != null)
                GameManager.Instance.ui.SetTool(toolName, raised);
        }

        /// <summary>
        /// Faz a luz tremeluzir usando ruído Perlin e interpolar entre o alcance baixo e o erguido.
        /// </summary>
        private void AnimateLight()
        {
            if (torchLight == null) return;
            flicker += Time.deltaTime * 7f;
            float noise = Mathf.PerlinNoise(flicker, 0.37f) * 0.35f + 0.82f;
            float targetRange = Raised ? raisedRange : lowRange;
            float targetIntensity = (Raised ? raisedIntensity : lowIntensity) * noise;
            torchLight.range = Mathf.Lerp(torchLight.range, targetRange, Time.deltaTime * 5f);
            torchLight.intensity = Mathf.Lerp(torchLight.intensity, targetIntensity, Time.deltaTime * 10f);
        }

        /// <summary>
        /// Gira o braço para cima/baixo, suavemente, conforme o estado da tocha.
        /// </summary>
        private void AnimateArm()
        {
            if (armPivot == null) return;
            float target = Raised ? raisedAngle : loweredAngle;
            // Interpola o ângulo guardado e aplica, sobrescrevendo a pose do Animator neste braço.
            armAngle = Mathf.Lerp(armAngle, target, Time.deltaTime * 10f);
            armPivot.localRotation = Quaternion.Euler(armAngle, 0f, 0f);

            // Mantém a tocha na vertical (chama para cima), girando apenas junto com o corpo.
            if (torchVisual != null && armPivot.parent != null)
            {
                Vector3 flatForward = Vector3.ProjectOnPlane(armPivot.parent.forward, Vector3.up);
                if (flatForward.sqrMagnitude > 0.001f)
                    torchVisual.rotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
            }
        }

        /// <summary>
        /// Revela todos os objetos escondidos dentro do raio da luz erguida.
        /// </summary>
        private void RevealNearby()
        {
            Vector3 origin = torchLight != null ? torchLight.transform.position : transform.position;
            float r2 = revealRadius * revealRadius;
            // Copia para um vetor, pois revelar pode alterar a lista global.
            TorchRevealable[] list = TorchRevealable.All.ToArray();
            foreach (TorchRevealable t in list)
            {
                if (t == null || t.Revealed) continue;
                if ((t.transform.position - origin).sqrMagnitude <= r2) t.Reveal();
            }
        }
    }
}
