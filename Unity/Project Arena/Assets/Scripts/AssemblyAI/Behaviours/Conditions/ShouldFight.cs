using AI.KnowledgeBase;
using BehaviorDesigner.Runtime.Tasks;

namespace AssemblyAI.Behaviours.Conditions
{
    public class ShouldFight : Conditional
    {
        private FightingMovementSkill skill;
        private TargetKnowledgeBase targetKb;
        
        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            targetKb = entity.TargetKb;
            skill = entity.MovementSkill;
        }

        public override TaskStatus OnUpdate()
        {
            if (skill == FightingMovementSkill.StandStill)
            {
                // Never fight back if I don't have the required movement skill!
                return TaskStatus.Failure;
            }
            return targetKb.HasSeenTarget() ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}