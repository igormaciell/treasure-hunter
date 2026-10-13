using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TreasureHunter
{
    /// <summary>
    /// Camada única de leitura de entrada (teclado e mouse) do jogo.
    /// Funciona tanto com o novo Input System (padrão do template URP do Unity 6)
    /// quanto com o Input Manager antigo, escolhendo automaticamente pelo define de compilação.
    /// Todos os scripts consultam esta classe em vez de acessar o teclado diretamente.
    /// </summary>
    public static class GameInput
    {
        /// <summary>Multiplicador aplicado ao delta do mouse do novo Input System para aproximar a escala do Input Manager antigo.</summary>
        private const float NewInputMouseScale = 0.05f;

        /// <summary>
        /// Lê o vetor de movimento (WASD ou setas).
        /// </summary>
        /// <returns>Vetor 2D normalizado: x = lateral (A/D), y = frente/trás (W/S).</returns>
        public static Vector2 Move()
        {
            float x = 0f, y = 0f;
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k != null)
            {
                // Soma as contribuições de cada tecla para formar os eixos.
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) x -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) y -= 1f;
                if (k.wKey.isPressed || k.upArrowKey.isPressed) y += 1f;
            }
#else
            x = Input.GetAxisRaw("Horizontal");
            y = Input.GetAxisRaw("Vertical");
#endif
            Vector2 v = new Vector2(x, y);
            // Evita que a diagonal seja mais rápida que o movimento reto.
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        /// <summary>
        /// Lê o deslocamento do mouse no quadro atual, usado para girar a câmera.
        /// </summary>
        /// <returns>Delta do mouse (x = horizontal, y = vertical) já em escala uniforme.</returns>
        public static Vector2 Look()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse m = Mouse.current;
            return m != null ? m.delta.ReadValue() * NewInputMouseScale : Vector2.zero;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
        }

        /// <summary>Indica se o salto (Espaço) foi pressionado neste quadro.</summary>
        /// <returns>true no quadro em que a tecla foi pressionada.</returns>
        public static bool JumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        /// <summary>Indica se o salto (Espaço) está sendo mantido pressionado (salto de altura variável).</summary>
        /// <returns>true enquanto a tecla estiver pressionada.</returns>
        public static bool JumpHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
#else
            return Input.GetKey(KeyCode.Space);
#endif
        }

        /// <summary>Indica se a corrida (Shift esquerdo) está ativa.</summary>
        /// <returns>true enquanto Shift estiver pressionado.</returns>
        public static bool RunHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
#else
            return Input.GetKey(KeyCode.LeftShift);
#endif
        }

        /// <summary>Indica se a interação (E) foi pressionada neste quadro.</summary>
        /// <returns>true no quadro em que E foi pressionado.</returns>
        public static bool InteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        /// <summary>Indica se o jogador pediu para erguer/abaixar a tocha (F ou botão esquerdo do mouse).</summary>
        /// <returns>true no quadro em que F ou o clique esquerdo ocorreu.</returns>
        public static bool ToolPressed()
        {
#if ENABLE_INPUT_SYSTEM
            bool key = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
            bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            return key || mouse;
#else
            return Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(0);
#endif
        }

        /// <summary>Indica se a tecla de troca de ferramenta (Q) foi pressionada.</summary>
        /// <returns>true no quadro em que Q foi pressionado.</returns>
        public static bool SwitchToolPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Q);
#endif
        }

        /// <summary>Indica se a pausa (Esc) foi pressionada.</summary>
        /// <returns>true no quadro em que Esc foi pressionado.</returns>
        public static bool PausePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        /// <summary>Indica se o Diário de Habilidades (Tab) foi aberto/fechado.</summary>
        /// <returns>true no quadro em que Tab foi pressionado.</returns>
        public static bool JournalPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Tab);
#endif
        }
    }
}
