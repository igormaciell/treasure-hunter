using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Marca de pouso: durante o salto, um círculo aparece no chão logo abaixo de John (Raycast para baixo).
    /// Verde = há uma superfície válida para pousar; vermelho = abismo ou superfície íngreme. Ajuda a calcular saltos.
    /// </summary>
    public class LandingIndicator : MonoBehaviour
    {
        /// <summary>Parkour do jogador (vazio = procura a instância).</summary>
        public ParkourController player;
        /// <summary>Renderizador do círculo.</summary>
        public Renderer marker;
        public float maxDistance = 25f;
        public float maxSlope = 45f;
        public Color validColor = new Color(0.35f, 1f, 0.55f, 0.7f);
        public Color invalidColor = new Color(1f, 0.3f, 0.25f, 0.7f);

        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock block;

        /// <summary>Prepara o bloco de propriedades.</summary>
        private void Awake()
        {
            block = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Depois do movimento: posiciona o círculo sob John e escolhe a cor.
        /// </summary>
        private void LateUpdate()
        {
            if (player == null) player = ParkourController.Instance;
            if (player == null || marker == null) return;

            bool airborne = !player.IsGrounded && !player.IsMantling && player.AirTime > 0.1f;
            if (!airborne)
            {
                if (marker.enabled) marker.enabled = false;
                return;
            }

            Vector3 origin = player.transform.position + Vector3.up * 0.3f;
            bool hit = Physics.Raycast(origin, Vector3.down, out RaycastHit info, maxDistance, ParkourLayers.SurfaceMask, QueryTriggerInteraction.Ignore);
            bool valid = hit && Vector3.Angle(info.normal, Vector3.up) <= maxSlope;
            Vector3 point = hit ? info.point + info.normal * 0.03f : origin + Vector3.down * 3f;
            transform.position = point;
            transform.rotation = hit ? Quaternion.FromToRotation(Vector3.up, info.normal) : Quaternion.identity;

            // Fica menor quanto mais perto do chão (dá noção de altura).
            float dist = hit ? info.distance : 3f;
            transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.2f, Mathf.Clamp01(dist / 8f));
            block.SetColor(ColorId, valid ? validColor : invalidColor);
            marker.SetPropertyBlock(block);
            if (!marker.enabled) marker.enabled = true;
        }
    }
}
