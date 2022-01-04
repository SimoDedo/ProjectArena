namespace AI.Actions
{
    public static class ActionWeaponReload
    {
        public static void Perform(AIEntity entity)
        {
            // TODO maybe use externally set flag to determine if I must reload or not
            if (!entity.TargetKb.HasSeenTarget())
            {
                var gunManager = entity.GunManager;
                var currentGun = gunManager.CurrentGunIndex;
                if (gunManager.CanGunReload(currentGun))
                {
                    gunManager.ReloadCurrentGun();
                }
            }
        }
    }
}