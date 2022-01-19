using ScriptableObjectArchitecture;
using UnityEngine;

namespace AssemblyLogging
{
    public struct SearchInfo
    {
        public int searcherId;
        public float timeLastSight;
    }

    public class BaseSearchInfoGameEvent : GameEventBase<SearchInfo> { }

    public sealed class SearchInfoGameEvent : ScriptableObjectSingleton<BaseSearchInfoGameEvent> { }
}