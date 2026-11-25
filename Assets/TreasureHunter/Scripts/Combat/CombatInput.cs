using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TreasureHunter
{
    /// <summary>
    /// Entradas do combate (Nível 2), no mesmo padrão do GameInput: funciona com o novo
    /// Input System e com o Input Manager antigo. Fica separado do GameInput para não alterar
    /// os controles do Nível 1.
    /// Controles: clique esquerdo atira/golpeia, botão direito mira, R recarrega, V coronhada/golpe,
    /// G arremessa pedra, 1 revólver, 2 faca, Q ou roda do mouse troca de arma.
    /// </summary>
    public static class CombatInput
    {
        /// <summary>Clique esquerdo pressionado neste quadro (atirar / golpear com a faca).</summary>
        /// <returns>true no quadro do clique.</returns>
        public static bool FirePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        /// <summary>Botão direito mantido (mirar).</summary>
        /// <returns>true enquanto o botão estiver pressionado.</returns>
        public static bool AimHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
            return Input.GetMouseButton(1);
#endif
        }

        /// <summary>Tecla R (recarregar o tambor).</summary>
        /// <returns>true no quadro em que R foi pressionado.</returns>
        public static bool ReloadPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }

        /// <summary>Tecla V (golpe corpo a corpo: coronhada ou faca).</summary>
        /// <returns>true no quadro em que V foi pressionado.</returns>
        public static bool MeleePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.V);
#endif
        }

        /// <summary>Tecla G (arremessar pedra).</summary>
        /// <returns>true no quadro em que G foi pressionado.</returns>
        public static bool ThrowPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.G);
#endif
        }

        /// <summary>Tecla 1 (equipar o revólver).</summary>
        /// <returns>true no quadro em que 1 foi pressionado.</returns>
        public static bool Weapon1Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Alpha1);
#endif
        }

        /// <summary>Tecla 2 (equipar a faca).</summary>
        /// <returns>true no quadro em que 2 foi pressionado.</returns>
        public static bool Weapon2Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Alpha2);
#endif
        }

        /// <summary>Q ou roda do mouse (alternar entre as armas).</summary>
        /// <returns>true no quadro da troca.</returns>
        public static bool NextWeaponPressed()
        {
#if ENABLE_INPUT_SYSTEM
            bool q = Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
            bool wheel = Mouse.current != null && Mathf.Abs(Mouse.current.scroll.ReadValue().y) > 0.1f;
            return q || wheel;
#else
            return Input.GetKeyDown(KeyCode.Q) || Mathf.Abs(Input.mouseScrollDelta.y) > 0.1f;
#endif
        }
    }
}
