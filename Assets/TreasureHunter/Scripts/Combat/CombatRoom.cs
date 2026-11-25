using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace TreasureHunter
{
    /// <summary>
    /// Sala de combate do Nível 2. Quando John entra na área da sala:
    /// salva o checkpoint, mostra o título e a missão, guarda o arsenal (para restaurar no checkpoint),
    /// fecha a grade de entrada e cria os inimigos com a função SpawnEnemy().
    /// Cada grupo de inimigos usa um VETOR DE PREFABS (variações do mesmo tipo, uma é sorteada) e um
    /// VETOR DE PONTOS (posições sorteadas sem repetir). Quando todos caem, a sala é liberada: as grades abrem,
    /// a missão muda e uma habilidade pode ser aprendida. Implementa IResettable para "Voltar ao checkpoint".
    /// </summary>
    public class CombatRoom : MonoBehaviour, IResettable
    {
        /// <summary>Grupo de inimigos de um mesmo tipo.</summary>
        [Serializable]
        public class SpawnGroup
        {
            /// <summary>Nome do grupo (só para organização).</summary>
            public string label = "Grupo";
            /// <summary>Vetor de prefabs (variações do inimigo); um é sorteado para cada inimigo.</summary>
            public GameObject[] variants;
            /// <summary>Vetor de pontos possíveis; as posições são sorteadas sem repetir.</summary>
            public Transform[] points;
            /// <summary>Quantos inimigos criar.</summary>
            public int count = 1;
            /// <summary>Se falso, os inimigos ficam de guarda até ver ou ouvir John.</summary>
            public bool startAlerted = true;
        }

        /// <summary>Salas do nível (para estatísticas).</summary>
        public static readonly List<CombatRoom> AllRooms = new List<CombatRoom>();
        /// <summary>Sala com combate em andamento.</summary>
        public static CombatRoom Active { get; private set; }

        [Header("Sala")]
        public int roomIndex = 1;
        public string title = "Nível 2 — As Catacumbas de Añang";
        public string subtitle = "";
        [TextArea] public string objective = "";
        [TextArea] public string tutorial = "";
        public string line = "";
        public Transform respawnPoint;

        [Header("Ao liberar a sala")]
        [TextArea] public string clearedObjective = "";
        public string clearedLine = "";
        public string skillOnClear = "";
        public SignalReceiver[] gatesOnClear;

        [Header("Inimigos")]
        public SpawnGroup[] groups;
        /// <summary>Pontos de ataque dos arqueiros nesta sala.</summary>
        public Transform[] attackPoints;
        public float spawnDelay = 1.0f;
        public float spawnInterval = 0.25f;

        [Header("Entrada")]
        public IronGate entranceGate;
        public bool closeEntrance = true;

        /// <summary>Inimigos vivos.</summary>
        public int Remaining => alive.Count + pendingSpawns;
        /// <summary>Total de inimigos da sala.</summary>
        public int Total
        {
            get
            {
                int t = 0;
                if (groups != null) foreach (SpawnGroup g in groups) if (g != null) t += Mathf.Max(0, g.count);
                return t;
            }
        }
        /// <summary>O combate já começou.</summary>
        public bool Started { get; private set; }
        /// <summary>A sala já foi liberada.</summary>
        public bool Cleared { get; private set; }
        /// <summary>Sala do checkpoint (IResettable).</summary>
        public int RoomIndex => roomIndex;

        private readonly List<EnemyBase> alive = new List<EnemyBase>();
        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private int pendingSpawns;
        private CombatSnapshot snapshot;
        private bool hasSnapshot;

        /// <summary>Registra a sala na lista global.</summary>
        private void OnEnable()
        {
            if (!AllRooms.Contains(this)) AllRooms.Add(this);
        }

        /// <summary>Remove a sala da lista global.</summary>
        private void OnDisable()
        {
            AllRooms.Remove(this);
            if (Active == this) Active = null;
        }

        /// <summary>
        /// Começa o combate quando John entra na área da sala.
        /// </summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (Started || Cleared || !other.CompareTag(GameTags.Player)) return;
            Begin();
        }

        /// <summary>
        /// Inicia o combate: checkpoint, avisos, arsenal guardado, grade fechada e inimigos.
        /// </summary>
        public void Begin()
        {
            Started = true;
            Active = this;
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                Transform p = respawnPoint != null ? respawnPoint : transform;
                gm.SetCheckpoint(p.position, p.rotation, roomIndex);
                if (gm.ui != null)
                {
                    if (!string.IsNullOrEmpty(subtitle)) gm.ui.ShowRoomTitle(title, subtitle);
                    if (!string.IsNullOrEmpty(objective)) gm.ui.SetObjective(objective);
                    if (!string.IsNullOrEmpty(tutorial)) gm.ui.ShowTutorial(tutorial, 8f);
                    if (!string.IsNullOrEmpty(line)) gm.ui.ShowLine(line);
                }
            }
            if (PlayerCombat.Instance != null) { snapshot = PlayerCombat.Instance.Capture(); hasSnapshot = true; }
            if (closeEntrance && entranceGate != null) entranceGate.SetOpen(false);
            if (CombatHUD.Instance != null) CombatHUD.Instance.ShowEnemies(Total, Total);
            StartCoroutine(SpawnRoutine());
        }

        /// <summary>
        /// Cria os inimigos de todos os grupos, um a um.
        /// </summary>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator SpawnRoutine()
        {
            pendingSpawns = Total;
            yield return new WaitForSeconds(spawnDelay);
            if (groups == null) yield break;
            foreach (SpawnGroup g in groups)
            {
                if (g == null || g.variants == null || g.variants.Length == 0 || g.points == null || g.points.Length == 0) continue;
                // Embaralha os índices dos pontos para sortear posições sem repetição.
                int[] order = new int[g.points.Length];
                for (int i = 0; i < order.Length; i++) order[i] = i;
                SymbolSequencePuzzle.Shuffle(order);
                for (int i = 0; i < g.count; i++)
                {
                    GameObject prefab = g.variants[UnityEngine.Random.Range(0, g.variants.Length)];
                    Transform point = g.points[order[i % order.Length]];
                    pendingSpawns--;
                    SpawnEnemy(prefab, point, g.startAlerted);
                    yield return new WaitForSeconds(spawnInterval);
                }
            }
            pendingSpawns = 0;
            CheckCleared();
        }

        /// <summary>
        /// Cria um inimigo num ponto (ajustado ao NavMesh) e o liga à sala.
        /// </summary>
        /// <param name="prefab">Prefab do inimigo.</param>
        /// <param name="point">Ponto de surgimento.</param>
        /// <param name="alerted">Se já surge alerta.</param>
        /// <returns>O inimigo criado (ou nulo).</returns>
        public EnemyBase SpawnEnemy(GameObject prefab, Transform point, bool alerted)
        {
            if (prefab == null || point == null) return null;
            Vector3 pos = point.position;
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2.5f, NavMesh.AllAreas)) pos = hit.position;
            GameObject go = Instantiate(prefab, pos, point.rotation);
            EnemyBase e = go.GetComponent<EnemyBase>();
            if (e == null) { Destroy(go); return null; }
            e.Room = this;
            e.StartAlerted = alerted;
            RangedEnemy archer = e as RangedEnemy;
            if (archer != null) archer.attackPoints = attackPoints;
            e.Died += OnEnemyDied;
            alive.Add(e);
            UpdateHUD();
            return e;
        }

        /// <summary>
        /// Guarda objetos criados durante o combate (itens soltos) para remover ao reiniciar.
        /// </summary>
        /// <param name="go">Objeto criado.</param>
        public void RegisterSpawned(GameObject go)
        {
            if (go != null) spawnedObjects.Add(go);
        }

        /// <summary>
        /// Um inimigo da sala morreu: atualiza a contagem e verifica se a sala foi liberada.
        /// </summary>
        /// <param name="e">Inimigo.</param>
        private void OnEnemyDied(EnemyBase e)
        {
            e.Died -= OnEnemyDied;
            alive.Remove(e);
            UpdateHUD();
            CheckCleared();
        }

        /// <summary>
        /// Libera a sala quando não restam inimigos vivos nem por surgir.
        /// </summary>
        private void CheckCleared()
        {
            if (!Started || Cleared || alive.Count > 0 || pendingSpawns > 0) return;
            Cleared = true;
            if (Active == this) Active = null;
            SignalReceiver.Broadcast(gatesOnClear, true, this);
            if (entranceGate != null) entranceGate.SetOpen(true);

            CombatAudio a = CombatAudio.Instance;
            if (a != null) CombatAudio.Play2D(a.roomClear, 0.9f);
            UIManager ui = GameManager.Instance != null ? GameManager.Instance.ui : null;
            if (ui != null)
            {
                ui.ShowToast("Sala liberada!", "Todos os inimigos foram derrotados. A passagem está aberta.", 3.5f);
                if (!string.IsNullOrEmpty(clearedObjective)) ui.SetObjective(clearedObjective);
                if (!string.IsNullOrEmpty(clearedLine)) ui.ShowLine(clearedLine);
            }
            if (!string.IsNullOrEmpty(skillOnClear)) PlayerCombat.UnlockSkill(skillOnClear);
            if (CombatHUD.Instance != null) CombatHUD.Instance.HideEnemies();
        }

        /// <summary>
        /// Envia a contagem de inimigos ao HUD.
        /// </summary>
        private void UpdateHUD()
        {
            if (CombatHUD.Instance != null && Started && !Cleared) CombatHUD.Instance.ShowEnemies(Remaining, Total);
        }

        /// <summary>
        /// Avisa todos os inimigos vivos da sala sobre um barulho.
        /// </summary>
        /// <param name="position">Origem.</param>
        public void AlertAll(Vector3 position)
        {
            foreach (EnemyBase e in alive) if (e != null) e.Hear(position);
        }

        /// <summary>
        /// Volta ao checkpoint: remove inimigos, itens e projéteis, restaura o arsenal guardado
        /// e reabre a grade de entrada; o combate recomeça quando John entrar de novo.
        /// </summary>
        public void ResetState()
        {
            if (Cleared || !Started) return;
            StopAllCoroutines();
            foreach (EnemyBase e in alive)
            {
                if (e != null) { e.Died -= OnEnemyDied; Destroy(e.gameObject); }
            }
            alive.Clear();
            foreach (GameObject g in spawnedObjects) if (g != null) Destroy(g);
            spawnedObjects.Clear();
            Projectile.ClearAll();
            pendingSpawns = 0;
            Started = false;
            if (Active == this) Active = null;
            if (hasSnapshot && PlayerCombat.Instance != null) PlayerCombat.Instance.Restore(snapshot);
            if (entranceGate != null) entranceGate.SetOpen(true);
            if (CombatHUD.Instance != null) CombatHUD.Instance.HideEnemies();
        }

        /// <summary>
        /// Soma dos inimigos de todas as salas do nível.
        /// </summary>
        /// <returns>Total de inimigos previstos.</returns>
        public static int TotalEnemiesInLevel()
        {
            int t = 0;
            foreach (CombatRoom r in AllRooms) if (r != null) t += r.Total;
            return t;
        }

        /// <summary>
        /// Limpa o estado global (início do nível).
        /// </summary>
        public static void ResetStatics()
        {
            Active = null;
        }
    }
}
