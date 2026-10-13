using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Objeto escondido (símbolo, inscrição, marca na parede) que aparece quando a luz
    /// da tocha erguida chega perto, ou quando recebe sinal dos braseiros acesos.
    /// Implementa o "enigma de luz e sombra" do GDD.
    /// </summary>
    public class TorchRevealable : SignalReceiver
    {
        /// <summary>Lista global usada pela tocha para procurar objetos próximos.</summary>
        public static readonly List<TorchRevealable> All = new List<TorchRevealable>();

        /// <summary>Renderizadores que ficam invisíveis até a revelação.</summary>
        public Renderer[] targets;
        /// <summary>Velocidade do efeito de aparecer (escala).</summary>
        public float revealSpeed = 2.5f;
        /// <summary>Habilidade desbloqueada quando o objeto é revelado pela primeira vez (opcional).</summary>
        public string skillOnReveal = "";

        /// <summary>Indica se o objeto já foi revelado.</summary>
        public bool Revealed { get; private set; }

        private float progress;
        private Vector3[] baseScales;

        /// <summary>
        /// Registra o objeto na lista global.
        /// </summary>
        private void OnEnable()
        {
            All.Add(this);
        }

        /// <summary>
        /// Remove o objeto da lista global.
        /// </summary>
        private void OnDisable()
        {
            All.Remove(this);
        }

        /// <summary>
        /// Guarda as escalas originais e esconde os renderizadores.
        /// </summary>
        private void Start()
        {
            if (targets == null) targets = new Renderer[0];
            baseScales = new Vector3[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                baseScales[i] = targets[i].transform.localScale;
                targets[i].enabled = Revealed;
            }
        }

        /// <summary>
        /// Anima o surgimento (escala de 0 até o tamanho original) depois de revelado.
        /// </summary>
        private void Update()
        {
            if (!Revealed || progress >= 1f) return;
            progress = Mathf.Min(1f, progress + Time.deltaTime * revealSpeed);
            float s = Mathf.SmoothStep(0f, 1f, progress);
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null) targets[i].transform.localScale = baseScales[i] * s;
            }
        }

        /// <summary>
        /// Revela o objeto (chamado pela tocha ou por sinal).
        /// </summary>
        public void Reveal()
        {
            if (Revealed) return;
            Revealed = true;
            progress = 0f;
            foreach (Renderer r in targets)
            {
                if (r == null) continue;
                r.enabled = true;
                r.transform.localScale = Vector3.zero;
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.torchWhoosh, transform.position, 0.7f);
            if (!string.IsNullOrEmpty(skillOnReveal) && SkillManager.Instance != null) SkillManager.Instance.Unlock(skillOnReveal);
        }

        /// <summary>
        /// Ao receber sinal (por exemplo, braseiros acesos), revela o objeto.
        /// </summary>
        /// <param name="on">Novo estado do receptor.</param>
        protected override void OnStateChanged(bool on)
        {
            if (on) Reveal();
        }
    }
}
