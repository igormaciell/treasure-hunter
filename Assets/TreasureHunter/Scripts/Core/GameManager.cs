using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TreasureHunter
{
    /// <summary>
    /// Estados possíveis da partida.
    /// </summary>
    public enum GameState { Playing, Paused, GameOver, LevelComplete, Cutscene }

    /// <summary>
    /// Interface para objetos que voltam ao estado inicial quando o jogador reinicia a sala
    /// (por exemplo, caixotes empurrados para o lugar errado).
    /// </summary>
    public interface IResettable
    {
        /// <summary>Índice da sala à qual o objeto pertence.</summary>
        int RoomIndex { get; }
        /// <summary>Restaura posição e estado iniciais.</summary>
        void ResetState();
    }

    /// <summary>
    /// Controlador central da partida: estado do jogo (jogando, pausado, derrota, vitória),
    /// pontuação (moedas, joias, artefatos), ponto de controle (checkpoint), reinício
    /// da sala/nível e retorno ao menu principal.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>Instância única acessível por qualquer script.</summary>
        public static GameManager Instance { get; private set; }

        [Header("Referências")]
        public PlayerController player;
        public PlayerHealth playerHealth;
        public UIManager ui;

        [Header("Nível")]
        public string levelName = "Nível 1 — A Entrada do Templo";
        public string mainMenuScene = "MainMenu";
        public int totalArtifacts = 3;
        [TextArea] public string startObjective = "Encontre o templo escondido na floresta.";

        /// <summary>Estado atual da partida.</summary>
        public GameState State { get; private set; } = GameState.Playing;
        /// <summary>Moedas coletadas.</summary>
        public int Coins { get; private set; }
        /// <summary>Joias coletadas.</summary>
        public int Gems { get; private set; }
        /// <summary>Nomes dos artefatos encontrados.</summary>
        public List<string> Artifacts { get; } = new List<string>();
        /// <summary>Índice da sala atual (usado para reiniciar a sala).</summary>
        public int CurrentRoom { get; private set; }

        private Vector3 checkpointPosition;
        private Quaternion checkpointRotation;
        private float levelTimer;
        private bool journalOpen;
        private readonly List<IResettable> resettables = new List<IResettable>();

        /// <summary>
        /// Registra o singleton e garante que o tempo do jogo esteja normal.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Define o checkpoint inicial na posição do jogador, prende o cursor
        /// e coleta todos os objetos reiniciáveis da cena.
        /// </summary>
        private void Start()
        {
            if (player != null)
            {
                checkpointPosition = player.transform.position;
                checkpointRotation = player.transform.rotation;
            }

            // Procura na cena todos os componentes que implementam IResettable.
            foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is IResettable r) resettables.Add(r);
            }

            SetCursorLocked(true);
            if (ui != null)
            {
                ui.SetObjective(startObjective);
                ui.UpdateTreasure(Coins, Gems, Artifacts.Count, totalArtifacts);
            }
        }

        /// <summary>
        /// Conta o tempo de jogo e trata as teclas globais (pausa e diário).
        /// </summary>
        private void Update()
        {
            if (State == GameState.Playing) levelTimer += Time.deltaTime;

            // Esc alterna a pausa (somente quando jogando ou já pausado).
            if (GameInput.PausePressed())
            {
                if (journalOpen) ToggleJournal();
                else if (State == GameState.Playing) SetPaused(true);
                else if (State == GameState.Paused) SetPaused(false);
            }

            // Tab abre o Diário de Habilidades.
            if (GameInput.JournalPressed() && (State == GameState.Playing || journalOpen)) ToggleJournal();
        }

        /// <summary>
        /// Abre ou fecha o Diário de Habilidades, pausando o tempo enquanto aberto.
        /// </summary>
        public void ToggleJournal()
        {
            journalOpen = !journalOpen;
            Time.timeScale = journalOpen ? 0f : 1f;
            if (ui != null) ui.ShowJournal(journalOpen);
        }

        /// <summary>
        /// Trava (ou libera) o cursor do mouse no centro da tela.
        /// </summary>
        /// <param name="locked">true para esconder e travar o cursor durante a jogabilidade.</param>
        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        /// <summary>
        /// Indica se o jogador pode controlar o personagem neste momento.
        /// </summary>
        /// <returns>true somente no estado Playing e com o diário fechado.</returns>
        public bool CanPlayerAct()
        {
            return State == GameState.Playing && !journalOpen;
        }

        /// <summary>
        /// Entra ou sai do estado de cena (cutscene), em que o jogador não controla o personagem.
        /// </summary>
        /// <param name="active">true para iniciar a cena; false para devolver o controle.</param>
        public void SetCutscene(bool active)
        {
            if (active && State == GameState.Playing) State = GameState.Cutscene;
            else if (!active && State == GameState.Cutscene) State = GameState.Playing;
        }

        /// <summary>
        /// Pausa ou retoma o jogo, exibindo o menu de pausa.
        /// </summary>
        /// <param name="paused">true para pausar; false para retomar.</param>
        public void SetPaused(bool paused)
        {
            if (State == GameState.GameOver || State == GameState.LevelComplete) return;
            State = paused ? GameState.Paused : GameState.Playing;
            Time.timeScale = paused ? 0f : 1f;
            SetCursorLocked(!paused);
            if (ui != null) ui.ShowPause(paused);
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicMuffled(paused);
        }

        /// <summary>
        /// Atualiza o ponto de controle onde o jogador renasce.
        /// </summary>
        /// <param name="position">Posição de renascimento.</param>
        /// <param name="rotation">Rotação de renascimento.</param>
        /// <param name="roomIndex">Índice da sala do checkpoint.</param>
        public void SetCheckpoint(Vector3 position, Quaternion rotation, int roomIndex)
        {
            checkpointPosition = position;
            checkpointRotation = rotation;
            CurrentRoom = roomIndex;
        }

        /// <summary>
        /// Teleporta o jogador para o último checkpoint e zera sua velocidade.
        /// </summary>
        public void RespawnPlayer()
        {
            if (player == null) return;
            player.Teleport(checkpointPosition, checkpointRotation);
        }

        /// <summary>
        /// Reinicia a sala atual: devolve os objetos da sala ao estado inicial e
        /// leva o jogador ao checkpoint. Chamado pelo botão "Reiniciar sala".
        /// </summary>
        public void RestartRoom()
        {
            // Restaura somente os objetos que pertencem à sala atual.
            foreach (IResettable r in resettables)
            {
                if (r != null && r.RoomIndex == CurrentRoom) r.ResetState();
            }
            RespawnPlayer();
            if (State == GameState.Paused) SetPaused(false);
            if (State == GameState.GameOver)
            {
                // Volta da derrota com a vida cheia.
                State = GameState.Playing;
                Time.timeScale = 1f;
                SetCursorLocked(true);
                if (playerHealth != null) playerHealth.ResetHealth();
                if (ui != null) ui.ShowGameOver(false);
            }
        }

        /// <summary>
        /// Recarrega a cena atual do zero (botão "Reiniciar nível/jogo").
        /// </summary>
        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Volta para a cena do menu principal.
        /// </summary>
        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SetCursorLocked(false);
            SceneManager.LoadScene(mainMenuScene);
        }

        /// <summary>
        /// Registra a coleta de uma moeda ou joia.
        /// </summary>
        /// <param name="type">Tipo do coletável.</param>
        /// <param name="amount">Quantidade somada.</param>
        public void AddTreasure(CollectibleType type, int amount)
        {
            if (type == CollectibleType.Coin) Coins += amount;
            else if (type == CollectibleType.Gem) Gems += amount;
            if (ui != null) ui.UpdateTreasure(Coins, Gems, Artifacts.Count, totalArtifacts);
        }

        /// <summary>
        /// Registra a descoberta de um artefato e mostra um aviso na tela.
        /// </summary>
        /// <param name="artifactName">Nome do artefato encontrado.</param>
        public void AddArtifact(string artifactName)
        {
            if (Artifacts.Contains(artifactName)) return;
            Artifacts.Add(artifactName);
            if (ui != null)
            {
                ui.UpdateTreasure(Coins, Gems, Artifacts.Count, totalArtifacts);
                ui.ShowToast("Artefato encontrado!", artifactName + "  (" + Artifacts.Count + "/" + totalArtifacts + ")");
            }
        }

        /// <summary>
        /// Encerra a partida com derrota (vida zerada) e mostra a tela de fim de jogo.
        /// </summary>
        public void GameOver()
        {
            if (State == GameState.GameOver) return;
            State = GameState.GameOver;
            Time.timeScale = 0f;
            SetCursorLocked(false);
            if (ui != null) ui.ShowGameOver(true);
        }

        /// <summary>
        /// Conclui o nível e mostra o resumo (artefatos, tesouros, vida, habilidades e tempo).
        /// </summary>
        public void CompleteLevel()
        {
            if (State == GameState.LevelComplete) return;
            State = GameState.LevelComplete;
            SetCursorLocked(false);
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(AudioManager.Instance.levelComplete);

            // Monta o texto do resumo exibido na tela de transição de nível.
            int minutes = Mathf.FloorToInt(levelTimer / 60f);
            int seconds = Mathf.FloorToInt(levelTimer % 60f);
            int hearts = playerHealth != null ? playerHealth.Current : 0;
            int maxHearts = playerHealth != null ? playerHealth.maxHearts : 0;
            int skills = SkillManager.Instance != null ? SkillManager.Instance.UnlockedCount : 0;
            int totalSkills = SkillManager.Instance != null ? SkillManager.Instance.TotalCount : 0;

            string summary =
                "Artefatos: " + Artifacts.Count + " / " + totalArtifacts + "\n" +
                (Artifacts.Count > 0 ? "   " + string.Join(", ", Artifacts) + "\n" : "") +
                "Moedas: " + Coins + "     Joias: " + Gems + "\n" +
                "Vida restante: " + hearts + " / " + maxHearts + " corações\n" +
                "Habilidades aprendidas: " + skills + " / " + totalSkills + "\n" +
                "Tempo: " + minutes.ToString("00") + ":" + seconds.ToString("00");

            if (ui != null) ui.ShowLevelComplete(levelName, summary);
        }
    }
}
