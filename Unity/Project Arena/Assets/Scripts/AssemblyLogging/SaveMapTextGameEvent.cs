using ScriptableObjectArchitecture;

namespace AssemblyLogging
{
    public class BaseSaveMapTextGameEvent : GameEventBase<string> { }

    public sealed class SaveMapTextGameEvent : ScriptableObjectSingleton<BaseSaveMapTextGameEvent> { }
}