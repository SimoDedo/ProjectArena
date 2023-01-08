using Events;

namespace Logging
{
    public struct FocusOnEnemyInfo
    {
        public int entityID;
        public bool isFocusing;
    }
    
    public class BaseFocusingOnEnemyGameEvent : GameEventBase<FocusOnEnemyInfo>
    {
        
    }

    public class FocusingOnEnemyGameEvent : ClassSingleton<BaseFocusingOnEnemyGameEvent>
    {
        
    }
}