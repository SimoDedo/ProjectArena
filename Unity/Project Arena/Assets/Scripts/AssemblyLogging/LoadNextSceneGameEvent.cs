using ScriptableObjectArchitecture;

namespace AssemblyLogging
{
    public class BaseLoadNextSceneGameEvent : GameEventBase { }

    public sealed class LoadNextSceneGameEvent : ScriptableObjectSingleton<BaseLoadNextSceneGameEvent> { }
}