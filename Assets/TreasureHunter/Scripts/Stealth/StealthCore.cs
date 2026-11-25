using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TreasureHunter
{
    /// <summary>
    /// Tipos de ruído que o jogador (ou o cenário) pode produzir. O tipo muda a reação de quem ouve:
    /// passos e pousos deixam o ogro desconfiado; um disparo revela na hora a posição de quem atirou.
    /// </summary>
    public enum NoiseType
    {
        /// <summary>Passos (andando, agachado ou em chão barulhento).</summary>
        Footstep,
        /// <summary>Passos correndo.</summary>
        Run,
        /// <summary>Impulso do pulo.</summary>
        Jump,
        /// <summary>Pouso depois de um pulo ou queda.</summary>
        Land,
        /// <summary>Disparo de arma de fogo (denuncia a origem imediatamente).</summary>
        Gunshot,
        /// <summary>Impacto de objeto arremessado (pedra de distração).</summary>
        Impact,
        /// <summary>Golpe corpo a corpo.</summary>
        Melee,
        /// <summary>Explosão ou desabamento.</summary>
        Explosion,
        /// <summary>Tocha acesa ou apagada, objetos mexidos.</summary>
        Handling
    }

    /// <summary>
    /// Um ruído no mundo: onde, quão longe se espalha, de que tipo e quem o fez.
    /// </summary>
    public struct NoiseEvent
    {
        /// <summary>Origem do som.</summary>
        public Vector3 position;
        /// <summary>Raio (m) em que o som ainda é percebido sem obstáculos.</summary>
        public float radius;
        /// <summary>Tipo do ruído.</summary>
        public NoiseType type;
        /// <summary>Objeto que causou o ruído (pode ser nulo).</summary>
        public GameObject source;
        /// <summary>true se foi o jogador (e não o cenário).</summary>
        public bool byPlayer;

        /// <summary>
        /// Cria um evento de ruído.
        /// </summary>
        /// <param name="position">Origem.</param>
        /// <param name="radius">Raio.</param>
        /// <param name="type">Tipo.</param>
        /// <param name="source">Quem fez o ruído.</param>
        /// <param name="byPlayer">true se foi o jogador.</param>
        public NoiseEvent(Vector3 position, float radius, NoiseType type, GameObject source, bool byPlayer)
        {
            this.position = position;
            this.radius = radius;
            this.type = type;
            this.source = source;
            this.byPlayer = byPlayer;
        }
    }

    /// <summary>
    /// Quem pode ouvir ruídos (implementado pelo <see cref="HearingSensor"/>).
    /// </summary>
    public interface INoiseListener
    {
        /// <summary>Um ruído aconteceu no mundo.</summary>
        /// <param name="noise">Dados do ruído.</param>
        void OnNoise(NoiseEvent noise);
    }

    /// <summary>
    /// Sistema de ruído compartilhado: qualquer script emite um ruído e todos os ouvintes registrados
    /// (ogros, e também os esqueletos do Nível 2 por compatibilidade) decidem se ouviram.
    /// </summary>
    public static class NoiseSystem
    {
        private static readonly List<INoiseListener> Listeners = new List<INoiseListener>();

        /// <summary>Último ruído produzido pelo jogador (o HUD mostra a intensidade).</summary>
        public static NoiseEvent LastPlayerNoise { get; private set; }
        /// <summary>Instante do último ruído do jogador.</summary>
        public static float LastPlayerNoiseTime { get; private set; } = -100f;
        /// <summary>Disparado a cada ruído (efeitos, HUD).</summary>
        public static event System.Action<NoiseEvent> Emitted;

        /// <summary>
        /// Registra um ouvinte.
        /// </summary>
        /// <param name="listener">Ouvinte.</param>
        public static void Register(INoiseListener listener)
        {
            if (listener != null && !Listeners.Contains(listener)) Listeners.Add(listener);
        }

        /// <summary>
        /// Remove um ouvinte.
        /// </summary>
        /// <param name="listener">Ouvinte.</param>
        public static void Unregister(INoiseListener listener)
        {
            Listeners.Remove(listener);
        }

        /// <summary>
        /// Emite um ruído: avisa os ouvintes registrados e os esqueletos do Nível 2 dentro do raio.
        /// </summary>
        /// <param name="position">Origem.</param>
        /// <param name="radius">Raio de alcance (m).</param>
        /// <param name="type">Tipo.</param>
        /// <param name="source">Quem fez o ruído.</param>
        /// <param name="byPlayer">true se foi o jogador.</param>
        public static void Emit(Vector3 position, float radius, NoiseType type, GameObject source = null, bool byPlayer = true)
        {
            if (radius <= 0f) return;
            NoiseEvent e = new NoiseEvent(position, radius, type, source, byPlayer);
            if (byPlayer)
            {
                LastPlayerNoise = e;
                LastPlayerNoiseTime = Time.time;
            }
            // Os esqueletos do Nível 2 continuam ouvindo como antes (só sons fortes os acordam).
            if (type == NoiseType.Gunshot || type == NoiseType.Impact || type == NoiseType.Explosion || type == NoiseType.Melee)
                EnemyBase.MakeNoise(position, radius);
            for (int i = Listeners.Count - 1; i >= 0; i--)
            {
                INoiseListener l = Listeners[i];
                if (l == null || (l is Object o && o == null)) { Listeners.RemoveAt(i); continue; }
                l.OnNoise(e);
            }
            Emitted?.Invoke(e);
        }

        /// <summary>
        /// Zera o último ruído do jogador (nova partida). Os ouvintes não são apagados: cada um se registra no
        /// OnEnable e os destruídos são removidos sozinhos na próxima emissão.
        /// </summary>
        public static void ResetPlayerNoise()
        {
            LastPlayerNoiseTime = -100f;
        }
    }

    /// <summary>
    /// Entradas da furtividade nos dois sistemas de entrada do Unity.
    /// </summary>
    public static class StealthInput
    {
        /// <summary>C alterna agachar/levantar.</summary>
        /// <returns>true no quadro em que C foi pressionado.</returns>
        public static bool CrouchTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.C);
#endif
        }

        /// <summary>Ctrl esquerdo: agachado enquanto estiver pressionado.</summary>
        /// <returns>true enquanto Ctrl estiver pressionado.</returns>
        public static bool CrouchHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;
