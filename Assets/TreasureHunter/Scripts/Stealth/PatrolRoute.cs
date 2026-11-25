using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Trajeto de patrulha reutilizável: pontos (filhos), pausa em cada ponto e para onde olhar durante a pausa
    /// (a rotação do ponto). Pode ser em circuito (volta ao primeiro) ou vai-e-volta.
    /// </summary>
    public class PatrolRoute : MonoBehaviour
    {
        /// <summary>Forma de percorrer os pontos.</summary>
        public enum Mode { Loop, PingPong }

        public Mode mode = Mode.Loop;
        /// <summary>Pontos do trajeto (em ordem).</summary>
        public Transform[] points;
        /// <summary>Pausa em cada ponto (segundos; vetor menor que os pontos repete o último valor).</summary>
        public float[] waitTimes = { 2f };
        /// <summary>Durante a pausa, olhar para a frente do ponto (rotação do Transform).</summary>
        public bool faceWaypointRotation = true;
        /// <summary>Cor no editor.</summary>
        public Color gizmoColor = new Color(1f, 0.55f, 0.15f, 0.9f);

        /// <summary>Quantidade de pontos.</summary>
        public int Count => points != null ? points.Length : 0;

        /// <summary>
        /// Posição de um ponto.
        /// </summary>
        /// <param name="i">Índice.</param>
        /// <returns>Posição.</returns>
        public Vector3 Point(int i)
        {
            if (Count == 0) return transform.position;
            Transform t = points[Mathf.Clamp(i, 0, Count - 1)];
            return t != null ? t.position : transform.position;
        }

        /// <summary>
        /// Rotação de um ponto (direção para olhar na pausa).
        /// </summary>
        /// <param name="i">Índice.</param>
        /// <returns>Rotação.</returns>
        public Quaternion Facing(int i)
        {
            if (Count == 0) return transform.rotation;
            Transform t = points[Mathf.Clamp(i, 0, Count - 1)];
            return t != null ? t.rotation : transform.rotation;
        }

        /// <summary>
        /// Pausa num ponto.
        /// </summary>
        /// <param name="i">Índice.</param>
        /// <returns>Segundos.</returns>
        public float Wait(int i)
        {
            if (waitTimes == null || waitTimes.Length == 0) return 0f;
            return waitTimes[Mathf.Clamp(i, 0, waitTimes.Length - 1)];
        }

        /// <summary>
        /// Próximo ponto (circuito ou vai-e-volta).
        /// </summary>
        /// <param name="current">Atual.</param>
        /// <param name="direction">Sentido (+1/-1), atualizado no vai-e-volta.</param>
        /// <returns>Próximo índice.</returns>
        public int Next(int current, ref int direction)
        {
            if (Count <= 1) return 0;
            if (mode == Mode.Loop) return (current + 1) % Count;
            int n = current + direction;
            if (n < 0 || n >= Count)
            {
                direction = -direction;
                n = current + direction;
            }
            return Mathf.Clamp(n, 0, Count - 1);
        }

        /// <summary>
        /// Ponto mais próximo de uma posição.
        /// </summary>
        /// <param name="position">Posição.</param>
        /// <returns>Índice.</returns>
        public int Closest(Vector3 position)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < Count; i++)
            {
                float d = (Point(i) - position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>
        /// Desenha o trajeto no editor.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (Count == 0) return;
            Gizmos.color = gizmoColor;
            for (int i = 0; i < Count; i++)
            {
                if (points[i] == null) continue;
                Gizmos.DrawWireSphere(points[i].position + Vector3.up * 0.2f, 0.35f);
                Gizmos.DrawLine(points[i].position + Vector3.up * 0.2f, points[i].position + Vector3.up * 0.2f + points[i].forward * 1.2f);
                int n = i + 1;
                if (n >= Count)
                {
                    if (mode != Mode.Loop) break;
                    n = 0;
                }
                if (points[n] != null) Gizmos.DrawLine(points[i].position + Vector3.up * 0.2f, points[n].position + Vector3.up * 0.2f);
            }
        }
    }
}
