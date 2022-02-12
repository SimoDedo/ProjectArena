using ScriptableObjectArchitecture;

namespace Logging
{
    public class BaseLoadNextSceneGameEvent : GameEventBase
    {
    }

    public sealed class LoadNextSceneGameEvent : ScriptableObjectSingleton<BaseLoadNextSceneGameEvent>
    {
    }
}