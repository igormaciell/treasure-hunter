using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Enigma de sequência/memória: a parede mostra (quando iluminada) uma ordem de símbolos
    /// e o jogador precisa pressioná-los na mesma ordem nos botões do mecanismo.
    /// A ordem é sorteada a cada partida embaralhando um vetor de símbolos (posição aleatória no vetor).
    /// Errar apenas reinicia o mecanismo, sem punição, como pede o GDD.
    /// Pode exigir energia (sinal de uma placa de pressão) antes de aceitar entradas.
    /// </summary>
    public class SymbolSequencePuzzle : SignalReceiver
    {
        /// <summary>Materiais dos símbolos apagados (índice = id do símbolo).</summary>
        [Header("Símbolos")]
        public Material[] glyphMaterials;
        /// <summary>Materiais dos símbolos acesos (mesma ordem de glyphMaterials).</summary>
        public Material[] glyphLitMaterials;

        /// <summary>Botões que o jogador pressiona.</summary>
        [Header("Peças")]
        public SymbolButton[] buttons;
        /// <summary>Renderizadores da parede que mostram a sequência correta (um por posição).</summary>
        public Renderer[] displaySlots;

        /// <summary>Tamanho da sequência.</summary>
        [Header("Regras")]
        public int sequenceLength = 4;
        /// <summary>Sorteia a sequência e a ordem dos botões ao iniciar.</summary>
        public bool randomize = true;
        /// <summary>Receptores avisados quando o enigma é resolvido (porta).</summary>
        public SignalReceiver[] targets;
        /// <summary>Habilidade desbloqueada ao resolver.</summary>
        public string skillOnSolve = "memoria";
        /// <summary>Frase de John ao resolver.</summary>
        public string lineOnSolve = "";
        /// <summary>Mensagem mostrada ao tentar usar o mecanismo sem energia.</summary>
        public string unpoweredMessage = "O mecanismo está travado. Algo precisa pressionar a placa.";

        /// <summary>Indica se o enigma já foi resolvido.</summary>
        public bool Solved { get; private set; }
        /// <summary>Indica se o mecanismo está energizado e aceita entradas.</summary>
        public bool Powered => requiredSignals <= 0 || IsOn;
        /// <summary>Mensagem para o jogador quando o mecanismo está sem energia.</summary>
        public string UnpoweredMessage => unpoweredMessage;

        private int[] solution;
        private int progress;

        /// <summary>
        /// Gera a solução, distribui os símbolos pelos botões e escreve a sequência na parede.
        /// </summary>
        private void Start()
        {
            int symbolCount = glyphMaterials != null ? glyphMaterials.Length : 0;
            if (symbolCount == 0 || buttons == null || buttons.Length == 0) return;

            // Vetor de símbolos 0..n-1, embaralhado se a randomização estiver ligada.
            int[] symbols = new int[symbolCount];
            for (int i = 0; i < symbolCount; i++) symbols[i] = i;
            if (randomize) Shuffle(symbols);

            // A solução usa os primeiros "sequenceLength" símbolos do vetor embaralhado.
            int length = Mathf.Min(sequenceLength, symbolCount, buttons.Length);
            solution = new int[length];
            for (int i = 0; i < length; i++) solution[i] = symbols[i];

            // Os botões recebem os símbolos da solução em outra ordem aleatória.
            int[] buttonSymbols = (int[])solution.Clone();
            if (randomize) Shuffle(buttonSymbols);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                int symbol = buttonSymbols[i % buttonSymbols.Length];
                buttons[i].Setup(this, symbol, glyphMaterials[symbol]);
            }

            // A parede mostra a ordem correta.
            if (displaySlots != null)
            {
                for (int i = 0; i < displaySlots.Length && i < solution.Length; i++)
                {
                    if (displaySlots[i] != null) displaySlots[i].sharedMaterial = glyphLitMaterials[solution[i]];
                }
            }
        }

        /// <summary>
        /// Embaralha um vetor de inteiros (algoritmo de Fisher-Yates com Random.Range).
        /// </summary>
        /// <param name="array">Vetor que será embaralhado no próprio lugar.</param>
        public static void Shuffle(int[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = array[i];
                array[i] = array[j];
                array[j] = tmp;
            }
        }

        /// <summary>
        /// Recebe o símbolo pressionado pelo jogador e verifica se é o próximo da sequência.
        /// </summary>
        /// <param name="button">Botão pressionado.</param>
        public void Press(SymbolButton button)
        {
            if (Solved || solution == null) return;
            AudioManager am = AudioManager.Instance;

            if (button.Symbol == solution[progress])
            {
                // Acerto: acende o botão e avança.
                button.SetLit(true, glyphLitMaterials[button.Symbol]);
                progress++;
                if (am != null) am.PlayAt(am.buttonPress, button.transform.position);
                if (progress >= solution.Length) Solve();
            }
            else
            {
                // Erro: o mecanismo apenas reinicia.
                if (am != null) am.PlayAt(am.puzzleWrong, button.transform.position);
                ResetProgress();
                if (GameManager.Instance != null && GameManager.Instance.ui != null)
                    GameManager.Instance.ui.ShowLine("Hmm... não foi essa ordem. O mecanismo voltou ao início.");
            }
        }

        /// <summary>
        /// Apaga todos os botões e volta ao primeiro símbolo da sequência.
        /// </summary>
        private void ResetProgress()
        {
            progress = 0;
            foreach (SymbolButton b in buttons)
            {
                if (b != null) b.SetLit(false, glyphMaterials[b.Symbol]);
            }
        }

        /// <summary>
        /// Marca o enigma como resolvido e abre a passagem.
        /// </summary>
        private void Solve()
        {
            Solved = true;
            SignalReceiver.Broadcast(targets, true, this);
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(AudioManager.Instance.puzzleSolved);
            if (!string.IsNullOrEmpty(skillOnSolve) && SkillManager.Instance != null) SkillManager.Instance.Unlock(skillOnSolve);
            if (!string.IsNullOrEmpty(lineOnSolve) && GameManager.Instance != null && GameManager.Instance.ui != null)
                GameManager.Instance.ui.ShowLine(lineOnSolve);
        }

        /// <summary>
        /// Ao receber energia (placa pressionada), avisa o jogador.
        /// </summary>
        /// <param name="on">Novo estado da energia.</param>
        protected override void OnStateChanged(bool on)
        {
            if (on && GameManager.Instance != null && GameManager.Instance.ui != null)
                GameManager.Instance.ui.ShowLine("Ouvi um clique perto da porta... o mecanismo dos símbolos ganhou vida.");
            foreach (SymbolButton b in buttons)
            {
                if (b != null) b.SetPoweredLight(on);
            }
        }
    }
}
