using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Placa de pressão no chão. Só fica pressionada enquanto um objeto com a tag "Pushable"
    /// (caixote) estiver REALMENTE em cima dela: o centro do caixote precisa estar sobre a placa,
    /// não basta encostar na borda. Ao tirar o caixote, a placa sobe e os mecanismos ligados
    /// (portas, energia do enigma) desligam de novo.
    /// O peso do jogador sozinho não é suficiente (opção acceptPlayer desligada por padrão).
    /// </summary>
    public class PressurePlate : MonoBehaviour
    {
        /// <summary>Parte visual que afunda quando pressionada.</summary>
        public Transform plateVisual;
        /// <summary>Mecanismos que recebem o sinal.</summary>
        public SignalReceiver[] targets;
        /// <summary>Profundidade que a placa afunda.</summary>
        public float pressDepth = 0.08f;
        /// <summary>Distância máxima (em X e em Z, metros) entre o centro do caixote e o centro da placa.</summary>
        public float centerTolerance = 0.5f;
        /// <summary>Se verdadeiro, o jogador também ativa a placa.</summary>
        public bool acceptPlayer = false;
        /// <summary>Luz que acende quando pressionada (opcional).</summary>
        public Light indicator;
        /// <summary>Habilidade desbloqueada na primeira ativação (opcional).</summary>
        public string skillOnPress = "";
        /// <summary>Mensagem de John na primeira ativação (opcional).</summary>
        public string lineOnPress = "";

        /// <summary>Indica se a placa está pressionada.</summary>
        public bool IsPressed { get; private set; }

        // Objetos aceitos que estão perto da placa (dentro do gatilho); o centro de cada um é conferido a cada passo.
        private readonly HashSet<Collider> nearby = new HashSet<Collider>();
        private Vector3 visualStart;
        private bool firstPress = true;

        /// <summary>
        /// Guarda a posição inicial da parte visual.
        /// </summary>
        private void Start()
        {
            if (plateVisual != null) visualStart = plateVisual.localPosition;
            if (indicator != null) indicator.enabled = false;
        }

        /// <summary>
        /// A cada passo de física confere se algum caixote está centralizado sobre a placa.
        /// </summary>
        private void FixedUpdate()
        {
            // Objetos destruídos ou desligados não disparam OnTriggerExit; limpa-os aqui.
            nearby.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
            SetPressed(AnyCentered());
        }

        /// <summary>
        /// Anima a placa afundando/subindo.
        /// </summary>
        private void Update()
        {
            if (plateVisual == null) return;
            Vector3 target = visualStart + Vector3.down * (IsPressed ? pressDepth : 0f);
            plateVisual.localPosition = Vector3.MoveTowards(plateVisual.localPosition, target, Time.deltaTime * 0.5f);
        }

        /// <summary>
        /// Registra um objeto aceito (pela Tag) que chegou perto da placa.
        /// </summary>
        /// <param name="other">Colisor que entrou no gatilho.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (Accepts(other)) nearby.Add(other);
        }

        /// <summary>
        /// Esquece o objeto que se afastou da placa.
        /// </summary>
        /// <param name="other">Colisor que saiu do gatilho.</param>
        private void OnTriggerExit(Collider other)
        {
            nearby.Remove(other);
        }

        /// <summary>
        /// Decide, pela Tag, se um colisor é capaz de pressionar a placa.
        /// </summary>
        /// <param name="other">Colisor a avaliar.</param>
        /// <returns>true se for um caixote (ou o jogador, quando permitido).</returns>
        private bool Accepts(Collider other)
        {
            if (other.CompareTag(GameTags.Pushable)) return true;
            return acceptPlayer && other.CompareTag(GameTags.Player);
        }

        /// <summary>
        /// Verifica se algum objeto próximo tem o centro sobre a placa.
        /// Usa uma pequena folga extra para soltar, evitando que a placa fique piscando na borda.
        /// </summary>
        /// <returns>true se algum objeto está centralizado sobre a placa.</returns>
        private bool AnyCentered()
        {
            float tolerance = IsPressed ? centerTolerance + 0.1f : centerTolerance;
            foreach (Collider c in nearby)
            {
                // Posição do centro do objeto no espaço da placa (desconsiderando a altura).
                Vector3 local = transform.InverseTransformPoint(c.bounds.center);
                if (Mathf.Abs(local.x) <= tolerance && Mathf.Abs(local.z) <= tolerance) return true;
            }
            return false;
        }

        /// <summary>
        /// Atualiza o estado pressionado e envia o sinal aos mecanismos quando ele muda.
        /// </summary>
        /// <param name="pressed">Novo estado.</param>
        private void SetPressed(bool pressed)
        {
            if (pressed == IsPressed) return;
            IsPressed = pressed;

            SignalReceiver.Broadcast(targets, pressed, this);
            if (indicator != null) indicator.enabled = pressed;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.plateClick, transform.position);

            if (pressed && firstPress)
            {
                firstPress = false;
                if (!string.IsNullOrEmpty(skillOnPress) && SkillManager.Instance != null) SkillManager.Instance.Unlock(skillOnPress);
                if (!string.IsNullOrEmpty(lineOnPress) && GameManager.Instance != null && GameManager.Instance.ui != null)
                    GameManager.Instance.ui.ShowLine(lineOnPress);
            }
        }
    }
}
