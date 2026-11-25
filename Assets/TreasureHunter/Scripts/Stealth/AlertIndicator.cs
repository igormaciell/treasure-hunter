using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Indicador sobre a cabeça do ogro: "?" amarelo (desconfiado), "?" laranja (investigando), "!" vermelho
    /// (perseguindo) e uma barra que enche conforme ele vai percebendo John. Sempre virado para a câmera.
    /// </summary>
    public class AlertIndicator : MonoBehaviour
    {
        /// <summary>O que mostrar.</summary>
        public enum Mode { Hidden, Watching, Suspicious, Searching, Alarmed }

        public Renderer question;
        public Renderer exclamation;
        public Renderer barBack;
        public Renderer barFill;
        /// <summary>Pivô da barra (escala em X = preenchimento).</summary>
        public Transform fillPivot;
        public Color watchColor = new Color(1f, 1f, 1f, 0.85f);
        public Color suspiciousColor = new Color(1f, 0.85f, 0.2f, 1f);
        public Color searchingColor = new Color(1f, 0.55f, 0.15f, 1f);
        public Color alarmedColor = new Color(1f, 0.18f, 0.12f, 1f);

        /// <summary>Modo atual.</summary>
        public Mode Current { get; private set; } = Mode.Hidden;

        private MaterialPropertyBlock block;
        private float fill;
        private Vector3 baseLocal;
        private float pulse;
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>Guarda a posição base.</summary>
        private void Awake()
        {
            block = new MaterialPropertyBlock();
            baseLocal = transform.localPosition;
            Apply();
        }

        /// <summary>
        /// Muda o modo e o preenchimento.
        /// </summary>
        /// <param name="mode">Modo.</param>
        /// <param name="awareness">Atenção (0 a 1).</param>
        public void Set(Mode mode, float awareness)
        {
            fill = Mathf.Clamp01(awareness);
            if (mode != Current)
            {
                if (mode == Mode.Alarmed || mode == Mode.Suspicious) pulse = 1f;
                Current = mode;
                Apply();
            }
        }

        /// <summary>
        /// Liga/desliga as partes e aplica as cores.
        /// </summary>
        private void Apply()
        {
            bool showQ = Current == Mode.Suspicious || Current == Mode.Searching;
            bool showE = Current == Mode.Alarmed;
            bool showBar = Current == Mode.Watching || Current == Mode.Suspicious;
            if (question != null) question.enabled = showQ;
            if (exclamation != null) exclamation.enabled = showE;
            if (barBack != null) barBack.enabled = showBar;
            if (barFill != null) barFill.enabled = showBar;
            Color c = Current == Mode.Alarmed ? alarmedColor : Current == Mode.Searching ? searchingColor : Current == Mode.Suspicious ? suspiciousColor : watchColor;
            Tint(question, c);
            Tint(exclamation, c);
            Tint(barFill, c);
            Tint(barBack, new Color(0f, 0f, 0f, 0.6f));
        }

        /// <summary>
        /// Cor por MaterialPropertyBlock (sem criar materiais).
        /// </summary>
        /// <param name="r">Renderizador.</param>
        /// <param name="c">Cor.</param>
        private void Tint(Renderer r, Color c)
        {
            if (r == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            block.SetColor(ColorId, c);
            r.SetPropertyBlock(block);
        }

        /// <summary>
        /// Vira para a câmera, balança e pulsa ao mudar de estado.
        /// </summary>
        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 dir = transform.position - cam.transform.position;
                if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
            pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * 2.5f);
            float s = 1f + pulse * 0.6f;
            transform.localScale = Vector3.one * s;
            // Flutua sobre a cabeça: volta à posição base e sobe/desce com Translate (eixo vertical do mundo).
            transform.localPosition = baseLocal;
            transform.Translate(Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.06f), Space.World);
            if (fillPivot != null) fillPivot.localScale = new Vector3(Mathf.Max(0.001f, fill), 1f, 1f);
        }
    }
}
