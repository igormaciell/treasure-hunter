using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Tocha do Nível 4 (mão esquerda, porque a direita segura o revólver). Diferente da tocha do Nível 1,
    /// ela pode ser apagada: acesa, ilumina a caverna escura mas deixa John muito mais visível para os ogros
    /// (de longe e em qualquer sombra); apagada, John depende da pouca luz do ambiente.
    /// </summary>
    public class StealthTorch : MonoBehaviour
    {
        [Header("Referências")]
        public Light torchLight;
        public ParticleSystem flame;
        /// <summary>Visual da tocha (fica de pé, girando só com o corpo).</summary>
        public Transform torchVisual;
        /// <summary>Braço que segura a tocha (sobrescreve a animação enquanto acesa).</summary>
        public Transform armPivot;

        [Header("Comportamento")]
        public bool startLit = false;
        public float litIntensity = 2.6f;
        public float litRange = 10f;
        /// <summary>Ângulo do braço com a tocha erguida à frente.</summary>
        public float litArmAngle = -70f;
        /// <summary>Ruído ao acender/apagar.</summary>
        public float toggleNoise = 2.5f;
        /// <summary>Efeitos ao acender (faíscas) e ao apagar (fumaça) — vetores de prefabs.</summary>
        public GameObject[] igniteEffects;
        public GameObject[] extinguishEffects;

        /// <summary>A tocha está acesa.</summary>
        public bool Lit { get; private set; }
        /// <summary>Vezes que a tocha foi acesa no nível.</summary>
        public int TimesLit { get; private set; }
        /// <summary>Disparado ao acender/apagar.</summary>
        public event System.Action<bool> Changed;

        private float flicker;
        private float armBlend;
        private PlayerStealth stealth;

        /// <summary>Estado inicial.</summary>
        private void Start()
        {
            stealth = GetComponent<PlayerStealth>();
            SetLit(startLit, false);
        }

        /// <summary>
        /// F acende/apaga; a chama tremula.
        /// </summary>
        private void Update()
        {
            bool canAct = GameManager.Instance == null || GameManager.Instance.CanPlayerAct();
            if (canAct && (stealth == null || !stealth.IsDead) && StealthInput.TorchPressed()) SetLit(!Lit, true);

            if (torchLight != null && Lit)
            {
                flicker += Time.deltaTime * 7f;
                float n = Mathf.PerlinNoise(flicker, 0.37f) * 0.35f + 0.82f;
                torchLight.intensity = litIntensity * n;
                torchLight.range = litRange;
            }
        }

        /// <summary>
        /// Depois do Animator: com a tocha acesa, o braço a segura erguida à frente e a chama fica de pé.
        /// </summary>
        private void LateUpdate()
        {
            armBlend = Mathf.MoveTowards(armBlend, Lit ? 1f : 0f, Time.deltaTime * 5f);
            if (armPivot != null && armBlend > 0.001f)
            {
                Quaternion held = Quaternion.Euler(litArmAngle, 0f, -12f);
                armPivot.localRotation = Quaternion.Slerp(armPivot.localRotation, held, armBlend);
            }
            if (torchVisual != null && armPivot != null && armPivot.parent != null)
            {
                Vector3 flat = Vector3.ProjectOnPlane(armPivot.parent.forward, Vector3.up);
                if (flat.sqrMagnitude > 0.001f) torchVisual.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
            }
        }

        /// <summary>
        /// Acende ou apaga a tocha.
        /// </summary>
        /// <param name="lit">Acesa.</param>
        /// <param name="feedback">Tocar som, efeitos, ruído e dica.</param>
        public void SetLit(bool lit, bool feedback)
        {
            Lit = lit;
            if (torchLight != null) torchLight.enabled = lit;
            if (flame != null)
            {
                if (lit) flame.Play(true);
                else flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (feedback)
            {
                if (lit) TimesLit++;
                StealthAudio a = StealthAudio.Instance;
                if (a != null) StealthAudio.Play(lit ? a.torchIgnite : a.torchExtinguish, transform.position + Vector3.up * 1.4f, 0.8f);
                Vector3 p = torchVisual != null ? torchVisual.position + Vector3.up * 0.5f : transform.position + Vector3.up * 1.6f;
                StealthUtil.SpawnAll(lit ? igniteEffects : extinguishEffects, p);
                NoiseSystem.Emit(transform.position, toggleNoise, NoiseType.Handling, gameObject);
                StealthUtil.Hint(lit ? "Tocha acesa: você enxerga melhor, mas os ogros te veem de longe!" : "Tocha apagada: as sombras voltam a te esconder.");
            }
            Changed?.Invoke(lit);
        }
    }
}
