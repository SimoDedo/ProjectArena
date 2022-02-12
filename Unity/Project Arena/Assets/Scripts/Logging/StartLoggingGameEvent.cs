using ScriptableObjectArchitecture;

namespace Logging
{
    public class BaseStartLoggingGameEvent : GameEventBase { }

    public sealed class StartLoggingGameEvent : ScriptableObjectSingleton<BaseStartLoggingGameEvent> { }
}