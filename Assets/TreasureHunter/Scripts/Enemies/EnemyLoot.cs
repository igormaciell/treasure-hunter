using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Item que um inimigo pode deixar cair ao morrer. A chance é sorteada com Random.value:
    /// nos esqueletos da faca, dropChance = 0,35 (35% de chance de soltar a faca).
    /// O item é sorteado de um VETOR DE PREFABS.
    /// </summary>
    public class EnemyLoot : MonoBehaviour
    {
        /// <summary>Chance de soltar um item (0 a 1). 0,35 = 35%.</summary>
        [Range(0f, 1f)] public float dropChance = 0.35f;
        /// <summary>Vetor de itens possíveis (um é sorteado).</summary>
        public GameObject[] dropPrefabs;

        /// <summary>
        /// Sorteia a queda do item.
        /// </summary>
        /// <param name="position">Posição do inimigo.</param>
        /// <param name="room">Sala do inimigo (para limpar o item ao reiniciar a sala).</param>
        /// <returns>O item criado, ou nulo se o sorteio falhou.</returns>
        public GameObject TryDrop(Vector3 position, CombatRoom room)
        {
            if (dropPrefabs == null || dropPrefabs.Length == 0) return null;
            if (Random.value >= dropChance) return null;
            GameObject prefab = dropPrefabs[Random.Range(0, dropPrefabs.Length)];
            if (prefab == null) return null;
            GameObject item = Instantiate(prefab, position + Vector3.up * 0.1f, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            if (room != null) room.RegisterSpawned(item);
            return item;
        }
    }
}
