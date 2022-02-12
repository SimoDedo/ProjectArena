using ScriptableObjectArchitecture;

namespace Logging
{
    public class BaseSaveMapTextGameEvent : GameEventBase<string> { }

    public sealed class SaveMapTextGameEvent : ScriptableObjectSingleton<BaseSaveMapTextGameEvent> { }
}