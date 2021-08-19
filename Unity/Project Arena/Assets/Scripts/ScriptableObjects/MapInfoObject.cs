using UnityEngine;
using UnityEngine.Events;

namespace ScriptableObjectArchitecture
{
    public struct MapInfo
    {
        public float height;
        public float width;
        public float ts;
        public bool f;
    }

    public class BaseMapInfoGameEvent : GameEventBase<MapInfo>
    {
    }

    public sealed class MapInfoGameEvent : ScriptableObjectSingleton<BaseMapInfoGameEvent>
    {
    }
}

namespace ScriptableObjectArchitecture
{
    public struct PositionInfo
    {
        public float x;
        public float z;
        public float dir;
        public int entityID;
    }

    public class BasePositionInfoGameEvent : GameEventBase<PositionInfo>
    {
    }

    public sealed class PositionInfoGameEvent : ScriptableObjectSingleton<BasePositionInfoGameEvent>
    {
    }
}


namespace ScriptableObjectArchitecture
{
    public struct GameInfo
    {
        public int gameDuration;
        public string scene;
    }

    public class BaseGameInfoGameEvent : GameEventBase<GameInfo>
    {
    }

    public sealed class GameInfoGameEvent : ScriptableObjectSingleton<BaseGameInfoGameEvent>
    {
    }
}

namespace ScriptableObjectArchitecture
{
    // TODO no reference to who is recharging?
    public struct ReloadInfo
    {
        public int ownerId;
        public int gunId;
        public int ammoInCharger;
        public int totalAmmo;
    }

    public class BaseReloadInfoGameEvent : GameEventBase<ReloadInfo>
    {
    }

    public sealed class ReloadInfoGameEvent : ScriptableObjectSingleton<BaseReloadInfoGameEvent>
    {
    }
}

namespace ScriptableObjectArchitecture
{
    public struct ShotInfo
    {
        public float x;
        public float z;
        public float direction;
        public int ownerId;
        public int gunID;
        public int ammoInCharger;
        public int totalAmmo;
    }

    public class BaseShotInfoGameEvent : GameEventBase<ShotInfo>
    {
    }

    public sealed class ShotInfoGameEvent : ScriptableObjectSingleton<BaseShotInfoGameEvent>
    {
    }
}

namespace ScriptableObjectArchitecture
{
    public struct SpawnInfo
    {
        public float x;
        public float z;
        public int entityId;
        public string spawnEntity;
    }

    public class BaseSpawnInfoGameEvent : GameEventBase<SpawnInfo>
    {
    }

    public sealed class SpawnInfoGameEvent : ScriptableObjectSingleton<BaseSpawnInfoGameEvent>
    {
    }
}

namespace ScriptableObjectArchitecture
{
    public struct KillInfo
    {
        public float x;
        public float z;
        public int killedEntityID;
        public string killedEntity;
        public int killerEntityID;
        public string killerEntity;
    }

    public class BaseKillInfoGameEvent : GameEventBase<KillInfo>
    {
    }

    public sealed class KillInfoGameEvent : ScriptableObjectSingleton<BaseKillInfoGameEvent>
    {
    }
}

namespace ScriptableObjectArchitecture
{
    public struct HitInfo
    {
        public float x;
        public float z;
        public int hitEntityID;
        public string hitEntity;
        public int hitterEntityID;
        public string hitterEntity;
        public int damage;
    }

    public class BaseHitInfoGameEvent : GameEventBase<HitInfo>
    {
    }

    public sealed class HitInfoGameEvent : ScriptableObjectSingleton<BaseHitInfoGameEvent>
    {
    }
}