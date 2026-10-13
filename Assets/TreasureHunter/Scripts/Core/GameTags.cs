namespace TreasureHunter
{
    /// <summary>
    /// Nomes das Tags usadas no jogo. Centralizar as strings evita erros de digitação
    /// nas comparações com CompareTag. As tags são criadas automaticamente pelo
    /// gerador do Nível 1 (menu Treasure Hunter).
    /// </summary>
    public static class GameTags
    {
        /// <summary>Tag do personagem controlado pelo jogador (tag nativa do Unity).</summary>
        public const string Player = "Player";
        /// <summary>Objetos que podem ser empurrados e que ativam placas de pressão (caixotes).</summary>
        public const string Pushable = "Pushable";
        /// <summary>Objetos que causam dano ao tocar o jogador (espinhos, tronco giratório).</summary>
        public const string Hazard = "Hazard";
        /// <summary>Itens coletáveis comuns (moedas, joias, corações).</summary>
        public const string Collectible = "Collectible";
        /// <summary>Artefatos (tesouros opcionais contados no resumo do nível).</summary>
        public const string Artifact = "Artifact";
        /// <summary>Pedras que caem do teto no Corredor das Pedras.</summary>
        public const string Rock = "Rock";
        /// <summary>Superfícies de chão/parede do cenário (usadas pela pedra para saber onde explodir).</summary>
        public const string Ground = "Ground";

        /// <summary>Lista de todas as tags personalizadas que o gerador precisa registrar.</summary>
        public static readonly string[] CustomTags = { Pushable, Hazard, Collectible, Artifact, Rock, Ground };
    }
}
