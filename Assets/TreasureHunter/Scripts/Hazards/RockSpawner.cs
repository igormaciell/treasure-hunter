using System.Collections;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Armadilha do Corredor das Pedras. Enquanto o jogador está no corredor, a função
    /// SpawnRock() cria pedras usando um VETOR DE PREFABS (pedra pequena, média, grande)
    /// em uma posição sorteada de um VETOR DE PONTOS no teto. Antes de cada pedra cai
    /// um pouco de poeira no ponto sorteado, avisando o jogador (sinal de perigo).
    /// </summary>
    public class RockSpawner : MonoBehaviour
    {
        /// <summary>Vetor de prefabs de pedras (um é sorteado a cada queda).</summary>
        public GameObject[] rockPrefabs;
        /// <summary>Vetor de pontos no teto de onde as pedras caem.</summary>
        public Transform[] spawnPoints;
        /// <summary>Prefab de poeira que avisa a queda.</summary>
        public GameObject warningDustPrefab;
        /// <summary>Intervalo mínimo entre pedras.</summary>
        public float minInterval = 0.7f;
        /// <summary>Intervalo máximo entre pedras.</summary>
        public float maxInterval = 1.5f;
        /// <summary>Tempo entre o aviso de poeira e a queda.</summary>
        public float warningTime = 0.7f;

        private bool playerInside;
        private Coroutine loop;
        private int lastIndex = -1;

        /// <summary>
        /// Quando o jogador entra na área, começa o ciclo de quedas.
        /// </summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(GameTags.Player)) return;
            playerInside = true;
            if (loop == null) loop = StartCoroutine(SpawnLoop());
        }

        /// <summary>
        /// Quando o jogador sai da área, as quedas param.
        /// </summary>
        /// <param name="other">Colisor que saiu.</param>
        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(GameTags.Player)) return;
            playerInside = false;
        }

        /// <summary>
        /// Laço das quedas: espera um tempo aleatório e chama SpawnRock() enquanto o jogador estiver dentro.
        /// </summary>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator SpawnLoop()
        {
            while (playerInside)
            {
                SpawnRock();
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            }
            loop = null;
        }

        /// <summary>
        /// Sorteia uma posição no vetor de pontos e um prefab no vetor de pedras,
        /// mostra o aviso e agenda a queda.
        /// </summary>
        public void SpawnRock()
        {
            if (rockPrefabs == null || rockPrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0) return;

            // Sorteia o índice do ponto, evitando repetir o último para variar.
            int index = Random.Range(0, spawnPoints.Length);
            if (spawnPoints.Length > 1 && index == lastIndex) index = (index + 1) % spawnPoints.Length;
            lastIndex = index;

            // Sorteia o tipo de pedra no vetor de prefabs.
            GameObject prefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
            StartCoroutine(DropAfterWarning(prefab, spawnPoints[index]));
        }

        /// <summary>
        /// Mostra a poeira de aviso e, após o tempo de aviso, instancia a pedra.
        /// </summary>
        /// <param name="prefab">Prefab da pedra.</param>
        /// <param name="point">Ponto do teto.</param>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator DropAfterWarning(GameObject prefab, Transform point)
        {
            if (warningDustPrefab != null) Instantiate(warningDustPrefab, point.position, Quaternion.identity);
            yield return new WaitForSeconds(warningTime);

            // Pequena variação horizontal para as pedras não caírem sempre no mesmo lugar exato.
            Vector3 offset = new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f));
            Instantiate(prefab, point.position + offset, Quaternion.identity);
        }

        /// <summary>
        /// Desenha os pontos de queda no editor.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (spawnPoints == null) return;
            Gizmos.color = new Color(1f, 0.5f, 0f);
            foreach (Transform t in spawnPoints)
            {
                if (t != null) Gizmos.DrawWireSphere(t.position, 0.3f);
            }
        }
    }
}
