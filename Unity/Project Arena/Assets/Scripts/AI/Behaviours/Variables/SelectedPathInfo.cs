using System;
using BehaviorDesigner.Runtime;
using UnityEngine.AI;

namespace AI.Behaviours.Variables
{
    [Serializable]
    public class SharedSelectedPathInfo : SharedVariable<NavMeshPath>
    {
        public static implicit operator SharedSelectedPathInfo(NavMeshPath value)
        {
            return new SharedSelectedPathInfo {Value = value};
        }
    }
}