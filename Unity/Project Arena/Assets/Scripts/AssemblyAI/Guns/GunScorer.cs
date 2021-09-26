using UnityEngine;

namespace AI.Guns
{
    public abstract class GunScorer : MonoBehaviour
    {
        public abstract float GetGunScore(float distance);
    }

    // TODO For now:
    //   Shotgun: 0-20
    //   Rocket: 20-30
    //   Assault: 30-60
    //   Rifle: 60-100
}