using Events;

namespace Logging
{
    public class BaseStartLoggingGameEvent : GameEventBase
    {
    }

    public sealed class StartLoggingGameEvent : ClassSingleton<BaseStartLoggingGameEvent>
    {
    }
}