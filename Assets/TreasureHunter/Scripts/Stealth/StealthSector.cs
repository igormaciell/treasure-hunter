using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Trecho do percurso furtivo (gatilho na entrada, dentro da passagem estreita segura).
    /// Ao entrar: vira o checkpoint, mostra título/missão/dica, guarda as balas e pedras de John e coloca os
    /// ogros do trecho em seus trajetos (ligando os que ainda não estavam em cena, sempre fora da vista).
    /// Se John morrer (ou reiniciar a sala), o trecho volta ao começo: ogros nos pontos iniciais, munição e
    /// pedras como na entrada.
    /// </summary>
    public class StealthSector : MonoBehaviour, IResettable
    {
        /// <summary>Ogro que deve patrulhar um trajeto neste trecho.</summary>
        [Serializable]
        public class OgreAssignment
        {
            public OgreAI ogre;
            public PatrolRoute route;
            public int startPoint;
        }

        /// <summary>Trecho ativo.</summary>
        public static StealthSector Active { get; private set; }
        /// <summary>Todos os trechos.</summary>
        public static readonly List<StealthSector> All = new List<StealthSector>();

        [Header("Trecho")]
        public int sectorIndex;
        public string title = "Nível 4 — As Galerias dos Ogros";
        public string subtitle = "";
        [TextArea] public string objective = "";
        [TextArea] public string tutorial = "";
        public string line = "";
        public string skillOnEnter = "";
        public Transform respawnPoint;

        [Header("Ogros")]
        public OgreAssignment[] ogres;
        /// <summary>Ogros que devem ficar fora de cena enquanto este trecho estiver ativo.</summary>
        public OgreAI[] dormant;

        /// <summary>John já entrou neste trecho.</summary>
        public bool Entered { get; private set; }
        /// <summary>Trecho (IResettable).</summary>
        public int RoomIndex => sectorIndex;
        /// <summary>Vezes que o trecho foi reiniciado (mortes).</summary>
        public int Restarts { get; private set; }

        private CombatSnapshot snapshot;
        private bool hasSnapshot;

        /// <summary>Registra.</summary>
        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        /// <summary>Remove o registro.</summary>
        private void OnDisable()
        {
            All.Remove(this);
            if (Active == this) Active = null;
        }

        /// <summary>
        /// John entrou na passagem do trecho.
        /// </summary>
        /// <param name="other">Colisor.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (Entered || !other.CompareTag(GameTags.Player)) return;
            Begin();
        }

        /// <summary>
        /// Começa o trecho.
        /// </summary>
        public void Begin()
        {
            Entered = true;
            Active = this;
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                Transform p = respawnPoint != null ? respawnPoint : transform;
                gm.SetCheckpoint(p.position, p.rotation, sectorIndex);
                if (gm.ui != null)
                {
                    if (!string.IsNullOrEmpty(subtitle)) gm.ui.ShowRoomTitle(title, subtitle);
                    if (!string.IsNullOrEmpty(objective)) gm.ui.SetObjective(objective);
                    if (!string.IsNullOrEmpty(tutorial)) gm.ui.ShowTutorial(tutorial, 9f);
                    if (!string.IsNullOrEmpty(line)) gm.ui.ShowLine(line);
                }
            }
            if (PlayerCombat.Instance != null) { snapshot = PlayerCombat.Instance.Capture(); hasSnapshot = true; }
            StealthAudio a = StealthAudio.Instance;
            if (a != null && sectorIndex > 0) StealthAudio.Play2D(a.checkpoint, 0.5f);
            StealthUtil.UnlockSkill(skillOnEnter);
            ApplyOgres(false);
        }

        /// <summary>
        /// Coloca cada ogro no trajeto deste trecho.
        /// </summary>
        /// <param name="forceReset">Teletransportar todos ao ponto inicial (reinício).</param>
        private void ApplyOgres(bool forceReset)
        {
            if (dormant != null)
                foreach (OgreAI o in dormant) if (o != null) o.SetDormant(true);
            if (ogres == null) return;
            foreach (OgreAssignment a in ogres)
            {
                if (a == null || a.ogre == null || a.route == null) continue;
                bool sameRoute = a.ogre.CurrentRoute == a.route && !a.ogre.Dormant && a.ogre.gameObject.activeSelf;
                if (sameRoute && !forceReset) continue;
                a.ogre.AssignRoute(a.route, a.startPoint, true);
            }
        }

        /// <summary>
        /// Reinício do trecho (morte ou "Reiniciar sala"): ogros nos pontos iniciais e o arsenal da entrada.
        /// </summary>
        public void ResetState()
        {
            Restarts++;
            ApplyOgres(true);
            if (hasSnapshot && PlayerCombat.Instance != null) PlayerCombat.Instance.Restore(snapshot);
            if (PlayerStealth.Instance != null) PlayerStealth.Instance.Revive();
        }

        /// <summary>Zera os estáticos.</summary>
        public static void ResetStatics()
        {
            Active = null;
            All.Clear();
        }
    }
}
