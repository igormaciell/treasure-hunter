using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Teto instável sobre o centro do salão. Ao receber o sinal da alavanca, cai poeira de aviso e,
    /// em seguida, a função SpawnRock() derruba pedregulhos sorteados de um VETOR DE PREFABS em posições
    /// sorteadas de um VETOR DE PONTOS. Serve para surpreender os esqueletos reunidos embaixo.
    /// </summary>
    public class CeilingCollapseTrap : SignalReceiver, IResettable
    {
        /// <summary>Sala da armadilha.</summary>
        public int roomIndex;
        /// <summary>Vetor de prefabs de pedregulhos.</summary>
        public GameObject[] rockPrefabs;
        /// <summary>Vetor de pontos do teto de onde as pedras caem.</summary>
        public Transform[] dropPoints;
        /// <summary>Quantas pedras caem.</summary>
        public int rockCount = 7;
        /// <summary>Tempo de aviso (poeira) antes da queda.</summary>
        public float warningTime = 1.0f;
        /// <summary>Intervalo entre as pedras.</summary>
        public float interval = 0.15f;
        /// <summary>Poeira de aviso.</summary>
        public GameObject warningEffect;

        private bool fired;
        private readonly List<GameObject> spawned = new List<GameObject>();

        /// <summary>Sala da armadilha (IResettable).</summary>
        public int RoomIndex => roomIndex;

        /// <summary>
        /// Ao ligar (alavanca puxada), começa o desabamento uma única vez.
        /// </summary>
        /// <param name="on">Novo estado.</param>
        protected override void OnStateChanged(bool on)
        {
            if (on && !fired) StartCoroutine(Collapse());
        }

        /// <summary>
        /// Aviso de poeira e tremor; depois derruba as pedras uma a uma.
        /// </summary>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator Collapse()
        {
            fired = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(AudioManager.Instance.rumble, transform.position, 1f);
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(warningTime, 0.06f);
            if (warningEffect != null && dropPoints != null)
            {
                foreach (Transform p in dropPoints) if (p != null) Instantiate(warningEffect, p.position, Quaternion.identity);
            }
            yield return new WaitForSeconds(warningTime);

            for (int i = 0; i < rockCount; i++)
            {
                SpawnRock();
                yield return new WaitForSeconds(interval);
            }
        }

        /// <summary>
        /// Sorteia um ponto do vetor de pontos e um prefab do vetor de pedras e cria a pedra.
        /// </summary>
        /// <returns>A pedra criada (ou nulo).</returns>
        public GameObject SpawnRock()
        {
            if (rockPrefabs == null || rockPrefabs.Length == 0 || dropPoints == null || dropPoints.Length == 0) return null;
            Transform point = dropPoints[Random.Range(0, dropPoints.Length)];
            GameObject prefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
            if (point == null || prefab == null) return null;
            Vector3 jitter = new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));
            GameObject rock = Instantiate(prefab, point.position + jitter, Random.rotation);
            spawned.Add(rock);
            return rock;
        }

        /// <summary>
        /// Rearma a armadilha e remove as pedras que sobraram (IResettable).
        /// </summary>
        public void ResetState()
        {
            StopAllCoroutines();
            foreach (GameObject g in spawned) if (g != null) Destroy(g);
            spawned.Clear();
            fired = false;
            ClearSignals();
        }
    }
}
