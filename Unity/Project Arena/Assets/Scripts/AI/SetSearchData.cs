using AI.SharedVariables;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AI
{
    public class SetSearchData: Action
    {
        public SharedSearchData data;
        public bool valueToSet;

        public override TaskStatus OnUpdate()
        {
            data.Value = new SearchData {shouldSearch = valueToSet, timeSetFlag = Time.time};
            return TaskStatus.Success;
        }
    }
}