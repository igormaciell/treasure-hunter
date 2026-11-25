using System;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Habilidade "Totem de Checkpoint" (fica no jogador). Segurando T aparece um totem fantasma onde a câmera
    /// aponta: verde em superfície válida, vermelho em inválida (com o motivo no HUD). Soltando T, o totem é
    /// colocado (ou movido) e vira o checkpoint: ao cair no abismo, John volta para ele.
    ///
    /// A superfície é validada por Raycast: precisa ser chão (não ar, não parede), plana e firme, com espaço
    /// para John ficar em pé, ao alcance do jogador e sem se mover ou desmoronar (Tag "Unstable").
    ///
    /// Para usar em outro nível (ex.: Nível 5): coloque o prefab John_Craft_Parkour (ou este componente no
    /// jogador) e marque <see cref="loadSavedProgress"/> — o totem coletado no Nível 3 fica salvo (PlayerProgress).
    /// </summary>
    public class TotemAbility : MonoBehaviour
    {
        /// <summary>Começa com o totem.</summary>
        [Header("Desbloqueio")]
        public bool unlocked = false;
        /// <summary>Libera o totem se ele já foi coletado em outra partida (PlayerPrefs).</summary>
        public bool loadSavedProgress = false;

        /// <summary>Totem colocado (criado por função na primeira colocação).</summary>
        [Header("Prefabs")]
        public GameObject totemPrefab;
        /// <summary>Totem fantasma (prévia verde/vermelha).</summary>
        public GameObject ghostPrefab;

        /// <summary>Alcance do raio da câmera.</summary>
        [Header("Validação")]
        public float cameraRayLength = 14f;
        /// <summary>Distância horizontal máxima entre John e o totem.</summary>
        public float maxDistance = 5f;
        /// <summary>Diferença de altura máxima entre John e o totem.</summary>
        public float maxHeightDifference = 3f;
        /// <summary>Inclinação máxima do chão (acima disso é parede).</summary>
        public float maxSlope = 30f;
        /// <summary>Raio da base que precisa estar apoiada.</summary>
        public float footprintRadius = 0.35f;
        /// <summary>Diferença de altura aceita entre os pontos da base.</summary>
        public float flatTolerance = 0.22f;
        public Color validColor = new Color(0.3f, 1f, 0.45f, 0.5f);
        public Color invalidColor = new Color(1f, 0.25f, 0.2f, 0.5f);

        /// <summary>O jogador tem o totem.</summary>
        public bool Unlocked => unlocked;
        /// <summary>T está pressionado (prévia visível).</summary>
        public bool Aiming { get; private set; }
        /// <summary>A posição da prévia é válida.</summary>
        public bool CandidateValid { get; private set; }
        /// <summary>Motivo da posição inválida (ou texto de ajuda quando válida).</summary>
        public string CandidateMessage { get; private set; } = "";
        /// <summary>Quantas vezes o totem foi colocado.</summary>
        public int Placements { get; private set; }
        /// <summary>Totem colocado (nulo antes da primeira colocação).</summary>
        public CheckpointTotem Totem { get; private set; }

        /// <summary>Estado mudou (desbloqueio, colocação).</summary>
        public event Action Changed;

        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private PlayerController controller;
        private ParkourController parkour;
        private GameObject ghost;
        private Renderer[] ghostRenderers;
        private Light ghostLight;
        private MaterialPropertyBlock block;
        private Vector3 candidatePoint, candidateStand;
        private Quaternion candidateRotation;
        private bool wasHeld;

        /// <summary>Referências e o fantasma (escondido).</summary>
        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            parkour = GetComponent<ParkourController>();
            block = new MaterialPropertyBlock();
            if (loadSavedProgress && PlayerProgress.HasTotem) unlocked = true;
            if (ghostPrefab != null)
            {
                ghost = Instantiate(ghostPrefab);
                ghost.name = "Totem_Fantasma (prévia)";
                ghostRenderers = ghost.GetComponentsInChildren<Renderer>(true);
                ghostLight = ghost.GetComponentInChildren<Light>(true);
                ghost.SetActive(false);
            }
        }

        /// <summary>Avisa o HUD do estado inicial.</summary>
        private void Start()
        {
            Changed?.Invoke();
        }

        /// <summary>
        /// Libera o totem (coleta no Nível 3) e salva o progresso.
        /// </summary>
        /// <param name="save">Gravar no PlayerPrefs.</param>
        public void Unlock(bool save)
        {
            unlocked = true;
            if (save) PlayerProgress.UnlockTotem();
            Changed?.Invoke();
        }

        /// <summary>
        /// Segurar T mostra a prévia; soltar coloca; botão direito cancela.
        /// </summary>
        private void Update()
        {
            bool canAct = GameManager.Instance == null || GameManager.Instance.CanPlayerAct();
            bool held = unlocked && canAct && ParkourInput.TotemHeld() && (parkour == null || !parkour.IsMantling);

            if (held)
            {
                if (!Aiming) { Aiming = true; Changed?.Invoke(); }
                if (ParkourInput.CancelPressed())
                {
                    Aiming = false;
                    wasHeld = false;
                    ShowGhost(false);
                    Changed?.Invoke();
                    return;
                }
                UpdateCandidate();
                wasHeld = true;
            }
            else if (wasHeld)
            {
                wasHeld = false;
                Aiming = false;
                ShowGhost(false);
                if (canAct && unlocked) TryPlace();
                Changed?.Invoke();
            }
            else if (Aiming)
            {
                Aiming = false;
                ShowGhost(false);
                Changed?.Invoke();
            }
        }

        // ================================================================== Prévia e validação

        /// <summary>
        /// Raio da câmera → ponto candidato → validação → fantasma verde ou vermelho.
        /// </summary>
        private void UpdateCandidate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            // Começa o raio na altura do jogador, para não acertar coisas entre a câmera e John.
            float skip = Mathf.Max(0f, Vector3.Dot(transform.position + Vector3.up - ray.origin, ray.direction) - 0.5f);
            Vector3 origin = ray.origin + ray.direction * skip;
            int mask = ParkourLayers.SurfaceMask;

            Vector3 point, normal = Vector3.up;
            Collider hitCollider = null;
            if (Physics.Raycast(origin, ray.direction, out RaycastHit hit, cameraRayLength, mask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                normal = hit.normal;
                hitCollider = hit.collider;
            }
            else
            {
                point = origin + ray.direction * Mathf.Min(cameraRayLength, maxDistance + 1f);
            }

            Vector3 flatToPlayer = transform.position - point;
            flatToPlayer.y = 0f;
            candidateRotation = flatToPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(flatToPlayer.normalized, Vector3.up) : Quaternion.identity;
            candidatePoint = point;
            CandidateValid = Validate(point, normal, hitCollider, out string message, out candidateStand);
            CandidateMessage = message;

            if (ghost == null) return;
            ShowGhost(true);
            ghost.transform.SetPositionAndRotation(point, candidateRotation);
            Color c = CandidateValid ? validColor : invalidColor;
            block.SetColor(ColorId, c);
            block.SetColor(EmissionId, new Color(c.r, c.g, c.b) * 1.5f);
            foreach (Renderer r in ghostRenderers) if (r != null) r.SetPropertyBlock(block);
            if (ghostLight != null) ghostLight.color = c;
        }

        /// <summary>
        /// Valida um ponto para o totem (todas as regras com Raycast/CheckCapsule).
        /// </summary>
        /// <param name="point">Ponto da superfície.</param>
        /// <param name="normal">Normal da superfície.</param>
        /// <param name="surface">Colisor atingido (nulo = nada, o ponto está no ar).</param>
        /// <param name="message">Motivo (inválido) ou ajuda (válido).</param>
        /// <param name="stand">Onde John reaparece.</param>
        /// <returns>true se o totem pode ficar ali.</returns>
        public bool Validate(Vector3 point, Vector3 normal, Collider surface, out string message, out Vector3 stand)
        {
            stand = point;
            int mask = ParkourLayers.SurfaceMask;
            if (surface == null) { message = "Não dá para colocar o totem no ar."; return false; }
            if (Vector3.Angle(normal, Vector3.up) > maxSlope) { message = "Isso é uma parede: o totem precisa de chão plano."; return false; }
            if (surface.CompareTag(ParkourTags.Unstable) || surface.GetComponentInParent<PlatformMotion>() != null ||
                surface.GetComponentInParent<CrumblingPlatform>() != null ||
                (surface.attachedRigidbody != null && !surface.attachedRigidbody.isKinematic))
            { message = "Superfície instável: ela se move ou desmorona."; return false; }

            Vector3 delta = point - transform.position;
            if (Mathf.Abs(delta.y) > maxHeightDifference || new Vector2(delta.x, delta.z).magnitude > maxDistance)
            { message = "Longe demais: aproxime-se do lugar."; return false; }

            // A base inteira precisa estar apoiada e nivelada (evita bordas, pontas de cristal e degraus).
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                Vector3 p = point + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * footprintRadius;
                if (!Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out RaycastHit h, 0.5f + flatTolerance, mask, QueryTriggerInteraction.Ignore)
                    || Mathf.Abs(h.point.y - point.y) > flatTolerance || Vector3.Angle(h.normal, Vector3.up) > maxSlope)
                { message = "Chão irregular ou estreito demais."; return false; }
            }

            // Espaço livre para o totem e para John em pé.
            if (Blocked(point)) { message = "Sem espaço livre acima."; return false; }

            // Lugar ao lado do totem para John reaparecer (ou no próprio ponto).
            stand = point + Vector3.up * 0.05f;
            Vector3 toPlayer = transform.position - point;
            toPlayer.y = 0f;
            Vector3 first = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : Vector3.forward;
            Vector3[] dirs = { first, Quaternion.Euler(0f, 90f, 0f) * first, Quaternion.Euler(0f, -90f, 0f) * first, -first };
            foreach (Vector3 d in dirs)
            {
                Vector3 s = point + d * 0.75f;
                if (Physics.Raycast(s + Vector3.up * 0.6f, Vector3.down, out RaycastHit h, 1f, mask, QueryTriggerInteraction.Ignore)
                    && Mathf.Abs(h.point.y - point.y) < 0.3f && Vector3.Angle(h.normal, Vector3.up) <= maxSlope && !Blocked(h.point))
                {
                    stand = h.point + Vector3.up * 0.05f;
                    break;
                }
            }
            message = "Solte T para colocar o totem aqui.";
            return true;
        }

        /// <summary>
        /// Verifica se há algo ocupando o espaço de uma pessoa em pé no ponto.
        /// </summary>
        /// <param name="feet">Ponto no chão.</param>
        /// <returns>true se bloqueado.</returns>
        private static bool Blocked(Vector3 feet)
        {
            const float r = 0.3f;
            return Physics.CheckCapsule(feet + Vector3.up * (r + 0.1f), feet + Vector3.up * (1.8f - r), r, ParkourLayers.SurfaceMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Mostra/esconde o fantasma.
        /// </summary>
        /// <param name="on">Visível.</param>
        private void ShowGhost(bool on)
        {
            if (ghost != null && ghost.activeSelf != on) ghost.SetActive(on);
        }

        // ================================================================== Colocação

        /// <summary>
        /// Coloca o totem na última posição validada (ou avisa por que não pode).
        /// </summary>
        private void TryPlace()
        {
            ParkourAudio a = ParkourAudio.Instance;
            if (!CandidateValid)
            {
                if (a != null) ParkourAudio.Play2D(a.totemInvalid, 0.8f);
                ParkourUtil.Hint(string.IsNullOrEmpty(CandidateMessage) ? "Posição inválida para o totem." : CandidateMessage);
                return;
            }
            PlaceTotem(candidatePoint, candidateRotation, candidateStand);
        }

        /// <summary>
        /// Cria (na primeira vez) ou move o totem e o torna o checkpoint ativo.
        /// </summary>
        /// <param name="point">Ponto da superfície.</param>
        /// <param name="rotation">Rotação do totem.</param>
        /// <param name="stand">Onde John reaparece.</param>
        public void PlaceTotem(Vector3 point, Quaternion rotation, Vector3 stand)
        {
            if (Totem == null) Totem = SpawnTotem();
            if (Totem == null) return;
            Vector3 look = point - stand;
            look.y = 0f;
            Quaternion standRotation = look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(-look.normalized, Vector3.up) : rotation;
            Totem.PlaceAt(point, rotation, stand, standRotation);

            GameManager gm = GameManager.Instance;
            if (gm != null) gm.SetCheckpoint(stand, standRotation, gm.CurrentRoom);
            ParkourCheckpoints.Set(Totem, "Totem");
            Placements++;
            if (parkour != null) parkour.Trigger("Place");
            ParkourUtil.Hint(Placements == 1 ? "Totem colocado! Se cair no abismo, você volta para ele." : "Totem reposicionado: novo checkpoint.");
            ParkourUtil.UnlockSkill("checkpoint_proprio");
            Changed?.Invoke();
        }

        /// <summary>
        /// Cria o totem a partir do prefab (spawn por função).
        /// </summary>
        /// <returns>O totem criado.</returns>
        private CheckpointTotem SpawnTotem()
        {
            if (totemPrefab == null) return null;
            GameObject go = Instantiate(totemPrefab);
            go.name = "Totem_de_Checkpoint (colocado)";
            return go.GetComponent<CheckpointTotem>();
        }

        /// <summary>
        /// John voltou ao checkpoint: se for o totem, mostra os efeitos dele.
        /// </summary>
        public void OnRespawned()
        {
            if (Totem != null && ReferenceEquals(ParkourCheckpoints.Current, Totem)) Totem.PlayRespawn();
        }
    }
}
