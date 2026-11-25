using System.Collections;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Plataforma frágil (rocha rachada ou cristal instável): quando John pisa, ela treme (Translate), solta poeira
    /// e cai com AddForce + AddTorque (ou se estilhaça). Depois de alguns segundos ela volta ao lugar.
    /// Tem a Tag "Unstable": o totem não pode ser colocado sobre ela.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CrumblingPlatform : MonoBehaviour, ILandingReceiver, IResettable
    {
        /// <summary>Sala (IResettable).</summary>
        public int roomIndex = 1;
        /// <summary>Tempo entre pisar e desmoronar.</summary>
        public float crumbleDelay = 0.9f;
        /// <summary>Tempo até reaparecer depois de cair.</summary>
        public float respawnDelay = 4.5f;
        /// <summary>Amplitude do tremor da parte visual.</summary>
        public float shakeAmount = 0.06f;
        /// <summary>true: estilhaça na hora (cristal); false: cai girando (rocha).</summary>
        public bool shatter = false;
        /// <summary>Parte visual que treme.</summary>
        public Transform visual;
        public GameObject[] crackEffects;
        public GameObject[] breakEffects;

        /// <summary>Sala da plataforma.</summary>
        public int RoomIndex => roomIndex;

        private Rigidbody rb;
        private Collider[] colliders;
        private Renderer[] renderers;
        private Vector3 startPos, visualStart;
        private Quaternion startRot;
        private bool triggered;
        private Coroutine routine;

        /// <summary>Guarda a pose inicial e deixa o corpo cinemático.</summary>
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            colliders = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
            startPos = transform.position;
            startRot = transform.rotation;
            if (visual != null) visualStart = visual.localPosition;
        }

        /// <summary>Pouso de John: começa a contagem.</summary>
        /// <param name="player">Jogador.</param>
        /// <param name="dropHeight">Altura da queda.</param>
        public void OnPlayerLanded(ParkourController player, float dropHeight)
        {
            Begin();
        }

        /// <summary>John em pé: começa a contagem (caso tenha chegado andando).</summary>
        /// <param name="player">Jogador.</param>
        public void OnPlayerStanding(ParkourController player)
        {
            Begin();
        }

        /// <summary>Inicia o tremor (uma vez).</summary>
        private void Begin()
        {
            if (triggered) return;
            triggered = true;
            routine = StartCoroutine(Crumble());
        }

        /// <summary>
        /// Tremor → queda/estilhaço → espera → volta.
        /// </summary>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator Crumble()
        {
            ParkourAudio a = ParkourAudio.Instance;
            if (a != null) ParkourAudio.Play(a.platformCrack, transform.position, 0.9f);
            ParkourUtil.SpawnAll(crackEffects, transform.position);

            float t = 0f;
            while (t < crumbleDelay)
            {
                t += Time.deltaTime;
                if (visual != null)
                {
                    visual.localPosition = visualStart;
                    float k = shakeAmount * (0.4f + t / crumbleDelay);
                    visual.Translate(new Vector3(Random.Range(-k, k), Random.Range(-k, k) * 0.5f, Random.Range(-k, k)), Space.Self);
                }
                yield return null;
            }
            if (visual != null) visual.localPosition = visualStart;

            if (a != null) ParkourAudio.Play(a.platformCrumble, transform.position, 1f);
            ParkourUtil.SpawnAll(breakEffects, transform.position);
            if (shatter)
            {
                SetPresent(false);
            }
            else
            {
                foreach (Collider c in colliders) c.enabled = false;
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.AddForce(Vector3.down * 2f, ForceMode.VelocityChange);
                rb.AddTorque(Random.insideUnitSphere * 1.5f, ForceMode.VelocityChange);
                yield return new WaitForSeconds(2.5f);
                SetPresent(false);
            }

            yield return new WaitForSeconds(respawnDelay);
            Restore();
            if (a != null) ParkourAudio.Play(a.platformRestore, transform.position, 0.7f);
        }

        /// <summary>
        /// Mostra/esconde a plataforma (render e colisores).
        /// </summary>
        /// <param name="on">Visível e sólida.</param>
        private void SetPresent(bool on)
        {
            foreach (Renderer r in renderers) r.enabled = on;
            foreach (Collider c in colliders) c.enabled = on;
        }

        /// <summary>
        /// Volta à pose inicial (cinemática, visível e sólida).
        /// </summary>
        private void Restore()
        {
            rb.isKinematic = true;
            transform.SetPositionAndRotation(startPos, startRot);
            rb.position = startPos;
            rb.rotation = startRot;
            if (visual != null) visual.localPosition = visualStart;
            SetPresent(true);
            triggered = false;
            routine = null;
        }

        /// <summary>Reinicia a plataforma (botão "Reiniciar sala").</summary>
        public void ResetState()
        {
            if (routine != null) StopCoroutine(routine);
            Restore();
        }
    }
}
