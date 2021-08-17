using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace AI
{
    public class ShouldLookForHealth: Conditional
    {
        public SharedInt thresholdLowHealth;
        private Entity entity;
        public override void OnAwake()
        {
            entity = GetComponent<Entity>();
        }

        public override TaskStatus OnUpdate()
        {
            return entity.health < thresholdLowHealth.Value ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}