using Events;
using UnityEngine;

namespace Logging
{
    /// <summary>
    /// Represents the game metrics as calculated by an entity.
    /// </summary>
    public struct GunShootingSoundInfo
    {
        public int gunOwnerId;

        public float gunLoudness;

        public Vector3 gunPosition;
    }

    public class BaseShootingSoundGameEvent : GameEventBase<GunShootingSoundInfo>
    {
    }

    public class ShootingSoundGameEvent : ClassSingleton<BaseShootingSoundGameEvent>
    {
    }
}