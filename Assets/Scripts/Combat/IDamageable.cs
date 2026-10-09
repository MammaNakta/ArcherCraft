using UnityEngine;

namespace ArcherCraft.Combat
{
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal);
    }
}
