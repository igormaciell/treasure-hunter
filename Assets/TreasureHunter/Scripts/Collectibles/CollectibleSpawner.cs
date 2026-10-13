using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Espalha tesouros pela sala ao iniciar o nível. A função SpawnCollectibles() escolhe
    /// posições aleatórias (sem repetir) de um VETOR DE PONTOS e, para cada uma, sorteia
    /// um prefab de um VETOR DE PREFABS (moeda, joia vermelha, joia azul...).
    /// Assim, cada partida tem uma distribuição diferente de tesouros.
    /// </summary>
    public class CollectibleSpawner : MonoBehaviour
    {
        /// <summary>Vetor de prefabs de coletáveis.</summary>
        public GameObject[] prefabs;
        /// <summary>Vetor de pontos possíveis.</summary>
        public Transform[] points;
        /// <summary>Quantos itens criar.</summary>
        public int count = 5;

        /// <summary>
        /// Cria os itens assim que a cena começa.
        /// </summary>
        private void Start()
        {
            SpawnCollectibles(count);
        }

        /// <summary>
        /// Cria "amount" itens em pontos sorteados do vetor, sem repetir pontos.
        /// </summary>
        /// <param name="amount">Quantidade de itens desejada (limitada ao número de pontos).</param>
        /// <returns>Quantidade de itens efetivamente criados.</returns>
        public int SpawnCollectibles(int amount)
        {
            if (prefabs == null || prefabs.Length == 0 || points == null || points.Length == 0) return 0;

            // Cria um vetor de índices 0..n-1 e o embaralha para sortear posições sem repetição.
            int[] order = new int[points.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            SymbolSequencePuzzle.Shuffle(order);

            int created = Mathf.Min(amount, points.Length);
            for (int i = 0; i < created; i++)
            {
                Transform point = points[order[i]];
                GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
                Instantiate(prefab, point.position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            }
            return created;
        }
    }
}
