using System.Collections;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Armadilha da introdução: o templo treme, a porta se fecha, sobe poeira
    /// e o piso desaba (cada bloco recebe AddForce e AddTorque), derrubando John
    /// no complexo subterrâneo.
    /// </summary>
    public class TempleCollapseTrap : MonoBehaviour
    {
        /// <summary>Blocos do piso que desabam.</summary>
        public Rigidbody[] floorTiles;
        /// <summary>Porta do templo que se fecha ao disparar.</summary>
        public StoneDoor entranceDoor;
        /// <summary>Poeira/fumaça do desabamento.</summary>
        public ParticleSystem[] dustEffects;
        /// <summary>Ponto acima do poço usado como checkpoint depois da armadilha (renascer = cair de novo no poço).</summary>
        public Transform respawnAbovePit;
        /// <summary>Tempo de tremor antes do piso cair.</summary>
        public float delayBeforeCollapse = 2.2f;
        /// <summary>Força para baixo aplicada aos blocos.</summary>
        public float downForce = 4f;
        /// <summary>Torque aleatório aplicado aos blocos.</summary>
        public float torque = 6f;

        private bool triggered;

        /// <summary>
        /// Dispara a armadilha (apenas uma vez).
        /// </summary>
        public void Trigger()
        {
            if (triggered) return;
            triggered = true;
            // A partir daqui, renascer leva o jogador para cima do poço (e não para fora do templo fechado).
            if (respawnAbovePit != null && GameManager.Instance != null)
                GameManager.Instance.SetCheckpoint(respawnAbovePit.position, respawnAbovePit.rotation, 0);
            StartCoroutine(CollapseRoutine());
        }

        /// <summary>
        /// Sequência temporizada da armadilha: tremor, porta, poeira e queda do piso.
        /// </summary>
        /// <returns>Enumerador da corrotina.</returns>
        private IEnumerator CollapseRoutine()
        {
            UIManager ui = GameManager.Instance != null ? GameManager.Instance.ui : null;
            AudioManager am = AudioManager.Instance;

            // 1) Tremor, estrondo e porta fechando. O jogador perde o controle durante a cena.
            if (GameManager.Instance != null) GameManager.Instance.SetCutscene(true);
            if (am != null) am.Play2D(am.rumble);
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(delayBeforeCollapse + 0.8f, 0.12f);
            if (entranceDoor != null) entranceDoor.SetOpen(false);
            if (ui != null) ui.ShowLine("O chão está tremendo... a porta está se fechando!");
            foreach (ParticleSystem p in dustEffects)
            {
                if (p != null) p.Play();
            }

            yield return new WaitForSeconds(delayBeforeCollapse);

            // 2) O piso desaba: cada bloco vira corpo dinâmico com força e torque aleatórios.
            foreach (Rigidbody tile in floorTiles)
            {
                if (tile == null) continue;
                tile.isKinematic = false;
                tile.AddForce(Vector3.down * downForce * Random.Range(0.6f, 1.4f), ForceMode.VelocityChange);
                tile.AddTorque(Random.insideUnitSphere * torque, ForceMode.VelocityChange);
                Destroy(tile.gameObject, 6f);
            }
            if (am != null) am.Play2D(am.rockExplosion, 0.8f);
            if (ui != null) ui.ShowLine("Nããão!");

            // 3) Devolve o controle depois de um instante de queda.
            yield return new WaitForSeconds(0.6f);
            if (GameManager.Instance != null) GameManager.Instance.SetCutscene(false);
        }
    }
}
