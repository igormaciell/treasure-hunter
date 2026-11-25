using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace TreasureHunter
{
    /// <summary>
    /// Gera o NavMesh do nível quando a cena começa, a partir dos colisores do cenário fixo
    /// (pisos, paredes, rampas, plataformas e coberturas reunidos sob um objeto raiz).
    /// Usa a API de construção do próprio Unity (UnityEngine.AI.NavMeshBuilder), sem depender de pacotes extras.
    /// Portões e objetos quebráveis usam NavMeshObstacle e ficam fora da geração, para poderem abrir/sumir.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class NavMeshRuntimeBaker : MonoBehaviour
    {
        /// <summary>Raiz do cenário fixo (somente os colisores abaixo dela entram no NavMesh).</summary>
        public Transform geometryRoot;
        /// <summary>Centro da área do NavMesh.</summary>
        public Vector3 boundsCenter = new Vector3(0f, 4f, 50f);
        /// <summary>Tamanho da área do NavMesh.</summary>
        public Vector3 boundsSize = new Vector3(40f, 14f, 130f);
        /// <summary>Raio dos agentes (margem junto às paredes).</summary>
        public float agentRadius = 0.4f;
        /// <summary>Altura dos agentes.</summary>
        public float agentHeight = 1.9f;
        /// <summary>Degrau máximo.</summary>
        public float agentClimb = 0.45f;
        /// <summary>Inclinação máxima (rampas das plataformas).</summary>
        public float agentSlope = 40f;

        private NavMeshDataInstance instance;

        /// <summary>
        /// Constrói o NavMesh antes de qualquer inimigo existir.
        /// </summary>
        private void Awake()
        {
            Build();
        }

        /// <summary>
        /// Coleta os colisores do cenário e constrói os dados do NavMesh, registrando-os na cena.
        /// </summary>
        public void Build()
        {
            if (instance.valid) instance.Remove();
            Transform root = geometryRoot != null ? geometryRoot : transform;
            List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(root, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);

            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = agentRadius;
            settings.agentHeight = agentHeight;
            settings.agentClimb = agentClimb;
            settings.agentSlope = agentSlope;

            NavMeshData data = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(boundsCenter, boundsSize), Vector3.zero, Quaternion.identity);
            if (data != null) instance = NavMesh.AddNavMeshData(data);
            else Debug.LogWarning("[Treasure Hunter] Não foi possível gerar o NavMesh do Nível 2.");
        }

        /// <summary>
        /// Remove o NavMesh ao sair da cena.
        /// </summary>
        private void OnDestroy()
        {
            if (instance.valid) instance.Remove();
        }

        /// <summary>
        /// Mostra a área do NavMesh no editor.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }
    }
}
