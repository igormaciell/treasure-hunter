using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Movimento de uma plataforma no passo de física atual (translação + rotação em torno de um pivô).
    /// As plataformas móveis e giratórias preenchem estes dados; o ParkourController usa para levar John junto.
    /// </summary>
    public class PlatformMotion : MonoBehaviour
    {
        /// <summary>Pivô da rotação neste passo.</summary>
        public Vector3 Pivot { get; private set; }
        /// <summary>Deslocamento neste passo.</summary>
        public Vector3 StepTranslation { get; private set; }
        /// <summary>Rotação neste passo.</summary>
        public Quaternion StepRotation { get; private set; } = Quaternion.identity;

        /// <summary>
        /// Registra o movimento que a plataforma fará neste passo.
        /// </summary>
        /// <param name="pivot">Pivô (posição atual).</param>
        /// <param name="translation">Deslocamento.</param>
        /// <param name="rotation">Rotação incremental.</param>
        public void Record(Vector3 pivot, Vector3 translation, Quaternion rotation)
        {
            Pivot = pivot;
            StepTranslation = translation;
            StepRotation = rotation;
        }

        /// <summary>
        /// Quanto um ponto apoiado na plataforma se desloca neste passo.
        /// </summary>
        /// <param name="point">Ponto no mundo.</param>
        /// <returns>Deslocamento do ponto.</returns>
        public Vector3 DeltaAt(Vector3 point)
        {
            Vector3 moved = Pivot + StepTranslation + StepRotation * (point - Pivot);
            return moved - point;
        }
    }
}
