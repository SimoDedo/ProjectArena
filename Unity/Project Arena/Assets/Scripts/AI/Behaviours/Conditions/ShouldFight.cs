using AI.AI.Layer2;
using BehaviorDesigner.Runtime.Tasks;

namespace AI.Behaviours.Conditions
{
    public class ShouldFight : Conditional
    {
        private AIEntity entity;
        private FightingMovementSkill skill;
        private TargetKnowledgeBase targetKb;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            targetKb = entity.TargetKb;
            skill = entity.MovementSkill;
        }

        public override TaskStatus OnUpdate()
        {
            if (skill == FightingMovementSkill.StandStill)
                // Never fight back if I don't have the required movement skill!
                return TaskStatus.Failure;

            if (targetKb.HasSeenTarget())
            {
                entity.IsFocusingOnEnemy = true;
                return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }
    }
}