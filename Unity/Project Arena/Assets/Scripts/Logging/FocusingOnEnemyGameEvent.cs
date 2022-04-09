using ScriptableObjectArchitecture;

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

    public class FocusingOnEnemyGameEvent : ScriptableObjectSingleton<BaseFocusingOnEnemyGameEvent>
    {
        
    }
}