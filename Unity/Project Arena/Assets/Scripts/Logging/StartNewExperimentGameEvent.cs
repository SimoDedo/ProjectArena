using ScriptableObjectArchitecture;

namespace Logging
{
    public class BaseStartNewExperimentGameEvent : GameEventBase { }

    public sealed class StartNewExperimentGameEvent : ScriptableObjectSingleton<BaseStartNewExperimentGameEvent> { }
}