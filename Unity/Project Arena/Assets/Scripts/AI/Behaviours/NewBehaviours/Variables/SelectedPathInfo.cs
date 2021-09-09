using BehaviorDesigner.Runtime;
using UnityEngine.AI;

namespace AI.Behaviours.NewBehaviours.Variables
{
    [System.Serializable]
    public class SharedSelectedPathInfo : SharedVariable<NavMeshPath>
    {
        public static implicit operator SharedSelectedPathInfo(NavMeshPath value)
        {
            return new SharedSelectedPathInfo {Value = value};
        }
    }
}