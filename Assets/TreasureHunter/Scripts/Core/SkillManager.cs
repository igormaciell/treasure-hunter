using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Definição de uma habilidade (tema "Skills"): identificador, nome exibido e descrição.
    /// </summary>
    [Serializable]
    public class SkillDefinition
    {
        /// <summary>Identificador interno usado pelos gatilhos da fase (ex.: "forca").</summary>
        public string id;
        /// <summary>Nome exibido ao jogador.</summary>
        public string displayName;
        /// <summary>Texto explicando o que o jogador aprendeu.</summary>
        [TextArea] public string description;

        /// <summary>
        /// Cria uma definição de habilidade.
        /// </summary>
        /// <param name="id">Identificador interno.</param>
        /// <param name="displayName">Nome exibido.</param>
        /// <param name="description">Descrição da habilidade.</param>
        public SkillDefinition(string id, string displayName, string description)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
        }
    }

    /// <summary>
    /// Sistema de habilidades que materializa o tema "Skills" do jogo.
    /// Cada sala do Nível 1 ensina uma habilidade; quando o jogador a demonstra,
    /// ela é desbloqueada, aparece um aviso na tela e fica registrada no Diário (Tab).
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        /// <summary>Instância única acessível por qualquer script.</summary>
        public static SkillManager Instance { get; private set; }

        /// <summary>Habilidades que podem ser aprendidas neste nível.</summary>
        public List<SkillDefinition> skills = new List<SkillDefinition>();

        /// <summary>Evento disparado quando uma habilidade é aprendida.</summary>
        public event Action<SkillDefinition> OnSkillUnlocked;

        private readonly HashSet<string> unlocked = new HashSet<string>();

        /// <summary>Quantidade de habilidades já aprendidas.</summary>
        public int UnlockedCount => unlocked.Count;
        /// <summary>Quantidade total de habilidades do nível.</summary>
        public int TotalCount => skills.Count;

        /// <summary>
        /// Registra o singleton e preenche a lista padrão caso esteja vazia.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (skills.Count == 0) skills = CreateLevel1Skills();
        }

        /// <summary>
        /// Cria a lista das sete habilidades ensinadas no Nível 1 — A Entrada do Templo.
        /// </summary>
        /// <returns>Lista com as definições das habilidades.</returns>
        public static List<SkillDefinition> CreateLevel1Skills()
        {
            return new List<SkillDefinition>
            {
                new SkillDefinition("exploracao", "Exploração", "Andar, correr e saltar pelo cenário para encontrar caminhos."),
                new SkillDefinition("observacao", "Observação", "Perceber detalhes escondidos no ambiente, como a alavanca entre as raízes."),
                new SkillDefinition("forca", "Força", "Empurrar objetos pesados até o lugar certo para acionar mecanismos."),
                new SkillDefinition("reflexos", "Reflexos", "Ler os sinais de perigo e desviar das pedras que caem do teto."),
                new SkillDefinition("tocha", "Domínio da Tocha", "Usar a luz da tocha para acender braseiros e revelar inscrições."),
                new SkillDefinition("memoria", "Memória", "Guardar e reproduzir uma sequência de símbolos antigos."),
                new SkillDefinition("combinacao", "Combinação", "Unir várias habilidades para resolver um desafio maior.")
            };
        }

        /// <summary>
        /// Verifica se uma habilidade já foi aprendida.
        /// </summary>
        /// <param name="id">Identificador da habilidade.</param>
        /// <returns>true se a habilidade já estiver desbloqueada.</returns>
        public bool IsUnlocked(string id)
        {
            return unlocked.Contains(id);
        }

        /// <summary>
        /// Desbloqueia uma habilidade (se ainda não estiver), tocando som e avisando a interface.
        /// </summary>
        /// <param name="id">Identificador da habilidade.</param>
        /// <returns>true se a habilidade foi desbloqueada agora; false se já existia ou não foi encontrada.</returns>
        public bool Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || unlocked.Contains(id)) return false;
            SkillDefinition def = skills.Find(s => s.id == id);
            if (def == null) return false;

            // Registra, toca o som e notifica os ouvintes (HUD).
            unlocked.Add(id);
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(AudioManager.Instance.skillUnlock, 0.8f);
            OnSkillUnlocked?.Invoke(def);
            return true;
        }
    }
}
