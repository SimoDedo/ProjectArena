using ScriptableObjectArchitecture;

namespace AssemblyLogging
{
    public class BaseStartNewExperimentGameEvent : GameEventBase { }

    public sealed class StartNewExperimentGameEvent : ScriptableObjectSingleton<BaseStartNewExperimentGameEvent> { }
}