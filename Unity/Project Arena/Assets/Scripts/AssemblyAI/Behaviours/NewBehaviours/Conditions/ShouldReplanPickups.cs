using AI.KnowledgeBase;
using BehaviorDesigner.Runtime.Tasks;

namespace AI.Behaviours.NewBehaviours
{
    public class ShouldReplanPickups : Conditional
    {
        private PickupKnowledgeBase knowledgeBase;
        private float lastUpdateTime;
        
        public override void OnAwake()
        {
            knowledgeBase = GetComponent<PickupKnowledgeBase>();
            lastUpdateTime = 0;
        }

        public override TaskStatus OnUpdate()
        {
            var latestUpdateTime = knowledgeBase.GetLastUpdateTime();
            if (latestUpdateTime != lastUpdateTime)
            {
                lastUpdateTime = latestUpdateTime;
                return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }
    }
    // TODO PROBABLY REMOVE
}