using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreasureHunter
{
    /// <summary>
    /// Menu principal: começa o Nível 1, mostra os controles e fecha o jogo.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        /// <summary>Nome da cena do Nível 1.</summary>
        public string levelScene = "Level1";
        public Button playButton;
        public Button controlsButton;
        public Button quitButton;
        public Button closeControlsButton;
        public GameObject controlsPanel;
        /// <summary>Som de clique (opcional).</summary>
        public AudioSource clickSource;

        /// <summary>
        /// Liga os botões e libera o cursor do mouse.
        /// </summary>
        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playButton != null) playButton.onClick.AddListener(Play);
            if (controlsButton != null) controlsButton.onClick.AddListener(() => ShowControls(true));
            if (closeControlsButton != null) closeControlsButton.onClick.AddListener(() => ShowControls(false));
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
            if (controlsPanel != null) controlsPanel.SetActive(false);
        }

        /// <summary>
        /// Carrega a cena do Nível 1.
        /// </summary>
        public void Play()
        {
            Click();
            SceneManager.LoadScene(levelScene);
        }

        /// <summary>
        /// Mostra/esconde o painel de controles.
        /// </summary>
        /// <param name="show">true para mostrar.</param>
        public void ShowControls(bool show)
        {
            Click();
            if (controlsPanel != null) controlsPanel.SetActive(show);
        }

        /// <summary>
        /// Fecha o jogo (no editor, para o modo Play).
        /// </summary>
        public void Quit()
        {
            Click();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Toca o som de clique, se houver.
        /// </summary>
        private void Click()
        {
            if (clickSource != null) clickSource.Play();
        }
    }
}
