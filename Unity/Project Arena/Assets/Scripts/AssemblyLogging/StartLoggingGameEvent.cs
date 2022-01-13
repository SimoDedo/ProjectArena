using ScriptableObjectArchitecture;

namespace AssemblyLogging
{
    public class BaseStartLoggingGameEvent : GameEventBase { }

    public sealed class StartLoggingGameEvent : ScriptableObjectSingleton<BaseStartLoggingGameEvent> { }
}