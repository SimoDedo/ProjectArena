using AI.AI.Layer2;
using BehaviorDesigner.Runtime.Tasks;

namespace AI.Behaviours.Conditions
{
    public class HasAreaKnowledge : Conditional
    {
        private MapKnowledge knowledge;
        public override void OnAwake()
        {
            knowledge = GetComponent<AIEntity>().MapKnowledge;
        }

        public override TaskStatus OnUpdate()
        {
            return knowledge.CanBeUsed ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}