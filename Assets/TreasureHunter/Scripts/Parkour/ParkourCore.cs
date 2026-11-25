using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TreasureHunter
{
    /// <summary>
    /// Tags usadas pelo parkour (Nível 3 em diante).
    /// </summary>
    public static class ParkourTags
    {
        /// <summary>Superfície que se move, cai ou quebra: não aceita o totem.</summary>
        public const string Unstable = "Unstable";
        /// <summary>Cristal (plataforma, decoração ou o cristal do totem).</summary>
        public const string Crystal = "Crystal";
        /// <summary>Pedra de checkpoint fixo.</summary>
        public const string Checkpoint = "Checkpoint";
        /// <summary>Totem de checkpoint (colocado, fantasma ou item).</summary>
        public const string Totem = "Totem";

        /// <summary>Todas as tags do parkour (o gerador registra no TagManager).</summary>
        public static readonly string[] All = { Unstable, Crystal, Checkpoint, Totem };
    }

    /// <summary>
    /// Layers do parkour e máscaras de Raycast derivadas delas.
    /// </summary>
    public static class ParkourLayers
    {
        /// <summary>Layer do totem e do seu fantasma (ignorados na validação de superfície e pela câmera).</summary>
        public const string TotemLayerName = "Totem";
        /// <summary>Layer dos itens e gatilhos de interação (criada no Nível 2).</summary>
        public const string InteractableLayerName = "Interactable";
        /// <summary>Layer do jogador (Ignore Raycast).</summary>
        public const int PlayerLayer = 2;

        private static int totem = -2, interactable = -2;

        /// <summary>Índice da layer do totem (−1 se não existir).</summary>
        public static int Totem => Get(ref totem, TotemLayerName);
        /// <summary>Índice da layer de interação (−1 se não existir).</summary>
        public static int Interactable => Get(ref interactable, InteractableLayerName);

        /// <summary>Superfícies sólidas onde se pode pisar, agarrar e colocar o totem.</summary>
        public static int SurfaceMask => Physics.DefaultRaycastLayers & ~Bit(Totem) & ~Bit(Interactable) & ~(1 << PlayerLayer);
        /// <summary>O que a câmera evita atravessar.</summary>
        public static int CameraMask => SurfaceMask;

        private static int Get(ref int cache, string name)
        {
            if (cache == -2) cache = LayerMask.NameToLayer(name);
            return cache;
        }

        private static int Bit(int layer)
        {
            return layer >= 0 ? 1 << layer : 0;
        }

        /// <summary>
        /// Limpa o cache (o editor chama depois de criar as layers).
        /// </summary>
        public static void ResetCache()
        {
            totem = interactable = -2;
        }
    }

    /// <summary>
    /// Progresso salvo entre os níveis (PlayerPrefs). O totem coletado no Nível 3 continua disponível
    /// no Nível 5 mesmo depois de fechar o jogo.
    /// </summary>
    public static class PlayerProgress
    {
        private const string TotemKey = "TreasureHunter.Progress.Totem";

        /// <summary>O jogador já coletou o Totem de Checkpoint em alguma partida.</summary>
        public static bool HasTotem => PlayerPrefs.GetInt(TotemKey, 0) == 1;

        /// <summary>
        /// Registra que o totem foi coletado.
        /// </summary>
        public static void UnlockTotem()
        {
            PlayerPrefs.SetInt(TotemKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Apaga o progresso salvo (útil para testar do zero).
        /// </summary>
        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(TotemKey);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Entradas do parkour (tecla T do totem), nos dois sistemas de entrada do Unity.
    /// </summary>
    public static class ParkourInput
    {
        /// <summary>T está pressionado (modo de posicionamento do totem).</summary>
        /// <returns>true enquanto T estiver pressionado.</returns>
        public static bool TotemHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.tKey.isPressed;
#else
            return Input.GetKey(KeyCode.T);
#endif
        }

        /// <summary>Cancela o posicionamento (botão direito do mouse).</summary>
        /// <returns>true no quadro em que o botão direito foi pressionado.</returns>
        public static bool CancelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(1);
#endif
        }
    }

    /// <summary>
    /// Objetos que reagem quando John pousa ou fica em cima deles (plataformas frágeis, cristais que brilham).
    /// </summary>
    public interface ILandingReceiver
    {
        /// <summary>John acabou de pousar sobre este objeto.</summary>
        /// <param name="player">Parkour do jogador.</param>
        /// <param name="dropHeight">Altura da queda até o pouso (m).</param>
        void OnPlayerLanded(ParkourController player, float dropHeight);

        /// <summary>John continua em pé sobre este objeto (chamado a cada passo de física).</summary>
        /// <param name="player">Parkour do jogador.</param>
        void OnPlayerStanding(ParkourController player);
    }

    /// <summary>
    /// Funções de apoio do parkour.
    /// </summary>
    public static class ParkourUtil
    {
        /// <summary>
        /// Instancia todos os efeitos de um vetor de prefabs num ponto.
        /// </summary>
        /// <param name="effects">Vetor de prefabs.</param>
        /// <param name="position">Posição.</param>
        public static void SpawnAll(GameObject[] effects, Vector3 position)
        {
            if (effects == null) return;
            foreach (GameObject fx in effects)
                if (fx != null) Object.Instantiate(fx, position, Quaternion.identity);
        }

        /// <summary>
        /// Instancia um efeito sorteado do vetor de prefabs.
        /// </summary>
        /// <param name="effects">Vetor de prefabs.</param>
        /// <param name="position">Posição.</param>
        /// <returns>A instância (ou nulo).</returns>
        public static GameObject SpawnRandom(GameObject[] effects, Vector3 position)
        {
            if (effects == null || effects.Length == 0) return null;
            GameObject fx = effects[Random.Range(0, effects.Length)];
            return fx != null ? Object.Instantiate(fx, position, Quaternion.identity) : null;
        }

        /// <summary>
        /// Mostra uma dica curta no HUD do parkour (ou no tutorial, se o HUD não existir).
        /// </summary>
        /// <param name="text">Texto.</param>
        public static void Hint(string text)
        {
            if (ParkourHUD.Instance != null) ParkourHUD.Instance.ShowHint(text);
            else if (GameManager.Instance != null && GameManager.Instance.ui != null) GameManager.Instance.ui.ShowTutorial(text, 3f);
        }

        /// <summary>
        /// Desbloqueia uma habilidade no diário (se existir na lista do nível).
        /// </summary>
        /// <param name="id">Identificador da habilidade.</param>
        public static void UnlockSkill(string id)
        {
            if (!string.IsNullOrEmpty(id) && SkillManager.Instance != null) SkillManager.Instance.Unlock(id);
        }
    }
}
