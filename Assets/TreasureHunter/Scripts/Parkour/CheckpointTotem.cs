using System.Collections;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Totem de Checkpoint colocado no cenário: sobe do chão (Translate), o cristal do topo gira e brilha,
    /// e ele marca onde John reaparece ao cair no abismo. Pode ser reposicionado quantas vezes quiser.
    /// </summary>
    public class CheckpointTotem : MonoBehaviour
    {
        /// <summary>Parte visual que sobe do chão.</summary>
        public Transform visual;
        /// <summary>Cristal do topo (gira).</summary>
        public Transform crystal;
        /// <summary>Luz do totem.</summary>
        public Light glowLight;
        public float lightIntensity = 2.4f;
        public float riseHeight = 1.1f;
        public float riseTime = 0.55f;
        public float spinSpeed = 90f;
        public GameObject[] activateEffects;
        public GameObject[] removeEffects;
        public GameObject[] respawnEffects;

        /// <summary>Onde John reaparece.</summary>
        public Vector3 RespawnPosition { get; private set; }
        /// <summary>Para onde John olha ao reaparecer.</summary>
        public Quaternion RespawnRotation { get; private set; } = Quaternion.identity;
        /// <summary>O totem já foi colocado alguma vez.</summary>
        public bool Placed { get; private set; }

        private Vector3 visualBase;
        private float flash;

        /// <summary>Guarda a posição de repouso da parte visual.</summary>
        private void Awake()
        {
            if (visual != null) visualBase = visual.localPosition;
        }

        /// <summary>Gira o cristal e controla a luz.</summary>
        private void Update()
        {
            if (crystal != null) crystal.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
            if (glowLight != null)
            {
                flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 2f);
                glowLight.intensity = lightIntensity * (0.85f + 0.15f * Mathf.Sin(Time.time * 3f)) + flash * 4f;
            }
        }

        /// <summary>
        /// Coloca (ou move) o totem.
        /// </summary>
        /// <param name="position">Ponto da superfície.</param>
        /// <param name="rotation">Rotação do totem.</param>
        /// <param name="respawnPosition">Onde John reaparece.</param>
        /// <param name="respawnRotation">Para onde John olha ao reaparecer.</param>
        public void PlaceAt(Vector3 position, Quaternion rotation, Vector3 respawnPosition, Quaternion respawnRotation)
        {
            if (Placed) ParkourUtil.SpawnAll(removeEffects, transform.position + Vector3.up * 0.3f);
            transform.SetPositionAndRotation(position, rotation);
            RespawnPosition = respawnPosition;
            RespawnRotation = respawnRotation;
            Placed = true;
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(Rise());
            ParkourUtil.SpawnAll(activateEffects, position + Vector3.up * 0.1f);
            ParkourAudio a = ParkourAudio.Instance;
            if (a != null) ParkourAudio.Play(a.totemPlace, position, 1f);
        }

        /// <summary>
        /// Animação de subida: a parte visual sai de dentro do chão com Translate.
        /// </summary>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator Rise()
        {
            if (visual == null) yield break;
            visual.localPosition = visualBase - Vector3.up * riseHeight;
            float speed = riseHeight / Mathf.Max(0.05f, riseTime);
            while (visual.localPosition.y < visualBase.y)
            {
                visual.Translate(Vector3.up * speed * Time.deltaTime, Space.World);
                yield return null;
            }
            visual.localPosition = visualBase;
            flash = 1f;
        }

        /// <summary>
        /// Efeitos de quando John reaparece neste totem.
        /// </summary>
        public void PlayRespawn()
        {
            flash = 1f;
            ParkourUtil.SpawnAll(respawnEffects, RespawnPosition);
            ParkourAudio a = ParkourAudio.Instance;
            if (a != null) ParkourAudio.Play(a.totemRespawn, RespawnPosition, 1f);
        }
    }
}
