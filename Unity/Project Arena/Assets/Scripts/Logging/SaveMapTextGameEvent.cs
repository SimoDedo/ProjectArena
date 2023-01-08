using Events;

namespace Logging
{
    public class BaseSaveMapTextGameEvent : GameEventBase<string>
    {
    }

    public sealed class SaveMapTextGameEvent : ClassSingleton<BaseSaveMapTextGameEvent>
    {
    }
}