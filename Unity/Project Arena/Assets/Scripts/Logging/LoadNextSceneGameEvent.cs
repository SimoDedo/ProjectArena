using Events;

namespace Logging
{
    public class BaseLoadNextSceneGameEvent : GameEventBase
    {
    }

    public sealed class LoadNextSceneGameEvent : ClassSingleton<BaseLoadNextSceneGameEvent>
    {
    }
}