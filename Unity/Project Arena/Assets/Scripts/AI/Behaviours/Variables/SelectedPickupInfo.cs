using System;
using BehaviorDesigner.Runtime;
using Pickables;

namespace AI.Behaviours.Variables
{
    [Serializable]
    public struct SelectedPickupInfo
    {
        public Pickable pickup;
        public float estimatedActivationTime;
    }

    [Serializable]
    public class SharedSelectedPickupInfo : SharedVariable<SelectedPickupInfo>
    {
        public static implicit operator SharedSelectedPickupInfo(SelectedPickupInfo value)
        {
            return new SharedSelectedPickupInfo {Value = value};
        }
    }
}