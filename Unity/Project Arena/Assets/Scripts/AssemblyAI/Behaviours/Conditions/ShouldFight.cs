using AI.KnowledgeBase;
using AssemblyLogging;
using BehaviorDesigner.Runtime.Tasks;

namespace AssemblyAI.Behaviours.Conditions
{
    public class ShouldFight : Conditional
    {
        private FightingMovementSkill skill;
        private TargetKnowledgeBase targetKb;
        private AIEntity entity;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
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

            if (targetKb.HasSeenTarget())
            {
                FightingStatusGameEvent.Instance.Raise(
                    new FightingStatus {entityId = entity.GetID(), isActivelyFighting = true}
                );
                return TaskStatus.Success;
            }
            return TaskStatus.Failure;
        }
    }
}