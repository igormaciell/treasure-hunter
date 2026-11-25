using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreasureHunter
{
    /// <summary>
    /// Menu simples de uma tela (usado na cena provisória do Nível 3): cada botão carrega uma cena.
    /// </summary>
    public class SimpleSceneMenu : MonoBehaviour
    {
        /// <summary>Ligação entre um botão e a cena que ele abre.</summary>
        [Serializable]
        public class SceneButton
        {
            public Button button;
            public string sceneName;
        }

        /// <summary>Botões do menu.</summary>
        public SceneButton[] buttons;
        /// <summary>Som de clique (opcional).</summary>
        public AudioSource clickSource;

        /// <summary>
        /// Libera o cursor e liga os botões.
        /// </summary>
        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (buttons == null) return;
            foreach (SceneButton b in buttons)
            {
                if (b == null || b.button == null) continue;
                string scene = b.sceneName;
                b.button.onClick.AddListener(() => Load(scene));
            }
        }

        /// <summary>
        /// Carrega uma cena pelo nome.
        /// </summary>
        /// <param name="scene">Nome da cena.</param>
        private void Load(string scene)
        {
            if (clickSource != null) clickSource.Play();
            if (!string.IsNullOrEmpty(scene)) SceneManager.LoadScene(scene);
        }
    }
}
