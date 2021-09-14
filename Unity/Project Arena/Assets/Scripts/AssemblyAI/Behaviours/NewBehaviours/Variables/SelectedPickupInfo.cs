using BehaviorDesigner.Runtime;

namespace AI.Behaviours.NewBehaviours.Variables
{
    [System.Serializable]
    public struct SelectedPickupInfo
    {
        public Pickable pickup;
        public float estimatedActivationTime;
    }

    [System.Serializable]
    public class SharedSelectedPickupInfo : SharedVariable<SelectedPickupInfo>
    {
        public static implicit operator SharedSelectedPickupInfo(SelectedPickupInfo value)
        {
            return new SharedSelectedPickupInfo {Value = value};
        }
    }
}