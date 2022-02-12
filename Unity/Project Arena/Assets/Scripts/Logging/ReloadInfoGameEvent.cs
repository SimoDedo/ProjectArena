using ScriptableObjectArchitecture;

namespace Logging
{
    // TODO no reference to who is recharging?
    public struct ReloadInfo
    {
        public int ownerId;
        public int gunId;
        public int ammoInCharger;
        public int totalAmmo;
    }

    public class BaseReloadInfoGameEvent : GameEventBase<ReloadInfo> { }

    public sealed class ReloadInfoGameEvent : ScriptableObjectSingleton<BaseReloadInfoGameEvent> { }
}