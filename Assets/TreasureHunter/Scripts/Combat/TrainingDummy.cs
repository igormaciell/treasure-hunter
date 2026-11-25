using UnityEngine;

namespace TreasureHunter
{
    /// <summary>
    /// Boneco de palha para treinar a coronhada e a faca sem gastar balas.
    /// Nunca quebra: balança como uma mola (rotação) a cada golpe, solta palha e faz barulho.
    /// Se levar um tiro, John lembra que as balas são poucas.
    /// </summary>
    public class TrainingDummy : MonoBehaviour, IDamageable
    {
        /// <summary>Parte que balança (pivô na base).</summary>
        public Transform pivot;
        /// <summary>Inclinação causada por golpe (graus).</summary>
        public float hitAngle = 28f;
        /// <summary>Rigidez da mola.</summary>
        public float stiffness = 60f;
        /// <summary>Amortecimento.</summary>
        public float damping = 5f;
        /// <summary>Efeitos do golpe (palha).</summary>
        public GameObject[] hitEffects;

        private Vector2 angle;
        private Vector2 velocity;
        private bool warnedAboutBullets;

        /// <summary>O boneco nunca "morre".</summary>
        public bool IsAlive => true;
        /// <summary>Objeto neutro.</summary>
        public Team Team => Team.Neutral;

        /// <summary>
        /// Recebe o golpe: impulso na mola, palha e som.
        /// </summary>
        /// <param name="info">Dados do golpe.</param>
        /// <returns>Sempre true.</returns>
        public bool TakeHit(DamageInfo info)
        {
            Vector3 local = pivot != null && pivot.parent != null ? pivot.parent.InverseTransformDirection(info.direction) : info.direction;
            // Golpe para frente (z) inclina em X; golpe lateral (x) inclina em Z.
            velocity += new Vector2(local.z, -local.x) * hitAngle * 6f;
            Projectile.SpawnEffects(hitEffects, info.point, -info.direction);
            CombatAudio a = CombatAudio.Instance;
            if (a != null) CombatAudio.Play(a.dummyHit, info.point, 0.9f);

            if (info.source == DamageSource.Bullet && !warnedAboutBullets && PlayerCombat.Instance != null)
            {
                warnedAboutBullets = true;
                PlayerCombat.Instance.ShowHint("Poupe balas! Treine a coronhada [V] no boneco: ela não gasta munição.");
            }
            return true;
        }

        /// <summary>
        /// Simula a mola (oscilação amortecida) e aplica a rotação.
        /// </summary>
        private void Update()
        {
            if (pivot == null) return;
            float dt = Time.deltaTime;
            Vector2 accel = -stiffness * angle - damping * velocity;
            velocity += accel * dt;
            angle += velocity * dt;
            angle = Vector2.ClampMagnitude(angle, 40f);
            pivot.localRotation = Quaternion.Euler(angle.x, 0f, angle.y);
        }
    }
}
