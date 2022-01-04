using AssemblyAI.Actions;

namespace AI.Actions
{
    public static class ActionAimView
    {
        public static void Perform(AIEntity entity)
        {
            var hasTarget = entity.TargetKb.HasSeenTarget();
            if (hasTarget)
            {
                AimWeapon(entity);
            }
            // TODO Missing has specific look at target
            else
            {
                LookAround(entity);
            }
        }

        private static void AimWeapon(AIEntity entity)
        {
            entity.BotState.actionAimGun ??= new ActionAimGun(entity);
            entity.BotState.actionAimGun.Perform();
        }

        private static void LookAround(AIEntity entity)
        {
            entity.BotState.actionLookAround ??= new ActionLookAround(entity);
            entity.BotState.actionLookAround.Perform();
        }
    }

}

namespace BotSpace
{
    public partial class BotState
    {
        internal ActionLookAround actionLookAround;
        internal ActionAimGun actionAimGun;
    }
}