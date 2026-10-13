using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Categorias dos assets do nível (usadas para comprovar os "70 assets de diferentes categorias").
    /// </summary>
    public enum AssetCategoryType
    {
        Arquitetura, Vegetacao, Props, Tesouros, Mecanismos, Iluminacao, Armadilhas, Inscricoes, Personagem, Efeitos
    }

    /// <summary>
    /// Etiqueta colocada em cada prefab do nível com sua categoria e nome legível.
    /// O gerador usa essas etiquetas para produzir o inventário de assets do Nível 1.
    /// Não tem comportamento em tempo de jogo.
    /// </summary>
    public class AssetCategory : MonoBehaviour
    {
        /// <summary>Categoria do asset.</summary>
        public AssetCategoryType category;
        /// <summary>Nome legível do asset.</summary>
        public string displayName;
    }
}
