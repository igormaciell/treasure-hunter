using System;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Guarda qual é o checkpoint ativo no parkour (pedra fixa ou totem) para o HUD e os efeitos de retorno.
    /// </summary>
    public static class ParkourCheckpoints
    {
        /// <summary>Objeto dono do checkpoint ativo (pedra ou totem).</summary>
        public static UnityEngine.Object Current { get; private set; }
        /// <summary>Nome exibido no HUD.</summary>
        public static string Label { get; private set; } = "Início";
        /// <summary>O checkpoint ativo mudou.</summary>
        public static event Action Changed;

        /// <summary>
        /// Registra o novo checkpoint ativo.
        /// </summary>
        /// <param name="owner">Pedra ou totem.</param>
        /// <param name="label">Nome para o HUD.</param>
        public static void Set(UnityEngine.Object owner, string label)
        {
            Current = owner;
            Label = label;
            Changed?.Invoke();
        }

        /// <summary>
        /// Volta ao estado inicial (nova cena).
        /// </summary>
        public static void ResetStatics()
        {
            Current = null;
            Label = "Início";
            Changed = null;
        }
    }

    /// <summary>
    /// Pedra de checkpoint fixo: ao passar por ela, as runas acendem e ela vira o ponto de retorno
    /// (até John passar por outra ou colocar o totem).
    /// </summary>
    public class CheckpointStone : MonoBehaviour
    {
        /// <summary>Onde John reaparece.</summary>
        public Transform respawnPoint;
        /// <summary>Sala do checkpoint.</summary>
        public int roomIndex = 1;
        /// <summary>Nome exibido no HUD.</summary>
        public string label = "Pedra de checkpoint";
        /// <summary>Habilidade desbloqueada na primeira ativação (opcional).</summary>
        public string skillOnActivate = "";
        /// <summary>Luz das runas (mais forte quando ativa).</summary>
        public Light runeLight;
        public float inactiveIntensity = 0.4f;
        public float activeIntensity = 2.2f;
        public GameObject[] activateEffects;

        private bool everActivated;

        /// <summary>Começa apagada.</summary>
        private void Start()
        {
            if (runeLight != null) runeLight.intensity = inactiveIntensity;
        }

        /// <summary>Ativa ao contato do jogador.</summary>
        /// <param name="other">Colisor que entrou.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(GameTags.Player)) Activate();
        }

        /// <summary>Mantém a luz de acordo com o estado.</summary>
        private void Update()
        {
            if (runeLight == null) return;
            float target = ReferenceEquals(ParkourCheckpoints.Current, this) ? activeIntensity : inactiveIntensity;
            runeLight.intensity = Mathf.MoveTowards(runeLight.intensity, target, Time.deltaTime * 3f);
        }

        /// <summary>
        /// Torna esta pedra o checkpoint ativo.
        /// </summary>
        public void Activate()
        {
            if (ReferenceEquals(ParkourCheckpoints.Current, this)) return;
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            Transform p = respawnPoint != null ? respawnPoint : transform;
            gm.SetCheckpoint(p.position, p.rotation, roomIndex);
            ParkourCheckpoints.Set(this, label);
            ParkourUtil.SpawnAll(activateEffects, transform.position + Vector3.up * 0.5f);
            ParkourAudio a = ParkourAudio.Instance;
            if (a != null) ParkourAudio.Play(a.checkpoint, transform.position, 0.8f);
            ParkourUtil.Hint("Checkpoint: " + label);
            if (!everActivated) ParkourUtil.UnlockSkill(skillOnActivate);
            everActivated = true;
        }
    }
}
