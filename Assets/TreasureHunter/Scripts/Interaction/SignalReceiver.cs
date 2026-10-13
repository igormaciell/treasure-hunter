using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Base para objetos que reagem a "sinais" de mecanismos (alavancas, placas de pressão,
    /// puzzles, braseiros). Um receptor pode exigir vários sinais ativos ao mesmo tempo
    /// (ex.: a porta da Sala 4 só abre com a placa pressionada E a sequência correta).
    /// </summary>
    public abstract class SignalReceiver : MonoBehaviour
    {
        /// <summary>Quantos emissores diferentes precisam estar ativos para o receptor ligar.</summary>
        public int requiredSignals = 1;
        /// <summary>Se verdadeiro, uma vez ligado o receptor nunca mais desliga (trava aberto).</summary>
        public bool latch = true;

        /// <summary>Indica se o receptor está ligado no momento.</summary>
        public bool IsOn { get; private set; }

        private readonly HashSet<Object> activeSources = new HashSet<Object>();

        /// <summary>
        /// Recebe um sinal de um emissor, atualiza a contagem e liga/desliga o receptor.
        /// </summary>
        /// <param name="active">true quando o emissor ativou; false quando desativou.</param>
        /// <param name="source">Objeto emissor (cada emissor conta apenas uma vez).</param>
        public void ReceiveSignal(bool active, Object source)
        {
            if (active) activeSources.Add(source);
            else activeSources.Remove(source);

            bool shouldBeOn = activeSources.Count >= requiredSignals;
            // Com trava, não volta a desligar depois de ligado.
            if (latch && IsOn) shouldBeOn = true;

            if (shouldBeOn != IsOn)
            {
                IsOn = shouldBeOn;
                OnStateChanged(IsOn);
            }
        }

        /// <summary>
        /// Restaura o estado desligado (usado ao reiniciar a sala).
        /// </summary>
        protected void ClearSignals()
        {
            activeSources.Clear();
            if (IsOn)
            {
                IsOn = false;
                OnStateChanged(false);
            }
        }

        /// <summary>
        /// Chamado quando o receptor liga ou desliga. Cada receptor implementa sua reação.
        /// </summary>
        /// <param name="on">Novo estado.</param>
        protected abstract void OnStateChanged(bool on);

        /// <summary>
        /// Envia um sinal a vários receptores de uma vez (função auxiliar dos emissores).
        /// </summary>
        /// <param name="targets">Receptores de destino (pode conter nulos).</param>
        /// <param name="active">Estado do sinal.</param>
        /// <param name="source">Emissor.</param>
        public static void Broadcast(SignalReceiver[] targets, bool active, Object source)
        {
            if (targets == null) return;
            foreach (SignalReceiver t in targets)
            {
                if (t != null) t.ReceiveSignal(active, source);
            }
        }
    }
}
