using AssemblyEntity.Component;

namespace AI.Actions
{
    public static class ActionSelectWeapon
    {
        public static void Perform(AIEntity entity)
        {
            // TODO We shouldn't directly use KB here, since we might be spotting enemies faster than usual for some
            // reason (e.g. searching after damage / fight). We need someone else here. Otherwise, we need to change
            // the KB to set the faster reaction time.
            var targetKb = entity.TargetKb;
            if (targetKb.HasSeenTarget())
            {
                // Since we have seen the target, we should select the best weapon to fight it
                ChooseWeaponForFight(entity);
            }
            else
            {
                // We haven't seen the target. We might as well reload all the weapons now. 
                // TODO Do not reload weapon if we are chasing the enemy? 
                ChooseWeaponToReload(entity);
            }
        }

        private static void ChooseWeaponForFight(AIEntity entity)
        {
            var enemyTransform = entity.GetEnemy().transform;
            var t = entity.transform;
            var gunManager = entity.GunManager;


            var distance = (enemyTransform.position - t.position).magnitude;
            var chosenIndex = -1;
            var bestScore = float.MinValue;
            for (var i = 0; i < gunManager.NumberOfGuns; i++)
            {
                if (gunManager.IsGunActive(i))
                {
                    var currentScore = gunManager.GetGunScore(i, distance);
                    if (currentScore > bestScore)
                    {
                        bestScore = currentScore;
                        chosenIndex = i;
                    }
                }
            }

            if (chosenIndex == -1)
            {
                // Find first active weapon
                chosenIndex = gunManager.FindLowestActiveGun();
            }

            gunManager.TryEquipGun(chosenIndex);
        }

        private static void ChooseWeaponToReload(AIEntity entity)
        {
            var gunManager = entity.GunManager;
            var gunCount = gunManager.NumberOfGuns;
            var mostUnchargedGun = GunManager.NO_GUN;
            var worstPercentage = 1f;
            for (var i = 0; i < gunCount; i++)
            {
                var ammoInCharger = gunManager.GetAmmoInChargerForGun(i);
                var chargerSize = gunManager.GetChargerSizeForGun(i);
                var percentage = ammoInCharger / (float) chargerSize;
                if (percentage < worstPercentage)
                {
                    mostUnchargedGun = i;
                    worstPercentage = percentage;
                }
            }

            if (mostUnchargedGun != GunManager.NO_GUN)
                gunManager.TryEquipGun(mostUnchargedGun);
        }
    }
}