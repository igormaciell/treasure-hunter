using System.Collections;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Cristal especial que guarda o totem. Ao interagir (E), John canaliza energia: o cristal racha três vezes,
    /// treme e se estilhaça — lascas físicas (vetor de prefabs sorteados, AddForce + AddTorque), partículas, luz e som —
    /// liberando o Totem de Checkpoint para coleta.
    /// </summary>
    public class TotemCrystal : Interactable
    {
        /// <summary>Cristal inteiro (some ao quebrar).</summary>
        public GameObject intactVisual;
        /// <summary>Item do totem (fica desativado dentro do cristal até ele quebrar).</summary>
        public GameObject totemItem;
        /// <summary>Luz interna do cristal.</summary>
        public Light coreLight;
        /// <summary>Vetor de lascas (sorteadas).</summary>
        public GameObject[] shardPrefabs;
        public int shardCount = 12;
        public float shardForce = 5f;
        public float shardLifetime = 6f;
        /// <summary>Rachaduras antes de quebrar.</summary>
        public int cracks = 3;
        public float crackInterval = 0.45f;
        public GameObject[] crackEffects;
        public GameObject[] breakEffects;
        [TextArea] public string brokenObjective = "Pegue o Totem de Checkpoint.";

        private bool broken, busy;
        private Vector3 visualStart;

        /// <summary>Esconde o totem dentro do cristal.</summary>
        private void Start()
        {
            if (totemItem != null) totemItem.SetActive(false);
            if (intactVisual != null) visualStart = intactVisual.transform.localPosition;
        }

        /// <summary>Só enquanto inteiro.</summary>
        /// <param name="interactor">Jogador.</param>
        /// <returns>true se pode interagir.</returns>
        public override bool CanInteract(PlayerInteractor interactor)
        {
            return !broken && !busy;
        }

        /// <summary>Texto do prompt.</summary>
        /// <returns>Texto.</returns>
        public override string GetPrompt()
        {
            return prompt;
        }

        /// <summary>Começa a quebrar o cristal.</summary>
        /// <param name="interactor">Jogador.</param>
        public override void Interact(PlayerInteractor interactor)
        {
            if (broken || busy) return;
            StartCoroutine(BreakSequence());
        }

        /// <summary>
        /// Rachaduras com tremor (Translate) e, no fim, o estilhaço.
        /// </summary>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator BreakSequence()
        {
            busy = true;
            if (GameManager.Instance != null && GameManager.Instance.ui != null) GameManager.Instance.ui.HidePrompt();
            ParkourAudio a = ParkourAudio.Instance;
            for (int i = 0; i < cracks; i++)
            {
                if (a != null) ParkourAudio.Play(a.crystalHit, transform.position + Vector3.up, 0.8f + 0.1f * i);
                ParkourUtil.SpawnAll(crackEffects, transform.position + Vector3.up * (0.8f + 0.4f * i));
                if (coreLight != null) coreLight.intensity *= 1.35f;
                float t = 0f;
                while (t < crackInterval)
                {
                    t += Time.deltaTime;
                    if (intactVisual != null)
                    {
                        Transform v = intactVisual.transform;
                        v.localPosition = visualStart;
                        float k = 0.03f * (i + 1);
                        v.Translate(Random.Range(-k, k), 0f, Random.Range(-k, k), Space.Self);
                    }
                    yield return null;
                }
            }
            Break();
            busy = false;
        }

        /// <summary>
        /// Estilhaça o cristal e libera o totem.
        /// </summary>
        public void Break()
        {
            broken = true;
            if (intactVisual != null) intactVisual.SetActive(false);
            Vector3 center = transform.position + Vector3.up * 1.2f;
            SpawnShards(center);
            ParkourUtil.SpawnAll(breakEffects, center);
            ParkourAudio a = ParkourAudio.Instance;
            if (a != null) ParkourAudio.Play(a.crystalBreak, center, 1f);
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.35f, 0.12f);
            if (coreLight != null) StartCoroutine(FadeLight());
            if (totemItem != null) totemItem.SetActive(true);
            if (GameManager.Instance != null && GameManager.Instance.ui != null)
            {
                GameManager.Instance.ui.ShowLine("O cristal se partiu... há um totem lá dentro!");
                GameManager.Instance.ui.SetObjective(brokenObjective);
            }
        }

        /// <summary>
        /// Cria as lascas sorteando do vetor de prefabs e as lança com força e giro.
        /// </summary>
        /// <param name="center">Centro da explosão.</param>
        private void SpawnShards(Vector3 center)
        {
            if (shardPrefabs == null || shardPrefabs.Length == 0) return;
            for (int i = 0; i < shardCount; i++)
            {
                GameObject prefab = shardPrefabs[Random.Range(0, shardPrefabs.Length)];
                if (prefab == null) continue;
                Vector3 dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) + 0.3f;
                GameObject s = Instantiate(prefab, center + dir * 0.4f, Random.rotation);
                Rigidbody rb = s.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(dir.normalized * shardForce * Random.Range(0.6f, 1.2f), ForceMode.VelocityChange);
                    rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.VelocityChange);
                }
                Destroy(s, shardLifetime + Random.Range(0f, 2f));
            }
        }

        /// <summary>
        /// A luz do cristal brilha forte e se apaga.
        /// </summary>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator FadeLight()
        {
            float start = coreLight.intensity * 2.5f;
            float t = 0f;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                coreLight.intensity = Mathf.Lerp(start, 0.6f, t / 1.2f);
                yield return null;
            }
        }
    }
}
