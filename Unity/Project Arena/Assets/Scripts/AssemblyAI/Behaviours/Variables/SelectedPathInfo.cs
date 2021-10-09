using BehaviorDesigner.Runtime;
using UnityEngine.AI;

namespace AssemblyAI.Behaviours.Variables
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