#else
            return Input.GetKey(KeyCode.LeftControl);
#endif
        }

        /// <summary>F acende/apaga a tocha (no Nível 4 o clique esquerdo é do revólver).</summary>
        /// <returns>true no quadro em que F foi pressionado.</returns>
        public static bool TorchPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F);
#endif
        }
    }

    /// <summary>
    /// Tags e camadas usadas pela furtividade.
    /// </summary>
    public static class StealthTags
    {
        /// <summary>Ogros (inimigos que não devem ser enfrentados).</summary>
        public const string Ogre = "Ogre";
        /// <summary>Objetos que bloqueiam a visão e servem de cobertura.</summary>
        public const string Cover = "Cover";
        /// <summary>Esconderijos (vegetação alta, fendas).</summary>
        public const string HidingSpot = "HidingSpot";
        /// <summary>Áreas seguras onde os ogros não entram.</summary>
        public const string SafeZone = "SafeZone";

        /// <summary>Todas as tags da furtividade.</summary>
        public static readonly string[] All = { Ogre, Cover, HidingSpot, SafeZone };
    }

    /// <summary>
    /// Funções de apoio da furtividade.
    /// </summary>
    public static class StealthUtil
    {
        /// <summary>
        /// Máscara da visão dos ogros: o cenário bloqueia; jogador, inimigos, projéteis, gatilhos e interativos não.
        /// </summary>
        public static int SightMask => CombatLayers.SightMask;

        /// <summary>
        /// Mostra uma dica curta no HUD da furtividade (ou no tutorial).
        /// </summary>
        /// <param name="text">Texto.</param>
        public static void Hint(string text)
        {
            if (StealthHUD.Instance != null) StealthHUD.Instance.ShowHint(text);
            else if (GameManager.Instance != null && GameManager.Instance.ui != null) GameManager.Instance.ui.ShowTutorial(text, 3f);
        }

        /// <summary>
        /// Desbloqueia uma habilidade no diário (se existir na lista do nível).
        /// </summary>
        /// <param name="id">Identificador.</param>
        public static void UnlockSkill(string id)
        {
            if (!string.IsNullOrEmpty(id) && SkillManager.Instance != null) SkillManager.Instance.Unlock(id);
        }

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
        /// <param name="parent">Pai opcional.</param>
        /// <returns>A instância (ou nulo).</returns>
        public static GameObject SpawnRandom(GameObject[] effects, Vector3 position, Transform parent = null)
        {
            if (effects == null || effects.Length == 0) return null;
            GameObject fx = effects[Random.Range(0, effects.Length)];
            if (fx == null) return null;
            GameObject go = Object.Instantiate(fx, position, Quaternion.identity);
            if (parent != null) go.transform.SetParent(parent, true);
            return go;
        }

        /// <summary>
        /// Clipe sorteado de um vetor (nulo se vazio).
        /// </summary>
        /// <param name="clips">Vetor de sons.</param>
        /// <returns>Um clipe.</returns>
        public static AudioClip Pick(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        /// <summary>
        /// Distância horizontal entre dois pontos.
        /// </summary>
        /// <param name="a">Ponto A.</param>
        /// <param name="b">Ponto B.</param>
        /// <returns>Distância no plano XZ.</returns>
        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
