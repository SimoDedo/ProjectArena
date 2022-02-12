using ScriptableObjectArchitecture;

namespace Logging
{
    /// <summary>
    ///     Raised when an entity starts looking for an enemy that he lost track of during a fight.
    ///     The int is the id of the entity.
    /// </summary>
    public class BaseSearchStartGameEvent : GameEventBase<int>
    {
    }

    public sealed class SearchStartGameEvent : ScriptableObjectSingleton<BaseSearchStartGameEvent>
    {
    }
}