using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureHunter
{
    /// <summary>
    /// Controla toda a interface do Nível 1: HUD (corações, ferramenta, tesouros, missão, prompt),
    /// avisos (habilidade nova, artefato), popups de tutorial, falas de John, título das salas,
    /// menu de pausa, tela de derrota, tela de nível concluído (com botão de reiniciar),
    /// Diário de Habilidades e o escurecimento de tela.
    /// As referências são preenchidas pelo gerador do nível.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        public Image[] hearts;
        public Sprite heartFull;
        public Sprite heartEmpty;
        public Text objectiveText;
        public Text treasureText;
        public Text toolText;
        public Image toolIcon;
        public GameObject promptPanel;
        public Text promptText;
        public Image damageFlash;

        [Header("Avisos")]
        public CanvasGroup toastGroup;
        public Text toastTitle;
        public Text toastBody;
        public CanvasGroup tutorialGroup;
        public Text tutorialText;
        public CanvasGroup lineGroup;
        public Text lineText;
        public CanvasGroup roomTitleGroup;
        public Text roomTitleText;
        public Text roomSubtitleText;

        [Header("Pausa")]
        public GameObject pausePanel;
        public Button resumeButton;
        public Button pauseRestartRoomButton;
        public Button pauseRestartLevelButton;
        public Button pauseMenuButton;

        [Header("Derrota")]
        public GameObject gameOverPanel;
        public Button gameOverContinueButton;
        public Button gameOverRestartButton;
        public Button gameOverMenuButton;

        [Header("Nível concluído")]
        public GameObject completePanel;
        public Text completeTitle;
        public Text completeSummary;
        public Button completeRestartButton;
        public Button completeMenuButton;

        [Header("Diário de Habilidades")]
        public GameObject journalPanel;
        public Text journalText;

        [Header("Transição")]
        public Image fadeImage;

        private struct ToastData { public string title; public string body; public float duration; }
        private readonly Queue<ToastData> toastQueue = new Queue<ToastData>();
        private float toastHideAt;
        private bool toastShowing;
        private float tutorialHideAt;
        private float lineHideAt;
        private float roomTitleHideAt;
        private float damageAlpha;

        /// <summary>
        /// Liga os botões às ações do GameManager e esconde os painéis.
        /// </summary>
        private void Awake()
        {
            Bind(resumeButton, () => GameManager.Instance.SetPaused(false));
            Bind(pauseRestartRoomButton, () => GameManager.Instance.RestartRoom());
            Bind(pauseRestartLevelButton, () => GameManager.Instance.RestartLevel());
            Bind(pauseMenuButton, () => GameManager.Instance.LoadMainMenu());
            Bind(gameOverContinueButton, () => GameManager.Instance.RestartRoom());
            Bind(gameOverRestartButton, () => GameManager.Instance.RestartLevel());
            Bind(gameOverMenuButton, () => GameManager.Instance.LoadMainMenu());
            Bind(completeRestartButton, () => GameManager.Instance.RestartLevel());
            Bind(completeMenuButton, () => GameManager.Instance.LoadMainMenu());

            SetActive(pausePanel, false);
            SetActive(gameOverPanel, false);
            SetActive(completePanel, false);
            SetActive(journalPanel, false);
            SetActive(promptPanel, false);
            SetGroup(toastGroup, 0f);
            SetGroup(tutorialGroup, 0f);
            SetGroup(lineGroup, 0f);
            SetGroup(roomTitleGroup, 0f);
            if (damageFlash != null) damageFlash.color = new Color(0.8f, 0f, 0f, 0f);
        }

        /// <summary>
        /// Inscreve-se no evento de habilidades e começa com a tela clara.
        /// </summary>
        private void Start()
        {
            if (SkillManager.Instance != null) SkillManager.Instance.OnSkillUnlocked += HandleSkillUnlocked;
            if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, 0f);
            RefreshJournal();
        }

        /// <summary>
        /// Cancela a inscrição no evento ao destruir a interface.
        /// </summary>
        private void OnDestroy()
        {
            if (SkillManager.Instance != null) SkillManager.Instance.OnSkillUnlocked -= HandleSkillUnlocked;
        }

        /// <summary>
        /// Atualiza os temporizadores e as transparências dos avisos (usa tempo real, funciona na pausa).
        /// </summary>
        private void Update()
        {
            float now = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            // Fila de avisos: mostra o próximo quando o atual termina.
            if (toastShowing && now >= toastHideAt) toastShowing = false;
            if (!toastShowing && toastQueue.Count > 0 && (toastGroup == null || toastGroup.alpha < 0.05f)) ShowNextToast();

            FadeGroup(toastGroup, toastShowing, dt);
            FadeGroup(tutorialGroup, now < tutorialHideAt, dt);
            FadeGroup(lineGroup, now < lineHideAt, dt);
            FadeGroup(roomTitleGroup, now < roomTitleHideAt, dt);

            // O flash vermelho de dano desaparece aos poucos.
            if (damageFlash != null && damageAlpha > 0f)
            {
                damageAlpha = Mathf.Max(0f, damageAlpha - dt * 1.5f);
                damageFlash.color = new Color(0.8f, 0f, 0f, damageAlpha);
            }
        }

        // ---------------------------------------------------------------- HUD

        /// <summary>
        /// Desenha os corações cheios/vazios.
        /// </summary>
        /// <param name="current">Corações atuais.</param>
        /// <param name="max">Corações máximos.</param>
        public void UpdateHearts(int current, int max)
        {
            if (hearts == null) return;
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] == null) continue;
                hearts[i].gameObject.SetActive(i < max);
                hearts[i].sprite = i < current ? heartFull : heartEmpty;
            }
        }

        /// <summary>
        /// Atualiza o contador de tesouros.
        /// </summary>
        /// <param name="coins">Moedas.</param>
        /// <param name="gems">Joias.</param>
        /// <param name="artifacts">Artefatos encontrados.</param>
        /// <param name="totalArtifacts">Artefatos existentes no nível.</param>
        public void UpdateTreasure(int coins, int gems, int artifacts, int totalArtifacts)
        {
            if (treasureText != null)
                treasureText.text = "Moedas " + coins + "   Joias " + gems + "   Artefatos " + artifacts + "/" + totalArtifacts;
        }

        /// <summary>
        /// Mostra a ferramenta ativa e seu estado.
        /// </summary>
        /// <param name="toolName">Nome da ferramenta.</param>
        /// <param name="active">Se a ferramenta está em uso (tocha erguida).</param>
        public void SetTool(string toolName, bool active)
        {
            if (toolText != null) toolText.text = toolName + (active ? " (erguida)" : "") + "\n[F] erguer";
            if (toolIcon != null) toolIcon.color = active ? new Color(1f, 0.85f, 0.4f) : Color.white;
        }

        /// <summary>
        /// Define o texto da missão atual.
        /// </summary>
        /// <param name="text">Texto da missão.</param>
        public void SetObjective(string text)
        {
            if (objectiveText != null) objectiveText.text = "Missão: " + text;
        }

        /// <summary>
        /// Mostra o prompt de interação (ex.: "[E] Puxar alavanca").
        /// </summary>
        /// <param name="text">Texto do prompt.</param>
        public void ShowPrompt(string text)
        {
            SetActive(promptPanel, true);
            if (promptText != null) promptText.text = text;
        }

        /// <summary>
        /// Esconde o prompt de interação.
        /// </summary>
        public void HidePrompt()
        {
            SetActive(promptPanel, false);
        }

        /// <summary>
        /// Pisca a tela em vermelho ao levar dano.
        /// </summary>
        public void FlashDamage()
        {
            damageAlpha = 0.45f;
        }

        // ---------------------------------------------------------------- Avisos

        /// <summary>
        /// Coloca um aviso na fila (habilidade nova, artefato, inscrição).
        /// </summary>
        /// <param name="title">Título do aviso.</param>
        /// <param name="body">Texto do aviso.</param>
        /// <param name="duration">Tempo na tela, em segundos.</param>
        public void ShowToast(string title, string body, float duration = 3.5f)
        {
            toastQueue.Enqueue(new ToastData { title = title, body = body, duration = duration });
        }

        /// <summary>
        /// Exibe o próximo aviso da fila.
        /// </summary>
        private void ShowNextToast()
        {
            ToastData t = toastQueue.Dequeue();
            if (toastTitle != null) toastTitle.text = t.title;
            if (toastBody != null) toastBody.text = t.body;
            toastShowing = true;
            toastHideAt = Time.unscaledTime + t.duration;
        }

        /// <summary>
        /// Mostra um popup curto de tutorial.
        /// </summary>
        /// <param name="text">Texto da dica.</param>
        /// <param name="duration">Tempo na tela.</param>
        public void ShowTutorial(string text, float duration = 6f)
        {
            if (tutorialText != null) tutorialText.text = text;
            tutorialHideAt = Time.unscaledTime + duration;
        }

        /// <summary>
        /// Mostra uma fala de John (legenda na parte de baixo da tela).
        /// </summary>
        /// <param name="text">Fala.</param>
        /// <param name="duration">Tempo na tela.</param>
        public void ShowLine(string text, float duration = 4f)
        {
            if (lineText != null) lineText.text = "John: " + text;
            lineHideAt = Time.unscaledTime + duration;
        }

        /// <summary>
        /// Mostra o título grande de uma sala.
        /// </summary>
        /// <param name="title">Título.</param>
        /// <param name="subtitle">Subtítulo.</param>
        public void ShowRoomTitle(string title, string subtitle)
        {
            if (roomTitleText != null) roomTitleText.text = title;
            if (roomSubtitleText != null) roomSubtitleText.text = subtitle;
            roomTitleHideAt = Time.unscaledTime + 3.5f;
        }

        /// <summary>
        /// Reação ao evento de habilidade desbloqueada: aviso + atualização do diário.
        /// </summary>
        /// <param name="skill">Habilidade aprendida.</param>
        private void HandleSkillUnlocked(SkillDefinition skill)
        {
            ShowToast("Nova habilidade: " + skill.displayName, skill.description, 4f);
            RefreshJournal();
        }

        // ---------------------------------------------------------------- Painéis

        /// <summary>
        /// Mostra/esconde o menu de pausa.
        /// </summary>
        /// <param name="show">true para mostrar.</param>
        public void ShowPause(bool show)
        {
            SetActive(pausePanel, show);
        }

        /// <summary>
        /// Mostra/esconde a tela de derrota.
        /// </summary>
        /// <param name="show">true para mostrar.</param>
        public void ShowGameOver(bool show)
        {
            SetActive(gameOverPanel, show);
        }

        /// <summary>
        /// Mostra a tela de nível concluído com o resumo.
        /// </summary>
        /// <param name="title">Nome do nível.</param>
        /// <param name="summary">Texto do resumo.</param>
        public void ShowLevelComplete(string title, string summary)
        {
            if (completeTitle != null) completeTitle.text = title + "\nconcluído!";
            if (completeSummary != null) completeSummary.text = summary;
            SetActive(completePanel, true);
            HidePrompt();
        }

        /// <summary>
        /// Mostra/esconde o Diário de Habilidades.
        /// </summary>
        /// <param name="show">true para mostrar.</param>
        public void ShowJournal(bool show)
        {
            RefreshJournal();
            SetActive(journalPanel, show);
        }

        /// <summary>
        /// Reescreve o texto do diário com as habilidades aprendidas e pendentes.
        /// </summary>
        private void RefreshJournal()
        {
            if (journalText == null || SkillManager.Instance == null) return;
            SkillManager sm = SkillManager.Instance;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Habilidades aprendidas: " + sm.UnlockedCount + " / " + sm.TotalCount);
            sb.AppendLine();
            foreach (SkillDefinition s in sm.skills)
            {
                bool ok = sm.IsUnlocked(s.id);
                sb.AppendLine((ok ? "[X] " : "[  ] ") + (ok ? s.displayName : "???"));
                sb.AppendLine("      " + (ok ? s.description : "Continue explorando para descobrir."));
            }
            journalText.text = sb.ToString();
        }

        /// <summary>
        /// Escurece ou clareia a tela.
        /// </summary>
        /// <param name="targetAlpha">Opacidade final (1 = preto).</param>
        /// <param name="duration">Duração em segundos.</param>
        /// <returns>Corrotina em execução (pode ser aguardada com yield return).</returns>
        public Coroutine Fade(float targetAlpha, float duration)
        {
            return StartCoroutine(FadeRoutine(targetAlpha, duration));
        }

        /// <summary>
        /// Corrotina que interpola a opacidade da imagem preta de transição.
        /// </summary>
        /// <param name="targetAlpha">Opacidade final.</param>
        /// <param name="duration">Duração.</param>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator FadeRoutine(float targetAlpha, float duration)
        {
            if (fadeImage == null) yield break;
            float start = fadeImage.color.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(start, targetAlpha, t / duration);
                fadeImage.color = new Color(0f, 0f, 0f, a);
                yield return null;
            }
            fadeImage.color = new Color(0f, 0f, 0f, targetAlpha);
        }

        // ---------------------------------------------------------------- Auxiliares

        /// <summary>
        /// Liga um botão a uma ação, tocando o som de clique.
        /// </summary>
        /// <param name="button">Botão (pode ser nulo).</param>
        /// <param name="action">Ação executada no clique.</param>
        private void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.Play2D(AudioManager.Instance.uiClick);
                action();
            });
        }

        /// <summary>
        /// Ativa/desativa um GameObject com verificação de nulo.
        /// </summary>
        /// <param name="go">Objeto.</param>
        /// <param name="active">Estado desejado.</param>
        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }

        /// <summary>
        /// Define a opacidade de um CanvasGroup com verificação de nulo.
        /// </summary>
        /// <param name="group">Grupo.</param>
        /// <param name="alpha">Opacidade.</param>
        private static void SetGroup(CanvasGroup group, float alpha)
        {
            if (group != null) group.alpha = alpha;
        }

        /// <summary>
        /// Aproxima gradualmente a opacidade de um grupo de 0 ou 1.
        /// </summary>
        /// <param name="group">Grupo.</param>
        /// <param name="visible">Se deve ficar visível.</param>
        /// <param name="dt">Tempo do quadro.</param>
        private static void FadeGroup(CanvasGroup group, bool visible, float dt)
        {
            if (group == null) return;
            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, dt * 4f);
        }
    }
}
