using AI.SharedVariables;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AI
{
    public class ShouldSearchTarget: Conditional
    {
        public SharedSearchData data;
        public SharedFloat giveUpSearchAfter;

        public override TaskStatus OnUpdate()
        {
            if (data.Value.shouldSearch && Time.time < data.Value.timeSetFlag + giveUpSearchAfter.Value)
                return TaskStatus.Success;
            return TaskStatus.Failure;
        }
    }
}