using System;
using BehaviorDesigner.Runtime;
using Graph;

namespace AI.Behaviours.Variables
{
    [Serializable]
    public class SharedSelectedArea : SharedVariable<Area>
    {
        public static implicit operator SharedSelectedArea(Area value)
        {
            return new SharedSelectedArea {Value = value};
        }
    }
}