using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Placa de pressão no chão. Fica pressionada enquanto um objeto com a tag "Pushable"
    /// (caixote) estiver sobre ela, enviando sinal aos mecanismos ligados.
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

        private readonly HashSet<Collider> objectsOnPlate = new HashSet<Collider>();
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
        /// Anima a placa afundando/subindo e remove da contagem objetos destruídos.
        /// </summary>
        private void Update()
        {
            // Objetos destruídos não disparam OnTriggerExit; limpa-os aqui.
            int removed = objectsOnPlate.RemoveWhere(c => c == null || !c.enabled);
            if (removed > 0) Evaluate();

            if (plateVisual != null)
            {
                Vector3 target = visualStart + Vector3.down * (IsPressed ? pressDepth : 0f);
                plateVisual.localPosition = Vector3.MoveTowards(plateVisual.localPosition, target, Time.deltaTime * 0.5f);
            }
        }

        /// <summary>
        /// Verifica se o objeto que entrou pode pressionar a placa (pela Tag) e o registra.
        /// </summary>
        /// <param name="other">Colisor que entrou no gatilho.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (!Accepts(other)) return;
            objectsOnPlate.Add(other);
            Evaluate();
        }

        /// <summary>
        /// Remove da contagem o objeto que saiu de cima da placa.
        /// </summary>
        /// <param name="other">Colisor que saiu do gatilho.</param>
        private void OnTriggerExit(Collider other)
        {
            if (objectsOnPlate.Remove(other)) Evaluate();
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
        /// Atualiza o estado pressionado e envia o sinal quando ele muda.
        /// </summary>
        private void Evaluate()
        {
            bool pressed = objectsOnPlate.Count > 0;
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
