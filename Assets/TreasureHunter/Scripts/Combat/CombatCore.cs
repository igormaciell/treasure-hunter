using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Lados do combate. Projéteis e golpes nunca ferem quem está do mesmo lado
    /// (as flechas de um esqueleto não acertam outro esqueleto).
    /// </summary>
    public enum Team { Player, Enemy, Neutral }

    /// <summary>
    /// Origem do dano. Define a reação do alvo (atordoamento, empurrão), os efeitos
    /// e as habilidades desbloqueadas (Pontaria, Corpo a corpo, Improviso).
    /// </summary>
    public enum DamageSource { Bullet, PistolWhip, Knife, Stone, Explosion, Trap, Arrow, EnemyMelee }

    /// <summary>
    /// Pacote de informações de um golpe: quanto, onde, de onde, com que força e de quem.
    /// </summary>
    public struct DamageInfo
    {
        /// <summary>Quantidade de dano (pontos de vida dos inimigos ou corações do jogador).</summary>
        public float amount;
        /// <summary>Ponto do impacto no mundo.</summary>
        public Vector3 point;
        /// <summary>Direção do golpe (normalizada).</summary>
        public Vector3 direction;
        /// <summary>Intensidade do empurrão.</summary>
        public float force;
        /// <summary>Origem do dano.</summary>
        public DamageSource source;
        /// <summary>Lado de quem causou o dano.</summary>
        public Team team;
        /// <summary>Objeto que causou o dano (pode ser nulo).</summary>
        public GameObject attacker;
        /// <summary>Verdadeiro quando o projétil acertou a cabeça.</summary>
        public bool headshot;

        /// <summary>
        /// Cria um pacote de dano.
        /// </summary>
        /// <param name="amount">Quantidade de dano.</param>
        /// <param name="point">Ponto do impacto.</param>
        /// <param name="direction">Direção do golpe.</param>
        /// <param name="force">Força do empurrão.</param>
        /// <param name="source">Origem.</param>
        /// <param name="team">Lado do atacante.</param>
        /// <param name="attacker">Atacante.</param>
        public DamageInfo(float amount, Vector3 point, Vector3 direction, float force, DamageSource source, Team team, GameObject attacker)
        {
            this.amount = amount;
            this.point = point;
            this.direction = direction;
            this.force = force;
            this.source = source;
            this.team = team;
            this.attacker = attacker;
            headshot = false;
        }

        /// <summary>Dano vindo do cenário (pedras, explosões, armadilhas): conta para a habilidade Improviso.</summary>
        public bool IsEnvironmental => source == DamageSource.Stone || source == DamageSource.Explosion || source == DamageSource.Trap;

        /// <summary>Dano de golpe corpo a corpo do jogador (coronhada ou faca).</summary>
        public bool IsPlayerMelee => source == DamageSource.PistolWhip || source == DamageSource.Knife;
    }

    /// <summary>
    /// Qualquer coisa que pode receber dano: inimigos, o jogador, caixotes, barris e o boneco de treino.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>Indica se o alvo ainda pode receber dano.</summary>
        bool IsAlive { get; }
        /// <summary>Lado do alvo.</summary>
        Team Team { get; }
        /// <summary>
        /// Aplica o golpe.
        /// </summary>
        /// <param name="info">Dados do golpe.</param>
        /// <returns>true se o dano foi aplicado.</returns>
        bool TakeHit(DamageInfo info);
    }

    /// <summary>
    /// Nomes das Tags do combate (Nível 2). São registradas pelo gerador do Nível 2.
    /// </summary>
    public static class CombatTags
    {
        /// <summary>Inimigos (esqueletos).</summary>
        public const string Enemy = "Enemy";
        /// <summary>Projéteis (balas, flechas, pedras arremessadas).</summary>
        public const string Projectile = "Projectile";
        /// <summary>Armas que o jogador pode pegar (revólver, faca).</summary>
        public const string Weapon = "Weapon";
        /// <summary>Objetos quebráveis (caixotes frágeis, barris explosivos).</summary>
        public const string Destructible = "Destructible";

        /// <summary>Lista das Tags que o gerador precisa registrar.</summary>
        public static readonly string[] All = { Enemy, Projectile, Weapon, Destructible };
    }

    /// <summary>
    /// Camadas (Layers) do combate e as máscaras usadas nas sondagens de física.
    /// O jogador continua na camada "Ignore Raycast" (2), como no Nível 1, para não atrapalhar
    /// as sondagens de chão e de câmera já existentes.
    /// </summary>
    public static class CombatLayers
    {
        /// <summary>Nome da camada dos inimigos.</summary>
        public const string EnemyLayerName = "Enemy";
        /// <summary>Nome da camada dos projéteis.</summary>
        public const string ProjectileLayerName = "Projectile";
        /// <summary>Nome da camada dos objetos interativos (itens, alavancas, destroços).</summary>
        public const string InteractableLayerName = "Interactable";
        /// <summary>Camada do jogador ("Ignore Raycast").</summary>
        public const int PlayerLayer = 2;

        private static int enemy = -2, projectile = -2, interactable = -2;

        /// <summary>Índice da camada dos inimigos (-1 se não existir).</summary>
        public static int Enemy => Get(ref enemy, EnemyLayerName);
        /// <summary>Índice da camada dos projéteis (-1 se não existir).</summary>
        public static int Projectile => Get(ref projectile, ProjectileLayerName);
        /// <summary>Índice da camada dos objetos interativos (-1 se não existir).</summary>
        public static int Interactable => Get(ref interactable, InteractableLayerName);

        /// <summary>Tudo o que um projétil pode acertar (inclui o jogador; exclui projéteis e itens).</summary>
        public static int ProjectileMask => ~(Bit(Projectile) | Bit(Interactable));
        /// <summary>Mira do jogador e golpes do jogador: como a anterior, mas sem o próprio jogador.</summary>
        public static int AimMask => ProjectileMask & ~(1 << PlayerLayer);
        /// <summary>Visão dos inimigos: só o cenário (sem jogador, inimigos, projéteis e itens).</summary>
        public static int SightMask => AimMask & ~Bit(Enemy);
        /// <summary>Colisão da câmera: cenário apenas.</summary>
        public static int CameraMask => SightMask & Physics.DefaultRaycastLayers;

        /// <summary>
        /// Busca (uma vez) o índice de uma camada pelo nome.
        /// </summary>
        /// <param name="cache">Valor guardado (-2 = ainda não buscado).</param>
        /// <param name="name">Nome da camada.</param>
        /// <returns>Índice da camada ou -1.</returns>
        private static int Get(ref int cache, string name)
        {
            if (cache == -2) cache = LayerMask.NameToLayer(name);
            return cache;
        }

        /// <summary>
        /// Converte o índice de camada em bit de máscara (0 se a camada não existir).
        /// </summary>
        /// <param name="layer">Índice da camada.</param>
        /// <returns>Bit da máscara.</returns>
        private static int Bit(int layer)
        {
            return layer >= 0 ? 1 << layer : 0;
        }

        /// <summary>
        /// Esquece os índices guardados (as camadas podem mudar entre gerações).
        /// </summary>
        public static void ResetCache()
        {
            enemy = projectile = interactable = -2;
        }

        /// <summary>
        /// Configura a matriz de colisão: projéteis não colidem entre si nem com itens,
        /// e inimigos não empurram itens nem destroços.
        /// </summary>
        public static void SetupCollisions()
        {
            if (Projectile >= 0)
            {
                Physics.IgnoreLayerCollision(Projectile, Projectile, true);
                if (Interactable >= 0) Physics.IgnoreLayerCollision(Projectile, Interactable, true);
            }
            if (Enemy >= 0 && Interactable >= 0) Physics.IgnoreLayerCollision(Enemy, Interactable, true);
        }
    }
}
